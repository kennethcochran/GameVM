using System;
using NUnit.Framework;
using GameVM.Compiler.Core.IR.Slab;
using GameVM.Compiler.Core.IR.Soa;
using GameVM.Compiler.Core.IR.SlabProcessing;
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
}