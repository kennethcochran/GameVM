using System;
using System.Diagnostics;
using GameVM.Compiler.Core.IR.Ast;

namespace GameVM.Compiler.Core.IR.Soa
{
    /// <summary>
    /// Builder for incrementally constructing an <see cref="AstTree"/>.
    /// Appends nodes in post-order (children before parent) so that each node's
    /// children occupy a contiguous span in the resulting array.
    /// Automatically resizes internal array as needed.
    /// </summary>
    public sealed class AstBuilder
    {
        private AstNode[] _nodes;
        private int _count;

        /// <summary>Creates a new <see cref="AstBuilder"/> with the default initial capacity.</summary>
        public AstBuilder() : this(256) { }

        /// <summary>Creates a new <see cref="AstBuilder"/> with the specified initial capacity.</summary>
        public AstBuilder(int initialCapacity)
        {
            _nodes = new AstNode[initialCapacity];
            _count = 0;
        }

        /// <summary>Current number of nodes in the builder.</summary>
        public int Count => _count;

        /// <summary>
        /// Adds a new AST node to the tree.
        /// Children must already exist in the builder; their indices are passed via <paramref name="childIndices"/>.
        /// The new node's children will be a contiguous span starting at <paramref name="childIndices"/>[0].
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
                // Children are already in the array (post-order construction).
                firstChild = childIndices[0];
                childCount = childIndices.Length;

                // Validate contiguous span: children must form an unbroken run in post-order.
                Debug.Assert(
                    childCount == 1 || childIndices[childCount - 1] - childIndices[0] + 1 == childCount,
                    $"Child indices must be contiguous. Got [{childIndices[0]}..{childIndices[childCount - 1]}] with count {childCount}.");
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

            _nodes[_count] = new AstNode(kind, flags, payload, childIndex, 1);
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
        }

        /// <summary>
        /// Builds an immutable <see cref="AstTree"/> from the accumulated nodes.
        /// The underlying array is NOT copied — the tree wraps the builder's array.
        /// The builder can be reused for another tree after calling <see cref="Clear"/>.
        /// </summary>
        /// <returns>An immutable <see cref="AstTree"/> view of the built nodes.</returns>
        public AstTree Build()
        {
            return new AstTree(_nodes, _count);
        }

        private void EnsureCapacity(int required)
        {
            if (required <= _nodes.Length)
                return;

            int newCapacity = Math.Max(_nodes.Length * 2, required);
            Array.Resize(ref _nodes, newCapacity);
        }
    }
}
