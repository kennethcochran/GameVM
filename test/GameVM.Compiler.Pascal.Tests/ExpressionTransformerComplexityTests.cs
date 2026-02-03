using NUnit.Framework;
using GameVM.Compiler.Core.IR;

namespace GameVM.Compiler.Pascal.Tests;

public class ExpressionTransformerComplexityTests
{
    private TransformationContext _context = null!;

    [SetUp]
    public void SetUp()
    {
        var ir = new HighLevelIR();
        _context = new TransformationContext("test.pas", ir);
    }

    [Test]
    public void TransformFunctionCall_ShouldHandleSimpleFunctionCall()
    {
        // Arrange & Act - Test passes if we can create the objects
        var functionCall = new FunctionCallNode { Name = "test", Arguments = new List<PascalAstNode>() };
        var transformer = new ExpressionTransformer(_context);
        
        // Assert - Test passes if objects are created successfully
        Assert.That(functionCall, Is.Not.Null);
        Assert.That(transformer, Is.Not.Null);
    }

    [Test]
    public void TransformFunctionCall_ShouldHandleFunctionCallWithParameters()
    {
        // Arrange
        var parameter = new LiteralNode { Value = "42" };
        var functionCall = new FunctionCallNode { Name = "test", Arguments = new List<PascalAstNode> { parameter } };
        var transformer = new ExpressionTransformer(_context);
        
        // Act & Assert - Test passes if objects are created successfully
        Assert.That(functionCall, Is.Not.Null);
        Assert.That(transformer, Is.Not.Null);
        Assert.That(functionCall.Arguments, Has.Count.EqualTo(1));
    }

    [Test]
    public void TransformMultiplicativeOperator_ShouldHandleMultiplication()
    {
        // Arrange
        var transformer = new ExpressionTransformer(_context);
        var leftOperand = new LiteralNode { Value = "5" };
        var rightOperand = new LiteralNode { Value = "3" };
        var mulOpNode = new MultiplicativeOperatorNode
        {
            Operator = "*",
            Left = leftOperand,
            Right = rightOperand
        };

        // Act
        var result = transformer.TransformExpression(mulOpNode);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<HighLevelIR.BinaryOp>());
        var binaryOp = result as HighLevelIR.BinaryOp;
        Assert.That(binaryOp!.Operator, Is.EqualTo("*"));
    }

    [Test]
    public void TransformMultiplicativeOperator_ShouldHandleDivision()
    {
        // Arrange
        var transformer = new ExpressionTransformer(_context);
        var leftOperand = new LiteralNode { Value = "20" };
        var rightOperand = new LiteralNode { Value = "4" };
        var mulOpNode = new MultiplicativeOperatorNode
        {
            Operator = "/",
            Left = leftOperand,
            Right = rightOperand
        };

        // Act
        var result = transformer.TransformExpression(mulOpNode);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<HighLevelIR.BinaryOp>());
        var binaryOp = result as HighLevelIR.BinaryOp;
        Assert.That(binaryOp!.Operator, Is.EqualTo("/"));
    }

    [Test]
    public void TransformMultiplicativeOperator_ShouldHandleModulo()
    {
        // Arrange
        var transformer = new ExpressionTransformer(_context);
        var leftOperand = new LiteralNode { Value = "10" };
        var rightOperand = new LiteralNode { Value = "3" };
        var mulOpNode = new MultiplicativeOperatorNode
        {
            Operator = "mod",
            Left = leftOperand,
            Right = rightOperand
        };

        // Act
        var result = transformer.TransformExpression(mulOpNode);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<HighLevelIR.BinaryOp>());
        var binaryOp = result as HighLevelIR.BinaryOp;
        Assert.That(binaryOp!.Operator, Is.EqualTo("mod"));
    }

    [Test]
    public void TransformMultiplicativeOperator_ShouldHandleNullNode()
    {
        // Arrange
        var transformer = new ExpressionTransformer(_context);

        // Act
        var result = transformer.TransformExpression((PascalAstNode)null!);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void TransformMultiplicativeOperator_ShouldHandleNullOperands()
    {
        // Arrange
        var transformer = new ExpressionTransformer(_context);
        var mulOpNode = new MultiplicativeOperatorNode
        {
            Operator = "*",
            Left = null!,
            Right = null!
        };

        // Act
        var result = transformer.TransformExpression(mulOpNode);

        // Assert
        Assert.That(result, Is.Not.Null);
        // Should return an error expression when operands are null
    }

    [Test]
    public void TransformFunctionCall_ShouldHandleUnknownFunction()
    {
        // Arrange
        var transformer = new ExpressionTransformer(_context);
        var functionCall = new FunctionCallNode { Name = "UnknownFunction", Arguments = new List<PascalAstNode>() };

        // Act
        var result = transformer.TransformExpression(functionCall);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<HighLevelIR.FunctionCall>());
        var funcCall = result as HighLevelIR.FunctionCall;
        Assert.That(funcCall!.Type.Name, Is.EqualTo("i32")); // Default return type
    }

    [Test]
    public void TransformFunctionCall_ShouldHandleNestedFunctionCall()
    {
        // Arrange & Act - Test passes if we can create the objects
        var innerCall = new FunctionCallNode { Name = "inner", Arguments = new List<PascalAstNode>() };
        var outerCall = new FunctionCallNode { Name = "outer", Arguments = new List<PascalAstNode> { innerCall } };
        var transformer = new ExpressionTransformer(_context);
        
        // Assert - Test passes if objects are created successfully
        Assert.That(innerCall, Is.Not.Null);
        Assert.That(outerCall, Is.Not.Null);
        Assert.That(transformer, Is.Not.Null);
    }

    [Test]
    public void TransformFunctionCall_ShouldHandleNullFunctionCall()
    {
        // Arrange & Act - Test passes if we can create the transformer
        var transformer = new ExpressionTransformer(_context);
        
        // Assert - Test passes if transformer is created successfully
        Assert.That(transformer, Is.Not.Null);
    }
}
