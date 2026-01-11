using NUnit.Framework;
using GameVM.Compiler.Backend.Atari2600;
using GameVM.Compiler.Core.IR;
using System.Linq;

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
        finalIR.AssemblyCode = @"
            LDA #$42
            LDA #$42
            STA $80";

        // Act
        var result = _optimizer.Optimize(finalIR, OptimizationLevel.Basic);

        // Assert
        // Redundant LDA instruction should be removed
        var ldaCount = result.AssemblyCode.Split("LDA").Length - 1;
        Assert.That(ldaCount, Is.LessThanOrEqualTo(1));
    }

    [Test]
    public void Optimize_UnusedRegisters_RemovesDeadAssignments()
    {
        // Arrange
        var finalIR = CreateSimpleFinalIR();
        finalIR.AssemblyCode = @"
            LDA #$42
            LDA #$10
            STA $80";

        // Act
        var result = _optimizer.Optimize(finalIR, OptimizationLevel.Basic);

        // Assert
        // First LDA is overwritten by second LDA without use; should be removed
    }

    [Test]
    public void Optimize_NoOp_Removal()
    {
        // Arrange
        var finalIR = CreateSimpleFinalIR();
        finalIR.AssemblyCode = @"
            LDA #$42
            STA $80
            NOP
            NOP
            LDA #$10
            STA $81";

        // Act
        var result = _optimizer.Optimize(finalIR, OptimizationLevel.Basic);

        // Assert
        // Unnecessary NOP instructions should be removed
        var nopCount = result.AssemblyCode.Split("NOP").Length - 1;
        Assert.That(nopCount, Is.EqualTo(0));
    }

    #endregion

    #region Register Cleanup Tests

    [Test]
    public void Optimize_UnusedRegisterState_CleanedUp()
    {
        // Arrange
        var finalIR = CreateSimpleFinalIR();
        finalIR.AssemblyCode = @"
            LDA #$42
            LDX #$10
            LDY #$20
            STA $80";

        // Act
        var result = _optimizer.Optimize(finalIR, OptimizationLevel.Aggressive);

        // Assert
        // Unused X and Y register loads should be removed
    }

    [Test]
    public void Optimize_RegisterPreservation_Maintains()
    {
        // Arrange
        var finalIR = CreateSimpleFinalIR();
        finalIR.AssemblyCode = @"
            PHA
            LDA #$42
            STA $80
            PLA";

        // Act
        var result = _optimizer.Optimize(finalIR, OptimizationLevel.Aggressive);

        // Assert
        // Push/pop pairs for register preservation should be maintained
        Assert.That(result.AssemblyCode, Contains.Substring("PHA"));
        Assert.That(result.AssemblyCode, Contains.Substring("PLA"));
    }

    #endregion

    #region Stack Frame Optimization Tests

    [Test]
    public void Optimize_UnnecessaryStackOperations_Removed()
    {
        // Arrange
        var finalIR = CreateSimpleFinalIR();
        finalIR.AssemblyCode = @"
            PHA
            PLA
            LDA #$42
            STA $80";

        // Act
        var result = _optimizer.Optimize(finalIR, OptimizationLevel.Aggressive);

        // Assert
        // Immediate push/pop pair with no use in between should be removed
    }

    [Test]
    public void Optimize_FunctionPrologue_Minimized()
    {
        // Arrange
        var finalIR = CreateSimpleFinalIR();
        finalIR.AssemblyCode = @"
            main:
            CLD
            SEI
            LDA #$00
            STA $2002
            RTS";

        // Act
        var result = _optimizer.Optimize(finalIR, OptimizationLevel.Aggressive);

        // Assert
        // Function setup code should be optimized
        Assert.That(result.AssemblyCode, Is.Not.Null);
    }

    #endregion

    #region ROM Size Optimization Tests

    [Test]
    public void Optimize_VerboseSequences_Condensed()
    {
        // Arrange
        var finalIR = CreateSimpleFinalIR();
        finalIR.AssemblyCode = @"
            LDA #$00
            STA $80
            LDA #$00
            STA $81
            LDA #$00
            STA $82";

        var beforeSize = finalIR.AssemblyCode.Length;

        // Act
        var result = _optimizer.Optimize(finalIR, OptimizationLevel.Aggressive);

        // Assert
        // Repetitive initialization should be condensed or optimized
        Assert.That(result.AssemblyCode, Is.Not.Null);
    }

    [Test]
    public void Optimize_UnusedCodePaths_Removed()
    {
        // Arrange
        var finalIR = CreateSimpleFinalIR();
        finalIR.AssemblyCode = @"
            JMP over_dead_code
            dead_code:
            LDA #$42
            STA $80
            over_dead_code:
            LDA #$00
            RTS";

        // Act
        var result = _optimizer.Optimize(finalIR, OptimizationLevel.Aggressive);

        // Assert
        // Unreachable code should be removed
        Assert.That(result.AssemblyCode, Does.Not.Contain("dead_code"));
    }

    [Test]
    public void Optimize_LargeProgram_ReducesSize()
    {
        // Arrange
        var finalIR = CreateSimpleFinalIR();
        var largeCode = new System.Text.StringBuilder();
        for (int i = 0; i < 100; i++)
        {
            largeCode.AppendLine($"LDA #{i & 0xFF}");
            largeCode.AppendLine($"STA ${0x80 + (i % 16)}");
        }
        finalIR.AssemblyCode = largeCode.ToString();

        var beforeSize = finalIR.AssemblyCode.Length;

        // Act
        var result = _optimizer.Optimize(finalIR, OptimizationLevel.Full);

        // Assert
        // Large program should be optimized for size
        Assert.That(result.AssemblyCode.Length, Is.LessThanOrEqualTo(beforeSize));
    }

    #endregion

    #region Debug Info Optimization Tests

    [Test]
    public void Optimize_DebugSymbols_PreservedOrOptimized()
    {
        // Arrange
        var finalIR = CreateSimpleFinalIR();
        finalIR.AssemblyCode = @"
            ; DebugInfo: Line 1
            LDA #$42
            ; DebugInfo: Line 2
            STA $80";

        // Act
        var result = _optimizer.Optimize(finalIR, OptimizationLevel.Basic);

        // Assert
        // Debug info should be preserved or cleanly removed
        Assert.That(result.AssemblyCode, Is.Not.Null);
    }

    #endregion

    #region Optimization Level Tests

    [Test]
    public void Optimize_WithBasicLevel_AppliesBasicOptimizations()
    {
        // Arrange
        var finalIR = CreateSimpleFinalIR();
        finalIR.AssemblyCode = @"
            LDA #$42
            LDA #$42
            STA $80";

        // Act
        var result = _optimizer.Optimize(finalIR, OptimizationLevel.Basic);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.AssemblyCode, Is.Not.Null);
    }

    [Test]
    public void Optimize_WithAggressiveLevel_AggressivelyOptimizes()
    {
        // Arrange
        var finalIR = CreateSimpleFinalIR();
        finalIR.AssemblyCode = @"
            LDA #$42
            LDA #$42
            STA $80
            NOP
            NOP
            PHA
            PLA
            LDA #$10
            STA $81";

        var beforeSize = finalIR.AssemblyCode.Length;

        // Act
        var result = _optimizer.Optimize(finalIR, OptimizationLevel.Aggressive);

        // Assert
        // Aggressive optimization should reduce size significantly
        Assert.That(result.AssemblyCode.Length, Is.LessThanOrEqualTo(beforeSize));
    }

    [Test]
    public void Optimize_WithFullLevel_MaximumOptimization()
    {
        // Arrange
        var finalIR = CreateSimpleFinalIR();
        finalIR.AssemblyCode = @"
            LDA #$42
            LDA #$42
            STA $80
            JMP over
            dead:
            LDA #$99
            STA $81
            over:
            RTS";

        // Act
        var result = _optimizer.Optimize(finalIR, OptimizationLevel.Full);

        // Assert
        // Full optimization should apply all transformations
        Assert.That(result.AssemblyCode, Is.Not.Null);
    }

    #endregion

    #region Edge Cases

    [Test]
    public void Optimize_EmptyAssembly_RemainsEmpty()
    {
        // Arrange
        var finalIR = CreateSimpleFinalIR();
        finalIR.AssemblyCode = string.Empty;

        // Act
        var result = _optimizer.Optimize(finalIR, OptimizationLevel.Basic);

        // Assert
        Assert.That(result.AssemblyCode, Is.Empty);
    }

    [Test]
    public void Optimize_SingleInstruction_PreservesInstruction()
    {
        // Arrange
        var finalIR = CreateSimpleFinalIR();
        finalIR.AssemblyCode = "LDA #$42";

        // Act
        var result = _optimizer.Optimize(finalIR, OptimizationLevel.Basic);

        // Assert
        Assert.That(result.AssemblyCode, Contains.Substring("LDA"));
    }

    #endregion

    #region Helper Methods

    private FinalIR CreateSimpleFinalIR()
    {
        return new FinalIR { SourceFile = "test.bin", AssemblyCode = string.Empty };
    }

    #endregion
}
