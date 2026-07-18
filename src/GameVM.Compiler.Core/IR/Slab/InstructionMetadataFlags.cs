using System;

namespace GameVM.Compiler.Core.IR.Slab
{
    /// <summary>
    /// Flag constants for instruction metadata bitfields.
    /// These mirror the masks defined in <c>InstructionMetadata</c>.
    /// They are exposed separately for consumers that only need flag values.
    /// </summary>
    public struct InstructionMetadataFlags
    {
        public const uint TerminatorMask = InstructionMetadata.TerminatorMask;
        public const uint DiagnosticMask = InstructionMetadata.DiagnosticMask;
        public const uint ReservedMask   = InstructionMetadata.ReservedMask;
    }
}
