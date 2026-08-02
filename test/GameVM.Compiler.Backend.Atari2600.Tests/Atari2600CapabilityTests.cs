using Xunit;
using GameVM.Compiler.Backend.Atari2600;
using GameVM.Compiler.Core.IR.Interfaces;
using GameVM.Compiler.Core.Enums;

namespace GameVM.Compiler.Backend.Atari2600.Tests;

public class Atari2600CapabilityTests
{
    [Fact]
    public void Atari2600CodeGenerator_ShouldImplementICapabilityProvider()
    {
        // Arrange
        var codeGenerator = new Atari2600CodeGenerator();
        
        // Act & Assert
        Assert.IsAssignableFrom<ICapabilityProvider>(codeGenerator);
    }

    [Fact]
    public void Atari2600CodeGenerator_ShouldReportL1BaseCapability()
    {
        // Arrange
        var capabilityProvider = new Atari2600CodeGenerator() as ICapabilityProvider;
        
        // Act
        var profile = capabilityProvider.GetCapabilityProfile();
        
        // Assert
        Assert.Equal(CapabilityLevel.L1, profile.BaseLevel);
    }

    [Fact]
    public void Atari2600CodeGenerator_ShouldReportDPCExtensionSupport()
    {
        // Arrange
        var capabilityProvider = new Atari2600CodeGenerator() as ICapabilityProvider;
        
        // Act
        var profile = capabilityProvider.GetCapabilityProfile();
        
        // Assert
        Assert.Contains("Ext.Math.Fast", profile.Extensions);
        Assert.Contains("Ext.Snd.Polyphonic", profile.Extensions);
    }

    [Fact]
    public void Atari2600CodeGenerator_ShouldReportCorrectSupportedExtensions()
    {
        // Arrange
        var capabilityProvider = new Atari2600CodeGenerator() as ICapabilityProvider;
        
        // Act
        var extensions = capabilityProvider.GetSupportedExtensions();
        
        // Assert
        Assert.Contains("Ext.Math.Fast", extensions);
        Assert.Contains("Ext.Snd.Polyphonic", extensions);
        Assert.Equal(2, extensions.Count()); // Only DPC-based extensions
    }
}
