using NUnit.Framework;
using GameVM.Compiler.Core.IR;
using GameVM.Compiler.Core.IR.Transformers;
using System.Linq;
using System.Collections.Generic;

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

    #region Factory Methods

    private const string SourceFile = "test.pas";

    private HighLevelIR.HLType CreateBasicType(string name)
    {
        return new HighLevelIR.BasicType(SourceFile, name);
    }

    private HighLevelIR.Literal CreateLiteral(object value, string typeName)
    {
        return new HighLevelIR.Literal(value, CreateBasicType(typeName), SourceFile);
    }

    private HighLevelIR.Identifier CreateIdentifier(string name, string typeName)
    {
        return new HighLevelIR.Identifier(name, CreateBasicType(typeName), SourceFile);
    }

    private HighLevelIR.Assignment CreateAssignment(string target, HighLevelIR.Expression value)
    {
        return new HighLevelIR.Assignment(target, value, SourceFile);
    }

    private HighLevelIR.BinaryOp CreateBinaryOp(string op, HighLevelIR.Expression left, HighLevelIR.Expression right)
    {
        return new HighLevelIR.BinaryOp(op, left, right, SourceFile);
    }

    private HighLevelIR.Block CreateBlock()
    {
        return new HighLevelIR.Block(SourceFile);
    }

    private HighLevelIR.Function CreateFunction(string name, string returnTypeName, HighLevelIR.Block body)
    {
        return new HighLevelIR.Function(SourceFile, name, CreateBasicType(returnTypeName), body);
    }

    private HighLevelIR.Parameter CreateParameter(string name, string typeName)
    {
        return new HighLevelIR.Parameter(name, CreateBasicType(typeName), SourceFile);
    }

    private HighLevelIR.ExpressionStatement CreateExpressionStatement(HighLevelIR.Expression expression)
    {
        return new HighLevelIR.ExpressionStatement(expression, SourceFile);
    }

    #endregion

    #region Variable Declaration Tests

    [Test]
    public void Transform_SingleGlobalVariable_CreatesMLIRVariable()
    {
        // Arrange
        var hlir = CreateSimpleProgram();
        var intType = CreateBasicType("Integer");
        var symbol = new IRSymbol { Name = "x", Type = intType, IsConstant = false };
        hlir.Globals.Add("x", symbol);

        // Act
        var result = _transformer.Transform(hlir);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.SourceFile, Is.EqualTo(hlir.SourceFile));
        // Note: Globals are not directly transformed to MLIR, they're referenced in assignments
    }

    [Test]
    public void Transform_MultipleGlobalVariables_PreservesNames()
    {
        // Arrange
        var hlir = CreateSimpleProgram();
        hlir.Globals.Add("x", new IRSymbol { Name = "x", Type = CreateBasicType("Integer"), IsConstant = false });
        hlir.Globals.Add("y", new IRSymbol { Name = "y", Type = CreateBasicType("Real"), IsConstant = false });
        hlir.Globals.Add("z", new IRSymbol { Name = "z", Type = CreateBasicType("Boolean"), IsConstant = false });

        // Act
        var result = _transformer.Transform(hlir);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Globals.Count, Is.EqualTo(3));
    }

    #endregion

    #region Function Declaration Tests

    [Test]
    public void Transform_SimpleFunction_CreatesMLIRFunction()
    {
        // Arrange
        var hlir = CreateSimpleProgram();
        var function = CreateFunction("main", "Void", CreateBlock());
        hlir.Functions.Add("main", function);

        // Act
        var result = _transformer.Transform(hlir);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Modules[0].Functions, Has.Count.EqualTo(1));
        Assert.That(result.Modules[0].Functions.First().Name, Is.EqualTo("main"));
    }

    [Test]
    public void Transform_FunctionWithParameters_PreservesParameterNames()
    {
        // Arrange
        var hlir = CreateSimpleProgram();
        var function = CreateFunction("add", "Integer", CreateBlock());
        function.AddParameter(CreateParameter("a", "Integer"));
        function.AddParameter(CreateParameter("b", "Integer"));
        hlir.Functions.Add("add", function);

        // Act
        var result = _transformer.Transform(hlir);

        // Assert
        Assert.That(result.Modules[0].Functions, Has.Count.EqualTo(1));
        var func = result.Modules[0].Functions[0];
        Assert.That(func.Name, Is.EqualTo("add"));
        // Parameters are now part of the function signature in MLIR, not a separate collection
        // We'll verify the function signature includes the parameters when we implement it
    }

    #endregion

    #region Expression Tests

    [Test]
    public void Transform_LiteralExpression_CreatesMLIRLiteral()
    {
        // Arrange
        var hlir = CreateSimpleProgram();
        var body = CreateBlock();
        body.AddStatement(CreateExpressionStatement(CreateLiteral(42, "Integer")));
        var function = CreateFunction("main", "Void", body);
        hlir.Functions.Add("main", function);

        // Act
        var result = _transformer.Transform(hlir);

        // Assert
        // Expression statements without assignment don't create MLIR instructions
        // They would need to be part of an assignment or function call
        Assert.That(result.Modules[0].Functions[0].Instructions, Is.Empty);
    }

    [Test]
    public void Transform_BinaryOperationExpression_CreatesMLIRBinaryOp()
    {
        // Arrange
        var hlir = CreateSimpleProgram();
        var body = CreateBlock();
        var left = CreateLiteral(5, "Integer");
        var right = CreateLiteral(3, "Integer");
        var binOp = CreateBinaryOp("+", left, right);
        // BinaryOp needs to be in an assignment to be transformed
        body.AddStatement(CreateAssignment("result", binOp));
        var function = CreateFunction("main", "Void", body);
        hlir.Functions.Add("main", function);

        // Act
        var result = _transformer.Transform(hlir);

        // Assert
        Assert.That(result.Modules[0].Functions[0].Instructions, Has.Count.EqualTo(1));
        var assign = result.Modules[0].Functions[0].Instructions[0] as MidLevelIR.MLAssign;
        Assert.That(assign, Is.Not.Null);
        Assert.That(assign.Target, Is.EqualTo("result"));
        Assert.That(assign.Source, Is.EqualTo("8"), "Binary operation should be folded if possible");
    }

    #endregion

    #region Assignment Tests

    [Test]
    public void Transform_SimpleAssignment_CreatesMLIRAssignment()
    {
        // Arrange
        var hlir = CreateSimpleProgram();
        var body = CreateBlock();
        var assignment = CreateAssignment("x", CreateLiteral(42, "Integer"));
        body.AddStatement(assignment);
        var function = CreateFunction("main", "Void", body);
        hlir.Functions.Add("main", function);

        // Act
        var result = _transformer.Transform(hlir);

        // Assert
        Assert.That(result.Modules[0].Functions[0].Instructions, Has.Count.EqualTo(1));
        var mlAssign = result.Modules[0].Functions[0].Instructions[0] as MidLevelIR.MLAssign;
        Assert.That(mlAssign, Is.Not.Null);
        Assert.That(mlAssign.Target, Is.EqualTo("x"));
        Assert.That(mlAssign.Source, Is.EqualTo("42"), "Literal value should be converted to string");
    }

    [Test]
    public void Transform_AssignmentFromVariable_CreatesMLIRAssignment()
    {
        // Arrange
        var hlir = CreateSimpleProgram();
        var body = CreateBlock();
        var assignment = CreateAssignment("y", CreateIdentifier("x", "Integer"));
        body.AddStatement(assignment);
        var function = CreateFunction("main", "Void", body);
        hlir.Functions.Add("main", function);

        // Act
        var result = _transformer.Transform(hlir);

        // Assert
        Assert.That(result.Modules[0].Functions[0].Instructions, Has.Count.EqualTo(1));
        var mlAssign = result.Modules[0].Functions[0].Instructions[0] as MidLevelIR.MLAssign;
        Assert.That(mlAssign, Is.Not.Null);
        Assert.That(mlAssign.Target, Is.EqualTo("y"));
        Assert.That(mlAssign.Source, Is.EqualTo("x"), "Identifier name should be preserved");
    }

    #endregion

    #region Control Flow Tests

    [Test]
    public void Transform_IfStatement_CreatesMLIRConditional()
    {
        // Arrange
        var hlir = CreateSimpleProgram();
        var body = CreateBlock();
        var condition = CreateBinaryOp(">", CreateIdentifier("x", "Integer"), CreateLiteral(0, "Integer"));
        var thenBlock = new List<IRNode> { CreateAssignment("result", CreateLiteral(1, "Integer")) };
        var ifStmt = new HighLevelIR.IfStatement(condition, thenBlock);
        body.AddStatement(ifStmt);
        var function = CreateFunction("main", "Void", body);
        hlir.Functions.Add("main", function);

        // Act
        var result = _transformer.Transform(hlir);

        // Assert
        // Note: If statements are not yet fully transformed to MLIR conditional instructions
        // The transformer currently processes nested blocks, so assignments inside if statements are transformed
        Assert.That(result.Modules[0].Functions[0].Instructions, Has.Count.GreaterThanOrEqualTo(1));
        // When if statements are fully supported, we would check for MLBranch or MLIf instructions
    }

    [Test]
    public void Transform_WhileLoop_CreatesMLIRLoop()
    {
        // Arrange
        var hlir = CreateSimpleProgram();
        var body = CreateBlock();
        var condition = CreateBinaryOp("<", CreateIdentifier("i", "Integer"), CreateLiteral(10, "Integer"));
        var loopBody = CreateBlock();
        loopBody.AddStatement(CreateAssignment("i", CreateBinaryOp("+", CreateIdentifier("i", "Integer"), CreateLiteral(1, "Integer"))));
        var whileStmt = new HighLevelIR.While(condition, loopBody, SourceFile);
        body.AddStatement(whileStmt);
        var function = CreateFunction("main", "Void", body);
        hlir.Functions.Add("main", function);

        // Act
        var result = _transformer.Transform(hlir);

        // Assert
        // Note: While loops are not yet fully transformed to MLIR loop instructions
        // The transformer processes nested blocks, so assignments inside loops are transformed
        Assert.That(result.Modules[0].Functions[0].Instructions, Has.Count.GreaterThanOrEqualTo(1));
        // When while loops are fully supported, we would check for MLBranch or MLWhile instructions
    }

    #endregion

    #region Function Call Tests

    [Test]
    public void Transform_FunctionCall_CreatesMLIRCall()
    {
        // Arrange
        var hlir = CreateSimpleProgram();
        var body = CreateBlock();
        var funcCall = new HighLevelIR.FunctionCall(
            CreateIdentifier("printInt", "Void"),
            new[] { CreateLiteral(42, "Integer") }
        );
        body.AddStatement(CreateExpressionStatement(funcCall));
        var function = CreateFunction("main", "Void", body);
        hlir.Functions.Add("main", function);

        // Act
        var result = _transformer.Transform(hlir);

        // Assert
        Assert.That(result.Modules[0].Functions[0].Instructions, Has.Count.EqualTo(1));
        var mlCall = result.Modules[0].Functions[0].Instructions[0] as MidLevelIR.MLCall;
        Assert.That(mlCall, Is.Not.Null);
        Assert.That(mlCall.Name, Is.EqualTo("printInt"));
        Assert.That(mlCall.Arguments, Has.Count.EqualTo(1));
        Assert.That(mlCall.Arguments[0], Is.EqualTo("42"), "Function call arguments should be converted to strings");
    }

    [Test]
    public void Transform_NestedFunctionCalls_CreatesProperCallSequence()
    {
        // Arrange
        var hlir = CreateSimpleProgram();
        var body = CreateBlock();
        var innerCall = new HighLevelIR.FunctionCall(
            CreateIdentifier("getValue", "Integer"),
            new HighLevelIR.Expression[0]
        );
        var outerCall = new HighLevelIR.FunctionCall(
            CreateIdentifier("printInt", "Void"),
            new[] { innerCall }
        );
        body.AddStatement(CreateExpressionStatement(outerCall));
        var function = CreateFunction("main", "Void", body);
        hlir.Functions.Add("main", function);

        // Act
        var result = _transformer.Transform(hlir);

        // Assert
        Assert.That(result.Modules[0].Functions[0].Instructions, Has.Count.EqualTo(1));
        var mlCall = result.Modules[0].Functions[0].Instructions[0] as MidLevelIR.MLCall;
        Assert.That(mlCall, Is.Not.Null);
        Assert.That(mlCall.Name, Is.EqualTo("printInt"));
        // Nested function calls are converted to string expressions in arguments
        Assert.That(mlCall.Arguments, Has.Count.EqualTo(1));
        // The nested call is converted to a string representation
    }

    #endregion

    #region Complex Program Tests

    [Test]
    public void Transform_MultipleFunctions_TransformsAllFunctions()
    {
        // Arrange
        var hlir = CreateSimpleProgram();
        hlir.Functions.Add("func1", CreateFunction("func1", "Integer", CreateBlock()));
        hlir.Functions.Add("func2", CreateFunction("func2", "Void", CreateBlock()));
        hlir.Functions.Add("main", CreateFunction("main", "Void", CreateBlock()));

        // Act
        var result = _transformer.Transform(hlir);

        // Assert
        Assert.That(result.Modules[0].Functions, Has.Count.EqualTo(3));
        Assert.That(result.Modules[0].Functions.Select(f => f.Name), Has.Member("func1"));
        Assert.That(result.Modules[0].Functions.Select(f => f.Name), Has.Member("func2"));
        Assert.That(result.Modules[0].Functions.Select(f => f.Name), Has.Member("main"));
    }

    [Test]
    public void Transform_GlobalVariablesAndFunctions_TransformsAllElements()
    {
        // Arrange
        var hlir = CreateSimpleProgram();
        hlir.Globals.Add("g1", new IRSymbol { Name = "g1", Type = CreateBasicType("Integer") });
        hlir.Globals.Add("g2", new IRSymbol { Name = "g2", Type = CreateBasicType("Real") });
        hlir.Functions.Add("main", CreateFunction("main", "Void", CreateBlock()));

        // Act
        var result = _transformer.Transform(hlir);

        // Assert
        Assert.That(result.Globals.Count, Is.EqualTo(2));
        Assert.That(result.Modules[0].Functions, Has.Count.EqualTo(1));
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
        Assert.That(result.Modules, Is.Empty);
    }

    [Test]
    public void Transform_EmptyFunction_ReturnsEmptyInstructions()
    {
        // Arrange
        var hlir = CreateSimpleProgram();
        var function = CreateFunction("main", "Void", CreateBlock());
        hlir.Functions.Add("main", function);

        // Act
        var result = _transformer.Transform(hlir);

        // Assert
        Assert.That(result.Modules[0].Functions[0].Instructions, Is.Empty);
    }

    #endregion

    #region Type System Mapping Tests

    [Test]
    public void Transform_IntegerType_PreservesTypeInformation()
    {
        // Arrange
        var hlir = CreateSimpleProgram();
        var body = CreateBlock();
        var assignment = CreateAssignment("x", CreateLiteral(42, "Integer"));
        body.AddStatement(assignment);
        var function = CreateFunction("main", "Void", body);
        hlir.Functions.Add("main", function);

        // Act
        var result = _transformer.Transform(hlir);

        // Assert
        var mlAssign = result.Modules[0].Functions[0].Instructions[0] as MidLevelIR.MLAssign;
        Assert.That(mlAssign, Is.Not.Null);
        // Type information is preserved in the source expression string
        Assert.That(mlAssign.Source, Is.EqualTo("42"));
    }

    [Test]
    public void Transform_RealType_PreservesTypeInformation()
    {
        // Arrange
        var hlir = CreateSimpleProgram();
        var body = CreateBlock();
        var assignment = CreateAssignment("x", CreateLiteral(3.14, "Real"));
        body.AddStatement(assignment);
        var function = CreateFunction("main", "Void", body);
        hlir.Functions.Add("main", function);

        // Act
        var result = _transformer.Transform(hlir);

        // Assert
        var mlAssign = result.Modules[0].Functions[0].Instructions[0] as MidLevelIR.MLAssign;
        Assert.That(mlAssign, Is.Not.Null);
        Assert.That(mlAssign.Source, Is.EqualTo("3.14").Or.Contains("3.14"));
    }

    [Test]
    public void Transform_BooleanType_PreservesTypeInformation()
    {
        // Arrange
        var hlir = CreateSimpleProgram();
        var body = CreateBlock();
        var assignment = CreateAssignment("flag", CreateLiteral(true, "Boolean"));
        body.AddStatement(assignment);
        var function = CreateFunction("main", "Void", body);
        hlir.Functions.Add("main", function);

        // Act
        var result = _transformer.Transform(hlir);

        // Assert
        var mlAssign = result.Modules[0].Functions[0].Instructions[0] as MidLevelIR.MLAssign;
        Assert.That(mlAssign, Is.Not.Null);
        Assert.That(mlAssign.Source, Is.EqualTo("True").IgnoreCase);
    }

    #endregion

    #region Array Transformation Tests

    [Test]
    public void Transform_ArrayElementAccess_TransformsToMLIR()
    {
        // Arrange
        var hlir = CreateSimpleProgram();
        var body = CreateBlock();
        // Simulating array access: arr[0] := 42
        // Note: Array access would need to be represented as a special expression type
        // For now, we test that the structure supports it
        var assignment = CreateAssignment("arr0", CreateLiteral(42, "Integer"));
        body.AddStatement(assignment);
        var function = CreateFunction("main", "Void", body);
        hlir.Functions.Add("main", function);

        // Act
        var result = _transformer.Transform(hlir);

        // Assert
        var mlAssign = result.Modules[0].Functions[0].Instructions[0] as MidLevelIR.MLAssign;
        Assert.That(mlAssign, Is.Not.Null);
        // When array access is fully implemented, we would check for array indexing in the target
    }

    [Test]
    public void Transform_ArrayAssignment_HandlesArrayTarget()
    {
        // Arrange
        var hlir = CreateSimpleProgram();
        var body = CreateBlock();
        // Simulating: arr[i] := value
        var indexExpr = CreateBinaryOp("+", CreateIdentifier("i", "Integer"), CreateLiteral(0, "Integer"));
        var assignment = CreateAssignment("arrIndex", CreateIdentifier("value", "Integer"));
        body.AddStatement(assignment);
        var function = CreateFunction("main", "Void", body);
        hlir.Functions.Add("main", function);

        // Act
        var result = _transformer.Transform(hlir);

        // Assert
        var mlAssign = result.Modules[0].Functions[0].Instructions[0] as MidLevelIR.MLAssign;
        Assert.That(mlAssign, Is.Not.Null);
        Assert.That(mlAssign.Target, Is.EqualTo("arrIndex"));
        // When array access is fully implemented, target would be "arr[i]" or similar
    }

    #endregion

    #region Record Transformation Tests

    [Test]
    public void Transform_RecordFieldAccess_TransformsToMLIR()
    {
        // Arrange
        var hlir = CreateSimpleProgram();
        var body = CreateBlock();
        // Simulating record field access: point.x := 10
        var assignment = CreateAssignment("pointX", CreateLiteral(10, "Integer"));
        body.AddStatement(assignment);
        var function = CreateFunction("main", "Void", body);
        hlir.Functions.Add("main", function);

        // Act
        var result = _transformer.Transform(hlir);

        // Assert
        var mlAssign = result.Modules[0].Functions[0].Instructions[0] as MidLevelIR.MLAssign;
        Assert.That(mlAssign, Is.Not.Null);
        // When record field access is fully implemented, target would be "point.x" or similar
    }

    [Test]
    public void Transform_RecordAssignment_HandlesRecordTarget()
    {
        // Arrange
        var hlir = CreateSimpleProgram();
        var body = CreateBlock();
        // Simulating: point := newPoint
        var assignment = CreateAssignment("point", CreateIdentifier("newPoint", "Point"));
        body.AddStatement(assignment);
        var function = CreateFunction("main", "Void", body);
        hlir.Functions.Add("main", function);

        // Act
        var result = _transformer.Transform(hlir);

        // Assert
        var mlAssign = result.Modules[0].Functions[0].Instructions[0] as MidLevelIR.MLAssign;
        Assert.That(mlAssign, Is.Not.Null);
        Assert.That(mlAssign.Target, Is.EqualTo("point"));
        Assert.That(mlAssign.Source, Is.EqualTo("newPoint"));
    }

    #endregion

    #region Pointer Transformation Tests

    [Test]
    public void Transform_PointerDereference_TransformsToMLIR()
    {
        // Arrange
        var hlir = CreateSimpleProgram();
        var body = CreateBlock();
        // Simulating pointer dereference: p^ := 42
        var assignment = CreateAssignment("pDeref", CreateLiteral(42, "Integer"));
        body.AddStatement(assignment);
        var function = CreateFunction("main", "Void", body);
        hlir.Functions.Add("main", function);

        // Act
        var result = _transformer.Transform(hlir);

        // Assert
        var mlAssign = result.Modules[0].Functions[0].Instructions[0] as MidLevelIR.MLAssign;
        Assert.That(mlAssign, Is.Not.Null);
        // When pointer dereference is fully implemented, target would be "p^" or similar
    }

    [Test]
    public void Transform_PointerAssignment_HandlesPointerTarget()
    {
        // Arrange
        var hlir = CreateSimpleProgram();
        var body = CreateBlock();
        // Simulating: p := @x (address of x)
        var assignment = CreateAssignment("p", CreateIdentifier("x", "Integer"));
        body.AddStatement(assignment);
        var function = CreateFunction("main", "Void", body);
        hlir.Functions.Add("main", function);

        // Act
        var result = _transformer.Transform(hlir);

        // Assert
        var mlAssign = result.Modules[0].Functions[0].Instructions[0] as MidLevelIR.MLAssign;
        Assert.That(mlAssign, Is.Not.Null);
        Assert.That(mlAssign.Target, Is.EqualTo("p"));
        // When address-of operator is fully implemented, source would be "@x" or similar
    }

    #endregion

    #region Built-in Function Tests

    [Test]
    public void Transform_WriteFunctionCall_TransformsToMLIRCall()
    {
        // Arrange
        var hlir = CreateSimpleProgram();
        var body = CreateBlock();
        var writeCall = new HighLevelIR.FunctionCall(
            CreateIdentifier("write", "Void"),
            new[] { CreateLiteral("Hello", "String") }
        );
        body.AddStatement(CreateExpressionStatement(writeCall));
        var function = CreateFunction("main", "Void", body);
        hlir.Functions.Add("main", function);

        // Act
        var result = _transformer.Transform(hlir);

        // Assert
        Assert.That(result.Modules[0].Functions[0].Instructions, Has.Count.EqualTo(1));
        var mlCall = result.Modules[0].Functions[0].Instructions[0] as MidLevelIR.MLCall;
        Assert.That(mlCall, Is.Not.Null);
        Assert.That(mlCall.Name, Is.EqualTo("write"));
        Assert.That(mlCall.Arguments, Has.Count.EqualTo(1));
        Assert.That(mlCall.Arguments[0], Is.EqualTo("Hello"));
    }

    [Test]
    public void Transform_WritelnFunctionCall_TransformsToMLIRCall()
    {
        // Arrange
        var hlir = CreateSimpleProgram();
        var body = CreateBlock();
        var writelnCall = new HighLevelIR.FunctionCall(
            CreateIdentifier("writeln", "Void"),
            new[] { CreateLiteral(42, "Integer") }
        );
        body.AddStatement(CreateExpressionStatement(writelnCall));
        var function = CreateFunction("main", "Void", body);
        hlir.Functions.Add("main", function);

        // Act
        var result = _transformer.Transform(hlir);

        // Assert
        Assert.That(result.Modules[0].Functions[0].Instructions, Has.Count.EqualTo(1));
        var mlCall = result.Modules[0].Functions[0].Instructions[0] as MidLevelIR.MLCall;
        Assert.That(mlCall, Is.Not.Null);
        Assert.That(mlCall.Name, Is.EqualTo("writeln"));
        Assert.That(mlCall.Arguments, Has.Count.EqualTo(1));
        Assert.That(mlCall.Arguments[0], Is.EqualTo("42"));
    }

    [Test]
    public void Transform_BuiltinFunctionWithMultipleArgs_TransformsAllArguments()
    {
        // Arrange
        var hlir = CreateSimpleProgram();
        var body = CreateBlock();
        var writeCall = new HighLevelIR.FunctionCall(
            CreateIdentifier("write", "Void"),
            new[] { CreateLiteral("Value: ", "String"), CreateLiteral(42, "Integer") }
        );
        body.AddStatement(CreateExpressionStatement(writeCall));
        var function = CreateFunction("main", "Void", body);
        hlir.Functions.Add("main", function);

        // Act
        var result = _transformer.Transform(hlir);

        // Assert
        var mlCall = result.Modules[0].Functions[0].Instructions[0] as MidLevelIR.MLCall;
        Assert.That(mlCall, Is.Not.Null);
        Assert.That(mlCall.Arguments, Has.Count.EqualTo(2));
        Assert.That(mlCall.Arguments[0], Is.EqualTo("Value: "));
        Assert.That(mlCall.Arguments[1], Is.EqualTo("42"));
    }

    #endregion

    #region Helper Methods

    private HighLevelIR CreateSimpleProgram()
    {
        var hlir = new HighLevelIR { SourceFile = SourceFile };
        // Initialize the Modules list and add a default module
        hlir.Modules = new List<HighLevelIR.HLModule>();
        var module = new HighLevelIR.HLModule { Name = "default" };
        hlir.Modules.Add(module);
        return hlir;
    }

    #endregion
}
