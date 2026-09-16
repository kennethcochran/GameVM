using System;
using System.Linq;
using Antlr4.Runtime;
using GameVM.Compiler.Core.IR.Buffers;
using GameVM.Compiler.Pascal.ANTLR;
using GameVM.Compiler.Pascal.Semantics;
using GameVM.Compiler.Pascal.Transformers;

namespace GameVM.Compiler.Pascal.Tests.Transformers;

/// <summary>
/// Seam tests for the dedicated semantic-analysis pass. The AST is produced by the
/// real visitor (source -> parse tree -> AstTree) so the per-node source-position
/// side table is populated, then analyzed by PascalSemanticAnalyzer alone.
/// </summary>
[TestFixture]
public class PascalSemanticAnalyzerTests
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

    private static GameVM.Compiler.Core.Interfaces.SemanticError[] Analyze(string code)
    {
        var (visitor, tree) = Build(code);
        var analyzer = new PascalSemanticAnalyzer(visitor.StringPool);
        return analyzer.Analyze(tree, visitor.Positions!).ToArray();
    }

    [Test]
    public void Analyze_ValidProgram_NoErrors()
    {
        var errors = Analyze("program Test;\nvar x: Integer;\nbegin\n  x := 5;\nend.");
        Assert.That(errors, Is.Empty);
    }

    [Test]
    public void Analyze_UndefinedAssignmentTarget_ReportsError()
    {
        var errors = Analyze("program Test;\nvar x: Integer;\nbegin\n  y := 5;\nend.");
        Assert.That(errors, Has.Length.EqualTo(1));
        Assert.That(errors[0].Message, Does.Contain("Undefined variable 'y'"));
        Assert.That(errors[0].ErrorCode, Is.EqualTo("PAS0001"));
    }

    [Test]
    public void Analyze_UndefinedVariableInRhsExpression_ReportsError()
    {
        var errors = Analyze("program Test;\nvar x: Integer;\nbegin\n  x := y + 1;\nend.");
        Assert.That(errors, Has.Length.EqualTo(1));
        Assert.That(errors[0].Message, Does.Contain("Undefined variable 'y'"));
        Assert.That(errors[0].ErrorCode, Is.EqualTo("PAS0001"));
    }

    [Test]
    public void Analyze_StringToIntegerMismatch_ReportsError()
    {
        var errors = Analyze("program Test;\nvar x: Integer;\nbegin\n  x := 'hello';\nend.");
        Assert.That(errors, Has.Length.EqualTo(1));
        Assert.That(errors[0].Message, Does.Contain("Type mismatch: cannot assign 'hello' to Integer variable 'x'"));
        Assert.That(errors[0].ErrorCode, Is.EqualTo("PAS0002"));
    }

    [Test]
    public void Analyze_MultipleErrors_AllReportedInOnePass()
    {
        var errors = Analyze("program Test;\nvar x: Integer;\nbegin\n  y := 'a';\n  z := 3;\nend.");
        Assert.That(errors, Has.Length.EqualTo(2));
        Assert.That(errors, Has.Some.Matches<GameVM.Compiler.Core.Interfaces.SemanticError>(e => e.Message.Contains("Undefined variable 'y'") && e.ErrorCode == "PAS0001"));
        Assert.That(errors, Has.Some.Matches<GameVM.Compiler.Core.Interfaces.SemanticError>(e => e.Message.Contains("Undefined variable 'z'") && e.ErrorCode == "PAS0001"));
    }

    [Test]
    public void Analyze_StringToIntegerMismatch_ReportsLineAndColumn()
    {
        var (visitor, _) = Build("program Test;\nvar x: Integer;\nbegin\n  x := 'hello';\nend.");
        var analyzer = new PascalSemanticAnalyzer(visitor.StringPool);
        var errors = analyzer.Analyze(visitor.BuildTree(), visitor.Positions!).ToArray();

        Assert.That(errors, Has.Length.EqualTo(1));
        Assert.That(errors[0].Line, Is.GreaterThan(0), "Line must be nonzero");
        Assert.That(errors[0].Column, Is.GreaterThan(0), "Column must be nonzero");
    }

    [Test]
    public void Analyze_DeclaredVariableInRhs_NoError()
    {
        var errors = Analyze("program Test;\nvar x: Integer; var y: Integer;\nbegin\n  x := y;\nend.");
        Assert.That(errors, Is.Empty);
    }

    [Test]
    public void Analyze_AssignedStringToNonIntegerType_NoError()
    {
        // Scope guard: only Integer targets are checked; a string variable accepts a string.
        var errors = Analyze("program Test;\nvar s: string;\nbegin\n  s := 'hello';\nend.");
        Assert.That(errors, Is.Empty);
    }
}
