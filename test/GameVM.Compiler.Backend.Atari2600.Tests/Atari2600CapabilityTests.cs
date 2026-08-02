using NUnit.Framework;
using GameVM.Compiler.Backend.Atari2600;
using GameVM.Compiler.Core.IR.Interfaces;
using GameVM.Compiler.Core.Enums;
using System.Collections.Generic;

namespace GameVM.Compiler.Backend.Atari2600.Tests;

public class Atari2600CapabilityTests
{
    [Test]
    public void Atari2600CodeGenerator_ShouldImplementICapabilityProvider()
    {
        // Arrange
        var codeGenerator = new Atari2600CodeGenerator();
        
        // Act & Assert
        Assert.That(codeGenerator, Is.InstanceOf<ICapabilityProvider>());
    }

    [Test]
    public void Atari2600CodeGenerator_ShouldReportL1BaseCapability()
    {
        // Arrange
        var capabilityProvider = new Atari2600CodeGenerator() as ICapabilityProvider;
        
        // Act
        var profile = capabilityProvider.GetCapabilityProfile();
        
        // Assert
        Assert.That(profile.BaseLevel, Is.EqualTo(CapabilityLevel.L1));
    }

    [Test]
    public void Atari2600CodeGenerator_ShouldReportDPCExtensionSupport()
    {
        // Arrange
        var capabilityProvider = new Atari2600CodeGenerator() as ICapabilityProvider;
        
        // Act
        var profile = capabilityProvider.GetCapabilityProfile();
        
        // Assert
        Assert.That(profile.Extensions, Does.Contain("Ext.Math.Fast"));
        Assert.That(profile.Extensions, Does.Contain("Ext.Snd.Polyphonic"));
    }

    [Test]
    public void Atari2600CodeGenerator_ShouldReportCorrectSupportedExtensions()
    {
        // Arrange
        var capabilityProvider = new Atari2600CodeGenerator() as ICapabilityProvider;
        
        // Act
        var extensions = capabilityProvider.GetSupportedExtensions();
        
        // Assert
        Assert.That(extensions, Does.Contain("Ext.Math.Fast"));
        Assert.That(extensions, Does.Contain("Ext.Snd.Polyphonic"));
        Assert.That(extensions.Count, Is.EqualTo(2)); // Only DPC-based extensions
    }
}
