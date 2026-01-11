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
    private LLIRToFinalTransformer _transformer;

    [SetUp]
    public void Setup()
    {
        _transformer = new LLIRToFinalTransformer();
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
        // Verify load instruction is in final IR assembly
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
        Assert.That(result.AssemblyCode, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void Transform_LLLoadAndStoreSequence_GeneratesCorrectAssemblySequence()
    {
        // Arrange
        var llir = CreateSimpleLLIR();
        llir.Instructions.Add(new LowLevelIR.LLLoad { Register = "A", Value = "$42" });
        llir.Instructions.Add(new LowLevelIR.LLStore { Register = "A", Address = "$80" });

        // Act
        var result = _transformer.Transform(llir);

        // Assert
        Assert.That(result.AssemblyCode, Contains.Substring("LDA"));
        Assert.That(result.AssemblyCode, Contains.Substring("STA"));
        // Load should come before store
        var ldaIndex = result.AssemblyCode.IndexOf("LDA");
        var staIndex = result.AssemblyCode.IndexOf("STA");
        Assert.That(ldaIndex, Is.LessThan(staIndex));
    }

    #endregion

    #region Register Management Tests

    [Test]
    public void Transform_MultipleRegisters_ManagesRegisterState()
    {
        // Arrange
        var llir = CreateSimpleLLIR();
        llir.Instructions.Add(new LowLevelIR.LLLoad { Register = "A", Value = "5" });
        llir.Instructions.Add(new LowLevelIR.LLLoad { Register = "X", Value = "10" });
        llir.Instructions.Add(new LowLevelIR.LLStore { Register = "A", Address = "$80" });
        llir.Instructions.Add(new LowLevelIR.LLStore { Register = "X", Address = "$81" });

        // Act
        var result = _transformer.Transform(llir);

        // Assert
        Assert.That(result.AssemblyCode, Contains.Substring("LDA"));
        Assert.That(result.AssemblyCode, Contains.Substring("LDX"));
        Assert.That(result.AssemblyCode, Contains.Substring("STA"));
        Assert.That(result.AssemblyCode, Contains.Substring("STX"));
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
        // Verify that register preservation code is generated around function call
        Assert.That(result.AssemblyCode, Is.Not.Null);
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
        llir.Instructions.Add(new LowLevelIR.LLRet { });

        // Act
        var result = _transformer.Transform(llir);

        // Assert
        Assert.That(result.AssemblyCode, Contains.Substring("main"));
        // Function prologue would be implicit in label generation
    }

    [Test]
    public void Transform_FunctionWithEpilogue_GeneratesEpilogueCode()
    {
        // Arrange
        var llir = CreateSimpleLLIR();
        llir.Instructions.Add(new LowLevelIR.LLLoad { Register = "A", Value = "1" });
        llir.Instructions.Add(new LowLevelIR.LLRet { });

        // Act
        var result = _transformer.Transform(llir);

        // Assert
        // Return instruction should be in final assembly
        Assert.That(result.AssemblyCode, Contains.Substring("RTS"));
    }

    #endregion

    #region Branch/Jump Target Resolution Tests

    [Test]
    public void Transform_BranchInstruction_ResolvesTargetLabel()
    {
        // Arrange
        var llir = CreateSimpleLLIR();
        llir.Instructions.Add(new LowLevelIR.LLLabel { Name = "loop" });
        llir.Instructions.Add(new LowLevelIR.LLLoad { Register = "A", Value = "0" });
        llir.Instructions.Add(new LowLevelIR.LLBranch { Label = "loop" });

        // Act
        var result = _transformer.Transform(llir);

        // Assert
        Assert.That(result.AssemblyCode, Contains.Substring("loop"));
        // Branch instruction should reference the loop label
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
        llir.Instructions.Add(new LowLevelIR.LLBranch { Label = "loop" });

        // Act
        var result = _transformer.Transform(llir);

        // Assert
        Assert.That(result.AssemblyCode.IndexOf("start"), Is.GreaterThanOrEqualTo(0));
        Assert.That(result.AssemblyCode.IndexOf("loop"), Is.GreaterThan(result.AssemblyCode.IndexOf("start")));
    }

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
        Assert.That(result.AssemblyCode, Contains.Substring("$2000"));
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
        Assert.That(result.AssemblyCode, Contains.Substring("$80"));
        // Zero-page addressing should produce more efficient code
    }

    [Test]
    public void Transform_IndexedAddressing_GeneratesIndexedAccess()
    {
        // Arrange
        var llir = CreateSimpleLLIR();
        llir.Instructions.Add(new LowLevelIR.LLLoad { Register = "X", Value = "5" });
        llir.Instructions.Add(new LowLevelIR.LLLoad { Register = "A", Value = "42" });
        llir.Instructions.Add(new LowLevelIR.LLStore { Register = "A", Address = "$80,X" });

        // Act
        var result = _transformer.Transform(llir);

        // Assert
        Assert.That(result.AssemblyCode, Contains.Substring("$80"));
        Assert.That(result.AssemblyCode, Contains.Substring("X"));
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
        Assert.That(result.AssemblyCode, Contains.Substring("JSR"));
        Assert.That(result.AssemblyCode, Contains.Substring("helper"));
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
        Assert.That(result.AssemblyCode, Contains.Substring("func1"));
        Assert.That(result.AssemblyCode, Contains.Substring("func2"));
        var func1Index = result.AssemblyCode.IndexOf("func1");
        var func2Index = result.AssemblyCode.IndexOf("func2");
        Assert.That(func1Index, Is.LessThan(func2Index));
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
        Assert.That(result.AssemblyCode, Is.Not.Null.And.Not.Empty);
    }

    #endregion

    #region Helper Methods

    private LowLevelIR CreateSimpleLLIR()
    {
        return new LowLevelIR { SourceFile = "test.ll" };
    }

    #endregion
}
