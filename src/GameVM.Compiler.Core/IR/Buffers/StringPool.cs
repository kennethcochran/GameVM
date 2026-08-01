using System;
using System.Collections.Generic;
using System.Text;

namespace GameVM.Compiler.Core.IR.Buffers
{
    /// <summary>
    /// String pool (intern table) for DOD compiler. Stores unique strings contiguously
    /// as length-prefixed entries and returns a 32-bit integer handle (offset) for each.
    /// Identical strings are deduplicated (canonicalized).
    /// </summary>
    public sealed class StringPool
    {
        private readonly Dictionary<string, uint> _interned;
        private readonly List<byte> _buffer;
        private uint _offset;

        public StringPool(int initialCapacity = 4096)
        {
            _interned = new Dictionary<string, uint>(initialCapacity);
            _buffer = new List<byte>(initialCapacity);
            _offset = 0;
            
            // Reserve offset 0 for "none/empty" string
            AddString("");
        }

        /// <summary>Total bytes in the pool buffer.</summary>
        public uint Size => _offset;

        /// <summary>Number of unique strings interned.</summary>
        public int Count => _interned.Count;

        /// <summary>Interns a string and returns its pool offset (handle).</summary>
        public uint Intern(string str)
        {
            if (string.IsNullOrEmpty(str))
                return 0; // offset 0 = empty string

            if (_interned.TryGetValue(str, out uint existingOffset))
                return existingOffset;

            uint newOffset = AddString(str);
            _interned[str] = newOffset;
            return newOffset;
        }

        /// <summary>Recovers the original string from a pool offset.</summary>
        public string Resolve(uint offset)
        {
            if (offset == 0) return "";
            if (offset >= (uint)_buffer.Count) return $"<invalid_pool_offset:{offset}>";

            int len = BitConverter.ToInt32(_buffer.ToArray(), (int)offset);
            if (len < 0) return $"<invalid_string_length:{len}_at_{offset}>";
            if ((offset + 4 + len) > (uint)_buffer.Count) return $"<string_overflow:{len}_at_{offset}>";
            
            return Encoding.UTF8.GetString(_buffer.ToArray(), (int)offset + 4, len);
        }

        /// <summary>Writes the entire pool to a byte array for inclusion in a slab.</summary>
        public byte[] ToByteArray()
        {
            return _buffer.ToArray();
        }

        /// <summary>Creates a StringPool from a previously serialized byte array.</summary>
        public static StringPool FromByteArray(byte[] data)
        {
            var pool = new StringPool();
            pool._buffer.Clear();
            pool._buffer.AddRange(data);
            pool._offset = (uint)data.Length;
            
            // Rebuild the dictionary by scanning the buffer
            pool._interned.Clear();
            pool._interned[""] = 0;
            
            uint pos = 4; // skip the first empty string (4 bytes for length 0)
            while (pos < pool._offset)
            {
                if (pos + 4 > pool._offset) break;
                
                int len = BitConverter.ToInt32(data, (int)pos);
                if (len < 0) break;
                pos += 4;
                
                if (pos + len > pool._offset) break;
                
                string str = Encoding.UTF8.GetString(data, (int)pos, len);
                pos += (uint)len;
                
                // Skip null terminator
                if (pos < pool._offset && data[pos] == 0) pos++;
                
                if (!pool._interned.ContainsKey(str))
                    pool._interned[str] = pos - 4u - (uint)len; // offset = start of length prefix
            }
            
            return pool;
        }

        private uint AddString(string str)
        {
            uint currentOffset = (uint)_buffer.Count;
            byte[] utf8 = Encoding.UTF8.GetBytes(str);
            int len = utf8.Length;
            
            // Write length (4 bytes)
            _buffer.AddRange(BitConverter.GetBytes(len));
            // Write string bytes
            _buffer.AddRange(utf8);
            // Write null terminator for safety
            _buffer.Add((byte)0);
            
            return currentOffset;
        }
    }
}