using GameVM.Compiler.Core.DOD;

namespace GameVM.Compiler.Core.Utilities;

/// <summary>
/// Bit manipulation utilities for encoding instruction metadata into a 32-bit value.
/// Wraps <c>InstructionMetadata.Encode</c> and the field setters so callers that only
/// need write-side access to the self-describing instruction header can avoid depending on
/// the full <c>InstructionMetadata</c> surface.
/// </summary>
public static class MetadataEncoder
{
    /// <summary>
    /// Encodes the full metadata header.
    /// Layout: bits 0-7 kind, 8-13 size, 14-19 arg count, 20 terminator, 21 diagnostic.
    /// </summary>
    public static uint Encode(byte kind, byte size, byte argCount, bool isTerminator = false, bool hasDiagnostic = false)
        => InstructionMetadata.Encode(kind, size, argCount, isTerminator, hasDiagnostic);

    /// <summary>Sets or clears the terminator flag (bit 20).</summary>
    public static uint SetTerminator(uint metadata, bool isTerminator)
        => InstructionMetadata.SetTerminator(metadata, isTerminator);

    /// <summary>Sets or clears the diagnostic-present flag (bit 21).</summary>
    public static uint SetDiagnostic(uint metadata, bool hasDiagnostic)
        => InstructionMetadata.SetDiagnostic(metadata, hasDiagnostic);

    /// <summary>Creates a NOP (no-operation) instruction metadata (kind 0, size 1, no args).</summary>
    public static uint CreateNop() => InstructionMetadata.CreateNop();
}
