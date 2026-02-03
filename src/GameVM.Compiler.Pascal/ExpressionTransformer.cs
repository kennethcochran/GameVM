using System;
using System.Collections.Generic;
using System.Linq;
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
        public HighLevelIR.Expression? TransformExpression(PascalAstNode astNode)
        {
            if (astNode == null)
                return null;

            try
            {
                if (astNode is VariableNode varNode)
                    return TransformVariable(varNode);
                if (astNode is ConstantNode constNode)
                    return TransformConstant(constNode);
                if (astNode is LiteralNode literalNode)
                    return TransformLiteral(literalNode);
                if (astNode is FunctionCallNode callNode)
                    return TransformFunctionCall(callNode);
                if (astNode is RelationalOperatorNode relOpNode)
                    return TransformRelationalOperator(relOpNode);
                if (astNode is AdditiveOperatorNode addOpNode)
                    return TransformAdditiveOperator(addOpNode);
                if (astNode is MultiplicativeOperatorNode mulOpNode)
                    return TransformMultiplicativeOperator(mulOpNode);
                if (astNode is UnaryOperatorNode unaryOpNode)
                    return TransformUnaryOperator(unaryOpNode);

                return CreateErrorExpression($"Unsupported expression type: {astNode.GetType().Name}");
            }
            catch (Exception ex)
            {
                return CreateErrorExpression($"Error transforming expression: {ex.Message}");
            }
        }

        /// <summary>
        /// Transforms a function call node to a High-Level IR expression
        /// </summary>
        private HighLevelIR.Expression TransformFunctionCall(FunctionCallNode callNode)
        {
            if (callNode == null)
                return CreateErrorExpression("Function call node is null");

            var arguments = TransformFunctionArguments(callNode.Arguments);
            if (arguments == null)
                return CreateErrorExpression("Failed to transform function arguments");

            var type = ResolveFunctionReturnType(callNode.Name);
            var funcExpr = new HighLevelIR.Identifier(callNode.Name, type, _context.SourceFile);
            var result = new HighLevelIR.FunctionCall(funcExpr, arguments);
            result.Type = type;
            return result;
        }

        private List<HighLevelIR.Expression>? TransformFunctionArguments(List<PascalAstNode> argumentNodes)
        {
            var arguments = new List<HighLevelIR.Expression>();
            foreach (var argNode in argumentNodes)
            {
                var arg = TransformExpression(argNode);
                if (arg == null)
                    return null;
                arguments.Add(arg);
            }
            return arguments;
        }

        private HighLevelIR.HlType ResolveFunctionReturnType(string functionName)
        {
            // Check if it's a known symbol
            var symbol = _context.LookupSymbol(functionName);
            if (symbol is HighLevelIR.Variable varSymbol)
                return varSymbol.Type;

            // Check global functions
            if (_context.IR.GlobalFunctions.TryGetValue(functionName, out var globalFunc))
                return globalFunc.ReturnType;

            // Search for the function in all modules
            var func = _context.IR.Modules
                .SelectMany(m => m.Functions)
                .FirstOrDefault(f => f.Name == functionName);
            
            return func?.ReturnType ?? _context.GetOrCreateBasicType("i32");
        }

        /// <summary>
        /// Transforms a variable reference to a High-Level IR expression
        /// </summary>
        private HighLevelIR.Expression TransformVariable(VariableNode varNode)
        {
            if (varNode == null)
                return CreateErrorExpression("Variable node is null");

            // Look up the variable in the symbol table (with scoping)
            var symbol = _context.LookupSymbol(varNode.Name);
            if (symbol != null)
            {
                var type = (HighLevelIR.HlType)symbol.Type;
                var ident = new HighLevelIR.Identifier(varNode.Name, type, _context.SourceFile);
                ident.Type = type;
                return ident;
            }

            return CreateErrorExpression($"Undefined variable: {varNode.Name}");
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
                return TryTransformConstantValue(constNode.Value);
            }
            catch (Exception ex)
            {
                return CreateErrorExpression($"Error transforming constant: {ex.Message}");
            }
        }

        private HighLevelIR.Expression TryTransformConstantValue(object? value)
        {
            if (value == null)
                return CreateErrorExpression("Constant value is null");

            var stringValue = value.ToString();
            
            // Try integer
            if (int.TryParse(stringValue, out int intValue))
                return CreateTypedLiteral(intValue, "i32");

            // Try double
            if (double.TryParse(stringValue, out double doubleValue))
                return CreateTypedLiteral(doubleValue, "f64");

            // Try boolean
            if (bool.TryParse(stringValue, out bool boolValue))
                return CreateTypedLiteral(boolValue, "bool");

            // Handle string
            if (value is string strValue)
                return CreateTypedLiteral(strValue, "string");

            return CreateErrorExpression($"Unsupported constant type: {value.GetType().Name}");
        }

        private HighLevelIR.Expression CreateTypedLiteral(object value, string typeName)
        {
            var type = _context.GetOrCreateBasicType(typeName);
            var result = new HighLevelIR.Literal(value, type, _context.SourceFile);
            result.Type = type;
            return result;
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

            var result = new HighLevelIR.BinaryOp(relOpNode.Operator, left, right, _context.SourceFile);
            result.Type = _context.GetOrCreateBasicType("bool");
            return result;
        }

        /// <summary>
        /// Transforms an additive operator expression
        /// </summary>
        private HighLevelIR.Expression TransformAdditiveOperator(AdditiveOperatorNode addOpNode)
        {
            if (addOpNode == null)
                return CreateErrorExpression("Additive operator node is null");

            var left = TransformExpression(addOpNode.Left);
            var right = TransformExpression(addOpNode.Right);

            if (left == null || right == null)
                return CreateErrorExpression("Failed to transform additive operator operands");

            var result = new HighLevelIR.BinaryOp(addOpNode.Operator, left, right, _context.SourceFile);
            result.Type = left.Type ?? right.Type ?? _context.GetOrCreateBasicType("i32");
            return result;
        }

        /// <summary>
        /// Transforms a multiplicative operator expression
        /// </summary>
        private HighLevelIR.Expression TransformMultiplicativeOperator(MultiplicativeOperatorNode mulOpNode)
        {
            if (mulOpNode == null)
                return CreateErrorExpression("Multiplicative operator node is null");

            var left = TransformExpression(mulOpNode.Left);
            var right = TransformExpression(mulOpNode.Right);

            if (left == null || right == null)
                return CreateErrorExpression("Failed to transform multiplicative operator operands");

            var result = new HighLevelIR.BinaryOp(mulOpNode.Operator, left, right, _context.SourceFile);
            result.Type = left.Type ?? right.Type ?? _context.GetOrCreateBasicType("i32");
            return result;
        }

        /// <summary>
        /// Transforms a unary operator expression
        /// </summary>
        private HighLevelIR.Expression TransformUnaryOperator(UnaryOperatorNode unaryOpNode)
        {
            if (unaryOpNode == null)
                return CreateErrorExpression("Unary operator node is null");

            var operand = TransformExpression(unaryOpNode.Operand);
            if (operand == null)
                return CreateErrorExpression("Failed to transform unary operator operand");

            var result = new HighLevelIR.UnaryOp(unaryOpNode.Operator, operand, _context.SourceFile);
            result.Type = operand.Type;
            return result;
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

            var result = new HighLevelIR.Literal(literalNode.Value, type, _context.SourceFile);
            result.Type = type; // Set the Literal.Type property
            
            // Also set the base Expression.Type property for consistency
            ((HighLevelIR.Expression)result).Type = type;
            
            return result;
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
