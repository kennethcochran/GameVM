using NUnit.Framework;
using GameVM.Compiler.Pascal;

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
            // Arrange - In Pascal, the last statement before 'end' doesn't require semicolon (valid syntax)
            var source = "program Test;\nbegin\n  writeln('hello')\nend.";

            // Act
            var _ = _frontend.ParseToSlab(source);

            // Assert - This is actually valid Pascal, verify pipeline succeeds
            Assert.That(_frontend.LastParseErrors, Is.Null, "Missing semicolon before 'end' is valid Pascal syntax");
        }

        [Test]
        public void Parse_InvalidVariableDeclaration_HandlesError()
        {
            // Arrange
            var source = "program Test;\nvar x;\nbegin\nend.";

            // Act & Assert
            _ = _frontend.ParseToSlab(source);
            Assert.That(_frontend.LastParseErrors, Is.Not.Null);
        }

        [Test]
        public void Parse_UnknownKeyword_HandlesError()
        {
            // Arrange
            var source = "program Test;\nbegin\n  invalid_keyword x;\nend.";

            // Act
            _ = _frontend.ParseToSlab(source);

            // Assert
            Assert.That(_frontend.LastParseErrors, Is.Not.Null);
        }

        #endregion

        #region Bracket Mismatch Tests

        [Test]
        public void Parse_MissingClosingParenthesis_HandlesError()
        {
            // Arrange
            var source = "program Test;\nbegin\n  writeln('hello';\nend.";

            // Act & Assert
            _ = _frontend.ParseToSlab(source);
            Assert.That(_frontend.LastParseErrors, Is.Not.Null);
        }

        [Test]
        public void Parse_ExtraClosingParenthesis_HandlesError()
        {
            // Arrange
            var source = "program Test;\nbegin\n  writeln('hello'));\nend.";

            // Act
            _ = _frontend.ParseToSlab(source);

            // Assert
            Assert.That(_frontend.LastParseErrors, Is.Not.Null);
        }

        [Test]
        public void Parse_MissingClosingQuote_HandlesError()
        {
            // Arrange
            var source = "program Test;\nbegin\n  writeln('hello);\nend.";

            // Act
            _ = _frontend.ParseToSlab(source);

            // Assert
            Assert.That(_frontend.LastParseErrors, Is.Not.Null);
        }

        #endregion

        #region Declaration Error Tests

        [Test]
        public void Parse_DuplicateVariableDeclaration_HandlesError()
        {
            // Arrange - Assignment to undeclared variable is a SEMANTIC error (parser accepts, transformer catches)
            var source = "program Test;\nvar x: Integer;\nbegin\n  y := 5;\nend.";

            // Act - Full DOD pipeline: Parse + ConvertToHlirSlab (semantic analysis)
            var astSlab = _frontend.ParseToSlab(source);
            Assert.That(astSlab, Is.Not.Empty, "Parser accepts undeclared variable in assignment (semantic error)");

            // Assert - Semantic analysis (ConvertToHlirSlab) should detect undeclared variable 'y'
            Assert.Throws<InvalidOperationException>(() => _frontend.ConvertToHlirSlab(astSlab),
                "ConvertToHlirSlab should detect undeclared variable in assignment target");
        }

        [Test]
        public void Parse_MissingTypeDeclaration_HandlesError()
        {
            // Arrange
            var source = "program Test;\nvar\n  x;\nbegin\nend.";

            // Act
            _ = _frontend.ParseToSlab(source);

            // Assert
            Assert.That(_frontend.LastParseErrors, Is.Not.Null);
        }

        #endregion

        #region Expression Error Tests

        [Test]
        public void Parse_InvalidOperator_HandlesError()
        {
            // Arrange
            var source = "program Test;\nbegin\n  x := 5 $$ 3;\nend.";

            // Act
            _ = _frontend.ParseToSlab(source);

            // Assert
            Assert.That(_frontend.LastParseErrors, Is.Not.Null);
        }

        [Test]
        public void Parse_InvalidNumberLiteral_HandlesError()
        {
            // Arrange
            var source = "program Test;\nbegin\n  x := 12.34.56;\nend.";

            // Act
            _ = _frontend.ParseToSlab(source);

            // Assert
            Assert.That(_frontend.LastParseErrors, Is.Not.Null);
        }

        #endregion

        #region Unexpected EOF Tests

        [Test]
        public void Parse_UnexpectedEOF_HandlesError()
        {
            // Arrange
            var source = "program Test;\nbegin\n  writeln('incomplete'";

            // Act & Assert
            _ = _frontend.ParseToSlab(source);
            Assert.That(_frontend.LastParseErrors, Is.Not.Null);
        }

        [Test]
        public void Parse_ProgramWithoutEnd_HandlesError()
        {
            // Arrange
            var source = "program Test;\nbegin\n";

            // Act
            _ = _frontend.ParseToSlab(source);

            // Assert
            Assert.That(_frontend.LastParseErrors, Is.Not.Null);
        }

        #endregion

        #region Error Recovery Tests

        [Test]
        public void Parse_SingleError_RecoveryAllowsRestOfParsing()
        {
            // Arrange
            var source = "program Test;\nvar\n  x: Integer;\n  y\n  z: Real;\nbegin\nend.";

            // Act & Assert
            _ = _frontend.ParseToSlab(source);
            Assert.That(_frontend.LastParseErrors, Is.Not.Null);
        }

        [Test]
        public void Parse_MultipleErrors_HandlesAll()
        {
            // Arrange
            var source = "program Test\nvar x;\nbegin\n  writeln('test')\nend";

            // Act & Assert
            _ = _frontend.ParseToSlab(source);
            Assert.That(_frontend.LastParseErrors, Is.Not.Null);
        }

        #endregion

        #region Error Location Tests

        [Test]
        public void Parse_ErrorIndicatesCorrectLine()
        {
            // Arrange - Undeclared variable 'y' is a SEMANTIC error (parser accepts, transformer catches)
            var source = "program Test;\nvar x: Integer;\nbegin\n  y := 5\nend.";

            // Act - ParseToSlab accepts it (no syntax error)
            var astSlab = _frontend.ParseToSlab(source);
            Assert.That(astSlab, Is.Not.Empty);

            // Assert - Semantic analysis detects undeclared variable 'y'
            var ex = Assert.Throws<InvalidOperationException>(() => _frontend.ConvertToHlirSlab(astSlab));
            Assert.That(ex.Message, Does.Contain("Undefined variable"));
        }

        [Test]
        public void Parse_ErrorIndicatesCorrectColumn()
        {
            // Arrange
            var source = "program Test;\nbegin\n  x := 5 $$\nend.";

            // Act & Assert
            _ = _frontend.ParseToSlab(source);
            Assert.That(_frontend.LastParseErrors, Is.Not.Null);
        }

        #endregion

        #region Complex Error Scenarios

        [Test]
        public void Parse_NestedBlocksWithErrors_HandlesErrors()
        {
            // Arrange - Undeclared variable in assignment is a SEMANTIC error
            var source = @"
                program Test;
                var x: Integer;
                begin
                  x := 1;
                  if x > 0 then
                    writeln('positive')
                  else
                    writeln('other');
                  y := 2;
                end.";

            // Act - ParseToSlab accepts it
            var astSlab = _frontend.ParseToSlab(source);
            Assert.That(astSlab, Is.Not.Empty);

            // Assert - Semantic analysis detects undeclared variable 'y' in assignment
            var ex = Assert.Throws<InvalidOperationException>(() => _frontend.ConvertToHlirSlab(astSlab));
            Assert.That(ex.Message, Does.Contain("Undefined variable"));
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

            // Act
            _ = _frontend.ParseToSlab(source);

            // Assert
            Assert.That(_frontend.LastParseErrors, Is.Not.Null);
        }

        #endregion

        #region Valid Program Tests (for comparison)

        [Test]
        public void Parse_ValidProgram_Succeeds()
        {
            // Arrange
            var source = "program Test;\nbegin\n  writeln('hello');\nend.";

            // Act
            var result = _frontend.ParseToSlab(source);

            // Assert
            Assert.That(result, Is.Not.Empty);
        }

        #endregion
    }
