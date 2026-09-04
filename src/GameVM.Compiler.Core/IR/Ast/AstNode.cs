using System;
using GameVM.Compiler.Core.IR.Soa;

namespace GameVM.Compiler.Core.IR.Ast
{
    /// <summary>
    /// A single node in the Array-of-Structures (AoS) AST.
    /// Fixed-size (16 bytes), laid out flat in an <see cref="AstTree"/> so that walking
    /// the tree touches one contiguous buffer with a uniform stride.
    ///
    /// Children of a node are a contiguous run in the same array
    /// (<see cref="FirstChild"/> .. <see cref="FirstChild"/> + <see cref="ChildCount"/>).
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

        /// <summary>Index of the first child node in the parent <see cref="AstTree"/>, or -1 if none.</summary>
        public readonly int FirstChild;

        /// <summary>Number of contiguous child nodes.</summary>
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

    /// <summary>
    /// Immutable Array-of-Structures (AoS) parse tree.
    /// A single flat <see cref="AstNode"/>[] where each node's fields are adjacent and
    /// children are contiguous spans. Built via <see cref="AstBuilder"/> and treated as
    /// immutable thereafter.
    ///
    /// An <see cref="AstTree"/> is empty when <see cref="Count"/> == 0 (the frontends return
    /// the default/empty tree on parse failure). There is a single implicit root — the
    /// program / method-declaration node — so no explicit root index is stored.
    /// </summary>
    public readonly struct AstTree
    {
        private readonly AstNode[] _nodes;
        private readonly int _count;

        /// <summary>An empty tree. Equivalent to the default value.</summary>
        public static readonly AstTree Empty = new AstTree(Array.Empty<AstNode>(), 0);

        /// <summary>Creates an <see cref="AstTree"/> from a node array. Used primarily by <see cref="AstBuilder"/>.</summary>
        public AstTree(AstNode[] nodes, int count)
        {
            _nodes = nodes;
            _count = count;
        }

        /// <summary>Gets the number of nodes in the tree. 0 means empty (parse failure).</summary>
        public int Count => _count;

        /// <summary>Gets the node at the specified index.</summary>
        public AstNode this[int index] => _nodes[index];

        /// <summary>
        /// Gets the contiguous span of children for the node at <paramref name="index"/>.
        /// Returns an empty span for a leaf or an out-of-range first-child index.
        /// </summary>
        public ReadOnlySpan<AstNode> Children(int index)
        {
            AstNode node = _nodes[index];
            if (node.ChildCount <= 0 || node.FirstChild < 0)
                return ReadOnlySpan<AstNode>.Empty;

            int first = node.FirstChild;
            if (first + node.ChildCount > _count)
                return ReadOnlySpan<AstNode>.Empty;

            return new ReadOnlySpan<AstNode>(_nodes, first, node.ChildCount);
        }

        /// <summary>Gets the kind byte of the node at <paramref name="index"/>.</summary>
        public byte GetKind(int index) => _nodes[index].Kind;
    }
}
