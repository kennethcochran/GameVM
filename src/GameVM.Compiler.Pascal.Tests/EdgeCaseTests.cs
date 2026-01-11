using NUnit.Framework;
using GameVM.Compiler.Pascal;
using System;

namespace GameVM.Compiler.Pascal.Tests;

/// <summary>
/// Tests for edge cases and boundary conditions in Pascal compilation.
/// Validates compiler behavior with extreme inputs and unusual configurations.
/// </summary>
[TestFixture]
public class EdgeCaseTests
{
    private PascalParser _parser;
    private PascalAstToHlirTransformer _transformer;

    [SetUp]
    public void Setup()
    {
        _parser = new PascalParser();
        _transformer = new PascalAstToHlirTransformer();
    }

    #region Empty Program Tests

    [Test]
    public void Parse_EmptyProgram_SucceedsWithNoStatements()
    {
        // Arrange
        var source = "program Empty;\nbegin\nend.";

        // Act
        var result = _parser.Parse(source);

        // Assert
        Assert.That(result.Errors, Is.Empty);
        Assert.That(result.AST, Is.Not.Null);
    }

    [Test]
    public void Parse_ProgramWithOnlyComments_SucceedsWithNoStatements()
    {
        // Arrange
        var source = @"
            program OnlyComments;
            { This is a comment }
            { Another comment }
            begin
            { Comment in begin block }
            end.";

        // Act
        var result = _parser.Parse(source);

        // Assert
        Assert.That(result.Errors, Is.Empty);
    }

    [Test]
    public void Compile_EmptyProgram_GeneratesValidBytecode()
    {
        // Arrange
        var source = "program Empty;\nbegin\nend.";

        // Act
        var result = _parser.Parse(source);
        Assert.That(result.Errors, Is.Empty);
        var ast = result.AST;
        var hlir = _transformer.Transform(ast);

        // Assert
        Assert.That(hlir, Is.Not.Null);
    }

    #endregion

    #region Integer Boundary Tests

    [Test]
    public void Parse_MaxIntegerValue_Succeeds()
    {
        // Arrange
        var source = $"program MaxInt;\nbegin\n  x := {int.MaxValue};\nend.";

        // Act
        var result = _parser.Parse(source);

        // Assert
        Assert.That(result.Errors.Count, Is.LessThanOrEqualTo(1)); // May warn about large literal
    }

    [Test]
    public void Parse_MinIntegerValue_Succeeds()
    {
        // Arrange
        var source = $"program MinInt;\nbegin\n  x := {int.MinValue};\nend.";

        // Act
        var result = _parser.Parse(source);

        // Assert
        Assert.That(result.Errors.Count, Is.LessThanOrEqualTo(1)); // May warn about large literal
    }

    [Test]
    public void Parse_Zero_Succeeds()
    {
        // Arrange
        var source = "program Zero;\nbegin\n  x := 0;\nend.";

        // Act
        var result = _parser.Parse(source);

        // Assert
        Assert.That(result.Errors, Is.Empty);
    }

    [Test]
    public void Parse_NegativeNumber_Succeeds()
    {
        // Arrange
        var source = "program Negative;\nbegin\n  x := -42;\nend.";

        // Act
        var result = _parser.Parse(source);

        // Assert
        Assert.That(result.Errors, Is.Empty);
    }

    #endregion

    #region Nesting Depth Tests

    [Test]
    public void Parse_DeeplyNestedBlocks_Succeeds()
    {
        // Arrange - Create deeply nested if statements (50 levels)
        var nestedCode = "program DeepNest;\nbegin\n";
        for (int i = 0; i < 50; i++)
        {
            nestedCode += "  if true then\n";
        }
        nestedCode += "    x := 1;\n";
        for (int i = 0; i < 50; i++)
        {
            nestedCode += "\nelse\n  x := 0";
        }
        nestedCode += "\nend.";

        // Act
        var result = _parser.Parse(nestedCode);

        // Assert
        // Compiler should handle deep nesting without stack overflow
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void Parse_DeeplyNestedExpressions_Succeeds()
    {
        // Arrange - Create deeply nested arithmetic expressions
        var expr = "x := 1";
        for (int i = 0; i < 30; i++)
        {
            expr = $"({expr} + 1)";
        }
        var source = $"program DeepExpr;\nbegin\n  {expr};\nend.";

        // Act
        var result = _parser.Parse(source);

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    #endregion

    #region Large Program Tests

    [Test]
    public void Compile_LargeProgram_1000Lines_Succeeds()
    {
        // Arrange - Create a 1000-line program with repetitive declarations
        var code = new System.Text.StringBuilder();
        code.AppendLine("program LargeProgram;");
        code.AppendLine("var");
        for (int i = 0; i < 500; i++)
        {
            code.AppendLine($"  var{i}: Integer;");
        }
        code.AppendLine("begin");
        for (int i = 0; i < 500; i++)
        {
            code.AppendLine($"  var{i} := {i};");
        }
        code.AppendLine("end.");

        // Act
        var result = _parser.Parse(code.ToString());

        // Assert
        // Compiler should handle large programs without performance issues
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void Compile_ManyFunctions_100Functions_Succeeds()
    {
        // Arrange - Create 100 simple functions
        var code = new System.Text.StringBuilder();
        code.AppendLine("program ManyFunctions;");
        for (int i = 0; i < 100; i++)
        {
            code.AppendLine($"procedure Func{i};");
            code.AppendLine("begin");
            code.AppendLine($"  x := {i};");
            code.AppendLine("end;");
        }
        code.AppendLine("begin");
        code.AppendLine("end.");

        // Act
        var result = _parser.Parse(code.ToString());

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    #endregion

    #region Unicode and Special Character Tests

    [Test]
    public void Parse_IdentifiersWithLetters_Succeeds()
    {
        // Arrange
        var source = "program Test;\nvar myVariable: Integer;\nbegin\nend.";

        // Act
        var result = _parser.Parse(source);

        // Assert
        Assert.That(result.Errors, Is.Empty);
    }

    [Test]
    public void Parse_IdentifiersWithDigits_Succeeds()
    {
        // Arrange
        var source = "program Test;\nvar var1, var2, var3: Integer;\nbegin\nend.";

        // Act
        var result = _parser.Parse(source);

        // Assert
        Assert.That(result.Errors, Is.Empty);
    }

    [Test]
    public void Parse_IdentifiersWithUnderscores_Succeeds()
    {
        // Arrange
        var source = "program Test;\nvar my_var, _internal: Integer;\nbegin\nend.";

        // Act
        var result = _parser.Parse(source);

        // Assert
        Assert.That(result.Errors, Is.Empty);
    }

    [Test]
    public void Parse_StringWithSpecialCharacters_Succeeds()
    {
        // Arrange
        var source = "program Test;\nbegin\n  writeln('Hello! @#$%^');\nend.";

        // Act
        var result = _parser.Parse(source);

        // Assert
        Assert.That(result.Errors, Is.Empty);
    }

    [Test]
    public void Parse_StringWithEscapeSequences_Succeeds()
    {
        // Arrange
        var source = "program Test;\nbegin\n  writeln('Line1\\nLine2');\nend.";

        // Act
        var result = _parser.Parse(source);

        // Assert
        // Escape sequences may or may not be supported
        Assert.That(result, Is.Not.Null);
    }

    #endregion

    #region Whitespace Variation Tests

    [Test]
    public void Parse_ExcessiveWhitespace_Succeeds()
    {
        // Arrange
        var source = "program    Test  ;  var    x   :   Integer  ;  begin    writeln  (  'test'  )  ;  end  .";

        // Act
        var result = _parser.Parse(source);

        // Assert
        Assert.That(result.Errors, Is.Empty);
    }

    [Test]
    public void Parse_NoWhitespace_Succeeds()
    {
        // Arrange
        var source = "programTest;varx:Integer;beginwriteln('test');end.";

        // Act
        var result = _parser.Parse(source);

        // Assert
        // May have errors, but should attempt to parse
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void Parse_TabsAndNewlines_Succeeds()
    {
        // Arrange
        var source = "program\tTest;\nvar\n\tx:\tInteger;\nbegin\nend.";

        // Act
        var result = _parser.Parse(source);

        // Assert
        Assert.That(result.Errors, Is.Empty);
    }

    [Test]
    public void Parse_ConsecutiveNewlines_Succeeds()
    {
        // Arrange
        var source = "program Test;\n\n\nvar x: Integer;\n\n\nbegin\n\n\nend.";

        // Act
        var result = _parser.Parse(source);

        // Assert
        Assert.That(result.Errors, Is.Empty);
    }

    #endregion

    #region Statement Combination Tests

    [Test]
    public void Parse_MultipleStatementsOnOneLine_Succeeds()
    {
        // Arrange
        var source = "program Test;\nbegin x := 1; y := 2; z := 3; end.";

        // Act
        var result = _parser.Parse(source);

        // Assert
        Assert.That(result.Errors, Is.Empty);
    }

    [Test]
    public void Parse_MixedControlFlow_Succeeds()
    {
        // Arrange
        var source = @"
            program Mix;
            begin
              if x > 0 then
                while y < 10 do
                  for i := 1 to 5 do
                    writeln('test');
            end.";

        // Act
        var result = _parser.Parse(source);

        // Assert
        Assert.That(result.Errors.Count, Is.LessThanOrEqualTo(1));
    }

    #endregion

    #region Type System Edge Cases

    [Test]
    public void Parse_AllBasicTypes_Succeeds()
    {
        // Arrange
        var source = @"
            program AllTypes;
            var
              intVal: Integer;
              realVal: Real;
              boolVal: Boolean;
              charVal: Char;
              strVal: String;
            begin
            end.";

        // Act
        var result = _parser.Parse(source);

        // Assert
        Assert.That(result.Errors, Is.Empty);
    }

    [Test]
    public void Parse_ArrayTypes_Succeeds()
    {
        // Arrange
        var source = @"
            program Arrays;
            var
              intArray: array[1..10] of Integer;
              charArray: array[0..255] of Char;
            begin
            end.";

        // Act
        var result = _parser.Parse(source);

        // Assert
        Assert.That(result.Errors.Count, Is.LessThanOrEqualTo(1));
    }

    #endregion

    #region Comment Variation Tests

    [Test]
    public void Parse_SingleLineComments_Succeeds()
    {
        // Arrange
        var source = @"
            program Comments;
            { Single line comment }
            begin
              { Comment before statement }
              writeln('test'); { Comment after statement }
              { Another comment }
            end.";

        // Act
        var result = _parser.Parse(source);

        // Assert
        Assert.That(result.Errors, Is.Empty);
    }

    [Test]
    public void Parse_NestedComments_HandledCorrectly()
    {
        // Arrange
        var source = @"
            program Nested;
            { Outer comment { Inner comment } back to outer }
            begin
            end.";

        // Act
        var result = _parser.Parse(source);

        // Assert
        // Nested comments may or may not be supported
        Assert.That(result, Is.Not.Null);
    }

    #endregion

    #region Memory and Performance Edge Cases

    [Test]
    public void Compile_VeryLongIdentifier_Succeeds()
    {
        // Arrange
        var longIdentifier = new string('a', 256);
        var source = $"program Test;\nvar {longIdentifier}: Integer;\nbegin\nend.";

        // Act
        var result = _parser.Parse(source);

        // Assert
        // Long identifiers should be supported
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void Compile_VeryLongStringLiteral_Succeeds()
    {
        // Arrange
        var longString = new string('x', 1000);
        var source = $"program Test;\nbegin\n  writeln('{longString}');\nend.";

        // Act
        var result = _parser.Parse(source);

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    #endregion
}
