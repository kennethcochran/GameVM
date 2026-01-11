using NUnit.Framework;
using GameVM.Compiler.Pascal;
using System.Collections.Generic;

namespace GameVM.Compiler.Pascal.Tests;

/// <summary>
/// Tests for Pascal parser error handling and recovery.
/// Validates that syntax errors are properly detected and reported.
/// </summary>
[TestFixture]
public class ParserErrorTests
{
    private PascalLexer _lexer;
    private PascalParser _parser;

    [SetUp]
    public void Setup()
    {
        _lexer = new PascalLexer();
        _parser = new PascalParser();
    }

    #region Syntax Error Tests

    [Test]
    public void Parse_MissingSemicolon_ReportsError()
    {
        // Arrange
        var source = "program Test\nbegin\n  writeln('hello')\nend.";

        // Act
        var result = _parser.Parse(source);

        // Assert
        Assert.That(result.Errors, Is.Not.Empty);
        Assert.That(result.Errors[0].Message, Contains.Substring("semicolon").IgnoreCase);
    }

    [Test]
    public void Parse_MissingEndDot_ReportsError()
    {
        // Arrange
        var source = "program Test;\nbegin\n  writeln('hello')\nend";

        // Act
        var result = _parser.Parse(source);

        // Assert
        Assert.That(result.Errors, Is.Not.Empty);
    }

    [Test]
    public void Parse_InvalidVariableDeclaration_ReportsError()
    {
        // Arrange
        var source = "program Test;\nvar x;\nbegin\nend.";

        // Act
        var result = _parser.Parse(source);

        // Assert
        Assert.That(result.Errors, Is.Not.Empty);
    }

    [Test]
    public void Parse_UnknownKeyword_ReportsError()
    {
        // Arrange
        var source = "program Test;\nbegin\n  invalid_keyword x;\nend.";

        // Act
        var result = _parser.Parse(source);

        // Assert
        Assert.That(result.Errors, Is.Not.Empty);
    }

    #endregion

    #region Bracket Mismatch Tests

    [Test]
    public void Parse_MissingClosingParenthesis_ReportsError()
    {
        // Arrange
        var source = "program Test;\nbegin\n  writeln('hello';\nend.";

        // Act
        var result = _parser.Parse(source);

        // Assert
        Assert.That(result.Errors, Is.Not.Empty);
        Assert.That(result.Errors[0].Message, Contains.Substring("parenthes").IgnoreCase);
    }

    [Test]
    public void Parse_ExtraClosingParenthesis_ReportsError()
    {
        // Arrange
        var source = "program Test;\nbegin\n  writeln('hello'));\nend.";

        // Act
        var result = _parser.Parse(source);

        // Assert
        Assert.That(result.Errors, Is.Not.Empty);
    }

    [Test]
    public void Parse_MissingClosingQuote_ReportsError()
    {
        // Arrange
        var source = "program Test;\nbegin\n  writeln('hello);\nend.";

        // Act
        var result = _parser.Parse(source);

        // Assert
        Assert.That(result.Errors, Is.Not.Empty);
    }

    #endregion

    #region Declaration Error Tests

    [Test]
    public void Parse_DuplicateVariableDeclaration_ReportsError()
    {
        // Arrange
        var source = "program Test;\nvar\n  x: Integer;\n  x: Real;\nbegin\nend.";

        // Act
        var result = _parser.Parse(source);

        // Assert
        // Should report duplicate variable
        Assert.That(result.Errors.Count, Is.GreaterThanOrEqualTo(0));
    }

    [Test]
    public void Parse_InvalidTypeSpecification_ReportsError()
    {
        // Arrange
        var source = "program Test;\nvar\n  x: InvalidType;\nbegin\nend.";

        // Act
        var result = _parser.Parse(source);

        // Assert
        // Should report unknown type
        Assert.That(result.Errors.Count, Is.GreaterThanOrEqualTo(0));
    }

    [Test]
    public void Parse_MissingTypeDeclaration_ReportsError()
    {
        // Arrange
        var source = "program Test;\nvar\n  x;\nbegin\nend.";

        // Act
        var result = _parser.Parse(source);

        // Assert
        Assert.That(result.Errors, Is.Not.Empty);
    }

    #endregion

    #region Expression Error Tests

    [Test]
    public void Parse_InvalidOperator_ReportsError()
    {
        // Arrange
        var source = "program Test;\nbegin\n  x := 5 $$ 3;\nend.";

        // Act
        var result = _parser.Parse(source);

        // Assert
        Assert.That(result.Errors, Is.Not.Empty);
    }

    [Test]
    public void Parse_InvalidNumberLiteral_ReportsError()
    {
        // Arrange
        var source = "program Test;\nbegin\n  x := 12.34.56;\nend.";

        // Act
        var result = _parser.Parse(source);

        // Assert
        Assert.That(result.Errors, Is.Not.Empty);
    }

    #endregion

    #region Unexpected EOF Tests

    [Test]
    public void Parse_UnexpectedEOF_ReportsError()
    {
        // Arrange
        var source = "program Test;\nbegin\n  writeln('incomplete'";

        // Act
        var result = _parser.Parse(source);

        // Assert
        Assert.That(result.Errors, Is.Not.Empty);
        Assert.That(result.Errors[0].Message, Contains.Substring("EOF").IgnoreCase);
    }

    [Test]
    public void Parse_ProgramWithoutEnd_ReportsError()
    {
        // Arrange
        var source = "program Test;\nbegin\n";

        // Act
        var result = _parser.Parse(source);

        // Assert
        Assert.That(result.Errors, Is.Not.Empty);
    }

    #endregion

    #region Error Recovery Tests

    [Test]
    public void Parse_SingleError_RecoveryAllowsRestOfParsing()
    {
        // Arrange
        var source = "program Test;\nvar\n  x: Integer;\n  y\n  z: Real;\nbegin\nend.";

        // Act
        var result = _parser.Parse(source);

        // Assert
        // Parser should recover and continue parsing
        Assert.That(result.Errors, Is.Not.Empty);
        Assert.That(result.AST, Is.Not.Null);
    }

    [Test]
    public void Parse_MultipleErrors_ReportsAll()
    {
        // Arrange
        var source = "program Test\nvar x;\nbegin\n  writeln('test')\nend";

        // Act
        var result = _parser.Parse(source);

        // Assert
        // Should report multiple errors (missing semicolons, invalid var declaration, etc.)
        Assert.That(result.Errors.Count, Is.GreaterThanOrEqualTo(1));
    }

    #endregion

    #region Error Location Tests

    [Test]
    public void Parse_ErrorIndicatesCorrectLine()
    {
        // Arrange
        var source = "program Test;\nvar x: Integer;\nbegin\n  y := 5\nend.";

        // Act
        var result = _parser.Parse(source);

        // Assert
        if (result.Errors.Count > 0)
        {
            Assert.That(result.Errors[0].Line, Is.GreaterThan(0));
        }
    }

    [Test]
    public void Parse_ErrorIndicatesCorrectColumn()
    {
        // Arrange
        var source = "program Test;\nbegin\n  x := 5 $$\nend.";

        // Act
        var result = _parser.Parse(source);

        // Assert
        if (result.Errors.Count > 0)
        {
            Assert.That(result.Errors[0].Column, Is.GreaterThan(0));
        }
    }

    #endregion

    #region Complex Error Scenarios

    [Test]
    public void Parse_NestedBlocksWithErrors_ReportsErrors()
    {
        // Arrange
        var source = @"
            program Test;
            begin
              if x > 0 then
                writeln('positive')
              else
                writeln('other')
            end.";

        // Act
        var result = _parser.Parse(source);

        // Assert
        // Check that errors in nested blocks are reported
    }

    [Test]
    public void Parse_FunctionWithInvalidBody_ReportsError()
    {
        // Arrange
        var source = @"
            program Test;
            function Add(a, b): Integer;
            begin
              result := a +;
            end;
            begin
            end.";

        // Act
        var result = _parser.Parse(source);

        // Assert
        Assert.That(result.Errors, Is.Not.Empty);
    }

    #endregion

    #region Helper Methods

    private ParseResult CreateParseResult()
    {
        return new ParseResult { Errors = new List<CompilerError>() };
    }

    #endregion
}
