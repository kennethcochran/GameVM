using System;

namespace GameVM.Compiler.Core.DOD
{
    /// <summary>
    /// Hashed symbol table using the FNV-1a algorithm. String identifiers are reduced to
    /// 32-bit integer hashes and stored in parallel primitive arrays (hashes + slab offsets),
    /// never as string names. Maps a symbol hash to the slab offset where its definition begins,
    /// enabling linking phases to match integer hashes instead of parsing string data.
    /// </summary>
    public sealed class HashedSymbolTable
    {
        private const float LoadFactor = 0.75f;

        private uint[] _hashes;
        private uint[] _offsets;
        private int _count;

        /// <summary>Creates a symbol table with the given initial capacity (rounded up to a power of two).</summary>
        public HashedSymbolTable(int initialCapacity = 16)
        {
            int size = NextPowerOfTwo(initialCapacity < 1 ? 1 : initialCapacity);
            _hashes = new uint[size];
            _offsets = new uint[size];
            _count = 0;
        }

        /// <summary>Number of symbols currently stored.</summary>
        public int Count => _count;

        /// <summary>Current bucket capacity.</summary>
        public int Capacity => _hashes.Length;

        /// <summary>Computes the 32-bit FNV-1a hash of a string identifier.</summary>
        public static uint Hash(ReadOnlySpan<char> name)
        {
            const uint offset = 2166136261u;
            const uint prime = 16777619u;
            uint hash = offset;
            for (int i = 0; i < name.Length; i++)
            {
                hash ^= name[i];
                hash *= prime;
            }
            return hash;
        }

        /// <summary>Computes the FNV-1a hash of a UTF-8 byte sequence.</summary>
        public static uint Hash(ReadOnlySpan<byte> bytes)
        {
            const uint offset = 2166136261u;
            const uint prime = 16777619u;
            uint hash = offset;
            for (int i = 0; i < bytes.Length; i++)
            {
                hash ^= bytes[i];
                hash *= prime;
            }
            return hash;
        }

        /// <summary>Adds or replaces a symbol, mapping its hash to the given slab offset.</summary>
        public void Add(string name, uint slabOffset)
        {
            Add(Hash(name.AsSpan()), slabOffset);
        }

        /// <summary>Adds or replaces a symbol by precomputed hash.</summary>
        public void Add(uint hash, uint slabOffset)
        {
            if (_count + 1 > _hashes.Length * LoadFactor)
                Grow();

            int index = FindSlot(hash);
            if (_hashes[index] == 0) // empty slot -> new entry
                _count++;
            _hashes[index] = hash == 0 ? 1u : hash; // reserve 0 as "empty" sentinel
            _offsets[index] = slabOffset;
        }

        /// <summary>Returns true if the symbol hash is present.</summary>
        public bool Contains(uint hash)
        {
            return FindSlot(hash) is int i && _hashes[i] != 0;
        }

        /// <summary>Returns the slab offset for a symbol hash, or 0 if not found.</summary>
        public uint GetOffset(uint hash)
        {
            int index = FindSlot(hash);
            return _hashes[index] == 0 ? 0u : _offsets[index];
        }

        /// <summary>Attempts to resolve a symbol name to its slab offset.</summary>
        public bool TryGetOffset(string name, out uint slabOffset)
        {
            return TryGetOffset(Hash(name.AsSpan()), out slabOffset);
        }

        /// <summary>Attempts to resolve a symbol hash to its slab offset.</summary>
        public bool TryGetOffset(uint hash, out uint slabOffset)
        {
            int index = FindSlot(hash);
            if (_hashes[index] != 0)
            {
                slabOffset = _offsets[index];
                return true;
            }

            slabOffset = 0u;
            return false;
        }

        // Linear-probe open addressing; finds the slot for a hash or the first empty slot.
        private int FindSlot(uint hash)
        {
            uint probe = hash == 0 ? 1u : hash;
            int mask = _hashes.Length - 1;
            int index = (int)(probe & mask);
            while (_hashes[index] != 0 && _hashes[index] != probe)
            {
                index = (index + 1) & mask;
            }
            return index;
        }

        private void Grow()
        {
            uint[] oldHashes = _hashes;
            uint[] oldOffsets = _offsets;
            int newSize = oldHashes.Length * 2;
            _hashes = new uint[newSize];
            _offsets = new uint[newSize];
            _count = 0;

            for (int i = 0; i < oldHashes.Length; i++)
            {
                if (oldHashes[i] != 0)
                    Add(oldHashes[i], oldOffsets[i]);
            }
        }

        private static int NextPowerOfTwo(int value)
        {
            int power = 1;
            while (power < value)
                power <<= 1;
            return power;
        }
    }
}
