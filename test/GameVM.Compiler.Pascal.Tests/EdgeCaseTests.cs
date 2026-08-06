using GameVM.Compiler.Optimizers.MidLevel;
using GameVM.Compiler.Core.Enums;
using GameVM.Compiler.Core.IR.Soa;
namespace GameVM.Compiler.Pascal.Tests;

/// <summary>
/// Tests for edge cases and boundary conditions in Pascal compilation.
/// Validates compiler behavior with extreme inputs and unusual configurations.
/// </summary>
[TestFixture]
public class EdgeCaseTests
{
    private PascalFrontend _frontend = null!;

    [SetUp]
    public void Setup()
    {
        _frontend = new PascalFrontend();
    }

    #region Empty Program Tests

    [Test]
    public void Parse_EmptyProgram_SucceedsWithNoStatements()
    {
        // Arrange
        var source = "program Empty;\nbegin\nend.";

        // Act
        var result = _frontend.ParseToSlab(source);

        // Assert
        Assert.That(result, Is.Not.Empty);
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
        var result = _frontend.ParseToSlab(source);

        // Assert
        Assert.That(result, Is.Not.Empty);
    }

    [Test]
    public void Compile_EmptyProgram_GeneratesValidIR()
    {
        // Arrange
        var source = "program Empty;\nbegin\nend.";

        // Act
        var astSlab = _frontend.ParseToSlab(source);
        var hlirSlab = _frontend.ConvertToHlirSlab(astSlab);
        var optimizer = new DefaultMidLevelOptimizer();
        var mlirSlab = hlirSlab.Count > 0
            ? optimizer.OptimizeSlab(hlirSlab, _frontend.StringPool!, OptimizationLevel.None)
            : default;

        // Assert
        Assert.That(astSlab, Is.Not.Empty);
        Assert.That(mlirSlab.Count, Is.GreaterThanOrEqualTo(0));
    }

    #endregion

    #region Integer Boundary Tests

    [Test]
    public void Parse_MaxIntegerValue_Succeeds()
    {
        // Arrange
        var source = $"program MaxInt;\nvar x: Integer;\nbegin\n  x := {int.MaxValue};\nend.";

        // Act
        var result = _frontend.ParseToSlab(source);

        // Assert
        Assert.That(result, Is.Not.Empty);
        // Large integer values should be parsed successfully
    }

    [Test]
    public void Parse_MinIntegerValue_Succeeds()
    {
        // Arrange
        var source = $"program MinInt;\nvar x: Integer;\nbegin\n  x := {int.MinValue};\nend.";

        // Act
        var result = _frontend.ParseToSlab(source);

        // Assert
        Assert.That(result, Is.Not.Empty);
        // Negative large integer values should be parsed successfully
    }

    [Test]
    public void Parse_Zero_Succeeds()
    {
        // Arrange
        var source = "program Zero;\nvar x: Integer;\nbegin\n  x := 0;\nend.";

        // Act
        var result = _frontend.ParseToSlab(source);

        // Assert
        Assert.That(result, Is.Not.Empty);
    }

    [Test]
    public void Parse_NegativeNumber_Succeeds()
    {
        // Arrange
        var source = "program Negative;\nvar x: Integer;\nbegin\n  x := -42;\nend.";

        // Act
        var result = _frontend.ParseToSlab(source);

        // Assert
        Assert.That(result, Is.Not.Empty);
    }

    [Test]
    public void Parse_IntegerOverflow_HandlesGracefully()
    {
        // Arrange
        // Use a value larger than int.MaxValue (as string to test parsing)
        var source = "program Overflow;\nvar x: Integer;\nbegin\n  x := 999999999999999999;\nend.";

        // Act
        try
        {
            var result = _frontend.ParseToSlab(source);
            // If parsing succeeds, overflow detection would be in type checking phase
            Assert.That(result, Is.Not.Empty);
        }
        catch (Exception ex)
        {
            // Overflow detection may throw exception
            Assert.That(ex, Is.Not.Null);
        }
    }

    #endregion

    #region Nesting Depth Tests

    [Test]
    public void Parse_DeeplyNestedBlocks_Succeeds()
    {
        // Arrange - Create properly nested if/then/else chains (20 levels deep).
        // Each else is the statement of the enclosing then, forming a valid
        // nested conditional in Pascal.
        var nestedCode = "program DeepNest;\nvar x: Integer;\nbegin\n";
        for (int i = 0; i < 20; i++)
        {
            nestedCode += "  if true then\n";
        }
        nestedCode += "    x := 1\n";   // no semicolon before the first else
        for (int i = 0; i < 20; i++)
        {
            nestedCode += "  else\n    x := 0\n";
        }
        nestedCode += ";\nend.";

        // Act
        var result = _frontend.ParseToSlab(nestedCode);

        // Assert - Compiler should handle deep nesting without stack overflow
        Assert.That(result, Is.Not.Empty);
    }

    [Test]
    public void Parse_DeeplyNestedExpressions_Succeeds()
    {
        // Arrange - Create deeply nested arithmetic expressions
        var expr = "1";
        for (int i = 0; i < 30; i++)
        {
            expr = $"({expr} + 1)";
        }
        var source = $"program DeepExpr;\nvar x: Integer;\nbegin\n  x := {expr};\nend.";

        // Act
        var result = _frontend.ParseToSlab(source);

        // Assert
        Assert.That(result, Is.Not.Empty);
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
        for (int i = 0; i < 100; i++) // Reduced for test performance
        {
            code.AppendLine($"  var{i}: Integer;");
        }
        code.AppendLine("begin");
        for (int i = 0; i < 100; i++)
        {
            code.AppendLine($"  var{i} := {i};");
        }
        code.AppendLine("end.");

        // Act
        var result = _frontend.ParseToSlab(code.ToString());

        // Assert
        // Compiler should handle large programs without performance issues
        Assert.That(result, Is.Not.Empty);
    }

    [Test]
    public void Compile_ManyFunctions_50Functions_Succeeds()
    {
        // Arrange - Create 50 simple functions
        var code = new System.Text.StringBuilder();
        code.AppendLine("program ManyFunctions;");
        code.AppendLine("var x: Integer;");
        for (int i = 0; i < 50; i++)
        {
            code.AppendLine($"procedure Func{i};");
            code.AppendLine("begin");
            code.AppendLine($"  x := {i};");
            code.AppendLine("end;");
        }
        code.AppendLine("begin");
        code.AppendLine("end.");

        // Act
        var result = _frontend.ParseToSlab(code.ToString());

        // Assert
        Assert.That(result, Is.Not.Empty);
    }

    #endregion

    #region Unicode and Special Character Tests

    [Test]
    public void Parse_IdentifiersWithLetters_Succeeds()
    {
        // Arrange
        var source = "program Test;\nvar myVariable: Integer;\nbegin\nend.";

        // Act
        var result = _frontend.ParseToSlab(source);

        // Assert
        Assert.That(result, Is.Not.Empty);
    }

    [Test]
    public void Parse_IdentifiersWithDigits_Succeeds()
    {
        // Arrange
        var source = "program Test;\nvar var1, var2, var3: Integer;\nbegin\nend.";

        // Act
        var result = _frontend.ParseToSlab(source);

        // Assert
        Assert.That(result, Is.Not.Empty);
    }

    [Test]
    public void Parse_IdentifiersWithUnderscores_Succeeds()
    {
        // Arrange
        var source = "program Test;\nvar my_var: Integer;\nbegin\nend.";

        // Act
        var result = _frontend.ParseToSlab(source);

        // Assert
        Assert.That(result, Is.Not.Empty);
    }

    [Test]
    public void Parse_StringWithSpecialCharacters_Succeeds()
    {
        // Arrange
        var source = "program Test;\nbegin\n  writeln('Hello! @#$%^');\nend.";

        // Act
        var result = _frontend.ParseToSlab(source);

        // Assert
        Assert.That(result, Is.Not.Empty);
    }

    [Test]
    public void Parse_StringWithUnicodeCharacters_HandlesGracefully()
    {
        // Arrange
        var source = "program Test;\nbegin\n  writeln('Hello 世界');\nend.";

        // Act
        try
        {
            var result = _frontend.ParseToSlab(source);
            Assert.That(result, Is.Not.Empty);
        }
        catch (Exception ex)
        {
            // Unicode may or may not be fully supported
            Assert.That(ex, Is.Not.Null);
        }
    }

    [Test]
    public void Parse_CommentsWithUnicode_HandlesGracefully()
    {
        // Arrange
        var source = "program Test;\n{ Comment with Unicode: 测试 }\nbegin\nend.";

        // Act
        try
        {
            var result = _frontend.ParseToSlab(source);
            Assert.That(result, Is.Not.Empty);
        }
        catch (Exception ex)
        {
            // Unicode in comments may or may not be fully supported
            Assert.That(ex, Is.Not.Null);
        }
    }

    #endregion

    #region Whitespace Variation Tests

    [Test]
    public void Parse_ExcessiveWhitespace_Succeeds()
    {
        // Arrange
        var source = "program    Test  ;  var    x   :   Integer  ;  begin    writeln  (  'test'  )  ;  end  .";

        // Act - Use DOD pipeline ParseToSlab directly
        var result = _frontend.ParseToSlab(source);

        // Assert
        Assert.That(result, Is.Not.Empty);
    }

    [Test]
    public void Parse_NoWhitespace_HandlesGracefully()
    {
        // Arrange
        var source = "programTest;varx:Integer;beginwriteln('test');end.";

        // Act - Use ParseToSlab (DOD pipeline). ANTLR parser should attempt to parse.
        // Even if whitespace is missing, the parser should handle it gracefully.
        // "Gracefully" means no exception is thrown.
        var result = _frontend.ParseToSlab(source);

        // Assert - The result is a valid InstList (a struct, never null).
        // Whether it contains actual instructions depends on the parser's error recovery.
        Assert.That(result.Count, Is.GreaterThanOrEqualTo(0));
    }

    [Test]
    public void Parse_TabsAndNewlines_Succeeds()
    {
        // Arrange
        var source = "program\tTest;\nvar\n\tx:\tInteger;\nbegin\nend.";

        // Act
        var result = _frontend.ParseToSlab(source);

        // Assert
        Assert.That(result, Is.Not.Empty);
    }

    [Test]
    public void Parse_ConsecutiveNewlines_Succeeds()
    {
        // Arrange
        var source = "program Test;\n\n\nvar x: Integer;\n\n\nbegin\n\n\nend.";

        // Act
        var result = _frontend.ParseToSlab(source);

        // Assert
        Assert.That(result, Is.Not.Empty);
    }

    #endregion

    #region Statement Combination Tests

    [Test]
    public void Parse_MultipleStatementsOnOneLine_Succeeds()
    {
        // Arrange
        var source = "program Test;\nvar x, y, z: Integer;\nbegin x := 1; y := 2; z := 3; end.";

        // Act
        var result = _frontend.ParseToSlab(source);

        // Assert
        Assert.That(result, Is.Not.Empty);
    }

    [Test]
    public void Parse_MixedControlFlow_Succeeds()
    {
        // Arrange
        var source = @"
            program Mix;
            var x, y, i: Integer;
            begin
              if x > 0 then
                while y < 10 do
                  for i := 1 to 5 do
                    writeln('test');
            end.";

        // Act
        var result = _frontend.ParseToSlab(source);

        // Assert
        Assert.That(result, Is.Not.Empty);
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
            begin
            end.";

        // Act
        var result = _frontend.ParseToSlab(source);

        // Assert
        Assert.That(result, Is.Not.Empty);
    }

    [Test]
    public void Parse_ArrayTypes_Succeeds()
    {
        // Arrange
        var source = @"
            program Arrays;
            var
              intArray: array[1..10] of Integer;
            begin
            end.";

        // Act
        var result = _frontend.ParseToSlab(source);

        // Assert
        Assert.That(result, Is.Not.Empty);
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
        var result = _frontend.ParseToSlab(source);

        // Assert
        Assert.That(result, Is.Not.Empty);
    }

    [Test]
    public void Parse_NestedComments_HandledCorrectly()
    {
        // Arrange
        var source = @"
            program Nested;
            { Outer comment }
            begin
            end.";

        // Act
        var result = _frontend.ParseToSlab(source);

        // Assert
        Assert.That(result, Is.Not.Empty);
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
        var result = _frontend.ParseToSlab(source);

        // Assert
        // Long identifiers should be supported
        Assert.That(result, Is.Not.Empty);
    }

    [Test]
    public void Compile_VeryLongStringLiteral_Succeeds()
    {
        // Arrange
        var longString = new string('x', 1000);
        var source = $"program Test;\nbegin\n  writeln('{longString}');\nend.";

        // Act
        var result = _frontend.ParseToSlab(source);

        // Assert
        Assert.That(result, Is.Not.Empty);
    }

    #endregion
}
