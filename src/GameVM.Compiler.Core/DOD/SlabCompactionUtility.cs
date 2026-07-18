using System;
using GameVM.Compiler.Core.Utilities;

namespace GameVM.Compiler.Core.DOD
{
    /// <summary>
    /// Slab compaction utility that rewrites instruction slabs into compacted form.
    /// Used by optimization passes to emit dead-code-eliminated or transformed code.
    /// </summary>
    public sealed class SlabCompactionUtility
    {
        private readonly uint[] _sourceSlab;
        private readonly uint[] _targetSlab;
        private int _sourceIndex;
        private int _targetIndex;

        /// <summary>
        /// Initializes a new instance of the <see cref="SlabCompactionUtility"/> class.
        /// </summary>
        /// <param name="sourceSlab">Source slab to compact (read from).</param>
        /// <param name="targetSlab">Target slab to write compacted data into.</param>
        public SlabCompactionUtility(uint[] sourceSlab, uint[] targetSlab)
        {
            _sourceSlab = sourceSlab ?? throw new ArgumentNullException(nameof(sourceSlab));
            _targetSlab = targetSlab ?? throw new ArgumentNullException(nameof(targetSlab));

            // Skip the 6-index header
            _sourceIndex = 6;
            _targetIndex = 6;

            // Copy header to target
            CopyHeader();
        }

        /// <summary>
        /// Copies the 6-index header to the target slab.
        /// </summary>
        private void CopyHeader()
        {
            for (int i = 0; i < 6; i++)
            {
                _targetSlab[i] = _sourceSlab[i];
            }
        }

        /// <summary>
        /// Processes the next instruction block. Returns false when no more blocks to process.
        /// </summary>
        /// <param name="keepCallback">Callback to determine whether to keep each instruction.</param>
        /// <returns>True if an instruction was processed, false if end of slab reached.</returns>
        public bool ProcessNext(Func<uint, bool> keepCallback)
        {
            if (_sourceIndex >= _sourceSlab.Length)
                return false;

            var metadata = _sourceSlab[_sourceIndex];
            var size = (int)MetadataDecoder.DecodeSize(metadata);
            var isNop = MetadataDecoder.IsNop(metadata);

            // Determine if we should keep this instruction
            bool keep = keepCallback?.Invoke(metadata) ?? !isNop;

            if (keep && size > 0)
            {
                // Copy instruction block to target
                for (int i = 0; i < size; i++)
                {
                    _targetSlab[_targetIndex + i] = _sourceSlab[_sourceIndex + i];
                }
                _targetIndex += size;
            }

            _sourceIndex += size;
            return true;
        }

        /// <summary>
        /// Gets the number of elements written to the target slab.
        /// </summary>
        public int CompactedElementCount => _targetIndex;

        /// <summary>
        /// Updates the target slab header with final element count.
        /// Call this after all ProcessNext calls are complete.
        /// </summary>
        public void FinalizeHeader()
        {
            // Update element count in header (index 4)
            _targetSlab[4] = (uint)(_targetIndex - 6); // Exclude header indices
        }
    }
}