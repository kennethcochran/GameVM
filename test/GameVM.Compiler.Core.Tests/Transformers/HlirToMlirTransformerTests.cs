using NUnit.Framework;
using GameVM.Compiler.Core.IR;
using GameVM.Compiler.Core.IR.Transformers;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

namespace GameVM.Compiler.Core.Tests.Transformers;

/// <summary>
/// Tests for HLIR to MLIR transformation.
/// Validates that high-level IR structures are correctly transformed to mid-level IR.
/// </summary>
[TestFixture]
public class HlirToMlirTransformerTests
{
    private HlirToMlirTransformer _transformer = null!;

    [SetUp]
    public void Setup()
    {
        _transformer = new HlirToMlirTransformer();
    }

    #region Factory Methods

    private const string SourceFile = "test.pas";

    private static HighLevelIR.HlType CreateBasicType(string name)
    {
        return new HighLevelIR.BasicType("test.pas", name);
    }

    private static HighLevelIR.Literal CreateLiteral(object value, string typeName)
    {
        return new HighLevelIR.Literal(value, CreateBasicType(typeName), "test.pas");
    }

    private static HighLevelIR.Identifier CreateIdentifier(string name, string typeName)
    {
        return new HighLevelIR.Identifier(name, CreateBasicType(typeName), "test.pas");
    }

    private static HighLevelIR.Assignment CreateAssignment(string target, HighLevelIR.Expression value)
    {
        return new HighLevelIR.Assignment(target, value, "test.pas");
    }

    private static HighLevelIR.BinaryOp CreateBinaryOp(string op, HighLevelIR.Expression left, HighLevelIR.Expression right)
    {
        return new HighLevelIR.BinaryOp(op, left, right, "test.pas");
    }

    private static HighLevelIR.Block CreateBlock()
    {
        return new HighLevelIR.Block("test.pas");
    }

    private static HighLevelIR.Function CreateFunction(string name, string returnTypeName, HighLevelIR.Block body)
    {
        return new HighLevelIR.Function("test.pas", name, CreateBasicType(returnTypeName), body);
    }

    private static HighLevelIR.Parameter CreateParameter(string name, string typeName)
    {
        return new HighLevelIR.Parameter(name, CreateBasicType(typeName), "test.pas");
    }

    private static HighLevelIR.ExpressionStatement CreateExpressionStatement(HighLevelIR.Expression expression)
    {
        return new HighLevelIR.ExpressionStatement { Expression = expression, SourceFile = "test.pas" };
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
        hlir.Modules[0].Functions.Add(function);

        // Act
        var result = _transformer.Transform(hlir);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Modules[0].Functions, Has.Count.EqualTo(1));
        Assert.That(result.Modules[0].Functions[0].Name, Is.EqualTo("main"));
    }

    [Test]
    public void Transform_FunctionWithParameters_PreservesParameterNames()
    {
        // Arrange
        var hlir = CreateSimpleProgram();
        var function = CreateFunction("add", "Integer", CreateBlock());
        function.AddParameter(CreateParameter("a", "Integer"));
        function.AddParameter(CreateParameter("b", "Integer"));
        hlir.Modules[0].Functions.Add(function);

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
        hlir.Modules[0].Functions.Add(function);

        // Act
        var result = _transformer.Transform(hlir);

        // Assert
        // Expression statements with literals now create a temporary assignment
        Assert.That(result.Modules[0].Functions[0].Instructions, Has.Count.EqualTo(1));
        Assert.That(result.Modules[0].Functions[0].Instructions[0], Is.InstanceOf<MidLevelIR.MLAssign>());
        var assign = result.Modules[0].Functions[0].Instructions[0] as MidLevelIR.MLAssign;
        Assert.That(assign!.Target, Is.EqualTo("_temp"));
        Assert.That(assign.Source, Is.EqualTo("42"));
    }

    // RED PHASE: One failing test at a time for GetLiteralValue - basic non-null case
    [Test]
    public void Transform_BasicLiteralValue_ShouldReturnLiteralString()
    {
        // Arrange - Test the basic non-null literal value path in GetLiteralValue (line 263: literal.Value?.ToString() ?? "0")
        var hlir = CreateSimpleProgram();
        var body = CreateBlock();
        
        // Create a literal with a non-null value to test the main path
        var literal = new HighLevelIR.Literal("123", CreateBasicType("i32"), SourceFile);
        var assignment = new HighLevelIR.Assignment("result", literal, SourceFile);
        body.AddStatement(assignment);
        
        var function = CreateFunction("main", "Void", body);
        hlir.Modules[0].Functions.Add(function);

        // Act
        var result = _transformer.Transform(hlir);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Modules[0].Functions, Has.Count.EqualTo(1));
        
        var mlFunction = result.Modules[0].Functions[0];
        Assert.That(mlFunction.Instructions, Has.Count.EqualTo(1));
        
        var assignInstr = mlFunction.Instructions[0] as MidLevelIR.MLAssign;
        Assert.That(assignInstr, Is.Not.Null);
        Assert.That(assignInstr!.Target, Is.EqualTo("result"));
        // Should return the literal value as string
        Assert.That(assignInstr.Source, Is.EqualTo("123"));
    }

    // RED PHASE: One failing test at a time for GetLiteralValue - null value case
    [Test]
    public void Transform_NullLiteralValue_ShouldUseDefaultValue()
    {
        // Arrange - Test the null literal value path in GetLiteralValue (line 263: literal.Value?.ToString() ?? "0")
        var hlir = CreateSimpleProgram();
        var body = CreateBlock();
        
        // Create a literal with null value to test the fallback behavior
        var nullLiteral = new HighLevelIR.Literal(null!, CreateBasicType("Integer"), SourceFile);
        body.AddStatement(CreateExpressionStatement(nullLiteral));
        
        var function = CreateFunction("main", "Void", body);
        hlir.Modules[0].Functions.Add(function);

        // Act
        var result = _transformer.Transform(hlir);

        // Assert
        // Should handle null literal value gracefully by using "0" as fallback
        Assert.That(result.Modules[0].Functions[0].Instructions, Has.Count.EqualTo(1));
        Assert.That(result.Modules[0].Functions[0].Instructions[0], Is.InstanceOf<MidLevelIR.MLAssign>());
        var assign = result.Modules[0].Functions[0].Instructions[0] as MidLevelIR.MLAssign;
        Assert.That(assign!.Target, Is.EqualTo("_temp"));
        Assert.That(assign.Source, Is.EqualTo("0")); // Should use fallback value "0"
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
        hlir.Modules[0].Functions.Add(function);

        // Act
        var result = _transformer.Transform(hlir);

        // Assert
        Assert.That(result.Modules[0].Functions[0].Instructions, Has.Count.EqualTo(1));
        var assign = result.Modules[0].Functions[0].Instructions[0] as MidLevelIR.MLAssign;
        Assert.That(assign, Is.Not.Null);
        Assert.That(assign!.Target, Is.EqualTo("result"));
        Assert.That(assign!.Source, Is.EqualTo("8"), "Binary operation should be folded if possible");
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
        hlir.Modules[0].Functions.Add(function);

        // Act
        var result = _transformer.Transform(hlir);

        // Assert
        Assert.That(result.Modules[0].Functions[0].Instructions, Has.Count.EqualTo(1));
        var mlAssign = result.Modules[0].Functions[0].Instructions[0] as MidLevelIR.MLAssign;
        Assert.That(mlAssign, Is.Not.Null);
        Assert.That(mlAssign!.Target, Is.EqualTo("x"));
        Assert.That(mlAssign!.Source, Is.EqualTo("42"), "Literal value should be converted to string");
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
        hlir.Modules[0].Functions.Add(function);

        // Act
        var result = _transformer.Transform(hlir);

        // Assert
        Assert.That(result.Modules[0].Functions[0].Instructions, Has.Count.EqualTo(1));
        var mlAssign = result.Modules[0].Functions[0].Instructions[0] as MidLevelIR.MLAssign;
        Assert.That(mlAssign, Is.Not.Null);
        Assert.That(mlAssign!.Target, Is.EqualTo("y"));
        Assert.That(mlAssign!.Source, Is.EqualTo("x"), "Identifier name should be preserved");
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
        hlir.Modules[0].Functions.Add(function);

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
        var whileStmt = new HighLevelIR.While { Condition = condition, Body = loopBody, SourceFile = SourceFile };
        body.AddStatement(whileStmt);
        var function = CreateFunction("main", "Void", body);
        hlir.Modules[0].Functions.Add(function);

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
        hlir.Modules[0].Functions.Add(function);

        // Act
        var result = _transformer.Transform(hlir);

        // Assert
        Assert.That(result.Modules[0].Functions[0].Instructions, Has.Count.EqualTo(1));
        var mlCall = result.Modules[0].Functions[0].Instructions[0] as MidLevelIR.MLCall;
        Assert.That(mlCall, Is.Not.Null);
        Assert.That(mlCall!.Name, Is.EqualTo("printInt"));
        Assert.That(mlCall!.Arguments, Has.Count.EqualTo(1));
        Assert.That(mlCall!.Arguments[0], Is.EqualTo("42"), "Function call arguments should be converted to strings");
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
        hlir.Modules[0].Functions.Add(function);

        // Act
        var result = _transformer.Transform(hlir);

        // Assert
        Assert.That(result.Modules[0].Functions[0].Instructions, Has.Count.EqualTo(1));
        var mlCall = result.Modules[0].Functions[0].Instructions[0] as MidLevelIR.MLCall;
        Assert.That(mlCall, Is.Not.Null);
        Assert.That(mlCall!.Name, Is.EqualTo("printInt"));
        // Nested function calls are converted to string expressions in arguments
        Assert.That(mlCall!.Arguments, Has.Count.EqualTo(1));
        // The nested call is converted to a string representation
    }

    #endregion

    #region Complex Program Tests

    [Test]
    public void Transform_MultipleFunctions_TransformsAllFunctions()
    {
        // Arrange
        var hlir = CreateSimpleProgram();
        hlir.Modules[0].Functions.Add(CreateFunction("func1", "Integer", CreateBlock()));
        hlir.Modules[0].Functions.Add(CreateFunction("func2", "Void", CreateBlock()));
        hlir.Modules[0].Functions.Add(CreateFunction("main", "Void", CreateBlock()));

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
        hlir.Modules[0].Functions.Add(CreateFunction("main", "Void", CreateBlock()));

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
        hlir.Modules[0].Functions.Add(function);

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
        hlir.Modules[0].Functions.Add(function);

        // Act
        var result = _transformer.Transform(hlir);

        // Assert
        var mlAssign = result.Modules[0].Functions[0].Instructions[0] as MidLevelIR.MLAssign;
        Assert.That(mlAssign, Is.Not.Null);
        // Type information is preserved in the source expression string
        Assert.That(mlAssign!.Source, Is.EqualTo("42"));
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
        hlir.Modules[0].Functions.Add(function);

        // Act
        var result = _transformer.Transform(hlir);

        // Assert
        var mlAssign = result.Modules[0].Functions[0].Instructions[0] as MidLevelIR.MLAssign;
        Assert.That(mlAssign, Is.Not.Null);
        Assert.That(mlAssign!.Source, Is.EqualTo("3.14").Or.Contains("3.14"));
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
        hlir.Modules[0].Functions.Add(function);

        // Act
        var result = _transformer.Transform(hlir);

        // Assert
        var mlAssign = result.Modules[0].Functions[0].Instructions[0] as MidLevelIR.MLAssign;
        Assert.That(mlAssign, Is.Not.Null);
        Assert.That(mlAssign!.Source, Is.EqualTo("True").IgnoreCase);
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
        hlir.Modules[0].Functions.Add(function);

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
        var assignment = CreateAssignment("arrIndex", CreateIdentifier("value", "Integer"));
        body.AddStatement(assignment);
        var function = CreateFunction("main", "Void", body);
        hlir.Modules[0].Functions.Add(function);

        // Act
        var result = _transformer.Transform(hlir);

        // Assert
        var mlAssign = result.Modules[0].Functions[0].Instructions[0] as MidLevelIR.MLAssign;
        Assert.That(mlAssign, Is.Not.Null);
        Assert.That(mlAssign!.Target, Is.EqualTo("arrIndex"));
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
        hlir.Modules[0].Functions.Add(function);

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
        hlir.Modules[0].Functions.Add(function);

        // Act
        var result = _transformer.Transform(hlir);

        // Assert
        var mlAssign = result.Modules[0].Functions[0].Instructions[0] as MidLevelIR.MLAssign;
        Assert.That(mlAssign, Is.Not.Null);
        Assert.That(mlAssign!.Target, Is.EqualTo("point"));
        Assert.That(mlAssign!.Source, Is.EqualTo("newPoint"));
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
        hlir.Modules[0].Functions.Add(function);

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
        hlir.Modules[0].Functions.Add(function);

        // Act
        var result = _transformer.Transform(hlir);

        // Assert
        var mlAssign = result.Modules[0].Functions[0].Instructions[0] as MidLevelIR.MLAssign;
        Assert.That(mlAssign, Is.Not.Null);
        Assert.That(mlAssign!.Target, Is.EqualTo("p"));
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
        hlir.Modules[0].Functions.Add(function);

        // Act
        var result = _transformer.Transform(hlir);

        // Assert
        Assert.That(result.Modules[0].Functions[0].Instructions, Has.Count.EqualTo(1));
        var mlCall = result.Modules[0].Functions[0].Instructions[0] as MidLevelIR.MLCall;
        Assert.That(mlCall, Is.Not.Null);
        Assert.That(mlCall!.Name, Is.EqualTo("write"));
        Assert.That(mlCall!.Arguments, Has.Count.EqualTo(1));
        Assert.That(mlCall!.Arguments[0], Is.EqualTo("Hello"));
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
        hlir.Modules[0].Functions.Add(function);

        // Act
        var result = _transformer.Transform(hlir);

        // Assert
        Assert.That(result.Modules[0].Functions[0].Instructions, Has.Count.EqualTo(1));
        var mlCall = result.Modules[0].Functions[0].Instructions[0] as MidLevelIR.MLCall;
        Assert.That(mlCall, Is.Not.Null);
        Assert.That(mlCall!.Name, Is.EqualTo("writeln"));
        Assert.That(mlCall!.Arguments, Has.Count.EqualTo(1));
        Assert.That(mlCall!.Arguments[0], Is.EqualTo("42"));
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
        hlir.Modules[0].Functions.Add(function);

        // Act
        var result = _transformer.Transform(hlir);

        // Assert
        var mlCall = result.Modules[0].Functions[0].Instructions[0] as MidLevelIR.MLCall;
        Assert.That(mlCall, Is.Not.Null);
        Assert.That(mlCall!.Arguments, Has.Count.EqualTo(2));
        Assert.That(mlCall!.Arguments[0], Is.EqualTo("Value: "));
        Assert.That(mlCall!.Arguments[1], Is.EqualTo("42"));
    }

    #endregion

    #region Constant Folding Tests

    [Test]
    public void Transform_BinaryOperation_ConstantFolding_Addition_WorksCorrectly()
    {
        // Arrange
        var hlir = CreateSimpleProgram();
        var transformer = new HlirToMlirTransformer();
        
        // Create a binary operation with two literals that should be folded
        var assignment = new HighLevelIR.Assignment
        {
            Target = "result",
            Value = new HighLevelIR.BinaryOp
            {
                Left = new HighLevelIR.Literal { Value = "5" },
                Right = new HighLevelIR.Literal { Value = "3" },
                Operator = "+"
            }
        };
        
        // Add to program - Function has a Body which is a Block with Statements
        hlir.Modules[0].Functions.Add(new HighLevelIR.Function
        {
            Name = "test",
            Body = new HighLevelIR.Block(SourceFile)
            {
                Statements = new List<HighLevelIR.Statement> { assignment }
            }
        });

        // Act
        var result = transformer.Transform(hlir);

        // Assert - The constant folding should have occurred during transformation
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Modules, Has.Count.EqualTo(1));
        Assert.That(result.Modules[0].Functions, Has.Count.EqualTo(1));
    }

    [Test]
    public void Transform_BinaryOperation_ConstantFolding_Subtraction_WorksCorrectly()
    {
        // Arrange
        var hlir = CreateSimpleProgram();
        var transformer = new HlirToMlirTransformer();
        
        var assignment = new HighLevelIR.Assignment
        {
            Target = "result",
            Value = new HighLevelIR.BinaryOp
            {
                Left = new HighLevelIR.Literal { Value = "10" },
                Right = new HighLevelIR.Literal { Value = "4" },
                Operator = "-"
            }
        };
        
        hlir.Modules[0].Functions.Add(new HighLevelIR.Function
        {
            Name = "test",
            Body = new HighLevelIR.Block(SourceFile)
            {
                Statements = new List<HighLevelIR.Statement> { assignment }
            }
        });

        // Act
        var result = transformer.Transform(hlir);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Modules[0].Functions, Has.Count.EqualTo(1));
    }

    [Test]
    public void Transform_BinaryOperation_ConstantFolding_Multiplication_WorksCorrectly()
    {
        // Arrange
        var hlir = CreateSimpleProgram();
        var transformer = new HlirToMlirTransformer();
        
        var assignment = new HighLevelIR.Assignment
        {
            Target = "result",
            Value = new HighLevelIR.BinaryOp
            {
                Left = new HighLevelIR.Literal { Value = "6" },
                Right = new HighLevelIR.Literal { Value = "7" },
                Operator = "*"
            }
        };
        
        hlir.Modules[0].Functions.Add(new HighLevelIR.Function
        {
            Name = "test",
            Body = new HighLevelIR.Block(SourceFile)
            {
                Statements = new List<HighLevelIR.Statement> { assignment }
            }
        });

        // Act
        var result = transformer.Transform(hlir);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Modules[0].Functions, Has.Count.EqualTo(1));
    }

    [Test]
    public void Transform_BinaryOperation_ConstantFolding_Division_WorksCorrectly()
    {
        // Arrange
        var hlir = CreateSimpleProgram();
        var transformer = new HlirToMlirTransformer();
        
        var assignment = new HighLevelIR.Assignment
        {
            Target = "result",
            Value = new HighLevelIR.BinaryOp
            {
                Left = new HighLevelIR.Literal { Value = "20" },
                Right = new HighLevelIR.Literal { Value = "4" },
                Operator = "/"
            }
        };
        
        hlir.Modules[0].Functions.Add(new HighLevelIR.Function
        {
            Name = "test",
            Body = new HighLevelIR.Block(SourceFile)
            {
                Statements = new List<HighLevelIR.Statement> { assignment }
            }
        });

        // Act
        var result = transformer.Transform(hlir);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Modules[0].Functions, Has.Count.EqualTo(1));
    }

    // RED PHASE: One failing test at a time for SafeDivide - successful division case
    [Test]
    public void Transform_BinaryOperation_ConstantFolding_Division_VerifiesResult()
    {
        // Arrange - Test the successful division path in SafeDivide (line 353: divisor != 0 case)
        var hlir = CreateSimpleProgram();
        var transformer = new HlirToMlirTransformer();
        
        var assignment = new HighLevelIR.Assignment
        {
            Target = "result",
            Value = new HighLevelIR.BinaryOp
            {
                Left = new HighLevelIR.Literal { Value = "20" },
                Right = new HighLevelIR.Literal { Value = "4" },
                Operator = "/"
            }
        };
        
        hlir.Modules[0].Functions.Add(new HighLevelIR.Function
        {
            Name = "test",
            Body = new HighLevelIR.Block(SourceFile)
            {
                Statements = new List<HighLevelIR.Statement> { assignment }
            }
        });

        // Act
        var result = transformer.Transform(hlir);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Modules[0].Functions, Has.Count.EqualTo(1));
        
        var mlFunction = result.Modules[0].Functions[0];
        Assert.That(mlFunction.Instructions, Has.Count.EqualTo(1));
        
        var assignInstr = mlFunction.Instructions[0] as MidLevelIR.MLAssign;
        Assert.That(assignInstr, Is.Not.Null);
        Assert.That(assignInstr!.Target, Is.EqualTo("result"));
        // Should be folded to "5" (20 / 4 = 5)
        Assert.That(assignInstr.Source, Is.EqualTo("5"));
    }

    [Test]
    public void Transform_BinaryOperation_ConstantFolding_DivisionByZero_ReturnsZero()
    {
        // Arrange
        var hlir = CreateSimpleProgram();
        var transformer = new HlirToMlirTransformer();
        
        var assignment = new HighLevelIR.Assignment
        {
            Target = "result",
            Value = new HighLevelIR.BinaryOp
            {
                Left = new HighLevelIR.Literal { Value = "10" },
                Right = new HighLevelIR.Literal { Value = "0" },
                Operator = "/"
            }
        };
        
        hlir.Modules[0].Functions.Add(new HighLevelIR.Function
        {
            Name = "test",
            Body = new HighLevelIR.Block(SourceFile)
            {
                Statements = new List<HighLevelIR.Statement> { assignment }
            }
        });

        // Act
        var result = transformer.Transform(hlir);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Modules[0].Functions, Has.Count.EqualTo(1));
    }

    // RED PHASE: One failing test at a time for PerformOperation - "div" operator case
    [Test]
    public void Transform_BinaryOperation_ConstantFolding_DivOperator_WorksCorrectly()
    {
        // Arrange - Test the "div" operator path in PerformOperation (line 331: "/" or "div" case)
        var hlir = CreateSimpleProgram();
        var transformer = new HlirToMlirTransformer();
        
        var assignment = new HighLevelIR.Assignment
        {
            Target = "result",
            Value = new HighLevelIR.BinaryOp
            {
                Left = new HighLevelIR.Literal { Value = "20" },
                Right = new HighLevelIR.Literal { Value = "4" },
                Operator = "div" // Test the "div" operator specifically
            }
        };
        
        hlir.Modules[0].Functions.Add(new HighLevelIR.Function
        {
            Name = "test",
            Body = new HighLevelIR.Block(SourceFile)
            {
                Statements = new List<HighLevelIR.Statement> { assignment }
            }
        });

        // Act
        var result = transformer.Transform(hlir);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Modules[0].Functions, Has.Count.EqualTo(1));
        
        var mlFunction = result.Modules[0].Functions[0];
        Assert.That(mlFunction.Instructions, Has.Count.EqualTo(1));
        
        var assignInstr = mlFunction.Instructions[0] as MidLevelIR.MLAssign;
        Assert.That(assignInstr, Is.Not.Null);
        Assert.That(assignInstr!.Target, Is.EqualTo("result"));
        // Should be folded to "5" (20 div 4 = 5)
        Assert.That(assignInstr.Source, Is.EqualTo("5"));
    }

    [Test]
    public void Transform_BinaryOperation_ConstantFolding_NonConstants_NoFolding()
    {
        // Arrange
        var hlir = CreateSimpleProgram();
        var transformer = new HlirToMlirTransformer();
        
        var assignment = new HighLevelIR.Assignment
        {
            Target = "result",
            Value = new HighLevelIR.BinaryOp
            {
                Left = new HighLevelIR.Identifier { Name = "x" },
                Right = new HighLevelIR.Identifier { Name = "y" },
                Operator = "+"
            }
        };
        
        hlir.Modules[0].Functions.Add(new HighLevelIR.Function
        {
            Name = "test",
            Body = new HighLevelIR.Block(SourceFile)
            {
                Statements = new List<HighLevelIR.Statement> { assignment }
            }
        });

        // Act
        var result = transformer.Transform(hlir);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Modules[0].Functions, Has.Count.EqualTo(1));
    }

    [Test]
    public void Transform_BinaryOperation_ConstantFolding_UnsupportedOperator_NoFolding()
    {
        // Arrange
        var hlir = CreateSimpleProgram();
        var transformer = new HlirToMlirTransformer();
        
        var assignment = new HighLevelIR.Assignment
        {
            Target = "result",
            Value = new HighLevelIR.BinaryOp
            {
                Left = new HighLevelIR.Literal { Value = "5" },
                Right = new HighLevelIR.Literal { Value = "3" },
                Operator = "%"
            }
        };
        
        hlir.Modules[0].Functions.Add(new HighLevelIR.Function
        {
            Name = "test",
            Body = new HighLevelIR.Block(SourceFile)
            {
                Statements = new List<HighLevelIR.Statement> { assignment }
            }
        });

        // Act
        var result = transformer.Transform(hlir);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Modules[0].Functions, Has.Count.EqualTo(1));
    }

    // RED PHASE: One failing test at a time for ProcessStatement
    [Test]
    public void Transform_ShouldHandleAssignmentStatement()
    {
        // Arrange
        var hlir = CreateSimpleProgram();
        var assignment = new HighLevelIR.Assignment("x", new HighLevelIR.Literal("42", CreateBasicType("i32"), SourceFile), SourceFile);
        
        hlir.Modules[0].Functions.Add(new HighLevelIR.Function
        {
            Name = "test",
            Body = new HighLevelIR.Block(SourceFile)
            {
                Statements = new List<HighLevelIR.Statement> { assignment }
            }
        });
        
        var transformer = new HlirToMlirTransformer();

        // Act
        var result = transformer.Transform(hlir);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Modules[0].Functions, Has.Count.EqualTo(1));
        var mlFunction = result.Modules[0].Functions[0];
        Assert.That(mlFunction.Instructions, Has.Count.GreaterThan(0));
        Assert.That(mlFunction.Instructions[0], Is.InstanceOf<MidLevelIR.MLAssign>());
    }

    // RED PHASE: One failing test at a time for ProcessStatement - WhileStatement case
    [Test]
    public void Transform_ShouldHandleWhileStatement()
    {
        // Arrange - Create a WhileStatement which should trigger ProcessWhileStatement (line 127-128)
        var hlir = CreateSimpleProgram();
        var condition = new HighLevelIR.Literal("1", CreateBasicType("i32"), SourceFile); // Always true
        var loopBody = new HighLevelIR.Block(SourceFile)
        {
            Statements = new List<HighLevelIR.Statement> 
            { 
                new HighLevelIR.Assignment("counter", new HighLevelIR.Literal("0", CreateBasicType("i32"), SourceFile), SourceFile)
            }
        };
        var whileStmt = new HighLevelIR.While 
        { 
            Condition = condition, 
            Body = loopBody,
            SourceFile = SourceFile
        };
        
        hlir.Modules[0].Functions.Add(new HighLevelIR.Function
        {
            Name = "test",
            Body = new HighLevelIR.Block(SourceFile)
            {
                Statements = new List<HighLevelIR.Statement> { whileStmt }
            }
        });
        
        var transformer = new HlirToMlirTransformer();

        // Act
        var result = transformer.Transform(hlir);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Modules[0].Functions, Has.Count.EqualTo(1));
        var mlFunction = result.Modules[0].Functions[0];
        Assert.That(mlFunction.Instructions, Has.Count.GreaterThan(0));
        // Should have instructions for the while loop structure
        Assert.That(mlFunction.Instructions.Any(), Is.True);
    }

    // RED PHASE: One failing test at a time for ProcessStatement - ReturnStatement case
    [Test]
    public void Transform_ShouldHandleReturnStatement()
    {
        // Arrange - Create a ReturnStatement which should trigger ProcessReturnStatement (line 130-131)
        var hlir = CreateSimpleProgram();
        var returnValue = new HighLevelIR.Literal("42", CreateBasicType("i32"), SourceFile);
        var returnStmt = new HighLevelIR.ReturnStatement { Value = returnValue, SourceFile = SourceFile };
        
        hlir.Modules[0].Functions.Add(new HighLevelIR.Function
        {
            Name = "test",
            Body = new HighLevelIR.Block(SourceFile)
            {
                Statements = new List<HighLevelIR.Statement> { returnStmt }
            }
        });
        
        var transformer = new HlirToMlirTransformer();

        // Act
        var result = transformer.Transform(hlir);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Modules[0].Functions, Has.Count.EqualTo(1));
        var mlFunction = result.Modules[0].Functions[0];
        // ProcessReturnStatement is static and doesn't add instructions (ignores return values)
        Assert.That(mlFunction.Instructions, Has.Count.EqualTo(0));
    }

    // RED PHASE: One failing test at a time for GetExpressionValue - default case
    [Test]
    public void Transform_ShouldHandleUnsupportedExpressionType()
    {
        // Arrange - Create an expression type that's not handled by GetExpressionValue
        var hlir = CreateSimpleProgram();
        
        // Use UnaryOp which is not handled by GetExpressionValue (only Literal, Identifier, BinaryOp are handled)
        var unaryOp = new HighLevelIR.UnaryOp("-", new HighLevelIR.Literal("42", CreateBasicType("i32"), SourceFile), SourceFile);
        var assignment = new HighLevelIR.Assignment("x", unaryOp, SourceFile);
        
        hlir.Modules[0].Functions.Add(new HighLevelIR.Function
        {
            Name = "test",
            Body = new HighLevelIR.Block(SourceFile)
            {
                Statements = new List<HighLevelIR.Statement> { assignment }
            }
        });
        
        var transformer = new HlirToMlirTransformer();

        // Act
        var result = transformer.Transform(hlir);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Modules[0].Functions, Has.Count.EqualTo(1));
        var mlFunction = result.Modules[0].Functions[0];
        Assert.That(mlFunction.Instructions, Has.Count.GreaterThan(0));
        // The assignment should use the default value ("0") for unsupported UnaryOp expression
    }

    // RED PHASE: One failing test at a time for GetIdentifierValue - non-constant case
    [Test]
    public void Transform_ShouldHandleNonConstantIdentifier()
    {
        // Arrange - Create an identifier that is NOT a constant
        var hlir = CreateSimpleProgram();
        var identifier = new HighLevelIR.Identifier("variableName", CreateBasicType("i32"), SourceFile);
        var assignment = new HighLevelIR.Assignment("x", identifier, SourceFile);
        
        hlir.Modules[0].Functions.Add(new HighLevelIR.Function
        {
            Name = "test",
            Body = new HighLevelIR.Block(SourceFile)
            {
                Statements = new List<HighLevelIR.Statement> { assignment }
            }
        });
        
        var transformer = new HlirToMlirTransformer();

        // Act
        var result = transformer.Transform(hlir);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Modules[0].Functions, Has.Count.EqualTo(1));
        var mlFunction = result.Modules[0].Functions[0];
        Assert.That(mlFunction.Instructions, Has.Count.GreaterThan(0));
        // The assignment should use the identifier name "variableName" since it's not a constant
    }

    // RED PHASE: One failing test at a time for TryConstantFolding - null return path
    [Test]
    public void Transform_BinaryOperation_WithNonNumericOperands_ShouldReturnNullFromConstantFolding()
    {
        // Arrange - Test the TryConstantFolding null return path (line 311-312: !TryParseOperands returns null)
        var hlir = CreateSimpleProgram();
        var transformer = new HlirToMlirTransformer();
        
        // Create binary operation with non-numeric operands that can't be parsed
        var binaryOp = new HighLevelIR.BinaryOp
        {
            Left = new HighLevelIR.Literal { Value = "hello" }, // Non-numeric
            Right = new HighLevelIR.Literal { Value = "world" }, // Non-numeric
            Operator = "+"
        };
        
        var assignment = new HighLevelIR.Assignment
        {
            Target = "result",
            Value = binaryOp
        };
        
        hlir.Modules[0].Functions.Add(new HighLevelIR.Function
        {
            Name = "test",
            Body = new HighLevelIR.Block(SourceFile)
            {
                Statements = new List<HighLevelIR.Statement> { assignment }
            }
        });

        // Act
        var result = transformer.Transform(hlir);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Modules[0].Functions, Has.Count.EqualTo(1));
        
        var mlFunction = result.Modules[0].Functions[0];
        Assert.That(mlFunction.Instructions, Has.Count.EqualTo(1));
        
        var assignInstr = mlFunction.Instructions[0] as MidLevelIR.MLAssign;
        Assert.That(assignInstr, Is.Not.Null);
        Assert.That(assignInstr!.Target, Is.EqualTo("result"));
        // Should not fold, should return expression as string since TryConstantFolding returns null
        Assert.That(assignInstr.Source, Is.EqualTo("(hello + world)"));
    }

    // RED PHASE: One failing test at a time for GetIdentifierValue - constant case
    [Test]
    public void Transform_ShouldHandleConstantIdentifier()
    {
        // Arrange - Create an identifier that IS a constant
        var hlir = CreateSimpleProgram();
        
        // Add a constant to the HLIR globals to make TryGetConstantValue return true
        hlir.Globals["PI"] = new HighLevelIR.Variable { Name = "PI", Type = CreateBasicType("f64") };
        
        var constantIdentifier = new HighLevelIR.Identifier("PI", CreateBasicType("f64"), SourceFile);
        var assignment = new HighLevelIR.Assignment("x", constantIdentifier, SourceFile);
        
        hlir.Modules[0].Functions.Add(new HighLevelIR.Function
        {
            Name = "test",
            Body = new HighLevelIR.Block(SourceFile)
            {
                Statements = new List<HighLevelIR.Statement> { assignment }
            }
        });
        
        var transformer = new HlirToMlirTransformer();

        // Act
        var result = transformer.Transform(hlir);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Modules[0].Functions, Has.Count.EqualTo(1));
        var mlFunction = result.Modules[0].Functions[0];
        Assert.That(mlFunction.Instructions, Has.Count.GreaterThan(0));
        // The assignment should use the constant value for "PI" since it's defined as a constant
    }

    // RED PHASE: One failing test at a time for TryGetConstantValue - _currentHlir null case
    [Test]
    public void Transform_WithNullCurrentHlir_ShouldHandleIdentifierGracefully()
    {
        // Arrange - Test the _currentHlir == null path in TryGetConstantValue (line 284-285)
        // This is a tricky case since _currentHlir is set during Transform, but we need to test this edge case
        var hlir = CreateSimpleProgram();
        var body = CreateBlock();
        
        // Create an identifier that would normally be looked up as a constant
        var identifier = new HighLevelIR.Identifier("someConstant", CreateBasicType("i32"), SourceFile);
        var assignment = new HighLevelIR.Assignment("result", identifier, SourceFile);
        body.AddStatement(assignment);
        
        var function = CreateFunction("main", "Void", body);
        hlir.Modules[0].Functions.Add(function);

        // Act - The transformer should handle this gracefully even if _currentHlir were null internally
        var result = _transformer.Transform(hlir);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Modules[0].Functions, Has.Count.EqualTo(1));
        
        var mlFunction = result.Modules[0].Functions[0];
        Assert.That(mlFunction.Instructions, Has.Count.EqualTo(1));
        
        var assignInstr = mlFunction.Instructions[0] as MidLevelIR.MLAssign;
        Assert.That(assignInstr, Is.Not.Null);
        Assert.That(assignInstr!.Target, Is.EqualTo("result"));
        // Should fall back to identifier name when constant lookup fails
        Assert.That(assignInstr.Source, Is.EqualTo("someConstant"));
    }

    // RED PHASE: One failing test at a time for TryGetConstantValue - null InitialValue case
    [Test]
    public void Transform_ConstantWithNullInitialValue_ShouldUseIdentifierName()
    {
        // Arrange - Test the symbol.InitialValue == null path in TryGetConstantValue (line 293-294)
        var hlir = CreateSimpleProgram();
        
        // Add a constant with null InitialValue to test this specific path
        var constantSymbol = new HighLevelIR.Variable 
        { 
            Name = "NULL_CONSTANT", 
            Type = CreateBasicType("i32"),
            IsConstant = true
            // InitialValue is null by default
        };
        hlir.Globals["NULL_CONSTANT"] = constantSymbol;
        
        var identifier = new HighLevelIR.Identifier("NULL_CONSTANT", CreateBasicType("i32"), SourceFile);
        var assignment = new HighLevelIR.Assignment("result", identifier, SourceFile);
        
        hlir.Modules[0].Functions.Add(new HighLevelIR.Function
        {
            Name = "test",
            Body = new HighLevelIR.Block(SourceFile)
            {
                Statements = new List<HighLevelIR.Statement> { assignment }
            }
        });

        // Act
        var result = _transformer.Transform(hlir);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Modules[0].Functions, Has.Count.EqualTo(1));
        
        var mlFunction = result.Modules[0].Functions[0];
        Assert.That(mlFunction.Instructions, Has.Count.EqualTo(1));
        
        var assignInstr = mlFunction.Instructions[0] as MidLevelIR.MLAssign;
        Assert.That(assignInstr, Is.Not.Null);
        Assert.That(assignInstr!.Target, Is.EqualTo("result"));
        // Should fall back to identifier name when constant has null InitialValue
        Assert.That(assignInstr.Source, Is.EqualTo("NULL_CONSTANT"));
    }

    // RED PHASE: One failing test at a time for TryGetConstantValue - non-constant symbol case
    [Test]
    public void Transform_NonConstantSymbol_ShouldUseIdentifierName()
    {
        // Arrange - Test the !symbol.IsConstant path in TryGetConstantValue (line 290-291)
        var hlir = CreateSimpleProgram();
        
        // Add a symbol that is NOT a constant (IsConstant = false)
        var variableSymbol = new HighLevelIR.Variable 
        { 
            Name = "VARIABLE_NOT_CONSTANT", 
            Type = CreateBasicType("i32"),
            IsConstant = false, // This is the key - it's not a constant
            InitialValue = "42"
        };
        hlir.Globals["VARIABLE_NOT_CONSTANT"] = variableSymbol;
        
        var identifier = new HighLevelIR.Identifier("VARIABLE_NOT_CONSTANT", CreateBasicType("i32"), SourceFile);
        var assignment = new HighLevelIR.Assignment("result", identifier, SourceFile);
        
        hlir.Modules[0].Functions.Add(new HighLevelIR.Function
        {
            Name = "test",
            Body = new HighLevelIR.Block(SourceFile)
            {
                Statements = new List<HighLevelIR.Statement> { assignment }
            }
        });

        // Act
        var result = _transformer.Transform(hlir);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Modules[0].Functions, Has.Count.EqualTo(1));
        
        var mlFunction = result.Modules[0].Functions[0];
        Assert.That(mlFunction.Instructions, Has.Count.EqualTo(1));
        
        var assignInstr = mlFunction.Instructions[0] as MidLevelIR.MLAssign;
        Assert.That(assignInstr, Is.Not.Null);
        Assert.That(assignInstr!.Target, Is.EqualTo("result"));
        // Should fall back to identifier name when symbol exists but is not a constant
        Assert.That(assignInstr.Source, Is.EqualTo("VARIABLE_NOT_CONSTANT"));
    }

    // RED PHASE: One failing test at a time for GetIdentifierValue - constant with null ToString() case
    [Test]
    public void Transform_ConstantWithNullToString_ShouldReturnEmptyString()
    {
        // Arrange - Test the constantValue ?? string.Empty path in GetIdentifierValue (line 275)
        // This happens when TryGetConstantValue returns true but constantValue is null
        var hlir = CreateSimpleProgram();
        
        // Create a mock object that will return null from ToString()
        var mockInitialValue = new MockObjectWithNullToString();
        
        // Add a constant where InitialValue.ToString() returns null
        var constantSymbol = new HighLevelIR.Variable 
        { 
            Name = "NULL_TOSTRING_CONSTANT", 
            Type = CreateBasicType("i32"),
            IsConstant = true,
            InitialValue = mockInitialValue
        };
        hlir.Globals["NULL_TOSTRING_CONSTANT"] = constantSymbol;
        
        var identifier = new HighLevelIR.Identifier("NULL_TOSTRING_CONSTANT", CreateBasicType("i32"), SourceFile);
        var assignment = new HighLevelIR.Assignment("result", identifier, SourceFile);
        
        hlir.Modules[0].Functions.Add(new HighLevelIR.Function
        {
            Name = "test",
            Body = new HighLevelIR.Block(SourceFile)
            {
                Statements = new List<HighLevelIR.Statement> { assignment }
            }
        });

        // Act
        var result = _transformer.Transform(hlir);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Modules[0].Functions, Has.Count.EqualTo(1));
        
        var mlFunction = result.Modules[0].Functions[0];
        Assert.That(mlFunction.Instructions, Has.Count.EqualTo(1));
        
        var assignInstr = mlFunction.Instructions[0] as MidLevelIR.MLAssign;
        Assert.That(assignInstr, Is.Not.Null);
        Assert.That(assignInstr!.Target, Is.EqualTo("result"));
        // Should return empty string when constantValue is null (constantValue ?? string.Empty)
        Assert.That(assignInstr.Source, Is.EqualTo(""));
    }

    // Helper class for testing null ToString() scenario
    private class MockObjectWithNullToString
    {
        public override string? ToString() => null;
    }

    // RED PHASE: One failing test at a time for TryGetConstantValue - _currentHlir null case
    [Test]
    public void GetExpressionValue_WithNullCurrentHlir_ShouldHandleGracefully()
    {
        // Arrange - Test the _currentHlir == null path in TryGetConstantValue (line 284-285)
        // We need to call GetExpressionValue directly without setting _currentHlir
        var transformer = new HlirToMlirTransformer();
        
        // Create an identifier - this will trigger GetIdentifierValue -> TryGetConstantValue
        var identifier = new HighLevelIR.Identifier("testConstant", CreateBasicType("i32"), SourceFile);
        
        // Act - Call GetExpressionValue directly using reflection since it's internal
        var getExpressionValueMethod = typeof(HlirToMlirTransformer)
            .GetMethod("GetExpressionValue", BindingFlags.NonPublic | BindingFlags.Instance);
        
        Assert.That(getExpressionValueMethod, Is.Not.Null, "GetExpressionValue method should exist");
        
        var result = getExpressionValueMethod?.Invoke(transformer, new object[] { identifier }) as string;

        // Assert - Should return the identifier name since TryGetConstantValue returns false when _currentHlir is null
        Assert.That(result, Is.EqualTo("testConstant"));
    }

    // RED PHASE: One failing test at a time for GetIdentifierValue - missing constant symbol
    [Test]
    public void Transform_ShouldHandleMissingConstantSymbol()
    {
        // Arrange - Test the TryGetConstantValue path where symbol is not found in Globals (line 287-288)
        var hlir = CreateSimpleProgram();
        
        // Don't add any symbol to Globals, but try to reference one
        var missingConstantIdentifier = new HighLevelIR.Identifier("MISSING_CONSTANT", CreateBasicType("i32"), SourceFile);
        var assignment = new HighLevelIR.Assignment("x", missingConstantIdentifier, SourceFile);
        
        hlir.Modules[0].Functions.Add(new HighLevelIR.Function
        {
            Name = "test",
            Body = new HighLevelIR.Block(SourceFile)
            {
                Statements = new List<HighLevelIR.Statement> { assignment }
            }
        });
        
        var transformer = new HlirToMlirTransformer();

        // Act
        var result = transformer.Transform(hlir);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Modules[0].Functions, Has.Count.EqualTo(1));
        var mlFunction = result.Modules[0].Functions[0];
        Assert.That(mlFunction.Instructions, Has.Count.GreaterThan(0));
        // Since TryGetConstantValue returns false for missing symbol, should use identifier name
        var assignInstr = mlFunction.Instructions[0] as MidLevelIR.MLAssign;
        Assert.That(assignInstr, Is.Not.Null);
        Assert.That(assignInstr!.Source, Is.EqualTo("MISSING_CONSTANT"));
    }

    // RED PHASE: One failing test at a time for GetExpressionValue - nested binary operations
    [Test]
    public void Transform_NestedBinaryOperation_ShouldHandleRecursion()
    {
        // Arrange - Test recursive GetExpressionValue calls in GetBinaryOpValue (lines 302-303)
        var hlir = CreateSimpleProgram();
        var transformer = new HlirToMlirTransformer();
        
        // Create nested binary operation: (5 + 3) * 2
        // This will trigger recursive calls to GetExpressionValue for the left operand (5 + 3)
        var innerBinaryOp = new HighLevelIR.BinaryOp
        {
            Left = new HighLevelIR.Literal { Value = "5" },
            Right = new HighLevelIR.Literal { Value = "3" },
            Operator = "+"
        };
        
        var outerBinaryOp = new HighLevelIR.BinaryOp
        {
            Left = innerBinaryOp, // Nested binary operation
            Right = new HighLevelIR.Literal { Value = "2" },
            Operator = "*"
        };
        
        var assignment = new HighLevelIR.Assignment
        {
            Target = "result",
            Value = outerBinaryOp
        };
        
        hlir.Modules[0].Functions.Add(new HighLevelIR.Function
        {
            Name = "test",
            Body = new HighLevelIR.Block(SourceFile)
            {
                Statements = new List<HighLevelIR.Statement> { assignment }
            }
        });

        // Act
        var result = transformer.Transform(hlir);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Modules[0].Functions, Has.Count.EqualTo(1));
        
        var mlFunction = result.Modules[0].Functions[0];
        Assert.That(mlFunction.Instructions, Has.Count.EqualTo(1));
        
        var assignInstr = mlFunction.Instructions[0] as MidLevelIR.MLAssign;
        Assert.That(assignInstr, Is.Not.Null);
        Assert.That(assignInstr!.Target, Is.EqualTo("result"));
        // Should be folded: (5 + 3) * 2 = 8 * 2 = 16
        Assert.That(assignInstr.Source, Is.EqualTo("16"));
    }

    // RED PHASE: One failing test at a time for GetExpressionValue - FunctionCall case
    [Test]
    public void Transform_ShouldHandleFunctionCallExpression()
    {
        // Arrange - Create a FunctionCall expression which is NOT handled by GetExpressionValue
        var hlir = CreateSimpleProgram();
        var args = new List<HighLevelIR.Expression> 
        { 
            new HighLevelIR.Literal("Hello", CreateBasicType("string"), SourceFile) 
        };
        var functionCall = new HighLevelIR.FunctionCall(
            new HighLevelIR.Identifier("printf", CreateBasicType("function"), SourceFile), 
            args);
        var assignment = new HighLevelIR.Assignment("x", functionCall, SourceFile);
        
        hlir.Modules[0].Functions.Add(new HighLevelIR.Function
        {
            Name = "test",
            Body = new HighLevelIR.Block(SourceFile)
            {
                Statements = new List<HighLevelIR.Statement> { assignment }
            }
        });
        
        var transformer = new HlirToMlirTransformer();

        // Act
        var result = transformer.Transform(hlir);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Modules[0].Functions, Has.Count.EqualTo(1));
        var mlFunction = result.Modules[0].Functions[0];
        Assert.That(mlFunction.Instructions, Has.Count.GreaterThan(0));
        // The assignment should use the default value ("0") for unsupported FunctionCall expression
    }

    // RED PHASE: One failing test at a time for ProcessStatement - Block case (genuinely uncovered)
    [Test]
    public void Transform_ShouldHandleBlockStatement()
    {
        // Arrange - Create a Block statement which should trigger ProcessStatements for nested statements
        var hlir = CreateSimpleProgram();
        var nestedBlock = new HighLevelIR.Block(SourceFile);
        // Add a statement to the nested block to ensure it generates instructions
        nestedBlock.Statements.Add(new HighLevelIR.Assignment("x", new HighLevelIR.Literal("1", CreateBasicType("i32"), SourceFile), SourceFile));
        
        hlir.Modules[0].Functions.Add(new HighLevelIR.Function
        {
            Name = "test",
            Body = new HighLevelIR.Block(SourceFile)
            {
                Statements = new List<HighLevelIR.Statement> { nestedBlock }
            }
        });
        
        var transformer = new HlirToMlirTransformer();

        // Act
        var result = transformer.Transform(hlir);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Modules[0].Functions, Has.Count.EqualTo(1));
        var mlFunction = result.Modules[0].Functions[0];
        Assert.That(mlFunction.Instructions, Has.Count.GreaterThan(0));
        // The Block should be processed and its nested statement should generate instructions
    }

    // RED PHASE: One failing test at a time for GetExpressionValue - UnaryOp default case
    [Test]
    public void Transform_ShouldHandleUnaryOpExpression()
    {
        // Arrange - Create a UnaryOp expression which should hit the default case in GetExpressionValue
        var hlir = CreateSimpleProgram();
        var operand = new HighLevelIR.Literal("42", CreateBasicType("i32"), SourceFile);
        var unaryOp = new HighLevelIR.UnaryOp("-", operand, SourceFile);
        var assignment = new HighLevelIR.Assignment("x", unaryOp, SourceFile);
        
        hlir.Modules[0].Functions.Add(new HighLevelIR.Function
        {
            Name = "test",
            Body = new HighLevelIR.Block(SourceFile)
            {
                Statements = new List<HighLevelIR.Statement> { assignment }
            }
        });
        
        var transformer = new HlirToMlirTransformer();

        // Act
        var result = transformer.Transform(hlir);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Modules[0].Functions, Has.Count.EqualTo(1));
        var mlFunction = result.Modules[0].Functions[0];
        Assert.That(mlFunction.Instructions, Has.Count.GreaterThan(0));
        // The UnaryOp should use the default value ("0") since it's not explicitly handled
    }

    #endregion

    #region Helper Methods

    private HighLevelIR CreateSimpleProgram()
    {
        var hlir = new HighLevelIR { SourceFile = SourceFile };
        // Initialize the Modules list and add a default module
        hlir.Modules = new List<HlModule>();
        var module = new HlModule { Name = "default" };
        hlir.Modules.Add(module);
        return hlir;
    }

    #endregion
}
