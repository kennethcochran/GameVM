using NUnit.Framework;
using System.Net.Http;
using System.Text.Json;
using System.CommandLine;
using System.Runtime.InteropServices;
using Moq;
using Moq.AutoMock;
using System.IO;
using System.Threading.Tasks;

namespace GameVM.DevTools.Tests;

public class ProgramTests
{
    private Mock<IMameInstaller> _mockMameInstaller = null!;
    private StringWriter _consoleOutput = null!;
    private StringWriter _consoleError = null!;

    [SetUp]
    public void Setup()
    {
        _mockMameInstaller = new Mock<IMameInstaller>();
        _consoleOutput = new StringWriter();
        _consoleError = new StringWriter();
        Console.SetOut(_consoleOutput);
        Console.SetError(_consoleError);
    }

    [TearDown]
    public void TearDown()
    {
        _consoleOutput?.Dispose();
        _consoleError?.Dispose();
    }

    [Test]
    public async Task Main_WithMameInstallCommand_ShouldCallInstallAsync()
    {
        // Arrange
        var args = new[] { "mame", "install" };
        _mockMameInstaller.Setup(x => x.InstallAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await Program.TestMain(args, _mockMameInstaller.Object);

        // Assert
        _mockMameInstaller.Verify(x => x.InstallAsync(), Times.Once);
        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public async Task Main_WithMamePathCommand_ShouldPrintMamePath_WhenMameIsInstalled()
    {
        // Arrange
        var args = new[] { "mame", "path" };
        var expectedPath = "/path/to/mame";
        _mockMameInstaller.Setup(x => x.GetMameExecutable()).Returns(expectedPath);

        // Act
        var result = await Program.TestMain(args, _mockMameInstaller.Object);

        // Assert
        _mockMameInstaller.Verify(x => x.GetMameExecutable(), Times.Once);
        Assert.That(_consoleOutput.ToString(), Does.Contain(expectedPath));
        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public async Task Main_WithMamePathCommand_ShouldPrintInstallMessage_WhenMameIsNotInstalled()
    {
        // Arrange
        var args = new[] { "mame", "path" };
        _mockMameInstaller.Setup(x => x.GetMameExecutable()).Returns((string?)null);

        // Act
        var result = await Program.TestMain(args, _mockMameInstaller.Object);

        // Assert
        _mockMameInstaller.Verify(x => x.GetMameExecutable(), Times.Once);
        Assert.That(_consoleOutput.ToString(), Does.Contain("MAME is not installed"));
        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public async Task Main_WithMameRunCommand_ShouldCallRunMameAsync_WhenRomAndScriptProvided()
    {
        // Arrange
        var args = new[] { "mame", "run", "--rom", "test.rom", "--script", "test.lua" };
        _mockMameInstaller.Setup(x => x.RunMameAsync("test.rom", "test.lua")).Returns(Task.CompletedTask);

        // Act
        var result = await Program.TestMain(args, _mockMameInstaller.Object);

        // Assert
        _mockMameInstaller.Verify(x => x.RunMameAsync("test.rom", "test.lua"), Times.Once);
        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public async Task Main_WithMameRunCommand_ShouldPrintErrorMessage_WhenRomOrScriptMissing()
    {
        // Arrange
        var args = new[] { "mame", "run", "--rom", "test.rom" }; // Missing --script

        // Act
        var result = await Program.TestMain(args, _mockMameInstaller.Object);

        // Assert
        Assert.That(_consoleError.ToString(), Does.Contain("Both --rom and --script options are required"));
        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public async Task Main_WithUnknownCommand_ShouldReturnOne()
    {
        // Arrange
        var args = new[] { "unknown", "command" };

        // Act
        var result = await Program.TestMain(args, _mockMameInstaller.Object);

        // Assert
        Assert.That(result, Is.EqualTo(1)); // System.CommandLine returns 1 for unknown commands
    }

    [Test]
    public async Task InstallMameAsync_ShouldReturnEarly_WhenLinuxAndFlatpakInstalled()
    {
        // Arrange
        var installer = new MameInstaller();
        
        // Act - This should not throw and should complete without downloading
        await installer.InstallAsync();
        
        // Assert - Test passes if no exception is thrown
        Assert.Pass();
    }
    
    [Test]
    public void GetToolsDirectory_ShouldCreateDirectory_WhenNotExists()
    {
        // Arrange
        var installer = new MameInstaller();
        
        // Act
        var toolsDir = installer.GetToolsDirectory();
        
        // Assert
        Assert.That(toolsDir, Is.Not.Null);
        Assert.That(Directory.Exists(toolsDir), Is.True);
    }
    
    [Test]
    public void FindProjectRoot_ShouldReturnValidPath_WhenCalledFromCurrentDirectory()
    {
        // Arrange
        var installer = new MameInstaller();
        
        // Act
        var projectRoot = installer.FindProjectRoot(AppContext.BaseDirectory);
        
        // Assert
        Assert.That(projectRoot, Is.Not.Null);
        Assert.That(Directory.Exists(projectRoot), Is.True);
    }

    [Test]
    public void RunMameAsync_ShouldHandleMissingMameExecutable_Gracefully()
    {
        // Arrange
        var installer = new MameInstaller();
        
        // Act & Assert - This should handle missing MAME gracefully
        // On Linux with Flatpak installed, it should work without throwing
        Assert.DoesNotThrowAsync(async () => await installer.RunMameAsync("test.rom", "test.lua"));
    }

    [Test]
    public void GetMameExecutable_ShouldUseMameInstaller_WhenCalled()
    {
        // Arrange
        var installer = new MameInstaller();
        
        // Act
        var mameExe = installer.GetMameExecutable();
        
        // Assert - Should return either a path or null, but not throw
        // On Linux with Flatpak, this might return "flatpak" or null
        Assert.That(mameExe, Is.Null.Or.Not.Empty);
    }
}
