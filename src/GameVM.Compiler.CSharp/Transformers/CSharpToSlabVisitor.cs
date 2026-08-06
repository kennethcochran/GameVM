using GameVM.Compiler.Core.IR.Slab;
using GameVM.Compiler.Core.IR.Buffers;
using Antlr4.Runtime;
using GameVM.Compiler.Core.IR.Soa;
using GameVM.Compiler.CSharp.ANTLR;
using static GameVM.Compiler.Core.IR.Soa.InstConstants;

namespace GameVM.Compiler.CSharp.Transformers
{
    public class CSharpToSlabVisitor : CSharpBaseVisitor<object>
    {
        private readonly InstListBuilder _builder;
        private readonly StringPool _stringPool;
        // Inlined from former InstructionMetadataFlags (deleted)
        private const byte VARIABLE_DECLARATION = 8;
        private const byte LITERAL_INT = 1;
        private const byte LITERAL_STRING = 2;
        private const byte LITERAL_BOOL = 3;
        private const byte IDENTIFIER = 4;

        public CSharpToSlabVisitor(InstListBuilder builder, StringPool stringPool)
        {
            _builder = builder ?? throw new ArgumentNullException(nameof(builder));
            _stringPool = stringPool ?? throw new ArgumentNullException(nameof(stringPool));
        }

        public InstList GetSlab()
        {
            return _builder.Build();
        }

        public override object VisitVariableDeclaration(CSharpParser.VariableDeclarationContext context)
        {
            // VARIABLE_DECLARATION: [typeKind, nameOffset]
            string typeNameStr = context.type().GetText();
            byte typeKind = typeNameStr switch
            {
                "int" => 1,
                "string" => 2,
                "bool" => 3,
                _ => 4
            };

            string varName = context.identifier().GetText();
            uint nameOffset = _stringPool.Intern(varName);

            // Always allocate 3 args: typeKind, nameOffset, initValue (0 if no initializer)
            uint initValue = 0u;
            if (context.expression() != null)
            {
                var exprObj = VisitExpression(context.expression());
                if (exprObj is int exprInt && exprInt >= 0)
                {
                    initValue = (uint)exprInt;
                }
            }

            int index = _builder.Add(VARIABLE_DECLARATION, InstructionFlag.None, 3, 0,
                (uint)typeKind, nameOffset, initValue);

            return index;
        }

        public override object VisitExpression(CSharpParser.ExpressionContext context)
        {
            if (context.literal() != null)
            {
                return VisitLiteral(context.literal());
            }
            if (context.identifier() != null)
            {
                return VisitIdentifier(context.identifier());
            }
            return base.VisitExpression(context);
        }

        public override object VisitLiteral(CSharpParser.LiteralContext context)
        {
            // Allocate space for literal instruction: metadata + 1 value = 2 uints
            if (context.INT_LITERAL() != null)
            {
                int value = int.Parse(context.INT_LITERAL().GetText());
                int index = _builder.Add(LITERAL_INT, InstructionFlag.None, 1, 0, (uint)value);
                return index;
            }
            else if (context.STRING_LITERAL() != null)
            {
                string text = context.STRING_LITERAL().GetText();
                uint stringId = _stringPool.Intern(text);
                int index = _builder.Add(LITERAL_STRING, InstructionFlag.None, 1, 0, stringId);
                return index;
            }
            else if (context.BOOL_LITERAL() != null)
            {
                bool value = context.BOOL_LITERAL().GetText() == "true";
                int index = _builder.Add(LITERAL_BOOL, InstructionFlag.None, 1, 0, value ? 1u : 0u);
                return index;
            }
            return 0; // default
        }

        public override object VisitIdentifier(CSharpParser.IdentifierContext context)
        {
            // Allocate space for identifier instruction: metadata + 1 id = 2 uints
            string name = context.GetText();
            uint nameId = _stringPool.Intern(name);
            int index = _builder.Add(IDENTIFIER, InstructionFlag.None, 1, 0, nameId);
            return index;
        }
    }
}