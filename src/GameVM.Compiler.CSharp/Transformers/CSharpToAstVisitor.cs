using GameVM.Compiler.CSharp.Ast;
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
            byte typeKind = GetTypeKind(context.type().GetText());

            string varName = context.identifier().GetText();
            uint nameOffset = _stringPool.Intern(varName);
            int nameIdx = _builder.Add((byte)CSharpAstNodeKind.Identifier, 0, nameOffset);

            int initIdx = VisitInitializer(context);

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

        private static readonly System.Collections.Generic.Dictionary<string, byte> _typeKinds =
            new()
            {
                ["int"] = 1,
                ["int32"] = 1,
                ["int64"] = 1,
                ["string"] = 2,
                ["bool"] = 3,
            };

        private static byte GetTypeKind(string typeNameStr)
        {
            return _typeKinds.TryGetValue(typeNameStr, out byte kind) ? kind : (byte)4;
        }

        private int VisitInitializer(CSharpParser.VariableDeclarationContext context)
        {
            if (context.expression() == null)
                return -1;

            var result = Visit(context.expression());
            return result is int idx ? idx : -1;
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
                return VisitIntLiteral(context);
            if (context.STRING_LITERAL() != null)
                return VisitStringLiteral(context);
            if (context.BOOL_LITERAL() != null)
                return VisitBoolLiteral(context);

            return 0;
        }

        private object VisitIntLiteral(CSharpParser.LiteralContext context)
        {
            string text = context.INT_LITERAL().GetText();
            if (long.TryParse(text, out long value))
            {
                return _builder.Add((byte)CSharpAstNodeKind.LiteralInt, 0, (uint)value);
            }
            return 0;
        }

        private object VisitStringLiteral(CSharpParser.LiteralContext context)
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

        private object VisitBoolLiteral(CSharpParser.LiteralContext context)
        {
            bool value = context.BOOL_LITERAL().GetText() == "true";
            return _builder.Add((byte)CSharpAstNodeKind.LiteralBool, 0, value ? 1u : 0u);
        }

        public override object VisitIdentifier(CSharpParser.IdentifierContext context)
        {
            string name = context.GetText();
            uint nameId = _stringPool.Intern(name);
            return _builder.Add((byte)CSharpAstNodeKind.Identifier, 0, nameId);
        }
    }
}