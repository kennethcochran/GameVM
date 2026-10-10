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
            var isLeader = IdentifyLeaders(successorResolver);

            // Step 2: Assign block IDs to each instruction in order.
            var (blockIdAt, blockCount) = AssignBlockIds(isLeader);

            // Step 3: Populate InstList.BlockIds[] with BlockId handle values.
            PopulateBlockIds(blockIdAt);

            // Step 4: Count edges and build CfgTable.
            return BuildCfgTable(successorResolver, isLeader, blockIdAt, blockCount);
        }

        private bool[] IdentifyLeaders(System.Func<int, int[]> successorResolver)
        {
            // Leaders are: the entry instruction, and every instruction reported by the
            // successor resolver as a control-flow target of a terminator.
            var isLeader = new bool[_slab.Count];

            // Entry instruction (first instruction) is always a leader.
            if (_slab.Count > 0)
                isLeader[0] = true;

            // First pass: walk instructions, mark targets reported by the resolver as leaders.
            for (int i = 0; i < _slab.Count; i++)
            {
                if (!IsTerminator(i))
                    continue;

                int[] successors = successorResolver(i);
                if (successors == null)
                    continue;

                foreach (int target in successors)
                {
                    if (target >= 0 && target < _slab.Count)
                        isLeader[target] = true;
                }
            }
            return isLeader;
        }

        private (int[] blockIdAt, int blockCount) AssignBlockIds(bool[] isLeader)
        {
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
            return (blockIdAt, blockCount);
        }

        private void PopulateBlockIds(int[] blockIdAt)
        {
            // 0 = unassigned (BlockId.Unassigned), 1+ = assigned block ID (BlockId.FromInt(blockIndex + 1))
            for (int i = 0; i < _slab.Count; i++)
            {
                if (blockIdAt[i] >= 0)
                    _slab.SetBlockId(i, blockIdAt[i] + 1); // Convert to BlockId storage format
                else
                    _slab.SetBlockId(i, 0); // BlockId.Unassigned.Value
            }
        }

        private CfgTable BuildCfgTable(
            System.Func<int, int[]> successorResolver,
            bool[] isLeader,
            int[] blockIdAt,
            int blockCount)
        {
            var edgeWritten = new int[blockCount];

            // Count edges to size the flat adjacency list.
            int edgePairs = CountEdges(successorResolver, blockIdAt, edgeWritten);

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
            PopulateEdges(table, successorResolver, blockIdAt, edgeStart);

            // Record per-block edge spans.
            for (int b = 0; b < blockCount; b++)
            {
                table.SetEdgeSpan(b, edgeStart[b], edgeWritten[b]);
            }

            return table;
        }

        private int CountEdges(
            System.Func<int, int[]> successorResolver,
            int[] blockIdAt,
            int[] edgeWritten)
        {
            int edgePairs = 0;
            for (int i = 0; i < _slab.Count; i++)
            {
                if (!IsTerminator(i))
                    continue;

                int[] successors = successorResolver(i);
                if (successors == null)
                    continue;

                edgePairs += CountValidSuccessors(successors, blockIdAt, blockIdAt[i], edgeWritten);
            }
            return edgePairs;
        }

        private static int CountValidSuccessors(int[] successors, int[] blockIdAt, int srcBlock, int[] edgeWritten)
        {
            int count = 0;
            foreach (int target in successors)
            {
                if (target < 0 || target >= blockIdAt.Length || blockIdAt[target] < 0)
                    continue;
                count++;
                edgeWritten[srcBlock]++;
            }
            return count;
        }

        private void PopulateEdges(
            CfgTable table,
            System.Func<int, int[]> successorResolver,
            int[] blockIdAt,
            int[] edgeStart)
        {
            var edgeCursor = (int[])edgeStart.Clone();
            for (int i = 0; i < _slab.Count; i++)
            {
                if (!IsTerminator(i))
                    continue;

                int[] successors = successorResolver(i);
                if (successors == null)
                    continue;

                int srcBlock = blockIdAt[i];
                foreach (int target in successors)
                {
                    if (!IsValidEdgeTarget(target, blockIdAt))
                        continue;

                    int dstBlock = blockIdAt[target];
                    int slot = edgeCursor[srcBlock];
                    table.SetEdge(slot, srcBlock);
                    table.SetEdge(slot + 1, dstBlock);
                    edgeCursor[srcBlock] = slot + 2;
                }
            }
        }

        private bool IsTerminator(int i)
        {
            ushort flags = _slab.GetFlags(i);
            return (flags & (ushort)InstructionFlag.Terminator) != 0;
        }

        private bool IsValidEdgeTarget(int target, int[] blockIdAt)
        {
            return target >= 0 && target < _slab.Count && blockIdAt[target] >= 0;
        }
    }
}