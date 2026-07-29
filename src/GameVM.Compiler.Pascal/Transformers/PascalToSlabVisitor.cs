using System;
using System.Collections.Generic;
using Antlr4.Runtime;
using GameVM.Compiler.Core.IR.Slab;
using GameVM.Compiler.Core.IR.SlabProcessing;
using static GameVM.Compiler.Core.IR.Slab.InstructionMetadata;
using static GameVM.Compiler.Core.IR.Slab.InstructionMetadataFlags;
using GameVM.Compiler.Pascal.ANTLR;

namespace GameVM.Compiler.Pascal.Transformers
{
    public class PascalToSlabVisitor : PascalBaseVisitor<object>
    {
        private readonly ArenaAllocator _arena;
        private uint _headerOffset;

        public PascalToSlabVisitor(ArenaAllocator arena)
        {
            _arena = arena ?? throw new ArgumentNullException(nameof(arena));
        }

        public uint[] GetSlab()
        {
            return _arena.ToContiguousArray();
        }

        public uint HeaderOffset => _headerOffset;

        public override object VisitProgram(PascalParser.ProgramContext context)
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

        public override object VisitVariableDeclaration(PascalParser.VariableDeclarationContext context)
        {
            // Allocate space for VARIABLE_DECLARATION instruction: metadata + 2 args = 3 uints
            uint startOffset = _arena.Allocate(3);
            
            string varName = "";
            string typeName = "integer"; // default

            // Get variable name
            if (context.identifierList() != null && context.identifierList().identifier() != null)
            {
                varName = context.identifierList().identifier(0).GetText(); // First variable
            }

            // Get type
            if (context.type_() != null)
            {
                // Simple type extraction - could be enhanced
                typeName = context.type_().GetText().ToLower();
            }

            // Map Pascal types to our type kinds
            byte typeKind = typeName switch
            {
                "integer" => 1,
                "string" => 2,
                "boolean" => 3,
                _ => 4 // unknown/other
            };

            // VARIABLE_DECLARATION instruction: [metadata, typeKind, varNameHash]
            _arena.Write(startOffset, Encode(VARIABLE_DECLARATION, 3, 2), typeKind, (uint)varName.GetHashCode());

            return startOffset;
        }

public override object VisitStatement(PascalParser.StatementContext context)
        {
            // Delegate to specific statement types
            if (context.unlabelledStatement() != null)
            {
                var unlabelled = context.unlabelledStatement();
                
                // Simple statements (assignment, procedure call, goto, empty)
                if (unlabelled.simpleStatement() != null && unlabelled.simpleStatement().assignmentStatement() != null)
                    return VisitAssignmentStatement(unlabelled.simpleStatement().assignmentStatement());
                
                // Structured statements (compound, if, while, for, repeat, with)
                if (unlabelled.structuredStatement() != null)
                {
                    var structured = unlabelled.structuredStatement();
                    
                    if (structured.compoundStatement() != null)
                        return VisitCompoundStatement(structured.compoundStatement());
                    
                    if (structured.conditionalStatement() != null)
                    {
                        var conditional = structured.conditionalStatement();
                        if (conditional.ifStatement() != null)
                            return VisitIfStatement(conditional.ifStatement());
                    }
                    
                    if (structured.repetetiveStatement() != null)
                    {
                        var repetitive = structured.repetetiveStatement();
                        if (repetitive.whileStatement() != null)
                            return VisitWhileStatement(repetitive.whileStatement());
                        if (repetitive.repeatStatement() != null)
                            return VisitRepeatStatement(repetitive.repeatStatement());
                        if (repetitive.forStatement() != null)
                            return VisitForStatement(repetitive.forStatement());
                    }
                }
            }
            
            return base.VisitStatement(context);
        }

        public override object VisitAssignmentStatement(PascalParser.AssignmentStatementContext context)
        {
            // ASSIGNMENT: [metadata, targetOffset, valueOffset]
            // Visit expression to get its offset (the value)
            var valueOffset = Visit(context.expression()) as uint?;
            
            // Get variable name and compute its hash for target location
            string varName = "";
            if (context.variable() != null)
            {
                varName = context.variable().GetText();
            }
            uint target = (uint)varName.GetHashCode(); // Hash of variable name as placeholder
            
            // Use default if visit failed (shouldn't happen in valid code)
            uint value = valueOffset ?? 0u;
            
            // ASSIGNMENT: [metadata, targetOffset, valueOffset]
            uint startOffset = _arena.Allocate(3);
            _arena.Write(startOffset, Encode(ASSIGNMENT, 3, 2), target, value);

            return startOffset;
        }

        public override object VisitIfStatement(PascalParser.IfStatementContext context)
        {
            // IF_STATEMENT: [metadata, conditionOffset, thenOffset, elseOffset?]
            // Visit children to get their offsets
            var conditionOffset = Visit(context.expression()) as uint?;
            var thenOffset = Visit(context.statement(0)) as uint?; // First statement is 'then'
            uint? elseOffset = null;
            if (context.statement().Length > 1)
            {
                elseOffset = Visit(context.statement(1)) as uint?; // Second statement is 'else'
            }

            uint condition = conditionOffset ?? 0u;
            uint then = thenOffset ?? 0u;
            uint elseBranch = elseOffset ?? 0u;
            byte argCount = elseOffset.HasValue ? (byte)3 : (byte)2;

            // Allocate space: metadata + args
            uint startOffset = _arena.Allocate(1 + argCount);
            uint metadata = Encode(IF_STATEMENT, (byte)(1 + argCount), argCount);
            _arena.Write(startOffset, metadata, condition, then, elseBranch);

            return startOffset;
        }

        public override object VisitWhileStatement(PascalParser.WhileStatementContext context)
        {
            // WHILE_STATEMENT: [metadata, conditionOffset, bodyOffset]
            // Visit children to get their offsets
            var conditionOffset = Visit(context.expression()) as uint?;
            var bodyOffset = Visit(context.statement()) as uint?;
            
            // Use defaults if visits failed (shouldn't happen in valid code)
            uint condition = conditionOffset ?? 0u;
            uint body = bodyOffset ?? 0u;

            // WHILE_STATEMENT: [metadata, conditionOffset, bodyOffset]
            uint startOffset = _arena.Allocate(3);
            _arena.Write(startOffset, Encode(WHILE_STATEMENT, 3, 2), condition, body);

            return startOffset;
        }

public override object VisitForStatement(PascalParser.ForStatementContext context)
        {
            // FOR_STATEMENT: [metadata, varNameOffset, initialValueOffset, finalValueOffset, direction, statementOffset]
            // For now, use placeholder values - to be implemented properly
            uint startOffset = _arena.Allocate(6);
            _arena.Write(startOffset, Encode(FOR_STATEMENT, 6, 5), 0, 0, 0, 0, 0);

            return startOffset;
        }

        public override object VisitRepeatStatement(PascalParser.RepeatStatementContext context)
        {
            // REPEAT_STATEMENT: [metadata, bodyOffset, conditionOffset]
            // For now, use placeholder values - to be implemented properly
            uint startOffset = _arena.Allocate(3);
            _arena.Write(startOffset, 0, 0, 0); // Will need proper opcode and implementation

            return startOffset;
        }

        public override object VisitCompoundStatement(PascalParser.CompoundStatementContext context)
        {
            // COMPOUND_STATEMENT: [metadata, statementOffset1, statementOffset2, ...]
            // For simplicity, we'll handle this as a block statement
            uint startOffset = _arena.Allocate(2); // Start with minimal args
            
            // Placeholder values
            _arena.Write(startOffset, Encode(BLOCK, 2, 1), 0);

            return startOffset;
        }

        public override object VisitExpression(PascalParser.ExpressionContext context)
        {
            // Delegate to specific expression types
            if (context.simpleExpression() != null)
                return VisitSimpleExpression(context.simpleExpression());
            return base.VisitExpression(context);
        }

        public override object VisitSimpleExpression(PascalParser.SimpleExpressionContext context)
        {
            // Handle terms and operators
            if (context.term() != null && context.additiveoperator() == null)
                return VisitTerm(context.term());
            
            // For binary operations, we'd need to handle left and right operands
            // This is simplified for now - just return the first term
            if (context.term() != null)
                return VisitTerm(context.term());
            
            return base.VisitSimpleExpression(context);
        }

        public override object VisitTerm(PascalParser.TermContext context)
        {
            // Handle factors and operators
            if (context.signedFactor() != null && context.multiplicativeoperator() == null)
                return VisitSignedFactor(context.signedFactor());
            
            // For binary operations, we'd need to handle left and right operands
            // This is simplified for now - just return the first signedFactor
            if (context.signedFactor() != null)
                return VisitSignedFactor(context.signedFactor());
            
            return base.VisitTerm(context);
        }

        public override object VisitSignedFactor(PascalParser.SignedFactorContext context)
        {
            // For now, delegate to factor
            return VisitFactor(context.factor());
        }

        public override object VisitFactor(PascalParser.FactorContext context)
        {
            if (context.unsignedConstant() != null)
                return VisitUnsignedConstant(context.unsignedConstant());
            if (context.variable() != null)
                return VisitVariable(context.variable());
            if (context.functionDesignator() != null)
                return VisitFunctionDesignator(context.functionDesignator());
            if (context.LPAREN() != null && context.expression() != null)
                return VisitExpression(context.expression()); // Parenthesized expression
            
            return base.VisitFactor(context);
        }

        public override object VisitUnsignedConstant(PascalParser.UnsignedConstantContext context)
        {
            if (context.unsignedNumber() != null)
                return VisitUnsignedNumber(context.unsignedNumber());
            if (context.@string() != null)
                return VisitString(context.@string());
            if (context.constantChr() != null)
                return VisitConstantChr(context.constantChr());
            if (context.NIL() != null)
            {
                // NIL literal - treat as integer 0
                uint startOffset = _arena.Allocate(2);
                _arena.Write(startOffset, Encode(LITERAL_INT, 2, 1), 0u);
                return startOffset;
            }
            
            return base.VisitUnsignedConstant(context);
        }

        public override object VisitUnsignedNumber(PascalParser.UnsignedNumberContext context)
        {
            if (context.unsignedInteger() != null)
                return VisitUnsignedInteger(context.unsignedInteger());
            if (context.unsignedReal() != null)
                return VisitUnsignedReal(context.unsignedReal());
            
            return base.VisitUnsignedNumber(context);
        }

        public override object VisitUnsignedInteger(PascalParser.UnsignedIntegerContext context)
        {
            // LITERAL_INTEGER: [metadata, value]
            uint startOffset = _arena.Allocate(2);
            
            if (int.TryParse(context.GetText(), out int value))
            {
                _arena.Write(startOffset, Encode(LITERAL_INT, 2, 1), (uint)value);
            }
            else
            {
                _arena.Write(startOffset, Encode(LITERAL_INT, 2, 1), 0u); // Default to 0 on parse error
            }
            
            return startOffset;
        }

        public override object VisitUnsignedReal(PascalParser.UnsignedRealContext context)
        {
            // For now, treat as integer (could be enhanced to handle floats)
            uint startOffset = _arena.Allocate(2);
            _arena.Write(startOffset, Encode(LITERAL_INT, 2, 1), 0u);
            return startOffset;
        }

        public override object VisitString(PascalParser.StringContext context)
        {
            // LITERAL_STRING: [metadata, stringHash]
            uint startOffset = _arena.Allocate(2);
            
            string text = context.GetText();
            // Remove quotes
            if (text.Length >= 2 && text.StartsWith('\'') && text.EndsWith('\''))
            {
                text = text.Substring(1, text.Length - 2);
            }
            
            uint stringId = (uint)text.GetHashCode();
            _arena.Write(startOffset, Encode(LITERAL_STRING, 2, 1), stringId);
            
            return startOffset;
        }

        public override object VisitBool_(PascalParser.Bool_Context context)
        {
            // LITERAL_BOOL: [metadata, value]
            uint startOffset = _arena.Allocate(2);
            
            bool value = context.GetText().ToLower() == "true";
            _arena.Write(startOffset, Encode(LITERAL_BOOL, 2, 1), value ? 1u : 0u);
            
            return startOffset;
        }

        public override object VisitConstantChr(PascalParser.ConstantChrContext context)
        {
            // CHR function call - treat as integer for now
            uint startOffset = _arena.Allocate(2);
            _arena.Write(startOffset, Encode(LITERAL_INT, 2, 1), 0u);
            return startOffset;
        }

        public override object VisitVariable(PascalParser.VariableContext context)
        {
            // IDENTIFIER: [metadata, nameHash]
            uint startOffset = _arena.Allocate(2);
            
            string varName = context.GetText();
            uint nameId = (uint)varName.GetHashCode();
            _arena.Write(startOffset, Encode(IDENTIFIER, 2, 1), nameId);
            
            return startOffset;
        }

        public override object VisitFunctionDesignator(PascalParser.FunctionDesignatorContext context)
        {
            // FUNCTION_DESIGNATOR: treat as identifier for now
            if (context.identifier() != null)
                return VisitIdentifier(context.identifier());
            
            return base.VisitFunctionDesignator(context);
        }
    }
}