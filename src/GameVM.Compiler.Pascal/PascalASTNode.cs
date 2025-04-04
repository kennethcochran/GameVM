// This file contains the definition of the AST nodes for the Pascal language.

using Antlr4.Runtime;

namespace GameVM.Compiler.Pascal
{
    // Using Pascal.g4 as a reference, create the node types for a Pascal AST.

    public class PascalASTNode
    {
        public virtual IList<PascalASTNode> Children => new List<PascalASTNode>();
    }

    public class ProgramNode : PascalASTNode
    {
        public required string Name { get; set; }
        public required List<PascalASTNode> UsesUnits { get; set; }
        public required BlockNode Block { get; set; }

        public ProgramNode()
        {
            UsesUnits = new List<PascalASTNode>();
        }
    }

    public class BlockNode : PascalASTNode
    {
        public required List<PascalASTNode> Statements { get; set; } = new();
    }

    public class StatementNode : PascalASTNode
    {
    }

    public class ExpressionNode : PascalASTNode
    {
    }

    public class VariableNode : PascalASTNode
    {
        public required string Name { get; set; }
    }

    public class ConstantNode : PascalASTNode
    {
        public required object Value { get; set; }
    }

    public class ProcedureNode : PascalASTNode
    {
        public required string Name { get; set; }
        public required List<VariableNode> Parameters { get; set; }
        public required PascalASTNode Block { get; set; }
    }

    public class FunctionNode : PascalASTNode
    {
        public required string Name { get; set; }
        public required List<VariableNode> Parameters { get; set; }
        public required PascalASTNode Block { get; set; }
    }

    public class BeginNode : PascalASTNode
    {
    }

    public class EndNode : PascalASTNode
    {
    }

    public class IfNode : PascalASTNode
    {
        public required PascalASTNode Condition { get; set; }
        public required PascalASTNode ThenBlock { get; set; }
        public PascalASTNode? ElseBlock { get; set; }
    }

    public class WhileNode : PascalASTNode
    {
        public required PascalASTNode Condition { get; set; }
        public required PascalASTNode Block { get; set; }
    }

    public class ForNode : PascalASTNode
    {
        public required PascalASTNode Variable { get; set; }
        public required PascalASTNode FromExpression { get; set; }
        public required PascalASTNode ToExpression { get; set; }
        public required PascalASTNode Block { get; set; }
    }

    public class RepeatNode : PascalASTNode
    {
        public required PascalASTNode Block { get; set; }
        public required PascalASTNode Condition { get; set; }
    }

    public class AssignmentNode : PascalASTNode
    {
        public required PascalASTNode Left { get; set; }
        public required PascalASTNode Right { get; set; }
    }

    public class ExpressionStatementNode : PascalASTNode
    {
        public required PascalASTNode Expression { get; set; }
    }

    public class ProcedureCallNode : PascalASTNode
    {
        public required string Name { get; set; }
        public required List<PascalASTNode> Arguments { get; set; }
    }

    public class FunctionCallNode : PascalASTNode
    {
        public required string Name { get; set; }
        public required List<PascalASTNode> Arguments { get; set; }
    }

    public class OperatorNode : PascalASTNode
    {
        public required string Operator { get; set; }
        public required PascalASTNode Left { get; set; }
        public required PascalASTNode Right { get; set; }
    }

    public class UnaryOperatorNode : PascalASTNode
    {
        public required string Operator { get; set; }
        public required PascalASTNode Operand { get; set; }
    }

    public class BinaryOperatorNode : PascalASTNode
    {
        public required string Operator { get; set; }
        public required PascalASTNode Left { get; set; }
        public required PascalASTNode Right { get; set; }
    }

    public class TernaryOperatorNode : PascalASTNode
    {
        public required PascalASTNode Condition { get; set; }
        public required PascalASTNode TrueExpression { get; set; }
        public required PascalASTNode FalseExpression { get; set; }
    }

    public class ComparisonOperatorNode : PascalASTNode
    {
        public required string Operator { get; set; }
        public required PascalASTNode Left { get; set; }
        public required PascalASTNode Right { get; set; }
    }

    public class LogicalOperatorNode : PascalASTNode
    {
        public required string Operator { get; set; }
        public required PascalASTNode Left { get; set; }
        public required PascalASTNode Right { get; set; }
    }

    public class RelationalOperatorNode : ExpressionNode
    {
        public required string Operator { get; set; }
        public required ExpressionNode Left { get; set; }
        public required ExpressionNode Right { get; set; }
    }

    public class AdditiveOperatorNode : PascalASTNode
    {
        public required string Operator { get; set; }
        public required PascalASTNode Left { get; set; }
        public required PascalASTNode Right { get; set; }
    }

    public class MultiplicativeOperatorNode : PascalASTNode
    {
        public required string Operator { get; set; }
        public required PascalASTNode Left { get; set; }
        public required PascalASTNode Right { get; set; }
    }

    public class VariableDeclarationNode : PascalASTNode
    {
        public required string Name { get; set; }
        public required TypeNode Type { get; set; }
    }

    public class TypeNode : PascalASTNode
    {
        public required string TypeName { get; set; }
    }

    public class ArrayTypeNode : TypeNode
    {
        public required TypeNode ElementType { get; set; }
        public required List<RangeNode> Dimensions { get; set; }
    }

    public class RangeNode : PascalASTNode
    {
        public required ExpressionNode Low { get; set; }
        public required ExpressionNode High { get; set; }
    }

    public class RecordTypeNode : TypeNode
    {
        public required List<FieldDeclarationNode> Fields { get; set; }
    }

    public class FieldDeclarationNode : PascalASTNode
    {
        public required string Name { get; set; }
        public required TypeNode Type { get; set; }
    }

    public class SimpleTypeNode : TypeNode
    {
    }

    public class StringTypeNode : SimpleTypeNode
    {
        public required ExpressionNode Length { get; set; }
    }

    public class CaseNode : PascalASTNode
    {
        public required ExpressionNode Selector { get; set; }
        public required List<CaseBranchNode> Branches { get; set; } = new();
        public PascalASTNode? ElseBlock { get; set; }
    }

    public class CaseBranchNode : PascalASTNode
    {
        public required List<ExpressionNode> Labels { get; set; }
        public required PascalASTNode Statement { get; set; }
    }

    public class WithNode : PascalASTNode
    {
        public required List<VariableNode> RecordVariables { get; set; }
        public required PascalASTNode Block { get; set; }
    }

    public class LiteralNode : ExpressionNode
    {
        public required object Value { get; set; }
    }

    public class StringLiteralNode : LiteralNode
    {
    }

    public class IntegerLiteralNode : LiteralNode
    {
    }

    public class RealLiteralNode : LiteralNode
    {
    }

    public class BooleanLiteralNode : LiteralNode
    {
    }

    public class CharacterLiteralNode : LiteralNode
    {
    }

    public class SetNode : ExpressionNode
    {
        public required List<ExpressionNode> Elements { get; set; }
    }
}
