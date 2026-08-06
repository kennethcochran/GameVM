using GameVM.Compiler.Core.IR.Soa;
using GameVM.Compiler.Pascal.ANTLR;
using GameVM.Compiler.Core.IR.Buffers;
using GameVM.Compiler.Core.IR;

namespace GameVM.Compiler.Pascal.Transformers
{
    public class PascalToSlabVisitor : PascalBaseVisitor<object>
    {
        private readonly InstListBuilder _builder;
        private readonly StringPool _stringPool;
        private readonly Dictionary<string, int> _constants = new Dictionary<string, int>();

        public PascalToSlabVisitor(InstListBuilder builder, StringPool stringPool)
        {
            _builder = builder ?? throw new ArgumentNullException(nameof(builder));
            _stringPool = stringPool ?? throw new ArgumentNullException(nameof(stringPool));
        }

        public InstList GetSlab()
        {
            return _builder.Build();
        }

        public override object VisitProgram(PascalParser.ProgramContext context)
        {
            var bodyItems = new List<int>();
            var block = context.block();
            if (block != null)
            {
                foreach (var cdp in block.constantDefinitionPart().Where(c => c != null))
                {
                    foreach (var cd in cdp.constantDefinition())
                    {
                        VisitConstantDefinition(cd);
                    }
                }

                foreach (var vdp in block.variableDeclarationPart().Where(v => v != null))
                {
                    foreach (var vd in vdp.variableDeclaration().Where(d => d != null))
                    {
                        var result = VisitVariableDeclaration(vd);
                        if (result is int idx)
                            bodyItems.Add(idx);
                        else if (result is List<int> indices)
                            bodyItems.AddRange(indices);
                    }
                }

                if (block.compoundStatement() != null)
                {
                    var result = VisitCompoundStatement(block.compoundStatement());
                    if (result is int compoundIdx)
                        bodyItems.Add(compoundIdx);
                }
            }

            // Emit the body as a BLOCK (with variable declarations and statements as operands)
            uint[] bodyOperands = bodyItems.Select(i => (uint)i).ToArray();
            int bodyBlockIdx = _builder.Add((byte)AstNodeKind.Block, InstructionFlag.None, 0, new ReadOnlySpan<uint>(bodyOperands));

            // Emit METHOD_DECLARATION: [nameOffset, bodyIdx]
            // bodyIdx is an InstIndex handle stored as a uint operand
            uint nameOffset = _stringPool.Intern("main");
            int methodDeclIdx = _builder.Add((byte)AstNodeKind.MethodDeclaration, InstructionFlag.None, 0, nameOffset, (uint)bodyBlockIdx);

            return methodDeclIdx;
        }

        public override object VisitVariableDeclaration(PascalParser.VariableDeclarationContext context)
        {
            string typeName = "integer";
            if (context.type_() != null)
            {
                typeName = context.type_().GetText().ToLower();
            }

            byte typeKind = typeName switch
            {
                "integer" => 1,
                "string" => 2,
                "boolean" => 3,
                _ => 4
            };

            var indices = new List<int>();
            if (context.identifierList() != null && context.identifierList().identifier() != null)
            {
                var identifiers = context.identifierList().identifier();
                for (int i = 0; i < identifiers.Length; i++)
                {
                    string varName = identifiers[i].GetText();
                    uint nameOffset = _stringPool.Intern(varName);
                    // VARIABLE_DECLARATION: [typeKind, nameOffset]
                    int idx = _builder.Add((byte)AstNodeKind.VariableDeclaration, InstructionFlag.None, 0, typeKind, nameOffset);
                    indices.Add(idx);
                }
            }

            return indices;
        }

        public override object VisitStatement(PascalParser.StatementContext context)
        {
            if (context.unlabelledStatement() != null)
            {
                var unlabelled = context.unlabelledStatement();

                if (unlabelled.simpleStatement() != null && unlabelled.simpleStatement().assignmentStatement() != null)
                    return VisitAssignmentStatement(unlabelled.simpleStatement().assignmentStatement());

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
            // ASSIGNMENT: [targetIdx, valueIdx] - InstIndex handles as operands
            int targetIdx = 0;
            if (context.variable() != null)
            {
                var result = Visit(context.variable());
                if (result is int idx) targetIdx = idx;
            }

            int valueIdx = 0;
            if (context.expression() != null)
            {
                var result = Visit(context.expression());
                if (result is int idx) valueIdx = idx;
            }

            int startIdx = _builder.Add((byte)AstNodeKind.Assignment, InstructionFlag.None, 0, (uint)targetIdx, (uint)valueIdx);

            return startIdx;
        }

        public override object VisitIfStatement(PascalParser.IfStatementContext context)
        {
            // IF_STATEMENT: [conditionIdx, thenIdx, elseIdx?]
            int conditionIdx = 0;
            var conditionResult = Visit(context.expression());
            if (conditionResult is int cIdx) conditionIdx = cIdx;

            int thenIdx = 0;
            var thenResult = Visit(context.statement(0));
            if (thenResult is int tIdx) thenIdx = tIdx;

            int? elseIdx = null;
            if (context.statement().Length > 1)
            {
                var elseResult = Visit(context.statement(1));
                if (elseResult is int eIdx) elseIdx = eIdx;
            }

            int startIdx;
            if (elseIdx.HasValue)
            {
                startIdx = _builder.Add((byte)AstNodeKind.IfStatement, InstructionFlag.Terminator, 0, (uint)conditionIdx, (uint)thenIdx, (uint)elseIdx.Value);
            }
            else
            {
                startIdx = _builder.Add((byte)AstNodeKind.IfStatement, InstructionFlag.Terminator, 0, (uint)conditionIdx, (uint)thenIdx);
            }

            return startIdx;
        }

        public override object VisitWhileStatement(PascalParser.WhileStatementContext context)
        {
            // WHILE_STATEMENT: [conditionIdx, bodyIdx]
            int conditionIdx = 0;
            var conditionResult = Visit(context.expression());
            if (conditionResult is int cIdx) conditionIdx = cIdx;

            int bodyIdx = 0;
            var bodyResult = Visit(context.statement());
            if (bodyResult is int bIdx) bodyIdx = bIdx;

            int startIdx = _builder.Add((byte)AstNodeKind.WhileStatement, InstructionFlag.Terminator, 0, (uint)conditionIdx, (uint)bodyIdx);

            return startIdx;
        }

        public override object VisitForStatement(PascalParser.ForStatementContext context)
        {
            // FOR_STATEMENT: [varNameOffset, initialValueIdx, finalValueIdx, direction, statementIdx]
            string varName = context.identifier().GetText();
            uint varNameOffset = _stringPool.Intern(varName);

            var forList = context.forList();
            int initialValueIdx = Visit(forList.initialValue().expression()) is int ivi ? ivi : 0;
            int finalValueIdx = Visit(forList.finalValue().expression()) is int fvi ? fvi : 0;

            // Direction: 0 for TO, 1 for DOWNTO
            uint direction = forList.TO() != null ? 0u : 1u;

            int statementIdx = Visit(context.statement()) is int si ? si : 0;

            uint[] operands = new uint[] { varNameOffset, (uint)initialValueIdx, (uint)finalValueIdx, direction, (uint)statementIdx };
            int startIdx = _builder.Add((byte)AstNodeKind.ForStatement, InstructionFlag.Terminator, 0, new ReadOnlySpan<uint>(operands));
            return startIdx;
        }

        public override object VisitRepeatStatement(PascalParser.RepeatStatementContext context)
        {
            // Placeholder: REPEAT_STATEMENT not yet defined; use NOP with placeholder operands
            int startIdx = _builder.Add((byte)AstNodeKind.Nop, InstructionFlag.Terminator, 0);
            return startIdx;
        }

        public override object VisitCompoundStatement(PascalParser.CompoundStatementContext context)
        {
            var statementsContext = context.statements();
            var statementIndices = new List<int>();

            if (statementsContext != null)
            {
                var stmts = statementsContext.statement();

                for (int i = 0; i < stmts.Length; i++)
                {
                    var stmt = stmts[i];
                    if (stmt == null) continue;

                    var result = Visit(stmt);
                    if (result == null) continue;

                    if (result is int idx)
                        statementIndices.Add(idx);
                }
            }

            // BLOCK: [statementIdx1, statementIdx2, ...]
            uint[] operands = statementIndices.Select(i => (uint)i).ToArray();
            int startIdx = _builder.Add((byte)AstNodeKind.Block, InstructionFlag.None, 0, new ReadOnlySpan<uint>(operands));

            return startIdx;
        }

        public override object VisitExpression(PascalParser.ExpressionContext context)
        {
            if (context.simpleExpression() != null)
                return VisitSimpleExpression(context.simpleExpression());
            return base.VisitExpression(context);
        }

        public override object VisitSimpleExpression(PascalParser.SimpleExpressionContext context)
        {
            var left = VisitTerm(context.term());

            if (context.simpleExpression() != null && context.additiveoperator() != null)
            {
                var right = VisitSimpleExpression(context.simpleExpression());
                var opText = context.additiveoperator().GetText();
                uint opHash = opText[0];

                // BINARY_OP: [leftIdx, rightIdx, opHash]
                int leftIdx = left is int l ? l : 0;
                int rightIdx = right is int r ? r : 0;
                int startIdx = _builder.Add((byte)AstNodeKind.BinaryOp, InstructionFlag.None, 0, (uint)leftIdx, (uint)rightIdx, opHash);
                return startIdx;
            }

            return left;
        }

        public override object VisitTerm(PascalParser.TermContext context)
        {
            var left = VisitSignedFactor(context.signedFactor());

            if (context.term() != null && context.multiplicativeoperator() != null)
            {
                var right = VisitTerm(context.term());
                var opText = context.multiplicativeoperator().GetText();
                uint opHash = opText[0];

                // BINARY_OP: [leftIdx, rightIdx, opHash]
                int leftIdx = left is int l ? l : 0;
                int rightIdx = right is int r ? r : 0;
                int startIdx = _builder.Add((byte)AstNodeKind.BinaryOp, InstructionFlag.None, 0, (uint)leftIdx, (uint)rightIdx, opHash);
                return startIdx;
            }

            return left;
        }

        public override object VisitSignedFactor(PascalParser.SignedFactorContext context)
        {
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
                return VisitExpression(context.expression());

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
                int startIdx = _builder.Add((byte)AstNodeKind.LiteralInt, InstructionFlag.None, 0, 0u);
                return startIdx;
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
            // LITERAL_INT: [value]
            if (int.TryParse(context.GetText(), out int value))
            {
                int startIdx = _builder.Add((byte)AstNodeKind.LiteralInt, InstructionFlag.None, 0, (uint)value);
                return startIdx;
            }
            else
            {
                int startIdx = _builder.Add((byte)AstNodeKind.LiteralInt, InstructionFlag.None, 0, 0u);
                return startIdx;
            }
        }

        public override object VisitUnsignedReal(PascalParser.UnsignedRealContext context)
        {
            // For now, treat as integer (could be enhanced to handle floats)
            int startIdx = _builder.Add((byte)AstNodeKind.LiteralInt, InstructionFlag.None, 0, 0u);
            return startIdx;
        }

        public override object VisitString(PascalParser.StringContext context)
        {
            // LITERAL_STRING: [stringOffset]
            string text = context.GetText();
            if (text.Length >= 2 && text.StartsWith('\'') && text.EndsWith('\''))
            {
                text = text.Substring(1, text.Length - 2);
            }

            uint stringOffset = _stringPool.Intern(text);
            int startIdx = _builder.Add((byte)AstNodeKind.LiteralString, InstructionFlag.None, 0, stringOffset);
            return startIdx;
        }

        public override object VisitBool_(PascalParser.Bool_Context context)
        {
            // LITERAL_BOOL: [value]
            bool value = context.GetText().ToLower() == "true";
            int startIdx = _builder.Add((byte)AstNodeKind.LiteralBool, InstructionFlag.None, 0, value ? 1u : 0u);
            return startIdx;
        }

        public override object VisitConstantChr(PascalParser.ConstantChrContext context)
        {
            int startIdx = _builder.Add((byte)AstNodeKind.LiteralInt, InstructionFlag.None, 0, 0u);
            return startIdx;
        }

        public override object VisitVariable(PascalParser.VariableContext context)
        {
            string varName = context.GetText();

            if (_constants.TryGetValue(varName, out int constValue))
            {
                return _builder.Add((byte)AstNodeKind.LiteralInt, InstructionFlag.None, 0, (uint)constValue);
            }

            // IDENTIFIER: [nameOffset]
            uint nameId = _stringPool.Intern(varName);
            return _builder.Add((byte)AstNodeKind.Identifier, InstructionFlag.None, 0, nameId);
        }

        public override object VisitConstantDefinitionPart(PascalParser.ConstantDefinitionPartContext context)
        {
            foreach (var def in context.constantDefinition())
            {
                VisitConstantDefinition(def);
            }
            return 0;
        }

        public override object VisitConstantDefinition(PascalParser.ConstantDefinitionContext context)
        {
            string name = context.identifier().GetText();
            string valueStr = context.constant().GetText();

            if (int.TryParse(valueStr, out int value))
            {
                _constants[name] = value;
            }
            else if (valueStr.StartsWith('\'') && valueStr.EndsWith('\''))
            {
                _constants[name] = valueStr.Length > 2 ? valueStr[1] : 0;
            }

            return 0;
        }

        public override object VisitFunctionDesignator(PascalParser.FunctionDesignatorContext context)
        {
            if (context.identifier() != null)
                return VisitIdentifier(context.identifier());

            return base.VisitFunctionDesignator(context);
        }

        public override object VisitIdentifier(PascalParser.IdentifierContext context)
        {
            string name = context.GetText();
            uint nameId = _stringPool.Intern(name);
            int startIdx = _builder.Add((byte)AstNodeKind.Identifier, InstructionFlag.None, 0, nameId);
            return startIdx;
        }
    }
}