using NUnit.Framework;
using Moq;
using Moq.AutoMock;

namespace GameVM.DevTools.Tests;

public class MameInstallerPlatformTests
{
    private AutoMocker _autoMocker = null!;
    private Mock<IConsoleService> _consoleService = null!;
    private Mock<IProcessService> _processService = null!;
    private Mock<IPlatformService> _platformService = null!;
    private Mock<IFileSystemService> _fileSystemService = null!;

    [SetUp]
    public void SetUp()
    {
        _autoMocker = new AutoMocker();
        _consoleService = _autoMocker.GetMock<IConsoleService>();
        _processService = _autoMocker.GetMock<IProcessService>();
        _platformService = _autoMocker.GetMock<IPlatformService>();
        _fileSystemService = _autoMocker.GetMock<IFileSystemService>();

        _fileSystemService.Setup(x => x.GetBaseDirectory()).Returns("/home/test/project");
    }

    private MameInstaller CreateInstaller() =>
        new(_consoleService.Object, _processService.Object, _platformService.Object, _fileSystemService.Object);

    [Test]
    public async Task TryInstallForPlatformAsync_OnLinux_DispatchesToLinuxInstaller()
    {
        _platformService.Setup(x => x.IsLinux()).Returns(true);
        _processService.Setup(x => x.GetCommandPath("apt-get")).Returns((string?)null);

        var result = await CreateInstaller().TryInstallForPlatformAsync();

        Assert.That(result, Is.False);
        _processService.Verify(x => x.GetCommandPath("apt-get"), Times.Once);
    }

    [Test]
    public async Task TryInstallForPlatformAsync_OnWindows_DispatchesToWindowsInstaller()
    {
        _platformService.Setup(x => x.IsLinux()).Returns(false);
        _platformService.Setup(x => x.IsWindows()).Returns(true);
        _processService.Setup(x => x.GetCommandPath("choco")).Returns((string?)null);

        var result = await CreateInstaller().TryInstallForPlatformAsync();

        Assert.That(result, Is.False);
        _processService.Verify(x => x.GetCommandPath("choco"), Times.Once);
    }

    [Test]
    public async Task TryInstallForPlatformAsync_OnMacOS_DispatchesToMacOSInstaller()
    {
        _platformService.Setup(x => x.IsLinux()).Returns(false);
        _platformService.Setup(x => x.IsWindows()).Returns(false);
        _platformService.Setup(x => x.IsMacOS()).Returns(true);
        _processService.Setup(x => x.GetCommandPath("brew")).Returns((string?)null);

        var result = await CreateInstaller().TryInstallForPlatformAsync();

        Assert.That(result, Is.False);
        _processService.Verify(x => x.GetCommandPath("brew"), Times.Once);
    }

    [Test]
    public async Task TryInstallForPlatformAsync_UnsupportedPlatform_ReturnsNull()
    {
        _platformService.Setup(x => x.IsLinux()).Returns(false);
        _platformService.Setup(x => x.IsWindows()).Returns(false);
        _platformService.Setup(x => x.IsMacOS()).Returns(false);

        var result = await CreateInstaller().TryInstallForPlatformAsync();

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task InstallOnLinuxAsync_AptGetFound_InstallSucceeds()
    {
        _processService.Setup(x => x.GetCommandPath("apt-get")).Returns("/usr/bin/apt-get");
        _processService.Setup(x => x.RunProcessAsync("sudo", "apt-get update && apt-get install -y mame", true, true))
            .ReturnsAsync(true);
        _processService.Setup(x => x.GetCommandPath("mame")).Returns("/usr/bin/mame");

        var result = await CreateInstaller().InstallOnLinuxAsync();

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task InstallOnLinuxAsync_InstallFails_ReturnsFalse()
    {
        _processService.Setup(x => x.GetCommandPath("apt-get")).Returns("/usr/bin/apt-get");
        _processService.Setup(x => x.RunProcessAsync("sudo", "apt-get update && apt-get install -y mame", true, true))
            .ReturnsAsync(false);

        var result = await CreateInstaller().InstallOnLinuxAsync();

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task InstallOnWindowsAsync_ChocoFound_InstallSucceeds()
    {
        _processService.Setup(x => x.GetCommandPath("choco")).Returns("C:\\ProgramData\\chocolatey\\bin\\choco.exe");
        _processService.Setup(x => x.RunProcessAsync("choco", "install mame -y --no-progress", true, true))
            .ReturnsAsync(true);
        _processService.Setup(x => x.GetCommandPath("mame")).Returns("C:\\tools\\mame\\mame.exe");

        var result = await CreateInstaller().InstallOnWindowsAsync();

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task InstallOnMacOSAsync_BrewFound_InstallSucceeds()
    {
        _processService.Setup(x => x.GetCommandPath("brew")).Returns("/opt/homebrew/bin/brew");
        _processService.Setup(x => x.RunProcessAsync("brew", "install mame", true, true))
            .ReturnsAsync(true);
        _processService.Setup(x => x.GetCommandPath("mame")).Returns("/opt/homebrew/bin/mame");

        var result = await CreateInstaller().InstallOnMacOSAsync();

        Assert.That(result, Is.True);
    }
}
