using GameVM.CrapGate.Core;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;

namespace GameVM.DevTools.Tests;

[TestFixture]
public class ComplexityAnalyzerTests
{
    private static int ComplexityOf(string methodCode)
    {
        var tree = CSharpSyntaxTree.ParseText($@"
class C {{
{methodCode}
}}");
        var method = tree.GetRoot()
            .DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Single();
        return ComplexityAnalyzer.ComputeMethodComplexity(method);
    }

    [Test]
    public void EmptyMethod_HasComplexityOne()
    {
        Assert.That(ComplexityOf("void M() { }"), Is.EqualTo(1));
    }

    [Test]
    public void SingleIf_HasComplexityTwo()
    {
        Assert.That(ComplexityOf("void M(bool b) { if (b) { } }"), Is.EqualTo(2));
    }

    [Test]
    public void IfElse_HasComplexityTwo()
    {
        // else doesn't add; only the if decision point
        Assert.That(ComplexityOf("void M(bool b) { if (b) { } else { } }"), Is.EqualTo(2));
    }

    [Test]
    public void NestedIf_HasComplexityThree()
    {
        Assert.That(ComplexityOf("void M(bool a, bool b) { if (a) { if (b) { } } }"), Is.EqualTo(3));
    }

    [Test]
    public void WhileLoop_HasComplexityTwo()
    {
        Assert.That(ComplexityOf("void M(bool b) { while (b) { } }"), Is.EqualTo(2));
    }

    [Test]
    public void ForAndForeach_EachAddOne()
    {
        Assert.That(ComplexityOf("void M(int[] xs) { for (int i = 0; i < 10; i++) { } foreach (var x in xs) { } }"),
            Is.EqualTo(3));
    }

    [Test]
    public void SwitchCases_EachAddsOne()
    {
        Assert.That(ComplexityOf("int M(int x) { switch (x) { case 1: return 1; case 2: return 2; default: return 0; } }"),
            Is.EqualTo(3)); // 1 + 2 cases (default doesn't add)
    }

    [Test]
    public void CatchClause_AddsOne()
    {
        Assert.That(ComplexityOf("void M() { try { } catch (Exception) { } }"), Is.EqualTo(2));
    }

    [Test]
    public void Ternary_AddsOne()
    {
        Assert.That(ComplexityOf("int M(bool b) => b ? 1 : 2;"), Is.EqualTo(2));
    }

    [Test]
    public void LogicalAndOr_EachAddsOne()
    {
        Assert.That(ComplexityOf("bool M(bool a, bool b, bool c) => a && b || c;"), Is.EqualTo(3));
    }

    [Test]
    public void NullCoalescing_AddsOne()
    {
        Assert.That(ComplexityOf("string M(string? s) => s ?? \"default\";"), Is.EqualTo(2));
    }

    [Test]
    public void SwitchExpressionArms_EachAddsOne()
    {
        Assert.That(ComplexityOf("int M(int x) => x switch { 1 => 10, 2 => 20, _ => 0 };"), Is.EqualTo(3));
    }
}
