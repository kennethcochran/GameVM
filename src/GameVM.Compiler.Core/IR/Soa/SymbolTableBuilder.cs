using System;
using GameVM.Compiler.Core.Enums;

namespace GameVM.Compiler.Core.IR.Soa;

/// <summary>
/// Incrementally constructs a <see cref="SymbolTable"/> from symbol entries.
/// Auto-resizes internal arrays as needed.
/// </summary>
public sealed class SymbolTableBuilder
{
    private const int InitialCapacity = 16;

    private uint[] _nameOffsets;
    private byte[] _kinds;
    private uint[] _typeIds;
    private uint[] _scopeIds;
    private uint[] _fileIds;
    private uint[] _lines;
    private uint[] _columns;
    private byte[] _storageClasses;
    private int _count;

    public SymbolTableBuilder() : this(InitialCapacity) { }

    public SymbolTableBuilder(int capacity)
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be positive.");

        _nameOffsets = new uint[capacity];
        _kinds = new byte[capacity];
        _typeIds = new uint[capacity];
        _scopeIds = new uint[capacity];
        _fileIds = new uint[capacity];
        _lines = new uint[capacity];
        _columns = new uint[capacity];
        _storageClasses = new byte[capacity];
        _count = 0;
    }

    /// <summary>Current number of symbols in the builder.</summary>
    public int Count => _count;

    /// <summary>Adds a symbol and returns its <see cref="SymbolId"/>.</summary>
    public SymbolId Add(
        uint nameOffset,
        SymbolKind kind,
        uint typeId,
        uint scopeId,
        uint fileId,
        uint line,
        uint column,
        StorageClass storageClass)
    {
        EnsureCapacity(_count + 1);

        int index = _count;
        _nameOffsets[index] = nameOffset;
        _kinds[index] = (byte)kind;
        _typeIds[index] = typeId;
        _scopeIds[index] = scopeId;
        _fileIds[index] = fileId;
        _lines[index] = line;
        _columns[index] = column;
        _storageClasses[index] = (byte)storageClass;
        _count++;

        return SymbolId.FromInt(index);
    }

    /// <summary>Builds an immutable <see cref="SymbolTable"/> from the accumulated entries.</summary>
    public SymbolTable Build()
    {
        var trimmedNameOffsets = new uint[_count];
        var trimmedKinds = new byte[_count];
        var trimmedTypeIds = new uint[_count];
        var trimmedScopeIds = new uint[_count];
        var trimmedFileIds = new uint[_count];
        var trimmedLines = new uint[_count];
        var trimmedColumns = new uint[_count];
        var trimmedStorageClasses = new byte[_count];

        Array.Copy(_nameOffsets, 0, trimmedNameOffsets, 0, _count);
        Array.Copy(_kinds, 0, trimmedKinds, 0, _count);
        Array.Copy(_typeIds, 0, trimmedTypeIds, 0, _count);
        Array.Copy(_scopeIds, 0, trimmedScopeIds, 0, _count);
        Array.Copy(_fileIds, 0, trimmedFileIds, 0, _count);
        Array.Copy(_lines, 0, trimmedLines, 0, _count);
        Array.Copy(_columns, 0, trimmedColumns, 0, _count);
        Array.Copy(_storageClasses, 0, trimmedStorageClasses, 0, _count);

        return new SymbolTable(
            trimmedNameOffsets, trimmedKinds, trimmedTypeIds,
            trimmedScopeIds, trimmedFileIds, trimmedLines,
            trimmedColumns, trimmedStorageClasses, _count);
    }

    private void EnsureCapacity(int minCount)
    {
        if (_nameOffsets.Length >= minCount)
            return;

        int newCapacity = Math.Max(_nameOffsets.Length * 2, minCount);
        ResizeArrays(newCapacity);
    }

    private void ResizeArrays(int newCapacity)
    {
        var newNameOffsets = new uint[newCapacity];
        var newKinds = new byte[newCapacity];
        var newTypeIds = new uint[newCapacity];
        var newScopeIds = new uint[newCapacity];
        var newFileIds = new uint[newCapacity];
        var newLines = new uint[newCapacity];
        var newColumns = new uint[newCapacity];
        var newStorageClasses = new byte[newCapacity];

        Array.Copy(_nameOffsets, 0, newNameOffsets, 0, _count);
        Array.Copy(_kinds, 0, newKinds, 0, _count);
        Array.Copy(_typeIds, 0, newTypeIds, 0, _count);
        Array.Copy(_scopeIds, 0, newScopeIds, 0, _count);
        Array.Copy(_fileIds, 0, newFileIds, 0, _count);
        Array.Copy(_lines, 0, newLines, 0, _count);
        Array.Copy(_columns, 0, newColumns, 0, _count);
        Array.Copy(_storageClasses, 0, newStorageClasses, 0, _count);

        _nameOffsets = newNameOffsets;
        _kinds = newKinds;
        _typeIds = newTypeIds;
        _scopeIds = newScopeIds;
        _fileIds = newFileIds;
        _lines = newLines;
        _columns = newColumns;
        _storageClasses = newStorageClasses;
    }
}
