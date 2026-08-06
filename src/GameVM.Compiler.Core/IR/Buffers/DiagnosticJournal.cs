using GameVM.Compiler.Core.IR.Soa;

namespace GameVM.Compiler.Core.IR.Buffers
{
    /// <summary>
    /// Lightweight diagnostic journal kept separate from the executable instruction slab.
    /// Maps an <see cref="InstIndex"/> to a source span and a diagnostic code. The Diagnostic
    /// flag on an instruction indicates an entry exists here for that instruction index.
    /// Strictly value-typed: backed only by primitive int[]/uint[] arrays (no
    /// List/Dictionary/reference types) so it is serialization-ready via block copy.
    /// </summary>
    public sealed class DiagnosticJournal
    {
        // Parallel primitive arrays indexed by entry order.
        private int[] _indices;
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
            _indices = new int[initialCapacity];
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
        /// Records a diagnostic for the given instruction index. If an entry already exists for
        /// the index it is replaced.
        /// </summary>
        public void Record(InstIndex index, uint sourceStart, uint sourceEnd, uint diagnosticCode)
        {
            int existing = FindIndex(index);
            if (existing >= 0)
            {
                _sourceStarts[existing] = sourceStart;
                _sourceEnds[existing] = sourceEnd;
                _diagnosticCodes[existing] = diagnosticCode;
                return;
            }

            if (_count == _capacity)
                Grow();

            _indices[_count] = index.Value;
            _sourceStarts[_count] = sourceStart;
            _sourceEnds[_count] = sourceEnd;
            _diagnosticCodes[_count] = diagnosticCode;
            _count++;
        }

        /// <summary>Returns true if a diagnostic exists for the given instruction index.</summary>
        public bool Has(InstIndex index) => FindIndex(index) >= 0;

        /// <summary>
        /// Retrieves the diagnostic for an instruction index. Returns false if none is recorded.
        /// </summary>
        public bool TryGet(InstIndex index, out DiagnosticEntry entry)
        {
            int pos = FindIndex(index);
            if (pos >= 0)
            {
                entry = new DiagnosticEntry(
                    InstIndex.FromInt(_indices[pos]),
                    _sourceStarts[pos],
                    _sourceEnds[pos],
                    _diagnosticCodes[pos]);
                return true;
            }

            entry = default;
            return false;
        }

        /// <summary>Removes the diagnostic for an instruction index if present.</summary>
        public bool Remove(InstIndex index)
        {
            int pos = FindIndex(index);
            if (pos < 0)
                return false;

            int last = _count - 1;
            if (pos != last)
            {
                _indices[pos] = _indices[last];
                _sourceStarts[pos] = _sourceStarts[last];
                _sourceEnds[pos] = _sourceEnds[last];
                _diagnosticCodes[pos] = _diagnosticCodes[last];
            }

            _count--;
            return true;
        }

        // Linear scan for the entry index (diagnostic counts are typically small/cold-path).
        private int FindIndex(InstIndex index)
        {
            for (int i = 0; i < _count; i++)
            {
                if (_indices[i] == index.Value)
                    return i;
            }
            return -1;
        }

        private void Grow()
        {
            int newCapacity = _capacity * 2;
            int[] idx = new int[newCapacity];
            uint[] ss = new uint[newCapacity];
            uint[] se = new uint[newCapacity];
            uint[] dc = new uint[newCapacity];
            System.Array.Copy(_indices, idx, _count);
            System.Array.Copy(_sourceStarts, ss, _count);
            System.Array.Copy(_sourceEnds, se, _count);
            System.Array.Copy(_diagnosticCodes, dc, _count);
            _indices = idx;
            _sourceStarts = ss;
            _sourceEnds = se;
            _diagnosticCodes = dc;
            _capacity = newCapacity;
        }
    }

    /// <summary>Value-typed diagnostic record: instruction index + source span + diagnostic code.</summary>
    public readonly struct DiagnosticEntry
    {
        public InstIndex InstIndex { get; }
        public uint SourceStart { get; }
        public uint SourceEnd { get; }
        public uint DiagnosticCode { get; }

        public DiagnosticEntry(InstIndex index, uint sourceStart, uint sourceEnd, uint diagnosticCode)
        {
            InstIndex = index;
            SourceStart = sourceStart;
            SourceEnd = sourceEnd;
            DiagnosticCode = diagnosticCode;
        }
    }
}