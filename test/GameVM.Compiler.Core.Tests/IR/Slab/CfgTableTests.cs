using GameVM.Compiler.Core.IR.Slab;
using GameVM.Compiler.Core.Utilities;
using NUnit.Framework;

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
        Assert.Throws<System.ArgumentOutOfRangeException>(() => new CfgTable(-1, 0));
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
    private static uint[] BuildSlab(params uint[] instructions)
    {
        var slab = new uint[6 + instructions.Length];
        slab[0] = 0x4741564D;
        slab[1] = 1;
        slab[2] = 0;
        slab[3] = 1;
        slab[4] = (uint)instructions.Length;
        slab[5] = 0;
        Array.Copy(instructions, 0, slab, 6, instructions.Length);
        return slab;
    }

    [Test]
    public void Build_SingleLinearBlock_OneBlockNoEdges()
    {
        var slab = BuildSlab(
            MetadataEncoder.Encode(1, 1, 0),
            MetadataEncoder.Encode(2, 1, 0),
            MetadataEncoder.Encode(3, 1, 0));
        // No terminators -> single block, no edges
        var pass = new CfgConstructionPass(slab);
        var table = pass.Build(_ => new int[0]);

        Assert.That(table.BlockCount, Is.EqualTo(1));
        Assert.That(table.GetBlockOffset(0), Is.EqualTo(6));
        Assert.That(table.GetSuccessors(0), Is.Empty);
    }

    [Test]
    public void Build_BranchSplitsBlocks_AndRecordsEdge()
    {
        // Block A: instr@6 (term -> target@9), instr@7, instr@8
        // Block B: instr@9
        var slab = BuildSlab(
            MetadataEncoder.Encode(1, 1, 0, isTerminator: true), // @6 jumps to @9
            MetadataEncoder.Encode(2, 1, 0),                     // @7
            MetadataEncoder.Encode(3, 1, 0),                     // @8
            MetadataEncoder.Encode(4, 1, 0));                    // @9 (leader)
        var pass = new CfgConstructionPass(slab);
        var table = pass.Build(offset => offset == 6 ? new[] { 9 } : new int[0]);

        Assert.That(table.BlockCount, Is.EqualTo(2));
        Assert.That(table.GetBlockOffset(0), Is.EqualTo(6));
        Assert.That(table.GetBlockOffset(1), Is.EqualTo(9));
        Assert.That(table.GetSuccessors(0), Is.EquivalentTo(new[] { 1 }));
    }

    [Test]
    public void Build_ReturnTerminator_CreatesBlockButNoEdge()
    {
        var slab = BuildSlab(
            MetadataEncoder.Encode(1, 1, 0, isTerminator: true)); // @6 returns (no target)
        var pass = new CfgConstructionPass(slab);
        var table = pass.Build(_ => new int[0]);

        Assert.That(table.BlockCount, Is.EqualTo(1));
        Assert.That(table.GetSuccessors(0), Is.Empty);
    }

    [Test]
    public void Build_PostTerminatorInstruction_IsLeader()
    {
        // @6 terminator (target@9); @7 falls through -> leader; @9 leader (target)
        var slab = BuildSlab(
            MetadataEncoder.Encode(1, 1, 0, isTerminator: true), // @6 -> @9
            MetadataEncoder.Encode(2, 1, 0),                     // @7 leader (fall-through)
            MetadataEncoder.Encode(3, 1, 0),                     // @8
            MetadataEncoder.Encode(4, 1, 0));                    // @9 leader (target)
        var pass = new CfgConstructionPass(slab);
        var table = pass.Build(offset => offset == 6 ? new[] { 9 } : new int[0]);

        // Leaders: @6 (entry/jump), @9 (target) => 2 blocks
        Assert.That(table.BlockCount, Is.EqualTo(2));
        Assert.That(table.GetBlockOffset(0), Is.EqualTo(6));
        Assert.That(table.GetBlockOffset(1), Is.EqualTo(9));
    }

    [Test]
    public void Constructor_WithTooShortSlab_Throws()
    {
        Assert.Throws<System.ArgumentException>(() => new CfgConstructionPass(new uint[3]));
    }
}
