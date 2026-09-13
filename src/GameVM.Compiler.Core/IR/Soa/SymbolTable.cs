using System;
using GameVM.Compiler.Core.Enums;

namespace GameVM.Compiler.Core.IR.Soa;

/// <summary>
/// Struct of Arrays (SoA) representation of a symbol table.
/// Parallel arrays indexed by <see cref="SymbolId"/> store per-symbol properties.
/// Language-agnostic: all frontends write, all backends read.
/// </summary>
public readonly struct SymbolTable
{
    private readonly uint[] _nameOffsets;
    private readonly byte[] _kinds;
    private readonly uint[] _typeIds;
    private readonly uint[] _scopeIds;
    private readonly uint[] _fileIds;
    private readonly uint[] _lines;
    private readonly uint[] _columns;
    private readonly byte[] _storageClasses;
    private readonly int _count;

    /// <summary>Gets the number of symbols in this table.</summary>
    public int Count => _count;

    /// <summary>Creates a <see cref="SymbolTable"/> from raw arrays. Used by <see cref="SymbolTableBuilder"/>.</summary>
    public SymbolTable(
        uint[] nameOffsets,
        byte[] kinds,
        uint[] typeIds,
        uint[] scopeIds,
        uint[] fileIds,
        uint[] lines,
        uint[] columns,
        byte[] storageClasses,
        int count)
    {
        _nameOffsets = nameOffsets;
        _kinds = kinds;
        _typeIds = typeIds;
        _scopeIds = scopeIds;
        _fileIds = fileIds;
        _lines = lines;
        _columns = columns;
        _storageClasses = storageClasses;
        _count = count;
    }

    /// <summary>Gets the StringPool offset of the symbol name at <paramref name="index"/>.</summary>
    public uint GetNameOffset(int index) { CheckIndex(index); return _nameOffsets[index]; }

    /// <summary>Gets the <see cref="SymbolKind"/> at <paramref name="index"/>.</summary>
    public SymbolKind GetKind(int index) { CheckIndex(index); return (SymbolKind)_kinds[index]; }

    /// <summary>Gets the type handle at <paramref name="index"/>.</summary>
    public uint GetTypeId(int index) { CheckIndex(index); return _typeIds[index]; }

    /// <summary>Gets the scope handle at <paramref name="index"/>.</summary>
    public uint GetScopeId(int index) { CheckIndex(index); return _scopeIds[index]; }

    /// <summary>Gets the StringPool offset of the source file at <paramref name="index"/>.</summary>
    public uint GetFileId(int index) { CheckIndex(index); return _fileIds[index]; }

    /// <summary>Gets the source line number at <paramref name="index"/>.</summary>
    public uint GetLine(int index) { CheckIndex(index); return _lines[index]; }

    /// <summary>Gets the source column number at <paramref name="index"/>.</summary>
    public uint GetColumn(int index) { CheckIndex(index); return _columns[index]; }

    /// <summary>Gets the <see cref="StorageClass"/> at <paramref name="index"/>.</summary>
    public StorageClass GetStorageClass(int index) { CheckIndex(index); return (StorageClass)_storageClasses[index]; }

    private void CheckIndex(int index)
    {
        if (index < 0 || index >= _count)
            throw new ArgumentOutOfRangeException(nameof(index), $"Symbol index {index} out of range 0-{_count - 1}");
    }
}
