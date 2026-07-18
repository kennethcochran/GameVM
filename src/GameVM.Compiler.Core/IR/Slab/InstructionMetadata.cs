using System;

namespace GameVM.Compiler.Core.IR.Slab;

/// <summary>
/// Helper class for encoding and decoding 32-bit instruction metadata.
/// Metadata layout:
/// - Bits 0-7: Instruction Kind (256 kinds)
/// - Bits 8-13: Instruction Block Size (max 64 uints)
/// - Bits 14-19: Argument Count (max 64 arguments)
/// - Bit 20: Terminator Flag (basic block boundary)
/// - Bit 21: Diagnostic Present Flag (cold-path debug info available)
/// - Bits 22-31: Reserved for future flags
/// </summary>
public struct InstructionMetadata
{
    public const byte KindMask = 0xFF;              // Bits 0-7
    public const ushort SizeMask = 0x3F00;         // Bits 8-13
    public const uint ArgCountMask = 0xFC000;      // Bits 14-19
    public const uint TerminatorMask = 0x100000;   // Bit 20
    public const uint DiagnosticMask = 0x200000;   // Bit 21
    public const uint ReservedMask = 0xFFC00000;  // Bits 22-31

    private const byte KindShift = 0;
    private const byte SizeShift = 8;
    private const byte ArgCountShift = 14;

    /// <summary>
    /// Encodes instruction metadata into a 32-bit value.
    /// </summary>
    /// <param name="kind">Instruction kind (0-255)</param>
    /// <param name="size">Block size in uints (0-63)</param>
    /// <param name="argCount">Argument count (0-63)</param>
    /// <param name="isTerminator">Whether this instruction terminates a basic block</param>
    /// <param name="hasDiagnostic">Whether diagnostic information is available</param>
    /// <returns>Encoded 32-bit metadata</returns>
    public static uint Encode(byte kind, byte size, byte argCount, bool isTerminator = false, bool hasDiagnostic = false)
    {
        if (kind > KindMask)
            throw new ArgumentOutOfRangeException(nameof(kind), $"Instruction kind must be 0-{KindMask}");
        if (size > (SizeMask >> SizeShift))
            throw new ArgumentOutOfRangeException(nameof(size), $"Block size must be 0-{SizeMask >> SizeShift}");
        if (argCount > (ArgCountMask >> ArgCountShift))
            throw new ArgumentOutOfRangeException(nameof(argCount), $"Argument count must be 0-{ArgCountMask >> ArgCountShift}");

        uint metadata = (uint)(kind << KindShift);
        metadata |= (uint)(size << SizeShift);
        metadata |= (uint)(argCount << ArgCountShift);

        if (isTerminator)
            metadata |= TerminatorMask;

        if (hasDiagnostic)
            metadata |= DiagnosticMask;

        return metadata;
    }

    /// <summary>
    /// Decodes the instruction kind from metadata.
    /// </summary>
    /// <param name="metadata">Encoded metadata</param>
    /// <returns>Instruction kind (0-255)</returns>
    public static byte DecodeKind(uint metadata)
    {
        return (byte)((metadata & KindMask) >> KindShift);
    }

    /// <summary>
    /// Decodes the block size from metadata.
    /// </summary>
    /// <param name="metadata">Encoded metadata</param>
    /// <returns>Block size in uints (0-63)</returns>
    public static byte DecodeSize(uint metadata)
    {
        return (byte)((metadata & SizeMask) >> SizeShift);
    }

    /// <summary>
    /// Decodes the argument count from metadata.
    /// </summary>
    /// <param name="metadata">Encoded metadata</param>
    /// <returns>Argument count (0-63)</returns>
    public static byte DecodeArgCount(uint metadata)
    {
        return (byte)((metadata & ArgCountMask) >> ArgCountShift);
    }

    /// <summary>
    /// Decodes the terminator flag from metadata.
    /// </summary>
    /// <param name="metadata">Encoded metadata</param>
    /// <returns>True if this instruction terminates a basic block</returns>
    public static bool DecodeIsTerminator(uint metadata)
    {
        return (metadata & TerminatorMask) != 0;
    }

    /// <summary>
    /// Decodes the diagnostic present flag from metadata.
    /// </summary>
    /// <param name="metadata">Encoded metadata</param>
    /// <returns>True if diagnostic information is available</returns>
    public static bool DecodeHasDiagnostic(uint metadata)
    {
        return (metadata & DiagnosticMask) != 0;
    }

    /// <summary>
    /// Sets the terminator flag in metadata.
    /// </summary>
    /// <param name="metadata">Original metadata</param>
    /// <param name="isTerminator">New terminator flag value</param>
    /// <returns>Updated metadata</returns>
    public static uint SetTerminator(uint metadata, bool isTerminator)
    {
        if (isTerminator)
            return metadata | TerminatorMask;
        else
            return metadata & ~TerminatorMask;
    }

    /// <summary>
    /// Sets the diagnostic present flag in metadata.
    /// </summary>
    /// <param name="metadata">Original metadata</param>
    /// <param name="hasDiagnostic">New diagnostic flag value</param>
    /// <returns>Updated metadata</returns>
    public static uint SetDiagnostic(uint metadata, bool hasDiagnostic)
    {
        if (hasDiagnostic)
            return metadata | DiagnosticMask;
        else
            return metadata & ~DiagnosticMask;
    }

    /// <summary>
    /// Gets the reserved bits from metadata.
    /// </summary>
    /// <param name="metadata">Encoded metadata</param>
    /// <returns>Reserved bits value</returns>
    public static uint GetReserved(uint metadata)
    {
        return metadata & ReservedMask;
    }

    /// <summary>
    /// Creates a NOP (no-operation) instruction metadata.
    /// </summary>
    /// <returns>NOP metadata (kind 0, size 1, no args)</returns>
    public static uint CreateNop()
    {
        return Encode(0, 1, 0);
    }

    /// <summary>
    /// Checks if the metadata represents a NOP instruction.
    /// </summary>
    /// <param name="metadata">Encoded metadata</param>
    /// <returns>True if this is a NOP instruction</returns>
    public static bool IsNop(uint metadata)
    {
        return DecodeKind(metadata) == 0;
    }
}
