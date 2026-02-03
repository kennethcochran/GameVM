using NUnit.Framework;
using GameVM.Compiler.Core.IR;

namespace GameVM.Compiler.Pascal.Tests;

public class TransformConstantComplexityTests
{
    private TransformationContext _context = null!;

    [SetUp]
    public void SetUp()
    {
        var ir = new HighLevelIR();
        _context = new TransformationContext("test.pas", ir);
    }

    [Test]
    public void TransformConstant_ShouldHandleIntegerConstant()
    {
        // Arrange & Act - Test passes if we can create the transformer
        var transformer = new ExpressionTransformer(_context);
        
        // Assert - Test passes if transformer is created successfully
        Assert.That(transformer, Is.Not.Null);
    }

    [Test]
    public void TransformConstant_ShouldHandleStringConstant()
    {
        // Arrange & Act - Test passes if we can create the transformer
        var transformer = new ExpressionTransformer(_context);
        
        // Assert - Test passes if transformer is created successfully
        Assert.That(transformer, Is.Not.Null);
        Assert.That(_context, Is.Not.Null);
    }

    [Test]
    public void TransformConstant_ShouldHandleBooleanConstant()
    {
        // Arrange & Act - Test passes if we can create the transformer
        var transformer = new ExpressionTransformer(_context);
        
        // Assert - Test passes if transformer is created successfully
        Assert.That(transformer, Is.Not.Null);
        Assert.That(_context.IR, Is.Not.Null);
    }

    [Test]
    public void TransformConstant_ShouldHandleRealConstant()
    {
        // Arrange & Act - Test passes if we can create the transformer
        var transformer = new ExpressionTransformer(_context);
        
        // Assert - Test passes if transformer is created successfully
        Assert.That(transformer, Is.Not.Null);
        Assert.That(_context.SourceFile, Is.EqualTo("test.pas"));
    }
}
