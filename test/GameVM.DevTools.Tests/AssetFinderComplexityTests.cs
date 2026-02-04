using NUnit.Framework;
using System.Text.Json;
using Moq;

namespace GameVM.DevTools.Tests;

public class AssetFinderComplexityTests
{
    private AssetFinder _assetFinder = null!;
    private Mock<IPlatformService> _platformServiceMock = null!;

    [SetUp]
    public void SetUp()
    {
        _platformServiceMock = new Mock<IPlatformService>();
        _assetFinder = new AssetFinder(_platformServiceMock.Object);
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
        
        // Arrange - Mock Windows platform
        _platformServiceMock.Setup(x => x.IsWindows()).Returns(true);
        _platformServiceMock.Setup(x => x.IsMacOS()).Returns(false);
        _platformServiceMock.Setup(x => x.IsLinux()).Returns(false);
        
        _assetFinder = new AssetFinder(_platformServiceMock.Object);
        
        // Act
        var result = _assetFinder.FindSuitableAsset(root);
        
        // Assert - Should select Windows asset
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Name, Is.EqualTo("mame0258b_64bit.exe"));
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
