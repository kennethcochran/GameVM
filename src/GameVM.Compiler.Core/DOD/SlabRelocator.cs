using System;

namespace GameVM.Compiler.Core.DOD
{
    /// <summary>
    /// Centralized offset-patching utility. Structural optimization passes and branch
    /// resolution rewrite instruction positions; rather than patching slab offsets ad hoc,
    /// all relocations funnel through this class. It computes a remap of old slab offsets to
    /// new offsets and rewrites every reference that points into the moved region. Reusing a
    /// single relocator also enables sliding imported libraries during future linking phases.
    /// </summary>
    public sealed class SlabRelocator
    {
        // Remap stored as parallel int[] arrays (old -> new) for value-type isolation.
        // No Dictionary/List: only primitive arrays are retained.
        private int[] _oldOffsets;
        private int[] _newOffsets;
        private int _count;
        private int _capacity;

        /// <summary>Creates a relocator with the given initial relocation capacity.</summary>
        public SlabRelocator(int initialCapacity = 16)
        {
            if (initialCapacity < 1)
                initialCapacity = 1;
            _capacity = initialCapacity;
            _oldOffsets = new int[initialCapacity];
            _newOffsets = new int[initialCapacity];
            _count = 0;
        }

        /// <summary>Registers that content previously at <paramref name="oldOffset"/> now lives at <paramref name="newOffset"/>.</summary>
        public void AddReloc(int oldOffset, int newOffset)
        {
            if (oldOffset < 0)
                throw new ArgumentOutOfRangeException(nameof(oldOffset));
            if (newOffset < 0)
                throw new ArgumentOutOfRangeException(nameof(newOffset));

            int existing = FindIndex(oldOffset);
            if (existing >= 0)
            {
                _newOffsets[existing] = newOffset;
                return;
            }

            if (_count == _capacity)
                Grow();

            _oldOffsets[_count] = oldOffset;
            _newOffsets[_count] = newOffset;
            _count++;
        }

        /// <summary>Returns true if a relocation exists for the given old offset.</summary>
        public bool HasReloc(int oldOffset) => FindIndex(oldOffset) >= 0;

        /// <summary>
        /// Resolves an old offset to its new location. Returns the original offset unchanged
        /// if no relocation is registered (stable references outside the moved region).
        /// </summary>
        public int Relocate(int oldOffset)
        {
            int index = FindIndex(oldOffset);
            return index >= 0 ? _newOffsets[index] : oldOffset;
        }

        /// <summary>
        /// Rewrites a slab in place: for every instruction block whose operand words fall in the
        /// relocation range, applies <see cref="Relocate"/> to those operands. Operand words are
        /// identified by the caller via <paramref name="operandIndices"/> (relative to each block start).
        /// Blocks are walked linearly using decoded sizes; the Diagnostic Present flag is preserved.
        /// </summary>
        /// <param name="slab">Slab to patch in place.</param>
        /// <param name="operandIndices">Per-block operand word offsets (relative to block start) to rewrite.</param>
        public void PatchSlab(uint[] slab, int[] operandIndices)
        {
            if (slab == null)
                throw new ArgumentNullException(nameof(slab));
            if (operandIndices == null)
                throw new ArgumentNullException(nameof(operandIndices));

            int offset = SlabHeader.HeaderIndex.Length; // skip header
            while (offset < slab.Length)
            {
                uint metadata = slab[offset];
                int size = (int)GameVM.Compiler.Core.Utilities.MetadataDecoder.DecodeSize(metadata);
                if (size <= 0)
                    break;

                foreach (int rel in operandIndices)
                {
                    int abs = offset + rel;
                    if (abs >= 0 && abs < offset + size && abs < slab.Length)
                    {
                        int oldRef = (int)slab[abs];
                        slab[abs] = (uint)Relocate(oldRef);
                    }
                }

                offset += size;
            }
        }

        /// <summary>Number of registered relocations.</summary>
        public int RelocCount => _count;

        // Linear scan for the entry index (relocation sets are typically small).
        private int FindIndex(int oldOffset)
        {
            for (int i = 0; i < _count; i++)
            {
                if (_oldOffsets[i] == oldOffset)
                    return i;
            }
            return -1;
        }

        private void Grow()
        {
            int newCapacity = _capacity * 2;
            int[] oo = new int[newCapacity];
            int[] no = new int[newCapacity];
            Array.Copy(_oldOffsets, oo, _count);
            Array.Copy(_newOffsets, no, _count);
            _oldOffsets = oo;
            _newOffsets = no;
            _capacity = newCapacity;
        }
    }
}
