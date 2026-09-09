using GameVM.Compiler.Core.IR.Hlir;
using GameVM.Compiler.Core.IR.Soa;
using NUnit.Framework;

namespace GameVM.Compiler.Core.Tests.IR.Slab;

[TestFixture]
public class HlirBuilderTests
{
    [Test]
    public void EmptyBuilder_Builds_EmptyTree()
    {
        var tree = new HlirBuilder().Build();

        Assert.That(tree.Count, Is.Zero);
        Assert.That(tree.Children(0).Length, Is.Zero); // out-of-range index is safe
    }

    [Test]
    public void LeafNode_WithPoolOffsetPayload()
    {
        var builder = new HlirBuilder();
        int leaf = builder.Add((byte)HlirNodeKind.Identifier, 0, 77, HlirPayloadKind.PoolOffset);

        var tree = builder.Build();
        Assert.That(tree.Count, Is.EqualTo(1));
        Assert.That(tree[leaf].Kind, Is.EqualTo((byte)HlirNodeKind.Identifier));
        Assert.That(tree[leaf].Payload, Is.EqualTo(77u));
        Assert.That(tree[leaf].PayloadKind, Is.EqualTo(HlirPayloadKind.PoolOffset));
        Assert.That(tree[leaf].IsLeaf, Is.True);
        Assert.That(tree.Children(leaf).Length, Is.Zero);
    }

    [Test]
    public void AssignWithTargetAndValue_StoresChildrenInSideBuffer()
    {
        var builder = new HlirBuilder();
        int target = builder.Add((byte)HlirNodeKind.Identifier, 0, 10, HlirPayloadKind.PoolOffset);
        int value = builder.Add((byte)HlirNodeKind.LiteralInt, 0, 42, HlirPayloadKind.Immediate);
        int assign = builder.Add((byte)HlirNodeKind.Assign, 0, 0, HlirPayloadKind.None, target, value);

        var tree = builder.Build();
        Assert.That(tree[assign].ChildCount, Is.EqualTo(2));

        var children = tree.Children(assign);
        Assert.That(children.Length, Is.EqualTo(2));
        Assert.That(children[0], Is.EqualTo(target));
        Assert.That(children[1], Is.EqualTo(value));
        Assert.That(tree[children[0]].Kind, Is.EqualTo((byte)HlirNodeKind.Identifier));
        Assert.That(tree[children[0]].Payload, Is.EqualTo(10u));
        Assert.That(tree[children[1]].Kind, Is.EqualTo((byte)HlirNodeKind.LiteralInt));
        Assert.That(tree[children[1]].Payload, Is.EqualTo(42u));
        Assert.That(tree[children[1]].PayloadKind, Is.EqualTo(HlirPayloadKind.Immediate));
    }

    [Test]
    public void NonContiguousChildren_AreSupported()
    {
        var builder = new HlirBuilder();
        int a = builder.Add((byte)HlirNodeKind.LiteralInt, 0, 1, HlirPayloadKind.Immediate);
        int b = builder.Add((byte)HlirNodeKind.LiteralInt, 0, 2, HlirPayloadKind.Immediate);
        int c = builder.Add((byte)HlirNodeKind.LiteralInt, 0, 3, HlirPayloadKind.Immediate);
        int parent = builder.Add((byte)HlirNodeKind.BinaryOp, 0, (uint)'+', HlirPayloadKind.Immediate, a, c);

        var tree = builder.Build();
        var children = tree.Children(parent);
        Assert.That(children.Length, Is.EqualTo(2));
        Assert.That(children[0], Is.EqualTo(a));
        Assert.That(children[1], Is.EqualTo(c));
        Assert.That(tree[children[0]].Payload, Is.EqualTo(1u));
        Assert.That(tree[children[1]].Payload, Is.EqualTo(3u));
        Assert.That(tree[b].Payload, Is.EqualTo(2u)); // sibling still present
    }

    [Test]
    public void PostOrder_TreeWithNestedBlocks_Builds()
    {
        var builder = new HlirBuilder();
        int innerAssign = builder.Add((byte)HlirNodeKind.Assign, 0, 0, HlirPayloadKind.None);
        int outerStmt = builder.Add((byte)HlirNodeKind.Assign, 0, 0, HlirPayloadKind.None);
        int call = builder.Add((byte)HlirNodeKind.Call, 0, 5, HlirPayloadKind.PoolOffset, innerAssign, outerStmt);
        int ret = builder.Add((byte)HlirNodeKind.Return, 0, 0, HlirPayloadKind.None);

        var tree = builder.Build();
        Assert.That(tree.Count, Is.EqualTo(4));
        Assert.That(tree[call].ChildCount, Is.EqualTo(2));
        Assert.That(tree[ret].IsLeaf, Is.True);
    }

    [Test]
    public void Clear_Resets_BuilderForReuse()
    {
        var builder = new HlirBuilder();
        builder.Add((byte)HlirNodeKind.LiteralInt, 0, 1, HlirPayloadKind.Immediate);
        builder.Clear();
        Assert.That(builder.Count, Is.Zero);

        int leaf = builder.Add((byte)HlirNodeKind.LiteralBool, 0, 1, HlirPayloadKind.Immediate);
        var tree = builder.Build();
        Assert.That(tree.Count, Is.EqualTo(1));
        Assert.That(tree.Children(leaf).Length, Is.Zero);
    }

    [Test]
    public void ManyNodes_Resize_KeepsStructure()
    {
        var builder = new HlirBuilder(4);
        var leaves = new int[100];
        for (int i = 0; i < 100; i++)
            leaves[i] = builder.Add((byte)HlirNodeKind.LiteralInt, 0, (uint)i, HlirPayloadKind.Immediate);

        int parent = builder.Add((byte)HlirNodeKind.Call, 0, 9, HlirPayloadKind.PoolOffset, leaves);
        var tree = builder.Build();

        Assert.That(tree.Count, Is.EqualTo(101));
        var children = tree.Children(parent);
        Assert.That(children.Length, Is.EqualTo(100));
        Assert.That(children[99], Is.EqualTo(leaves[99]));
        Assert.That(tree[leaves[99]].Payload, Is.EqualTo(99u));
    }
}