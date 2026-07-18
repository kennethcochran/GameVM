using GameVM.Compiler.Core.IR.Slab;

namespace GameVM.Compiler.Core.Utilities;

/// <summary>
/// Helper functions for decoding instruction metadata from a 32-bit encoded value.
/// Thin stateless wrappers over <c>InstructionMetadata</c> for callers that only need
/// read-side access to the self-describing instruction header.
/// </summary>
public static class MetadataDecoder
{
    /// <summary>Decodes the instruction kind (bits 0-7) from metadata.</summary>
    public static byte DecodeKind(uint metadata) => InstructionMetadata.DecodeKind(metadata);

    /// <summary>Decodes the block size in uints (bits 8-13) from metadata.</summary>
    public static byte DecodeSize(uint metadata) => InstructionMetadata.DecodeSize(metadata);

    /// <summary>Decodes the argument count (bits 14-19) from metadata.</summary>
    public static byte DecodeArgCount(uint metadata) => InstructionMetadata.DecodeArgCount(metadata);

    /// <summary>Decodes the terminator flag (bit 20) from metadata.</summary>
    public static bool DecodeIsTerminator(uint metadata) => InstructionMetadata.DecodeIsTerminator(metadata);

    /// <summary>Decodes the diagnostic-present flag (bit 21) from metadata.</summary>
    public static bool DecodeHasDiagnostic(uint metadata) => InstructionMetadata.DecodeHasDiagnostic(metadata);

    /// <summary>Returns the reserved bits (bits 22-31) from metadata.</summary>
    public static uint GetReserved(uint metadata) => InstructionMetadata.GetReserved(metadata);

    /// <summary>Returns true if the metadata represents a NOP instruction (kind 0).</summary>
    public static bool IsNop(uint metadata) => InstructionMetadata.IsNop(metadata);
}
