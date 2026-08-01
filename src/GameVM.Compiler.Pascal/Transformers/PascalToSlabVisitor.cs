using System;
using System.Collections.Generic;
using Antlr4.Runtime;
using GameVM.Compiler.Core.IR.Slab;
using GameVM.Compiler.Core.IR.SlabProcessing;
using static GameVM.Compiler.Core.IR.Slab.InstructionMetadata;
using static GameVM.Compiler.Core.IR.Slab.InstructionMetadataFlags;
using GameVM.Compiler.Pascal.ANTLR;
using GameVM.Compiler.Core.IR.Buffers;

namespace GameVM.Compiler.Pascal.Transformers
{
    public class PascalToSlabVisitor : PascalBaseVisitor<object>
    {
        private readonly ArenaAllocator _arena;
        private readonly StringPool _stringPool;
        private uint _headerOffset;
        private readonly Dictionary<string, int> _constants = new Dictionary<string, int>();

        public PascalToSlabVisitor(ArenaAllocator arena, StringPool stringPool)
        {
            _arena = arena ?? throw new ArgumentNullException(nameof(arena));
            _stringPool = stringPool ?? throw new ArgumentNullException(nameof(stringPool));
        }

        public uint[] GetSlab()
        {
            return _arena.ToContiguousArray();
        }

        public StringPool GetStringPool()
        {
            return _stringPool;
        }

        public uint HeaderOffset => _headerOffset;

public override object VisitProgram(PascalParser.ProgramContext context)
        {
            // Allocate header space at offset 0
            _headerOffset = _arena.Allocate(SlabHeader.HeaderIndex.Length);
            
            // Create header with proper magic and write it (element count = 1 for the main method)
            var header = SlabHeader.ForStage(irStage: 1, elementCount: 1);
            uint[] headerData = new uint[SlabHeader.HeaderIndex.Length];
            header.WriteTo(headerData);
            _arena.Write(_headerOffset, headerData);

            // Reserve METHOD_DECLARATION slot (body offset filled in after body is emitted)
            uint methodDeclOffset = _arena.Allocate(3);
            
            // Build the main function body as a BLOCK containing variable declarations + compound statement
            var bodyItems = new List<uint>();
            var block = context.block();
            if (block != null)
            {
                // Process constant definition parts first (register constants)
                foreach (var cdp in block.constantDefinitionPart().Where(c => c != null))
                {
                    foreach (var cd in cdp.constantDefinition())
                    {
                        VisitConstantDefinition(cd);
                    }
                }

                // Process variable declaration parts
                foreach (var vdp in block.variableDeclarationPart().Where(v => v != null))
                {
                    foreach (var vd in vdp.variableDeclaration().Where(d => d != null))
                    {
                        var offsets = VisitVariableDeclaration(vd) as List<uint>;
                        if (offsets != null)
                            bodyItems.AddRange(offsets);
                    }
                }

                // Process compound statement (begin...end)
                if (block.compoundStatement() != null)
                {
                    var result = VisitCompoundStatement(block.compoundStatement());
                    if (result is uint compoundOffset)
                        bodyItems.Add(compoundOffset);
                }
            }
            
            // Emit the body as a BLOCK
            uint bodyBlockOffset = _arena.Allocate(1 + bodyItems.Count);
            _arena.Write(bodyBlockOffset, Encode(BLOCK, (byte)(1 + bodyItems.Count), (byte)bodyItems.Count));
            for (int i = 0; i < bodyItems.Count; i++)
            {
                _arena.Write(bodyBlockOffset + 1 + (uint)i, bodyItems[i]);
            }
            
            // Fill in METHOD_DECLARATION: [metadata, nameOffset, bodyOffset]
            _arena.Write(methodDeclOffset, Encode(METHOD_DECLARATION, 3, 2), _stringPool.Intern("main"), bodyBlockOffset);
            
            return GetSlab();
        }

        public override object VisitVariableDeclaration(PascalParser.VariableDeclarationContext context)
        {
            string typeName = "integer"; // default
            
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
            
            // Emit one VARIABLE_DECLARATION instruction per identifier in the list
            // VARIABLE_DECLARATION instruction: [metadata, typeKind, varNameOffset]
            var offsets = new List<uint>();
            if (context.identifierList() != null && context.identifierList().identifier() != null)
            {
                var identifiers = context.identifierList().identifier();
                for (int i = 0; i < identifiers.Length; i++)
                {
                    string varName = identifiers[i].GetText();
                    uint startOffset = _arena.Allocate(3);
                    offsets.Add(startOffset);
                    _arena.Write(startOffset, Encode(VARIABLE_DECLARATION, 3, 2), typeKind, _stringPool.Intern(varName));
                }
            }
            
            return offsets;
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
            // Visit the variable to get the slab offset of the IDENTIFIER node
            uint targetOffset = 0;
            if (context.variable() != null)
                targetOffset = (uint)(Visit(context.variable()) ?? 0u);

            // Visit the expression to get its slab offset
            uint valueOffset = 0;
            if (context.expression() != null)
                valueOffset = (uint)(Visit(context.expression()) ?? 0u);
            
            // ASSIGNMENT: [metadata, targetOffset, valueOffset]
            uint startOffset = _arena.Allocate(3);
            _arena.Write(startOffset, Encode(ASSIGNMENT, 3, 2), targetOffset, valueOffset);

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
            var statementsContext = context.statements();
            int statementCount = 0;
            var statementOffsets = new List<uint>();

            if (statementsContext != null)
            {
                var stmts = statementsContext.statement();
                statementCount = stmts.Length;

                for (int i = 0; i < statementCount; i++)
                {
                    var stmt = stmts[i];
                    if (stmt == null)
                    {
                        // Skip null statements
                        continue;
                    }
                    var result = Visit(stmt);
                    if (result == null)
                    {
                        // Skip null results
                        continue;
                    }
                    statementOffsets.Add((uint)result);
                }
            }

            // BLOCK: [metadata, statementOffset1, statementOffset2, ...]
            uint startOffset = _arena.Allocate(1 + statementOffsets.Count);
            _arena.Write(startOffset, Encode(BLOCK, (byte)(1 + statementOffsets.Count), (byte)statementOffsets.Count));

            // Write statement offsets
            for (int i = 0; i < statementOffsets.Count; i++)
            {
                _arena.Write((uint)(startOffset + 1 + i), statementOffsets[i]);
            }

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
            var left = VisitTerm(context.term());
            
            // Handle additive operators (binary operations)
            if (context.simpleExpression() != null && context.additiveoperator() != null)
            {
                var right = VisitSimpleExpression(context.simpleExpression());
                var opText = context.additiveoperator().GetText();
                uint opHash = opText[0]; // ASCII value of operator char
                
                // BINARY_OP: [metadata, leftOffset, rightOffset, opHash]
                uint startOffset = _arena.Allocate(4);
                uint leftOffset = left as uint? ?? 0;
                uint rightOffset = right as uint? ?? 0;
                _arena.Write(startOffset, Encode(BINARY_OP, 4, 3), leftOffset, rightOffset, opHash);
                return startOffset;
            }
            
            return left;
        }

        public override object VisitTerm(PascalParser.TermContext context)
        {
            // Handle factors and operators
            var left = VisitSignedFactor(context.signedFactor());
            
            // Handle multiplicative operators (binary operations)
            if (context.term() != null && context.multiplicativeoperator() != null)
            {
                var right = VisitTerm(context.term());
                var opText = context.multiplicativeoperator().GetText();
                uint opHash = opText[0]; // ASCII value of operator char
                
                // BINARY_OP: [metadata, leftOffset, rightOffset, opHash]
                uint startOffset = _arena.Allocate(4);
                uint leftOffset = left as uint? ?? 0;
                uint rightOffset = right as uint? ?? 0;
                _arena.Write(startOffset, Encode(BINARY_OP, 4, 3), leftOffset, rightOffset, opHash);
                return startOffset;
            }
            
            return left;
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
            // LITERAL_STRING: [metadata, stringOffset]
            uint startOffset = _arena.Allocate(2);
            
            string text = context.GetText();
            // Remove quotes
            if (text.Length >= 2 && text.StartsWith('\'') && text.EndsWith('\''))
            {
                text = text.Substring(1, text.Length - 2);
            }
            
            uint stringOffset = _stringPool.Intern(text);
            _arena.Write(startOffset, Encode(LITERAL_STRING, 2, 1), stringOffset);
            
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
            string varName = context.GetText();
            
            // Check if this is a known constant - if so, emit LITERAL_INT instead of IDENTIFIER
            if (_constants.TryGetValue(varName, out int constValue))
            {
                uint startOffset = _arena.Allocate(2);
                _arena.Write(startOffset, Encode(LITERAL_INT, 2, 1), (uint)constValue);
                return startOffset;
            }
            
            // IDENTIFIER: [metadata, nameOffset]
            uint idOffset = _arena.Allocate(2);
            uint nameId = _stringPool.Intern(varName);
            _arena.Write(idOffset, Encode(IDENTIFIER, 2, 1), nameId);
            return idOffset;
        }

        public override object VisitConstantDefinitionPart(PascalParser.ConstantDefinitionPartContext context)
        {
            foreach (var def in context.constantDefinition())
            {
                VisitConstantDefinition(def);
            }
            return 0u;
        }

        public override object VisitConstantDefinition(PascalParser.ConstantDefinitionContext context)
        {
            string name = context.identifier().GetText();
            string valueStr = context.constant().GetText();
            
            // Parse the constant value
            if (int.TryParse(valueStr, out int value))
            {
                _constants[name] = value;
            }
            else if (valueStr.StartsWith('\'') && valueStr.EndsWith('\''))
            {
                // String constant - treat as ASCII value of first char for now
                _constants[name] = valueStr.Length > 2 ? valueStr[1] : 0;
            }
            
            return 0u;
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