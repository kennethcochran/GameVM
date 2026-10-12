using GameVM.Compiler.CSharp.Transformers;
using NUnit.Framework;

namespace GameVM.DevTools.Tests;

[TestFixture]
public class CSharpToSlabVisitorTests
{
    [Test]
    public void MapTypeNameToKind_Int_ReturnsOne()
    {
        Assert.That(CSharpToSlabVisitor.MapTypeNameToKind("int"), Is.EqualTo(1));
    }

    [Test]
    public void MapTypeNameToKind_String_ReturnsTwo()
    {
        Assert.That(CSharpToSlabVisitor.MapTypeNameToKind("string"), Is.EqualTo(2));
    }

    [Test]
    public void MapTypeNameToKind_Bool_ReturnsThree()
    {
        Assert.That(CSharpToSlabVisitor.MapTypeNameToKind("bool"), Is.EqualTo(3));
    }

    [Test]
    public void MapTypeNameToKind_Unknown_ReturnsFour()
    {
        Assert.That(CSharpToSlabVisitor.MapTypeNameToKind("double"), Is.EqualTo(4));
        Assert.That(CSharpToSlabVisitor.MapTypeNameToKind("int32"), Is.EqualTo(4));
    }
}
