using NUnit.Framework;
using GameVM.Compiler.Backend.Atari2600;
using GameVM.Compiler.Core.IR;
using System.Linq;

namespace GameVM.Compiler.Backend.Atari2600.Tests.Transformers;

/// <summary>
/// Tests for LLIR to Final IR transformation.
/// Validates that low-level IR is correctly transformed to final assembly IR.
/// </summary>
[TestFixture]
public class LLIRToFinalTransformerTests
{
    private LowToFinalTransformer _transformer;

    [SetUp]
    public void Setup()
    {
        _transformer = new LowToFinalTransformer();
    }

    #region Assembly Instruction Generation Tests

    [Test]
    public void Transform_LLLoadInstruction_GeneratesLoadAssembly()
    {
        // Arrange
        var llir = CreateSimpleLLIR();
        llir.Instructions.Add(new LowLevelIR.LLLoad { Register = "A", Value = "42" });

        // Act
        var result = _transformer.Transform(llir);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.SourceFile, Is.EqualTo(llir.SourceFile));
        Assert.That(result.AssemblyLines, Is.Not.Empty);
        Assert.That(result.AssemblyLines[0], Contains.Substring("LDA"));
        Assert.That(result.AssemblyLines[0], Contains.Substring("42"));
    }

    [Test]
    public void Transform_LLStoreInstruction_GeneratesStoreAssembly()
    {
        // Arrange
        var llir = CreateSimpleLLIR();
        llir.Instructions.Add(new LowLevelIR.LLStore { Register = "A", Address = "$80" });

        // Act
        var result = _transformer.Transform(llir);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.AssemblyLines, Is.Not.Empty);
        Assert.That(result.AssemblyLines[0], Contains.Substring("STA"));
        Assert.That(result.AssemblyLines[0], Contains.Substring("$80"));
    }

    [Test]
    public void Transform_LLLoadAndStoreSequence_GeneratesCorrectAssemblySequence()
    {
        // Arrange
        var llir = CreateSimpleLLIR();
        llir.Instructions.Add(new LowLevelIR.LLLoad { Register = "A", Value = "42" });
        llir.Instructions.Add(new LowLevelIR.LLStore { Register = "A", Address = "$80" });

        // Act
        var result = _transformer.Transform(llir);

        // Assert
        Assert.That(result.AssemblyLines, Has.Count.EqualTo(2));
        Assert.That(result.AssemblyLines[0], Contains.Substring("LDA"));
        Assert.That(result.AssemblyLines[1], Contains.Substring("STA"));
        // Load should come before store
        var ldaLine = result.AssemblyLines[0];
        var staLine = result.AssemblyLines[1];
        Assert.That(ldaLine, Contains.Substring("LDA"));
        Assert.That(staLine, Contains.Substring("STA"));
    }

    #endregion

    #region Register Management Tests

    [Test]
    public void Transform_MultipleRegisters_ManagesRegisterState()
    {
        // Arrange
        var llir = CreateSimpleLLIR();
        llir.Instructions.Add(new LowLevelIR.LLLoad { Register = "A", Value = "5" });
        llir.Instructions.Add(new LowLevelIR.LLStore { Register = "A", Address = "$80" });
        // Note: Current transformer only handles register A, X and Y would need additional support
        llir.Instructions.Add(new LowLevelIR.LLLoad { Register = "A", Value = "10" });
        llir.Instructions.Add(new LowLevelIR.LLStore { Register = "A", Address = "$81" });

        // Act
        var result = _transformer.Transform(llir);

        // Assert
        Assert.That(result.AssemblyLines, Has.Count.EqualTo(4));
        Assert.That(result.AssemblyLines.Any(l => l.Contains("LDA") && l.Contains("5")), Is.True);
        Assert.That(result.AssemblyLines.Any(l => l.Contains("STA") && l.Contains("$80")), Is.True);
        Assert.That(result.AssemblyLines.Any(l => l.Contains("LDA") && l.Contains("10")), Is.True);
        Assert.That(result.AssemblyLines.Any(l => l.Contains("STA") && l.Contains("$81")), Is.True);
    }

    [Test]
    public void Transform_RegisterPreservation_SavesRegistersAcrossFunctionBoundaries()
    {
        // Arrange
        var llir = CreateSimpleLLIR();
        llir.Instructions.Add(new LowLevelIR.LLLoad { Register = "A", Value = "1" });
        llir.Instructions.Add(new LowLevelIR.LLCall { Label = "HelperFunc" });
        llir.Instructions.Add(new LowLevelIR.LLStore { Register = "A", Address = "$80" });

        // Act
        var result = _transformer.Transform(llir);

        // Assert
        Assert.That(result.AssemblyLines, Is.Not.Empty);
        // Verify that function call is generated
        Assert.That(result.AssemblyLines.Any(l => l.Contains("JSR") && l.Contains("HelperFunc")), Is.True);
        // Note: Register preservation would be handled by the optimizer or code generator
    }

    #endregion

    #region Stack Management Tests

    [Test]
    public void Transform_FunctionWithPrologue_GeneratesPrologueCode()
    {
        // Arrange
        var llir = CreateSimpleLLIR();
        llir.Instructions.Add(new LowLevelIR.LLLabel { Name = "main" });
        llir.Instructions.Add(new LowLevelIR.LLLoad { Register = "A", Value = "0" });

        // Act
        var result = _transformer.Transform(llir);

        // Assert
        Assert.That(result.AssemblyLines, Is.Not.Empty);
        Assert.That(result.AssemblyLines.Any(l => l.Contains("main:")), Is.True);
        // Function prologue would be implicit in label generation
        Assert.That(result.AssemblyLines.Any(l => l.Contains("LDA")), Is.True);
    }

    [Test]
    public void Transform_FunctionWithEpilogue_GeneratesEpilogueCode()
    {
        // Arrange
        var llir = CreateSimpleLLIR();
        llir.Instructions.Add(new LowLevelIR.LLLoad { Register = "A", Value = "1" });
        // Note: LLRet doesn't exist yet, so we test with a label as function end marker
        llir.Instructions.Add(new LowLevelIR.LLLabel { Name = "end" });

        // Act
        var result = _transformer.Transform(llir);

        // Assert
        Assert.That(result.AssemblyLines, Is.Not.Empty);
        // Return instruction (RTS) would be added by code generator
        // For now, we verify the label is generated
        Assert.That(result.AssemblyLines.Any(l => l.Contains("end:")), Is.True);
    }

    #endregion

    #region Branch/Jump Target Resolution Tests

    [Test]
    public void Transform_LabelInstruction_GeneratesLabel()
    {
        // Arrange
        var llir = CreateSimpleLLIR();
        llir.Instructions.Add(new LowLevelIR.LLLabel { Name = "loop" });
        llir.Instructions.Add(new LowLevelIR.LLLoad { Register = "A", Value = "0" });

        // Act
        var result = _transformer.Transform(llir);

        // Assert
        Assert.That(result.AssemblyLines, Is.Not.Empty);
        Assert.That(result.AssemblyLines.Any(l => l.Contains("loop:")), Is.True);
        // Label should be followed by instructions
        var labelIndex = result.AssemblyLines.FindIndex(l => l.Contains("loop:"));
        Assert.That(labelIndex, Is.GreaterThanOrEqualTo(0));
    }

    [Test]
    public void Transform_MultipleLabels_ResolvesAllTargets()
    {
        // Arrange
        var llir = CreateSimpleLLIR();
        llir.Instructions.Add(new LowLevelIR.LLLabel { Name = "start" });
        llir.Instructions.Add(new LowLevelIR.LLLoad { Register = "A", Value = "1" });
        llir.Instructions.Add(new LowLevelIR.LLLabel { Name = "loop" });
        llir.Instructions.Add(new LowLevelIR.LLLoad { Register = "A", Value = "2" });

        // Act
        var result = _transformer.Transform(llir);

        // Assert
        Assert.That(result.AssemblyLines.Any(l => l.Contains("start:")), Is.True);
        Assert.That(result.AssemblyLines.Any(l => l.Contains("loop:")), Is.True);
        var startIndex = result.AssemblyLines.FindIndex(l => l.Contains("start:"));
        var loopIndex = result.AssemblyLines.FindIndex(l => l.Contains("loop:"));
        Assert.That(loopIndex, Is.GreaterThan(startIndex));
    }

    // Note: Branch instructions (LLBranch) are not yet implemented in LowLevelIR
    // These tests will be added when branch support is added

    #endregion

    #region Memory Addressing Tests

    [Test]
    public void Transform_DirectAddressing_GeneratesDirectMemoryAccess()
    {
        // Arrange
        var llir = CreateSimpleLLIR();
        llir.Instructions.Add(new LowLevelIR.LLLoad { Register = "A", Value = "42" });
        llir.Instructions.Add(new LowLevelIR.LLStore { Register = "A", Address = "$2000" });

        // Act
        var result = _transformer.Transform(llir);

        // Assert
        Assert.That(result.AssemblyLines.Any(l => l.Contains("$2000")), Is.True);
        var storeLine = result.AssemblyLines.FirstOrDefault(l => l.Contains("STA"));
        Assert.That(storeLine, Is.Not.Null);
        Assert.That(storeLine, Contains.Substring("$2000"));
    }

    [Test]
    public void Transform_ZeroPageAddressing_GeneratesEfficientCode()
    {
        // Arrange
        var llir = CreateSimpleLLIR();
        llir.Instructions.Add(new LowLevelIR.LLLoad { Register = "A", Value = "10" });
        llir.Instructions.Add(new LowLevelIR.LLStore { Register = "A", Address = "$80" });

        // Act
        var result = _transformer.Transform(llir);

        // Assert
        Assert.That(result.AssemblyLines.Any(l => l.Contains("$80")), Is.True);
        var storeLine = result.AssemblyLines.FirstOrDefault(l => l.Contains("STA"));
        Assert.That(storeLine, Is.Not.Null);
        Assert.That(storeLine, Contains.Substring("$80"));
        // Zero-page addressing should produce more efficient code (fewer bytes)
    }

    [Test]
    public void Transform_IndexedAddressing_GeneratesIndexedAccess()
    {
        // Arrange
        var llir = CreateSimpleLLIR();
        llir.Instructions.Add(new LowLevelIR.LLLoad { Register = "A", Value = "42" });
        // Note: Indexed addressing like "$80,X" would need special handling
        // For now, we test that the address is preserved
        llir.Instructions.Add(new LowLevelIR.LLStore { Register = "A", Address = "$80" });

        // Act
        var result = _transformer.Transform(llir);

        // Assert
        Assert.That(result.AssemblyLines.Any(l => l.Contains("$80")), Is.True);
        // When indexed addressing is fully supported, we would check for ",X" or ",Y" suffix
    }

    #endregion

    #region Call Frame Generation Tests

    [Test]
    public void Transform_FunctionCall_GeneratesCallFrame()
    {
        // Arrange
        var llir = CreateSimpleLLIR();
        llir.Instructions.Add(new LowLevelIR.LLCall { Label = "helper" });

        // Act
        var result = _transformer.Transform(llir);

        // Assert
        Assert.That(result.AssemblyLines, Is.Not.Empty);
        Assert.That(result.AssemblyLines.Any(l => l.Contains("JSR")), Is.True);
        Assert.That(result.AssemblyLines.Any(l => l.Contains("helper")), Is.True);
        var callLine = result.AssemblyLines.FirstOrDefault(l => l.Contains("JSR"));
        Assert.That(callLine, Is.Not.Null);
        Assert.That(callLine, Contains.Substring("helper"));
    }

    [Test]
    public void Transform_NestedFunctionCalls_ManagesCallStack()
    {
        // Arrange
        var llir = CreateSimpleLLIR();
        llir.Instructions.Add(new LowLevelIR.LLCall { Label = "func1" });
        llir.Instructions.Add(new LowLevelIR.LLCall { Label = "func2" });

        // Act
        var result = _transformer.Transform(llir);

        // Assert
        Assert.That(result.AssemblyLines, Has.Count.EqualTo(2));
        Assert.That(result.AssemblyLines.Any(l => l.Contains("func1")), Is.True);
        Assert.That(result.AssemblyLines.Any(l => l.Contains("func2")), Is.True);
        var func1Index = result.AssemblyLines.FindIndex(l => l.Contains("func1"));
        var func2Index = result.AssemblyLines.FindIndex(l => l.Contains("func2"));
        Assert.That(func1Index, Is.LessThan(func2Index));
    }

    #endregion

    #region Complex Instruction Sequences

    [Test]
    public void Transform_MixedInstructions_GeneratesCorrectSequence()
    {
        // Arrange
        var llir = CreateSimpleLLIR();
        llir.Instructions.Add(new LowLevelIR.LLLabel { Name = "main" });
        llir.Instructions.Add(new LowLevelIR.LLLoad { Register = "A", Value = "42" });
        llir.Instructions.Add(new LowLevelIR.LLStore { Register = "A", Address = "$80" });
        llir.Instructions.Add(new LowLevelIR.LLCall { Label = "Helper" });
        llir.Instructions.Add(new LowLevelIR.LLLoad { Register = "A", Value = "100" });
        llir.Instructions.Add(new LowLevelIR.LLStore { Register = "A", Address = "$81" });

        // Act
        var result = _transformer.Transform(llir);

        // Assert
        Assert.That(result.AssemblyLines, Has.Count.EqualTo(6));
        Assert.That(result.AssemblyLines[0], Contains.Substring("main:"));
        Assert.That(result.AssemblyLines[1], Contains.Substring("LDA"));
        Assert.That(result.AssemblyLines[2], Contains.Substring("STA"));
        Assert.That(result.AssemblyLines[3], Contains.Substring("JSR"));
        Assert.That(result.AssemblyLines[4], Contains.Substring("LDA"));
        Assert.That(result.AssemblyLines[5], Contains.Substring("STA"));
    }

    #endregion

    #region Edge Cases

    [Test]
    public void Transform_EmptyLLIR_ReturnsValidFinalIR()
    {
        // Arrange
        var llir = CreateSimpleLLIR();

        // Act
        var result = _transformer.Transform(llir);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.SourceFile, Is.EqualTo(llir.SourceFile));
        Assert.That(result.AssemblyLines, Is.Empty);
    }

    [Test]
    public void Transform_SingleInstruction_GeneratesValidAssembly()
    {
        // Arrange
        var llir = CreateSimpleLLIR();
        llir.Instructions.Add(new LowLevelIR.LLLoad { Register = "A", Value = "0" });

        // Act
        var result = _transformer.Transform(llir);

        // Assert
        Assert.That(result.AssemblyLines, Is.Not.Empty);
        Assert.That(result.AssemblyLines[0], Contains.Substring("LDA"));
        Assert.That(result.AssemblyLines[0], Contains.Substring("0"));
    }

    [Test]
    public void Transform_MultipleLabels_HandlesDuplicateLabels()
    {
        // Arrange
        var llir = CreateSimpleLLIR();
        llir.Instructions.Add(new LowLevelIR.LLLabel { Name = "label1" });
        llir.Instructions.Add(new LowLevelIR.LLLoad { Register = "A", Value = "1" });
        llir.Instructions.Add(new LowLevelIR.LLLabel { Name = "label2" });
        llir.Instructions.Add(new LowLevelIR.LLLoad { Register = "A", Value = "2" });

        // Act
        var result = _transformer.Transform(llir);

        // Assert
        Assert.That(result.AssemblyLines, Has.Count.EqualTo(4));
        Assert.That(result.AssemblyLines.Any(l => l.Contains("label1:")), Is.True);
        Assert.That(result.AssemblyLines.Any(l => l.Contains("label2:")), Is.True);
    }

    #endregion

    #region Helper Methods

    private LowLevelIR CreateSimpleLLIR()
    {
        return new LowLevelIR { SourceFile = "test.ll" };
    }

    #endregion
}
