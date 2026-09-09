using GameVM.Compiler.Core.IR.Interfaces;
using GameVM.Compiler.Core.Enums;

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
    public void Atari2600CodeGenerator_ReportsNoDpcExtensions()
    {
        // Arrange
        var capabilityProvider = new Atari2600CodeGenerator() as ICapabilityProvider;

        // Act
        var profile = capabilityProvider.GetCapabilityProfile();

        // Assert: DPC accelerator extensions are not implemented/advertised.
        Assert.That(profile.Extensions, Does.Not.Contain("Ext.Math.Fast"));
        Assert.That(profile.Extensions, Does.Not.Contain("Ext.Snd.Polyphonic"));
    }

    [Test]
    public void Atari2600CodeGenerator_ShouldReportCorrectSupportedExtensions()
    {
        // Arrange
        var capabilityProvider = new Atari2600CodeGenerator() as ICapabilityProvider;
        
        // Act
        var extensions = capabilityProvider.GetSupportedExtensions();
        
        // Assert: only the backend marker extension is advertised.
        Assert.That(extensions, Does.Contain("atari2600"));
        Assert.That(extensions, Does.Not.Contain("Ext.Math.Fast"));
    }
}
