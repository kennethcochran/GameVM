using System;
using System.Collections.Generic;

namespace GameVM.Compiler.Core.IR.SlabProcessing;

/// <summary>
/// Arena allocator for contiguous memory slab allocation.
/// Provides bump-pointer allocation for zero-fragmentation memory management.
/// </summary>
public sealed class ArenaAllocator
{
    private readonly List<uint[]> _chunks;
    private uint[] _currentChunk;
    private int _currentOffset;
    private readonly int _chunkSize;
    private int _totalAllocated;
    private int _currentChunkBaseOffset;

    /// <summary>
    /// Initializes a new instance of the <see cref="ArenaAllocator"/> class.
    /// </summary>
    /// <param name="initialChunkSize">The initial size of each chunk in uints.</param>
    public ArenaAllocator(int initialChunkSize = 4096)
    {
        if (initialChunkSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(initialChunkSize), "Chunk size must be positive.");

        _chunkSize = initialChunkSize;
        _chunks = new List<uint[]>(4);
        _currentChunk = new uint[initialChunkSize];
        _currentOffset = 0;
        _totalAllocated = 0;
        _currentChunkBaseOffset = 0;
        _chunks.Add(_currentChunk);
    }

    /// <summary>
    /// Allocates space for the specified number of uints and returns the offset.
    /// </summary>
    /// <param name="count">The number of uints to allocate.</param>
    /// <returns>The offset within the arena where the allocation begins.</returns>
    public uint Allocate(int count)
    {
        if (count <= 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Allocation count must be positive.");

        // Check if we need a new chunk
        if (_currentOffset + count > _currentChunk.Length)
        {
            AllocateNewChunk(count);
        }

        uint offset = (uint)(_currentChunkBaseOffset + _currentOffset);
        _currentOffset += count;
        _totalAllocated += count;

        return offset;
    }
    /// <summary>
    /// Allocates an array of the specified type and count in the arena.
    /// </summary>
    /// <typeparam name="T">The element type (must be unmanaged).</typeparam>
    /// <param name="count">The number of elements to allocate.</param>
    /// <returns>A managed array to be used as backing storage for InstList parallel arrays.</returns>
    public T[] AllocateArray<T>(int count) where T : unmanaged
    {
        if (count <= 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Allocation count must be positive.");

        // Calculate size in uints (each uint = 4 bytes)
        int elementSize = System.Runtime.InteropServices.Marshal.SizeOf<T>();
        int totalBytes = count * elementSize;
        int uintCount = (totalBytes + 3) / 4; // Round up to uint boundary

        // Allocate space in the arena (advances bump pointer for accounting)
        Allocate(uintCount);

        // Return a managed array as the backing storage
        // The caller constructs InstList over these arrays
        return new T[count];
    }


    /// <summary>
    /// Writes uint values to the arena at the specified offset.
    /// </summary>
    /// <param name="offset">The offset to write to.</param>
    /// <param name="values">The values to write.</param>
    public void Write(uint offset, params uint[] values)
    {
        if (values == null)
            throw new ArgumentNullException(nameof(values));
        if (values.Length == 0)
            return;

        int chunkIndex = FindChunkIndex(offset);
        int localOffset = (int)(offset - GetChunkBaseOffset(chunkIndex));

        uint[] chunk = _chunks[chunkIndex];

        for (int i = 0; i < values.Length; i++)
        {
            if (localOffset >= chunk.Length)
            {
                chunkIndex++;
                localOffset = 0;
                if (chunkIndex >= _chunks.Count)
                    throw new ArgumentOutOfRangeException(nameof(offset), "Write exceeds allocated space.");
                chunk = _chunks[chunkIndex];
            }

            chunk[localOffset++] = values[i];
        }
    }

    /// <summary>
    /// Reads uint values from the arena at the specified offset.
    /// </summary>
    /// <param name="offset">The offset to read from.</param>
    /// <param name="count">The number of uints to read.</param>
    /// <returns>The uint values read from the arena.</returns>
    public uint[] Read(uint offset, int count)
    {
        if (count <= 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Read count must be positive.");

        uint[] result = new uint[count];
        int chunkIndex = FindChunkIndex(offset);
        int localOffset = (int)(offset - GetChunkBaseOffset(chunkIndex));
        int readIndex = 0;

        while (readIndex < count)
        {
            uint[] chunk = _chunks[chunkIndex];
            int remainingInChunk = chunk.Length - localOffset;
            int toRead = Math.Min(remainingInChunk, count - readIndex);

            Array.Copy(chunk, localOffset, result, readIndex, toRead);

            readIndex += toRead;
            localOffset += toRead;

            if (localOffset >= chunk.Length && readIndex < count)
            {
                chunkIndex++;
                localOffset = 0;
                if (chunkIndex >= _chunks.Count)
                    throw new ArgumentOutOfRangeException(nameof(offset), "Read exceeds allocated space.");
            }
        }

        return result;
    }

    /// <summary>
    /// Gets the total number of uints allocated across all chunks.
    /// </summary>
    public int TotalAllocated => _totalAllocated;

    /// <summary>
    /// Gets the number of chunks currently allocated.
    /// </summary>
    public int ChunkCount => _chunks.Count;

    /// <summary>
    /// Resets the arena, deallocating all memory.
    /// </summary>
    public void Reset()
    {
        _currentChunk = _chunks[0];
        _currentOffset = 0;
        _totalAllocated = 0;
        _currentChunkBaseOffset = 0;
        Array.Clear(_currentChunk, 0, _currentChunk.Length);

        // Clear additional chunks if any
        for (int i = 1; i < _chunks.Count; i++)
        {
            Array.Clear(_chunks[i], 0, _chunks[i].Length);
        }
    }

    /// <summary>
    /// Converts the arena to a single contiguous uint array.
    /// </summary>
    /// <returns>A contiguous array containing all allocated data.</returns>
    public uint[] ToContiguousArray()
    {
        uint[] result = new uint[_totalAllocated];
        int resultOffset = 0;

        foreach (var chunk in _chunks)
        {
            int copyLength = Math.Min(chunk.Length, _totalAllocated - resultOffset);
            if (copyLength <= 0)
                break;

            Array.Copy(chunk, 0, result, resultOffset, copyLength);
            resultOffset += copyLength;
        }

        return result;
    }

    private void AllocateNewChunk(int requiredSize)
    {
        int newChunkSize = Math.Max(_chunkSize, requiredSize);
        _currentChunkBaseOffset += _currentChunk.Length;
        _currentChunk = new uint[newChunkSize];
        _currentOffset = 0;
        _chunks.Add(_currentChunk);
    }

    private int FindChunkIndex(uint offset)
    {
        // Binary search for the chunk containing the offset
        int left = 0;
        int right = _chunks.Count - 1;

        while (left < right)
        {
            int mid = (left + right + 1) / 2;
            if (GetChunkBaseOffset(mid) <= offset)
                left = mid;
            else
                right = mid - 1;
        }

        return left;
    }

    private uint GetChunkBaseOffset(int chunkIndex)
    {
        uint offset = 0;
        for (int i = 0; i < chunkIndex; i++)
        {
            offset += (uint)_chunks[i].Length;
        }
        return offset;
    }
}
