using System;

namespace GameVM.Compiler.Core.DOD
{
    /// <summary>
    /// Type-Length-Value (TLV) chunk for auxiliary slab data sections. Each chunk is
    /// [type:uint][length:uint][value:length*uint], allowing readers to skip unknown chunk
    /// types without understanding their contents. Stored in a plain uint[] so it composes
    /// with the contiguous-slab model.
    /// </summary>
    public readonly struct TlvEntry
    {
        public const int HeaderWords = 2; // type + length

        private readonly uint _type;
        private readonly uint[] _value;

        public TlvEntry(uint type, uint[] value)
        {
            _type = type;
            _value = value ?? Array.Empty<uint>();
        }

        /// <summary>Chunk type identifier.</summary>
        public uint Type => _type;

        /// <summary>Number of uints in the value payload.</summary>
        public uint Length => (uint)_value.Length;

        /// <summary>The value payload words.</summary>
        public ReadOnlySpan<uint> Value => _value;

        /// <summary>Total words occupied by this chunk including the 2-word header.</summary>
        public int TotalWords => HeaderWords + _value.Length;

        /// <summary>Writes this chunk into a slab starting at the given index.</summary>
        public void WriteTo(uint[] slab, int startIndex)
        {
            if (startIndex < 0 || startIndex + TotalWords > slab.Length)
                throw new ArgumentException("Chunk does not fit in slab at the given index", nameof(startIndex));
            slab[startIndex] = _type;
            slab[startIndex + 1] = (uint)_value.Length;
            Array.Copy(_value, 0, slab, startIndex + HeaderWords, _value.Length);
        }

        /// <summary>
        /// Reads a chunk from a slab at the given index. Returns the chunk and sets
        /// <paramref name="nextIndex"/> to the start of the following chunk (for skipping).
        /// </summary>
        public static TlvEntry Read(uint[] slab, int startIndex, out int nextIndex)
        {
            if (startIndex < 0 || startIndex + HeaderWords > slab.Length)
                throw new ArgumentException("Chunk header does not fit in slab", nameof(startIndex));

            uint type = slab[startIndex];
            uint length = slab[startIndex + 1];
            int valueStart = startIndex + HeaderWords;

            if (valueStart + (int)length > slab.Length)
                throw new ArgumentException("Chunk value exceeds slab length", nameof(startIndex));

            uint[] value = new uint[length];
            Array.Copy(slab, valueStart, value, 0, (int)length);

            nextIndex = valueStart + (int)length;
            return new TlvEntry(type, value);
        }
    }
}
