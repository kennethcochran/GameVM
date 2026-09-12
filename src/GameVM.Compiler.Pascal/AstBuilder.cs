using System;

namespace GameVM.Compiler.Pascal.Ast;

/// <summary>
/// Builder for incrementally constructing an <see cref="AstTree"/>.
/// Appends nodes in post-order (children before parent); each node records a span of
/// child indices in a side buffer, so children need not be a contiguous run of nodes.
/// Automatically resizes internal arrays as needed.
/// </summary>
public sealed class AstBuilder
{
    private AstNode[] _nodes;
    private int[] _childIndices;
    private int _childCount;

    private int _count;

    /// <summary>Creates a new <see cref="AstBuilder"/> with the default initial capacity.</summary>
    public AstBuilder() : this(256)
    {
    }

    /// <summary>Creates a new <see cref="AstBuilder"/> with the specified initial capacity.</summary>
    public AstBuilder(int initialCapacity)
    {
        if (initialCapacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(initialCapacity));

        _nodes = new AstNode[initialCapacity];
        _childIndices = new int[initialCapacity * 2];
        _childCount = 0;
        _count = 0;
    }

    /// <summary>Current number of nodes in the builder.</summary>
    public int Count => _count;

    /// <summary>
    /// Adds a new AST node to the tree.
    /// Children must already exist in the builder; their indices are copied into the
    /// side child-index buffer. No contiguity or ordering constraint is imposed.
    /// </summary>
    /// <param name="kind">The node kind (AstNodeKind byte value).</param>
    /// <param name="flags">Instruction flags (Terminator, Diagnostic, etc.).</param>
    /// <param name="payload">By-kind payload: StringPool offset, literal value, operator char, direction, etc.</param>
    /// <param name="childIndices">Indices of child nodes in this builder. Must be in the range [0, <see cref="Count"/>).</param>
    /// <returns>The index of the newly added node.</returns>
    public int Add(byte kind, ushort flags, uint payload, params int[] childIndices)
    {
        EnsureCapacity(_count + 1);

        int firstChild = -1;
        int childCount = 0;

        if (childIndices != null && childIndices.Length > 0)
        {
            EnsureChildCapacity(_childCount + childIndices.Length);
            firstChild = _childCount;
            childCount = childIndices.Length;
            Array.Copy(childIndices, 0, _childIndices, _childCount, childIndices.Length);
            _childCount += childIndices.Length;
        }

        _nodes[_count] = new AstNode(kind, flags, payload, firstChild, childCount);
        return _count++;
    }

    /// <summary>
    /// Overload for single-child nodes (common case).
    /// </summary>
    public int Add(byte kind, ushort flags, uint payload, int childIndex)
    {
        EnsureCapacity(_count + 1);
        EnsureChildCapacity(_childCount + 1);

        int firstChild = _childCount;
        _childIndices[_childCount] = childIndex;
        _childCount++;

        _nodes[_count] = new AstNode(kind, flags, payload, firstChild, 1);
        return _count++;
    }

    /// <summary>
    /// Overload for zero-child (leaf) nodes.
    /// </summary>
    public int Add(byte kind, ushort flags, uint payload)
    {
        EnsureCapacity(_count + 1);

        _nodes[_count] = new AstNode(kind, flags, payload, -1, 0);
        return _count++;
    }

    /// <summary>
    /// Clears the builder, resetting node count to 0.
    /// </summary>
    public void Clear()
    {
        _count = 0;
        _childCount = 0;
    }

    /// <summary>
    /// Builds an immutable <see cref="AstTree"/> from the accumulated nodes.
    /// The underlying arrays are NOT copied — the tree wraps the builder's arrays.
    /// The builder can be reused for another tree after calling <see cref="Clear"/>.
    /// </summary>
    /// <returns>An immutable <see cref="AstTree"/> view of the built nodes.</returns>
    public AstTree Build()
    {
        return new AstTree(_nodes, _childIndices, _count);
    }

    /// <summary>
    /// Builds an immutable <see cref="AstTree"/> from the accumulated nodes,
    /// ensuring a minimum capacity.
    /// </summary>
    public AstTree Build(uint minCapacity)
    {
        EnsureCapacity((int)minCapacity);
        return new AstTree(_nodes, _childIndices, _count);
    }

    private void EnsureCapacity(int required)
    {
        if (required <= _nodes.Length)
            return;

        int newCapacity = Math.Max(_nodes.Length * 2, required);
        Array.Resize(ref _nodes, newCapacity);
    }

    private void EnsureChildCapacity(int required)
    {
        if (required <= _childIndices.Length)
            return;

        int newCapacity = Math.Max(_childIndices.Length * 2, required);
        Array.Resize(ref _childIndices, newCapacity);
    }
}
