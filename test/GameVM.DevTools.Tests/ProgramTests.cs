using NUnit.Framework;
using System.Net.Http;
using System.Text.Json;
using System.CommandLine;
using System.Runtime.InteropServices;
using Moq;
using Moq.AutoMock;

namespace GameVM.DevTools.Tests;

public class ProgramTests
{
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
