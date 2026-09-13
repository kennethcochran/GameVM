using System;
using GameVM.Compiler.Core.Enums;

namespace GameVM.Compiler.Core.IR.Soa;

/// <summary>
/// Struct of Arrays (SoA) representation of declaration contexts (scopes).
/// Parallel arrays indexed by context ID (0 = module-level root).
/// Parent chain enables scope nesting traversal.
/// </summary>
public readonly struct DeclarationContext
{
    private readonly uint[] _parentContextIds;
    private readonly uint[] _ownerSymbolIds;
    private readonly byte[] _kinds;
    private readonly int _count;

    /// <summary>Gets the number of declaration contexts.</summary>
    public int Count => _count;

    /// <summary>Creates a <see cref="DeclarationContext"/> from raw arrays. Used by <see cref="DeclarationContextBuilder"/>.</summary>
    public DeclarationContext(
        uint[] parentContextIds,
        uint[] ownerSymbolIds,
        byte[] kinds,
        int count)
    {
        _parentContextIds = parentContextIds;
        _ownerSymbolIds = ownerSymbolIds;
        _kinds = kinds;
        _count = count;
    }

    /// <summary>Gets the parent context ID at <paramref name="index"/> (0 = module level, root has no parent).</summary>
    public uint GetParentContextId(int index) { CheckIndex(index); return _parentContextIds[index]; }

    /// <summary>Gets the symbol that owns this context at <paramref name="index"/>.</summary>
    public uint GetOwnerSymbolId(int index) { CheckIndex(index); return _ownerSymbolIds[index]; }

    /// <summary>Gets the <see cref="ContextKind"/> at <paramref name="index"/>.</summary>
    public ContextKind GetKind(int index) { CheckIndex(index); return (ContextKind)_kinds[index]; }

    private void CheckIndex(int index)
    {
        if (index < 0 || index >= _count)
            throw new ArgumentOutOfRangeException(nameof(index), $"Context index {index} out of range 0-{_count - 1}");
    }
}
