using NUnit.Framework;
using System.Text.Json;

namespace GameVM.DevTools.Tests;

public class AssetFinderSeparateFileTests
{
    [Test]
    public void AssetFinder_ShouldBeAccessible_WhenDefinedInSeparateFile()
    {
        // Arrange & Act - AssetFinder should be accessible from its own file
        var assetFinder = new AssetFinder();
        
        // Assert - Test passes if we can create an AssetFinder instance
        Assert.That(assetFinder, Is.Not.Null);
    }

    [Test]
    public void AssetInfo_ShouldBeAccessible_WhenDefinedInSeparateFile()
    {
        // Arrange & Act - AssetInfo should be accessible from its own file
        var assetInfo = new AssetInfo();
        
        // Assert - Test passes if we can create an AssetInfo instance
        Assert.That(assetInfo, Is.Not.Null);
    }

    [Test]
    public void ServiceInterfaces_ShouldBeAccessible_WhenDefinedInSeparateFiles()
    {
        // Arrange & Act - Service interfaces should be accessible from their own files
        var httpClientFactory = new DefaultHttpClientFactory();
        var consoleService = new DefaultConsoleService();
        var processService = new DefaultProcessService();
        
        // Assert - Test passes if we can create instances of all services
        Assert.That(httpClientFactory, Is.Not.Null);
        Assert.That(consoleService, Is.Not.Null);
        Assert.That(processService, Is.Not.Null);
    }
}
