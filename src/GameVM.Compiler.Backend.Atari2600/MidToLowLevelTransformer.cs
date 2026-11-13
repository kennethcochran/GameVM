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
        // Error-context key naming the MLIR slab index in GameVmException
        // data dictionaries; shared by every throw site in this class so
        // the key is spelled one way everywhere.
        private const string SlabIndexKey = "SlabIndex";
        private const string TempPrefix = "__tmp_";
        private const string InvalidPoolOffsetPrefix = "<invalid_pool_offset";

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

        // Width in zero-page slots of each variable in the slab being
        // transformed (1 or 2), inferred up front by InferWidths.
        // Integer values are 16-bit: a variable takes two consecutive
        // slots, low byte first, when it holds a value above 255, is the
        // result of 16-bit arithmetic, or is copied from such a value.
        // A variable with no such evidence keeps one slot, so small
        // values keep the established single-slot lowering. Recomputed
        // for every TransformSlab call; allocation consults it through
        // WidthOf, which defaults an unrecorded name to one slot.
        private readonly Dictionary<string, int> _variableWidths = new(StringComparer.OrdinalIgnoreCase);

        // Counter for result slots invented by a wide Add/Sub that no
        // following Assign names (defensive only: real MLIR always
        // names the result temp in the next instruction).
        private int _wideTempCounter;

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

            // Widths are a property of the whole slab, not of the first
            // use: a declaration Assign(x, 0) precedes x := 300 in real
            // MLIR, so a single pass would fix x at one slot before ever
            // seeing the wide value. Infer every variable's width up
            // front, then allocate with the final widths.
            InferWidths(inputSlab, stringPool);

            // Indices consumed by a wide Add/Sub's lookahead: the
            // Assign(tmp, 0) that names its result temp, and the
            // Assign(target, tmp) when the result stores straight into
            // its target. Both are no-ops once the Add/Sub has stored
            // both bytes itself.
            var consumed = new HashSet<int>();

            var builder = new InstListBuilder();
            for (int i = 0; i < inputSlab.Count; i++)
            {
                if (consumed.Contains(i))
                    continue;

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
                                [SlabIndexKey] = i,
                                ["MlirKind"] = nameof(MlirInstructionKind.Output),
                                ["MlirKindByte"] = kind,
                            });

                    default:
                        MapArithmetic(inputSlab, i, kind, builder, stringPool, consumed);
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
        private void MapArithmetic(InstList inputSlab, int instIdx, byte kind, InstListBuilder builder, StringPool stringPool, HashSet<int> consumed)
        {
            ReadOnlySpan<uint> operands = inputSlab.GetOperands(instIdx);
            byte llirKind = MapToLlirKind(kind, instIdx);

            if (TryEmitMultiply(builder, stringPool, llirKind, operands, instIdx))
                return;
            if (TryEmitLoadThenOp(inputSlab, instIdx, builder, stringPool, llirKind, operands, consumed))
                return;
            if (TryEmitBitwiseNot(builder, stringPool, llirKind, operands, instIdx))
                return;

            EmitResolvedOp(builder, stringPool, llirKind, operands);
        }

        /// <summary>Maps an MLIR arithmetic/compare kind to its LLIR kind.
        /// A kind with no LLIR mapping fails loudly: emitting a Nop for it
        /// would silently drop the computation from the ROM.</summary>
        private static byte MapToLlirKind(byte kind, int instIdx)
        {
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
                        [SlabIndexKey] = instIdx,
                        ["MlirKindByte"] = kind,
                    });

            return llirKind;
        }

        /// <summary>Lowers a Multiply: Load the left operand, then Multiply
        /// with the right operand plus the shift-and-add scratch. A Multiply
        /// with fewer than two operands is malformed: the fallback would emit
        /// an underspecified LLIR Multiply that code generation cannot emit,
        /// silently truncating the ROM at that instruction. Returns false
        /// when the op is not a Multiply.</summary>
        private bool TryEmitMultiply(InstListBuilder builder, StringPool stringPool, byte llirKind, ReadOnlySpan<uint> operands, int instIdx)
        {
            if (llirKind != (byte)LlirInstructionKind.Multiply)
                return false;

            if (operands.Length < 2)
                throw new GameVmException(
                    ErrorCode.UnhandledInstruction,
                    $"MidToLowLevelTransformer cannot lower MLIR Multiply at slab index {instIdx}: " +
                    $"expected at least two operands (left, right), got {operands.Length}.",
                    ErrorSeverity.Error,
                    new Dictionary<string, object?>
                    {
                        [SlabIndexKey] = instIdx,
                        ["MlirKind"] = nameof(MlirInstructionKind.Multiply),
                        ["OperandCount"] = operands.Length,
                    });

            EmitMultiplyOp(builder, stringPool, operands);
            return true;
        }

        /// <summary>Whether a kind lowers as Load(left) then op(right), with
        /// the result in the accumulator: the binary arithmetic and bitwise
        /// ops (Add/Sub/BitwiseAnd/BitwiseOr) and Cmp, which needs A holding
        /// the left value before the compare.</summary>
        private static bool IsLoadThenOpKind(byte llirKind)
        {
            return llirKind == (byte)LlirInstructionKind.Add
                || llirKind == (byte)LlirInstructionKind.Sub
                || llirKind == (byte)LlirInstructionKind.BitwiseAnd
                || llirKind == (byte)LlirInstructionKind.BitwiseOr
                || llirKind == (byte)LlirInstructionKind.Cmp;
        }

        /// <summary>Lowers a two-operand op as Load(left) then op(right).
        /// Returns false when the kind is not a load-then-op kind or the op
        /// carries fewer than two operands; such short forms fall through to
        /// the resolved-operand fallback below. Add/Sub of 16-bit values
        /// take the wide path instead: a low byte pair that stores its
        /// result, then a high byte pair that consumes the low pair's
        /// carry (Add) or borrow (Sub).</summary>
        private bool TryEmitLoadThenOp(InstList inputSlab, int instIdx, InstListBuilder builder, StringPool stringPool, byte llirKind, ReadOnlySpan<uint> operands, HashSet<int> consumed)
        {
            if (!IsLoadThenOpKind(llirKind) || operands.Length < 2)
                return false;

            if ((llirKind == (byte)LlirInstructionKind.Add || llirKind == (byte)LlirInstructionKind.Sub)
                && ArithmeticWidth(operands, stringPool) == 2)
            {
                EmitWideLoadThenOp(inputSlab, instIdx, builder, stringPool, llirKind, operands, consumed);
                return true;
            }

            EmitLoadThenOp(builder, stringPool, llirKind, operands);
            return true;
        }

        /// <summary>
        /// Width of an Add/Sub result: the wider of its two operands. A
        /// literal contributes 2 only above 255; a name or temp
        /// contributes its inferred width (1 when nothing recorded one —
        /// widths are earned from evidence in the slab, never assumed).
        /// </summary>
        private int ArithmeticWidth(ReadOnlySpan<uint> operands, StringPool stringPool)
        {
            return Math.Max(OperandWidth(operands[0], stringPool), OperandWidth(operands[1], stringPool));
        }

        private int OperandWidth(uint slot, StringPool stringPool)
        {
            string text = stringPool.Resolve(slot);
            if (IsName(text))
            {
                if (text.StartsWith(TempPrefix, StringComparison.Ordinal))
                    return _variableWidths.TryGetValue(text, out int tempWidth) ? tempWidth : 1;
                return WidthOf(text);
            }
            if (TryParseNumeric(text, out uint numeric))
                return numeric > 255 ? 2 : 1;
            return 1;
        }

        /// <summary>
        /// Lowers a 16-bit Add/Sub as two byte pairs. The low pair loads
        /// the left low byte, adds/subtracts the right low byte (setting
        /// the carry), and stores the low result; the high pair loads the
        /// left high byte, adds/subtracts the right high byte with the
        /// CarryIn flag (no CLC/SEC — the low pair's carry/borrow must
        /// survive), and stores the high result. A one-slot operand has
        /// no high byte: its high contribution is immediate 0, which is
        /// exact — the missing byte is zero. The result stores straight
        /// into its ultimate target when the MLIR shape allows it (see
        /// ResolveWideResult), so no temp copy is needed.
        /// </summary>
        private void EmitWideLoadThenOp(InstList inputSlab, int instIdx, InstListBuilder builder, StringPool stringPool, byte llirKind, ReadOnlySpan<uint> operands, HashSet<int> consumed)
        {
            string destName = ResolveWideResult(inputSlab, instIdx, stringPool, consumed);
            ushort destAddr = GetAddressForVariable(destName);

            EmitLoadOperand(builder, stringPool, operands[0], high: false);
            builder.Add(llirKind, InstructionFlag.None, 0, AddOperands(stringPool, operands[1], high: false));
            builder.Add((byte)LlirInstructionKind.Store, InstructionFlag.None, 0, destAddr);

            EmitLoadOperand(builder, stringPool, operands[0], high: true);
            builder.Add(llirKind, InstructionFlag.CarryIn, 0, AddOperands(stringPool, operands[1], high: true));
            builder.Add((byte)LlirInstructionKind.Store, InstructionFlag.None, 0, (ushort)(destAddr + 1));
        }

        /// <summary>
        /// Finds where a wide Add/Sub result goes, consuming the MLIR
        /// Assigns that name it. The MLIR shape for `z := x + y` is
        /// Add(x, y); Assign(__tmp_0, 0); Assign(z, __tmp_0): the temp
        /// is only a name for the accumulator result, so when nothing
        /// else reads it, the result stores straight into z and both
        /// Assigns are consumed (they would otherwise store the high
        /// byte to the temp and copy it on — extra instructions that
        /// also leave the low byte stranded in the temp). When the temp
        /// IS read by a later arithmetic op, the result must live in
        /// the temp's own slots for that op to load, so only the
        /// Assign(tmp, 0) is consumed and the result goes to the temp.
        /// With no following Assign(tmp, 0) (hand-built MLIR), the
        /// result goes to a fresh wide temp.
        /// </summary>
        private string ResolveWideResult(InstList inputSlab, int instIdx, StringPool stringPool, HashSet<int> consumed)
        {
            int nextIdx = instIdx + 1;
            if (nextIdx >= inputSlab.Count
                || (MlirInstructionKind)inputSlab.GetKind(nextIdx) != MlirInstructionKind.Assign)
                return NewWideTemp();

            ReadOnlySpan<uint> nextOps = inputSlab.GetOperands(nextIdx);
            if (nextOps.Length < 2 || nextOps[1] != 0)
                return NewWideTemp();

            string tempName = stringPool.Resolve(nextOps[0]);
            if (!tempName.StartsWith(TempPrefix, StringComparison.Ordinal))
                return NewWideTemp();

            consumed.Add(nextIdx);

            int targetIdx = instIdx + 2;
            if (targetIdx < inputSlab.Count
                && (MlirInstructionKind)inputSlab.GetKind(targetIdx) == MlirInstructionKind.Assign)
            {
                ReadOnlySpan<uint> targetOps = inputSlab.GetOperands(targetIdx);
                if (targetOps.Length >= 2 && targetOps[1] == nextOps[0]
                    && !TempIsReadByLaterOperation(inputSlab, instIdx, nextOps[0]))
                {
                    consumed.Add(targetIdx);
                    return stringPool.Resolve(targetOps[0]);
                }
            }

            return tempName;
        }

        private string NewWideTemp()
        {
            string name = $"__tmp_wide_{_wideTempCounter++}";
            _variableWidths[name] = 2;
            return name;
        }

        /// <summary>Loads one byte of a wide operand into A: a variable
        /// loads the requested byte from its slot (the high byte lives in
        /// the slot after the low byte); a one-slot variable has no high
        /// byte, so its high load is an immediate 0; a literal loads the
        /// matching byte as an immediate.</summary>
        private void EmitLoadOperand(InstListBuilder builder, StringPool stringPool, uint slot, bool high)
        {
            string text = stringPool.Resolve(slot);
            if (IsName(text))
            {
                ushort addr = GetAddressForVariable(text);
                if (high && WidthOf(text) == 1)
                {
                    builder.Add((byte)LlirInstructionKind.Load, InstructionFlag.None, 0, 0);
                }
                else
                {
                    builder.Add((byte)LlirInstructionKind.Load, InstructionFlag.None, 0, (ushort)(high ? addr + 1 : addr), 0);
                }
            }
            else if (TryParseNumeric(text, out uint numeric))
            {
                uint byteValue = high ? (numeric >> 8) & 0xFF : numeric & 0xFF;
                builder.Add((byte)LlirInstructionKind.Load, InstructionFlag.None, 0, byteValue);
            }
            else
            {
                builder.Add((byte)LlirInstructionKind.Load, InstructionFlag.None, 0, 0);
            }
        }

        /// <summary>One byte of a wide Add/Sub's right operand, in LLIR
        /// operand form: a variable contributes the address of the slot
        /// holding that byte; a one-slot variable's high contribution is
        /// immediate 0; a literal contributes the matching byte as an
        /// immediate.</summary>
        private uint[] AddOperands(StringPool stringPool, uint slot, bool high)
        {
            string text = stringPool.Resolve(slot);
            if (IsName(text))
            {
                ushort addr = GetAddressForVariable(text);
                if (high && WidthOf(text) == 1)
                    return new uint[] { 0 };
                return new uint[] { (ushort)(high ? addr + 1 : addr), 0 };
            }
            if (TryParseNumeric(text, out uint numeric))
                return new uint[] { high ? (numeric >> 8) & 0xFF : numeric & 0xFF };
            return new uint[] { 0 };
        }

        /// <summary>Lowers a BitwiseNot: Load the operand into A, then
        /// complement it with EOR #$FF. The op carries no operands of its
        /// own — the value to complement is the accumulator. A BitwiseNot
        /// with no MLIR operand is malformed: the fallback would emit a bare
        /// complement over a stale accumulator. Returns false when the op is
        /// not a BitwiseNot.</summary>
        private bool TryEmitBitwiseNot(InstListBuilder builder, StringPool stringPool, byte llirKind, ReadOnlySpan<uint> operands, int instIdx)
        {
            if (llirKind != (byte)LlirInstructionKind.BitwiseNot)
                return false;

            if (operands.Length < 1)
                throw new GameVmException(
                    ErrorCode.UnhandledInstruction,
                    $"MidToLowLevelTransformer cannot lower MLIR BitwiseNot at slab index {instIdx}: " +
                    $"expected one operand (the value to complement), got {operands.Length}.",
                    ErrorSeverity.Error,
                    new Dictionary<string, object?>
                    {
                        [SlabIndexKey] = instIdx,
                        ["MlirKind"] = nameof(LlirInstructionKind.BitwiseNot),
                        ["OperandCount"] = operands.Length,
                    });

            EmitLoadLeft(builder, stringPool, operands);
            builder.Add((byte)LlirInstructionKind.BitwiseNot, InstructionFlag.None, 0);
            return true;
        }

        /// <summary>Fallback for single-operand forms (and Load): resolve
        /// each operand to a machine operand and emit the op as-is.</summary>
        private void EmitResolvedOp(InstListBuilder builder, StringPool stringPool, byte llirKind, ReadOnlySpan<uint> operands)
        {
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
                !name.StartsWith(InvalidPoolOffsetPrefix, StringComparison.Ordinal) &&
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
                   !text.StartsWith(InvalidPoolOffsetPrefix, StringComparison.Ordinal) &&
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
            if (targetName.StartsWith(TempPrefix, StringComparison.Ordinal))
            {
                if (TempIsReadByLaterOperation(inputSlab, instIdx, targetSlot))
                    builder.Add((byte)LlirInstructionKind.Store, InstructionFlag.None, 0, GetAddressForVariable(targetName));
                return;
            }

            ushort targetAddr = GetAddressForVariable(targetName);

            string valueStr = stringPool.Resolve(valueSlot);
            bool isTemp = valueStr.StartsWith(TempPrefix, StringComparison.Ordinal);
            bool valueIsName = valueStr.Length > 0 &&
                               !valueStr.StartsWith(InvalidPoolOffsetPrefix, StringComparison.Ordinal) &&
                               !IsNumeric(valueStr);

            if (isTemp)
            {
                // The computed value already lives in the accumulator (A) from the
                // preceding Add/Sub. Store it: STA zp. A two-slot temp's bytes
                // are already in memory — a wide Add/Sub stored both — so
                // copy both bytes from the temp's slots instead.
                if (WidthOf(valueStr) == 2)
                {
                    ushort tempAddr = GetAddressForVariable(valueStr);
                    builder.Add((byte)LlirInstructionKind.Load, InstructionFlag.None, 0, tempAddr, 0);
                    builder.Add((byte)LlirInstructionKind.Store, InstructionFlag.None, 0, targetAddr);
                    builder.Add((byte)LlirInstructionKind.Load, InstructionFlag.None, 0, (ushort)(tempAddr + 1), 0);
                    builder.Add((byte)LlirInstructionKind.Store, InstructionFlag.None, 0, (ushort)(targetAddr + 1));
                }
                else
                {
                    builder.Add((byte)LlirInstructionKind.Store, InstructionFlag.None, 0, targetAddr);
                }
            }
            else if (valueIsName)
            {
                // Variable copy: LDA zp; STA zp, low byte then high byte
                // when the value is two slots. A one-slot source copied
                // into a two-slot target leaves a stale high byte, so the
                // high copy loads immediate 0 instead of the source's
                // (nonexistent) high byte.
                ushort valueAddr = GetAddressForVariable(valueStr);
                builder.Add((byte)LlirInstructionKind.Load, InstructionFlag.None, 0, valueAddr, 0);
                builder.Add((byte)LlirInstructionKind.Store, InstructionFlag.None, 0, targetAddr);
                if (Math.Max(WidthOf(valueStr), WidthOf(targetName)) == 2)
                {
                    if (WidthOf(valueStr) == 2)
                        builder.Add((byte)LlirInstructionKind.Load, InstructionFlag.None, 0, (ushort)(valueAddr + 1), 0);
                    else
                        builder.Add((byte)LlirInstructionKind.Load, InstructionFlag.None, 0, 0);
                    builder.Add((byte)LlirInstructionKind.Store, InstructionFlag.None, 0, (ushort)(targetAddr + 1));
                }
            }
            else if (TryParseNumeric(valueStr, out uint numeric))
            {
                // Compile-time constant: LDA #imm; STA zp (folded). A
                // two-slot target takes both bytes even when the value
                // fits in one — the high byte must be 0, not whatever
                // the slot held before (zero at reset, stale after a
                // previous wide assignment to the same variable).
                builder.Add((byte)LlirInstructionKind.Assign, InstructionFlag.None, 0, targetAddr, numeric & 0xFF);
                if (WidthOf(targetName) == 2)
                    builder.Add((byte)LlirInstructionKind.Assign, InstructionFlag.None, 0, (ushort)(targetAddr + 1), (numeric >> 8) & 0xFF);
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

        /// <summary>
        /// Width in zero-page slots (1 or 2) of a variable: its inferred
        /// width, or 1 when nothing recorded it. TIA registers are fixed
        /// single-byte hardware addresses, never variables, so they are
        /// always width 1 even if a wide literal is assigned to one.
        /// </summary>
        private int WidthOf(string name)
        {
            if (_addressMap.ContainsKey(name))
                return 1;
            return _variableWidths.TryGetValue(name, out int width) ? width : 1;
        }

        /// <summary>
        /// Infers each variable's width for the slab (see the
        /// _variableWidths field). Width is the maximum over all the
        /// evidence in the slab, so a declaration Assign(x, 0) does not
        /// pin x to one slot when a later x := 300 needs two. A name
        /// with no evidence keeps one slot: widths are earned from what
        /// the slab shows, never assumed, so bare variable arithmetic
        /// keeps the established single-slot lowering. Evidence:
        ///  - Assign(target, literal): 2 when the literal exceeds 255.
        ///  - Assign(target, name): the source's width (1 when the
        ///    source has no recorded width).
        ///  - Assign(target, __tmp_N): the temp's width (1 when the
        ///    temp has no recorded width).
        ///  - Add/Sub: the wider operand's width (an operand with no
        ///    recorded width counts as 1). The result temp (named by
        ///    the following Assign(tmp, 0)) takes the result width.
        ///  - Multiply, BitwiseAnd/Or: 8-bit ops; their result temp
        ///    stays width 1, and their operands gain no width evidence.
        ///  - BitwiseNot: the result temp takes its operand's width.
        /// Cmp, Load, Call, Return, Branch and friends carry no width
        /// evidence: comparisons stay 8-bit (a separate slice), and a
        /// variable that only ever appears in one keeps one slot.
        /// Assigns to a __tmp_ target are the op-result marker, not a
        /// value assignment, and record nothing.
        /// </summary>
        private void InferWidths(InstList inputSlab, StringPool stringPool)
        {
            _variableWidths.Clear();
            for (int i = 0; i < inputSlab.Count; i++)
            {
                byte kind = inputSlab.GetKind(i);
                ReadOnlySpan<uint> operands = inputSlab.GetOperands(i);

                switch (kind)
                {
                    case (byte)MlirInstructionKind.Assign:
                        InferAssignWidth(operands, stringPool);
                        break;
                    case (byte)LlirInstructionKind.Add:
                    case (byte)LlirInstructionKind.Sub:
                        InferArithmeticWidth(inputSlab, i, operands, stringPool);
                        break;
                    case (byte)LlirInstructionKind.Multiply:
                    case (byte)LlirInstructionKind.BitwiseAnd:
                    case (byte)LlirInstructionKind.BitwiseOr:
                        InferResultTempWidth(inputSlab, i, 1, stringPool);
                        break;
                    case (byte)LlirInstructionKind.BitwiseNot:
                        InferBitwiseNotWidth(inputSlab, i, operands, stringPool);
                        break;
                }
            }
        }

        private void InferAssignWidth(ReadOnlySpan<uint> operands, StringPool stringPool)
        {
            if (operands.Length < 2)
                return;

            string targetName = stringPool.Resolve(operands[0]);
            if (string.IsNullOrEmpty(targetName) || targetName.StartsWith(TempPrefix, StringComparison.Ordinal))
                return;

            string valueStr = stringPool.Resolve(operands[1]);
            if (valueStr.StartsWith(TempPrefix, StringComparison.Ordinal))
            {
                RecordWidth(targetName, _variableWidths.TryGetValue(valueStr, out int tempWidth) ? tempWidth : 1);
            }
            else if (valueStr.Length > 0
                && !valueStr.StartsWith(InvalidPoolOffsetPrefix, StringComparison.Ordinal)
                && !IsNumeric(valueStr))
            {
                // Copy source: the target takes the source's width (1
                // when nothing recorded one for the source).
                int sourceWidth = _variableWidths.TryGetValue(valueStr, out int recorded) ? recorded : 1;
                RecordWidth(targetName, sourceWidth);
            }
            else if (TryParseNumeric(valueStr, out uint numeric))
            {
                RecordWidth(targetName, numeric > 255 ? 2 : 1);
            }
        }

        private void InferArithmeticWidth(InstList inputSlab, int instIdx, ReadOnlySpan<uint> operands, StringPool stringPool)
        {
            if (operands.Length < 2)
                return;

            int leftWidth = InferOperandWidth(operands[0], stringPool);
            int rightWidth = InferOperandWidth(operands[1], stringPool);
            InferResultTempWidth(inputSlab, instIdx, Math.Max(leftWidth, rightWidth), stringPool);
        }

        /// <summary>
        /// Width of an arithmetic operand for inference: a literal by
        /// value; a name or temp by its recorded width (1 when
        /// unrecorded).
        /// </summary>
        private int InferOperandWidth(uint slot, StringPool stringPool)
        {
            string text = stringPool.Resolve(slot);
            if (IsName(text))
            {
                if (text.StartsWith(TempPrefix, StringComparison.Ordinal))
                    return _variableWidths.TryGetValue(text, out int tempWidth) ? tempWidth : 1;
                return _variableWidths.TryGetValue(text, out int varWidth) ? varWidth : 1;
            }
            if (TryParseNumeric(text, out uint numeric))
                return numeric > 255 ? 2 : 1;
            return 1;
        }

        /// <summary>
        /// Records the result temp of the op at <paramref name="instIdx"/>
        /// (named by the following Assign(tmp, 0)) at
        /// <paramref name="width"/>.
        /// </summary>
        private void InferResultTempWidth(InstList inputSlab, int instIdx, int width, StringPool stringPool)
        {
            int nextIdx = instIdx + 1;
            if (nextIdx >= inputSlab.Count
                || (MlirInstructionKind)inputSlab.GetKind(nextIdx) != MlirInstructionKind.Assign)
                return;

            ReadOnlySpan<uint> nextOps = inputSlab.GetOperands(nextIdx);
            if (nextOps.Length < 2 || nextOps[1] != 0)
                return;

            string tempName = stringPool.Resolve(nextOps[0]);
            if (tempName.StartsWith(TempPrefix, StringComparison.Ordinal))
                RecordWidth(tempName, width);
        }

        private void InferBitwiseNotWidth(InstList inputSlab, int instIdx, ReadOnlySpan<uint> operands, StringPool stringPool)
        {
            if (operands.Length < 1)
                return;

            // The complement is one byte, but the result temp inherits
            // its operand's width so a later copy or Add treats the
            // value consistently.
            InferResultTempWidth(inputSlab, instIdx, InferOperandWidth(operands[0], stringPool), stringPool);
        }

        /// <summary>Raises a variable's recorded width to at least
        /// <paramref name="width"/>; widths only ever grow within a
        /// slab (see InferWidths).</summary>
        private void RecordWidth(string name, int width)
        {
            if (_variableWidths.TryGetValue(name, out int existing))
            {
                if (width > existing)
                    _variableWidths[name] = width;
            }
            else
            {
                _variableWidths[name] = width;
            }
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
                // A two-slot variable takes two consecutive addresses,
                // low byte first, so the high byte is always low + 1.
                // The second allocation throws exactly like the first
                // when the extent runs into the reserved $FE-$FF: a
                // variable that cannot hold both bytes fails loudly
                // instead of silently sharing its high byte with the
                // stack or a neighbour.
                addr = AllocateZeroPageAddress(name);
                if (WidthOf(name) == 2)
                    AllocateZeroPageAddress(name);
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