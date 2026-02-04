using NUnit.Framework;
using System.Text.Json;
using Moq;
using Moq.AutoMock;
using System.Runtime.InteropServices;

namespace GameVM.DevTools.Tests;

public class MameInstallerTests
{
    private AssetFinder _assetFinder = null!;
    private AutoMocker _autoMocker = null!;
    private Mock<IConsoleService> _consoleService = null!;
    private Mock<IProcessService> _processService = null!;
    private Mock<IPlatformService> _platformServiceMock = null!;
    private Mock<IFileSystemService> _fileSystemServiceMock = null!;

    [SetUp]
    public void SetUp()
    {
        _autoMocker = new AutoMocker();
        _consoleService = _autoMocker.GetMock<IConsoleService>();
        _processService = _autoMocker.GetMock<IProcessService>();
        _platformServiceMock = _autoMocker.GetMock<IPlatformService>();
        _fileSystemServiceMock = _autoMocker.GetMock<IFileSystemService>();
        
        _assetFinder = new AssetFinder(_platformServiceMock.Object);
        
        // Setup default file system mock behavior
        _fileSystemServiceMock.Setup(x => x.GetBaseDirectory()).Returns("/home/test/project");
        _fileSystemServiceMock.Setup(x => x.DirectoryExists(It.IsAny<string>())).Returns(false);
        _fileSystemServiceMock.Setup(x => x.FileExists(It.IsAny<string>())).Returns(false);
        _fileSystemServiceMock.Setup(x => x.GetFiles(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SearchOption>())).Returns(Array.Empty<string>());
    }

    // RED PHASE: One failing test at a time for InstallAsync - Linux platform with apt-get
    [Test]
    public async Task InstallAsync_OnLinuxWithAptGet_ShouldInstallViaAptGet()
    {
        // Arrange - Test the Linux platform path with apt-get available
        _platformServiceMock.Setup(x => x.IsLinux()).Returns(true);
        _processService.Setup(x => x.GetCommandPath("apt-get")).Returns("/usr/bin/apt-get");
        _processService.Setup(x => x.RunProcessAsync("sudo", "apt-get update && apt-get install -y mame", true, true))
            .ReturnsAsync(true);
        _processService.Setup(x => x.GetCommandPath("mame")).Returns("/usr/bin/mame");

        var installer = new MameInstaller(_consoleService.Object, _processService.Object, _platformServiceMock.Object, _fileSystemServiceMock.Object);

        // Act
        await installer.InstallAsync();

        // Assert - Should install via apt-get
        _consoleService.Verify(x => x.WriteLine("Installing MAME using apt-get (Debian-based Linux)..."), Times.Once);
        _consoleService.Verify(x => x.WriteLine("MAME installed successfully from Debian repositories"), Times.Once);
        _consoleService.Verify(x => x.WriteLine("MAME available at: /usr/bin/mame"), Times.Once);
    }

    // RED PHASE: One failing test at a time for InstallAsync - Linux platform without apt-get
    [Test]
    public async Task InstallAsync_OnLinuxWithoutAptGet_ShouldShowErrorMessage()
    {
        // Arrange - Test the Linux platform path where apt-get is not available
        _platformServiceMock.Setup(x => x.IsLinux()).Returns(true);
        _processService.Setup(x => x.GetCommandPath("apt-get")).Returns((string?)null);

        var installer = new MameInstaller(_consoleService.Object, _processService.Object, _platformServiceMock.Object, _fileSystemServiceMock.Object);

        // Act
        await installer.InstallAsync();

        // Assert - Should show error message for non-Debian systems
        _consoleService.Verify(x => x.WriteLine("Installing MAME using apt-get (Debian-based Linux)..."), Times.Once);
        _consoleService.Verify(x => x.WriteLine("ERROR: apt-get not found. This installer supports Debian-based Linux distributions."), Times.Once);
        _consoleService.Verify(x => x.WriteLine("For other Linux distributions, please install MAME using your package manager:"), Times.Once);
        _consoleService.Verify(x => x.WriteLine("  - Fedora/RHEL: sudo dnf install mame"), Times.Once);
    }

    // RED PHASE: One failing test at a time for InstallAsync - Windows platform with Chocolatey
    [Test]
    public async Task InstallAsync_OnWindowsPlatform_WithChocolatey_ShouldInstallViaChoco()
    {
        // Arrange - Test the Windows platform path with Chocolatey available
        _platformServiceMock.Setup(x => x.IsWindows()).Returns(true);
        _processService.Setup(x => x.GetCommandPath("choco")).Returns("C:\\ProgramData\\chocolatey\\bin\\choco.exe");
        _processService.Setup(x => x.RunProcessAsync("choco", "install mame -y --no-progress", true, true))
            .ReturnsAsync(true);
        _processService.Setup(x => x.GetCommandPath("mame")).Returns("C:\\ProgramData\\chocolatey\\bin\\mame.exe");

        var installer = new MameInstaller(_consoleService.Object, _processService.Object, _platformServiceMock.Object, _fileSystemServiceMock.Object);

        // Act
        await installer.InstallAsync();

        // Assert - Should install via Chocolatey
        _consoleService.Verify(x => x.WriteLine("Installing MAME using Chocolatey..."), Times.Once);
        _consoleService.Verify(x => x.WriteLine("MAME installed successfully via Chocolatey"), Times.Once);
        _consoleService.Verify(x => x.WriteLine("MAME available at: C:\\ProgramData\\chocolatey\\bin\\mame.exe"), Times.Once);
    }

    // RED PHASE: One failing test at a time for InstallAsync - Exception handling
    [Test]
    public async Task InstallAsync_WhenExceptionOccurs_ShouldHandleGracefully()
    {
        // Arrange - Test the exception handling path
        _platformServiceMock.Setup(x => x.IsLinux()).Returns(true);
        _processService.Setup(x => x.GetCommandPath("apt-get")).Returns("/usr/bin/apt-get");
        _processService.Setup(x => x.RunProcessAsync("sudo", "apt-get update && apt-get install -y mame", true, true))
            .ThrowsAsync(new InvalidOperationException("Network error"));

        var installer = new MameInstaller(_consoleService.Object, _processService.Object, _platformServiceMock.Object, _fileSystemServiceMock.Object);

        // Act
        await installer.InstallAsync();

        // Assert - Should handle exception gracefully
        _consoleService.Verify(x => x.WriteLine("Installing MAME using apt-get (Debian-based Linux)..."), Times.Once);
        _consoleService.Verify(x => x.WriteLine("Error during installation: Network error"), Times.Once);
        _consoleService.Verify(x => x.WriteLine("Please ensure you have the necessary permissions and network access."), Times.Once);
    }

    // RED PHASE: One failing test at a time for InstallAsync - macOS platform with Homebrew
    [Test]
    public async Task InstallAsync_OnMacOSPlatform_WithHomebrew_ShouldInstallViaBrew()
    {
        // Arrange - Test the macOS platform path with Homebrew available
        _platformServiceMock.Setup(x => x.IsMacOS()).Returns(true);
        _processService.Setup(x => x.GetCommandPath("brew")).Returns("/opt/homebrew/bin/brew");
        _processService.Setup(x => x.RunProcessAsync("brew", "install mame", true, true))
            .ReturnsAsync(true);
        _processService.Setup(x => x.GetCommandPath("mame")).Returns("/opt/homebrew/bin/mame");

        var installer = new MameInstaller(_consoleService.Object, _processService.Object, _platformServiceMock.Object, _fileSystemServiceMock.Object);

        // Act
        await installer.InstallAsync();

        // Assert - Should install via Homebrew
        _consoleService.Verify(x => x.WriteLine("Installing MAME using Homebrew..."), Times.Once);
        _consoleService.Verify(x => x.WriteLine("MAME installed successfully via Homebrew"), Times.Once);
        _consoleService.Verify(x => x.WriteLine("MAME available at: /opt/homebrew/bin/mame"), Times.Once);
    }

    // RED PHASE: One failing test at a time for InstallAsync - Windows platform without Chocolatey
    [Test]
    public async Task InstallAsync_OnWindowsPlatform_WithoutChocolatey_ShouldShowErrorMessage()
    {
        // Arrange - Test the Windows platform path where Chocolatey is not available
        _platformServiceMock.Setup(x => x.IsWindows()).Returns(true);
        _processService.Setup(x => x.GetCommandPath("choco")).Returns((string?)null);

        var installer = new MameInstaller(_consoleService.Object, _processService.Object, _platformServiceMock.Object, _fileSystemServiceMock.Object);

        // Act
        await installer.InstallAsync();

        // Assert - Should show error message for missing Chocolatey
        _consoleService.Verify(x => x.WriteLine("Installing MAME using Chocolatey..."), Times.Once);
        _consoleService.Verify(x => x.WriteLine("ERROR: Chocolatey not found. Please install Chocolatey first:"), Times.Once);
        _consoleService.Verify(x => x.WriteLine("  Run PowerShell as Administrator and execute:"), Times.Once);
    }

    [Test]
    public void FindSuitableAsset_ShouldReturnWindowsAsset_WhenWindowsPlatform()
    {
        // Arrange
        var json = @"{
            ""assets"": [
                {
                    ""name"": ""mame0258b_64bit.exe"",
                    ""browser_download_url"": ""https://github.com/mamedev/mame/releases/download/mame0258/mame0258b_64bit.exe""
                }
            ]
        }";
        
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        
        // Arrange - Mock Windows platform
        _platformServiceMock.Setup(x => x.IsWindows()).Returns(true);
        _platformServiceMock.Setup(x => x.IsMacOS()).Returns(false);
        _platformServiceMock.Setup(x => x.IsLinux()).Returns(false);
        
        _assetFinder = new AssetFinder(_platformServiceMock.Object);
        
        // Act
        var result = _assetFinder.FindSuitableAsset(root);
        
        // Assert - Should find Windows asset
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Name, Is.EqualTo("mame0258b_64bit.exe"));
    }

    [Test]
    public void FindSuitableAsset_ShouldReturnMacAsset_WhenMacPlatform()
    {
        // Arrange
        var json = @"{
            ""assets"": [
                {
                    ""name"": ""mame0258_macos.zip"",
                    ""browser_download_url"": ""https://github.com/mamedev/mame/releases/download/mame0258/mame0258_macos.zip""
                }
            ]
        }";
        
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        
        // Arrange - Mock Mac platform
        _platformServiceMock.Setup(x => x.IsWindows()).Returns(false);
        _platformServiceMock.Setup(x => x.IsMacOS()).Returns(true);
        _platformServiceMock.Setup(x => x.IsLinux()).Returns(false);
        
        _assetFinder = new AssetFinder(_platformServiceMock.Object);
        
        // Act
        var result = _assetFinder.FindSuitableAsset(root);
        
        // Assert - Should find Mac asset
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Name, Is.EqualTo("mame0258_macos.zip"));
    }

    [Test]
    public void FindSuitableAsset_ShouldReturnNull_WhenNoMatchingAssets()
    {
        // Arrange
        var json = @"{
            ""assets"": [
                {
                    ""name"": ""source.tar.gz"",
                    ""browser_download_url"": ""https://github.com/mamedev/mame/releases/download/mame0258/source.tar.gz""
                }
            ]
        }";
        
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        
        // Act
        var result = _assetFinder.FindSuitableAsset(root);
        
        // Assert
        Assert.That(result, Is.Null);
    }
}
