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
}