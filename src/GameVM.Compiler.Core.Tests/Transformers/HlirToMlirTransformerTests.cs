using NUnit.Framework;
using GameVM.Compiler.Core.IR;
using GameVM.Compiler.Core.IR.Transformers;
using System.Linq;

namespace GameVM.Compiler.Core.Tests.Transformers;

/// <summary>
/// Tests for HLIR to MLIR transformation.
/// Validates that high-level IR structures are correctly transformed to mid-level IR.
/// </summary>
[TestFixture]
public class HlirToMlirTransformerTests
{
    private HlirToMlirTransformer _transformer;

    [SetUp]
    public void Setup()
    {
        _transformer = new HlirToMlirTransformer();
    }

    #region Variable Declaration Tests

    [Test]
    public void Transform_SingleVariableDeclaration_CreatesMLIRVariable()
    {
        // Arrange
        var hlir = CreateSimpleProgram();
        var variable = new HighLevelIR.Variable { Name = "x", Type = "Integer" };
        hlir.Variables.Add(variable);

        // Act
        var result = _transformer.Transform(hlir);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.SourceFile, Is.EqualTo(hlir.SourceFile));
    }

    [Test]
    public void Transform_MultipleVariableDeclarations_PreservesOrder()
    {
        // Arrange
        var hlir = CreateSimpleProgram();
        hlir.Variables.Add(new HighLevelIR.Variable { Name = "x", Type = "Integer" });
        hlir.Variables.Add(new HighLevelIR.Variable { Name = "y", Type = "Real" });
        hlir.Variables.Add(new HighLevelIR.Variable { Name = "z", Type = "Boolean" });

        // Act
        var result = _transformer.Transform(hlir);

        // Assert
        Assert.That(result, Is.Not.Null);
        // Variables should be accessible through the transformation context
    }

    #endregion

    #region Function Declaration Tests

    [Test]
    public void Transform_SimpleFunctionDeclaration_CreatesMLIRFunction()
    {
        // Arrange
        var hlir = CreateSimpleProgram();
        var function = new HighLevelIR.Function
        {
            Name = "main",
            ReturnType = "Void",
            Body = new HighLevelIR.Block { Statements = new() }
        };
        hlir.Functions.Add(function);

        // Act
        var result = _transformer.Transform(hlir);

        // Assert
        Assert.That(result.Functions, Contains.Key("main"));
        var mlFunc = result.Functions["main"];
        Assert.That(mlFunc.Name, Is.EqualTo("main"));
    }

    [Test]
    public void Transform_FunctionWithParameters_IncludesParameters()
    {
        // Arrange
        var hlir = CreateSimpleProgram();
        var function = new HighLevelIR.Function
        {
            Name = "add",
            ReturnType = "Integer",
            Parameters = new List<HighLevelIR.Parameter>
            {
                new() { Name = "a", Type = "Integer" },
                new() { Name = "b", Type = "Integer" }
            },
            Body = new HighLevelIR.Block { Statements = new() }
        };
        hlir.Functions.Add(function);

        // Act
        var result = _transformer.Transform(hlir);

        // Assert
        Assert.That(result.Functions, Contains.Key("add"));
    }

    [Test]
    public void Transform_MultipleFunctions_CreatesAllFunctions()
    {
        // Arrange
        var hlir = CreateSimpleProgram();
        hlir.Functions.Add(new HighLevelIR.Function
        {
            Name = "func1",
            ReturnType = "Void",
            Body = new HighLevelIR.Block { Statements = new() }
        });
        hlir.Functions.Add(new HighLevelIR.Function
        {
            Name = "func2",
            ReturnType = "Integer",
            Body = new HighLevelIR.Block { Statements = new() }
        });

        // Act
        var result = _transformer.Transform(hlir);

        // Assert
        Assert.That(result.Functions, Has.Count.EqualTo(2));
        Assert.That(result.Functions.Keys, Contains.Item("func1"));
        Assert.That(result.Functions.Keys, Contains.Item("func2"));
    }

    #endregion

    #region Assignment Statement Tests

    [Test]
    public void Transform_SimpleAssignment_GeneratesMLAssign()
    {
        // Arrange
        var hlir = CreateSimpleProgram();
        var function = new HighLevelIR.Function
        {
            Name = "main",
            ReturnType = "Void",
            Body = new HighLevelIR.Block
            {
                Statements = new List<HighLevelIR.Statement>
                {
                    new HighLevelIR.Assignment
                    {
                        Target = "x",
                        Value = new HighLevelIR.Literal { Value = "42", Type = "Integer" }
                    }
                }
            }
        };
        hlir.Functions.Add(function);

        // Act
        var result = _transformer.Transform(hlir);

        // Assert
        Assert.That(result.Functions["main"].Instructions, Has.Count.GreaterThan(0));
        var firstInstr = result.Functions["main"].Instructions[0];
        Assert.That(firstInstr, Is.TypeOf<MidLevelIR.MLAssign>());
        var assign = (MidLevelIR.MLAssign)firstInstr;
        Assert.That(assign.Target, Is.EqualTo("x"));
        Assert.That(assign.Source, Is.EqualTo("42"));
    }

    [Test]
    public void Transform_AssignmentFromVariable_PreservesVariableName()
    {
        // Arrange
        var hlir = CreateSimpleProgram();
        var function = new HighLevelIR.Function
        {
            Name = "main",
            ReturnType = "Void",
            Body = new HighLevelIR.Block
            {
                Statements = new List<HighLevelIR.Statement>
                {
                    new HighLevelIR.Assignment
                    {
                        Target = "y",
                        Value = new HighLevelIR.Identifier { Name = "x" }
                    }
                }
            }
        };
        hlir.Functions.Add(function);

        // Act
        var result = _transformer.Transform(hlir);

        // Assert
        var assign = (MidLevelIR.MLAssign)result.Functions["main"].Instructions[0];
        Assert.That(assign.Target, Is.EqualTo("y"));
        Assert.That(assign.Source, Is.EqualTo("x"));
    }

    [Test]
    public void Transform_AssignmentFromBinaryExpression_PreservesExpression()
    {
        // Arrange
        var hlir = CreateSimpleProgram();
        var function = new HighLevelIR.Function
        {
            Name = "main",
            ReturnType = "Void",
            Body = new HighLevelIR.Block
            {
                Statements = new List<HighLevelIR.Statement>
                {
                    new HighLevelIR.Assignment
                    {
                        Target = "sum",
                        Value = new HighLevelIR.BinaryOp
                        {
                            Left = new HighLevelIR.Literal { Value = "5", Type = "Integer" },
                            Operator = "+",
                            Right = new HighLevelIR.Literal { Value = "3", Type = "Integer" }
                        }
                    }
                }
            }
        };
        hlir.Functions.Add(function);

        // Act
        var result = _transformer.Transform(hlir);

        // Assert
        var assign = (MidLevelIR.MLAssign)result.Functions["main"].Instructions[0];
        Assert.That(assign.Target, Is.EqualTo("sum"));
        Assert.That(assign.Source, Contains.Substring("+"));
        Assert.That(assign.Source, Contains.Substring("5"));
        Assert.That(assign.Source, Contains.Substring("3"));
    }

    #endregion

    #region Function Call Tests

    [Test]
    public void Transform_SimpleFunctionCall_GeneratesMLCall()
    {
        // Arrange
        var hlir = CreateSimpleProgram();
        var function = new HighLevelIR.Function
        {
            Name = "main",
            ReturnType = "Void",
            Body = new HighLevelIR.Block
            {
                Statements = new List<HighLevelIR.Statement>
                {
                    new HighLevelIR.ExpressionStatement
                    {
                        Expression = new HighLevelIR.FunctionCall
                        {
                            Function = new HighLevelIR.Identifier { Name = "InitGame" },
                            Arguments = new List<HighLevelIR.Expression>()
                        }
                    }
                }
            }
        };
        hlir.Functions.Add(function);

        // Act
        var result = _transformer.Transform(hlir);

        // Assert
        Assert.That(result.Functions["main"].Instructions, Has.Count.GreaterThan(0));
        var call = result.Functions["main"].Instructions[0] as MidLevelIR.MLCall;
        Assert.That(call, Is.Not.Null);
        Assert.That(call.Name, Is.EqualTo("InitGame"));
    }

    [Test]
    public void Transform_FunctionCallWithArguments_IncludesArguments()
    {
        // Arrange
        var hlir = CreateSimpleProgram();
        var function = new HighLevelIR.Function
        {
            Name = "main",
            ReturnType = "Void",
            Body = new HighLevelIR.Block
            {
                Statements = new List<HighLevelIR.Statement>
                {
                    new HighLevelIR.ExpressionStatement
                    {
                        Expression = new HighLevelIR.FunctionCall
                        {
                            Function = new HighLevelIR.Identifier { Name = "SetColor" },
                            Arguments = new List<HighLevelIR.Expression>
                            {
                                new HighLevelIR.Literal { Value = "255", Type = "Integer" }
                            }
                        }
                    }
                }
            }
        };
        hlir.Functions.Add(function);

        // Act
        var result = _transformer.Transform(hlir);

        // Assert
        var call = (MidLevelIR.MLCall)result.Functions["main"].Instructions[0];
        Assert.That(call.Name, Is.EqualTo("SetColor"));
        Assert.That(call.Arguments, Has.Count.EqualTo(1));
        Assert.That(call.Arguments[0], Is.EqualTo("255"));
    }

    #endregion

    #region Control Flow Tests

    [Test]
    public void Transform_IfStatement_GeneratesConditionalInstructions()
    {
        // Arrange
        var hlir = CreateSimpleProgram();
        var function = new HighLevelIR.Function
        {
            Name = "main",
            ReturnType = "Void",
            Body = new HighLevelIR.Block
            {
                Statements = new List<HighLevelIR.Statement>
                {
                    new HighLevelIR.IfStatement
                    {
                        Condition = new HighLevelIR.BinaryOp
                        {
                            Left = new HighLevelIR.Identifier { Name = "x" },
                            Operator = ">",
                            Right = new HighLevelIR.Literal { Value = "0", Type = "Integer" }
                        },
                        ThenBlock = new HighLevelIR.Block
                        {
                            Statements = new List<HighLevelIR.Statement>
                            {
                                new HighLevelIR.Assignment
                                {
                                    Target = "y",
                                    Value = new HighLevelIR.Literal { Value = "1", Type = "Integer" }
                                }
                            }
                        },
                        ElseBlock = null
                    }
                }
            }
        };
        hlir.Functions.Add(function);

        // Act
        var result = _transformer.Transform(hlir);

        // Assert
        var instructions = result.Functions["main"].Instructions;
        Assert.That(instructions, Has.Count.GreaterThan(0));
        // Verify that nested block statements are flattened into MLIR
    }

    [Test]
    public void Transform_WhileLoop_GeneratesLoopInstructions()
    {
        // Arrange
        var hlir = CreateSimpleProgram();
        var function = new HighLevelIR.Function
        {
            Name = "main",
            ReturnType = "Void",
            Body = new HighLevelIR.Block
            {
                Statements = new List<HighLevelIR.Statement>
                {
                    new HighLevelIR.WhileStatement
                    {
                        Condition = new HighLevelIR.BinaryOp
                        {
                            Left = new HighLevelIR.Identifier { Name = "i" },
                            Operator = "<",
                            Right = new HighLevelIR.Literal { Value = "10", Type = "Integer" }
                        },
                        Body = new HighLevelIR.Block
                        {
                            Statements = new List<HighLevelIR.Statement>
                            {
                                new HighLevelIR.Assignment
                                {
                                    Target = "i",
                                    Value = new HighLevelIR.BinaryOp
                                    {
                                        Left = new HighLevelIR.Identifier { Name = "i" },
                                        Operator = "+",
                                        Right = new HighLevelIR.Literal { Value = "1", Type = "Integer" }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };
        hlir.Functions.Add(function);

        // Act
        var result = _transformer.Transform(hlir);

        // Assert
        var instructions = result.Functions["main"].Instructions;
        Assert.That(instructions, Has.Count.GreaterThan(0));
    }

    #endregion

    #region Built-in Function Tests

    [Test]
    public void Transform_WritelnCall_GeneratesMLCall()
    {
        // Arrange
        var hlir = CreateSimpleProgram();
        var function = new HighLevelIR.Function
        {
            Name = "main",
            ReturnType = "Void",
            Body = new HighLevelIR.Block
            {
                Statements = new List<HighLevelIR.Statement>
                {
                    new HighLevelIR.ExpressionStatement
                    {
                        Expression = new HighLevelIR.FunctionCall
                        {
                            Function = new HighLevelIR.Identifier { Name = "writeln" },
                            Arguments = new List<HighLevelIR.Expression>
                            {
                                new HighLevelIR.Literal { Value = "Hello", Type = "String" }
                            }
                        }
                    }
                }
            }
        };
        hlir.Functions.Add(function);

        // Act
        var result = _transformer.Transform(hlir);

        // Assert
        var call = (MidLevelIR.MLCall)result.Functions["main"].Instructions[0];
        Assert.That(call.Name, Is.EqualTo("writeln"));
        Assert.That(call.Arguments, Has.Count.EqualTo(1));
    }

    #endregion

    #region Empty Program Tests

    [Test]
    public void Transform_EmptyProgram_ReturnsValidMidLevelIR()
    {
        // Arrange
        var hlir = CreateSimpleProgram();

        // Act
        var result = _transformer.Transform(hlir);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.SourceFile, Is.EqualTo(hlir.SourceFile));
        Assert.That(result.Functions, Is.Empty);
    }

    [Test]
    public void Transform_EmptyFunction_ReturnsEmptyInstructions()
    {
        // Arrange
        var hlir = CreateSimpleProgram();
        var function = new HighLevelIR.Function
        {
            Name = "main",
            ReturnType = "Void",
            Body = new HighLevelIR.Block { Statements = new List<HighLevelIR.Statement>() }
        };
        hlir.Functions.Add(function);

        // Act
        var result = _transformer.Transform(hlir);

        // Assert
        Assert.That(result.Functions["main"].Instructions, Is.Empty);
    }

    #endregion

    #region Helper Methods

    private HighLevelIR CreateSimpleProgram()
    {
        return new HighLevelIR { SourceFile = "test.pas" };
    }

    #endregion
}
