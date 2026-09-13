using GameVM.Compiler.Core.Enums;
using GameVM.Compiler.Core.IR.Soa;

namespace GameVM.Compiler.Core.Tests.IR.Soa;

/// <summary>
/// Unit tests for SymbolTable and SymbolTableBuilder SoA data structures.
/// </summary>
[TestFixture]
public class SymbolTableTests
{
    [Test]
    public void Builder_AddSingleSymbol_ProducesCorrectTable()
    {
        var builder = new SymbolTableBuilder();
        builder.Add(10, SymbolKind.Variable, 0, 0, 0, 1, 2, StorageClass.Local);

        var table = builder.Build();

        Assert.That(table.Count, Is.EqualTo(1));
        Assert.That(table.GetNameOffset(0), Is.EqualTo(10));
        Assert.That(table.GetKind(0), Is.EqualTo(SymbolKind.Variable));
        Assert.That(table.GetStorageClass(0), Is.EqualTo(StorageClass.Local));
        Assert.That(table.GetLine(0), Is.EqualTo(1));
        Assert.That(table.GetColumn(0), Is.EqualTo(2));
    }

    [Test]
    public void Builder_AddMultipleSymbols_AllRetrievable()
    {
        var builder = new SymbolTableBuilder();
        var id0 = builder.Add(10, SymbolKind.Variable, 0, 0, 0, 1, 0, StorageClass.Local);
        var id1 = builder.Add(20, SymbolKind.Function, 0, 0, 0, 5, 0, StorageClass.Global);
        var id2 = builder.Add(30, SymbolKind.Constant, 0, 0, 0, 10, 0, StorageClass.Global);

        var table = builder.Build();

        Assert.That(table.Count, Is.EqualTo(3));
        Assert.That(id0.Value, Is.EqualTo(0));
        Assert.That(id1.Value, Is.EqualTo(1));
        Assert.That(id2.Value, Is.EqualTo(2));
        Assert.That(table.GetKind(0), Is.EqualTo(SymbolKind.Variable));
        Assert.That(table.GetKind(1), Is.EqualTo(SymbolKind.Function));
        Assert.That(table.GetKind(2), Is.EqualTo(SymbolKind.Constant));
    }

    [Test]
    public void Builder_OutOfRangeIndex_ThrowsArgumentOutOfRange()
    {
        var builder = new SymbolTableBuilder();
        builder.Add(10, SymbolKind.Variable, 0, 0, 0, 0, 0, StorageClass.Local);
        var table = builder.Build();

        Assert.Throws<ArgumentOutOfRangeException>(() => table.GetKind(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => table.GetKind(1));
    }

    [Test]
    public void Builder_AutoResizes()
    {
        var builder = new SymbolTableBuilder(2);
        for (int i = 0; i < 100; i++)
            builder.Add((uint)i, SymbolKind.Variable, 0, 0, 0, 0, 0, StorageClass.Local);

        var table = builder.Build();
        Assert.That(table.Count, Is.EqualTo(100));
        Assert.That(table.GetNameOffset(99), Is.EqualTo(99));
    }
}

/// <summary>
/// Unit tests for DeclarationContext and DeclarationContextBuilder.
/// </summary>
[TestFixture]
public class DeclarationContextTests
{
    [Test]
    public void Builder_AddContext_ProducesCorrectTable()
    {
        var builder = new DeclarationContextBuilder();
        int rootId = builder.Add(0, 0, ContextKind.Module);
        int _ = builder.Add((uint)rootId, 1, ContextKind.Function);

        var ctx = builder.Build();

        Assert.That(ctx.Count, Is.EqualTo(2));
        Assert.That(ctx.GetKind(0), Is.EqualTo(ContextKind.Module));
        Assert.That(ctx.GetKind(1), Is.EqualTo(ContextKind.Function));
        Assert.That(ctx.GetParentContextId(0), Is.EqualTo(0));
        Assert.That(ctx.GetParentContextId(1), Is.EqualTo((uint)rootId));
        Assert.That(ctx.GetOwnerSymbolId(1), Is.EqualTo(1));
    }

    [Test]
    public void Builder_NestsCorrectly()
    {
        var builder = new DeclarationContextBuilder();
        int module = builder.Add(0, 0, ContextKind.Module);
        int fn = builder.Add((uint)module, 1, ContextKind.Function);
        int _ = builder.Add((uint)fn, 0, ContextKind.Block);

        var ctx = builder.Build();

        Assert.That(ctx.Count, Is.EqualTo(3));
        Assert.That(ctx.GetParentContextId(2), Is.EqualTo((uint)fn));
        Assert.That(ctx.GetKind(2), Is.EqualTo(ContextKind.Block));
    }

    [Test]
    public void Builder_EmptyTable_ProducesZeroCount()
    {
        var builder = new DeclarationContextBuilder();
        var ctx = builder.Build();
        Assert.That(ctx.Count, Is.EqualTo(0));
    }
}
