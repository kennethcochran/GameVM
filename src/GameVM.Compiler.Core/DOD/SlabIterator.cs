using System;
using GameVM.Compiler.Core.Utilities;

namespace GameVM.Compiler.Core.DOD
{
    /// <summary>
    /// Slab iterator for reading self-describing instruction blocks sequentially.
    /// Maintains current position in the uint[] slab and provides access to each instruction block.
    /// </summary>
    public class SlabIterator
    {
        private readonly uint[] _slab;
        private int _current = 6;  // Start after standardized 6-index header

        /// <summary>
        /// Initializes the iterator with a 32-bit instruction slab.
        /// </summary>
        public SlabIterator(uint[] slab)
        {
            _slab = slab ?? throw new ArgumentNullException("slab");
            if (_slab.Length < 6)
                throw new ArgumentException("Slab must contain at least 6 indices for the header");
        }

        /// <summary>
        /// Moves to the next instruction block in the slab.
        /// Returns false when no more blocks remain.
        /// </summary>
        public bool MoveNext()
        {
            // Validate current position doesn't exceed slab length
            if (_current >= _slab.Length)
                return false;

            // Read metadata from current position
            var metadata = _slab[_current];

            // Validate instruction block size (block may end exactly at slab boundary)
            var size = MetadataDecoder.DecodeSize(metadata);
            if (_current + size > _slab.Length)
                throw new InvalidOperationException($"Block at {_current} of size {size} exceeds slab length");

            // Create instruction instance based on decoded metadata
            var instruction = CreateInstructionFromMetadata(metadata);

            // Move current position past this block
            _current += size;

            // Return current instruction
            CurrentInstruction = instruction;
            return true;
        }

        /// <summary>
        /// Current instruction being processed by the iterator.
        /// </summary>
        public Instruction CurrentInstruction { get; private set; }

        /// <summary>
        /// Creates an instruction instance from decoded metadata.
        /// This can be extended to handle specific instruction kinds.
        /// </summary>
        private Instruction CreateInstructionFromMetadata(uint metadata)
        {
            var kind = MetadataDecoder.DecodeKind(metadata);
            var flags = new InstructionFlags
            {
                IsTerminator = MetadataDecoder.DecodeIsTerminator(metadata),
                HasDiagnostic = MetadataDecoder.DecodeHasDiagnostic(metadata)
            };

            // Return value-type Instruction struct
            return new Instruction
            {
                Kind = kind,
                Flags = flags
            };
        }
    }

    /// <summary>
    /// Simple structure to hold decoded instruction metadata.
    /// </summary>
    public struct Instruction
    {
        public byte Kind { get; set; }
        public InstructionFlags Flags { get; set; }
    }

    /// <summary>
    /// Bit flags for instruction metadata.
    /// </summary>
    public struct InstructionFlags
    {
        public bool IsTerminator { get; set; }
        public bool HasDiagnostic { get; set; }
    }
}
