using System;
using System.Collections.Generic;
using GameVM.Compiler.Core.IR;

namespace GameVM.Compiler.Pascal
{
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

        public HighLevelIR.Statement TransformStatement(PascalAstNode stmtNode)
        {
            if (stmtNode == null)
                return CreateErrorStatement("Statement node is null");

            try
            {
                return stmtNode switch
                {
                    AssignmentNode assignNode => TransformAssignment(assignNode),
                    ExpressionStatementNode exprStmtNode => TransformExpressionStatement(exprStmtNode),
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

        private HighLevelIR.Statement TransformAssignment(AssignmentNode assignNode)
        {
            if (assignNode == null)
                return CreateErrorStatement("Assignment node is null");

            var targetExpr = TransformExpressionNode(assignNode.Left);
            var value = TransformExpressionNode(assignNode.Right);

            if (targetExpr == null || value == null)
            {
                return HandleFailedExpressionTransformation(assignNode, value);
            }

            ValidateTypeCompatibility(targetExpr, value);

            return CreateAssignmentStatement(targetExpr, value);
        }

        private HighLevelIR.Statement HandleFailedExpressionTransformation(
            AssignmentNode assignNode, 
            HighLevelIR.Expression? value)
        {
            // Handle function return assignment case
            if (IsFunctionReturnAssignment(assignNode, value))
            {
                return HandleFunctionReturnAssignment(assignNode, value!);
            }
            
            return CreateErrorStatement("Failed to transform assignment operands");
        }

        private bool IsFunctionReturnAssignment(
            AssignmentNode assignNode, 
            HighLevelIR.Expression? value)
        {
            return assignNode.Left is VariableNode varNode 
                   && _context.FunctionScope.Count > 0 
                   && _context.FunctionScope.Peek().Name.Equals(varNode.Name, StringComparison.OrdinalIgnoreCase) 
                   && value != null;
        }

        private HighLevelIR.Statement HandleFunctionReturnAssignment(AssignmentNode assignNode, HighLevelIR.Expression value)
        {
            var varNode = (VariableNode)assignNode.Left;
            var returnType = _context.FunctionScope.Peek().ReturnType;
            
            ValidateFunctionReturnType(returnType, value);

            return new HighLevelIR.Assignment(varNode.Name, value, _context.SourceFile);
        }

        private void ValidateFunctionReturnType(HighLevelIR.HlType? returnType, HighLevelIR.Expression value)
        {
            if (IsFunctionReturnTypeMismatch(returnType, value))
            {
                AddFunctionReturnTypeMismatchError(returnType!, value);
            }
        }

        private static bool IsFunctionReturnTypeMismatch(HighLevelIR.HlType? returnType, HighLevelIR.Expression value)
        {
            return returnType != null 
                   && value.Type != null 
                   && !IsCompatible(returnType, value.Type);
        }

        private void AddFunctionReturnTypeMismatchError(HighLevelIR.HlType returnType, HighLevelIR.Expression value)
        {
            _context.AddError($"Type mismatch: Cannot assign {value.Type!.Name} to return type {returnType.Name}");
        }

        private void ValidateTypeCompatibility(HighLevelIR.Expression targetExpr, HighLevelIR.Expression value)
        {
            if (targetExpr.Type != null && value.Type != null && !IsCompatible(targetExpr.Type, value.Type))
            {
                _context.AddError($"Type mismatch: Cannot assign {value.Type.Name} to {targetExpr.Type.Name}");
            }
        }

        private HighLevelIR.Statement CreateAssignmentStatement(HighLevelIR.Expression targetExpr, HighLevelIR.Expression value)
        {
            if (targetExpr is HighLevelIR.Identifier identifier)
            {
                return new HighLevelIR.Assignment(identifier.Name, value, _context.SourceFile);
            }

            return CreateErrorStatement("Assignment target must be an identifier");
        }

        private static bool IsCompatible(HighLevelIR.HlType targetType, HighLevelIR.HlType valueType)
        {
            if (targetType.Name.Equals(valueType.Name, StringComparison.OrdinalIgnoreCase))
                return true;

            if (targetType.Name.Equals("f64", StringComparison.OrdinalIgnoreCase) && 
                valueType.Name.Equals("i32", StringComparison.OrdinalIgnoreCase))
                return true;
            
            if (targetType.Name.Equals("real", StringComparison.OrdinalIgnoreCase) && 
                valueType.Name.Equals("integer", StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }

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

            List<IRNode>? elseList = null;
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

            return new HighLevelIR.While { Condition = condition, Body = irBlock, SourceFile = _context.SourceFile };
        }

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

            return new HighLevelIR.Statement(_context.SourceFile);
        }

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

            return CreateRepeatLoopStatement(loopBodyStmt, condition);
        }

        private HighLevelIR.Statement CreateRepeatLoopStatement(HighLevelIR.Statement loopBodyStmt, HighLevelIR.Expression condition)
        {
            // Create a basic statement for now - could be enhanced later to create proper repeat loop IR
            // Log the transformation details for debugging purposes
            Console.WriteLine($"Creating repeat loop with condition type: {condition?.Type?.Name ?? "null"}");
            Console.WriteLine($"Loop body statement type: {loopBodyStmt?.GetType().Name ?? "null"}");
            
            return new HighLevelIR.Statement(_context.SourceFile);
        }

        private HighLevelIR.Statement TransformProcedureCall(ProcedureCallNode procCallNode)
        {
            if (procCallNode == null)
                return CreateErrorStatement("Procedure call node is null");

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
                return new HighLevelIR.ExpressionStatement { Expression = new HighLevelIR.FunctionCall(writeFunc, args), SourceFile = _context.SourceFile };
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
            return new HighLevelIR.ExpressionStatement { Expression = callExpr, SourceFile = _context.SourceFile };
        }

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

        private HighLevelIR.Expression? TransformExpressionNode(PascalAstNode node)
        {
            if (node is ExpressionNode exprNode)
                return _expressionTransformer.TransformExpression(exprNode);
            return null;
        }

        private HighLevelIR.Statement TransformExpressionStatement(ExpressionStatementNode exprStmtNode)
        {
            if (exprStmtNode?.Expression == null)
                return CreateErrorStatement("Expression statement node is null");

            if (exprStmtNode.Expression is AssignmentNode assignNode)
                return TransformAssignment(assignNode);
            
            if (exprStmtNode.Expression is ProcedureCallNode procCallNode)
                return TransformProcedureCall(procCallNode);

            return CreateErrorStatement($"Unsupported expression type in statement: {exprStmtNode.Expression.GetType().Name}");
        }

        private HighLevelIR.Statement CreateErrorStatement(string message)
        {
            _context.AddError(message);
            return new HighLevelIR.Statement(_context.SourceFile);
        }
    }
}
