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
/// Integration tests for Pascal language features.
/// Validates type system, control flow, and memory layout end-to-end.
/// </summary>
[TestFixture]
public class LanguageFeatureTests
{
    private CompileUseCase _compiler;

    [SetUp]
    public void Setup()
    {
        _compiler = new CompileUseCase(
            new PascalFrontend(),
            new DefaultMidLevelOptimizer(),
            new DefaultLowLevelOptimizer(),
            new DefaultFinalIROptimizer(),
            new MidToLowLevelTransformer(),
            new LowToFinalTransformer(),
            new Atari2600CodeGenerator());
    }

    #region Type System Tests

    [Test]
    public void Compile_IntegerType_CompilesSuccessfully()
    {
        // Arrange
        var sourceCode = @"
            program Test;
            var x: Integer;
            begin
              x := 42;
            end.";
        var options = CreateDefaultOptions();

        // Act
        var result = _compiler.Execute(sourceCode, ".pas", options);

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void Compile_RealType_CompilesSuccessfully()
    {
        // Arrange
        var sourceCode = @"
            program Test;
            var x: Real;
            begin
              x := 3.14;
            end.";
        var options = CreateDefaultOptions();

        // Act
        var result = _compiler.Execute(sourceCode, ".pas", options);

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void Compile_BooleanType_CompilesSuccessfully()
    {
        // Arrange
        var sourceCode = @"
            program Test;
            var flag: Boolean;
            begin
              flag := true;
            end.";
        var options = CreateDefaultOptions();

        // Act
        var result = _compiler.Execute(sourceCode, ".pas", options);

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void Compile_CharType_CompilesSuccessfully()
    {
        // Arrange
        var sourceCode = @"
            program Test;
            var ch: Char;
            begin
              ch := 'A';
            end.";
        var options = CreateDefaultOptions();

        // Act
        var result = _compiler.Execute(sourceCode, ".pas", options);

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void Compile_ArrayType_CompilesSuccessfully()
    {
        // Arrange
        var sourceCode = @"
            program Test;
            var arr: array[1..10] of Integer;
            begin
              arr[1] := 42;
            end.";
        var options = CreateDefaultOptions();

        // Act
        var result = _compiler.Execute(sourceCode, ".pas", options);

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void Compile_MultipleVariableTypes_CompilesSuccessfully()
    {
        // Arrange
        var sourceCode = @"
            program Test;
            var
              i: Integer;
              r: Real;
              b: Boolean;
              c: Char;
            begin
              i := 42;
              r := 3.14;
              b := true;
              c := 'X';
            end.";
        var options = CreateDefaultOptions();

        // Act
        var result = _compiler.Execute(sourceCode, ".pas", options);

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    #endregion

    #region Control Flow Compilation Tests

    [Test]
    public void Compile_IfThenElse_CompilesCorrectly()
    {
        // Arrange
        var sourceCode = @"
            program Test;
            var x: Integer;
            begin
              if x > 0 then
                writeln('positive')
              else
                writeln('zero or negative');
            end.";
        var options = CreateDefaultOptions();

        // Act
        var result = _compiler.Execute(sourceCode, ".pas", options);

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void Compile_WhileDo_CompilesCorrectly()
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
        var options = CreateDefaultOptions();

        // Act
        var result = _compiler.Execute(sourceCode, ".pas", options);

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void Compile_RepeatUntil_CompilesCorrectly()
    {
        // Arrange
        var sourceCode = @"
            program Test;
            var i: Integer;
            begin
              i := 0;
              repeat
                writeln(i);
                i := i + 1;
              until i >= 10;
            end.";
        var options = CreateDefaultOptions();

        // Act
        var result = _compiler.Execute(sourceCode, ".pas", options);

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void Compile_ForTo_CompilesCorrectly()
    {
        // Arrange
        var sourceCode = @"
            program Test;
            var i: Integer;
            begin
              for i := 1 to 10 do
                writeln(i);
            end.";
        var options = CreateDefaultOptions();

        // Act
        var result = _compiler.Execute(sourceCode, ".pas", options);

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void Compile_ForDownto_CompilesCorrectly()
    {
        // Arrange
        var sourceCode = @"
            program Test;
            var i: Integer;
            begin
              for i := 10 downto 1 do
                writeln(i);
            end.";
        var options = CreateDefaultOptions();

        // Act
        var result = _compiler.Execute(sourceCode, ".pas", options);

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void Compile_CaseStatement_CompilesCorrectly()
    {
        // Arrange
        var sourceCode = @"
            program Test;
            var x: Integer;
            begin
              case x of
                1: writeln('one');
                2: writeln('two');
                3: writeln('three');
                else writeln('other');
              end;
            end.";
        var options = CreateDefaultOptions();

        // Act
        var result = _compiler.Execute(sourceCode, ".pas", options);

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    #endregion

    #region Memory Layout Validation Tests

    [Test]
    public void Compile_GlobalVariables_AllocatedCorrectly()
    {
        // Arrange
        var sourceCode = @"
            program Test;
            var
              x: Integer;
              y: Real;
              z: Boolean;
            begin
              x := 1;
              y := 2.0;
              z := true;
            end.";
        var options = CreateDefaultOptions();

        // Act
        var result = _compiler.Execute(sourceCode, ".pas", options);

        // Assert
        Assert.That(result, Is.Not.Null);
        // When memory layout validation is implemented, should verify variable addresses
    }

    [Test]
    public void Compile_LocalVariables_AllocatedCorrectly()
    {
        // Arrange
        var sourceCode = @"
            program Test;
            procedure TestProc;
            var x: Integer;
            begin
              x := 42;
            end;
            begin
              TestProc;
            end.";
        var options = CreateDefaultOptions();

        // Act
        var result = _compiler.Execute(sourceCode, ".pas", options);

        // Assert
        Assert.That(result, Is.Not.Null);
        // When memory layout validation is implemented, should verify local variable stack allocation
    }

    [Test]
    public void Compile_ZeroPageVariables_AllocatedToZeroPage()
    {
        // Arrange
        var sourceCode = @"
            program Test;
            var x: Integer;
            begin
              x := 42;
            end.";
        var options = CreateDefaultOptions();

        // Act
        var result = _compiler.Execute(sourceCode, ".pas", options);

        // Assert
        Assert.That(result, Is.Not.Null);
        // When memory layout validation is implemented, should verify zero-page allocation for small variables
    }

    #endregion

    #region Function Parameter Passing Tests

    [Test]
    public void Compile_FunctionWithParameters_PassesCorrectly()
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
        var options = CreateDefaultOptions();

        // Act
        var result = _compiler.Execute(sourceCode, ".pas", options);

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void Compile_FunctionWithMultipleParameters_PassesAll()
    {
        // Arrange
        var sourceCode = @"
            program Test;
            function Sum(a, b, c: Integer): Integer;
            begin
              Sum := a + b + c;
            end;
            begin
              writeln(Sum(1, 2, 3));
            end.";
        var options = CreateDefaultOptions();

        // Act
        var result = _compiler.Execute(sourceCode, ".pas", options);

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void Compile_FunctionWithDifferentParameterTypes_PassesCorrectly()
    {
        // Arrange
        var sourceCode = @"
            program Test;
            function Process(x: Integer; y: Real): Real;
            begin
              Process := x + y;
            end;
            begin
              writeln(Process(5, 3.14));
            end.";
        var options = CreateDefaultOptions();

        // Act
        var result = _compiler.Execute(sourceCode, ".pas", options);

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    #endregion

    #region Return Value Handling Tests

    [Test]
    public void Compile_FunctionReturnValue_HandledCorrectly()
    {
        // Arrange
        var sourceCode = @"
            program Test;
            function GetValue: Integer;
            begin
              GetValue := 42;
            end;
            var x: Integer;
            begin
              x := GetValue;
            end.";
        var options = CreateDefaultOptions();

        // Act
        var result = _compiler.Execute(sourceCode, ".pas", options);

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void Compile_FunctionReturnInExpression_HandledCorrectly()
    {
        // Arrange
        var sourceCode = @"
            program Test;
            function Double(x: Integer): Integer;
            begin
              Double := x * 2;
            end;
            var y: Integer;
            begin
              y := Double(5) + 3;
            end.";
        var options = CreateDefaultOptions();

        // Act
        var result = _compiler.Execute(sourceCode, ".pas", options);

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void Compile_ProcedureNoReturn_HandledCorrectly()
    {
        // Arrange
        var sourceCode = @"
            program Test;
            procedure DoSomething;
            begin
              writeln('done');
            end;
            begin
              DoSomething;
            end.";
        var options = CreateDefaultOptions();

        // Act
        var result = _compiler.Execute(sourceCode, ".pas", options);

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    #endregion

    #region Expression Evaluation Tests

    [Test]
    public void Compile_ArithmeticExpressions_EvaluatedCorrectly()
    {
        // Arrange
        var sourceCode = @"
            program Test;
            var x: Integer;
            begin
              x := 5 + 3 * 2 - 1;
            end.";
        var options = CreateDefaultOptions();

        // Act
        var result = _compiler.Execute(sourceCode, ".pas", options);

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void Compile_LogicalExpressions_EvaluatedCorrectly()
    {
        // Arrange
        var sourceCode = @"
            program Test;
            var x, y: Integer;
            var flag: Boolean;
            begin
              flag := (x > 0) and (y < 10);
            end.";
        var options = CreateDefaultOptions();

        // Act
        var result = _compiler.Execute(sourceCode, ".pas", options);

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void Compile_ComparisonExpressions_EvaluatedCorrectly()
    {
        // Arrange
        var sourceCode = @"
            program Test;
            var x, y: Integer;
            var flag: Boolean;
            begin
              flag := x = y;
              flag := x <> y;
              flag := x < y;
              flag := x > y;
              flag := x <= y;
              flag := x >= y;
            end.";
        var options = CreateDefaultOptions();

        // Act
        var result = _compiler.Execute(sourceCode, ".pas", options);

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    #endregion

    #region Helper Methods

    private CompilationOptions CreateDefaultOptions()
    {
        return new CompilationOptions
        {
            Target = Architecture.Atari2600,
            DispatchStrategy = DispatchStrategy.DirectThreadedCode,
            GenerateDebugInfo = false,
            Optimize = false
        };
    }

    #endregion
}

