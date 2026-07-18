using GameVM.Compiler.Core.Utilities;

using GameVM.Compiler.Core.IR.Slab;
namespace GameVM.Compiler.Core.IR.SlabProcessing
{
    /// <summary>
    /// Builds a <see cref="CfgTable"/> from a linear instruction slab by identifying
    /// basic-block leaders and assigning stable Block IDs. Leaders are: the entry
    /// instruction, and every instruction reported by the successor resolver as a
    /// control-flow target of a terminator. The resolver is the single source of truth
    /// for control flow (it decides which offsets are jump targets and/or fall-through
    /// successors), so no fall-through heuristic is applied here.
    /// </summary>
    public sealed class CfgConstructionPass
    {
        private readonly uint[] _slab;

        /// <summary>
        /// Initializes a new instance of the <see cref="CfgConstructionPass"/> class.
        /// </summary>
        public CfgConstructionPass(uint[] slab)
        {
            _slab = slab ?? throw new System.ArgumentNullException(nameof(slab));
            if (_slab.Length < 6)
                throw new System.ArgumentException("Slab must contain at least 6 indices for the header");
        }

        /// <summary>
        /// Constructs the CFG. The <paramref name="successorResolver"/> returns the slab
        /// offsets of all instructions that are control-flow successors of the terminator
        /// at the given offset (e.g. the jump target, and/or the fall-through instruction).
        /// Return an empty array for a terminator with no successors (e.g. return).
        /// </summary>
        public CfgTable Build(System.Func<int, int[]> successorResolver)
        {
            var isLeader = new bool[_slab.Length];

            // Entry instruction (first block after header) is always a leader.
            int entry = 6;
            if (entry < _slab.Length)
                isLeader[entry] = true;

            // First pass: walk instructions, mark targets reported by the resolver as leaders.
            int offset = entry;
            while (offset < _slab.Length)
            {
                uint metadata = _slab[offset];
                int size = MetadataDecoder.DecodeSize(metadata);
                if (size <= 0)
                    break;

                if (MetadataDecoder.DecodeIsTerminator(metadata))
                {
                    int[] successors = successorResolver(offset);
                    if (successors != null)
                    {
                        foreach (int target in successors)
                        {
                            if (target >= 0 && target < isLeader.Length)
                                isLeader[target] = true;
                        }
                    }
                }

                offset += size;
            }

            // Assign Block IDs to leaders in slab order.
            int blockCount = 0;
            var blockIdAt = new int[_slab.Length];
            for (int i = 0; i < isLeader.Length; i++)
            {
                if (isLeader[i])
                {
                    blockIdAt[i] = blockCount;
                    blockCount++;
                }
                else
                {
                    blockIdAt[i] = -1;
                }
            }

            // Per-block outgoing-edge counts, sized once blockCount is known.
            var edgeWritten = new int[blockCount];

            // Count edges to size the flat adjacency list.
            int edgePairs = 0;
            offset = entry;
            while (offset < _slab.Length)
            {
                uint metadata = _slab[offset];
                int size = MetadataDecoder.DecodeSize(metadata);
                if (size <= 0)
                    break;

                if (MetadataDecoder.DecodeIsTerminator(metadata))
                {
                    int[] successors = successorResolver(offset);
                    if (successors != null)
                    {
                        foreach (int target in successors)
                        {
                            if (target >= 0 && blockIdAt[target] >= 0)
                            {
                                edgePairs++;
                                edgeWritten[blockIdAt[offset]]++;
                            }
                        }
                    }
                }

                offset += size;
            }

            var table = new CfgTable(blockCount, edgePairs);

            // Populate blockOffsets.
            for (int i = 0; i < isLeader.Length; i++)
            {
                if (isLeader[i])
                    table.SetBlockOffset(blockIdAt[i], i);
            }

            // Compute per-block edge span start positions (cumulative by block id).
            var edgeStart = new int[blockCount];
            for (int b = 1; b < blockCount; b++)
            {
                edgeStart[b] = edgeStart[b - 1] + edgeWritten[b - 1] * 2;
            }

            // Populate edges (source, target) pairs, using edgeStart as the write cursor.
            var edgeCursor = (int[])edgeStart.Clone();
            offset = entry;
            while (offset < _slab.Length)
            {
                uint metadata = _slab[offset];
                int size = MetadataDecoder.DecodeSize(metadata);
                if (size <= 0)
                    break;

                if (MetadataDecoder.DecodeIsTerminator(metadata))
                {
                    int[] successors = successorResolver(offset);
                    if (successors != null)
                    {
                        int srcBlock = blockIdAt[offset];
                        foreach (int target in successors)
                        {
                            if (target >= 0 && blockIdAt[target] >= 0)
                            {
                                int dstBlock = blockIdAt[target];
                                int slot = edgeCursor[srcBlock];
                                table.SetEdge(slot, srcBlock);
                                table.SetEdge(slot + 1, dstBlock);
                                edgeCursor[srcBlock] = slot + 2;
                            }
                        }
                    }
                }

                offset += size;
            }

            // Record per-block edge spans.
            for (int b = 0; b < blockCount; b++)
            {
                table.SetEdgeSpan(b, edgeStart[b], edgeWritten[b]);
            }

            return table;
        }
    }
}
