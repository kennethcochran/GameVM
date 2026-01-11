using System;
using System.Collections.Generic;
using GameVM.Compiler.Core.IR;

namespace GameVM.Compiler.Pascal
{
    /// <summary>
    /// Transforms expression AST nodes to High-Level IR expressions
    /// </summary>
    public class ExpressionTransformer
    {
        private readonly TransformationContext _context;

        public ExpressionTransformer(TransformationContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Transforms an expression node to a High-Level IR expression
        /// </summary>
        public HighLevelIR.Expression TransformExpression(ExpressionNode exprNode)
        {
            if (exprNode == null)
                return CreateErrorExpression("Expression node is null");

            try
            {
                if (exprNode is VariableNode varNode)
                    return TransformVariable(varNode);
                if (exprNode is ConstantNode constNode)
                    return TransformConstant(constNode);
                if (exprNode is LiteralNode literalNode)
                    return TransformLiteral(literalNode);
                if (exprNode is RelationalOperatorNode relOpNode)
                    return TransformRelationalOperator(relOpNode);

                // For operator nodes, cast from PascalASTNode
                var astNode = exprNode as PascalASTNode;
                if (astNode is AdditiveOperatorNode addOpNode)
                    return TransformAdditiveOperator(addOpNode);
                if (astNode is MultiplicativeOperatorNode mulOpNode)
                    return TransformMultiplicativeOperator(mulOpNode);
                if (astNode is UnaryOperatorNode unaryOpNode)
                    return TransformUnaryOperator(unaryOpNode);

                return CreateErrorExpression($"Unsupported expression type: {exprNode.GetType().Name}");
            }
            catch (Exception ex)
            {
                return CreateErrorExpression($"Error transforming expression: {ex.Message}");
            }
        }

        /// <summary>
        /// Transforms a variable reference to a High-Level IR expression
        /// </summary>
        private HighLevelIR.Expression TransformVariable(VariableNode varNode)
        {
            if (varNode == null)
                return CreateErrorExpression("Variable node is null");

            // Look up the variable in the symbol table
            if (_context.SymbolTable.TryGetValue(varNode.Name, out var symbol))
            {
                return new HighLevelIR.Identifier(varNode.Name, (HighLevelIR.HLType)symbol.Type, _context.SourceFile);
            }

            return CreateErrorExpression($"Variable '{varNode.Name}' not declared");
        }

        /// <summary>
        /// Transforms a constant to a High-Level IR expression
        /// </summary>
        private HighLevelIR.Expression TransformConstant(ConstantNode constNode)
        {
            if (constNode == null)
                return CreateErrorExpression("Constant node is null");

            try
            {
                // Try to parse as integer
                if (int.TryParse(constNode.Value?.ToString(), out int intValue))
                {
                    return new HighLevelIR.Literal(intValue, _context.GetOrCreateBasicType("i32"), _context.SourceFile);
                }

                // Try to parse as double
                if (double.TryParse(constNode.Value?.ToString(), out double doubleValue))
                {
                    return new HighLevelIR.Literal(doubleValue, _context.GetOrCreateBasicType("f64"), _context.SourceFile);
                }

                // Try to parse as boolean
                if (bool.TryParse(constNode.Value?.ToString(), out bool boolValue))
                {
                    return new HighLevelIR.Literal(boolValue, _context.GetOrCreateBasicType("bool"), _context.SourceFile);
                }

                if (constNode.Value is string strValue)
                {
                    return new HighLevelIR.Literal(strValue, _context.GetOrCreateBasicType("string"), _context.SourceFile);
                }

                return CreateErrorExpression($"Unsupported constant type: {constNode.Value?.GetType().Name}");
            }
            catch (Exception ex)
            {
                return CreateErrorExpression($"Error transforming constant: {ex.Message}");
            }
        }

        /// <summary>
        /// Transforms a relational operator expression
        /// </summary>
        private HighLevelIR.Expression TransformRelationalOperator(RelationalOperatorNode relOpNode)
        {
            if (relOpNode == null)
                return CreateErrorExpression("Relational operator node is null");

            var left = TransformExpression(relOpNode.Left);
            var right = TransformExpression(relOpNode.Right);

            if (left == null || right == null)
                return CreateErrorExpression("Failed to transform relational operator operands");

            return new HighLevelIR.BinaryOp(relOpNode.Operator, left, right, _context.SourceFile);
        }

        /// <summary>
        /// Transforms an additive operator expression
        /// </summary>
        private HighLevelIR.Expression TransformAdditiveOperator(AdditiveOperatorNode addOpNode)
        {
            if (addOpNode == null)
                return CreateErrorExpression("Additive operator node is null");

            var left = TransformExpression(addOpNode.Left as ExpressionNode);
            var right = TransformExpression(addOpNode.Right as ExpressionNode);

            if (left == null || right == null)
                return CreateErrorExpression("Failed to transform additive operator operands");

            return new HighLevelIR.BinaryOp(addOpNode.Operator, left, right, _context.SourceFile);
        }

        /// <summary>
        /// Transforms a multiplicative operator expression
        /// </summary>
        private HighLevelIR.Expression TransformMultiplicativeOperator(MultiplicativeOperatorNode mulOpNode)
        {
            if (mulOpNode == null)
                return CreateErrorExpression("Multiplicative operator node is null");

            var left = TransformExpression(mulOpNode.Left as ExpressionNode);
            var right = TransformExpression(mulOpNode.Right as ExpressionNode);

            if (left == null || right == null)
                return CreateErrorExpression("Failed to transform multiplicative operator operands");

            return new HighLevelIR.BinaryOp(mulOpNode.Operator, left, right, _context.SourceFile);
        }

        /// <summary>
        /// Transforms a unary operator expression
        /// </summary>
        private HighLevelIR.Expression TransformUnaryOperator(UnaryOperatorNode unaryOpNode)
        {
            if (unaryOpNode == null)
                return CreateErrorExpression("Unary operator node is null");

            var operand = TransformExpression(unaryOpNode.Operand as ExpressionNode);
            if (operand == null)
                return CreateErrorExpression("Failed to transform unary operator operand");

            return new HighLevelIR.Expression(_context.SourceFile);
        }

        /// <summary>
        /// Transforms a literal to a High-Level IR expression
        /// </summary>
        private HighLevelIR.Expression TransformLiteral(LiteralNode literalNode)
        {
            if (literalNode == null)
                return CreateErrorExpression("Literal node is null");

            var type = _context.GetOrCreateBasicType("i32"); // Default
            if (literalNode is IntegerLiteralNode) type = _context.GetOrCreateBasicType("i32");
            else if (literalNode is RealLiteralNode) type = _context.GetOrCreateBasicType("f64");
            else if (literalNode is BooleanLiteralNode) type = _context.GetOrCreateBasicType("bool");
            else if (literalNode is StringLiteralNode) type = _context.GetOrCreateBasicType("string");

            return new HighLevelIR.Literal(literalNode.Value, type, _context.SourceFile);
        }

        /// <summary>
        /// Creates an error expression
        /// </summary>
        private HighLevelIR.Expression CreateErrorExpression(string message)
        {
            _context.AddError(message);
            return new HighLevelIR.Expression(_context.SourceFile);
        }
    }
}
