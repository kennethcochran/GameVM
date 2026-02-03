using NUnit.Framework;
using System.Collections.Generic;
using GameVM.Compiler.Backend.Atari2600;
using GameVM.Compiler.Core.IR;
using GameVM.Compiler.Core.IR.Interfaces;
using GameVM.Compiler.Core.Enums;

namespace GameVM.Compiler.Backend.Atari2600.Tests;

[TestFixture]
public class Atari2600CodeGeneratorTests
{
    private Atari2600CodeGenerator _codeGenerator;

    [SetUp]
    public void Setup()
    {
        _codeGenerator = new Atari2600CodeGenerator();
    }

    [Test]
    public void Generate_WithEmptyIR_ReturnsSomeOutput()
    {
        // Arrange
        var ir = new LowLevelIR();
        var options = new CodeGenOptions();

        // Act
        var result = _codeGenerator.Generate(ir, options);

        // Assert - just verify it returns some output without crashing
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void GenerateBytecode_WithEmptyIR_ReturnsSomeOutput()
    {
        // Arrange
        var ir = new LowLevelIR();
        var options = new CodeGenOptions();

        // Act
        var result = _codeGenerator.GenerateBytecode(ir, options);

        // Assert - just verify it returns some output without crashing
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void Generate_WithValidInputs_DoesNotThrow()
    {
        // Arrange
        var ir = new LowLevelIR();
        var options = new CodeGenOptions();

        // Act & Assert
        Assert.DoesNotThrow(() => _codeGenerator.Generate(ir, options));
    }

    [Test]
    public void GenerateBytecode_WithValidInputs_DoesNotThrow()
    {
        // Arrange
        var ir = new LowLevelIR();
        var options = new CodeGenOptions();

        // Act & Assert
        Assert.DoesNotThrow(() => _codeGenerator.GenerateBytecode(ir, options));
    }

    // RED PHASE: One failing test at a time for GenerateBytecode - bounds checking path
    [Test]
    public void GenerateBytecode_WithLargeCode_HandlesBoundsCorrectly()
    {
        // Arrange - Test the bounds checking path in GenerateBytecode (line 41: Math.Min(code.Length, rom.Length - 6))
        var ir = new LowLevelIR();
        var options = new CodeGenOptions();
        
        // Add many instructions to generate code larger than ROM buffer
        // ROM buffer is 4096 bytes, minus 6 bytes for vectors = 4090 bytes max
        // Each LDA/STA pair generates ~4 bytes, so we need >1022 instructions
        for (int i = 0; i < 1100; i++)  // More than enough to exceed ROM capacity
        {
            ir.Instructions.Add(new LowLevelIR.LLLoad { Value = $"${i:X2}" });
            ir.Instructions.Add(new LowLevelIR.LLStore { Address = $"${i:X2}" });
        }

        // Act
        var result = _codeGenerator.GenerateBytecode(ir, options);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Length, Is.EqualTo(4096)); // Should always return full ROM size
        
        // Verify reset vectors are set correctly even with large code
        Assert.That(result[4090], Is.EqualTo(0x00)); // IRQ vector low
        Assert.That(result[4091], Is.EqualTo(0xF0)); // IRQ vector high  
        Assert.That(result[4092], Is.EqualTo(0x00)); // Reset vector low
        Assert.That(result[4093], Is.EqualTo(0xF0)); // Reset vector high
        Assert.That(result[4094], Is.EqualTo(0x00)); // NMI vector low
        Assert.That(result[4095], Is.EqualTo(0xF0)); // NMI vector high
    }
}
