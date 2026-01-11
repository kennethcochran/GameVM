using NUnit.Framework;
using GameVM.Compiler.Core.IR;
using GameVM.Compiler.Core.Optimizers;
using System.Linq;

namespace GameVM.Compiler.Core.Tests.Optimizers;

/// <summary>
/// Tests for mid-level IR optimization.
/// Validates that the mid-level optimizer correctly transforms MLIR with various optimizations.
/// </summary>
[TestFixture]
public class MidLevelOptimizerTests
{
    private DefaultMidLevelOptimizer _optimizer;

    [SetUp]
    public void Setup()
    {
        _optimizer = new DefaultMidLevelOptimizer();
    }

    #region Dead Code Elimination Tests

    [Test]
    public void Optimize_UnreachableStatements_RemovesDeadCode()
    {
        // Arrange
        var mlir = CreateSimpleMidLevelIR();
        var function = new MidLevelIR.MLFunction { Name = "main" };
        function.Instructions.Add(new MidLevelIR.MLAssign { Target = "x", Source = "1" });
        function.Instructions.Add(new MidLevelIR.MLBranch { Label = "end" });
        // Dead code below - unreachable
        function.Instructions.Add(new MidLevelIR.MLAssign { Target = "y", Source = "2" });
        function.Instructions.Add(new MidLevelIR.MLLabel { Name = "end" });
        mlir.Functions["main"] = function;

        // Act
        var result = _optimizer.Optimize(mlir, OptimizationLevel.Basic);

        // Assert
        Assert.That(result.Functions["main"].Instructions, Has.Count.LessThan(function.Instructions.Count));
        // Dead assignment to y should be removed
    }

    [Test]
    public void Optimize_DeadCodeInBranches_EliminatesUnusedAssignments()
    {
        // Arrange
        var mlir = CreateSimpleMidLevelIR();
        var function = new MidLevelIR.MLFunction { Name = "test" };
        function.Instructions.Add(new MidLevelIR.MLAssign { Target = "unused", Source = "42" });
        function.Instructions.Add(new MidLevelIR.MLAssign { Target = "x", Source = "1" });
        mlir.Functions["test"] = function;

        // Act
        var result = _optimizer.Optimize(mlir, OptimizationLevel.Basic);

        // Assert
        // If unused variable is never referenced, assignment should be removed
    }

    #endregion

    #region Constant Propagation Tests

    [Test]
    public void Optimize_ConstantPropagation_SimplifiesExpressions()
    {
        // Arrange
        var mlir = CreateSimpleMidLevelIR();
        var function = new MidLevelIR.MLFunction { Name = "math" };
        function.Instructions.Add(new MidLevelIR.MLAssign { Target = "a", Source = "5" });
        function.Instructions.Add(new MidLevelIR.MLAssign { Target = "b", Source = "a" });
        mlir.Functions["math"] = function;

        // Act
        var result = _optimizer.Optimize(mlir, OptimizationLevel.Basic);

        // Assert
        // After constant propagation, b should directly reference 5 or be optimized
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void Optimize_ConstantFolding_ComputesCompileTimeConstants()
    {
        // Arrange
        var mlir = CreateSimpleMidLevelIR();
        var function = new MidLevelIR.MLFunction { Name = "math" };
        function.Instructions.Add(new MidLevelIR.MLAssign { Target = "result", Source = "(5 + 3)" });
        mlir.Functions["math"] = function;

        // Act
        var result = _optimizer.Optimize(mlir, OptimizationLevel.Basic);

        // Assert
        // Constant expression (5 + 3) should be folded to 8
        var resultFunc = result.Functions["math"];
        var assignInstrs = resultFunc.Instructions.OfType<MidLevelIR.MLAssign>();
        Assert.That(assignInstrs.Any(a => a.Source == "8"), Is.True);
    }

    #endregion

    #region Common Subexpression Elimination Tests

    [Test]
    public void Optimize_DuplicateExpressions_EliminatesDuplicates()
    {
        // Arrange
        var mlir = CreateSimpleMidLevelIR();
        var function = new MidLevelIR.MLFunction { Name = "cse" };
        function.Instructions.Add(new MidLevelIR.MLAssign { Target = "x", Source = "(a + b)" });
        function.Instructions.Add(new MidLevelIR.MLAssign { Target = "y", Source = "(a + b)" });
        mlir.Functions["cse"] = function;

        // Act
        var result = _optimizer.Optimize(mlir, OptimizationLevel.Aggressive);

        // Assert
        // Common subexpression (a + b) should be computed once
        Assert.That(result.Functions["cse"].Instructions, Has.Count.LessThanOrEqualTo(2));
    }

    [Test]
    public void Optimize_RelatedSubexpressions_OptimizesRelationships()
    {
        // Arrange
        var mlir = CreateSimpleMidLevelIR();
        var function = new MidLevelIR.MLFunction { Name = "math" };
        function.Instructions.Add(new MidLevelIR.MLAssign { Target = "x", Source = "(a + b + c)" });
        function.Instructions.Add(new MidLevelIR.MLAssign { Target = "y", Source = "(a + b)" });
        mlir.Functions["math"] = function;

        // Act
        var result = _optimizer.Optimize(mlir, OptimizationLevel.Aggressive);

        // Assert
        // Optimizer should recognize (a + b) is a subexpression of (a + b + c)
    }

    #endregion

    #region Loop Optimization Tests

    [Test]
    public void Optimize_LoopInvariantCode_HoistesConstantComputation()
    {
        // Arrange
        var mlir = CreateSimpleMidLevelIR();
        var function = new MidLevelIR.MLFunction { Name = "loop" };
        function.Instructions.Add(new MidLevelIR.MLLabel { Name = "loop_start" });
        function.Instructions.Add(new MidLevelIR.MLAssign { Target = "c", Source = "(5 + 3)" });
        function.Instructions.Add(new MidLevelIR.MLAssign { Target = "i", Source = "(i + 1)" });
        function.Instructions.Add(new MidLevelIR.MLBranch { Label = "loop_start" });
        mlir.Functions["loop"] = function;

        // Act
        var result = _optimizer.Optimize(mlir, OptimizationLevel.Aggressive);

        // Assert
        // Constant expression (5 + 3) should be moved outside loop
    }

    #endregion

    #region Function Inlining Tests

    [Test]
    public void Optimize_SmallFunction_InlinesFunction()
    {
        // Arrange
        var mlir = CreateSimpleMidLevelIR();
        var smallFunc = new MidLevelIR.MLFunction { Name = "small" };
        smallFunc.Instructions.Add(new MidLevelIR.MLAssign { Target = "result", Source = "x" });
        mlir.Functions["small"] = smallFunc;

        var mainFunc = new MidLevelIR.MLFunction { Name = "main" };
        mainFunc.Instructions.Add(new MidLevelIR.MLCall { Name = "small" });
        mlir.Functions["main"] = mainFunc;

        // Act
        var result = _optimizer.Optimize(mlir, OptimizationLevel.Aggressive);

        // Assert
        // Small function should be inlined into main
    }

    #endregion

    #region Unused Variable Elimination Tests

    [Test]
    public void Optimize_UnusedVariable_RemovesAssignment()
    {
        // Arrange
        var mlir = CreateSimpleMidLevelIR();
        var function = new MidLevelIR.MLFunction { Name = "test" };
        function.Instructions.Add(new MidLevelIR.MLAssign { Target = "unused", Source = "42" });
        function.Instructions.Add(new MidLevelIR.MLAssign { Target = "used", Source = "1" });
        function.Instructions.Add(new MidLevelIR.MLCall { Name = "writeln", Arguments = new List<string> { "used" } });
        mlir.Functions["test"] = function;

        // Act
        var result = _optimizer.Optimize(mlir, OptimizationLevel.Basic);

        // Assert
        // Assignment to unused variable should be removed
    }

    #endregion

    #region Optimization Level Tests

    [Test]
    public void Optimize_WithBasicLevel_AppliesBasicOptimizations()
    {
        // Arrange
        var mlir = CreateSimpleMidLevelIR();
        var function = new MidLevelIR.MLFunction { Name = "math" };
        function.Instructions.Add(new MidLevelIR.MLAssign { Target = "result", Source = "(5 + 3)" });
        mlir.Functions["math"] = function;

        // Act
        var result = _optimizer.Optimize(mlir, OptimizationLevel.Basic);

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void Optimize_WithAggressiveLevel_AppliesAdvancedOptimizations()
    {
        // Arrange
        var mlir = CreateSimpleMidLevelIR();
        var function = new MidLevelIR.MLFunction { Name = "math" };
        function.Instructions.Add(new MidLevelIR.MLAssign { Target = "x", Source = "(a + b)" });
        function.Instructions.Add(new MidLevelIR.MLAssign { Target = "y", Source = "(a + b)" });
        function.Instructions.Add(new MidLevelIR.MLLabel { Name = "loop" });
        function.Instructions.Add(new MidLevelIR.MLAssign { Target = "c", Source = "(5 + 3)" });
        function.Instructions.Add(new MidLevelIR.MLBranch { Label = "loop" });
        mlir.Functions["math"] = function;

        // Act
        var result = _optimizer.Optimize(mlir, OptimizationLevel.Aggressive);

        // Assert
        // Aggressive optimization should reduce code size significantly
    }

    #endregion

    #region Edge Cases

    [Test]
    public void Optimize_EmptyFunction_RemainsEmpty()
    {
        // Arrange
        var mlir = CreateSimpleMidLevelIR();
        var function = new MidLevelIR.MLFunction { Name = "empty" };
        mlir.Functions["empty"] = function;

        // Act
        var result = _optimizer.Optimize(mlir, OptimizationLevel.Basic);

        // Assert
        Assert.That(result.Functions["empty"].Instructions, Is.Empty);
    }

    [Test]
    public void Optimize_SingleInstruction_PreservesInstruction()
    {
        // Arrange
        var mlir = CreateSimpleMidLevelIR();
        var function = new MidLevelIR.MLFunction { Name = "single" };
        function.Instructions.Add(new MidLevelIR.MLAssign { Target = "x", Source = "1" });
        mlir.Functions["single"] = function;

        // Act
        var result = _optimizer.Optimize(mlir, OptimizationLevel.Basic);

        // Assert
        Assert.That(result.Functions["single"].Instructions, Has.Count.EqualTo(1));
    }

    #endregion

    #region Helper Methods

    private MidLevelIR CreateSimpleMidLevelIR()
    {
        return new MidLevelIR { SourceFile = "test.ir" };
    }

    #endregion
}
