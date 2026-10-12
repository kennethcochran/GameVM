using GameVM.Compiler.CSharp.Transformers;
using NUnit.Framework;

namespace GameVM.DevTools.Tests;

[TestFixture]
public class CSharpToAstVisitorTests
{
    [Test]
    public void MapTypeNameToKind_Int_ReturnsOne()
    {
        Assert.That(CSharpToAstVisitor.MapTypeNameToKind("int"), Is.EqualTo(1));
    }

    [Test]
    public void MapTypeNameToKind_Int32_ReturnsOne()
    {
        Assert.That(CSharpToAstVisitor.MapTypeNameToKind("int32"), Is.EqualTo(1));
    }

    [Test]
    public void MapTypeNameToKind_Int64_ReturnsOne()
    {
        Assert.That(CSharpToAstVisitor.MapTypeNameToKind("int64"), Is.EqualTo(1));
    }

    [Test]
    public void MapTypeNameToKind_String_ReturnsTwo()
    {
        Assert.That(CSharpToAstVisitor.MapTypeNameToKind("string"), Is.EqualTo(2));
    }

    [Test]
    public void MapTypeNameToKind_Bool_ReturnsThree()
    {
        Assert.That(CSharpToAstVisitor.MapTypeNameToKind("bool"), Is.EqualTo(3));
    }

    [Test]
    public void MapTypeNameToKind_Unknown_ReturnsFour()
    {
        Assert.That(CSharpToAstVisitor.MapTypeNameToKind("float"), Is.EqualTo(4));
        Assert.That(CSharpToAstVisitor.MapTypeNameToKind(""), Is.EqualTo(4));
    }

    [Test]
    public void UnquoteStringLiteral_QuotedString_RemovesQuotes()
    {
        Assert.That(CSharpToAstVisitor.UnquoteStringLiteral("\"hello\""), Is.EqualTo("hello"));
    }

    [Test]
    public void UnquoteStringLiteral_EmptyQuotedString_ReturnsEmpty()
    {
        Assert.That(CSharpToAstVisitor.UnquoteStringLiteral("\"\""), Is.EqualTo(""));
    }

    [Test]
    public void UnquoteStringLiteral_UnquotedString_ReturnsAsIs()
    {
        Assert.That(CSharpToAstVisitor.UnquoteStringLiteral("hello"), Is.EqualTo("hello"));
    }

    [Test]
    public void UnquoteStringLiteral_SingleChar_ReturnsAsIs()
    {
        Assert.That(CSharpToAstVisitor.UnquoteStringLiteral("\""), Is.EqualTo("\""));
    }

    [Test]
    public void UnquoteStringLiteral_MismatchedQuotes_ReturnsAsIs()
    {
        Assert.That(CSharpToAstVisitor.UnquoteStringLiteral("\"hello"), Is.EqualTo("\"hello"));
        Assert.That(CSharpToAstVisitor.UnquoteStringLiteral("hello\""), Is.EqualTo("hello\""));
    }

    [Test]
    public void TryParseIntLiteral_ValidNumber_ReturnsTrueAndValue()
    {
        bool ok = CSharpToAstVisitor.TryParseIntLiteral("42", out uint value);
        Assert.That(ok, Is.True);
        Assert.That(value, Is.EqualTo(42u));
    }

    [Test]
    public void TryParseIntLiteral_InvalidNumber_ReturnsFalse()
    {
        bool ok = CSharpToAstVisitor.TryParseIntLiteral("abc", out uint value);
        Assert.That(ok, Is.False);
        Assert.That(value, Is.EqualTo(0u));
    }

    [Test]
    public void TryParseIntLiteral_Zero_ReturnsTrue()
    {
        bool ok = CSharpToAstVisitor.TryParseIntLiteral("0", out uint value);
        Assert.That(ok, Is.True);
        Assert.That(value, Is.EqualTo(0u));
    }

    [Test]
    public void ParseBoolLiteral_True_ReturnsOne()
    {
        Assert.That(CSharpToAstVisitor.ParseBoolLiteral("true"), Is.EqualTo(1u));
    }

    [Test]
    public void ParseBoolLiteral_False_ReturnsZero()
    {
        Assert.That(CSharpToAstVisitor.ParseBoolLiteral("false"), Is.EqualTo(0u));
    }

    [Test]
    public void ParseBoolLiteral_Other_ReturnsZero()
    {
        Assert.That(CSharpToAstVisitor.ParseBoolLiteral("True"), Is.EqualTo(0u));
        Assert.That(CSharpToAstVisitor.ParseBoolLiteral(""), Is.EqualTo(0u));
    }
}
