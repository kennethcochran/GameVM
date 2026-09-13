using System;
using GameVM.Compiler.Core.Enums;

namespace GameVM.Compiler.Core.IR.Soa;

/// <summary>
/// Incrementally constructs a <see cref="DeclarationContext"/> table.
/// Auto-resizes internal arrays as needed.
/// </summary>
public sealed class DeclarationContextBuilder
{
    private const int InitialCapacity = 8;

    private uint[] _parentContextIds;
    private uint[] _ownerSymbolIds;
    private byte[] _kinds;
    private int _count;

    public DeclarationContextBuilder() : this(InitialCapacity) { }

    public DeclarationContextBuilder(int capacity)
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be positive.");

        _parentContextIds = new uint[capacity];
        _ownerSymbolIds = new uint[capacity];
        _kinds = new byte[capacity];
        _count = 0;
    }

    /// <summary>Current number of contexts in the builder.</summary>
    public int Count => _count;

    /// <summary>Adds a declaration context and returns its 0-based index (usable as a ScopeId).</summary>
    public int Add(uint parentContextId, uint ownerSymbolId, ContextKind kind)
    {
        EnsureCapacity(_count + 1);

        int index = _count;
        _parentContextIds[index] = parentContextId;
        _ownerSymbolIds[index] = ownerSymbolId;
        _kinds[index] = (byte)kind;
        _count++;

        return index;
    }

    /// <summary>Builds an immutable <see cref="DeclarationContext"/> from the accumulated entries.</summary>
    public DeclarationContext Build()
    {
        var trimmedParents = new uint[_count];
        var trimmedOwners = new uint[_count];
        var trimmedKinds = new byte[_count];

        Array.Copy(_parentContextIds, 0, trimmedParents, 0, _count);
        Array.Copy(_ownerSymbolIds, 0, trimmedOwners, 0, _count);
        Array.Copy(_kinds, 0, trimmedKinds, 0, _count);

        return new DeclarationContext(trimmedParents, trimmedOwners, trimmedKinds, _count);
    }

    private void EnsureCapacity(int minCount)
    {
        if (_parentContextIds.Length >= minCount)
            return;

        int newCapacity = Math.Max(_parentContextIds.Length * 2, minCount);
        ResizeArrays(newCapacity);
    }

    private void ResizeArrays(int newCapacity)
    {
        var newParents = new uint[newCapacity];
        var newOwners = new uint[newCapacity];
        var newKinds = new byte[newCapacity];

        Array.Copy(_parentContextIds, 0, newParents, 0, _count);
        Array.Copy(_ownerSymbolIds, 0, newOwners, 0, _count);
        Array.Copy(_kinds, 0, newKinds, 0, _count);

        _parentContextIds = newParents;
        _ownerSymbolIds = newOwners;
        _kinds = newKinds;
    }
}
