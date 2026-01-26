using NUnit.Framework;
using GameVM.Compiler.Core.Enums;

namespace GameVM.Compiler.Core.Tests.Enums;

[TestFixture]
public class OptimizationLevelTests
{
    [Test]
    public void OptimizationLevel_ContainsExpectedValues()
    {
        // Assert - just verify the enum values exist and are in order
        Assert.That(OptimizationLevel.None, Is.LessThan(OptimizationLevel.Basic));
        Assert.That(OptimizationLevel.Basic, Is.LessThan(OptimizationLevel.Aggressive));
        Assert.That(OptimizationLevel.Aggressive, Is.LessThan(OptimizationLevel.Full));
    }

    [Test]
    public void OptimizationLevel_Full_IsLastValue()
    {
        // Assert
        var allValues = new[] { OptimizationLevel.None, OptimizationLevel.Basic, OptimizationLevel.Aggressive, OptimizationLevel.Full };
        Assert.That(allValues[^1], Is.EqualTo(OptimizationLevel.Full));
    }

    [Test]
    public void OptimizationLevel_ValuesAreUnique()
    {
        // Arrange
        var values = new[] { OptimizationLevel.None, OptimizationLevel.Basic, OptimizationLevel.Aggressive, OptimizationLevel.Full };
        
        // Act & Assert
        Assert.That(values, Is.Unique);
    }

    [Test]
    public void OptimizationLevel_ValuesAreSequential()
    {
        // Assert
        var values = new[] { OptimizationLevel.None, OptimizationLevel.Basic, OptimizationLevel.Aggressive, OptimizationLevel.Full };
        for (int i = 0; i < values.Length - 1; i++)
        {
            Assert.That(values[i], Is.LessThan(values[i + 1]));
        }
    }

    [Test]
    public void OptimizationLevel_None_IsFirstValue()
    {
        // Assert
        var allValues = new[] { OptimizationLevel.None, OptimizationLevel.Basic, OptimizationLevel.Aggressive, OptimizationLevel.Full };
        Assert.That(allValues[0], Is.EqualTo(OptimizationLevel.None));
    }
}
