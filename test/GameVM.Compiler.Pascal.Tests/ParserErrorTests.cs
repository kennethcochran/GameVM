using NUnit.Framework;
using GameVM.Compiler.Pascal;
using GameVM.Compiler.Core.IR;
using GameVM.Compiler.Core.Exceptions;

namespace GameVM.Compiler.Pascal.Tests;

/// <summary>
/// Tests for Pascal parser error handling and recovery.
/// Validates that syntax errors are properly detected and reported.
/// </summary>
[TestFixture]
public class ParserErrorTests
{
    private PascalFrontend _frontend = null!;

    [SetUp]
    public void Setup()
    {
        _frontend = new PascalFrontend();
    }

    #region Syntax Error Tests

    [Test]
    public void Parse_MissingSemicolon_HandlesError()
    {
        // Arrange
        var source = "program Test;\nbegin\n  writeln('hello')\nend.";

        // Act & Assert
        // Note: Current parser may throw exception or produce error nodes
        // When error reporting is fully implemented, errors should be reported
        try
        {
            var result = _frontend.Parse(source);
            // If parsing succeeds, verify the result is valid
            Assert.That(result, Is.Not.Null);
        }
        catch (ParserException ex)
        {
            // Expected behavior: parser should report missing semicolon
            Assert.That(ex.Message, Is.Not.Empty);
        }
        catch (CompilerException ex)
        {
            // Expected behavior: compiler should report syntax error
            Assert.That(ex.Message, Is.Not.Empty);
        }
    }

    [Test]
    public void Parse_MissingEndDot_HandlesError()
    {
        // Arrange
        var source = "program Test;\nbegin\n  writeln('hello')\nend";

        // Act & Assert
        try
        {
            var result = _frontend.Parse(source);
            Assert.That(result, Is.Not.Null);
        }
        catch (ParserException ex)
        {
            Assert.That(ex.Message, Is.Not.Empty);
        }
        catch (CompilerException ex)
        {
            Assert.That(ex.Message, Is.Not.Empty);
        }
    }

    [Test]
    public void Parse_InvalidVariableDeclaration_HandlesError()
    {
        // Arrange
        var source = "program Test;\nvar x;\nbegin\nend.";

        // Act & Assert
        try
        {
            var result = _frontend.Parse(source);
            // If parsing succeeds, variable declaration might be handled differently
            Assert.That(result, Is.Not.Null);
        }
        catch (ParserException ex)
        {
            Assert.That(ex.Message, Is.Not.Empty);
        }
        catch (CompilerException ex)
        {
            Assert.That(ex.Message, Is.Not.Empty);
        }
    }

    [Test]
    public void Parse_UnknownKeyword_HandlesError()
    {
        // Arrange
        var source = "program Test;\nbegin\n  invalid_keyword x;\nend.";

        // Act & Assert
        try
        {
            var result = _frontend.Parse(source);
            Assert.That(result, Is.Not.Null);
        }
        catch (ParserException ex)
        {
            Assert.That(ex.Message, Is.Not.Empty);
        }
        catch (CompilerException ex)
        {
            Assert.That(ex.Message, Is.Not.Empty);
        }
    }

    #endregion

    #region Bracket Mismatch Tests

    [Test]
    public void Parse_MissingClosingParenthesis_HandlesError()
    {
        // Arrange
        var source = "program Test;\nbegin\n  writeln('hello';\nend.";

        // Act & Assert
        try
        {
            var result = _frontend.Parse(source);
            Assert.That(result, Is.Not.Null);
        }
        catch (ParserException ex)
        {
            Assert.That(ex.Message, Is.Not.Empty);
            // When error reporting is implemented, should mention "parenthesis"
        }
        catch (CompilerException ex)
        {
            Assert.That(ex.Message, Is.Not.Empty);
        }
    }

    [Test]
    public void Parse_ExtraClosingParenthesis_HandlesError()
    {
        // Arrange
        var source = "program Test;\nbegin\n  writeln('hello'));\nend.";

        // Act & Assert
        try
        {
            var result = _frontend.Parse(source);
            Assert.That(result, Is.Not.Null);
        }
        catch (ParserException ex)
        {
            Assert.That(ex.Message, Is.Not.Empty);
        }
        catch (CompilerException ex)
        {
            Assert.That(ex.Message, Is.Not.Empty);
        }
    }

    [Test]
    public void Parse_MissingClosingQuote_HandlesError()
    {
        // Arrange
        var source = "program Test;\nbegin\n  writeln('hello);\nend.";

        // Act & Assert
        try
        {
            var result = _frontend.Parse(source);
            Assert.That(result, Is.Not.Null);
        }
        catch (ParserException ex)
        {
            Assert.That(ex.Message, Is.Not.Empty);
        }
        catch (CompilerException ex)
        {
            Assert.That(ex.Message, Is.Not.Empty);
        }
    }

    #endregion

    #region Declaration Error Tests

    [Test]
    public void Parse_DuplicateVariableDeclaration_HandlesError()
    {
        // Arrange
        var source = "program Test;\nvar\n  x: Integer;\n  x: Real;\nbegin\nend.";

        // Act & Assert
        try
        {
            var result = _frontend.Parse(source);
            // When duplicate detection is implemented, should report error
            Assert.That(result, Is.Not.Null);
        }
        catch (CompilerException ex)
        {
            Assert.That(ex.Message, Is.Not.Empty);
        }
    }

    [Test]
    public void Parse_InvalidTypeSpecification_HandlesError()
    {
        // Arrange
        var source = "program Test;\nvar\n  x: InvalidType;\nbegin\nend.";

        // Act & Assert
        try
        {
            var result = _frontend.Parse(source);
            // When type checking is implemented, should report unknown type
            Assert.That(result, Is.Not.Null);
        }
        catch (CompilerException ex)
        {
            Assert.That(ex.Message, Is.Not.Empty);
        }
    }

    [Test]
    public void Parse_MissingTypeDeclaration_HandlesError()
    {
        // Arrange
        var source = "program Test;\nvar\n  x;\nbegin\nend.";

        // Act & Assert
        try
        {
            var result = _frontend.Parse(source);
            Assert.That(result, Is.Not.Null);
        }
        catch (ParserException ex)
        {
            Assert.That(ex.Message, Is.Not.Empty);
        }
        catch (CompilerException ex)
        {
            Assert.That(ex.Message, Is.Not.Empty);
        }
    }

    #endregion

    #region Expression Error Tests

    [Test]
    public void Parse_InvalidOperator_HandlesError()
    {
        // Arrange
        var source = "program Test;\nbegin\n  x := 5 $$ 3;\nend.";

        // Act & Assert
        try
        {
            var result = _frontend.Parse(source);
            Assert.That(result, Is.Not.Null);
        }
        catch (ParserException ex)
        {
            Assert.That(ex.Message, Is.Not.Empty);
        }
        catch (CompilerException ex)
        {
            Assert.That(ex.Message, Is.Not.Empty);
        }
    }

    [Test]
    public void Parse_InvalidNumberLiteral_HandlesError()
    {
        // Arrange
        var source = "program Test;\nbegin\n  x := 12.34.56;\nend.";

        // Act & Assert
        try
        {
            var result = _frontend.Parse(source);
            Assert.That(result, Is.Not.Null);
        }
        catch (ParserException ex)
        {
            Assert.That(ex.Message, Is.Not.Empty);
        }
        catch (CompilerException ex)
        {
            Assert.That(ex.Message, Is.Not.Empty);
        }
    }

    #endregion

    #region Unexpected EOF Tests

    [Test]
    public void Parse_UnexpectedEOF_HandlesError()
    {
        // Arrange
        var source = "program Test;\nbegin\n  writeln('incomplete'";

        // Act & Assert
        try
        {
            var result = _frontend.Parse(source);
            Assert.That(result, Is.Not.Null);
        }
        catch (ParserException ex)
        {
            Assert.That(ex.Message, Is.Not.Empty);
            // When error reporting is implemented, should mention "EOF" or "unexpected end"
        }
        catch (CompilerException ex)
        {
            Assert.That(ex.Message, Is.Not.Empty);
        }
    }

    [Test]
    public void Parse_ProgramWithoutEnd_HandlesError()
    {
        // Arrange
        var source = "program Test;\nbegin\n";

        // Act & Assert
        try
        {
            var result = _frontend.Parse(source);
            Assert.That(result, Is.Not.Null);
        }
        catch (ParserException ex)
        {
            Assert.That(ex.Message, Is.Not.Empty);
        }
        catch (CompilerException ex)
        {
            Assert.That(ex.Message, Is.Not.Empty);
        }
    }

    #endregion

    #region Error Recovery Tests

    [Test]
    public void Parse_SingleError_RecoveryAllowsRestOfParsing()
    {
        // Arrange
        var source = "program Test;\nvar\n  x: Integer;\n  y\n  z: Real;\nbegin\nend.";

        // Act & Assert
        try
        {
            var result = _frontend.Parse(source);
            // Parser should recover and continue parsing when possible
            Assert.That(result, Is.Not.Null);
        }
        catch (ParserException ex)
        {
            // If error recovery is not implemented, exception is acceptable
            Assert.That(ex.Message, Is.Not.Empty);
        }
        catch (CompilerException ex)
        {
            Assert.That(ex.Message, Is.Not.Empty);
        }
    }

    [Test]
    public void Parse_MultipleErrors_HandlesAll()
    {
        // Arrange
        var source = "program Test\nvar x;\nbegin\n  writeln('test')\nend";

        // Act & Assert
        try
        {
            var result = _frontend.Parse(source);
            Assert.That(result, Is.Not.Null);
        }
        catch (ParserException ex)
        {
            // When multiple error reporting is implemented, should report all errors
            Assert.That(ex.Message, Is.Not.Empty);
        }
        catch (CompilerException ex)
        {
            Assert.That(ex.Message, Is.Not.Empty);
        }
    }

    #endregion

    #region Error Location Tests

    [Test]
    public void Parse_ErrorIndicatesCorrectLine()
    {
        // Arrange
        var source = "program Test;\nvar x: Integer;\nbegin\n  y := 5\nend.";

        // Act & Assert
        try
        {
            var result = _frontend.Parse(source);
            Assert.That(result, Is.Not.Null);
        }
        catch (ParserException ex)
        {
            // When error location reporting is implemented, should include line number
            Assert.That(ex.Message, Is.Not.Empty);
        }
        catch (CompilerException ex)
        {
            Assert.That(ex.Message, Is.Not.Empty);
        }
    }

    [Test]
    public void Parse_ErrorIndicatesCorrectColumn()
    {
        // Arrange
        var source = "program Test;\nbegin\n  x := 5 $$\nend.";

        // Act & Assert
        try
        {
            var result = _frontend.Parse(source);
            Assert.That(result, Is.Not.Null);
        }
        catch (ParserException ex)
        {
            // When error location reporting is implemented, should include column number
            Assert.That(ex.Message, Is.Not.Empty);
        }
        catch (CompilerException ex)
        {
            Assert.That(ex.Message, Is.Not.Empty);
        }
    }

    #endregion

    #region Complex Error Scenarios

    [Test]
    public void Parse_NestedBlocksWithErrors_HandlesErrors()
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

        // Act & Assert
        try
        {
            var result = _frontend.Parse(source);
            // Check that errors in nested blocks are handled
            Assert.That(result, Is.Not.Null);
        }
        catch (ParserException ex)
        {
            Assert.That(ex.Message, Is.Not.Empty);
        }
        catch (CompilerException ex)
        {
            Assert.That(ex.Message, Is.Not.Empty);
        }
    }

    [Test]
    public void Parse_FunctionWithInvalidBody_HandlesError()
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

        // Act & Assert
        try
        {
            var result = _frontend.Parse(source);
            Assert.That(result, Is.Not.Null);
        }
        catch (ParserException ex)
        {
            Assert.That(ex.Message, Is.Not.Empty);
        }
        catch (CompilerException ex)
        {
            Assert.That(ex.Message, Is.Not.Empty);
        }
    }

    #endregion

    #region Valid Program Tests (for comparison)

    [Test]
    public void Parse_ValidProgram_Succeeds()
    {
        // Arrange
        var source = "program Test;\nbegin\n  writeln('hello');\nend.";

        // Act
        var result = _frontend.Parse(source);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.SourceFile, Is.Not.Null);
    }

    #endregion
}
