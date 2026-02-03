using NUnit.Framework;
using GameVM.Compiler.Backend.Atari2600;

namespace GameVM.Compiler.Backend.Atari2600.Tests;

[TestFixture]
public class M6502EmitterTests
{
    [SetUp]
    public void Setup()
    {
        // M6502Emitter is now static, no instance needed
    }

    #region LDA (Load Accumulator) Tests

    [Test]
    public void Emit_LDA_Immediate_GeneratesCorrectOpcode()
    {
        // Arrange
        var instructions = new List<string> { "LDA #$2A" };

        // Act
        var result = M6502Emitter.Emit(instructions);

        // Assert
        Assert.That(result, Has.Length.EqualTo(2));
        Assert.That(result[0], Is.EqualTo(0xA9)); // LDA immediate opcode
        Assert.That(result[1], Is.EqualTo(0x2A)); // Value
    }

    [Test]
    public void Emit_LDA_Immediate_WithDecimal_GeneratesCorrectOpcode()
    {
        // Arrange
        var instructions = new List<string> { "LDA #42" };

        // Act
        var result = M6502Emitter.Emit(instructions);

        // Assert
        Assert.That(result, Has.Length.EqualTo(2));
        Assert.That(result[0], Is.EqualTo(0xA9));
        Assert.That(result[1], Is.EqualTo(42));
    }

    [Test]
    public void Emit_LDA_Immediate_WithZero_GeneratesCorrectOpcode()
    {
        // Arrange
        var instructions = new List<string> { "LDA #$00" };

        // Act
        var result = M6502Emitter.Emit(instructions);

        // Assert
        Assert.That(result, Has.Length.EqualTo(2));
        Assert.That(result[0], Is.EqualTo(0xA9));
        Assert.That(result[1], Is.EqualTo(0x00));
    }

    [Test]
    public void Emit_LDA_Immediate_WithMaxByte_GeneratesCorrectOpcode()
    {
        // Arrange
        var instructions = new List<string> { "LDA #$FF" };

        // Act
        var result = M6502Emitter.Emit(instructions);

        // Assert
        Assert.That(result, Has.Length.EqualTo(2));
        Assert.That(result[0], Is.EqualTo(0xA9));
        Assert.That(result[1], Is.EqualTo(0xFF));
    }

    #endregion

    #region STA (Store Accumulator) Tests

    [Test]
    public void Emit_STA_ZeroPage_GeneratesCorrectOpcode()
    {
        // Arrange
        var instructions = new List<string> { "STA $80" };

        // Act
        var result = M6502Emitter.Emit(instructions);

        // Assert
        Assert.That(result, Has.Length.EqualTo(2));
        Assert.That(result[0], Is.EqualTo(0x85)); // STA zero page opcode
        Assert.That(result[1], Is.EqualTo(0x80)); // Address
    }

    [Test]
    public void Emit_STA_ZeroPage_TIARegister_GeneratesCorrectOpcode()
    {
        // Arrange
        var instructions = new List<string> { "STA $09" }; // COLUBK

        // Act
        var result = M6502Emitter.Emit(instructions);

        // Assert
        Assert.That(result, Has.Length.EqualTo(2));
        Assert.That(result[0], Is.EqualTo(0x85));
        Assert.That(result[1], Is.EqualTo(0x09));
    }

    [Test]
    public void Emit_STA_Absolute_GeneratesCorrectOpcode()
    {
        // Arrange
        var instructions = new List<string> { "STA $0900" };

        // Act
        var result = M6502Emitter.Emit(instructions);

        // Assert
        Assert.That(result, Has.Length.EqualTo(3));
        Assert.That(result[0], Is.EqualTo(0x8D)); // STA absolute opcode
        Assert.That(result[1], Is.EqualTo(0x00)); // Low byte
        Assert.That(result[2], Is.EqualTo(0x09)); // High byte
    }

    [Test]
    public void Emit_STA_Absolute_HighAddress_GeneratesCorrectOpcode()
    {
        // Arrange
        var instructions = new List<string> { "STA $F000" };

        // Act
        var result = M6502Emitter.Emit(instructions);

        // Assert
        Assert.That(result, Has.Length.EqualTo(3));
        Assert.That(result[0], Is.EqualTo(0x8D));
        Assert.That(result[1], Is.EqualTo(0x00)); // Low byte
        Assert.That(result[2], Is.EqualTo(0xF0)); // High byte
    }

    #endregion

    #region Multiple Instruction Tests

    [Test]
    public void Emit_MultipleInstructions_GeneratesCorrectSequence()
    {
        // Arrange
        var instructions = new List<string>
        {
            "LDA #$2A",
            "STA $80"
        };

        // Act
        var result = M6502Emitter.Emit(instructions);

        // Assert
        Assert.That(result, Has.Length.EqualTo(4));
        Assert.That(result[0], Is.EqualTo(0xA9)); // LDA #$2A
        Assert.That(result[1], Is.EqualTo(0x2A));
        Assert.That(result[2], Is.EqualTo(0x85)); // STA $80
        Assert.That(result[3], Is.EqualTo(0x80));
    }

    [Test]
    public void Emit_ComplexSequence_GeneratesCorrectBytecode()
    {
        // Arrange
        var instructions = new List<string>
        {
            "LDA #$0A",
            "STA $09",
            "LDA #$FF",
            "STA $0900"
        };

        // Act
        var result = M6502Emitter.Emit(instructions);

        // Assert
        Assert.That(result, Has.Length.EqualTo(9));
        // LDA #$0A
        Assert.That(result[0], Is.EqualTo(0xA9));
        Assert.That(result[1], Is.EqualTo(0x0A));
        // STA $09
        Assert.That(result[2], Is.EqualTo(0x85));
        Assert.That(result[3], Is.EqualTo(0x09));
        // LDA #$FF
        Assert.That(result[4], Is.EqualTo(0xA9));
        Assert.That(result[5], Is.EqualTo(0xFF));
        // STA $0900
        Assert.That(result[6], Is.EqualTo(0x8D));
        Assert.That(result[7], Is.EqualTo(0x00));
        Assert.That(result[8], Is.EqualTo(0x09));
    }

    #endregion

    #region Edge Cases and Error Handling

    [Test]
    public void Emit_EmptyInstructionList_ReturnsEmptyArray()
    {
        // Arrange
        var instructions = new List<string>();

        // Act
        var result = M6502Emitter.Emit(instructions);

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void Emit_WhitespaceInstructions_HandlesCorrectly()
    {
        // Arrange
        var instructions = new List<string>
        {
            "  LDA #$2A  ",
            "  STA $80  "
        };

        // Act
        var result = M6502Emitter.Emit(instructions);

        // Assert
        Assert.That(result, Has.Length.EqualTo(4));
        Assert.That(result[0], Is.EqualTo(0xA9));
        Assert.That(result[1], Is.EqualTo(0x2A));
        Assert.That(result[2], Is.EqualTo(0x85));
        Assert.That(result[3], Is.EqualTo(0x80));
    }

    // RED PHASE: One failing test at a time for M6502Emitter.Emit - label handling
    [Test]
    public void Emit_LabelInstruction_ShouldBeSkipped()
    {
        // Arrange - Test the label handling path (line 15: trimmed.EndsWith(':'))
        var instructions = new List<string> { "LOOP:", "LDA #$01", "STA $80" };

        // Act
        var result = M6502Emitter.Emit(instructions);

        // Assert
        // Label should be skipped, only LDA and STA should generate bytes (2 + 2 = 4 bytes)
        Assert.That(result, Has.Length.EqualTo(4));
        // Verify the label didn't add any bytes
        Assert.That(result[0], Is.EqualTo(0xA9)); // LDA opcode
        Assert.That(result[1], Is.EqualTo(0x01)); // LDA value
        Assert.That(result[2], Is.EqualTo(0x85)); // STA zero page opcode
        Assert.That(result[3], Is.EqualTo(0x80)); // STA address
    }

    #endregion
}
