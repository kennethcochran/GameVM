using System;

namespace GameVM.Compiler.Core.IR.Ast;

/// <summary>
/// A single node in the Array-of-Structures (AoS) AST.
/// Fixed-size (16 bytes), laid out flat in an <see cref="AstTree"/> so that walking
/// the tree touches one contiguous buffer with a uniform stride.
///
/// Children of a node are addressed via a contiguous span of child indices stored
/// in a separate side buffer (the Zig <c>Ast.zig</c> <c>extra_data</c>/<c>SubRange</c>
/// pattern): <see cref="FirstChild"/> is an offset into that buffer,
/// <see cref="ChildCount"/> is the span length. The node array holds every node once;
/// children are referenced by index, decoupling child order from node allocation
/// order — any legal post-order tree is expressible.
///
/// <see cref="Payload"/> carries the by-kind value: a StringPool offset for
/// identifiers/literals, an operator char for binary ops, a direction for for-loops.
/// </summary>
public readonly struct AstNode
{
    /// <summary>AstNodeKind tag byte.</summary>
    public readonly byte Kind;

    /// <summary>Bitwise flags (InstructionFlag).</summary>
    public readonly ushort Flags;

    /// <summary>By-kind payload (StringPool offset, literal value, operator char, direction).</summary>
    public readonly uint Payload;

    /// <summary>Offset of the child-index span in the parent <see cref="AstTree"/>'s side buffer, or -1 if none.</summary>
    public readonly int FirstChild;

    /// <summary>Number of child indices in the side-buffer span.</summary>
    public readonly int ChildCount;

    public AstNode(byte kind, ushort flags, uint payload, int firstChild, int childCount)
    {
        Kind = kind;
        Flags = flags;
        Payload = payload;
        FirstChild = firstChild;
        ChildCount = childCount;
    }

    /// <summary>True when this node has no children.</summary>
    public bool IsLeaf => ChildCount == 0;
}

// <summary>
// Immutable Array-of-Structures (AoS) parse tree.
// A flat <see cref="AstNode"/>[] holding every node exactly once (fields adjacent per
// node), plus a side buffer of child indices so children need not be a contiguous
// node run. Built via <see cref="AstBuilder"/> and treated as immutable thereafter.
//
// An <see cref="AstTree"/> is empty when <see cref="Count"/> == 0 (the frontends return
// the default/empty tree on parse failure). There is a single implicit root — the
// program / method-declaration node — so no explicit root index is stored.
// </summary>
public readonly struct AstTree
{
    private readonly AstNode[] _nodes;
    private readonly int[] _childIndices;
    private readonly int _count;

    /// <summary>Current number of nodes in the tree.</summary>
    public int Count => _count;

    /// <summary>Gets the node at the given index.</summary>
    public AstNode this[int index] => _nodes[index];

    /// <summary>Gets the child indices side buffer.</summary>
    public int[] ChildIndices => _childIndices;

    /// <summary>Gets the node kind at the given index.</summary>
    public byte GetKind(int index) => _nodes[index].Kind;

    /// <summary>Gets the child index span for a given node.</summary>
    public ReadOnlySpan<int> Children(int index)
    {
        var node = _nodes[index];
        if (node.FirstChild < 0)
            return ReadOnlySpan<int>.Empty;
        return _childIndices.AsSpan(node.FirstChild, node.ChildCount);
    }

    /// <summary>Gets an empty AstTree.</summary>
    public static AstTree Empty => new AstTree(Array.Empty<AstNode>(), Array.Empty<int>(), 0);

    /// <summary>Initializes a new <see cref="AstTree"/> from node and child-index arrays.</summary>
    public AstTree(AstNode[] nodes, int[] childIndices, int count)
    {
        _nodes = nodes;
        _childIndices = childIndices;
        _count = count;
    }
}