using System;
using System.Collections.Generic;
using GameVM.Compiler.Pascal;

namespace GameVM.Compiler.Pascal
{
    /// <summary>
    /// Builder for creating AST nodes.
    /// Standardizes AST creation and reduces coupling in visitors.
    /// </summary>
    public class AstBuilder
    {
        public ProgramNode CreateProgram(string name, BlockNode block)
        {
            return new ProgramNode { Name = name, Block = block, UsesUnits = new List<PascalAstNode>() };
        }

        public BlockNode CreateBlock(List<PascalAstNode> statements)
        {
            return new BlockNode { Statements = statements };
        }

        public VariableNode CreateVariable(string name)
        {
            return new VariableNode { Name = name };
        }

        public ConstantNode CreateConstant(object value)
        {
            return new ConstantNode { Value = value };
        }

        public IntegerLiteralNode CreateIntegerLiteral(string value)
        {
            if (int.TryParse(value, out int result))
            {
                return new IntegerLiteralNode { Value = result };
            }
            return new IntegerLiteralNode { Value = 0 }; // Error case or default
        }

        public RealLiteralNode CreateRealLiteral(string value)
        {
            if (double.TryParse(value, out double result))
            {
                return new RealLiteralNode { Value = result };
            }
            return new RealLiteralNode { Value = 0.0 };
        }

        public StringLiteralNode CreateStringLiteral(string value)
        {
            // Remove quotes if present
            if (value.Length >= 2 && value.StartsWith('\'') && value.EndsWith('\''))
            {
                value = value.Substring(1, value.Length - 2);
            }
            return new StringLiteralNode { Value = value ?? string.Empty };
        }

        public BooleanLiteralNode CreateBooleanLiteral(bool value)
        {
            return new BooleanLiteralNode { Value = value };
        }

        public AssignmentNode CreateAssignment(PascalAstNode left, PascalAstNode right)
        {
            return new AssignmentNode { Left = left, Right = right };
        }

        public IfNode CreateIf(PascalAstNode condition, PascalAstNode thenBlock, PascalAstNode? elseBlock = null)
        {
            return new IfNode { Condition = condition, ThenBlock = thenBlock, ElseBlock = elseBlock };
        }

        public WhileNode CreateWhile(PascalAstNode condition, PascalAstNode block)
        {
            return new WhileNode { Condition = condition, Block = block };
        }

        public RepeatNode CreateRepeat(PascalAstNode block, PascalAstNode condition)
        {
            return new RepeatNode { Block = block, Condition = condition };
        }

        public ForNode CreateFor(PascalAstNode variable, PascalAstNode fromExpr, PascalAstNode toExpr, PascalAstNode block)
        {
            return new ForNode { Variable = variable, FromExpression = fromExpr, ToExpression = toExpr, Block = block };
        }

        public ProcedureCallNode CreateProcedureCall(string name, List<PascalAstNode> arguments)
        {
            return new ProcedureCallNode { Name = name, Arguments = arguments };
        }

        public FunctionCallNode CreateFunctionCall(string name, List<PascalAstNode> arguments)
        {
            return new FunctionCallNode { Name = name, Arguments = arguments };
        }

        public AdditiveOperatorNode CreateAdditive(ExpressionNode left, string op, ExpressionNode right)
        {
            return new AdditiveOperatorNode { Left = left, Operator = op, Right = right };
        }

        public MultiplicativeOperatorNode CreateMultiplicative(ExpressionNode left, string op, ExpressionNode right)
        {
            return new MultiplicativeOperatorNode { Left = left, Operator = op, Right = right };
        }

        public RelationalOperatorNode CreateRelational(ExpressionNode left, string op, ExpressionNode right)
        {
            return new RelationalOperatorNode { Left = left, Operator = op, Right = right };
        }

        public UnaryOperatorNode CreateUnary(string op, ExpressionNode operand)
        {
            return new UnaryOperatorNode { Operator = op, Operand = operand };
        }

        public SetNode CreateSet(List<ExpressionNode> elements)
        {
            return new SetNode { Elements = elements };
        }
    }
}
