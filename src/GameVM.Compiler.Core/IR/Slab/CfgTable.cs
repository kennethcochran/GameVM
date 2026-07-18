namespace GameVM.Compiler.Core.IR.Slab
{
    /// <summary>
    /// Control Flow Graph stored as parallel, cache-friendly primitive arrays.
    /// Block IDs are stable references for branch targets (not raw slab offsets).
    /// </summary>
    public struct CfgTable
    {
        /// <summary>Maps BlockID -&gt; first slab offset of that block's instruction.</summary>
        private readonly int[] _blockOffsets;

        /// <summary>Flat adjacency list: each edge is a pair (sourceBlockId, targetBlockId).</summary>
        private readonly int[] _cfgEdges;

        /// <summary>Per-block index into <see cref="_cfgEdges"/> where this block's edges begin.</summary>
        private readonly int[] _edgeStart;

        /// <summary>Per-block count of outgoing edges stored in <see cref="_cfgEdges"/>.</summary>
        private readonly int[] _edgeCount;

        /// <summary>Number of basic blocks in the graph.</summary>
        public int BlockCount => _blockOffsets.Length;

        /// <summary>
        /// Initializes a new instance of the <see cref="CfgTable"/> class with pre-sized arrays.
        /// </summary>
        public CfgTable(int blockCount, int edgeCapacity)
        {
            if (blockCount < 0)
                throw new System.ArgumentOutOfRangeException(nameof(blockCount));
            if (edgeCapacity < 0)
                throw new System.ArgumentOutOfRangeException(nameof(edgeCapacity));

            _blockOffsets = new int[blockCount];
            _edgeStart = new int[blockCount];
            _edgeCount = new int[blockCount];
            _cfgEdges = new int[edgeCapacity * 2];
        }

        /// <summary>Sets the slab offset where the given block begins.</summary>
        public void SetBlockOffset(int blockId, int slabOffset)
        {
            _blockOffsets[blockId] = slabOffset;
        }

        /// <summary>Returns the slab offset where the given block begins.</summary>
        public int GetBlockOffset(int blockId) => _blockOffsets[blockId];

        /// <summary>
        /// Records the outgoing-edge span for a block. Edges themselves are written via
        /// <see cref="SetEdge"/> at the returned absolute indices.
        /// </summary>
        public void SetEdgeSpan(int blockId, int start, int count)
        {
            _edgeStart[blockId] = start;
            _edgeCount[blockId] = count;
        }

        /// <summary>Writes a single edge endpoint (source or target) at an absolute edge index.</summary>
        public void SetEdge(int edgeIndex, int blockId)
        {
            _cfgEdges[edgeIndex] = blockId;
        }

        /// <summary>Reads a single edge endpoint at an absolute edge index.</summary>
        public int GetEdge(int edgeIndex) => _cfgEdges[edgeIndex];

        /// <summary>Number of outgoing edges from the given block.</summary>
        public int GetEdgeCount(int blockId) => _edgeCount[blockId];

        /// <summary>Absolute index into <see cref="_cfgEdges"/> where the block's edges begin.</summary>
        public int GetEdgeStart(int blockId) => _edgeStart[blockId];

        /// <summary>
        /// Enumerates the target Block IDs of outgoing edges from the given block.
        /// </summary>
        public int[] GetSuccessors(int blockId)
        {
            int start = _edgeStart[blockId];
            int count = _edgeCount[blockId];
            var successors = new int[count];
            for (int i = 0; i < count; i++)
            {
                // Edges stored as (source, target) pairs; target is the odd element
                successors[i] = _cfgEdges[start + i * 2 + 1];
            }
            return successors;
        }
    }
}
