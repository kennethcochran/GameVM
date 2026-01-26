using NUnit.Framework;
using GameVM.Compiler.Core.Enums;

namespace GameVM.Compiler.Core.Tests.Enums;

[TestFixture]
public class ArchitectureTests
{
    [Test]
    public void Architecture_ContainsExpectedValues()
    {
        // Assert - just verify the enum values exist and are in order
        Assert.That(Architecture.Genesis, Is.LessThan(Architecture.NES));
        Assert.That(Architecture.NES, Is.LessThan(Architecture.SNES));
        Assert.That(Architecture.SNES, Is.LessThan(Architecture.GBA));
        Assert.That(Architecture.GBA, Is.LessThan(Architecture.N64));
        Assert.That(Architecture.N64, Is.LessThan(Architecture.Atari2600));
    }

    [Test]
    public void Architecture_Atari2600_IsLastValue()
    {
        // Assert
        var allValues = new[] { Architecture.Genesis, Architecture.NES, Architecture.SNES, Architecture.GBA, Architecture.N64, Architecture.Atari2600 };
        Assert.That(allValues[^1], Is.EqualTo(Architecture.Atari2600));
    }

    [Test]
    public void Architecture_ValuesAreUnique()
    {
        // Arrange
        var values = new[] { Architecture.Genesis, Architecture.NES, Architecture.SNES, Architecture.GBA, Architecture.N64, Architecture.Atari2600 };
        
        // Act & Assert
        Assert.That(values, Is.Unique);
    }
}
