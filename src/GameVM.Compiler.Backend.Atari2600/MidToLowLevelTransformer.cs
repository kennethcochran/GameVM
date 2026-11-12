using System;
using System.Collections.Generic;
using GameVM.Compiler.Core.IR;
using GameVM.Compiler.Core.IR.Soa;
using GameVM.Compiler.Core.IR.Buffers;
using GameVM.Compiler.Core.IR.Interfaces;
using GameVM.Compiler.Core.Enums;
using GameVM.Compiler.Core.Exceptions;

namespace GameVM.Compiler.Backend.Atari2600
{
    /// <summary>
    /// Transforms MLIR (Mid-Level IR) to LLIR (Low-Level IR) for the Atari 2600 target.
    /// Resolves StringPool offsets to zero-page addresses (deterministic $80-$FD allocation
    /// with $FE-$FF reserved for the 6502 stack, TIA register mapping), and lowers semantic
    /// arithmetic/compare/branch/call into LLIR Load/Store/Add/Sub/Cmp/Branch/Jump instructions.
    /// </summary>
    public class MidToLowLevelTransformer : IIRSlabTransformer
    {
        private readonly Dictionary<string, string> _addressMap = new(StringComparer.OrdinalIgnoreCase);

        // Deterministic allocation of user variables to zero-page addresses
        // ($80-$FD); $FE-$FF are reserved for the 6502 stack — the 2600's
        // 128 bytes of RAM are shared between zero page and stack, and the
        // outermost JSR return address is pushed at $01FF/$01FE.
        private readonly Dictionary<string, ushort> _userAddresses = new(StringComparer.OrdinalIgnoreCase);

        // Compiler-internal scratch (multiply routine) lives in its own
        // name map, so a user variable named "__mul_m" gets a different
        // address than the scratch. Both maps share the address cursor
        // below, so no two names ever share an address.
        private readonly Dictionary<string, ushort> _scratchAddresses = new(StringComparer.OrdinalIgnoreCase);
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

                    case MlirInstructionKind.Transition:
                        // Branch polarity marker: consumed, not copied.
                        // ProcessBranch (for the Branch that follows) reads
                        // this marker from the input slab to decide polarity
                        // and re-emits it ahead of the LLIR Branch, so
                        // emitting it here as well would duplicate it.
                        break;

                    case MlirInstructionKind.Bcs:
                        // Carry-set marker: consumed, not copied.
                        // ProcessBranch (for the Branch that follows) reads
                        // this marker from the input slab and re-emits it
                        // ahead of the LLIR Branch, so emitting it here as
                        // well would duplicate it.
                        break;

                    case MlirInstructionKind.Call:
                        ProcessCall(inputSlab, i, builder);
                        break;

                    case MlirInstructionKind.Return:
                        uint retVal = operands.Length > 0 ? operands[0] : 0;
                        // Preserve instruction flags (notably CalleeReturn)
                        // so the code generator can tell a callee return
                        // (RTS) from the program-entry return (halt loop).
                        builder.Add((byte)LlirInstructionKind.Return, (InstructionFlag)inputSlab.GetFlags(i), 0, retVal);
                        break;

                    case MlirInstructionKind.Nop:
                        // Benign no-op (empty statement, empty block): skip
                        // it. Emits zero bytes — on a 4K 2600 ROM, padding
                        // NOPs for scaffolding would be pure waste.
                        break;

                    case MlirInstructionKind.Output:
                        // write/writeln have no lowering on this target: the
                        // 2600 has no console. Fail loudly instead of silently
                        // dropping the construct.
                        throw new GameVmException(
                            ErrorCode.UnhandledInstruction,
                            $"MidToLowLevelTransformer has no LLIR lowering for MLIR instruction " +
                            $"'Output' (kind 0x{kind:X2}) at slab index {i}. The construct is not " +
                            "supported on the Atari 2600 target (write/writeln require a console).",
                            ErrorSeverity.Error,
                            new Dictionary<string, object?>
                            {
                                ["SlabIndex"] = i,
                                ["MlirKind"] = nameof(MlirInstructionKind.Output),
                                ["MlirKindByte"] = kind,
                            });

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
                (byte)LlirInstructionKind.Multiply => (byte)LlirInstructionKind.Multiply,
                (byte)LlirInstructionKind.BitwiseAnd => (byte)LlirInstructionKind.BitwiseAnd,
                (byte)LlirInstructionKind.BitwiseOr => (byte)LlirInstructionKind.BitwiseOr,
                (byte)LlirInstructionKind.BitwiseNot => (byte)LlirInstructionKind.BitwiseNot,
                (byte)LlirInstructionKind.Cmp => (byte)LlirInstructionKind.Cmp,
                (byte)LlirInstructionKind.Load => (byte)LlirInstructionKind.Load,
                _ => (byte)LlirInstructionKind.Nop
            };

            if (llirKind == (byte)LlirInstructionKind.Nop)
                throw new GameVmException(
                    ErrorCode.UnhandledInstruction,
                    $"MidToLowLevelTransformer has no LLIR mapping for MLIR instruction " +
                    $"kind 0x{kind:X2} at slab index {instIdx}.",
                    ErrorSeverity.Error,
                    new Dictionary<string, object?>
                    {
                        ["SlabIndex"] = instIdx,
                        ["MlirKindByte"] = kind,
                    });

            // A Multiply with fewer than two operands is malformed: the
            // fallback below would emit an underspecified LLIR Multiply
            // that code generation cannot emit, silently truncating the
            // ROM at that instruction.
            if (llirKind == (byte)LlirInstructionKind.Multiply && operands.Length < 2)
                throw new GameVmException(
                    ErrorCode.UnhandledInstruction,
                    $"MidToLowLevelTransformer cannot lower MLIR Multiply at slab index {instIdx}: " +
                    $"expected at least two operands (left, right), got {operands.Length}.",
                    ErrorSeverity.Error,
                    new Dictionary<string, object?>
                    {
                        ["SlabIndex"] = instIdx,
                        ["MlirKind"] = nameof(MlirInstructionKind.Multiply),
                        ["OperandCount"] = operands.Length,
                    });

            // Multiply: Load the left operand, then Multiply with the right
            // operand plus two scratch addresses for the shift-and-add routine.
            if (llirKind == (byte)LlirInstructionKind.Multiply && operands.Length >= 2)
            {
                EmitMultiplyOp(builder, stringPool, operands);
                return;
            }

            // Binary arithmetic (Add|Sub) and bitwise (BitwiseAnd|BitwiseOr):
            // emit Load(left) then op(right), accumulator holds result
            if ((llirKind == (byte)LlirInstructionKind.Add || llirKind == (byte)LlirInstructionKind.Sub
                || llirKind == (byte)LlirInstructionKind.BitwiseAnd || llirKind == (byte)LlirInstructionKind.BitwiseOr)
                && operands.Length >= 2)
            {
                EmitLoadThenOp(builder, stringPool, llirKind, operands);
                return;
            }

            // BitwiseNot: Load the operand into A, then complement it with
            // EOR #$FF. The op carries no operands of its own — the value
            // to complement is the accumulator. A BitwiseNot with no MLIR
            // operand is malformed: the fallback below would emit a bare
            // complement over a stale accumulator.
            if (llirKind == (byte)LlirInstructionKind.BitwiseNot && operands.Length < 1)
                throw new GameVmException(
                    ErrorCode.UnhandledInstruction,
                    $"MidToLowLevelTransformer cannot lower MLIR BitwiseNot at slab index {instIdx}: " +
                    $"expected one operand (the value to complement), got {operands.Length}.",
                    ErrorSeverity.Error,
                    new Dictionary<string, object?>
                    {
                        ["SlabIndex"] = instIdx,
                        ["MlirKind"] = nameof(LlirInstructionKind.BitwiseNot),
                        ["OperandCount"] = operands.Length,
                    });

            if (llirKind == (byte)LlirInstructionKind.BitwiseNot && operands.Length >= 1)
            {
                EmitLoadLeft(builder, stringPool, operands);
                builder.Add((byte)LlirInstructionKind.BitwiseNot, InstructionFlag.None, 0);
                return;
            }

            // Cmp: emit Load(left) first so A holds the value, then CMP.
            if (llirKind == (byte)LlirInstructionKind.Cmp && operands.Length >= 2)
            {
                EmitLoadThenOp(builder, stringPool, llirKind, operands);
                return;
            }

            // Existing behavior for Cmp and single-operand forms below
            var resolved = new uint[operands.Length];
            for (int i = 0; i < operands.Length; i++)
                resolved[i] = ResolveSlot(operands[i], stringPool);
            builder.Add(llirKind, InstructionFlag.None, 0, resolved);
        }

        // Binary op sequence shared by Add/Sub/Cmp: Load the left operand
        // into A, then apply the op with the right operand.
        private void EmitLoadThenOp(InstListBuilder builder, StringPool stringPool, byte llirKind, ReadOnlySpan<uint> operands)
        {
            EmitLoadLeft(builder, stringPool, operands);
            builder.Add(llirKind, InstructionFlag.None, 0, RightOperands(stringPool, operands));
        }

        // Loads the left operand of a binary op into A: a variable loads its
        // VALUE from memory ([addr, 0] -> zp form); a literal loads immediate.
        private void EmitLoadLeft(InstListBuilder builder, StringPool stringPool, ReadOnlySpan<uint> operands)
        {
            string leftText = stringPool.Resolve(operands[0]);
            if (IsName(leftText))
            {
                ushort leftAddr = GetAddressForVariable(leftText);
                builder.Add((byte)LlirInstructionKind.Load, InstructionFlag.None, 0, leftAddr, 0);
            }
            else
            {
                uint leftVal = ResolveSlot(operands[0], stringPool);
                builder.Add((byte)LlirInstructionKind.Load, InstructionFlag.None, 0, leftVal);
            }
        }

        // The right operand of a binary op as the leading operands of the op
        // instruction: a variable contributes its VALUE from memory
        // ([addr, 0] -> zp form) — never its address as an immediate; a
        // literal contributes its value. extraOperands are appended after
        // the right operand (Multiply's two scratch addresses).
        private uint[] RightOperands(StringPool stringPool, ReadOnlySpan<uint> operands, params uint[] extraOperands)
        {
            string rightText = stringPool.Resolve(operands[1]);
            uint[] right;
            if (IsName(rightText))
            {
                ushort rightAddr = GetAddressForVariable(rightText);
                right = new uint[] { rightAddr, 0 };
            }
            else
            {
                right = new uint[] { ResolveSlot(operands[1], stringPool) };
            }
            if (extraOperands.Length == 0)
                return right;
            uint[] combined = new uint[right.Length + extraOperands.Length];
            Array.Copy(right, combined, right.Length);
            Array.Copy(extraOperands, 0, combined, right.Length, extraOperands.Length);
            return combined;
        }

        /// <summary>
        /// Multiply sequence: Load the left operand into A, then Multiply with
        /// the right operand. The 6502 has no multiply, so the backend lowers
        /// Multiply to an inline shift-and-add routine that needs two scratch
        /// zero-page bytes — a copy of the multiplicand, shifted left each
        /// iteration, and a copy of the multiplier, shifted right. Scratch is
        /// allocated through a separate internal-name map (never the user
        /// variable map), so a user variable named "__mul_m" gets its own
        /// address and is never overwritten by the routine; the scratch is
        /// shared across multiplies in one program. LLIR Multiply operands:
        /// variable right is (rightAddr, 0, scratchM, scratchN); literal right
        /// is (rightVal, scratchM, scratchN). The code generator tells them
        /// apart by operand count (4 vs 3).
        /// </summary>
        private void EmitMultiplyOp(InstListBuilder builder, StringPool stringPool, ReadOnlySpan<uint> operands)
        {
            EmitLoadLeft(builder, stringPool, operands);

            ushort scratchM = GetAddressForScratch("__mul_m");
            ushort scratchN = GetAddressForScratch("__mul_n");

            builder.Add((byte)LlirInstructionKind.Multiply, InstructionFlag.None, 0,
                RightOperands(stringPool, operands, scratchM, scratchN));
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

        private static bool IsName(string text)
        {
            return text.Length > 0 &&
                   !text.StartsWith("<invalid_pool_offset", StringComparison.Ordinal) &&
                   !IsNumeric(text);
        }

        private void ProcessAssign(InstList inputSlab, int instIdx, InstListBuilder builder, StringPool stringPool)
        {
            ReadOnlySpan<uint> operands = inputSlab.GetOperands(instIdx);
            if (operands.Length < 2) return;

            uint targetSlot = operands[0];
            uint valueSlot = operands[1];

            string targetName = stringPool.Resolve(targetSlot);
            if (string.IsNullOrEmpty(targetName)) return;

            // "__tmp_*" names are virtual registers backed by the accumulator:
            // Assign(tmp, ...) is a declaration/no-op (value was computed in A).
            // Except when a later operation reads the temp as an operand (a
            // nested expression like (a * b) * c): that operation loads the
            // temp from memory, so the value must be stored here first, or
            // the load reads an unwritten zero-page address.
            if (targetName.StartsWith("__tmp_", StringComparison.Ordinal))
            {
                if (TempIsReadByLaterOperation(inputSlab, instIdx, targetSlot))
                    builder.Add((byte)LlirInstructionKind.Store, InstructionFlag.None, 0, GetAddressForVariable(targetName));
                return;
            }

            ushort targetAddr = GetAddressForVariable(targetName);

            string valueStr = stringPool.Resolve(valueSlot);
            bool isTemp = valueStr.StartsWith("__tmp_", StringComparison.Ordinal);
            bool valueIsName = valueStr.Length > 0 &&
                               !valueStr.StartsWith("<invalid_pool_offset", StringComparison.Ordinal) &&
                               !IsNumeric(valueStr);

            if (isTemp)
            {
                // The computed value already lives in the accumulator (A) from the
                // preceding Add/Sub. Store it: STA zp.
                builder.Add((byte)LlirInstructionKind.Store, InstructionFlag.None, 0, targetAddr);
            }
            else if (valueIsName)
            {
                // Variable copy: LDA zp; STA zp.
                ushort valueAddr = GetAddressForVariable(valueStr);
                builder.Add((byte)LlirInstructionKind.Load, InstructionFlag.None, 0, valueAddr, 0);
                builder.Add((byte)LlirInstructionKind.Store, InstructionFlag.None, 0, targetAddr);
            }
            else if (TryParseNumeric(valueStr, out uint numeric))
            {
                // Compile-time constant: LDA #imm; STA zp (folded).
                builder.Add((byte)LlirInstructionKind.Assign, InstructionFlag.None, 0, targetAddr, numeric & 0xFF);
            }
            else
            {
                builder.Add((byte)LlirInstructionKind.Assign, InstructionFlag.None, 0, targetAddr, valueSlot & 0xFF);
            }
        }

        /// <summary>
        /// Whether a later MLIR instruction reads <paramref name="tempSlot"/>
        /// as an operand of an arithmetic/compare op (Add/Sub/Multiply/Cmp)
        /// or a bitwise op (BitwiseAnd/BitwiseOr/BitwiseNot).
        /// Such a read loads the temp from its zero-page address, so the temp
        /// must be stored to memory when it is assigned. A temp consumed only
        /// by the Assign that stores it into its real target reads the
        /// accumulator directly and needs no store.
        /// </summary>
        private static bool TempIsReadByLaterOperation(InstList inputSlab, int instIdx, uint tempSlot)
        {
            for (int j = instIdx + 1; j < inputSlab.Count; j++)
            {
                byte kind = inputSlab.GetKind(j);
                if (kind != (byte)LlirInstructionKind.Add
                    && kind != (byte)LlirInstructionKind.Sub
                    && kind != (byte)LlirInstructionKind.Multiply
                    && kind != (byte)LlirInstructionKind.BitwiseAnd
                    && kind != (byte)LlirInstructionKind.BitwiseOr
                    && kind != (byte)LlirInstructionKind.BitwiseNot
                    && kind != (byte)LlirInstructionKind.Cmp)
                    continue;
                ReadOnlySpan<uint> operands = inputSlab.GetOperands(j);
                for (int k = 0; k < operands.Length; k++)
                {
                    if (operands[k] == tempSlot)
                        return true;
                }
            }
            return false;
        }

        private static void ProcessBranch(InstList inputSlab, int instIdx, InstListBuilder builder)
        {
            ReadOnlySpan<uint> operands = inputSlab.GetOperands(instIdx);
            if (operands.Length < 1) return;

            uint targetLabel = operands[0];

            bool isConditional = false;
            bool invert = false;
            bool carrySet = false;

            if (instIdx > 0)
            {
                byte prevKind = inputSlab.GetKind(instIdx - 1);
                if (prevKind == (byte)LlirInstructionKind.Transition)
                {
                    invert = true;

                    if (instIdx > 1 && inputSlab.GetKind(instIdx - 2) == (byte)LlirInstructionKind.Cmp)
                        isConditional = true;
                }
                else if (prevKind == (byte)LlirInstructionKind.Bcs)
                {
                    carrySet = true;

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
                // Emit Transition BEFORE the Branch so the codegen sees
                // lastWasTransition=true and selects BEQ (not BNE).
                if (invert)
                    builder.Add((byte)LlirInstructionKind.Transition, InstructionFlag.None, 0);
                // Emit Bcs BEFORE the Branch so the codegen sees the
                // pending carry-set marker and selects BCS (not BNE).
                if (carrySet)
                    builder.Add((byte)LlirInstructionKind.Bcs, InstructionFlag.None, 0);
                builder.Add((byte)LlirInstructionKind.Branch, InstructionFlag.None, 0, targetLabel);
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

            // Function calls are emitted as single-operand Calls carrying the
            // function's label pool-offset; the codegen resolves it to a ROM
            // address via labelOffsets, exactly like Jump.
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
                addr = AllocateZeroPageAddress(name);
                _userAddresses[name] = addr;
            }
            return addr;
        }

        /// <summary>
        /// Allocates a zero-page address for a compiler-internal name
        /// (multiply scratch). Internal names live in their own map, never
        /// in the user-variable map, so a user variable with the same name
        /// gets a different address. The address cursor is shared with
        /// user variables.
        /// </summary>
        private ushort GetAddressForScratch(string name)
        {
            if (!_scratchAddresses.TryGetValue(name, out ushort addr))
            {
                addr = AllocateZeroPageAddress(name);
                _scratchAddresses[name] = addr;
            }
            return addr;
        }

        /// <summary>
        /// Hands out the next zero-page address ($80-$FD). $FE-$FF are
        /// reserved for the 6502 stack: the reset prologue sets SP to $FF,
        /// so the outermost JSR pushes its return address at $01FF/$01FE —
        /// the same physical RAM as $FF/$FE, since zero page and stack
        /// share the 2600's 128 bytes. Allocating there would let a
        /// variable (or the multiply scratch) be overwritten by the return
        /// address, or overwrite it.
        /// </summary>
        private ushort AllocateZeroPageAddress(string name)
        {
            if (_nextUserAddress > 0xFD)
                throw new GameVmException(
                    ErrorCode.ZeroPageExhausted,
                    $"MidToLowLevelTransformer: zero-page exhausted while allocating '{name}'. " +
                    "The Atari 2600 has 128 bytes of RAM ($80-$FF) shared between variables and the " +
                    "6502 stack; $FE-$FF are reserved for the stack (the outermost JSR return address), " +
                    "so $80-$FD are available for variables. Reduce the number of live variables.",
                    ErrorSeverity.Error,
                    new Dictionary<string, object?>
                    {
                        ["VariableName"] = name,
                        ["AllocatedCount"] = _userAddresses.Count + _scratchAddresses.Count,
                    });
            ushort addr = _nextUserAddress;
            _nextUserAddress++;
            return addr;
        }

        private void InitializeAddressMap()
        {
            // TIA write-register map, verified against the Stella Programmer's
            // Guide "TIA WRITE ADDRESS SUMMARY"
            // (https://alienbill.com/2600/101/docs/stella.html).
            // NOTE: TIA read registers (CXM0P..INPT5 at $00-$0D) share addresses
            // with this write map; the table below covers STA targets only.
            // Loads from TIA read registers are not yet supported.
            _addressMap.Clear();
            _addressMap["VSYNC"] = "$00";
            _addressMap["VBLANK"] = "$01";
            _addressMap["WSYNC"] = "$02";
            _addressMap["RSYNC"] = "$03";
            _addressMap["NUSIZ0"] = "$04";
            _addressMap["NUSIZ1"] = "$05";
            _addressMap["COLUP0"] = "$06";
            _addressMap["COLUP1"] = "$07";
            _addressMap["COLUPF"] = "$08";
            _addressMap["COLUBK"] = "$09";
            _addressMap["CTRLPF"] = "$0A";
            _addressMap["REFP0"] = "$0B";
            _addressMap["REFP1"] = "$0C";
            _addressMap["PF0"] = "$0D";
            _addressMap["PF1"] = "$0E";
            _addressMap["PF2"] = "$0F";
            _addressMap["RESP0"] = "$10";
            _addressMap["RESP1"] = "$11";
            _addressMap["RESM0"] = "$12";
            _addressMap["RESM1"] = "$13";
            _addressMap["RESBL"] = "$14";
            _addressMap["AUDC0"] = "$15";
            _addressMap["AUDC1"] = "$16";
            _addressMap["AUDF0"] = "$17";
            _addressMap["AUDF1"] = "$18";
            _addressMap["AUDV0"] = "$19";
            _addressMap["AUDV1"] = "$1A";
            _addressMap["GRP0"] = "$1B";
            _addressMap["GRP1"] = "$1C";
            _addressMap["ENAM0"] = "$1D";
            _addressMap["ENAM1"] = "$1E";
            _addressMap["ENABL"] = "$1F";
            _addressMap["HMP0"] = "$20";
            _addressMap["HMP1"] = "$21";
            _addressMap["HMM0"] = "$22";
            _addressMap["HMM1"] = "$23";
            _addressMap["HMBL"] = "$24";
            _addressMap["VDELP0"] = "$25";
            _addressMap["VDELP1"] = "$26";
            _addressMap["VDELBL"] = "$27";
            _addressMap["RESMP0"] = "$28";
            _addressMap["RESMP1"] = "$29";
            _addressMap["HMOVE"] = "$2A";
            _addressMap["HMCLR"] = "$2B";
            _addressMap["CXCLR"] = "$2C";
        }
    }
}