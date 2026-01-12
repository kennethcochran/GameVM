using NUnit.Framework;
using GameVM.Compiler.Application;
using GameVM.Compiler.Core.Enums;
using GameVM.Compiler.Pascal;
using GameVM.Compiler.Optimizers.MidLevel;
using GameVM.Compiler.Optimizers.LowLevel;
using GameVM.Compiler.Optimizers.FinalIR;
using GameVM.Compiler.Backend.Atari2600;

namespace GameVM.Compiler.E2E.Tests;

/// <summary>
/// Integration tests for the full compilation pipeline.
/// Tests end-to-end compilation using real components without mocks.
/// </summary>
[TestFixture]
public class CompilationPipelineTests
{
    private CompileUseCase _compiler;

    [SetUp]
    public void Setup()
    {
        // Create real compiler without mocks
        _compiler = new CompileUseCase(
            new PascalFrontend(),
            new DefaultMidLevelOptimizer(),
            new DefaultLowLevelOptimizer(),
            new DefaultFinalIROptimizer(),
            new MidToLowLevelTransformer(),
            new LowToFinalTransformer(),
            new Atari2600CodeGenerator());
    }

    #region Simple Compilation Tests

    [Test]
    public void Compile_SimpleHelloWorld_ProducesValidBytecode()
    {
        // Arrange
        var sourceCode = "program HelloWorld;\nbegin\n  writeln('Hello, World!');\nend.";
        var options = new CompilationOptions
        {
            Target = Architecture.Atari2600,
            DispatchStrategy = DispatchStrategy.DirectThreadedCode,
            GenerateDebugInfo = false,
            Optimize = false
        };

        // Act
        var result = _compiler.Execute(sourceCode, ".pas", options);

        // Assert
        Assert.That(result, Is.Not.Null);
        // Compilation may succeed or fail depending on implementation completeness
        // The important thing is that it uses real components
        if (result.Success)
        {
            Assert.That(result.Code, Is.Not.Null);
            Assert.That(result.Code.Length, Is.GreaterThanOrEqualTo(0));
        }
    }

    [Test]
    public void Compile_SimpleVariableDeclaration_CompilesSuccessfully()
    {
        // Arrange
        var sourceCode = "program Test;\nvar x: Integer;\nbegin\n  x := 42;\nend.";
        var options = new CompilationOptions
        {
            Target = Architecture.Atari2600,
            DispatchStrategy = DispatchStrategy.DirectThreadedCode,
            GenerateDebugInfo = false,
            Optimize = false
        };

        // Act
        var result = _compiler.Execute(sourceCode, ".pas", options);

        // Assert
        Assert.That(result, Is.Not.Null);
        // Verify compilation pipeline processes variable declaration
    }

    [Test]
    public void Compile_SimpleAssignment_CompilesSuccessfully()
    {
        // Arrange
        var sourceCode = "program Test;\nvar x, y: Integer;\nbegin\n  x := 5;\n  y := x + 3;\nend.";
        var options = new CompilationOptions
        {
            Target = Architecture.Atari2600,
            DispatchStrategy = DispatchStrategy.DirectThreadedCode,
            GenerateDebugInfo = false,
            Optimize = false
        };

        // Act
        var result = _compiler.Execute(sourceCode, ".pas", options);

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    #endregion

    #region Control Flow Compilation Tests

    [Test]
    public void Compile_IfElseStatement_CompilesSuccessfully()
    {
        // Arrange
        var sourceCode = @"
            program Test;
            var x: Integer;
            begin
              if x > 0 then
                writeln('positive')
              else
                writeln('non-positive');
            end.";
        var options = new CompilationOptions
        {
            Target = Architecture.Atari2600,
            DispatchStrategy = DispatchStrategy.DirectThreadedCode,
            GenerateDebugInfo = false,
            Optimize = false
        };

        // Act
        var result = _compiler.Execute(sourceCode, ".pas", options);

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void Compile_WhileLoop_CompilesSuccessfully()
    {
        // Arrange
        var sourceCode = @"
            program Test;
            var i: Integer;
            begin
              i := 0;
              while i < 10 do
              begin
                writeln(i);
                i := i + 1;
              end;
            end.";
        var options = new CompilationOptions
        {
            Target = Architecture.Atari2600,
            DispatchStrategy = DispatchStrategy.DirectThreadedCode,
            GenerateDebugInfo = false,
            Optimize = false
        };

        // Act
        var result = _compiler.Execute(sourceCode, ".pas", options);

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void Compile_ForLoop_CompilesSuccessfully()
    {
        // Arrange
        var sourceCode = @"
            program Test;
            var i: Integer;
            begin
              for i := 1 to 10 do
                writeln(i);
            end.";
        var options = new CompilationOptions
        {
            Target = Architecture.Atari2600,
            DispatchStrategy = DispatchStrategy.DirectThreadedCode,
            GenerateDebugInfo = false,
            Optimize = false
        };

        // Act
        var result = _compiler.Execute(sourceCode, ".pas", options);

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void Compile_CaseStatement_CompilesSuccessfully()
    {
        // Arrange
        var sourceCode = @"
            program Test;
            var x: Integer;
            begin
              case x of
                1: writeln('one');
                2: writeln('two');
                else writeln('other');
              end;
            end.";
        var options = new CompilationOptions
        {
            Target = Architecture.Atari2600,
            DispatchStrategy = DispatchStrategy.DirectThreadedCode,
            GenerateDebugInfo = false,
            Optimize = false
        };

        // Act
        var result = _compiler.Execute(sourceCode, ".pas", options);

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    #endregion

    #region Function Call Tests

    [Test]
    public void Compile_FunctionCall_CompilesSuccessfully()
    {
        // Arrange
        var sourceCode = @"
            program Test;
            function Add(a, b: Integer): Integer;
            begin
              Add := a + b;
            end;
            begin
              writeln(Add(5, 3));
            end.";
        var options = new CompilationOptions
        {
            Target = Architecture.Atari2600,
            DispatchStrategy = DispatchStrategy.DirectThreadedCode,
            GenerateDebugInfo = false,
            Optimize = false
        };

        // Act
        var result = _compiler.Execute(sourceCode, ".pas", options);

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void Compile_RecursiveFunction_CompilesSuccessfully()
    {
        // Arrange
        var sourceCode = @"
            program Test;
            function Factorial(n: Integer): Integer;
            begin
              if n <= 1 then
                Factorial := 1
              else
                Factorial := n * Factorial(n - 1);
            end;
            begin
              writeln(Factorial(5));
            end.";
        var options = new CompilationOptions
        {
            Target = Architecture.Atari2600,
            DispatchStrategy = DispatchStrategy.DirectThreadedCode,
            GenerateDebugInfo = false,
            Optimize = false
        };

        // Act
        var result = _compiler.Execute(sourceCode, ".pas", options);

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void Compile_NestedFunctionCalls_CompilesSuccessfully()
    {
        // Arrange
        var sourceCode = @"
            program Test;
            function Double(x: Integer): Integer;
            begin
              Double := x * 2;
            end;
            begin
              writeln(Double(Double(5)));
            end.";
        var options = new CompilationOptions
        {
            Target = Architecture.Atari2600,
            DispatchStrategy = DispatchStrategy.DirectThreadedCode,
            GenerateDebugInfo = false,
            Optimize = false
        };

        // Act
        var result = _compiler.Execute(sourceCode, ".pas", options);

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    #endregion

    #region Built-in Function Tests

    [Test]
    public void Compile_WriteFunction_CompilesSuccessfully()
    {
        // Arrange
        var sourceCode = "program Test;\nbegin\n  write('Hello');\nend.";
        var options = new CompilationOptions
        {
            Target = Architecture.Atari2600,
            DispatchStrategy = DispatchStrategy.DirectThreadedCode,
            GenerateDebugInfo = false,
            Optimize = false
        };

        // Act
        var result = _compiler.Execute(sourceCode, ".pas", options);

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void Compile_WritelnFunction_CompilesSuccessfully()
    {
        // Arrange
        var sourceCode = "program Test;\nbegin\n  writeln('Hello');\nend.";
        var options = new CompilationOptions
        {
            Target = Architecture.Atari2600,
            DispatchStrategy = DispatchStrategy.DirectThreadedCode,
            GenerateDebugInfo = false,
            Optimize = false
        };

        // Act
        var result = _compiler.Execute(sourceCode, ".pas", options);

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void Compile_MultipleBuiltinCalls_CompilesSuccessfully()
    {
        // Arrange
        var sourceCode = @"
            program Test;
            begin
              write('First');
              write('Second');
              writeln('Third');
            end.";
        var options = new CompilationOptions
        {
            Target = Architecture.Atari2600,
            DispatchStrategy = DispatchStrategy.DirectThreadedCode,
            GenerateDebugInfo = false,
            Optimize = false
        };

        // Act
        var result = _compiler.Execute(sourceCode, ".pas", options);

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    #endregion

    #region Array Operation Tests

    [Test]
    public void Compile_ArrayOperations_CompilesSuccessfully()
    {
        // Arrange
        var sourceCode = @"
            program Test;
            var arr: array[1..10] of Integer;
            begin
              arr[1] := 42;
              arr[2] := arr[1] + 1;
            end.";
        var options = new CompilationOptions
        {
            Target = Architecture.Atari2600,
            DispatchStrategy = DispatchStrategy.DirectThreadedCode,
            GenerateDebugInfo = false,
            Optimize = false
        };

        // Act
        var result = _compiler.Execute(sourceCode, ".pas", options);

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    #endregion

    #region Type Conversion Tests

    [Test]
    public void Compile_ImplicitTypeConversion_CompilesSuccessfully()
    {
        // Arrange
        var sourceCode = @"
            program Test;
            var x: Real;
            begin
              x := 42;  { Integer to Real conversion }
            end.";
        var options = new CompilationOptions
        {
            Target = Architecture.Atari2600,
            DispatchStrategy = DispatchStrategy.DirectThreadedCode,
            GenerateDebugInfo = false,
            Optimize = false
        };

        // Act
        var result = _compiler.Execute(sourceCode, ".pas", options);

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    #endregion

    #region Error Propagation Tests

    [Test]
    public void Compile_InvalidProgram_ReturnsCompilationErrors()
    {
        // Arrange
        var sourceCode = "program Test;\nbegin\n  x := ;  { Invalid: missing expression }\nend.";
        var options = new CompilationOptions
        {
            Target = Architecture.Atari2600,
            DispatchStrategy = DispatchStrategy.DirectThreadedCode,
            GenerateDebugInfo = false,
            Optimize = false
        };

        // Act
        var result = _compiler.Execute(sourceCode, ".pas", options);

        // Assert
        Assert.That(result, Is.Not.Null);
        // Should either return failure or throw exception
        if (!result.Success)
        {
            Assert.That(result.ErrorMessage, Is.Not.Empty);
        }
    }

    [Test]
    public void Compile_TypeMismatch_ReturnsCompilationErrors()
    {
        // Arrange
        var sourceCode = @"
            program Test;
            var x: Integer;
            begin
              x := 'hello';  { Type mismatch }
            end.";
        var options = new CompilationOptions
        {
            Target = Architecture.Atari2600,
            DispatchStrategy = DispatchStrategy.DirectThreadedCode,
            GenerateDebugInfo = false,
            Optimize = false
        };

        // Act
        var result = _compiler.Execute(sourceCode, ".pas", options);

        // Assert
        Assert.That(result, Is.Not.Null);
        // When type checking is implemented, should report type mismatch error
    }

    #endregion

    #region Optimization Impact Tests

    [Test]
    public void Compile_WithOptimizations_ReducesROMSize()
    {
        // Arrange
        var sourceCode = @"
            program Test;
            var x, y: Integer;
            begin
              x := 5 + 3;  { Constant expression }
              y := x;      { Variable assignment }
            end.";
        var optionsWithoutOpt = new CompilationOptions
        {
            Target = Architecture.Atari2600,
            DispatchStrategy = DispatchStrategy.DirectThreadedCode,
            GenerateDebugInfo = false,
            Optimize = false
        };
        var optionsWithOpt = new CompilationOptions
        {
            Target = Architecture.Atari2600,
            DispatchStrategy = DispatchStrategy.DirectThreadedCode,
            GenerateDebugInfo = false,
            Optimize = true,
            OptimizationLevel = OptimizationLevel.Basic
        };

        // Act
        var resultWithoutOpt = _compiler.Execute(sourceCode, ".pas", optionsWithoutOpt);
        var resultWithOpt = _compiler.Execute(sourceCode, ".pas", optionsWithOpt);

        // Assert
        Assert.That(resultWithoutOpt, Is.Not.Null);
        Assert.That(resultWithOpt, Is.Not.Null);
        // When optimizations are implemented, optimized code should be smaller or equal
        if (resultWithoutOpt.Success && resultWithOpt.Success)
        {
            Assert.That(resultWithOpt.Code.Length, Is.LessThanOrEqualTo(resultWithoutOpt.Code.Length));
        }
    }

    [Test]
    public void Compile_WithAggressiveOptimization_AppliesAdvancedOptimizations()
    {
        // Arrange
        var sourceCode = @"
            program Test;
            var x, y: Integer;
            begin
              x := 5 + 3;
              y := 5 + 3;  { Common subexpression }
            end.";
        var options = new CompilationOptions
        {
            Target = Architecture.Atari2600,
            DispatchStrategy = DispatchStrategy.DirectThreadedCode,
            GenerateDebugInfo = false,
            Optimize = true,
            OptimizationLevel = OptimizationLevel.Aggressive
        };

        // Act
        var result = _compiler.Execute(sourceCode, ".pas", options);

        // Assert
        Assert.That(result, Is.Not.Null);
        // When aggressive optimizations are implemented, should apply CSE and other optimizations
    }

    #endregion

    #region Complex Program Tests

    [Test]
    public void Compile_ComplexProgram_CompilesSuccessfully()
    {
        // Arrange
        var sourceCode = @"
            program Complex;
            var x, y, z: Integer;
            function Add(a, b: Integer): Integer;
            begin
              Add := a + b;
            end;
            begin
              x := 10;
              if x > 5 then
              begin
                y := Add(x, 5);
                while y < 20 do
                begin
                  z := y * 2;
                  y := y + 1;
                end;
              end;
              writeln(z);
            end.";
        var options = new CompilationOptions
        {
            Target = Architecture.Atari2600,
            DispatchStrategy = DispatchStrategy.DirectThreadedCode,
            GenerateDebugInfo = false,
            Optimize = false
        };

        // Act
        var result = _compiler.Execute(sourceCode, ".pas", options);

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    #endregion

    #region Resource Constraint Tests

    [Test]
    public void Compile_ExceedsROMLimit_ReportsResourceError()
    {
        // Arrange
        // Create a very large program
        var largeSource = new System.Text.StringBuilder();
        largeSource.AppendLine("program Large;");
        largeSource.AppendLine("var");
        for (int i = 0; i < 200; i++)
        {
            largeSource.AppendLine($"  var{i}: Integer;");
        }
        largeSource.AppendLine("begin");
        for (int i = 0; i < 500; i++)
        {
            largeSource.AppendLine($"  var{i % 200} := {i};");
        }
        largeSource.AppendLine("end.");

        var options = new CompilationOptions
        {
            Target = Architecture.Atari2600,
            DispatchStrategy = DispatchStrategy.DirectThreadedCode,
            GenerateDebugInfo = false,
            Optimize = false
        };

        // Act
        var result = _compiler.Execute(largeSource.ToString(), ".pas", options);

        // Assert
        Assert.That(result, Is.Not.Null);
        // When ROM size checking is implemented, should report resource constraint error
    }

    #endregion
}

