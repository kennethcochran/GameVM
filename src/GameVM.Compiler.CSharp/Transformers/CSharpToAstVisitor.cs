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

        /// <summary>Maps a C# type name to its AST type-kind byte. Pure function for testability.</summary>
        internal static byte MapTypeNameToKind(string typeNameStr)
        {
            return typeNameStr switch
            {
                "int" => 1,
                "string" => 2,
                "bool" => 3,
                "int32" => 1,
                "int64" => 1,
                _ => 4
            };
        }

        public override object VisitVariableDeclaration(CSharpParser.VariableDeclarationContext context)
        {
            // VARIABLE_DECLARATION: payload = typeKind, child = [nameIdentifier, initExpression?]
            byte typeKind = MapTypeNameToKind(context.type().GetText());

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
                if (TryParseIntLiteral(context.INT_LITERAL().GetText(), out uint value))
                {
                    return _builder.Add((byte)CSharpAstNodeKind.LiteralInt, 0, value);
                }
            }
            else if (context.STRING_LITERAL() != null)
            {
                string text = UnquoteStringLiteral(context.STRING_LITERAL().GetText());
                uint stringOffset = _stringPool.Intern(text);
                return _builder.Add((byte)CSharpAstNodeKind.LiteralString, 0, stringOffset);
            }
            else if (context.BOOL_LITERAL() != null)
            {
                uint value = ParseBoolLiteral(context.BOOL_LITERAL().GetText());
                return _builder.Add((byte)CSharpAstNodeKind.LiteralBool, 0, value);
            }

            return 0;
        }

        /// <summary>Parses an integer literal to uint. Pure function for testability.</summary>
        internal static bool TryParseIntLiteral(string text, out uint value)
        {
            if (long.TryParse(text, out long longValue))
            {
                value = (uint)longValue;
                return true;
            }
            value = 0;
            return false;
        }

        /// <summary>Parses a bool literal to 1/0. Pure function for testability.</summary>
        internal static uint ParseBoolLiteral(string text)
        {
            return text == "true" ? 1u : 0u;
        }

        /// <summary>Removes surrounding double quotes from a string literal. Pure function for testability.</summary>
        internal static string UnquoteStringLiteral(string text)
        {
            if (text.Length >= 2 && text.StartsWith('"') && text.EndsWith('"'))
            {
                return text.Substring(1, text.Length - 2);
            }
            return text;
        }

        public override object VisitIdentifier(CSharpParser.IdentifierContext context)
        {
            string name = context.GetText();
            uint nameId = _stringPool.Intern(name);
            return _builder.Add((byte)CSharpAstNodeKind.Identifier, 0, nameId);
        }
    }
}