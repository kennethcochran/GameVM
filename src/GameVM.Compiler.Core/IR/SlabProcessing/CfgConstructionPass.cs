using GameVM.Compiler.Core.IR.Soa;
using GameVM.Compiler.Core.IR.Slab;

namespace GameVM.Compiler.Core.IR.SlabProcessing
{
    /// <summary>
    /// Builds a <see cref="CfgTable"/> from an <see cref="InstList"/> by identifying
    /// basic-block leaders and assigning stable Block IDs. Leaders are: the entry
    /// instruction, and every instruction reported by the successor resolver as a
    /// control-flow target of a terminator. The resolver is the single source of truth
    /// for control flow (it decides which instruction indices are jump targets and/or
    /// fall-through successors), so no fall-through heuristic is applied here.
    /// </summary>
    public sealed class CfgConstructionPass
    {
        private readonly InstList _slab;

        /// <summary>
        /// Initializes a new instance of the <see cref="CfgConstructionPass"/> class.
        /// </note>
        /// <param name="slab">The instruction list to build the CFG from.</param>
        /// <exception cref="ArgumentException">Thrown when the slab is empty.</exception>
        public CfgConstructionPass(InstList slab)
        {
            if (slab.Count == 0)
                throw new System.ArgumentException("Slab must contain at least one instruction", nameof(slab));
            _slab = slab;
        }

        /// <summary>
        /// Constructs the CFG. The <paramref name="successorResolver"/> returns the
        /// instruction indices of all instructions that are control-flow successors of
        /// the terminator at the given index (e.g. the jump target, and/or the
        /// fall-through instruction). Return an empty array for a terminator with no
        /// successors (e.g. return).
        /// </summary>
        /// <returns>The constructed control flow graph.</returns>
        public CfgTable Build(System.Func<int, int[]> successorResolver)
        {
            // Step 1: Identify basic-block leaders.
            // Leaders are: the entry instruction, and every instruction reported by the
            // successor resolver as a control-flow target of a terminator.
            var isLeader = new bool[_slab.Count];

            // Entry instruction (first instruction) is always a leader.
            if (_slab.Count > 0)
                isLeader[0] = true;

            // First pass: walk instructions, mark targets reported by the resolver as leaders.
            for (int i = 0; i < _slab.Count; i++)
            {
                ushort flags = _slab.GetFlags(i);
                bool isTerminator = (flags & (ushort)InstructionFlag.Terminator) != 0;
                if (isTerminator)
                {
                    int[] successors = successorResolver(i);
                    if (successors != null)
                    {
                        foreach (int target in successors)
                        {
                            if (target >= 0 && target < _slab.Count)
                                isLeader[target] = true;
                        }
                    }
                }
            }

            // Step 2: Assign block IDs to each instruction in order.
            // Instructions between leaders (inclusive) belong to the same block.
            int blockCount = 0;
            var blockIdAt = new int[_slab.Count];
            int currentBlockId = -1; // will be incremented to 0 for first block

            for (int i = 0; i < _slab.Count; i++)
            {
                if (isLeader[i])
                {
                    currentBlockId++;
                    blockCount++;
                }
                blockIdAt[i] = currentBlockId;
            }

            // Step 3: Populate InstList.BlockIds[] with BlockId handle values:
            // 0 = unassigned (BlockId.Unassigned), 1+ = assigned block ID (BlockId.FromInt(blockIndex + 1))
            for (int i = 0; i < _slab.Count; i++)
            {
                if (blockIdAt[i] >= 0)
                    _slab.SetBlockId(i, blockIdAt[i] + 1); // Convert to BlockId storage format
                else
                    _slab.SetBlockId(i, 0); // BlockId.Unassigned.Value
            }

            // Step 4: Count edges and build CfgTable.
            var edgeWritten = new int[blockCount];

            // Count edges to size the flat adjacency list.
            int edgePairs = 0;
            for (int i = 0; i < _slab.Count; i++)
            {
                ushort flags = _slab.GetFlags(i);
                bool isTerminator = (flags & (ushort)InstructionFlag.Terminator) != 0;
                if (isTerminator)
                {
                    int[] successors = successorResolver(i);
                    if (successors != null)
                    {
                        foreach (int target in successors)
                        {
                            if (target >= 0 && target < _slab.Count && blockIdAt[target] >= 0)
                            {
                                edgePairs++;
                                edgeWritten[blockIdAt[i]]++;
                            }
                        }
                    }
                }
            }

            var table = new CfgTable(blockCount, edgePairs);

            // Populate blockOffsets: map block ID -> first instruction index in that block.
            for (int i = 0; i < _slab.Count; i++)
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
            for (int i = 0; i < _slab.Count; i++)
            {
                ushort flags = _slab.GetFlags(i);
                bool isTerminator = (flags & (ushort)InstructionFlag.Terminator) != 0;
                if (isTerminator)
                {
                    int[] successors = successorResolver(i);
                    if (successors != null)
                    {
                        int srcBlock = blockIdAt[i];
                        foreach (int target in successors)
                        {
                            if (target >= 0 && target < _slab.Count && blockIdAt[target] >= 0)
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