using GameVM.Compiler.Core.IR;
using System.Collections.Generic;

namespace GameVM.Compiler.Core.Tests.Fixtures;

/// <summary>
/// Builder for creating IR structures in tests.
/// Simplifies IR construction for test scenarios.
/// </summary>
public static class IRBuilder
{
    private const string DefaultSourceFile = "test.pas";

    public static HighLevelIR CreateHLIR(string sourceFile = DefaultSourceFile)
    {
        return new HighLevelIR { SourceFile = sourceFile };
    }

    public static HighLevelIR.Function CreateFunction(
        string name,
        string returnTypeName = "Void",
        HighLevelIR.Block? body = null,
        string sourceFile = DefaultSourceFile)
    {
        body ??= new HighLevelIR.Block(sourceFile);
        return new HighLevelIR.Function(sourceFile, name, CreateBasicType(returnTypeName, sourceFile), body);
    }

    public static HighLevelIR.BasicType CreateBasicType(string name, string sourceFile = DefaultSourceFile)
    {
        return new HighLevelIR.BasicType(sourceFile, name);
    }

    public static HighLevelIR.Parameter CreateParameter(string name, string typeName, string sourceFile = DefaultSourceFile)
    {
        return new HighLevelIR.Parameter(name, CreateBasicType(typeName, sourceFile), sourceFile);
    }

    public static IRSymbol CreateVariable(string name, string typeName, string sourceFile = DefaultSourceFile)
    {
        return new IRSymbol
        {
            Name = name,
            Type = CreateBasicType(typeName, sourceFile),
            IsConstant = false,
            InitialValue = string.Empty
        };
    }

    public static HighLevelIR.Literal CreateLiteral(object value, string typeName, string sourceFile = DefaultSourceFile)
    {
        return new HighLevelIR.Literal(value, CreateBasicType(typeName, sourceFile), sourceFile);
    }

    public static HighLevelIR.Identifier CreateIdentifier(string name, string typeName, string sourceFile = DefaultSourceFile)
    {
        return new HighLevelIR.Identifier(name, CreateBasicType(typeName, sourceFile), sourceFile);
    }

    public static HighLevelIR.BinaryOp CreateBinaryOp(
        string op,
        HighLevelIR.Expression left,
        HighLevelIR.Expression right,
        string sourceFile = DefaultSourceFile)
    {
        return new HighLevelIR.BinaryOp(op, left, right, sourceFile);
    }

    public static HighLevelIR.Assignment CreateAssignment(
        string target,
        HighLevelIR.Expression value,
        string sourceFile = DefaultSourceFile)
    {
        return new HighLevelIR.Assignment(target, value, sourceFile);
    }

    public static HighLevelIR.Block CreateBlock(string sourceFile = DefaultSourceFile)
    {
        return new HighLevelIR.Block(sourceFile);
    }

    public static HighLevelIR.Block CreateBlockWithStatements(
        params HighLevelIR.Statement[] statements)
    {
        var block = CreateBlock();
        foreach (var stmt in statements)
        {
            block.AddStatement(stmt);
        }
        return block;
    }

    public static HighLevelIR.IfStatement CreateIfStatement(
        HighLevelIR.Expression condition,
        List<IRNode> thenBlock,
        List<IRNode>? elseBlock = null,
        string sourceFile = DefaultSourceFile)
    {
        return new HighLevelIR.IfStatement(condition, thenBlock, elseBlock);
    }

    public static HighLevelIR.While CreateWhile(
        HighLevelIR.Expression condition,
        HighLevelIR.Block body,
        string sourceFile = DefaultSourceFile)
    {
        return new HighLevelIR.While { Condition = condition, Body = body, SourceFile = sourceFile };
    }

    public static HighLevelIR.FunctionCall CreateFunctionCall(
        string functionName,
        params HighLevelIR.Expression[] arguments)
    {
        var functionExpr = CreateIdentifier(functionName, "Void");
        return new HighLevelIR.FunctionCall(functionExpr, arguments);
    }

    public static HighLevelIR.ExpressionStatement CreateExpressionStatement(
        HighLevelIR.Expression expression,
        string sourceFile = DefaultSourceFile)
    {
        return new HighLevelIR.ExpressionStatement { Expression = expression, SourceFile = sourceFile };
    }

    public static IRSymbol CreateSymbol(
        string name,
        string typeName,
        bool isConstant = false,
        object? initialValue = null,
        string sourceFile = DefaultSourceFile)
    {
        return new IRSymbol
        {
            Name = name,
            Type = CreateBasicType(typeName, sourceFile),
            IsConstant = isConstant,
            InitialValue = initialValue ?? string.Empty
        };
    }
}

