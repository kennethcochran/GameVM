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

            var targetExpr = TransformExpressionNode(assignNode.Left);
            var value = TransformExpressionNode(assignNode.Right);

            if (targetExpr == null || value == null)
            {
                // Special case for Pascal return value assignment: Double := x * 2;
                // Double might not be a variable, but the current function name.
                if (assignNode.Left is VariableNode varNode)
                {
                    if (_context.FunctionScope.Count > 0 && 
                        _context.FunctionScope.Peek().Name.Equals(varNode.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        if (value == null) return CreateErrorStatement("Failed to transform assignment value");
                        return new HighLevelIR.Assignment(varNode.Name, value, _context.SourceFile);
                    }
                }
                return CreateErrorStatement("Failed to transform assignment operands");
            }

            if (targetExpr is HighLevelIR.Identifier identifier)
            {
                return new HighLevelIR.Assignment(identifier.Name, value, _context.SourceFile);
            }

            return CreateErrorStatement("Assignment target must be an identifier");
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

            var thenStmt = TransformStatement(ifNode.ThenBlock);
            if (thenStmt == null)
                return CreateErrorStatement("Failed to transform then block");

            List<IRNode> thenList = thenStmt is HighLevelIR.Block block ? new List<IRNode>(block.Statements) : new List<IRNode> { thenStmt };

            List<IRNode> elseList = null;
            if (ifNode.ElseBlock != null)
            {
                var elseStmt = TransformStatement(ifNode.ElseBlock);
                if (elseStmt != null)
                {
                    elseList = elseStmt is HighLevelIR.Block elseBlock ? new List<IRNode>(elseBlock.Statements) : new List<IRNode> { elseStmt };
                }
            }

            return new HighLevelIR.IfStatement(condition, thenList, elseList);
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

            var loopBodyStmt = TransformStatement(whileNode.Block);
            if (loopBodyStmt == null)
                return CreateErrorStatement("Failed to transform while loop body");

            HighLevelIR.Block irBlock = loopBodyStmt as HighLevelIR.Block ?? new HighLevelIR.Block(_context.SourceFile);
            if (loopBodyStmt is not HighLevelIR.Block)
            {
                irBlock.AddStatement(loopBodyStmt);
            }

            return new HighLevelIR.While(condition, irBlock, _context.SourceFile);
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

            var loopBodyStmt = TransformStatement(forNode.Block);
            if (loopBodyStmt == null)
                return CreateErrorStatement("Failed to transform for loop body");

            // For now, we'll transform this to a while loop in HLIR or just leave a placeholder
            // A real for loop would need an iterator variable.
            // For simplicity in this E2E, we'll just return a basic statement or implement it as while.
            return new HighLevelIR.Statement(_context.SourceFile);
        }

        /// <summary>
        /// Transforms a repeat-until loop statement
        /// </summary>
        private HighLevelIR.Statement TransformRepeatLoop(RepeatNode repeatNode)
        {
            if (repeatNode == null)
                return CreateErrorStatement("Repeat node is null");

            var loopBodyStmt = TransformStatement(repeatNode.Block);
            if (loopBodyStmt == null)
                return CreateErrorStatement("Failed to transform repeat loop body");

            var condition = TransformExpressionNode(repeatNode.Condition);
            if (condition == null)
                return CreateErrorStatement("Failed to transform repeat until condition");

            // Transform to while loop or similar
            return new HighLevelIR.Statement(_context.SourceFile);
        }

        /// <summary>
        /// Transforms a procedure call statement
        /// </summary>
        private HighLevelIR.Statement TransformProcedureCall(ProcedureCallNode procCallNode)
        {
            if (procCallNode == null)
                return CreateErrorStatement("Procedure call node is null");

            // Built-in functions
            if (procCallNode.Name.Equals("write", StringComparison.OrdinalIgnoreCase) || 
                procCallNode.Name.Equals("writeln", StringComparison.OrdinalIgnoreCase))
            {
                var args = new List<HighLevelIR.Expression>();
                foreach (var argNode in procCallNode.Arguments)
                {
                    if (argNode is ExpressionNode expr)
                    {
                        var transformedArg = _expressionTransformer.TransformExpression(expr);
                        if (transformedArg != null) args.Add(transformedArg);
                    }
                }
                var writeFunc = new HighLevelIR.Identifier(procCallNode.Name, _context.GetOrCreateBasicType("void"), _context.SourceFile);
                return new HighLevelIR.ExpressionStatement(new HighLevelIR.FunctionCall(writeFunc, args), _context.SourceFile);
            }

            var arguments = new List<HighLevelIR.Expression>();
            foreach (var argNode in procCallNode.Arguments)
            {
                var arg = TransformExpressionNode(argNode);
                if (arg == null)
                    return CreateErrorStatement("Failed to transform procedure argument");
                arguments.Add(arg);
            }

            var funcExpr = new HighLevelIR.Identifier(procCallNode.Name, _context.GetOrCreateBasicType("void"), _context.SourceFile);
            var callExpr = new HighLevelIR.FunctionCall(funcExpr, arguments);
            return new HighLevelIR.ExpressionStatement(callExpr, _context.SourceFile);
        }

        /// <summary>
        /// Transforms a block (compound statement)
        /// </summary>
        private HighLevelIR.Statement TransformBlock(BlockNode blockNode)
        {
            if (blockNode == null)
                return CreateErrorStatement("Block node is null");

            var irBlock = new HighLevelIR.Block(_context.SourceFile);
            foreach (var stmt in blockNode.Statements)
            {
                var transformedStmt = TransformStatement(stmt);
                if (transformedStmt != null)
                    irBlock.AddStatement(transformedStmt);
            }

            return irBlock;
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
