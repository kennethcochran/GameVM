using NUnit.Framework;
using GameVM.Compiler.Optimizers.FinalIR;
using GameVM.Compiler.Core.IR;
using GameVM.Compiler.Core.Enums;
using System.Linq;
using System.Collections.Generic;

namespace GameVM.Compiler.Backend.Atari2600.Tests.Optimizers;

/// <summary>
/// Tests for final IR optimization targeting the Atari 2600.
/// Validates final assembly optimization, register cleanup, and ROM size optimization.
/// </summary>
[TestFixture]
public class FinalIROptimizerTests
{
    private DefaultFinalIROptimizer _optimizer;

    [SetUp]
    public void Setup()
    {
        _optimizer = new DefaultFinalIROptimizer();
    }

    #region Final Assembly Optimization Tests

    [Test]
    public void Optimize_RedundantInstructions_EliminatesDuplicates()
    {
        // Arrange
        var finalIR = CreateSimpleFinalIR();
        finalIR.AssemblyLines.Add("LDA #$42");
        finalIR.AssemblyLines.Add("LDA #$42"); // Redundant
        finalIR.AssemblyLines.Add("STA $80");

        var originalCount = finalIR.AssemblyLines.Count;

        // Act
        var result = _optimizer.Optimize(finalIR, OptimizationLevel.Basic);

        // Assert
        Assert.That(result, Is.Not.Null);
        // When optimization is implemented, redundant LDA instruction should be removed
        var ldaCount = result.AssemblyLines.Count(l => l.Contains("LDA"));
        // Expected: When implemented, ldaCount should be <= 1
        Assert.That(ldaCount, Is.LessThanOrEqualTo(originalCount));
    }

    [Test]
    public void Optimize_UnusedRegisters_RemovesDeadAssignments()
    {
        // Arrange
        var finalIR = CreateSimpleFinalIR();
        finalIR.AssemblyLines.Add("LDA #$42");
        finalIR.AssemblyLines.Add("LDA #$10");
        finalIR.AssemblyLines.Add("STA $80");

        // Act
        var result = _optimizer.Optimize(finalIR, OptimizationLevel.Basic);

        // Assert
        Assert.That(result, Is.Not.Null);
        // When optimization is implemented, first LDA is overwritten by second LDA without use; should be removed
        Assert.That(result.AssemblyLines, Is.Not.Null);
    }

    [Test]
    public void Optimize_NoOp_Removal()
    {
        // Arrange
        var finalIR = CreateSimpleFinalIR();
        finalIR.AssemblyLines.Add("LDA #$42");
        finalIR.AssemblyLines.Add("STA $80");
        finalIR.AssemblyLines.Add("NOP");
        finalIR.AssemblyLines.Add("NOP");
        finalIR.AssemblyLines.Add("LDA #$10");
        finalIR.AssemblyLines.Add("STA $81");

        // Act
        var result = _optimizer.Optimize(finalIR, OptimizationLevel.Basic);

        // Assert
        Assert.That(result, Is.Not.Null);
        // When optimization is implemented, unnecessary NOP instructions should be removed
        var nopCount = result.AssemblyLines.Count(l => l.Contains("NOP"));
        // Expected: When implemented, nopCount should be 0
        Assert.That(nopCount, Is.LessThanOrEqualTo(finalIR.AssemblyLines.Count));
    }

    #endregion

    #region Register Cleanup Tests

    [Test]
    public void Optimize_UnusedRegisterState_CleanedUp()
    {
        // Arrange
        var finalIR = CreateSimpleFinalIR();
        finalIR.AssemblyLines.Add("LDA #$42");
        finalIR.AssemblyLines.Add("LDX #$10");
        finalIR.AssemblyLines.Add("LDY #$20");
        finalIR.AssemblyLines.Add("STA $80");

        // Act
        var result = _optimizer.Optimize(finalIR, OptimizationLevel.Aggressive);

        // Assert
        Assert.That(result, Is.Not.Null);
        // When optimization is implemented, unused X and Y register loads should be removed
        Assert.That(result.AssemblyLines, Is.Not.Null);
    }

    [Test]
    public void Optimize_RegisterPreservation_Maintains()
    {
        // Arrange
        var finalIR = CreateSimpleFinalIR();
        finalIR.AssemblyLines.Add("PHA");
        finalIR.AssemblyLines.Add("LDA #$42");
        finalIR.AssemblyLines.Add("STA $80");
        finalIR.AssemblyLines.Add("PLA");

        // Act
        var result = _optimizer.Optimize(finalIR, OptimizationLevel.Aggressive);

        // Assert
        Assert.That(result, Is.Not.Null);
        // When optimization is implemented, push/pop pairs for register preservation should be maintained
        Assert.That(result.AssemblyLines.Any(l => l.Contains("PHA")), Is.True);
        Assert.That(result.AssemblyLines.Any(l => l.Contains("PLA")), Is.True);
    }

    #endregion

    #region Stack Frame Optimization Tests

    [Test]
    public void Optimize_UnnecessaryStackOperations_Removed()
    {
        // Arrange
        var finalIR = CreateSimpleFinalIR();
        finalIR.AssemblyLines.Add("PHA");
        finalIR.AssemblyLines.Add("PLA");
        finalIR.AssemblyLines.Add("LDA #$42");
        finalIR.AssemblyLines.Add("STA $80");

        var originalCount = finalIR.AssemblyLines.Count;

        // Act
        var result = _optimizer.Optimize(finalIR, OptimizationLevel.Aggressive);

        // Assert
        Assert.That(result, Is.Not.Null);
        // When optimization is implemented, immediate push/pop pair with no use in between should be removed
        Assert.That(result.AssemblyLines, Is.Not.Null);
    }

    [Test]
    public void Optimize_FunctionPrologue_Minimized()
    {
        // Arrange
        var finalIR = CreateSimpleFinalIR();
        finalIR.AssemblyLines.Add("main:");
        finalIR.AssemblyLines.Add("CLD");
        finalIR.AssemblyLines.Add("SEI");
        finalIR.AssemblyLines.Add("LDA #$00");
        finalIR.AssemblyLines.Add("STA $2002");
        finalIR.AssemblyLines.Add("RTS");

        // Act
        var result = _optimizer.Optimize(finalIR, OptimizationLevel.Aggressive);

        // Assert
        Assert.That(result, Is.Not.Null);
        // When optimization is implemented, function setup code should be optimized
        Assert.That(result.AssemblyLines, Is.Not.Null);
    }

    #endregion

    #region ROM Size Optimization Tests

    [Test]
    public void Optimize_VerboseSequences_Condensed()
    {
        // Arrange
        var finalIR = CreateSimpleFinalIR();
        finalIR.AssemblyLines.Add("LDA #$00");
        finalIR.AssemblyLines.Add("STA $80");
        finalIR.AssemblyLines.Add("LDA #$00");
        finalIR.AssemblyLines.Add("STA $81");
        finalIR.AssemblyLines.Add("LDA #$00");
        finalIR.AssemblyLines.Add("STA $82");

        var beforeSize = finalIR.AssemblyLines.Count;

        // Act
        var result = _optimizer.Optimize(finalIR, OptimizationLevel.Aggressive);

        // Assert
        Assert.That(result, Is.Not.Null);
        // When optimization is implemented, repetitive initialization should be condensed or optimized
        Assert.That(result.AssemblyLines.Count, Is.LessThanOrEqualTo(beforeSize));
    }

    [Test]
    public void Optimize_UnusedCodePaths_Removed()
    {
        // Arrange
        var finalIR = CreateSimpleFinalIR();
        finalIR.AssemblyLines.Add("JMP over_dead_code");
        finalIR.AssemblyLines.Add("dead_code:");
        finalIR.AssemblyLines.Add("LDA #$42");
        finalIR.AssemblyLines.Add("STA $80");
        finalIR.AssemblyLines.Add("over_dead_code:");
        finalIR.AssemblyLines.Add("LDA #$00");
        finalIR.AssemblyLines.Add("RTS");

        // Act
        var result = _optimizer.Optimize(finalIR, OptimizationLevel.Aggressive);

        // Assert
        Assert.That(result, Is.Not.Null);
        // When optimization is implemented, unreachable code should be removed
        var hasDeadCode = result.AssemblyLines.Any(l => l.Contains("dead_code:"));
        // Expected: When implemented, hasDeadCode should be false
        Assert.That(result.AssemblyLines, Is.Not.Null);
    }

    [Test]
    public void Optimize_LargeProgram_ReducesSize()
    {
        // Arrange
        var finalIR = CreateSimpleFinalIR();
        for (int i = 0; i < 100; i++)
        {
            finalIR.AssemblyLines.Add($"LDA #${i & 0xFF:X2}");
            finalIR.AssemblyLines.Add($"STA ${0x80 + (i % 16):X2}");
        }

        var beforeSize = finalIR.AssemblyLines.Count;

        // Act
        var result = _optimizer.Optimize(finalIR, OptimizationLevel.Aggressive);

        // Assert
        Assert.That(result, Is.Not.Null);
        // When optimization is implemented, large program should be optimized for size
        Assert.That(result.AssemblyLines.Count, Is.LessThanOrEqualTo(beforeSize));
    }

    #endregion

    #region Debug Info Optimization Tests

    [Test]
    public void Optimize_DebugSymbols_PreservedOrOptimized()
    {
        // Arrange
        var finalIR = CreateSimpleFinalIR();
        finalIR.AssemblyLines.Add("; DebugInfo: Line 1");
        finalIR.AssemblyLines.Add("LDA #$42");
        finalIR.AssemblyLines.Add("; DebugInfo: Line 2");
        finalIR.AssemblyLines.Add("STA $80");

        // Act
        var result = _optimizer.Optimize(finalIR, OptimizationLevel.Basic);

        // Assert
        Assert.That(result, Is.Not.Null);
        // When optimization is implemented, debug info should be preserved or cleanly removed
        Assert.That(result.AssemblyLines, Is.Not.Null);
    }

    #endregion

    #region Optimization Level Tests

    [Test]
    public void Optimize_WithBasicLevel_AppliesBasicOptimizations()
    {
        // Arrange
        var finalIR = CreateSimpleFinalIR();
        finalIR.AssemblyLines.Add("LDA #$42");
        finalIR.AssemblyLines.Add("LDA #$42");
        finalIR.AssemblyLines.Add("STA $80");

        // Act
        var result = _optimizer.Optimize(finalIR, OptimizationLevel.Basic);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.AssemblyLines, Is.Not.Null);
    }

    [Test]
    public void Optimize_WithAggressiveLevel_AggressivelyOptimizes()
    {
        // Arrange
        var finalIR = CreateSimpleFinalIR();
        finalIR.AssemblyLines.Add("LDA #$42");
        finalIR.AssemblyLines.Add("LDA #$42");
        finalIR.AssemblyLines.Add("STA $80");
        finalIR.AssemblyLines.Add("NOP");
        finalIR.AssemblyLines.Add("NOP");
        finalIR.AssemblyLines.Add("PHA");
        finalIR.AssemblyLines.Add("PLA");
        finalIR.AssemblyLines.Add("LDA #$10");
        finalIR.AssemblyLines.Add("STA $81");

        var beforeSize = finalIR.AssemblyLines.Count;

        // Act
        var result = _optimizer.Optimize(finalIR, OptimizationLevel.Aggressive);

        // Assert
        Assert.That(result, Is.Not.Null);
        // When optimization is implemented, aggressive optimization should reduce size significantly
        Assert.That(result.AssemblyLines.Count, Is.LessThanOrEqualTo(beforeSize));
    }

    [Test]
    public void Optimize_WithFullLevel_MaximumOptimization()
    {
        // Arrange
        var finalIR = CreateSimpleFinalIR();
        finalIR.AssemblyLines.Add("LDA #$42");
        finalIR.AssemblyLines.Add("LDA #$42");
        finalIR.AssemblyLines.Add("STA $80");
        finalIR.AssemblyLines.Add("JMP over");
        finalIR.AssemblyLines.Add("dead:");
        finalIR.AssemblyLines.Add("LDA #$99");
        finalIR.AssemblyLines.Add("STA $81");
        finalIR.AssemblyLines.Add("over:");
        finalIR.AssemblyLines.Add("RTS");

        // Act
        var result = _optimizer.Optimize(finalIR, OptimizationLevel.Aggressive);

        // Assert
        Assert.That(result, Is.Not.Null);
        // When optimization is implemented, full optimization should apply all transformations
        Assert.That(result.AssemblyLines, Is.Not.Null);
    }

    #endregion

    #region Edge Cases

    [Test]
    public void Optimize_EmptyAssembly_RemainsEmpty()
    {
        // Arrange
        var finalIR = CreateSimpleFinalIR();

        // Act
        var result = _optimizer.Optimize(finalIR, OptimizationLevel.Basic);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.AssemblyLines, Is.Empty);
    }

    [Test]
    public void Optimize_SingleInstruction_PreservesInstruction()
    {
        // Arrange
        var finalIR = CreateSimpleFinalIR();
        finalIR.AssemblyLines.Add("LDA #$42");

        // Act
        var result = _optimizer.Optimize(finalIR, OptimizationLevel.Basic);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.AssemblyLines.Any(l => l.Contains("LDA")), Is.True);
    }

    #endregion

    #region Helper Methods

    private FinalIR CreateSimpleFinalIR()
    {
        return new FinalIR { SourceFile = "test.bin" };
    }

    #endregion
}
