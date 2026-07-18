using System.Collections.Generic;

namespace GameVM.Compiler.Core.DOD
{
    /// <summary>
    /// Lightweight diagnostic journal kept separate from the executable instruction slab.
    /// Maps a slab offset to a source span and a diagnostic code. The Diagnostic Present Flag
    /// (bit 21) on an instruction indicates an entry exists here for that offset. Strictly
    /// value-typed: backed by parallel primitive arrays for serialization readiness.
    /// </summary>
    public sealed class DiagnosticJournal
    {
        // Parallel arrays indexed by entry order.
        private readonly List<uint> _slabOffsets = new List<uint>();
        private readonly List<uint> _sourceStarts = new List<uint>();
        private readonly List<uint> _sourceEnds = new List<uint>();
        private readonly List<uint> _diagnosticCodes = new List<uint>();

        // Offset -> entry index for O(1) lookup.
        private readonly Dictionary<uint, int> _offsetToEntry = new Dictionary<uint, int>();

        /// <summary>Number of recorded diagnostics.</summary>
        public int Count => _slabOffsets.Count;

        /// <summary>
        /// Records a diagnostic for the given slab offset. If an entry already exists for the
        /// offset it is replaced.
        /// </summary>
        public void Record(uint slabOffset, uint sourceStart, uint sourceEnd, uint diagnosticCode)
        {
            if (_offsetToEntry.TryGetValue(slabOffset, out int existing))
            {
                _sourceStarts[existing] = sourceStart;
                _sourceEnds[existing] = sourceEnd;
                _diagnosticCodes[existing] = diagnosticCode;
                return;
            }

            int index = _slabOffsets.Count;
            _slabOffsets.Add(slabOffset);
            _sourceStarts.Add(sourceStart);
            _sourceEnds.Add(sourceEnd);
            _diagnosticCodes.Add(diagnosticCode);
            _offsetToEntry.Add(slabOffset, index);
        }

        /// <summary>Returns true if a diagnostic exists for the given slab offset.</summary>
        public bool Has(uint slabOffset) => _offsetToEntry.ContainsKey(slabOffset);

        /// <summary>
        /// Retrieves the diagnostic for a slab offset. Returns false if none is recorded.
        /// </summary>
        public bool TryGet(uint slabOffset, out DiagnosticEntry entry)
        {
            if (_offsetToEntry.TryGetValue(slabOffset, out int index))
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
            if (!_offsetToEntry.TryGetValue(slabOffset, out int index))
                return false;

            // Swap-remove with index bookkeeping update.
            int last = _slabOffsets.Count - 1;
            if (index != last)
            {
                _slabOffsets[index] = _slabOffsets[last];
                _sourceStarts[index] = _sourceStarts[last];
                _sourceEnds[index] = _sourceEnds[last];
                _diagnosticCodes[index] = _diagnosticCodes[last];
                _offsetToEntry[_slabOffsets[index]] = index;
            }

            _slabOffsets.RemoveAt(last);
            _sourceStarts.RemoveAt(last);
            _sourceEnds.RemoveAt(last);
            _diagnosticCodes.RemoveAt(last);
            _offsetToEntry.Remove(slabOffset);
            return true;
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
