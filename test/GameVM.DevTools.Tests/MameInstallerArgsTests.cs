using GameVM.DevTools;
using NUnit.Framework;

namespace GameVM.DevTools.Tests;

[TestFixture]
public class MameInstallerArgsTests
{
    [Test]
    public void BuildMameArguments_WithoutScript_ContainsRomPath()
    {
        var args = MameInstaller.BuildMameArguments("/tmp/test.bin", "");
        Assert.That(args, Does.Contain("a2600"));
        Assert.That(args, Does.Contain("test.bin"));
        Assert.That(args, Does.Not.Contain("autoboot_script"));
    }

    [Test]
    public void BuildMameArguments_WithScript_ContainsScriptPath()
    {
        var args = MameInstaller.BuildMameArguments("/tmp/test.bin", "/tmp/script.txt");
        Assert.That(args, Does.Contain("autoboot_script"));
        Assert.That(args, Does.Contain("script.txt"));
    }
}
