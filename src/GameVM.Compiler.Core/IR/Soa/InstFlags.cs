using System;

namespace GameVM.Compiler.Core.IR.Soa;

/// <summary>
/// Bitwise flags for instructions in an <see cref="InstList"/>.
/// Uses ushort (16 bits) for compact storage.
/// </summary>
[Flags]
public enum InstructionFlag : ushort
{
    /// <summary>No flags set.</summary>
    None = 0,

    /// <summary>Instruction terminates a basic block (branch, return, etc.).</summary>
    Terminator = 1 << 0,

    /// <summary>Diagnostic information is available for this instruction (cold path).</summary>
    Diagnostic = 1 << 1,

    /// <summary>
    /// On an Add/Sub instruction: consume the carry flag left by the
    /// preceding arithmetic instead of setting it first. The high byte
    /// of a 16-bit Add/Sub carries this flag, so the code generator
    /// emits a bare ADC/SBC (no CLC/SEC) and the low byte's carry or
    /// borrow flows into the high byte.
    /// </summary>
    CarryIn = 1 << 3,

    /// <summary>
    /// On a Return instruction: the return belongs to a callable body (a
    /// function/procedure reached via Call), not to the program entry.
    /// Backends emit a true subroutine return (e.g. 6502 RTS) for flagged
    /// returns; the unflagged entry return has no caller and typically
    /// ends in a halt loop instead.
    /// </summary>
    CalleeReturn = 1 << 2,

    // Bits 4-15 reserved for future use
}
