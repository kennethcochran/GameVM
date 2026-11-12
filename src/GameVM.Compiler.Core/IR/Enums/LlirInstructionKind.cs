namespace GameVM.Compiler.Core.IR;

/// <summary>
/// LLIR instruction kind enumeration for type discrimination.
/// Values correspond to InstructionMetadataFlags.KindMask.
/// </summary>
public enum LlirInstructionKind : byte
{
    /// <summary>Unknown or invalid instruction</summary>
    Unknown = 0,

    /// <summary>NOP / empty instruction</summary>
    Nop = 0,

    /// <summary>Label instruction for control flow</summary>
    Label = 192,

    /// <summary>Load instruction (LDA #imm / LDA zp)</summary>
    Load = 193,

    /// <summary>Store instruction (STA zp / STA abs)</summary>
    Store = 194,

    /// <summary>Call instruction (JSR)</summary>
    Call = 195,

    /// <summary>Jump instruction (JMP)</summary>
    Jump = 196,

    /// <summary>Branch instruction (relative conditional)</summary>
    Branch = 197,

    /// <summary>Return instruction (RTS)</summary>
    Return = 198,

    /// <summary>Syscall instruction (JSR to a fixed address)</summary>
    Syscall = 199,

    /// <summary>Add: A += operand (ADC)</summary>
    Add = 200,

    /// <summary>Subtract: A -= operand (SBC)</summary>
    Sub = 201,

    /// <summary>Compare: set flags A vs operand (CMP)</summary>
    Cmp = 202,

    /// <summary>Branch if not equal (BNE) — branch-to-target-when-true for ==</summary>
    Bne = 203,

    /// <summary>Branch if equal (BEQ) — branch-to-target-when-true for !=</summary>
    Beq = 204,

    /// <summary>Branch if plus/positive (BPL)</summary>
    Bpl = 205,

    /// <summary>Branch if minus/negative (BMI)</summary>
    Bmi = 206,

    /// <summary>
    /// Marks a branch polarity transition: the following <see cref="Branch"/> is
    /// inverted relative to the HLIR semantic "branch-to-target-when-true"
    /// convention. Emits no bytes; only meaningful immediately before a Branch.
    /// </summary>
    Transition = 207,

    /// <summary>Assign: operands[0]=targetAddr, operands[1]=value (immediate).</summary>
    Assign = 208,

    /// <summary>
    /// Marks the following <see cref="Branch"/> as a carry-set branch (BCS):
    /// after a Cmp, carry is set exactly when the left operand is greater
    /// than or equal to the right, so BCS skips exactly when a less-than
    /// condition is false. Emits no bytes; only meaningful immediately
    /// before a Branch.
    /// </summary>
    Bcs = 209,

    /// <summary>
    /// Multiply: 8-bit unsigned product. Operands are the right operand plus
    /// two scratch zero-page addresses (see MidToLowLevelTransformer):
    /// variable right is (rightAddr, 0, scratchM, scratchN), literal right
    /// is (rightVal, scratchM, scratchN). The left operand arrives in the
    /// accumulator via a preceding Load. The backend lowers it to an inline
    /// shift-and-add routine and leaves the product in the accumulator.
    /// </summary>
    Multiply = 210,

    /// <summary>
    /// BitwiseAnd: 8-bit bitwise AND. Operands follow the Add/Sub convention
    /// (see MidToLowLevelTransformer): the left operand arrives in the
    /// accumulator via a preceding Load, the right operand is (rightAddr, 0)
    /// for a variable or (rightVal) for a literal. The backend emits AND
    /// (zp or immediate) and leaves the result in the accumulator.
    /// </summary>
    BitwiseAnd = 211,

    /// <summary>
    /// BitwiseOr: 8-bit bitwise OR. Operands follow the Add/Sub convention
    /// (see MidToLowLevelTransformer): the left operand arrives in the
    /// accumulator via a preceding Load, the right operand is (rightAddr, 0)
    /// for a variable or (rightVal) for a literal. The backend emits ORA
    /// (zp or immediate) and leaves the result in the accumulator.
    /// </summary>
    BitwiseOr = 212,

    /// <summary>
    /// BitwiseNot: 8-bit bitwise complement. Carries no operands: the value
    /// arrives in the accumulator via a preceding Load. The backend emits
    /// EOR #$FF and leaves the complement in the accumulator.
    /// </summary>
    BitwiseNot = 213,
}