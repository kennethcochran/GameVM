using System;
using NUnit.Framework;
using GameVM.Compiler.Core.IR;
using GameVM.Compiler.Core.IR.Buffers;
using GameVM.Compiler.Core.IR.Hlir;
using GameVM.Compiler.Core.IR.Slab;
using GameVM.Compiler.Core.IR.Soa;
using GameVM.Compiler.Core.IR.SlabProcessing;
using GameVM.Compiler.Core.IR.Transformers;
namespace GameVM.Compiler.Core.Tests.IR.Slab;

public class CfgTableTests
{
    [Test]
    public void Constructor_WithValidSizes_CreatesArrays()
    {
        var table = new CfgTable(3, 2);
        Assert.That(table.BlockCount, Is.EqualTo(3));
        Assert.That(table.GetEdgeCount(0), Is.EqualTo(0));
    }

    [Test]
    public void Constructor_WithNegativeBlockCount_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new CfgTable(-1, 0));
    }

    [Test]
    public void SetBlockOffset_RoundTrips()
    {
        var table = new CfgTable(2, 1);
        table.SetBlockOffset(1, 42);
        Assert.That(table.GetBlockOffset(1), Is.EqualTo(42));
    }

    [Test]
    public void SetEdgeSpan_And_GetSuccessors_ReturnsTargets()
    {
        // Block 0 -> Block 1 and Block 2
        var table = new CfgTable(3, 2);
        table.SetEdgeSpan(0, 0, 2);
        table.SetEdge(0, 0); // source
        table.SetEdge(1, 1); // target
        table.SetEdge(2, 0); // source
        table.SetEdge(3, 2); // target

        var successors = table.GetSuccessors(0);
        Assert.That(successors, Is.EquivalentTo(new[] { 1, 2 }));
        Assert.That(table.GetEdgeCount(0), Is.EqualTo(2));
    }

    [Test]
    public void GetSuccessors_NoEdges_ReturnsEmpty()
    {
        var table = new CfgTable(2, 0);
        Assert.That(table.GetSuccessors(1), Is.Empty);
    }
}

public class CfgConstructionPassTests
{
    private static InstList BuildList(
        InstructionFlag flags0 = InstructionFlag.None,
        InstructionFlag flags1 = InstructionFlag.None,
        InstructionFlag flags2 = InstructionFlag.None,
        InstructionFlag flags3 = InstructionFlag.None)
    {
        var builder = new InstListBuilder();
        builder.Add(1, flags0, 0);
        builder.Add(2, flags1, 0);
        builder.Add(3, flags2, 0);
        builder.Add(4, flags3, 0);
        return builder.Build();
    }

    [Test]
    public void Build_SingleLinearBlock_OneBlockNoEdges()
    {
        // No terminators -> single block, no edges
        var slab = BuildList();
        var pass = new CfgConstructionPass(slab);
        var table = pass.Build(_ => new int[0]);

        Assert.That(table.BlockCount, Is.EqualTo(1));
        Assert.That(table.GetBlockOffset(0), Is.EqualTo(0));
        Assert.That(table.GetSuccessors(0), Is.Empty);
    }

    [Test]
    public void Build_BranchSplitsBlocks_AndRecordsEdge()
    {
        // Block A: instr@0 (term -> target@3), instr@1, instr@2
        // Block B: instr@3
        var slab = BuildList(InstructionFlag.Terminator, InstructionFlag.None, InstructionFlag.None, InstructionFlag.None);
        var pass = new CfgConstructionPass(slab);
        var table = pass.Build(idx => idx == 0 ? new[] { 3 } : new int[0]);

        Assert.That(table.BlockCount, Is.EqualTo(2));
        Assert.That(table.GetBlockOffset(0), Is.EqualTo(0));
        Assert.That(table.GetBlockOffset(1), Is.EqualTo(3));
        Assert.That(table.GetSuccessors(0), Is.EquivalentTo(new[] { 1 }));
    }

    [Test]
    public void Build_ReturnTerminator_CreatesBlockButNoEdge()
    {
        // Single terminator returning (no target)
        var builder = new InstListBuilder();
        builder.Add(1, InstructionFlag.Terminator, 0);
        var slab = builder.Build();
        var pass = new CfgConstructionPass(slab);
        var table = pass.Build(_ => new int[0]);

        Assert.That(table.BlockCount, Is.EqualTo(1));
        Assert.That(table.GetSuccessors(0), Is.Empty);
    }

    [Test]
    public void Build_PostTerminatorInstruction_IsLeader()
    {
        // @0 terminator -> @3; @1 falls through -> leader; @3 leader (target)
        var slab = BuildList(InstructionFlag.Terminator, InstructionFlag.None, InstructionFlag.None, InstructionFlag.None);
        var pass = new CfgConstructionPass(slab);
        var table = pass.Build(idx => idx == 0 ? new[] { 3 } : new int[0]);

        // Leaders: @0 and @3 => 2 blocks
        Assert.That(table.BlockCount, Is.EqualTo(2));
        Assert.That(table.GetBlockOffset(0), Is.EqualTo(0));
        Assert.That(table.GetBlockOffset(1), Is.EqualTo(3));
    }

    [Test]
    public void Constructor_WithTooShortSlab_Throws()
    {
        // An empty InstList has no entry instruction and is invalid for CFG construction.
        Assert.Throws<ArgumentException>(
            () => new CfgConstructionPass(new InstListBuilder().Build()).Build(_ => new int[0]));
    }

    [Test]
    public void Build_PopulatesInstListBlockIdsWithBlockIdHandles()
    {
        // Arrange: two blocks (0: instr@0-1, 1: instr@2-3) with a branch from @0 to @2
        var slab = BuildList(InstructionFlag.Terminator, InstructionFlag.None, InstructionFlag.None, InstructionFlag.None);
        var pass = new CfgConstructionPass(slab);
        var table = pass.Build(idx => idx == 0 ? new[] { 2 } : new int[0]);

        // Act & Assert: verify InstList.BlockIds[] contains BlockId handle values
        // BlockId handle mapping: 0 = unassigned, 1+ = assigned block ID (blockIndex + 1)
        ReadOnlySpan<int> blockIds = slab.BlockIds;

        // Instruction 0: leader of block 0 => BlockId.FromInt(0 + 1) = 1
        Assert.That(blockIds[0], Is.EqualTo(1), "Instruction 0 should be in block 0");
        // Instruction 1: in block 0 => BlockId.FromInt(0 + 1) = 1
        Assert.That(blockIds[1], Is.EqualTo(1), "Instruction 1 should be in block 0");
        // Instruction 2: leader of block 1 => BlockId.FromInt(1 + 1) = 2
        Assert.That(blockIds[2], Is.EqualTo(2), "Instruction 2 should be in block 1");
        // Instruction 3: in block 1 => BlockId.FromInt(1 + 1) = 2
        Assert.That(blockIds[3], Is.EqualTo(2), "Instruction 3 should be in block 1");

        // Also verify CfgTable still works correctly
        Assert.That(table.BlockCount, Is.EqualTo(2));
        Assert.That(table.GetBlockOffset(0), Is.EqualTo(0));
        Assert.That(table.GetBlockOffset(1), Is.EqualTo(2));
        Assert.That(table.GetSuccessors(0), Is.EqualTo(new[] { 1 }));
    }

    [Test]
    public void BlocksAreAssignedForBranchingProgram()
    {
        // A branching program (a while loop) lowered to MLIR must come
        // out of the pipeline with real basic blocks: every instruction
        // assigned to a block, the loop split into header/body/exit, and
        // a CfgTable built from that MLIR carrying the loop's edges.
        // Today the CFG pass is wired into nothing, so the MLIR carries
        // no Block IDs at all (all Unassigned) and no terminators are
        // flagged — this fails at the first BlockId assertion.
        var pool = new StringPool();
        var builder = new HlirBuilder();
        uint xOffset = pool.Intern("x");

        // Body of while: x := 0
        int identBody = builder.Add((byte)HlirNodeKind.Identifier, 0, xOffset, HlirPayloadKind.PoolOffset);
        int litBody = builder.Add((byte)HlirNodeKind.LiteralInt, 0, (uint)0, HlirPayloadKind.Immediate);
        int assign = builder.Add((byte)HlirNodeKind.Assign, 0, 0, HlirPayloadKind.None, identBody, litBody);

        // Condition: x <> 0
        int identCond = builder.Add((byte)HlirNodeKind.Identifier, 0, xOffset, HlirPayloadKind.PoolOffset);
        int litCond = builder.Add((byte)HlirNodeKind.LiteralInt, 0, (uint)0, HlirPayloadKind.Immediate);
        int binOpNe = builder.Add((byte)HlirNodeKind.BinaryOp, 0, (uint)'!', HlirPayloadKind.Immediate, identCond, litCond);

        // While node: 2 children — condition + body
        builder.Add((byte)HlirNodeKind.While, 0, 0, HlirPayloadKind.None, binOpNe, assign);

        var tree = builder.Build();
        var mlir = new HlirTreeToMlirTransformer(pool).Transform(tree);

        // Baseline shape this test pins against (same as the transformer's
        // own While test): Label, Cmp, Transition, Branch, Assign, Branch, Label.
        Assert.That(mlir.Count, Is.EqualTo(7));
        Assert.That(mlir.GetKind(0), Is.EqualTo((byte)MlirInstructionKind.Label));
        Assert.That(mlir.GetKind(1), Is.EqualTo((byte)LlirInstructionKind.Cmp));
        Assert.That(mlir.GetKind(2), Is.EqualTo((byte)LlirInstructionKind.Transition));
        Assert.That(mlir.GetKind(3), Is.EqualTo((byte)MlirInstructionKind.Branch));
        Assert.That(mlir.GetKind(4), Is.EqualTo((byte)MlirInstructionKind.Assign));
        Assert.That(mlir.GetKind(5), Is.EqualTo((byte)MlirInstructionKind.Branch));
        Assert.That(mlir.GetKind(6), Is.EqualTo((byte)MlirInstructionKind.Label));

        // Every instruction belongs to a block, split at the branches:
        // header [0..3], body [4..5], exit [6]. Block IDs are the
        // 1-based handles the pass writes (block index + 1).
        int[] expectedBlockIds = { 1, 1, 1, 1, 2, 2, 3 };
        using (Assert.EnterMultipleScope())
        {
            for (int i = 0; i < mlir.Count; i++)
                Assert.That(mlir.GetBlockId(i), Is.EqualTo(expectedBlockIds[i]),
                    $"instruction {i} must sit in block {expectedBlockIds[i] - 1}");
        }

        // The two branches terminate their blocks.
        Assert.That(((InstructionFlag)mlir.GetFlags(3)).HasFlag(InstructionFlag.Terminator), Is.True,
            "the conditional exit branch must terminate the header block");
        Assert.That(((InstructionFlag)mlir.GetFlags(5)).HasFlag(InstructionFlag.Terminator), Is.True,
            "the back-edge branch must terminate the body block");

        // Build the CFG from the MLIR. The successor resolver reads
        // branch targets as Block IDs (the post-slice encoding) and adds
        // the fall-through successor for a conditional branch (the one
        // marked by the Transition before it); an unconditional branch
        // has only its target.
        int FirstIndexOfBlock(int blockIdValue)
        {
            for (int j = 0; j < mlir.Count; j++)
                if (mlir.GetBlockId(j) == blockIdValue) return j;
            return -1;
        }

        int[] Successors(int terminatorIdx)
        {
            if ((MlirInstructionKind)mlir.GetKind(terminatorIdx) != MlirInstructionKind.Branch)
                return Array.Empty<int>();
            int targetIdx = FirstIndexOfBlock((int)mlir.GetOperand(terminatorIdx, 0));
            if (targetIdx < 0)
                return Array.Empty<int>();
            bool isConditional = terminatorIdx > 0 &&
                mlir.GetKind(terminatorIdx - 1) == (byte)LlirInstructionKind.Transition;
            if (isConditional && terminatorIdx + 1 < mlir.Count)
                return new[] { targetIdx, terminatorIdx + 1 };
            return new[] { targetIdx };
        }

        var table = new CfgConstructionPass(mlir).Build(Successors);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(table.BlockCount, Is.EqualTo(3), "the loop has three blocks: header, body, exit");
            Assert.That(table.GetBlockOffset(0), Is.EqualTo(0), "header starts at instruction 0");
            Assert.That(table.GetBlockOffset(1), Is.EqualTo(4), "body starts at instruction 4");
            Assert.That(table.GetBlockOffset(2), Is.EqualTo(6), "exit starts at instruction 6");
            Assert.That(table.GetSuccessors(0), Is.EquivalentTo(new[] { 1, 2 }),
                "the header falls through to the body and branches to the exit");
            Assert.That(table.GetSuccessors(1), Is.EquivalentTo(new[] { 0 }),
                "the body branches back to the header");
            Assert.That(table.GetSuccessors(2), Is.Empty, "the exit block has no outgoing edges");
        }
    }
}