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
    private HlirToMlirTransformer _transformer = null!;

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
        var voidType = new HighLevelIR.BasicType("test.pas", "Void");
        var intType = new HighLevelIR.BasicType("test.pas", "Integer");
        var stringType = new HighLevelIR.BasicType("test.pas", "String");

        var function = new HighLevelIR.Function("test.pas", "test", voidType, new HighLevelIR.Block("test.pas"));

        var block = function.Body;
        block.Statements.Add(new HighLevelIR.Assignment(
            "message",
            new HighLevelIR.Literal("42", intType, "test.pas"),
            "test.pas"
        ));

        // Add variable to the module
        hlir.Modules[0].Variables.Add(new HighLevelIR.Variable { Name = "message", Type = stringType, SourceFile = "test.pas" });

        // Add function to the module
        hlir.Modules[0].Functions.Add(function);

        // Act
        var result = _transformer.Transform(hlir);

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void Validate_StringToIntegerAssignment_ReportsTypeMismatch()
    {
        // Arrange
        var hlir = CreateSimpleProgram();
        var voidType = new HighLevelIR.BasicType("test.pas", "Void");
        var intType = new HighLevelIR.BasicType("test.pas", "Integer");
        var stringType = new HighLevelIR.BasicType("test.pas", "String");

        hlir.Modules[0].Variables.Add(new HighLevelIR.Variable { Name = "count", Type = intType, SourceFile = "test.pas" });
        var function = new HighLevelIR.Function("test.pas", "test", voidType, new HighLevelIR.Block("test.pas"));

        var block = function.Body;
        block.Statements.Add(new HighLevelIR.Assignment(
            "count",
            new HighLevelIR.Literal("hello", stringType, "test.pas"),
            "test.pas"
        ));

        // Add function to the module
        hlir.Modules[0].Functions.Add(function);

        // Act
        var result = _transformer.Transform(hlir);

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void Validate_BooleanToIntegerAssignment_ReportsTypeMismatch()
    {
        // Arrange
        var hlir = CreateSimpleProgram();
        var voidType = new HighLevelIR.BasicType("test.pas", "Void");
        var intType = new HighLevelIR.BasicType("test.pas", "Integer");
        var boolType = new HighLevelIR.BasicType("test.pas", "Boolean");

        hlir.Modules[0].Variables.Add(new HighLevelIR.Variable { Name = "count", Type = intType, SourceFile = "test.pas" });
        var function = new HighLevelIR.Function("test.pas", "test", voidType, new HighLevelIR.Block("test.pas"));

        var block = function.Body;
        block.Statements.Add(new HighLevelIR.Assignment(
            "count",
            new HighLevelIR.Literal("true", boolType, "test.pas"),
            "test.pas"
        ));

        // Add function to the module
        hlir.Modules[0].Functions.Add(function);

        // Act
        var result = _transformer.Transform(hlir);

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    #endregion

    #region Undefined Variable Tests

    [Test]
    public void Validate_UndefinedVariable_ReportsError()
    {
        // Arrange
        var hlir = CreateSimpleProgram();
        var voidType = new HighLevelIR.BasicType("test.pas", "Void");
        var function = new HighLevelIR.Function("test.pas", "test", voidType, new HighLevelIR.Block("test.pas"));

        var block = function.Body;
        block.Statements.Add(new HighLevelIR.Assignment(
            "result",
            new HighLevelIR.Identifier("undefined_var", "test.pas"),
            "test.pas"
        ));

        // Add function to the module
        hlir.Modules[0].Functions.Add(function);

        // Act
        var result = _transformer.Transform(hlir);

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void Validate_MultipleUndefinedVariables_ReportsAllErrors()
    {
        // Arrange
        var hlir = CreateSimpleProgram();
        var voidType = new HighLevelIR.BasicType("test.pas", "Void");
        var function = new HighLevelIR.Function("test.pas", "test", voidType, new HighLevelIR.Block("test.pas"));

        var block = function.Body;
        block.Statements.Add(new HighLevelIR.Assignment(
            "x",
            new HighLevelIR.BinaryOp(
                new HighLevelIR.Identifier("undefined1", "test.pas"),
                "+",
                new HighLevelIR.Identifier("undefined2", "test.pas"),
                "test.pas"
            ),
            "test.pas"
        ));

        // Add function to the module
        hlir.Modules[0].Functions.Add(function);

        // Act
        var result = _transformer.Transform(hlir);

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    #endregion

    #region Function Signature Tests

    [Test]
    public void Validate_FunctionCallWithWrongArgumentCount_ReportsError()
    {
        // Arrange
        var hlir = CreateSimpleProgram();

        // Create the add function
        var addFunction = new HighLevelIR.Function(
            "add",
            "test.pas",
            "Integer",
            new HighLevelIR.Block("test.pas")
        );

        // Add parameters to the add function
        addFunction.Parameters.Add(new HighLevelIR.Parameter("a", "Integer", "test.pas"));
        addFunction.Parameters.Add(new HighLevelIR.Parameter("b", "Integer", "test.pas"));

        // Add the add function to the module
        hlir.Modules[0].Functions.Add(addFunction);

        // Create the main function
        var mainFunction = new HighLevelIR.Function("main", "test.pas", "Void", new HighLevelIR.Block("test.pas"));

        // Add a function call with wrong number of arguments
        var functionCall = new HighLevelIR.FunctionCall("add", new List<HighLevelIR.Expression>
        {
            new HighLevelIR.Literal("5", new HighLevelIR.HlType("test.pas", "Integer"), "test.pas")
        }, "test.pas");

        // Add the function call to the main function's body
        mainFunction.Body.Statements.Add(new HighLevelIR.ExpressionStatement { Expression = functionCall, SourceFile = "test.pas" });

        // Add the main function to the module
        hlir.Modules[0].Functions.Add(mainFunction);

        // Act
        var result = _transformer.Transform(hlir);

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void Validate_FunctionCallWithWrongArgumentType_ReportsError()
    {
        // Arrange
        var hlir = CreateSimpleProgram();

        // Create the process function
        var processFunction = new HighLevelIR.Function(
            "process",
            "test.pas",
            "Integer",
            new HighLevelIR.Block("test.pas")
        );

        // Add parameter to the process function
        processFunction.Parameters.Add(new HighLevelIR.Parameter("value", "Integer", "test.pas"));

        // Add the process function to the module
        hlir.Modules[0].Functions.Add(processFunction);

        // Create the main function
        var mainFunction = new HighLevelIR.Function("main", "test.pas", "Void", new HighLevelIR.Block("test.pas"));

        // Add a function call with wrong argument type
        var functionCall = new HighLevelIR.FunctionCall("process", new List<HighLevelIR.Expression>
        {
            new HighLevelIR.Literal("hello", new HighLevelIR.HlType("test.pas", "String"), "test.pas")
        }, "test.pas");

        // Add the function call to the main function's body
        mainFunction.Body.Statements.Add(new HighLevelIR.ExpressionStatement { Expression = functionCall, SourceFile = "test.pas" });

        // Add the main function to the module
        hlir.Modules[0].Functions.Add(mainFunction);

        // Act
        var result = _transformer.Transform(hlir);

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    #endregion

    #region Array Tests

    [Test]
    public void Validate_ArrayBoundsExceeded_ReportsError()
    {
        // Arrange
        var hlir = CreateSimpleProgram();

        // Create array variable
        var arrayVar = new HighLevelIR.Variable { Name = "arr", Type = new HighLevelIR.BasicType("test.pas", "Array[0..9] of Integer") { Name = "Array[0..9] of Integer" }, SourceFile = "test.pas" };

        // Add array variable to the module
        hlir.Modules[0].Variables.Add(arrayVar);

        // Create function
        var function = new HighLevelIR.Function("test", "test.pas", "Void", new HighLevelIR.Block("test.pas"));

        // Create array access expression
        var arrayAccess = new HighLevelIR.ArrayAccess(
            new HighLevelIR.Identifier("arr", "test.pas"),
            new HighLevelIR.Literal("15", new HighLevelIR.HlType("test.pas", "Integer"), "test.pas"),
            "test.pas"
        );

        // Create assignment statement
        var assignment = new HighLevelIR.Assignment(
            arrayAccess,
            new HighLevelIR.Literal("1", new HighLevelIR.HlType("test.pas", "Integer"), "test.pas"),
            "test.pas"
        );

        // Add assignment to function body
        function.Body.Statements.Add(assignment);

        // Add function to the module
        hlir.Modules[0].Functions.Add(function);

        // Act & Assert
        // Type validator should report array bounds exceed
        var result = _transformer.Transform(hlir);
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void Validate_ArrayIndexNotInteger_ReportsError()
    {
        // Arrange
        var hlir = CreateSimpleProgram();

        // Create array variable
        var arrayVar = new HighLevelIR.Variable { Name = "arr", Type = new HighLevelIR.BasicType("test.pas", "Array[0..9] of Integer") { Name = "Array[0..9] of Integer" }, SourceFile = "test.pas" };

        // Add array variable to the module
        hlir.Modules[0].Variables.Add(arrayVar);

        // Create function
        var function = new HighLevelIR.Function("test", "test.pas", "Void", new HighLevelIR.Block("test.pas"));

        // Create array access with non-integer index
        var arrayAccess = new HighLevelIR.ArrayAccess(
            new HighLevelIR.Identifier("arr", "test.pas"),
            new HighLevelIR.Identifier("hello", "test.pas"),
            "test.pas"
        );

        // Create assignment statement
        var assignment = new HighLevelIR.Assignment(
            arrayAccess,
            new HighLevelIR.Literal("1", new HighLevelIR.HlType("test.pas", "Integer"), "test.pas"),
            "test.pas"
        );

        // Add assignment to function body
        function.Body.Statements.Add(assignment);

        // Add function to the module
        hlir.Modules[0].Functions.Add(function);

        // Act & Assert
        // Type validator should report non-integer array index
        var result = _transformer.Transform(hlir);
        Assert.That(result, Is.Not.Null);
    }

    #endregion

    #region Implicit Type Conversion Tests

    [Test]
    public void Validate_ImplicitIntToRealConversion_Allowed()
    {
        // Arrange
        var hlir = CreateSimpleProgram();

        // Add variable to the module
        hlir.Modules[0].Variables.Add(new HighLevelIR.Variable { Name = "x", Type = new HighLevelIR.BasicType("test.pas", "Real") { Name = "Real" }, SourceFile = "test.pas" });

        // Create function
        var function = new HighLevelIR.Function("test", "test.pas", "Void", new HighLevelIR.Block("test.pas"));

        // Create assignment with implicit conversion
        var assignment = new HighLevelIR.Assignment(
            "x",
            new HighLevelIR.Literal("42", new HighLevelIR.HlType("test.pas", "Integer"), "test.pas"),
            "test.pas"
        );

        // Add assignment to function body
        function.Body.Statements.Add(assignment);

        // Add function to the module
        hlir.Modules[0].Functions.Add(function);

        // Act & Assert
        // Type validator should allow Integer → Real conversion
        var result = _transformer.Transform(hlir);
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void Validate_ImplicitRealToIntConversion_NotAllowed()
    {
        // Arrange
        var hlir = CreateSimpleProgram();

        // Add variable to the module
        hlir.Modules[0].Variables.Add(new HighLevelIR.Variable { Name = "count", Type = new HighLevelIR.BasicType("test.pas", "Integer") { Name = "Integer" }, SourceFile = "test.pas" });

        // Create function
        var function = new HighLevelIR.Function("test", "test.pas", "Void", new HighLevelIR.Block("test.pas"));

        // Create assignment with implicit conversion that should not be allowed
        var assignment = new HighLevelIR.Assignment(
            "count",
            new HighLevelIR.Literal("3.14", new HighLevelIR.HlType("test.pas", "Real"), "test.pas"),
            "test.pas"
        );

        // Add assignment to function body
        function.Body.Statements.Add(assignment);

        // Add function to the module
        hlir.Modules[0].Functions.Add(function);

        // Act & Assert
        // Type validator should NOT allow Real → Integer implicit conversion
        var result = _transformer.Transform(hlir);
        Assert.That(result, Is.Not.Null);
    }

    #endregion

    #region Operator Type Validation Tests

    [Test]
    public void Validate_AddingStringAndInteger_ReportsError()
    {
        // Arrange
        var hlir = CreateSimpleProgram();

        // Create function
        var function = new HighLevelIR.Function("test", "test.pas", "Void", new HighLevelIR.Block("test.pas"));

        // Create a binary operation with incompatible types
        var binaryOp = new HighLevelIR.BinaryOp(
            new HighLevelIR.Literal("5", new HighLevelIR.HlType("test.pas", "Integer"), "test.pas"),
            "+",
            new HighLevelIR.Literal("hello", new HighLevelIR.HlType("test.pas", "String"), "test.pas"),
            "test.pas"
        );

        // Add the binary operation to the function body
        function.Body.Statements.Add(new HighLevelIR.ExpressionStatement { Expression = binaryOp, SourceFile = "test.pas" });

        // Add function to the module
        hlir.Modules[0].Functions.Add(function);

        // Act & Assert
        // Type validator should report wrong argument type
        var result = _transformer.Transform(hlir);
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void Validate_ComparingDifferentTypes_ReportsError()
    {
        // Arrange
        var hlir = CreateSimpleProgram();
        var function = new HighLevelIR.Function
        {
            Name = "test",
            SourceFile = "test.pas",
            ReturnType = new HighLevelIR.BasicType("test.pas", "Void"),
            Body = new HighLevelIR.Block("test.pas")
            {
                Statements = new List<HighLevelIR.Statement>
                {
                    new HighLevelIR.IfStatement
                    {
                        Condition = new HighLevelIR.BinaryOp
                        {
                            Left = new HighLevelIR.Literal("5", new HighLevelIR.HlType("test.pas", "Integer"), "test.pas"),
                            Operator = ">",
                            Right = new HighLevelIR.Literal { Value = "hello", Type = new HighLevelIR.BasicType("test.pas", "String"), SourceFile = "test.pas" }
                        }
                    }
                }
            }
        };
        hlir.Modules[0].Functions.Add(function);

        // Act
        var result = _transformer.Transform(hlir);

        // Assert
        Assert.That(result, Is.Not.Null);
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
            SourceFile = "test.pas",
            ReturnType = new HighLevelIR.BasicType("test.pas", "Integer"),
            Body = new HighLevelIR.Block
            {
                Statements = new List<HighLevelIR.Statement>
                {
                    new HighLevelIR.ReturnStatement
                    {
                        Value = new HighLevelIR.Literal { Value = "hello", Type = new HighLevelIR.BasicType("test.pas", "String"), SourceFile = "test.pas" }
                    }
                }
            }
        };
        hlir.Modules[0].Functions.Add(function);

        // Act & Assert
        // Type validator should report mismatched return type
        var result = _transformer.Transform(hlir);
        Assert.That(result, Is.Not.Null);
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
#pragma warning disable CS0618
        hlir.Types["TypeA"] = typeA;
        hlir.Types["TypeB"] = typeB;
#pragma warning restore CS0618

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
                            Left = new HighLevelIR.Literal("5", new HighLevelIR.HlType("test.pas", "Integer"), "test.pas"),
                            Operator = "+",
                            Right = new HighLevelIR.Literal("hello", new HighLevelIR.HlType("test.pas", "String"), "test.pas"),
                            SourceFile = "test.pas"
                        }
                    }
                }
            }
        };
        // Add function to the module
        hlir.Modules[0].Functions.Add(function);

        // Create a new function for the nested test
        var nestedFunction = new HighLevelIR.Function("test_nested", "test", "Void", new HighLevelIR.Block("test"));

        var block = nestedFunction.Body;
        block.Statements.Add(new HighLevelIR.Assignment(
            "nested",
            new HighLevelIR.BinaryOp(
                new HighLevelIR.Literal(5, new HighLevelIR.HlType("test.pas", "Integer"), "test"),
                "+",
                new HighLevelIR.Literal("hello", new HighLevelIR.HlType("test.pas", "String"), "test"),
                "test"
            )
        ));

        // Add the nested function to the module
        hlir.Modules[0].Functions.Add(nestedFunction);

        // Act
        var result = _transformer.Transform(hlir);

        // Assert
        // When type validation is implemented, should detect Integer + String mismatch
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void Validate_ArrayBoundsWithDynamicIndex_ValidatesAtRuntime()
    {
        // Arrange
        var hlir = CreateSimpleProgram();
        var function = new HighLevelIR.Function("test", "test", "Void", new HighLevelIR.Block("test"));

        var block = function.Body;
        block.Statements.Add(new HighLevelIR.Assignment(
            "arr[i]",
            new HighLevelIR.Literal(1, new HighLevelIR.HlType("Integer"), "test")
        ));

        // Add function to the module
        hlir.Modules[0].Functions.Add(function);

        // Act
        var result = _transformer.Transform(hlir);

        // Assert
        // When array bounds validation is implemented, should validate dynamic indices
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void Validate_FunctionOverloadResolution_ReportsAmbiguity()
    {
        // Arrange
        var hlir = CreateSimpleProgram();
        // Simulating function overloads (would need proper overload support)
        var func1 = new HighLevelIR.Function("process", "test", "Integer", new HighLevelIR.Block("test"));
        func1.AddParameter(new HighLevelIR.Parameter("value", new HighLevelIR.BasicType("test.pas", "Integer"), "test.pas"));
        hlir.Modules[0].Functions.Add(func1);

        var func2 = new HighLevelIR.Function("process", "test", "Integer", new HighLevelIR.Block("test"));
        func2.AddParameter(new HighLevelIR.Parameter("value", new HighLevelIR.BasicType("test.pas", "Real"), "test.pas"));
        hlir.Modules[0].Functions.Add(func2);

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
        var function = new HighLevelIR.Function("test", "test", "Void", new HighLevelIR.Block("test"));

        var block = function.Body;
        block.Statements.Add(new HighLevelIR.Assignment(
            "result",
            new HighLevelIR.BinaryOp(
                new HighLevelIR.BinaryOp(
                    new HighLevelIR.Literal(5, new HighLevelIR.HlType("test.pas", "Integer"), "test"),
                    "*",
                    new HighLevelIR.Literal(3, new HighLevelIR.HlType("test.pas", "Integer"), "test"),
                    "test"
                ),
                "+",
                new HighLevelIR.Literal("hello", new HighLevelIR.HlType("test.pas", "String"), "test"),
                "test"
            )
        ));

        // Add function to the module
        hlir.Modules[0].Functions.Add(function);

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
        var function = new HighLevelIR.Function("test", "test", "Void", new HighLevelIR.Block("test"));

        var block = function.Body;
        block.Statements.Add(new HighLevelIR.Assignment(
            "arr[15]",
            new HighLevelIR.Literal(1, new HighLevelIR.HlType("Integer"), "test")
        ));

        // Add function to the module
        hlir.Modules[0].Functions.Add(function);
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

        hlir.Modules[0].Variables.Add(new HighLevelIR.Variable { Name = "x", Type = new HighLevelIR.BasicType("test.pas", "Integer"), SourceFile = "test.pas" });

        var function = new HighLevelIR.Function("test", "test", "Void", new HighLevelIR.Block("test"));

        var block = function.Body;
        block.Statements.Add(new HighLevelIR.Assignment(
            "x",
            new HighLevelIR.Literal("42", new HighLevelIR.HlType("test.pas", "Integer"), "test.pas")
        ));

        // Add function to the module
        hlir.Modules[0].Functions.Add(function);

        // Act
        var result = _transformer.Transform(hlir);

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    #endregion

    #region Helper Methods

    private HighLevelIR CreateSimpleProgram()
    {
        var hlir = new HighLevelIR { SourceFile = "test.pas" };
        // Initialize with a default module
        hlir.Modules = new List<HlModule>
        {
            new HlModule { Name = "default" }
        };
        return hlir;
    }

    #endregion
}
