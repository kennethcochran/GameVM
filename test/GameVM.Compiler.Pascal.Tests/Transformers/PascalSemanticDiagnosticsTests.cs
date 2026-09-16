using System;
using System.Linq;
using Antlr4.Runtime;
using GameVM.Compiler.Core.Interfaces;
using GameVM.Compiler.Core.IR.Buffers;
using GameVM.Compiler.Pascal.ANTLR;
using GameVM.Compiler.Pascal.Semantics;
using GameVM.Compiler.Pascal.Transformers;

namespace GameVM.Compiler.Pascal.Tests.Transformers;

/// <summary>
/// Tests that Pascal semantic errors are surfaced as a UI-neutral DTO
/// through the compilation result — any consumer can read structured
/// diagnostics without parsing a formatted string.
/// </summary>
[TestFixture]
public class PascalSemanticDiagnosticsTests
{
    private static (PascalToAstVisitor Visitor, GameVM.Compiler.Pascal.Ast.AstTree Tree) Build(string code)
    {
        var inputStream = new AntlrInputStream(code);
        var lexer = new PascalLexer(inputStream);
        var tokenStream = new CommonTokenStream(lexer);
        var parser = new PascalParser(tokenStream);
        var context = parser.program();

        var visitor = new PascalToAstVisitor(new StringPool());
        visitor.Visit(context);
        return (visitor, visitor.BuildTree());
    }

    [Test]
    public void Diagnostics_ForUndefinedVariable_CarriesLineAndColumn()
    {
        var code = "program Test;\nvar x: Integer;\nbegin\n  y := 5;\nend.";
        var (visitor, tree) = Build(code);
        var analyzer = new PascalSemanticAnalyzer(visitor.StringPool);
        var errors = analyzer.Analyze(tree, visitor.Positions!).ToArray();
        var diagnostics = new SemanticDiagnostics(errors);

        Assert.That(diagnostics.Errors, Has.Count.EqualTo(1));
        Assert.That(diagnostics.Errors[0].Line, Is.GreaterThan(0));
        Assert.That(diagnostics.Errors[0].Column, Is.GreaterThan(0));
        Assert.That(diagnostics.Errors[0].Message, Does.Contain("Undefined variable 'y'"));
    }

    [Test]
    public void Diagnostics_Summary_RendersLineColumnMessage()
    {
        var code = "program Test;\nvar x: Integer;\nbegin\n  x := 'hello';\nend.";
        var (visitor, tree) = Build(code);
        var analyzer = new PascalSemanticAnalyzer(visitor.StringPool);
        var errors = analyzer.Analyze(tree, visitor.Positions!).ToArray();
        var diagnostics = new SemanticDiagnostics(errors);

        // Summary format: line:column: message
        var line = diagnostics.Errors[0].Line.ToString();
        var col = diagnostics.Errors[0].Column.ToString();
        Assert.That(diagnostics.Summary, Does.Contain(line + ":" + col + ":"));
        Assert.That(diagnostics.Summary, Does.Contain("Type mismatch"));
    }

    [Test]
    public void Diagnostics_MultipleErrors_AllInSummary()
    {
        var code = "program Test;\nvar x: Integer;\nbegin\n  y := 'a';\n  z := 3;\nend.";
        var (visitor, tree) = Build(code);
        var analyzer = new PascalSemanticAnalyzer(visitor.StringPool);
        var errors = analyzer.Analyze(tree, visitor.Positions!).ToArray();
        var diagnostics = new SemanticDiagnostics(errors);

        Assert.That(diagnostics.Errors, Has.Count.EqualTo(2));
        Assert.That(diagnostics.Summary, Does.Contain("Undefined variable 'y'"));
        Assert.That(diagnostics.Summary, Does.Contain("Undefined variable 'z'"));
    }
}