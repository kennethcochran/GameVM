using NUnit.Framework;
using GameVM.Compiler.Pascal;

namespace GameVM.Compiler.Pascal.Tests;

[TestFixture]
public class AstVisitorTests
{
    [Test]
    public void AstVisitor_ShouldBeCreated_WhenDefaultConstructor()
    {
        // Arrange & Act
        var visitor = new AstVisitor();
        
        // Assert
        Assert.That(visitor, Is.Not.Null);
    }

    [Test]
    public void VisitStatement_ShouldThrowNullReference_WhenNullContext()
    {
        // Arrange
        var visitor = new AstVisitor();
        
        // Act & Assert - Method should throw when passed null
        Assert.Throws<System.NullReferenceException>(() => visitor.VisitStatement(null!));
    }

    [Test]
    public void VisitAssignmentStatement_ShouldThrowNullReference_WhenNullContext()
    {
        // Arrange
        var visitor = new AstVisitor();
        
        // Act & Assert - Method should throw when passed null
        Assert.Throws<System.NullReferenceException>(() => visitor.VisitAssignmentStatement(null!));
    }

    [Test]
    public void VisitVariable_ShouldThrowNullReference_WhenNullContext()
    {
        // Arrange
        var visitor = new AstVisitor();
        
        // Act & Assert - Method should throw when passed null
        Assert.Throws<System.NullReferenceException>(() => visitor.VisitVariable(null!));
    }

    [Test]
    public void VisitLabel_ShouldThrowNullReference_WhenNullContext()
    {
        // Arrange
        var visitor = new AstVisitor();
        
        // Act & Assert - Method should throw when passed null
        Assert.Throws<System.NullReferenceException>(() => visitor.VisitLabel(null!));
    }

    [Test]
    public void VisitExpression_ShouldThrowNullReference_WhenNullContext()
    {
        // Arrange
        var visitor = new AstVisitor();
        
        // Act & Assert - Method should throw when passed null
        Assert.Throws<System.NullReferenceException>(() => visitor.VisitExpression(null!));
    }
}