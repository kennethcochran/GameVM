using GameVM.Compiler.Core.IR.Ast;
using GameVM.Compiler.Core.IR.Soa;
using NUnit.Framework;

namespace GameVM.Compiler.Core.Tests.IR.Slab;

[TestFixture]
public class AstBuilderTests
{
    [Test]
    public void EmptyBuilder_Builds_EmptyTree()
    {
        var tree = new AstBuilder().Build();

        Assert.That(tree.Count, Is.Zero);
        Assert.That(tree.Children(0).Length, Is.Zero); // out-of-range index is safe
    }

    [Test]
    public void LeafNode_NoChildren_IsLeaf()
    {
        var builder = new AstBuilder();
        int leaf = builder.Add(1, 0, 42); // kind 1, payload 42

        var tree = builder.Build();
        Assert.That(tree.Count, Is.EqualTo(1));
        Assert.That(tree[leaf].Kind, Is.EqualTo(1));
        Assert.That(tree[leaf].Payload, Is.EqualTo(42u));
        Assert.That(tree[leaf].ChildCount, Is.Zero);
        Assert.That(tree[leaf].IsLeaf, Is.True);
        Assert.That(tree.Children(leaf).Length, Is.Zero);
    }

    [Test]
    public void SingleChild_StoredInSideBuffer()
    {
        var builder = new AstBuilder();
        int leaf = builder.Add(1, 0, 7);
        int parent = builder.Add(2, 0, 0, leaf);

        var tree = builder.Build();
        Assert.That(tree[parent].ChildCount, Is.EqualTo(1));
        Assert.That(tree[parent].FirstChild, Is.GreaterThanOrEqualTo(0));

        var children = tree.Children(parent);
        Assert.That(children.Length, Is.EqualTo(1));
        Assert.That(children[0], Is.EqualTo(leaf));
        Assert.That(tree[children[0]].Kind, Is.EqualTo(1));
        Assert.That(tree[children[0]].Payload, Is.EqualTo(7u));
    }

    [Test]
    public void NonContiguousChildren_AreSupported()
    {
        // The contiguity constraint is removed: a parent may reference children that
        // are NOT adjacent in the node array (the case that crashed VisitBlock).
        var builder = new AstBuilder();
        int a = builder.Add(1, 0, 10);
        int b = builder.Add(2, 0, 20); // sibling added before parent
        int c = builder.Add(3, 0, 30);
        int parent = builder.Add(4, 0, 0, a, c); // children a (index 0) and c (index 2) are non-adjacent

        var tree = builder.Build();
        Assert.That(tree[parent].ChildCount, Is.EqualTo(2));

        var children = tree.Children(parent);
        Assert.That(children.Length, Is.EqualTo(2));
        Assert.That(children[0], Is.EqualTo(a));
        Assert.That(children[1], Is.EqualTo(c));
        Assert.That(tree[children[0]].Payload, Is.EqualTo(10u));
        Assert.That(tree[children[1]].Payload, Is.EqualTo(30u));
        Assert.That(tree[b].Payload, Is.EqualTo(20u)); // sibling node still present
    }

    [Test]
    public void PostOrder_TreeWithBlockNestedInBlock_Builds()
    {
        // The exact shape that previously hit the contiguity Debug.Assert:
        // Program -> Block -> [Block(inner)] where the inner block is a sibling
        // of statements in the outer block.
        var builder = new AstBuilder();
        int innerStmt = builder.Add(7, 0, 0);               // assignment leaf (kind 7)
        int innerBlock = builder.Add(15, 0, 0, innerStmt);  // Block kind 15
        int outerStmt = builder.Add(7, 0, 0);               // another leaf
        int outerBlock = builder.Add(15, 0, 0, innerBlock, outerStmt); // non-contiguous children
        int program = builder.Add(30, 0, 0, outerBlock);

        var tree = builder.Build();

        Assert.That(tree.Count, Is.EqualTo(5));
        Assert.That(tree[program].ChildCount, Is.EqualTo(1));
        Assert.That(tree[outerBlock].ChildCount, Is.EqualTo(2));
        Assert.That(tree[outerBlock].Kind, Is.EqualTo(15));

        var outerChildren = tree.Children(outerBlock);
        Assert.That(tree[outerChildren[0]].Kind, Is.EqualTo(15)); // inner block
        Assert.That(tree[outerChildren[1]].Payload, Is.EqualTo(0u)); // stmt leaf
    }

    [Test]
    public void Clear_Resets_BuilderForReuse()
    {
        var builder = new AstBuilder();
        builder.Add(1, 0, 1);
        builder.Clear();
        Assert.That(builder.Count, Is.Zero);

        int leaf = builder.Add(2, 0, 5);
        var tree = builder.Build();
        Assert.That(tree.Count, Is.EqualTo(1));
        Assert.That(tree.Children(leaf).Length, Is.Zero);
    }

    [Test]
    public void ManyNodes_Resize_KeepsStructure()
    {
        var builder = new AstBuilder(4); // tiny capacity forces resize
        var leaves = new int[100];
        for (int i = 0; i < 100; i++)
            leaves[i] = builder.Add(1, 0, (uint)i);

        int parent = builder.Add(2, 0, 0, leaves);
        var tree = builder.Build();

        var children = tree.Children(parent);
        Assert.That(children.Length, Is.EqualTo(100));
        Assert.That(children[99], Is.EqualTo(leaves[99]));
        Assert.That(tree[leaves[99]].Payload, Is.EqualTo(99u));
    }
}