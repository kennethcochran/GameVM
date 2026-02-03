using NUnit.Framework;
using System.Text.Json;
using System.Runtime.InteropServices;

namespace GameVM.DevTools.Tests;

public class AssetFinderComplexityTests
{
    private AssetFinder _assetFinder = null!;

    [SetUp]
    public void SetUp()
    {
        _assetFinder = new AssetFinder();
    }

    [Test]
    public void FindSuitableAsset_ShouldHandleWindowsAssets_WithSeparateLogic()
    {
        // This test will help us extract Windows-specific logic
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
        
        // Act
        var result = _assetFinder.FindSuitableAsset(root);
        
        // Assert - Platform-specific behavior
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Name, Is.EqualTo("mame0258b_64bit.exe"));
        }
        else
        {
            Assert.That(result, Is.Null);
        }
    }

    [Test]
    public void FindSuitableAsset_ShouldHandleMacAssets_WithSeparateLogic()
    {
        // This test will help us extract Mac-specific logic
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
        
        // Act
        var result = _assetFinder.FindSuitableAsset(root);
        
        // Assert - Platform-specific behavior
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Name, Is.EqualTo("mame0258_macos.zip"));
        }
        else
        {
            Assert.That(result, Is.Null);
        }
    }

    [Test]
    public void FindSuitableAsset_ShouldHandleMultipleAssets_WithPrioritization()
    {
        // This test will help us extract asset selection logic
        var json = @"{
            ""assets"": [
                {
                    ""name"": ""mame0258b_64bit.exe"",
                    ""browser_download_url"": ""https://github.com/mamedev/mame/releases/download/mame0258/mame0258b_64bit.exe""
                },
                {
                    ""name"": ""mame0258_macos.zip"",
                    ""browser_download_url"": ""https://github.com/mamedev/mame/releases/download/mame0258/mame0258_macos.zip""
                },
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
        
        // Assert - Should select appropriate asset for current platform
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Name, Is.EqualTo("mame0258b_64bit.exe"));
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Name, Is.EqualTo("mame0258_macos.zip"));
        }
        else
        {
            Assert.That(result, Is.Null);
        }
    }

    [Test]
    public void FindSuitableAsset_ShouldHandleEmptyAssets_Gracefully()
    {
        // This test will help us extract error handling logic
        var json = @"{
            ""assets"": []
        }";
        
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        
        // Act
        var result = _assetFinder.FindSuitableAsset(root);
        
        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void FindSuitableAsset_ShouldHandleMissingAssets_Gracefully()
    {
        // This test will help us extract error handling logic
        var json = @"{
            ""other"": ""data""
        }";
        
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        
        // Act
        var result = _assetFinder.FindSuitableAsset(root);
        
        // Assert
        Assert.That(result, Is.Null);
    }
}
