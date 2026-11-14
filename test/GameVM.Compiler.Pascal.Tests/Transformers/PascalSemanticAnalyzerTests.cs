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
    public void TypeMismatchMessageNamesBooleanValueCorrectly()
    {
        // A boolean literal assigned to an Integer must be reported by its
        // value ('true' / 'false'), not by whatever its payload resolves to.
        // The payload is the boolean value itself (1 / 0), not a string-pool
        // offset, so the message must map the value back to its name. Today
        // no error is reported at all: the literal is dropped to a Nop by
        // the visitor, so it never reaches the analyzer as a LiteralBool.
        var trueErrors = Analyze("program Test;\nvar x: Integer;\nbegin\n  x := true;\nend.");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(trueErrors, Has.Length.EqualTo(1));
            Assert.That(trueErrors[0].Message, Does.Contain("Type mismatch: cannot assign 'true' to Integer variable 'x'"));
            Assert.That(trueErrors[0].ErrorCode, Is.EqualTo("PAS0002"));
        }

        var falseErrors = Analyze("program Test;\nvar x: Integer;\nbegin\n  x := false;\nend.");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(falseErrors, Has.Length.EqualTo(1));
            Assert.That(falseErrors[0].Message, Does.Contain("Type mismatch: cannot assign 'false' to Integer variable 'x'"));
            Assert.That(falseErrors[0].ErrorCode, Is.EqualTo("PAS0002"));
        }
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

    [Test]
    public void VariableDeclaredWithNamedTypeResolvesToUnderlyingType()
    {
        // TScore is an alias for Integer, so the string-to-Integer check
        // must apply to s exactly as it does to a plain Integer variable.
        // Today the type section is never visited, the analyzer records
        // the unresolved name 'tscore', and the check silently never fires.
        var errors = Analyze("program Test;\ntype TScore = Integer;\nvar s: TScore;\nbegin\n  s := 'hello';\nend.");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(errors, Has.Length.EqualTo(1));
            Assert.That(errors[0].Message, Does.Contain("Type mismatch: cannot assign 'hello' to Integer variable 's'"));
            Assert.That(errors[0].ErrorCode, Is.EqualTo("PAS0002"));
        }
    }

    [Test]
    public void NamedTypeAliasChainResolvesToUnderlyingType()
    {
        // Resolution is transitive: B names A, A names Integer.
        var errors = Analyze("program Test;\ntype A = Integer; B = A;\nvar x: B;\nbegin\n  x := true;\nend.");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(errors, Has.Length.EqualTo(1));
            Assert.That(errors[0].Message, Does.Contain("Type mismatch: cannot assign 'true' to Integer variable 'x'"));
            Assert.That(errors[0].ErrorCode, Is.EqualTo("PAS0002"));
        }
    }

    [Test]
    public void SubrangeNamedTypeResolvesToIntegerForTypeCheck()
    {
        // A subrange of integer literals is an integer type: the check applies.
        var errors = Analyze("program Test;\ntype TDigit = 0..9;\nvar d: TDigit;\nbegin\n  d := 'x';\nend.");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(errors, Has.Length.EqualTo(1));
            Assert.That(errors[0].Message, Does.Contain("Type mismatch: cannot assign 'x' to Integer variable 'd'"));
            Assert.That(errors[0].ErrorCode, Is.EqualTo("PAS0002"));
        }
    }

    [Test]
    public void NamedTypeVariableAssignedIntegerLiteral_NoErrors()
    {
        // Guard: resolving the named type must not reject valid use.
        var errors = Analyze("program Test;\ntype TScore = Integer;\nvar s: TScore;\nbegin\n  s := 42;\nend.");
        Assert.That(errors, Is.Empty);
    }

    [Test]
    public void TypeSectionDoesNotDisturbPlainIntegerTypeCheck()
    {
        // Guard: the presence of a type section leaves the existing
        // plain-Integer check exactly as it was.
        var errors = Analyze("program Test;\ntype TScore = Integer;\nvar s: TScore; x: Integer;\nbegin\n  x := 'hello';\nend.");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(errors, Has.Length.EqualTo(1));
            Assert.That(errors[0].Message, Does.Contain("Type mismatch: cannot assign 'hello' to Integer variable 'x'"));
            Assert.That(errors[0].ErrorCode, Is.EqualTo("PAS0002"));
        }
    }
}
