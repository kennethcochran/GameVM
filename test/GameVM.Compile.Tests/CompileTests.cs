using NUnit.Framework;
using System.IO;
using System.CommandLine;
using Moq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using GameVM.Compiler.Application;
using GameVM.Compiler.Application.Services;
using GameVM.Compiler.Core.IR.Interfaces;
using GameVM.Compiler.Core.IR;
using GameVM.Compiler.Optimizers.MidLevel;
using GameVM.Compiler.Optimizers.LowLevel;
using GameVM.Compiler.Core.Interfaces;
using GameVM.Compiler.Pascal;
using GameVM.Compiler.Backend.Atari2600;

namespace GameVM.Compile.Tests;

[TestFixture]
public class CompileTests
{
    private StringWriter _consoleOutput = null!;

    [SetUp]
    public void SetUp()
    {
        _consoleOutput = new StringWriter();
        Console.SetOut(_consoleOutput);
    }

    [TearDown]
    public void TearDown()
    {
        _consoleOutput?.Dispose();
        Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
    }

    // RED PHASE: Test for extracted validation method
    [Test]
    public void ValidateArguments_WithMissingInput_ShouldReturnFalse()
    {
        // Arrange
        var input = "";
        var output = "test.bin";

        // Act
        var result = GameVM.Compile.Program.ValidateArguments(input, output);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void ValidateArguments_WithMissingOutput_ShouldReturnFalse()
    {
        // Arrange
        var input = "test.pas";
        var output = "";

        // Act
        var result = GameVM.Compile.Program.ValidateArguments(input, output);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void ValidateArguments_WithBothArguments_ShouldReturnTrue()
    {
        // Arrange
        var input = "test.pas";
        var output = "test.bin";

        // Act
        var result = GameVM.Compile.Program.ValidateArguments(input, output);

        // Assert
        Assert.That(result, Is.True);
    }

    // Original integration tests (simplified)
    [Test]
    public void Main_WithMissingInputArgument_ShouldShowErrorMessage()
    {
        // Arrange - Test the validation path (lines 57-61)
        var args = new[] { "--output", "test.bin" }; // Missing --input

        // Act - Call the Main method
        GameVM.Compile.Program.Main(args);

        // Assert - Should show error message for missing input
        var output = _consoleOutput.ToString();
        Assert.That(output, Does.Contain("Error: Both --input and --output are required."));
    }

    [Test]
    public void Main_WithMissingOutputArgument_ShouldShowErrorMessage()
    {
        // Arrange - Test the validation path (lines 57-61)
        var args = new[] { "--input", "test.pas" }; // Missing --output

        // Act - Call the Main method
        GameVM.Compile.Program.Main(args);

        // Assert - Should show error message for missing output
        var output = _consoleOutput.ToString();
        Assert.That(output, Does.Contain("Error: Both --input and --output are required."));
    }

    [Test]
    public void TestPlaceholder()
    {
        Assert.Pass("Compile tests placeholder");
    }
}
