using System;
using System.Collections.Generic;
using GameVM.Compiler.Core.IR;
using GameVM.Compiler.Core.IR.Soa;
using GameVM.Compiler.Core.IR.Buffers;
using GameVM.Compiler.Core.IR.Interfaces;
using GameVM.Compiler.Core.Enums;

namespace GameVM.Compiler.Backend.Atari2600
{
    /// <summary>
    /// Transforms MLIR (Mid-Level IR) to LLIR (Low-Level IR) for the Atari 2600 target.
    /// Resolves StringPool offsets to zero-page addresses (deterministic $80+ allocation,
    /// TIA register mapping), and lowers semantic arithmetic/compare/branch/call into
    /// LLIR Load/Store/Add/Sub/Cmp/Branch/Jump instructions.
    /// </summary>
    public class MidToLowLevelTransformer : IIRSlabTransformer
    {
        private readonly Dictionary<string, string> _addressMap = new(StringComparer.OrdinalIgnoreCase);

        // Deterministic allocation of user variables to zero-page addresses ($80-$FF)
        private readonly Dictionary<string, ushort> _userAddresses = new(StringComparer.OrdinalIgnoreCase);
        private ushort _nextUserAddress = 0x80;

        public MidToLowLevelTransformer()
        {
            InitializeAddressMap();
        }

        /// <summary>
        /// Transforms an MLIR slab to LLIR slab for Atari 2600 target.
        /// </summary>
        public InstList TransformSlab(InstList inputSlab, StringPool stringPool)
        {
            if (inputSlab.Count == 0)
            {
                return Empty();
            }

            var builder = new InstListBuilder();

            for (int i = 0; i < inputSlab.Count; i++)
            {
                byte kind = inputSlab.GetKind(i);
                ReadOnlySpan<uint> operands = inputSlab.GetOperands(i);

                switch ((MlirInstructionKind)kind)
                {
                    case MlirInstructionKind.Label:
                        uint labelName = operands.Length > 0 ? operands[0] : 0;
                        builder.Add((byte)LlirInstructionKind.Label, InstructionFlag.None, 0, labelName);
                        break;

                    case MlirInstructionKind.Assign:
                        ProcessAssign(inputSlab, i, builder, stringPool);
                        break;

                    case MlirInstructionKind.Branch:
                        ProcessBranch(inputSlab, i, builder);
                        break;

                    case MlirInstructionKind.Call:
                        ProcessCall(inputSlab, i, builder);
                        break;

                    case MlirInstructionKind.Return:
                        uint retVal = operands.Length > 0 ? operands[0] : 0;
                        builder.Add((byte)LlirInstructionKind.Return, InstructionFlag.None, 0, retVal);
                        break;

                    case MlirInstructionKind.Nop:
                        // write/writeln stub placeholder — no LLIR emitted.
                        break;

                    default:
                        MapArithmetic(inputSlab, i, kind, builder, stringPool);
                        break;
                }
            }

            return builder.Build();
        }

        private static InstList Empty()
        {
            return new InstList(
                Array.Empty<byte>(),
                Array.Empty<ushort>(),
                Array.Empty<ushort>(),
                Array.Empty<uint>(),
                Array.Empty<uint>(),
                Array.Empty<uint>(),
                Array.Empty<int>(),
                0,
                0);
        }

        /// <summary>Maps an arithmetic/compare MLIR op to its LLIR op with resolved operands.</summary>
        private void MapArithmetic(InstList inputSlab, int instIdx, byte kind, InstListBuilder builder, StringPool stringPool)
        {
            ReadOnlySpan<uint> operands = inputSlab.GetOperands(instIdx);

            byte llirKind = kind switch
            {
                (byte)LlirInstructionKind.Add => (byte)LlirInstructionKind.Add,
                (byte)LlirInstructionKind.Sub => (byte)LlirInstructionKind.Sub,
                (byte)LlirInstructionKind.Cmp => (byte)LlirInstructionKind.Cmp,
                (byte)LlirInstructionKind.Load => (byte)LlirInstructionKind.Load,
                _ => (byte)LlirInstructionKind.Nop
            };

            if (llirKind == (byte)LlirInstructionKind.Nop)
                return;

            var resolved = new uint[operands.Length];
            for (int i = 0; i < operands.Length; i++)
            {
                resolved[i] = ResolveSlot(operands[i], stringPool);
            }

            builder.Add(llirKind, InstructionFlag.None, 0, resolved);
        }

        /// <summary>
        /// Resolves a slot to a machine operand: if it is a valid pool string that
        /// names a variable/register, returns the zero-page address; a numeric string
        /// (or any other non-name) is a literal immediate.
        /// </summary>
        private ushort ResolveSlot(uint slot, StringPool stringPool)
        {
            string name = stringPool.Resolve(slot);
            if (name.Length > 0 &&
                !name.StartsWith("<invalid_pool_offset", StringComparison.Ordinal) &&
                !IsNumeric(name))
            {
                return GetAddressForVariable(name);
            }

            // Literal: the slot is either the numeric-ish text (parse it) or the value itself.
            if (name.Length > 0 && TryParseNumeric(name, out uint numeric))
                return (ushort)(numeric & 0xFF);

            return (ushort)(slot & 0xFF);
        }

        private static bool TryParseNumeric(string text, out uint value)
        {
            value = 0;
            if (text.Length > 1 && text[0] == '0' && (text[1] == 'x' || text[1] == 'X'))
            {
                if (ushort.TryParse(text.AsSpan(2), System.Globalization.NumberStyles.HexNumber, null, out ushort hex))
                {
                    value = hex;
                    return true;
                }
                return false;
            }
            return uint.TryParse(text, out value);
        }
        private static bool IsNumeric(string text)
        {
            if (text.Length > 1 && text[0] == '0' && (text[1] == 'x' || text[1] == 'X'))
                return ushort.TryParse(text.AsSpan(2), System.Globalization.NumberStyles.HexNumber, null, out _);
            return uint.TryParse(text, out _) ||
                   int.TryParse(text, out _) ||
                   (text.Length > 0 && text[0] == '-' && int.TryParse(text, out _));
        }

        private void ProcessAssign(InstList inputSlab, int instIdx, InstListBuilder builder, StringPool stringPool)
        {
            ReadOnlySpan<uint> operands = inputSlab.GetOperands(instIdx);
            if (operands.Length < 2) return;

            uint targetSlot = operands[0];
            uint valueSlot = operands[1];

            string targetName = stringPool.Resolve(targetSlot);
            if (string.IsNullOrEmpty(targetName)) return;

            ushort targetAddr = GetAddressForVariable(targetName);

            string valueStr = stringPool.Resolve(valueSlot);
            bool valueIsName = valueStr.Length > 0 &&
                               !valueStr.StartsWith("<invalid_pool_offset", StringComparison.Ordinal) &&
                               !IsNumeric(valueStr);

            uint valueOperand;
            if (valueIsName)
            {
                valueOperand = GetAddressForVariable(valueStr);
            }
            else if (TryParseNumeric(valueStr, out uint numeric))
            {
                valueOperand = numeric & 0xFF;
            }
            else
            {
                valueOperand = valueSlot & 0xFF;
            }

            // Emit a single Assign: LDA #imm; STA zp
            builder.Add((byte)LlirInstructionKind.Assign, InstructionFlag.None, 0, targetAddr, valueOperand);
        }

        private static void ProcessBranch(InstList inputSlab, int instIdx, InstListBuilder builder)
        {
            ReadOnlySpan<uint> operands = inputSlab.GetOperands(instIdx);
            if (operands.Length < 1) return;

            uint targetLabel = operands[0];

            bool isConditional = false;
            bool invert = false;

            if (instIdx > 0)
            {
                byte prevKind = inputSlab.GetKind(instIdx - 1);
                if (prevKind == (byte)LlirInstructionKind.Transition)
                {
                    invert = true;
                    if (instIdx > 1 && inputSlab.GetKind(instIdx - 2) == (byte)LlirInstructionKind.Cmp)
                        isConditional = true;
                }
                else if (prevKind == (byte)LlirInstructionKind.Cmp)
                {
                    isConditional = true;
                }
                else if (prevKind == (byte)LlirInstructionKind.Load)
                {
                    isConditional = true;
                }
            }

            if (isConditional)
            {
                builder.Add((byte)LlirInstructionKind.Branch, InstructionFlag.None, 0, targetLabel);
                if (invert)
                    builder.Add((byte)LlirInstructionKind.Transition, InstructionFlag.None, 0);
            }
            else
            {
                builder.Add((byte)LlirInstructionKind.Jump, InstructionFlag.None, 0, targetLabel);
            }
        }

        private static void ProcessCall(InstList inputSlab, int instIdx, InstListBuilder builder)
        {
            ReadOnlySpan<uint> operands = inputSlab.GetOperands(instIdx);
            if (operands.Length < 1) return;

            uint funcOffset = operands[0];

            // Function calls are emitted as JSRs; the target address is resolved later.
            builder.Add((byte)LlirInstructionKind.Call, InstructionFlag.None, 0, funcOffset);
        }

        private ushort GetAddressForVariable(string name)
        {
            bool isTia = _addressMap.TryGetValue(name, out string? addrStr);
            if (isTia && addrStr != null)
            {
                if (addrStr.Length > 0 && addrStr[0] == '$')
                    addrStr = addrStr.Substring(1);
                if (ushort.TryParse(addrStr, System.Globalization.NumberStyles.HexNumber, null, out ushort parsedAddr))
                    return parsedAddr;
            }

            if (!_userAddresses.TryGetValue(name, out ushort addr))
            {
                addr = _nextUserAddress;
                _userAddresses[name] = addr;
                _nextUserAddress = (ushort)((_nextUserAddress + 1) & 0xFF);
            }
            return addr;
        }

        private void InitializeAddressMap()
        {
            _addressMap.Clear();
            _addressMap["COLUBK"] = "$09";
            _addressMap["COLUPF"] = "$08";
            _addressMap["COLUP0"] = "$06";
            _addressMap["COLUP1"] = "$07";
            _addressMap["PF0"] = "$0D";
            _addressMap["PF1"] = "$0E";
            _addressMap["PF2"] = "$0F";
            _addressMap["RESP0"] = "$01";
            _addressMap["RESP1"] = "$02";
            _addressMap["RESM0"] = "$03";
            _addressMap["RESM1"] = "$04";
            _addressMap["RESBL"] = "$05";
            _addressMap["AUDC0"] = "$02";
            _addressMap["AUDC1"] = "$06";
            _addressMap["AUDF0"] = "$04";
            _addressMap["AUDF1"] = "$08";
            _addressMap["AUDV0"] = "$03";
            _addressMap["AUDV1"] = "$07";
            _addressMap["WSYNC"] = "$02";
            _addressMap["RSYNC"] = "$04";
            _addressMap["NUSIZ0"] = "$0B";
            _addressMap["NUSIZ1"] = "$0C";
            _addressMap["RESF0"] = "$07";
            _addressMap["RESF1"] = "$08";
            _addressMap["HMP0"] = "$00";
            _addressMap["HMP1"] = "$01";
            _addressMap["HMM0"] = "$02";
            _addressMap["HMM1"] = "$03";
            _addressMap["HMPG"] = "$04";
            _addressMap["HMBL"] = "$05";
            _addressMap["VDELP0"] = "$0B";
            _addressMap["VDELP1"] = "$0C";
            _addressMap["VDELBL"] = "$0D";
            _addressMap["RESET"] = "$FF";
        }
    }
}