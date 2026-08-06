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

    // Bits 2-15 reserved for future use
}
