using NUnit.Framework;
using GameVM.Compiler.Core.Enums;

namespace GameVM.Compiler.Core.Tests.Enums;

[TestFixture]
public class DispatchStrategyTests
{
    [Test]
    public void DispatchStrategy_ContainsExpectedValues()
    {
        // Assert - just verify the enum values exist and are in order
        Assert.That(DispatchStrategy.DirectThreadedCode, Is.LessThan(DispatchStrategy.TokenThreadedCode));
        Assert.That(DispatchStrategy.TokenThreadedCode, Is.LessThan(DispatchStrategy.SubroutineThreadedCode));
        Assert.That(DispatchStrategy.SubroutineThreadedCode, Is.LessThan(DispatchStrategy.NativeCode));
    }

    [Test]
    public void DispatchStrategy_NativeCode_IsLastValue()
    {
        // Assert
        var allValues = new[] { DispatchStrategy.DirectThreadedCode, DispatchStrategy.TokenThreadedCode, DispatchStrategy.SubroutineThreadedCode, DispatchStrategy.NativeCode };
        Assert.That(allValues[^1], Is.EqualTo(DispatchStrategy.NativeCode));
    }

    [Test]
    public void DispatchStrategy_ValuesAreUnique()
    {
        // Arrange
        var values = new[] { DispatchStrategy.DirectThreadedCode, DispatchStrategy.TokenThreadedCode, DispatchStrategy.SubroutineThreadedCode, DispatchStrategy.NativeCode };
        
        // Act & Assert
        Assert.That(values, Is.Unique);
    }

    [Test]
    public void DispatchStrategy_ThreadedCodeStrategies_AreSequential()
    {
        // Assert
        var threadedStrategies = new[] { DispatchStrategy.DirectThreadedCode, DispatchStrategy.TokenThreadedCode, DispatchStrategy.SubroutineThreadedCode };
        for (int i = 0; i < threadedStrategies.Length - 1; i++)
        {
            Assert.That(threadedStrategies[i], Is.LessThan(threadedStrategies[i + 1]));
        }
    }
}
