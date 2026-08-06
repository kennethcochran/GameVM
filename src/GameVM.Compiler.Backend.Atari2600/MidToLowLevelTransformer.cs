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
    /// Handles 6502-specific code generation and memory mapping.
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
        /// <param name="inputSlab">The MLIR instruction list to transform.</param>
        /// <param name="stringPool">String pool for symbol resolution.</param>
        /// <returns>The transformed LLIR instruction list.</returns>
        public InstList TransformSlab(InstList inputSlab, StringPool stringPool)
        {
            if (inputSlab.Count == 0)
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

            var builder = new InstListBuilder();
            int functionCount = 0;

            // Iterate through input MLIR slab
            int i = 0;
            while (i < inputSlab.Count)
            {
                int instIdx = i;
                byte kind = inputSlab.GetKind(instIdx);

                if (kind == (byte)MlirInstructionKind.Label)
                {
                    // Emit label instruction with function name hash
                    ReadOnlySpan<uint> operands = inputSlab.GetOperands(instIdx);
                    uint funcNameHash = operands.Length > 0 ? operands[0] : 0;
                    builder.Add((byte)LlirInstructionKind.Label, InstructionFlag.None, 0, funcNameHash);
                    functionCount++;

                    // Process function body until next label or end
                    int j = instIdx + 1;
                    while (j < inputSlab.Count && inputSlab.GetKind(j) != (byte)MlirInstructionKind.Label)
                    {
                        var instKind = (MlirInstructionKind)inputSlab.GetKind(j);
                        ProcessInstruction(inputSlab, j, instKind, builder, stringPool);
                        j++;
                    }
                    // Advance past the function body (j is now the next label, or end)
                    // This modifies the loop counter to skip processed instructions - required for correct control flow
                    i = j;
                }
                else
                {
                    // Non-label instruction - process and advance
                    var instKind = (MlirInstructionKind)kind;
                    ProcessInstruction(inputSlab, instIdx, instKind, builder, stringPool);
                    i++;
                }
            }

            return builder.Build();
        }

        private void ProcessInstruction(InstList inputSlab, int instIndex, MlirInstructionKind kind, InstListBuilder builder, StringPool stringPool)
        {
            switch (kind)
            {
                case MlirInstructionKind.Assign:
                    ProcessAssignment(inputSlab, instIndex, builder, stringPool);
                    break;
                default:
                    // Map MLIR kind to LLIR kind and copy operands
                    byte llirKind = MapMlirKindToLlirKind(kind);
                    ReadOnlySpan<uint> operands = inputSlab.GetOperands(instIndex);
                    builder.Add(llirKind, InstructionFlag.None, 0, operands);
                    break;
            }
        }

        private void ProcessAssignment(InstList inputSlab, int instIndex, InstListBuilder builder, StringPool stringPool)
        {
            // MLIR_ASSIGN: [metadata, targetSlotId, valueSlotId]
            // SlotIds are string pool offsets
            ReadOnlySpan<uint> operands = inputSlab.GetOperands(instIndex);
            if (operands.Length < 2) return;

            uint targetPoolOffset = operands[0];
            uint valuePoolOffset = operands[1];

            // Resolve target variable name from string pool
            string targetName = stringPool.Resolve(targetPoolOffset);
            if (string.IsNullOrEmpty(targetName)) return;

            // Resolve value expression from string pool
            string valueExpr = stringPool.Resolve(valuePoolOffset);
            if (valueExpr == null) return;

            // Determine if value is a numeric literal
            bool isNumericLiteral = false;
            ushort numericValue = 0;

            // Try to parse as hex (0xFF format) or decimal
            if (valueExpr.Length >= 2 && (valueExpr[0] == '0' && (valueExpr[1] == 'x' || valueExpr[1] == 'X')))
            {
                if (ushort.TryParse(valueExpr.Substring(2), System.Globalization.NumberStyles.HexNumber, null, out ushort hexValue))
                {
                    isNumericLiteral = true;
                    numericValue = hexValue;
                }
            }
            else if (ushort.TryParse(valueExpr, out ushort decValue))
            {
                isNumericLiteral = true;
                numericValue = decValue;
            }

            // Get target address
            ushort targetAddr = 0;
            bool isTiaRegister = _addressMap.TryGetValue(targetName, out string? addrStr);
            if (isTiaRegister && addrStr != null)
            {
                // TIA register address
                if (addrStr.Length > 0 && addrStr[0] == '$')
                {
                    addrStr = addrStr.Substring(1);
                }
                if (ushort.TryParse(addrStr, System.Globalization.NumberStyles.HexNumber, null, out ushort parsedAddr))
                {
                    targetAddr = parsedAddr;
                }
            }
            else
            {
                // User variable - assign to zero-page starting at $80
                // Deterministic allocation: first var -> $80, second -> $81, etc.
                if (!_userAddresses.TryGetValue(targetName, out ushort addr))
                {
                    addr = _nextUserAddress;
                    _userAddresses[targetName] = addr;
                    _nextUserAddress = (ushort)((_nextUserAddress + 1) & 0xFF); // Wrap at $100
                }
                targetAddr = addr;
            }

            if (isNumericLiteral)
            {
                // Emit: LDA #value (immediate), STA addr (zero-page)
                // Load: [dummyReg, immediateValue] -> codegen reads operand[1] as value
                builder.Add((byte)LlirInstructionKind.Load, InstructionFlag.None, 0, 0, numericValue);
                // Store: [targetReg=0, addressLow, addressHigh] -> codegen reads operand[1] as addrLow
                builder.Add((byte)LlirInstructionKind.Store, InstructionFlag.None, 0, 0, targetAddr, 0);
            }
            else
            {
                // Value is a variable name - load from that variable's address
                ushort valueAddr = GetAddressForVariable(valueExpr);

                // Emit: LDA addr (absolute), STA addr (zero-page)
                builder.Add((byte)LlirInstructionKind.Load, InstructionFlag.None, 0, 0, (ushort)(valueAddr & 0xFF), (ushort)((valueAddr >> 8) & 0xFF));
                builder.Add((byte)LlirInstructionKind.Store, InstructionFlag.None, 0, 0, targetAddr, 0);
            }
        }

        private ushort GetAddressForVariable(string name)
        {
            bool isTia = _addressMap.TryGetValue(name, out string? addrStr);
            if (isTia && addrStr != null)
            {
                if (addrStr.Length > 0 && addrStr[0] == '$')
                {
                    addrStr = addrStr.Substring(1);
                }
                if (ushort.TryParse(addrStr, System.Globalization.NumberStyles.HexNumber, null, out ushort parsedAddr))
                {
                    return parsedAddr;
                }
            }

            // User variable - deterministic allocation
            if (!_userAddresses.TryGetValue(name, out ushort addr))
            {
                addr = _nextUserAddress;
                _userAddresses[name] = addr;
                _nextUserAddress = (ushort)((_nextUserAddress + 1) & 0xFF);
            }
            return addr;
        }

        private static byte MapMlirKindToLlirKind(MlirInstructionKind kind)
        {
            return kind switch
            {
                MlirInstructionKind.Assign => (byte)LlirInstructionKind.Store,
                MlirInstructionKind.Branch => (byte)LlirInstructionKind.Branch,
                MlirInstructionKind.Call => (byte)LlirInstructionKind.Call,
                MlirInstructionKind.Return => (byte)LlirInstructionKind.Return,
                MlirInstructionKind.Variable => (byte)LlirInstructionKind.Load,
                MlirInstructionKind.Block => (byte)LlirInstructionKind.Nop,
                MlirInstructionKind.ExpressionStatement => (byte)LlirInstructionKind.Nop,
                _ => (byte)LlirInstructionKind.Nop
            };
        }

        private void InitializeAddressMap()
        {
            // Atari 2600 TIA registers
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