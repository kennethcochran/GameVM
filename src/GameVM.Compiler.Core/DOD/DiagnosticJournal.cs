namespace GameVM.Compiler.Core.DOD
{
    /// <summary>
    /// Lightweight diagnostic journal kept separate from the executable instruction slab.
    /// Maps a slab offset to a source span and a diagnostic code. The Diagnostic Present Flag
    /// (bit 21) on an instruction indicates an entry exists here for that offset. Strictly
    /// value-typed: backed only by primitive uint[] arrays (no List/Dictionary/reference types)
    /// so it is serialization-ready via block copy.
    /// </summary>
    public sealed class DiagnosticJournal
    {
        // Parallel primitive arrays indexed by entry order.
        private uint[] _slabOffsets;
        private uint[] _sourceStarts;
        private uint[] _sourceEnds;
        private uint[] _diagnosticCodes;
        private int _count;
        private int _capacity;

        /// <summary>Creates a journal with the given initial capacity for diagnostic entries.</summary>
        public DiagnosticJournal(int initialCapacity = 16)
        {
            if (initialCapacity < 1)
                initialCapacity = 1;
            _capacity = initialCapacity;
            _slabOffsets = new uint[initialCapacity];
            _sourceStarts = new uint[initialCapacity];
            _sourceEnds = new uint[initialCapacity];
            _diagnosticCodes = new uint[initialCapacity];
            _count = 0;
        }

        /// <summary>Number of recorded diagnostics.</summary>
        public int Count => _count;

        /// <summary>Current entry capacity.</summary>
        public int Capacity => _capacity;

        /// <summary>
        /// Records a diagnostic for the given slab offset. If an entry already exists for the
        /// offset it is replaced.
        /// </summary>
        public void Record(uint slabOffset, uint sourceStart, uint sourceEnd, uint diagnosticCode)
        {
            int existing = FindIndex(slabOffset);
            if (existing >= 0)
            {
                _sourceStarts[existing] = sourceStart;
                _sourceEnds[existing] = sourceEnd;
                _diagnosticCodes[existing] = diagnosticCode;
                return;
            }

            if (_count == _capacity)
                Grow();

            _slabOffsets[_count] = slabOffset;
            _sourceStarts[_count] = sourceStart;
            _sourceEnds[_count] = sourceEnd;
            _diagnosticCodes[_count] = diagnosticCode;
            _count++;
        }

        /// <summary>Returns true if a diagnostic exists for the given slab offset.</summary>
        public bool Has(uint slabOffset) => FindIndex(slabOffset) >= 0;

        /// <summary>
        /// Retrieves the diagnostic for a slab offset. Returns false if none is recorded.
        /// </summary>
        public bool TryGet(uint slabOffset, out DiagnosticEntry entry)
        {
            int index = FindIndex(slabOffset);
            if (index >= 0)
            {
                entry = new DiagnosticEntry(
                    _slabOffsets[index],
                    _sourceStarts[index],
                    _sourceEnds[index],
                    _diagnosticCodes[index]);
                return true;
            }

            entry = default;
            return false;
        }

        /// <summary>Removes the diagnostic for a slab offset if present.</summary>
        public bool Remove(uint slabOffset)
        {
            int index = FindIndex(slabOffset);
            if (index < 0)
                return false;

            int last = _count - 1;
            if (index != last)
            {
                _slabOffsets[index] = _slabOffsets[last];
                _sourceStarts[index] = _sourceStarts[last];
                _sourceEnds[index] = _sourceEnds[last];
                _diagnosticCodes[index] = _diagnosticCodes[last];
            }

            _count--;
            return true;
        }

        // Linear scan for the entry index (diagnostic counts are typically small/cold-path).
        private int FindIndex(uint slabOffset)
        {
            for (int i = 0; i < _count; i++)
            {
                if (_slabOffsets[i] == slabOffset)
                    return i;
            }
            return -1;
        }

        private void Grow()
        {
            int newCapacity = _capacity * 2;
            uint[] so = new uint[newCapacity];
            uint[] ss = new uint[newCapacity];
            uint[] se = new uint[newCapacity];
            uint[] dc = new uint[newCapacity];
            System.Array.Copy(_slabOffsets, so, _count);
            System.Array.Copy(_sourceStarts, ss, _count);
            System.Array.Copy(_sourceEnds, se, _count);
            System.Array.Copy(_diagnosticCodes, dc, _count);
            _slabOffsets = so;
            _sourceStarts = ss;
            _sourceEnds = se;
            _diagnosticCodes = dc;
            _capacity = newCapacity;
        }
    }

    /// <summary>Value-typed diagnostic record: slab offset + source span + diagnostic code.</summary>
    public readonly struct DiagnosticEntry
    {
        public uint SlabOffset { get; }
        public uint SourceStart { get; }
        public uint SourceEnd { get; }
        public uint DiagnosticCode { get; }

        public DiagnosticEntry(uint slabOffset, uint sourceStart, uint sourceEnd, uint diagnosticCode)
        {
            SlabOffset = slabOffset;
            SourceStart = sourceStart;
            SourceEnd = sourceEnd;
            DiagnosticCode = diagnosticCode;
        }
    }
}
