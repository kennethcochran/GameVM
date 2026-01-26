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
}
