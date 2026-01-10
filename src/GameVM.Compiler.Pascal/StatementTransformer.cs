using System;
using System.Collections.Generic;
using GameVM.Compiler.Core.IR;

namespace GameVM.Compiler.Pascal
{
    /// <summary>
    /// Transforms statement AST nodes to High-Level IR statements
    /// </summary>
    public class StatementTransformer
    {
        private readonly TransformationContext _context;
        private readonly ExpressionTransformer _expressionTransformer;

        public StatementTransformer(
            TransformationContext context,
            ExpressionTransformer expressionTransformer)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _expressionTransformer = expressionTransformer ?? throw new ArgumentNullException(nameof(expressionTransformer));
        }

        /// <summary>
        /// Transforms a statement node to a High-Level IR statement
        /// </summary>
        public HighLevelIR.Statement TransformStatement(PascalASTNode stmtNode)
        {
            if (stmtNode == null)
                return CreateErrorStatement("Statement node is null");

            try
            {
                return stmtNode switch
                {
                    AssignmentNode assignNode => TransformAssignment(assignNode),
                    IfNode ifNode => TransformIfStatement(ifNode),
                    WhileNode whileNode => TransformWhileLoop(whileNode),
                    ForNode forNode => TransformForLoop(forNode),
                    RepeatNode repeatNode => TransformRepeatLoop(repeatNode),
                    ProcedureCallNode procCallNode => TransformProcedureCall(procCallNode),
                    BlockNode blockNode => TransformBlock(blockNode),
                    _ => CreateErrorStatement($"Unsupported statement type: {stmtNode.GetType().Name}")
                };
            }
            catch (Exception ex)
            {
                return CreateErrorStatement($"Error transforming statement: {ex.Message}");
            }
        }

        /// <summary>
        /// Transforms an assignment statement
        /// </summary>
        private HighLevelIR.Statement TransformAssignment(AssignmentNode assignNode)
        {
            if (assignNode == null)
                return CreateErrorStatement("Assignment node is null");

            var target = TransformExpressionNode(assignNode.Left);
            var value = TransformExpressionNode(assignNode.Right);

            if (target == null || value == null)
                return CreateErrorStatement("Failed to transform assignment operands");

            return new HighLevelIR.Statement(_context.SourceFile);
        }

        /// <summary>
        /// Transforms an if statement
        /// </summary>
        private HighLevelIR.Statement TransformIfStatement(IfNode ifNode)
        {
            if (ifNode == null)
                return CreateErrorStatement("If node is null");

            var condition = TransformExpressionNode(ifNode.Condition);
            if (condition == null)
                return CreateErrorStatement("Failed to transform if condition");

            var thenBlock = TransformStatement(ifNode.ThenBlock);
            if (thenBlock == null)
                return CreateErrorStatement("Failed to transform then block");

            HighLevelIR.Statement elseBlock = null;
            if (ifNode.ElseBlock != null)
            {
                elseBlock = TransformStatement(ifNode.ElseBlock);
                if (elseBlock == null)
                    return CreateErrorStatement("Failed to transform else block");
            }

            return new HighLevelIR.Statement(_context.SourceFile);
        }

        /// <summary>
        /// Transforms a while loop statement
        /// </summary>
        private HighLevelIR.Statement TransformWhileLoop(WhileNode whileNode)
        {
            if (whileNode == null)
                return CreateErrorStatement("While node is null");

            var condition = TransformExpressionNode(whileNode.Condition);
            if (condition == null)
                return CreateErrorStatement("Failed to transform while condition");

            var loopBody = TransformStatement(whileNode.Block);
            if (loopBody == null)
                return CreateErrorStatement("Failed to transform while loop body");

            return new HighLevelIR.Statement(_context.SourceFile);
        }

        /// <summary>
        /// Transforms a for loop statement
        /// </summary>
        private HighLevelIR.Statement TransformForLoop(ForNode forNode)
        {
            if (forNode == null)
                return CreateErrorStatement("For node is null");

            var fromExpr = TransformExpressionNode(forNode.FromExpression);
            var toExpr = TransformExpressionNode(forNode.ToExpression);

            if (fromExpr == null || toExpr == null)
                return CreateErrorStatement("Failed to transform for loop bounds");

            var loopBody = TransformStatement(forNode.Block);
            if (loopBody == null)
                return CreateErrorStatement("Failed to transform for loop body");

            return new HighLevelIR.Statement(_context.SourceFile);
        }

        /// <summary>
        /// Transforms a repeat-until loop statement
        /// </summary>
        private HighLevelIR.Statement TransformRepeatLoop(RepeatNode repeatNode)
        {
            if (repeatNode == null)
                return CreateErrorStatement("Repeat node is null");

            var loopBody = TransformStatement(repeatNode.Block);
            if (loopBody == null)
                return CreateErrorStatement("Failed to transform repeat loop body");

            var condition = TransformExpressionNode(repeatNode.Condition);
            if (condition == null)
                return CreateErrorStatement("Failed to transform repeat until condition");

            return new HighLevelIR.Statement(_context.SourceFile);
        }

        /// <summary>
        /// Transforms a procedure call statement
        /// </summary>
        private HighLevelIR.Statement TransformProcedureCall(ProcedureCallNode procCallNode)
        {
            if (procCallNode == null)
                return CreateErrorStatement("Procedure call node is null");

            var arguments = new List<HighLevelIR.Expression>();
            foreach (var argNode in procCallNode.Arguments)
            {
                var arg = TransformExpressionNode(argNode);
                if (arg == null)
                    return CreateErrorStatement("Failed to transform procedure argument");
                arguments.Add(arg);
            }

            return new HighLevelIR.Statement(_context.SourceFile);
        }

        /// <summary>
        /// Transforms a block (compound statement)
        /// </summary>
        private HighLevelIR.Statement TransformBlock(BlockNode blockNode)
        {
            if (blockNode == null)
                return CreateErrorStatement("Block node is null");

            var statements = new List<HighLevelIR.Statement>();
            foreach (var stmt in blockNode.Statements)
            {
                var transformedStmt = TransformStatement(stmt);
                if (transformedStmt != null)
                    statements.Add(transformedStmt);
            }

            return new HighLevelIR.Statement(_context.SourceFile);
        }

        /// <summary>
        /// Transforms a generic PascalASTNode that might be an expression
        /// </summary>
        private HighLevelIR.Expression TransformExpressionNode(PascalASTNode node)
        {
            if (node is ExpressionNode exprNode)
                return _expressionTransformer.TransformExpression(exprNode);
            return null;
        }

        /// <summary>
        /// Creates an error statement
        /// </summary>
        private HighLevelIR.Statement CreateErrorStatement(string message)
        {
            _context.AddError(message);
            return new HighLevelIR.Statement(_context.SourceFile);
        }
    }
}
