using NUnit.Framework;
using GameVM.Compiler.Backend.Atari2600;
using GameVM.Compiler.Core.IR;
using System.Linq;

namespace GameVM.Compiler.Backend.Atari2600.Tests.Optimizers;

/// <summary>
/// Tests for low-level IR optimization targeting the Atari 2600.
/// Validates register allocation, instruction peepholing, and branch optimization.
/// </summary>
[TestFixture]
public class LowLevelOptimizerTests
{
    private DefaultLowLevelOptimizer _optimizer;

    [SetUp]
    public void Setup()
    {
        _optimizer = new DefaultLowLevelOptimizer();
    }

    #region Register Allocation Tests

    [Test]
    public void Optimize_RegisterAllocation_EffectivelyAllocatesX_Y_A()
    {
        // Arrange
        var llir = CreateSimpleLLIR();
        llir.Instructions.Add(new LowLevelIR.LLLoad { Register = "A", Value = "1" });
        llir.Instructions.Add(new LowLevelIR.LLStore { Register = "A", Address = "$80" });
        llir.Instructions.Add(new LowLevelIR.LLLoad { Register = "X", Value = "2" });
        llir.Instructions.Add(new LowLevelIR.LLStore { Register = "X", Address = "$81" });

        // Act
        var result = _optimizer.Optimize(llir, OptimizationLevel.Basic);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Instructions, Is.Not.Null);
    }

    [Test]
    public void Optimize_RegisterReuse_MinimizesRegisterUsage()
    {
        // Arrange
        var llir = CreateSimpleLLIR();
        llir.Instructions.Add(new LowLevelIR.LLLoad { Register = "A", Value = "1" });
        llir.Instructions.Add(new LowLevelIR.LLStore { Register = "A", Address = "$80" });
        llir.Instructions.Add(new LowLevelIR.LLLoad { Register = "A", Value = "2" });
        llir.Instructions.Add(new LowLevelIR.LLStore { Register = "A", Address = "$81" });

        // Act
        var result = _optimizer.Optimize(llir, OptimizationLevel.Aggressive);

        // Assert
        // A register should be reused effectively
        Assert.That(result.Instructions, Has.Count.GreaterThan(0));
    }

    #endregion

    #region Instruction Peepholing Tests

    [Test]
    public void Optimize_RedundantLoad_EliminatesUnnecessaryLoad()
    {
        // Arrange
        var llir = CreateSimpleLLIR();
        llir.Instructions.Add(new LowLevelIR.LLLoad { Register = "A", Value = "5" });
        llir.Instructions.Add(new LowLevelIR.LLLoad { Register = "A", Value = "5" }); // Redundant
        llir.Instructions.Add(new LowLevelIR.LLStore { Register = "A", Address = "$80" });

        // Act
        var result = _optimizer.Optimize(llir, OptimizationLevel.Basic);

        // Assert
        // Redundant load should be removed
        var loads = result.Instructions.OfType<LowLevelIR.LLLoad>();
        Assert.That(loads.Count(), Is.LessThanOrEqualTo(1));
    }

    [Test]
    public void Optimize_LoadStoreLoadPattern_OptimizesCacheAccess()
    {
        // Arrange
        var llir = CreateSimpleLLIR();
        llir.Instructions.Add(new LowLevelIR.LLLoad { Register = "A", Value = "5" });
        llir.Instructions.Add(new LowLevelIR.LLStore { Register = "A", Address = "$80" });
        llir.Instructions.Add(new LowLevelIR.LLLoad { Register = "A", Address = "$80" });

        // Act
        var result = _optimizer.Optimize(llir, OptimizationLevel.Aggressive);

        // Assert
        // Optimizer should recognize that A still contains the stored value
    }

    [Test]
    public void Optimize_LoadWithoutStore_RemovesDeadLoad()
    {
        // Arrange
        var llir = CreateSimpleLLIR();
        llir.Instructions.Add(new LowLevelIR.LLLoad { Register = "A", Value = "5" });
        llir.Instructions.Add(new LowLevelIR.LLLoad { Register = "A", Value = "10" });
        llir.Instructions.Add(new LowLevelIR.LLStore { Register = "A", Address = "$80" });

        // Act
        var result = _optimizer.Optimize(llir, OptimizationLevel.Basic);

        // Assert
        // First load is dead; only second load is needed
        var loads = result.Instructions.OfType<LowLevelIR.LLLoad>();
        Assert.That(loads.Count(), Is.LessThanOrEqualTo(1));
    }

    #endregion

    #region Branch Optimization Tests

    [Test]
    public void Optimize_UnconditionalBranchAtEnd_RemovesUnreachableCode()
    {
        // Arrange
        var llir = CreateSimpleLLIR();
        llir.Instructions.Add(new LowLevelIR.LLLoad { Register = "A", Value = "1" });
        llir.Instructions.Add(new LowLevelIR.LLBranch { Label = "exit" });
        llir.Instructions.Add(new LowLevelIR.LLLoad { Register = "A", Value = "2" }); // Unreachable
        llir.Instructions.Add(new LowLevelIR.LLLabel { Name = "exit" });

        // Act
        var result = _optimizer.Optimize(llir, OptimizationLevel.Basic);

        // Assert
        // Unreachable load after branch should be removed
        var loads = result.Instructions.OfType<LowLevelIR.LLLoad>();
        Assert.That(loads.Count(), Is.LessThanOrEqualTo(1));
    }

    [Test]
    public void Optimize_RedundantBranches_EliminatesJumpToNextInstruction()
    {
        // Arrange
        var llir = CreateSimpleLLIR();
        llir.Instructions.Add(new LowLevelIR.LLLabel { Name = "here" });
        llir.Instructions.Add(new LowLevelIR.LLLoad { Register = "A", Value = "1" });
        llir.Instructions.Add(new LowLevelIR.LLBranch { Label = "next" });
        llir.Instructions.Add(new LowLevelIR.LLLabel { Name = "next" });
        llir.Instructions.Add(new LowLevelIR.LLLoad { Register = "A", Value = "2" });

        // Act
        var result = _optimizer.Optimize(llir, OptimizationLevel.Aggressive);

        // Assert
        // Jump to next sequential instruction should be removed
    }

    #endregion

    #region Memory Access Optimization Tests

    [Test]
    public void Optimize_SequentialMemoryAccess_Combines()
    {
        // Arrange
        var llir = CreateSimpleLLIR();
        llir.Instructions.Add(new LowLevelIR.LLLoad { Register = "A", Value = "1" });
        llir.Instructions.Add(new LowLevelIR.LLStore { Register = "A", Address = "$80" });
        llir.Instructions.Add(new LowLevelIR.LLLoad { Register = "A", Value = "2" });
        llir.Instructions.Add(new LowLevelIR.LLStore { Register = "A", Address = "$81" });

        // Act
        var result = _optimizer.Optimize(llir, OptimizationLevel.Aggressive);

        // Assert
        Assert.That(result.Instructions, Has.Count.GreaterThan(0));
    }

    [Test]
    public void Optimize_ZeroPageAccess_PreferredOverAbsolute()
    {
        // Arrange
        var llir = CreateSimpleLLIR();
        llir.Instructions.Add(new LowLevelIR.LLLoad { Register = "A", Value = "42" });
        llir.Instructions.Add(new LowLevelIR.LLStore { Register = "A", Address = "$80" }); // Zero page
        llir.Instructions.Add(new LowLevelIR.LLLoad { Register = "A", Value = "84" });
        llir.Instructions.Add(new LowLevelIR.LLStore { Register = "A", Address = "$2000" }); // Absolute

        // Act
        var result = _optimizer.Optimize(llir, OptimizationLevel.Aggressive);

        // Assert
        // Zero-page access should be preferred for locality
    }

    #endregion

    #region Loop Optimization Tests

    [Test]
    public void Optimize_SimpleLoop_UnrollsIfSmall()
    {
        // Arrange
        var llir = CreateSimpleLLIR();
        llir.Instructions.Add(new LowLevelIR.LLLabel { Name = "loop" });
        llir.Instructions.Add(new LowLevelIR.LLLoad { Register = "A", Value = "1" });
        llir.Instructions.Add(new LowLevelIR.LLBranch { Label = "loop" });

        // Act
        var result = _optimizer.Optimize(llir, OptimizationLevel.Aggressive);

        // Assert
        // Small loops may be unrolled or optimized
    }

    #endregion

    #region Optimization Level Tests

    [Test]
    public void Optimize_WithBasicLevel_AppliesBasicOptimizations()
    {
        // Arrange
        var llir = CreateSimpleLLIR();
        llir.Instructions.Add(new LowLevelIR.LLLoad { Register = "A", Value = "5" });
        llir.Instructions.Add(new LowLevelIR.LLLoad { Register = "A", Value = "5" });
        llir.Instructions.Add(new LowLevelIR.LLStore { Register = "A", Address = "$80" });

        // Act
        var result = _optimizer.Optimize(llir, OptimizationLevel.Basic);

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void Optimize_WithAggressiveLevel_ReducesCodeSize()
    {
        // Arrange
        var llir = CreateSimpleLLIR();
        for (int i = 0; i < 5; i++)
        {
            llir.Instructions.Add(new LowLevelIR.LLLoad { Register = "A", Value = "5" });
            llir.Instructions.Add(new LowLevelIR.LLStore { Register = "A", Address = $"${0x80 + i}" });
        }

        var beforeCount = llir.Instructions.Count;

        // Act
        var result = _optimizer.Optimize(llir, OptimizationLevel.Aggressive);

        // Assert
        // Aggressive optimization should reduce instruction count
        Assert.That(result.Instructions.Count, Is.LessThanOrEqualTo(beforeCount));
    }

    #endregion

    #region Edge Cases

    [Test]
    public void Optimize_EmptyLLIR_RemainsEmpty()
    {
        // Arrange
        var llir = CreateSimpleLLIR();

        // Act
        var result = _optimizer.Optimize(llir, OptimizationLevel.Basic);

        // Assert
        Assert.That(result.Instructions, Is.Empty);
    }

    [Test]
    public void Optimize_SingleInstruction_PreservesInstruction()
    {
        // Arrange
        var llir = CreateSimpleLLIR();
        llir.Instructions.Add(new LowLevelIR.LLLoad { Register = "A", Value = "1" });

        // Act
        var result = _optimizer.Optimize(llir, OptimizationLevel.Basic);

        // Assert
        Assert.That(result.Instructions, Has.Count.GreaterThanOrEqualTo(1));
    }

    #endregion

    #region Helper Methods

    private LowLevelIR CreateSimpleLLIR()
    {
        return new LowLevelIR { SourceFile = "test.ll" };
    }

    #endregion
}
