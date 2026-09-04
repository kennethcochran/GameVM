using GameVM.Compiler.Core.IR.Ast;
using GameVM.Compiler.Core.IR.Soa;
using GameVM.Compiler.Core.IR.Buffers;
using Antlr4.Runtime;
using GameVM.Compiler.CSharp.ANTLR;

namespace GameVM.Compiler.CSharp.Transformers
{
    /// <summary>
    /// ANTLR visitor that builds a C# AoS AST from the parse tree.
    /// Produces an <see cref="AstTree"/> via <see cref="AstBuilder"/> in post-order.
    /// </summary>

    public class CSharpToAstVisitor : CSharpBaseVisitor<object>
    {
        private readonly AstBuilder _builder;
        private readonly StringPool _stringPool;

        public CSharpToAstVisitor(AstBuilder builder, StringPool stringPool)
        {
            _builder = builder ?? throw new ArgumentNullException(nameof(builder));
            _stringPool = stringPool ?? throw new ArgumentNullException(nameof(stringPool));
        }

        public AstTree GetTree()
        {
            return _builder.Build();
        }

        public override object VisitVariableDeclaration(CSharpParser.VariableDeclarationContext context)
        {
            // VARIABLE_DECLARATION: payload = typeKind, child = [nameIdentifier, initExpression?]
            string typeNameStr = context.type().GetText();
            byte typeKind = typeNameStr switch
            {
                "int" => 1,
                "string" => 2,
                "bool" => 3,
                "int32" => 1,
                "int64" => 1,
                _ => 4
            };

            string varName = context.identifier().GetText();
            uint nameOffset = _stringPool.Intern(varName);
            int nameIdx = _builder.Add((byte)CSharpAstNodeKind.Identifier, 0, nameOffset);

            int initIdx = -1;
            if (context.expression() != null)
            {
                var result = Visit(context.expression());
                if (result is int idx) initIdx = idx;
            }

            // VARIABLE_DECLARATION: payload = typeKind, children = [name, init?]
            if (initIdx >= 0)
            {
                return _builder.Add((byte)CSharpAstNodeKind.VariableDeclaration, 0, (uint)typeKind, nameIdx, initIdx);
            }
            else
            {
                return _builder.Add((byte)CSharpAstNodeKind.VariableDeclaration, 0, (uint)typeKind, nameIdx);
            }
        }

        public override object VisitExpression(CSharpParser.ExpressionContext context)
        {
            // Expression -> Literal or Identifier
            if (context.literal() != null)
                return VisitLiteral(context.literal());
            if (context.identifier() != null)
                return VisitIdentifier(context.identifier());

            return base.VisitExpression(context);
        }

        public override object VisitLiteral(CSharpParser.LiteralContext context)
        {
            if (context.INT_LITERAL() != null)
            {
                string text = context.INT_LITERAL().GetText();
                if (long.TryParse(text, out long value))
                {
                    return _builder.Add((byte)CSharpAstNodeKind.LiteralInt, 0, (uint)value);
                }
            }
            else if (context.STRING_LITERAL() != null)
            {
                string text = context.STRING_LITERAL().GetText();
                // Remove quotes
                if (text.Length >= 2 && text.StartsWith('"') && text.EndsWith('"'))
                {
                    text = text.Substring(1, text.Length - 2);
                }
                uint stringOffset = _stringPool.Intern(text);
                return _builder.Add((byte)CSharpAstNodeKind.LiteralString, 0, stringOffset);
            }
            else if (context.BOOL_LITERAL() != null)
            {
                bool value = context.BOOL_LITERAL().GetText() == "true";
                return _builder.Add((byte)CSharpAstNodeKind.LiteralBool, 0, value ? 1u : 0u);
            }

            return 0;
        }

        public override object VisitIdentifier(CSharpParser.IdentifierContext context)
        {
            string name = context.GetText();
            uint nameId = _stringPool.Intern(name);
            return _builder.Add((byte)CSharpAstNodeKind.Identifier, 0, nameId);
        }
    }
}