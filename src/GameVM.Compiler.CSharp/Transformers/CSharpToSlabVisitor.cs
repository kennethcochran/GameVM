using System;
using System.Collections.Generic;
using Antlr4.Runtime;
using GameVM.Compiler.Core.IR.Slab;
using GameVM.Compiler.Core.IR.SlabProcessing;
using static GameVM.Compiler.Core.IR.Slab.InstructionMetadata;
using static GameVM.Compiler.Core.IR.Slab.InstructionMetadataFlags;
using GameVM.Compiler.CSharp.ANTLR;

namespace GameVM.Compiler.CSharp.Transformers
{
    public class CSharpToSlabVisitor : CSharpBaseVisitor<object>
    {
        private readonly ArenaAllocator _arena;
        private uint _headerOffset;

        public CSharpToSlabVisitor(ArenaAllocator arena)
        {
            _arena = arena ?? throw new ArgumentNullException(nameof(arena));
        }

        public uint[] GetSlab()
        {
            return _arena.ToContiguousArray();
        }

        public uint HeaderOffset => _headerOffset;

        public override object VisitProgram(CSharpParser.ProgramContext context)
        {
            // Allocate header space at offset 0
            _headerOffset = _arena.Allocate(SlabHeader.HeaderIndex.Length);
            
            // Create header with proper magic and write it
            var header = SlabHeader.ForStage(irStage: 1, elementCount: 0);
            uint[] headerData = new uint[SlabHeader.HeaderIndex.Length];
            header.WriteTo(headerData);
            _arena.Write(_headerOffset, headerData);
            
            return base.VisitProgram(context);
        }

        public override object VisitVariableDeclaration(CSharpParser.VariableDeclarationContext context)
        {
            // Allocate space for VARIABLE_DECLARATION instruction: metadata + 2 args = 3 uints
            uint startOffset = _arena.Allocate(3);
            
            string typeNameStr = context.type().GetText();
            byte typeKind = typeNameStr switch
            {
                "int" => 1,
                "string" => 2,
                "bool" => 3,
                _ => 4
            };

            string varName = context.identifier().GetText();

            // VARIABLE_DECLARATION instruction: [metadata, typeKind, varNameHash]
            _arena.Write(startOffset, Encode(VARIABLE_DECLARATION, 3, 2), typeKind, (uint)varName.GetHashCode());

            if (context.expression() != null)
            {
                uint exprOffset = (uint)VisitExpression(context.expression());
                // Store the expression result as the third argument (initializer value)
                _arena.Write(startOffset + 2, exprOffset);
            }

            return startOffset;
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
            uint startOffset = _arena.Allocate(2);
            if (context.INT_LITERAL() != null)
            {
                int value = int.Parse(context.INT_LITERAL().GetText());
                _arena.Write(startOffset, Encode(LITERAL_INT, 2, 1), (uint)value);
            }
            else if (context.STRING_LITERAL() != null)
            {
                string text = context.STRING_LITERAL().GetText();
                uint stringId = (uint)text.GetHashCode();
                _arena.Write(startOffset, Encode(LITERAL_STRING, 2, 1), stringId);
            }
            else if (context.BOOL_LITERAL() != null)
            {
                bool value = context.BOOL_LITERAL().GetText() == "true";
                _arena.Write(startOffset, Encode(LITERAL_BOOL, 2, 1), value ? 1u : 0u);
            }
            return startOffset;
        }

        public override object VisitIdentifier(CSharpParser.IdentifierContext context)
        {
            // Allocate space for identifier instruction: metadata + 1 id = 2 uints
            uint startOffset = _arena.Allocate(2);
            string name = context.GetText();
            uint nameId = (uint)name.GetHashCode();
            _arena.Write(startOffset, Encode(IDENTIFIER, 2, 1), nameId);
            return startOffset;
        }
    }
}