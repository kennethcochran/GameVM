using GameVM.Compiler.Pascal.ANTLR;
using System.Collections.Generic;
using System.Linq;

namespace GameVM.Compiler.Pascal
{
    /// <summary>
    /// Visitor for expression-related AST nodes in Pascal
    /// </summary>
    public class ExpressionVisitor : PascalBaseVisitor<PascalAstNode>
    {
        private readonly AstBuilder _astBuilder;

        public ExpressionVisitor(AstBuilder astBuilder)
        {
            _astBuilder = astBuilder ?? throw new ArgumentNullException(nameof(astBuilder));
        }

        // Constructor for backward compatibility if needed, or update call sites
        // Eliminating default constructor to enforce dependency injection

        public override PascalAstNode VisitExpression(PascalParser.ExpressionContext context)
        {
            if (context.relationaloperator() != null)
            {
                var left = Visit(context.simpleExpression()) as ExpressionNode;
                var right = Visit(context.expression()) as ExpressionNode;
                if (left == null || right == null)
                {
                    return new ErrorNode("Relational operation missing operand");
                }
                return _astBuilder.CreateRelational(left, context.relationaloperator().GetText(), right);
            }
            var simpleExpression = Visit(context.simpleExpression());
            return simpleExpression ?? new ErrorNode("Invalid simple expression");
        }

        public override PascalAstNode VisitSimpleExpression(PascalParser.SimpleExpressionContext context)
        {
            if (context.additiveoperator() != null)
            {
                var left = Visit(context.term()) as ExpressionNode;
                var right = Visit(context.simpleExpression()) as ExpressionNode;
                if (left == null || right == null)
                {
                    return new ErrorNode("Additive operation missing operand");
                }
                return _astBuilder.CreateAdditive(left, context.additiveoperator().GetText(), right);
            }
            var term = Visit(context.term());
            return term ?? new ErrorNode("Invalid term");
        }

        public override PascalAstNode VisitTerm(PascalParser.TermContext context)
        {
            if (context.multiplicativeoperator() != null)
            {
                var left = Visit(context.signedFactor()) as ExpressionNode;
                var right = Visit(context.term()) as ExpressionNode;
                if (left == null || right == null)
                {
                    return new ErrorNode("Multiplicative operation missing operand");
                }
                return _astBuilder.CreateMultiplicative(left, context.multiplicativeoperator().GetText(), right);
            }
            var signedFactor = Visit(context.signedFactor());
            return signedFactor ?? new ErrorNode("Invalid signed factor");
        }

        public override PascalAstNode VisitSignedFactor(PascalParser.SignedFactorContext context)
        {
            if (context == null)
                return new ErrorNode("Null signed factor context");

            var factorCtx = context.factor();
            if (factorCtx == null)
                return new ErrorNode("Missing factor");

            var factor = Visit(factorCtx);
            if (factor == null)
            {
                return new ErrorNode("Invalid factor");
            }
            string? sign;
            if (context.PLUS() != null)
                sign = "+";
            else if (context.MINUS() != null)
                sign = "-";
            else
                sign = null;
            if (factor is ErrorNode)
            {
                return factor; // Propagate the error
            }
            if (factor is ExpressionNode expressionNode && sign != null)
            {
                return _astBuilder.CreateUnary(sign, expressionNode);
            }
            return factor;
        }

        public override PascalAstNode VisitFactor(PascalParser.FactorContext context)
        {
            if (context.variable() != null)
            {
                return Visit(context.variable()) ?? new ErrorNode("Invalid variable");
            }
            if (context.expression() != null)
            {
                var expression = Visit(context.expression()) as ExpressionNode;
                if (expression == null)
                {
                    return new ErrorNode("Invalid parenthesized expression");
                }
                return expression;
            }
            if (context.unsignedConstant() != null)
            {
                var constant = context.unsignedConstant().GetText();
                // Heuristic to determine type based on string format
                if (constant.Contains('.') || constant.Contains('e') || constant.Contains('E'))
                    return _astBuilder.CreateRealLiteral(constant);

                if (long.TryParse(constant, out _))
                    return _astBuilder.CreateIntegerLiteral(constant);

                return _astBuilder.CreateConstant(constant);
            }
            if (context.set_() != null)
            {
                return Visit(context.set_());
            }
            return new ErrorNode("Unknown factor");
        }

        public override PascalAstNode VisitVariable(PascalParser.VariableContext context)
        {
            var identifier = context.identifier();
            if (identifier == null || identifier.Length == 0)
            {
                return new ErrorNode("Variable must have an identifier");
            }
            return _astBuilder.CreateVariable(identifier[0].GetText());
        }

        public override PascalAstNode VisitSet_(PascalParser.Set_Context context)
        {
            var elements = new List<ExpressionNode>();
            var elementList = context.elementList();
            if (elementList == null) return _astBuilder.CreateSet(elements);

            foreach (var elementCtx in elementList.element())
            {
                ProcessSetElement(elementCtx, elements);
            }

            return _astBuilder.CreateSet(elements);
        }

        private static void ProcessSetElement(PascalParser.ElementContext elementCtx, List<ExpressionNode> elements)
        {
            if (elementCtx.expression().Length == 2)
            {
                ProcessSetRange(elementCtx, elements);
            }
            else if (elementCtx.expression().Length == 1)
            {
                ProcessSingleSetElement(elementCtx, elements);
            }
        }

        private static void ProcessSetRange(PascalParser.ElementContext elementCtx, List<ExpressionNode> elements)
        {
            var low = elementCtx.expression(0);
            var high = elementCtx.expression(1);
            
            if (low != null)
            {
                elements.Add(new VariableNode { Name = low.GetText() });
            }
            if (high != null)
            {
                elements.Add(new VariableNode { Name = high.GetText() });
            }
        }

        private static void ProcessSingleSetElement(PascalParser.ElementContext elementCtx, List<ExpressionNode> elements)
        {
            var expr = elementCtx.expression(0);
            if (expr != null)
            {
                elements.Add(new VariableNode { Name = expr.GetText() });
            }
        }

        public override PascalAstNode VisitConstant(PascalParser.ConstantContext context)
        {
            // Handle numeric constants
            if (context.unsignedNumber() != null)
            {
                var number = context.unsignedNumber().GetText();
                if (number.Contains('.') || number.Contains('e') || number.Contains('E'))
                    return _astBuilder.CreateRealLiteral(number);
                return _astBuilder.CreateIntegerLiteral(number);
            }
            // Handle other constant types
            var text = context.GetText();
            if (text.StartsWith('\''))
                return _astBuilder.CreateStringLiteral(text);

            return _astBuilder.CreateConstant(text);
        }

        protected override PascalAstNode DefaultResult => null!;
    }
}
