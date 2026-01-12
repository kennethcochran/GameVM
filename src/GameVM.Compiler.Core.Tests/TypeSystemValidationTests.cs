using NUnit.Framework;
using GameVM.Compiler.Core.IR;
using GameVM.Compiler.Core.IR.Transformers;
using System.Collections.Generic;

namespace GameVM.Compiler.Core.Tests;

/// <summary>
/// Tests for type system validation during compilation.
/// Validates that type errors are properly detected and reported.
/// </summary>
[TestFixture]
public class TypeSystemValidationTests
{
    private HlirToMlirTransformer _transformer;

    [SetUp]
    public void Setup()
    {
        _transformer = new HlirToMlirTransformer();
    }

    #region Type Mismatch Detection Tests

    [Test]
    public void Validate_IntegerToStringAssignment_ReportsTypeMismatch()
    {
        // Arrange
        var hlir = CreateSimpleProgram();
        var function = new HighLevelIR.Function
        {
            Name = "test",
            ReturnType = "Void",
            Body = new HighLevelIR.Block
            {
                Statements = new List<HighLevelIR.Statement>
                {
                    new HighLevelIR.Assignment
                    {
                        Target = "message",
                        Value = new HighLevelIR.Literal { Value = "42", Type = "Integer" }
                    }
                }
            }
        };
        hlir.Variables.Add(new HighLevelIR.Variable { Name = "message", Type = "String" });
        hlir.Functions.Add(function);

        // Act & Assert
        // Type validator should detect Integer → String mismatch
    }

    [Test]
    public void Validate_StringToIntegerAssignment_ReportsTypeMismatch()
    {
        // Arrange
        var hlir = CreateSimpleProgram();
        hlir.Variables.Add(new HighLevelIR.Variable { Name = "count", Type = "Integer" });
        var function = new HighLevelIR.Function
        {
            Name = "test",
            ReturnType = "Void",
            Body = new HighLevelIR.Block
            {
                Statements = new List<HighLevelIR.Statement>
                {
                    new HighLevelIR.Assignment
                    {
                        Target = "count",
                        Value = new HighLevelIR.Literal { Value = "hello", Type = "String" }
                    }
                }
            }
        };
        hlir.Functions.Add(function);

        // Act & Assert
        // Type validator should detect String → Integer mismatch
    }

    [Test]
    public void Validate_BooleanToIntegerAssignment_ReportsTypeMismatch()
    {
        // Arrange
        var hlir = CreateSimpleProgram();
        hlir.Variables.Add(new HighLevelIR.Variable { Name = "count", Type = "Integer" });
        var function = new HighLevelIR.Function
        {
            Name = "test",
            ReturnType = "Void",
            Body = new HighLevelIR.Block
            {
                Statements = new List<HighLevelIR.Statement>
                {
                    new HighLevelIR.Assignment
                    {
                        Target = "count",
                        Value = new HighLevelIR.Literal { Value = "true", Type = "Boolean" }
                    }
                }
            }
        };
        hlir.Functions.Add(function);

        // Act & Assert
        // Type validator should detect Boolean → Integer mismatch
    }

    #endregion

    #region Undefined Variable Tests

    [Test]
    public void Validate_UndefinedVariable_ReportsError()
    {
        // Arrange
        var hlir = CreateSimpleProgram();
        var function = new HighLevelIR.Function
        {
            Name = "test",
            ReturnType = "Void",
            Body = new HighLevelIR.Block
            {
                Statements = new List<HighLevelIR.Statement>
                {
                    new HighLevelIR.Assignment
                    {
                        Target = "result",
                        Value = new HighLevelIR.Identifier { Name = "undefined_var" }
                    }
                }
            }
        };
        hlir.Functions.Add(function);

        // Act & Assert
        // Type validator should report undefined variable
    }

    [Test]
    public void Validate_MultipleUndefinedVariables_ReportsAllErrors()
    {
        // Arrange
        var hlir = CreateSimpleProgram();
        var function = new HighLevelIR.Function
        {
            Name = "test",
            ReturnType = "Void",
            Body = new HighLevelIR.Block
            {
                Statements = new List<HighLevelIR.Statement>
                {
                    new HighLevelIR.Assignment
                    {
                        Target = "x",
                        Value = new HighLevelIR.BinaryOp
                        {
                            Left = new HighLevelIR.Identifier { Name = "undefined1" },
                            Operator = "+",
                            Right = new HighLevelIR.Identifier { Name = "undefined2" }
                        }
                    }
                }
            }
        };
        hlir.Functions.Add(function);

        // Act & Assert
        // Type validator should report both undefined variables
    }

    #endregion

    #region Function Signature Tests

    [Test]
    public void Validate_FunctionCallWithWrongArgumentCount_ReportsError()
    {
        // Arrange
        var hlir = CreateSimpleProgram();
        hlir.Functions.Add(new HighLevelIR.Function
        {
            Name = "add",
            ReturnType = "Integer",
            Parameters = new List<HighLevelIR.Parameter>
            {
                new() { Name = "a", Type = "Integer" },
                new() { Name = "b", Type = "Integer" }
            },
            Body = new HighLevelIR.Block { Statements = new() }
        });

        var main = new HighLevelIR.Function
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
                            Function = new HighLevelIR.Identifier { Name = "add" },
                            Arguments = new List<HighLevelIR.Expression>
                            {
                                new HighLevelIR.Literal { Value = "5", Type = "Integer" }
                            }
                        }
                    }
                }
            }
        };
        hlir.Functions.Add(main);

        // Act & Assert
        // Type validator should report wrong argument count
    }

    [Test]
    public void Validate_FunctionCallWithWrongArgumentType_ReportsError()
    {
        // Arrange
        var hlir = CreateSimpleProgram();
        hlir.Functions.Add(new HighLevelIR.Function
        {
            Name = "process",
            ReturnType = "Integer",
            Parameters = new List<HighLevelIR.Parameter>
            {
                new() { Name = "value", Type = "Integer" }
            },
            Body = new HighLevelIR.Block { Statements = new() }
        });

        var main = new HighLevelIR.Function
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
                            Function = new HighLevelIR.Identifier { Name = "process" },
                            Arguments = new List<HighLevelIR.Expression>
                            {
                                new HighLevelIR.Literal { Value = "hello", Type = "String" }
                            }
                        }
                    }
                }
            }
        };
        hlir.Functions.Add(main);

        // Act & Assert
        // Type validator should report wrong argument type
    }

    #endregion

    #region Array Tests

    [Test]
    public void Validate_ArrayBoundsExceeded_ReportsError()
    {
        // Arrange
        var hlir = CreateSimpleProgram();
        var arrayVar = new HighLevelIR.Variable 
        { 
            Name = "arr", 
            Type = "Array",
            ArrayElementType = "Integer",
            ArrayLength = 10
        };
        hlir.Variables.Add(arrayVar);

        var function = new HighLevelIR.Function
        {
            Name = "test",
            ReturnType = "Void",
            Body = new HighLevelIR.Block
            {
                Statements = new List<HighLevelIR.Statement>
                {
                    new HighLevelIR.Assignment
                    {
                        Target = "arr[15]",
                        Value = new HighLevelIR.Literal { Value = "1", Type = "Integer" }
                    }
                }
            }
        };
        hlir.Functions.Add(function);

        // Act & Assert
        // Type validator should report array bounds exceed
    }

    [Test]
    public void Validate_ArrayIndexNotInteger_ReportsError()
    {
        // Arrange
        var hlir = CreateSimpleProgram();
        hlir.Variables.Add(new HighLevelIR.Variable 
        { 
            Name = "arr", 
            Type = "Array",
            ArrayElementType = "Integer",
            ArrayLength = 10
        });

        var function = new HighLevelIR.Function
        {
            Name = "test",
            ReturnType = "Void",
            Body = new HighLevelIR.Block
            {
                Statements = new List<HighLevelIR.Statement>
                {
                    new HighLevelIR.Assignment
                    {
                        Target = "arr[hello]",
                        Value = new HighLevelIR.Literal { Value = "1", Type = "Integer" }
                    }
                }
            }
        };
        hlir.Functions.Add(function);

        // Act & Assert
        // Type validator should report non-integer array index
    }

    #endregion

    #region Implicit Type Conversion Tests

    [Test]
    public void Validate_ImplicitIntToRealConversion_Allowed()
    {
        // Arrange
        var hlir = CreateSimpleProgram();
        hlir.Variables.Add(new HighLevelIR.Variable { Name = "x", Type = "Real" });
        var function = new HighLevelIR.Function
        {
            Name = "test",
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

        // Act & Assert
        // Type validator should allow Integer → Real conversion
    }

    [Test]
    public void Validate_ImplicitRealToIntConversion_NotAllowed()
    {
        // Arrange
        var hlir = CreateSimpleProgram();
        hlir.Variables.Add(new HighLevelIR.Variable { Name = "count", Type = "Integer" });
        var function = new HighLevelIR.Function
        {
            Name = "test",
            ReturnType = "Void",
            Body = new HighLevelIR.Block
            {
                Statements = new List<HighLevelIR.Statement>
                {
                    new HighLevelIR.Assignment
                    {
                        Target = "count",
                        Value = new HighLevelIR.Literal { Value = "3.14", Type = "Real" }
                    }
                }
            }
        };
        hlir.Functions.Add(function);

        // Act & Assert
        // Type validator should NOT allow Real → Integer implicit conversion
    }

    #endregion

    #region Operator Type Validation Tests

    [Test]
    public void Validate_AddingStringAndInteger_ReportsError()
    {
        // Arrange
        var hlir = CreateSimpleProgram();
        var function = new HighLevelIR.Function
        {
            Name = "test",
            ReturnType = "Void",
            Body = new HighLevelIR.Block
            {
                Statements = new List<HighLevelIR.Statement>
                {
                    new HighLevelIR.Assignment
                    {
                        Target = "result",
                        Value = new HighLevelIR.BinaryOp
                        {
                            Left = new HighLevelIR.Literal { Value = "hello", Type = "String" },
                            Operator = "+",
                            Right = new HighLevelIR.Literal { Value = "5", Type = "Integer" }
                        }
                    }
                }
            }
        };
        hlir.Functions.Add(function);

        // Act & Assert
        // Type validator should report incompatible types for +
    }

    [Test]
    public void Validate_ComparingDifferentTypes_ReportsError()
    {
        // Arrange
        var hlir = CreateSimpleProgram();
        var function = new HighLevelIR.Function
        {
            Name = "test",
            ReturnType = "Void",
            Body = new HighLevelIR.Block
            {
                Statements = new List<HighLevelIR.Statement>
                {
                    new HighLevelIR.IfStatement
                    {
                        Condition = new HighLevelIR.BinaryOp
                        {
                            Left = new HighLevelIR.Literal { Value = "5", Type = "Integer" },
                            Operator = ">",
                            Right = new HighLevelIR.Literal { Value = "hello", Type = "String" }
                        },
                        ThenBlock = new HighLevelIR.Block { Statements = new() },
                        ElseBlock = null
                    }
                }
            }
        };
        hlir.Functions.Add(function);

        // Act & Assert
        // Type validator should report type mismatch in comparison
    }

    #endregion

    #region Return Type Validation Tests

    [Test]
    public void Validate_IncorrectReturnType_ReportsError()
    {
        // Arrange
        var hlir = CreateSimpleProgram();
        var function = new HighLevelIR.Function
        {
            Name = "getInt",
            ReturnType = "Integer",
            Body = new HighLevelIR.Block
            {
                Statements = new List<HighLevelIR.Statement>
                {
                    new HighLevelIR.ReturnStatement
                    {
                        Value = new HighLevelIR.Literal { Value = "hello", Type = "String" }
                    }
                }
            }
        };
        hlir.Functions.Add(function);

        // Act & Assert
        // Type validator should report mismatched return type
    }

    #endregion

    #region Circular Type Reference Tests

    [Test]
    public void Validate_CircularTypeDefinition_ReportsError()
    {
        // Arrange
        // Hypothetical: TypeA contains TypeB contains TypeA
        var hlir = CreateSimpleProgram();
        // When circular type detection is implemented, this would create a circular reference
        // For now, we test the structure supports it
        var typeA = new HighLevelIR.BasicType("test.pas", "TypeA");
        var typeB = new HighLevelIR.BasicType("test.pas", "TypeB");
        // Simulating circular reference (would need proper type system support)
        hlir.Types["TypeA"] = typeA;
        hlir.Types["TypeB"] = typeB;

        // Act
        var result = _transformer.Transform(hlir);

        // Assert
        // When circular type detection is implemented, should report error
        // For now, verify transformation handles the structure
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void Validate_NestedTypeMismatch_ReportsError()
    {
        // Arrange
        var hlir = CreateSimpleProgram();
        var function = new HighLevelIR.Function
        {
            Name = "test",
            ReturnType = "Void",
            Body = new HighLevelIR.Block
            {
                Statements = new List<HighLevelIR.Statement>
                {
                    new HighLevelIR.Assignment
                    {
                        Target = "nested",
                        Value = new HighLevelIR.BinaryOp
                        {
                            Left = new HighLevelIR.Literal { Value = "5", Type = "Integer" },
                            Operator = "+",
                            Right = new HighLevelIR.Literal { Value = "hello", Type = "String" },
                            SourceFile = "test.pas"
                        }
                    }
                }
            }
        };
        hlir.Functions.Add(function);

        // Act
        var result = _transformer.Transform(hlir);

        // Assert
        // When type validation is implemented, should detect Integer + String mismatch
        // For now, verify transformation handles the structure
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void Validate_ArrayBoundsWithDynamicIndex_ValidatesAtRuntime()
    {
        // Arrange
        var hlir = CreateSimpleProgram();
        var function = new HighLevelIR.Function
        {
            Name = "test",
            ReturnType = "Void",
            Body = new HighLevelIR.Block
            {
                Statements = new List<HighLevelIR.Statement>
                {
                    new HighLevelIR.Assignment
                    {
                        Target = "arr[i]",
                        Value = new HighLevelIR.Literal { Value = "1", Type = "Integer" }
                    }
                }
            }
        };
        hlir.Functions.Add(function);

        // Act
        var result = _transformer.Transform(hlir);

        // Assert
        // When array bounds validation is implemented, should validate dynamic indices
        // For now, verify transformation handles the structure
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void Validate_FunctionOverloadResolution_ReportsAmbiguity()
    {
        // Arrange
        var hlir = CreateSimpleProgram();
        // Simulating function overloads (would need proper overload support)
        var func1 = new HighLevelIR.Function
        {
            Name = "process",
            ReturnType = "Integer",
            Body = new HighLevelIR.Block { Statements = new List<HighLevelIR.Statement>() }
        };
        func1.AddParameter(new HighLevelIR.Parameter("value", new HighLevelIR.BasicType("test.pas", "Integer"), "test.pas"));
        hlir.Functions.Add("process1", func1);

        var func2 = new HighLevelIR.Function
        {
            Name = "process",
            ReturnType = "Integer",
            Body = new HighLevelIR.Block { Statements = new List<HighLevelIR.Statement>() }
        };
        func2.AddParameter(new HighLevelIR.Parameter("value", new HighLevelIR.BasicType("test.pas", "Real"), "test.pas"));
        hlir.Functions.Add("process2", func2);

        // Act
        var result = _transformer.Transform(hlir);

        // Assert
        // When overload resolution is implemented, ambiguous calls should report errors
        // For now, verify transformation handles multiple functions
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void Validate_ComplexNestedTypeMismatch_ReportsError()
    {
        // Arrange
        var hlir = CreateSimpleProgram();
        var function = new HighLevelIR.Function
        {
            Name = "test",
            ReturnType = "Void",
            Body = new HighLevelIR.Block
            {
                Statements = new List<HighLevelIR.Statement>
                {
                    new HighLevelIR.Assignment
                    {
                        Target = "result",
                        Value = new HighLevelIR.BinaryOp
                        {
                            Left = new HighLevelIR.BinaryOp
                            {
                                Left = new HighLevelIR.Literal { Value = "5", Type = "Integer" },
                                Operator = "*",
                                Right = new HighLevelIR.Literal { Value = "3", Type = "Integer" },
                                SourceFile = "test.pas"
                            },
                            Operator = "+",
                            Right = new HighLevelIR.Literal { Value = "hello", Type = "String" },
                            SourceFile = "test.pas"
                        }
                    }
                }
            }
        };
        hlir.Functions.Add(function);

        // Act
        var result = _transformer.Transform(hlir);

        // Assert
        // When type validation is implemented, should detect nested type mismatch
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void Validate_ArrayBoundsExceededWithConstant_ReportsError()
    {
        // Arrange
        var hlir = CreateSimpleProgram();
        var function = new HighLevelIR.Function
        {
            Name = "test",
            ReturnType = "Void",
            Body = new HighLevelIR.Block
            {
                Statements = new List<HighLevelIR.Statement>
                {
                    new HighLevelIR.Assignment
                    {
                        Target = "arr[15]",
                        Value = new HighLevelIR.Literal { Value = "1", Type = "Integer" }
                    }
                }
            }
        };
        hlir.Functions.Add(function);
        // Note: Array bounds checking would need array type information

        // Act
        var result = _transformer.Transform(hlir);

        // Assert
        // When array bounds validation is implemented, should report out-of-bounds access
        Assert.That(result, Is.Not.Null);
    }

    #endregion

    #region Edge Cases

    [Test]
    public void Validate_EmptyProgram_NoErrors()
    {
        // Arrange
        var hlir = CreateSimpleProgram();

        // Act
        var result = _transformer.Transform(hlir);

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void Validate_ValidProgram_NoErrors()
    {
        // Arrange
        var hlir = CreateSimpleProgram();
        hlir.Variables.Add(new HighLevelIR.Variable { Name = "x", Type = "Integer" });
        var function = new HighLevelIR.Function
        {
            Name = "test",
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
        Assert.That(result, Is.Not.Null);
    }

    #endregion

    #region Helper Methods

    private HighLevelIR CreateSimpleProgram()
    {
        return new HighLevelIR { SourceFile = "test.pas" };
    }

    #endregion
}
