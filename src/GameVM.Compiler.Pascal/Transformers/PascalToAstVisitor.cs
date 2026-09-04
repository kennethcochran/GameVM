using GameVM.Compiler.Core.IR.Ast;
using GameVM.Compiler.Core.IR.Soa;
using GameVM.Compiler.Pascal.ANTLR;
using GameVM.Compiler.Core.IR.Buffers;

namespace GameVM.Compiler.Pascal.Transformers
{
    /// <summary>
    /// ANTLR visitor that builds a Pascal AoS AST from the parse tree.
    /// Produces an <see cref="AstTree"/> via <see cref="AstBuilder"/> in post-order.
    /// </summary>
    public class PascalToAstVisitor : PascalBaseVisitor<object>
    {
        private readonly AstBuilder _builder;
        private readonly StringPool _strings;

        public PascalToAstVisitor()
        {
            _builder = new AstBuilder();
            _strings = new StringPool();
        }

        /// <summary>
        /// Constructor used by tests that supply an external builder.
        /// The supplied builder is used directly; its StringPool is used for interning.
        /// </summary>
        public PascalToAstVisitor(AstBuilder builder, StringPool strings)
        {
            _builder = builder ?? new AstBuilder();
            _strings = strings ?? new StringPool();
        }

        public PascalToAstVisitor(StringPool strings)
        {
            _builder = new AstBuilder();
            _strings = strings;
        }

        /// <summary>
        /// Alias for <see cref="BuildTree"/> for backwards compatibility with tests.
        /// </summary>
        public AstTree GetTree() => BuildTree();

        /// <summary>
        /// Gets the string pool containing all identifiers and string literals discovered during visiting.
        /// </summary>
        public StringPool Strings => _strings;

        public override object VisitProgram(PascalParser.ProgramContext context)
        {
            var block = context.block();
            if (block != null)
            {
                var blockResult = VisitBlock(block);
                if (blockResult is int bi)
                    return _builder.Add((byte)PascalAstNodeKind.Program, 0, 0, bi);
            }
            return _builder.Add((byte)PascalAstNodeKind.Program, 0, 0);
        }

        public override object VisitBlock(PascalParser.BlockContext context)
        {
            var childIndices = new List<int>();

            // Visit constant definitions (NOP in HLIR)
            foreach (var cdp in context.constantDefinitionPart().Where(c => c != null))
            {
                foreach (var cd in cdp.constantDefinition())
                {
                    Visit(cd);
                }
            }

            foreach (var vdp in context.variableDeclarationPart().Where(v => v != null))
            {
                foreach (var vd in vdp.variableDeclaration().Where(d => d != null))
                {
                    var vdResult = VisitVariableDeclaration(vd);
                    if (vdResult is int idx)
                        childIndices.Add(idx);
                    else if (vdResult is List<int> indices)
                        childIndices.AddRange(indices);
                }
            }

            if (context.compoundStatement() != null)
            {
                var csResult = VisitCompoundStatement(context.compoundStatement());
                if (csResult is int csi) childIndices.Add(csi);
            }

            return _builder.Add((byte)PascalAstNodeKind.Block, 0, 0, childIndices.ToArray());
        }

        public override object VisitVariableDeclaration(PascalParser.VariableDeclarationContext context)
        {
            var typeText = context.type_()?.GetText().ToLower() ?? "integer";

            var indices = new List<int>();
            if (context.identifierList()?.identifier() != null)
            {
                foreach (var id in context.identifierList().identifier())
                {
                    string varName = id.GetText();
                    uint nameOffset = _strings.Intern(varName);

                    int nameIdx = _builder.Add((byte)PascalAstNodeKind.Identifier, 0, nameOffset);
                    int typeIdx = _builder.Add((byte)PascalAstNodeKind.TypeDefinition, 0, _strings.Intern(typeText));

                    indices.Add(_builder.Add((byte)PascalAstNodeKind.VariableDeclaration, 0, 0, nameIdx, typeIdx));
                }
            }

            return indices;
        }

        public override object VisitStatement(PascalParser.StatementContext context)
        {
            if (context.unlabelledStatement() != null)
            {
                var unlabelled = context.unlabelledStatement();

                if (unlabelled.simpleStatement()?.assignmentStatement() != null)
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

            return _builder.Add((byte)PascalAstNodeKind.Nop, 0, 0);
        }

        public override object VisitAssignmentStatement(PascalParser.AssignmentStatementContext context)
        {
            int targetIdx = Visit(context.variable()) is int t ? t : _builder.Add((byte)PascalAstNodeKind.Identifier, 0, 0);
            int valueIdx = Visit(context.expression()) is int v ? v : _builder.Add((byte)PascalAstNodeKind.Nop, 0, 0);

            return _builder.Add((byte)PascalAstNodeKind.Assignment, 0, 0, targetIdx, valueIdx);
        }

        public override object VisitIfStatement(PascalParser.IfStatementContext context)
        {
            int conditionIdx = Visit(context.expression()) is int c ? c : _builder.Add((byte)PascalAstNodeKind.Nop, 0, 0);
            int thenIdx = Visit(context.statement(0)) is int t ? t : _builder.Add((byte)PascalAstNodeKind.Nop, 0, 0);

            if (context.statement().Length > 1)
            {
                int elseIdx = Visit(context.statement(1)) is int e ? e : _builder.Add((byte)PascalAstNodeKind.Nop, 0, 0);
                return _builder.Add((byte)PascalAstNodeKind.IfStatement, 0, 0, conditionIdx, thenIdx, elseIdx);
            }

            return _builder.Add((byte)PascalAstNodeKind.IfStatement, 0, 0, conditionIdx, thenIdx);
        }

        public override object VisitWhileStatement(PascalParser.WhileStatementContext context)
        {
            int conditionIdx = Visit(context.expression()) is int c ? c : _builder.Add((byte)PascalAstNodeKind.Nop, 0, 0);
            int bodyIdx = Visit(context.statement()) is int b ? b : _builder.Add((byte)PascalAstNodeKind.Nop, 0, 0);

            return _builder.Add((byte)PascalAstNodeKind.WhileStatement, 0, 0, conditionIdx, bodyIdx);
        }

        public override object VisitForStatement(PascalParser.ForStatementContext context)
        {
            string varName = context.identifier().GetText();
            uint varNameOffset = _strings.Intern(varName);

            var forList = context.forList();
            int initialValueIdx = Visit(forList.initialValue().expression()) is int ivi ? ivi : _builder.Add((byte)PascalAstNodeKind.Nop, 0, 0);
            int finalValueIdx = Visit(forList.finalValue().expression()) is int fvi ? fvi : _builder.Add((byte)PascalAstNodeKind.Nop, 0, 0);

            uint direction = forList.TO() != null ? 0u : 1u;
            int varNameIdx = _builder.Add((byte)PascalAstNodeKind.Identifier, 0, varNameOffset);
            int statementIdx = Visit(context.statement()) is int si ? si : _builder.Add((byte)PascalAstNodeKind.Nop, 0, 0);

            return _builder.Add((byte)PascalAstNodeKind.ForStatement, 0, direction, varNameIdx, initialValueIdx, finalValueIdx, statementIdx);
        }

        public override object VisitRepeatStatement(PascalParser.RepeatStatementContext context)
        {
            // REPEAT_STATEMENT: stays as NOP placeholder (semantic enrichment is a later feature)
            return _builder.Add((byte)PascalAstNodeKind.Nop, 0, 0);
        }

        public override object VisitCompoundStatement(PascalParser.CompoundStatementContext context)
        {
            var statementsContext = context.statements();
            var statementIndices = new List<int>();

            if (statementsContext != null)
            {
                foreach (var stmt in statementsContext.statement().Where(s => s != null))
                {
                    var stmtResult = Visit(stmt);
                    if (stmtResult is int si) statementIndices.Add(si);
                }
            }

            return _builder.Add((byte)PascalAstNodeKind.Block, 0, 0, statementIndices.ToArray());
        }

        public override object VisitExpression(PascalParser.ExpressionContext context)
        {
            if (context.simpleExpression() != null)
            {
                var left = VisitSimpleExpression(context.simpleExpression());
                if (context.expression() != null && context.relationaloperator() != null)
                {
                    char op = context.relationaloperator().GetText() switch
                    {
                        "=" => '=',
                        "<>" => '!',
                        "<" => '<',
                        ">" => '>',
                        "<=" => 'L',
                        ">=" => 'G',
                        _ => '?'
                    };
                    var right = VisitExpression(context.expression());
                    int leftIdx = left is int l ? l : _builder.Add((byte)PascalAstNodeKind.Nop, 0, 0);
                    int rightIdx = right is int r ? r : _builder.Add((byte)PascalAstNodeKind.Nop, 0, 0);
                    int opIdx = _builder.Add((byte)PascalAstNodeKind.BinaryOp, 0, (uint)op);
                    return _builder.Add((byte)PascalAstNodeKind.BinaryOp, 0, 0, leftIdx, rightIdx, opIdx);
                }
                return left;
            }
            return _builder.Add((byte)PascalAstNodeKind.Nop, 0, 0);
        }

        public override object VisitSimpleExpression(PascalParser.SimpleExpressionContext context)
        {
            var left = VisitTerm(context.term());

            if (context.simpleExpression() != null && context.additiveoperator() != null)
            {
                var right = VisitSimpleExpression(context.simpleExpression());
                char op = context.additiveoperator().GetText().ToUpperInvariant() switch
                {
                    "+" => '+',
                    "-" => '-',
                    "OR" => '|',
                    _ => '?'
                };
                int leftIdx = left is int l ? l : _builder.Add((byte)PascalAstNodeKind.Nop, 0, 0);
                int rightIdx = right is int r ? r : _builder.Add((byte)PascalAstNodeKind.Nop, 0, 0);
                int opIdx = _builder.Add((byte)PascalAstNodeKind.BinaryOp, 0, (uint)op);
                return _builder.Add((byte)PascalAstNodeKind.BinaryOp, 0, 0, leftIdx, rightIdx, opIdx);
            }

            return left;
        }

        public override object VisitTerm(PascalParser.TermContext context)
        {
            var left = VisitSignedFactor(context.signedFactor());

            if (context.term() != null && context.multiplicativeoperator() != null)
            {
                var right = VisitTerm(context.term());
                char op = context.multiplicativeoperator().GetText().ToUpperInvariant() switch
                {
                    "*" => '*',
                    "/" => '/',
                    "DIV" => 'D',
                    "MOD" => 'M',
                    "AND" => '&',
                    _ => '?'
                };
                int leftIdx = left is int l ? l : _builder.Add((byte)PascalAstNodeKind.Nop, 0, 0);
                int rightIdx = right is int r ? r : _builder.Add((byte)PascalAstNodeKind.Nop, 0, 0);
                int opIdx = _builder.Add((byte)PascalAstNodeKind.BinaryOp, 0, (uint)op);
                return _builder.Add((byte)PascalAstNodeKind.BinaryOp, 0, 0, leftIdx, rightIdx, opIdx);
            }

            return left;
        }

        public override object VisitSignedFactor(PascalParser.SignedFactorContext context)
        {
            var factor = VisitFactor(context.factor());
            if (context.MINUS() != null)
            {
                int factorIdx = factor is int f ? f : _builder.Add((byte)PascalAstNodeKind.Nop, 0, 0);
                return _builder.Add((byte)PascalAstNodeKind.BinaryOp, 0, (uint)'-', factorIdx);
            }
            return factor;
        }

        public override object VisitFactor(PascalParser.FactorContext context)
        {
            if (context.unsignedConstant() != null)
                return VisitUnsignedConstant(context.unsignedConstant());
            if (context.variable() != null)
                return VisitVariable(context.variable());
            if (context.functionDesignator() != null)
            {
                var fd = context.functionDesignator();
                int nameIdx = _builder.Add((byte)PascalAstNodeKind.Identifier, 0, _strings.Intern(fd.identifier().GetText()));
                var paramList = fd.parameterList();
                var argIndices = new List<int>();
                if (paramList != null)
                {
                    foreach (var ap in paramList.actualParameter())
                    {
                        var exprResult = Visit(ap.expression());
                        if (exprResult is int ei) argIndices.Add(ei);
                    }
                }
                int argsIdx = _builder.Add((byte)PascalAstNodeKind.Block, 0, 0, argIndices.ToArray());
                return _builder.Add((byte)PascalAstNodeKind.MethodCall, 0, 0, nameIdx, argsIdx);
            }
            if (context.LPAREN() != null && context.expression() != null)
                return VisitExpression(context.expression());
            if (context.NOT() != null && context.factor() != null)
            {
                int operandIdx = VisitFactor(context.factor()) is int o ? o : _builder.Add((byte)PascalAstNodeKind.Nop, 0, 0);
                return _builder.Add((byte)PascalAstNodeKind.BinaryOp, 0, (uint)'!', operandIdx);
            }

            return _builder.Add((byte)PascalAstNodeKind.Nop, 0, 0);
        }

        public override object VisitUnsignedConstant(PascalParser.UnsignedConstantContext context)
        {
            if (context.unsignedNumber() != null)
                return VisitUnsignedNumber(context.unsignedNumber());
            if (context.@string() != null)
                return VisitString(context.@string());
            if (context.constantChr() != null)
            {
                int val = VisitConstantChr(context.constantChr()) is int c ? c : 32;
                return _builder.Add((byte)PascalAstNodeKind.LiteralInt, 0, _strings.Intern(val.ToString()));
            }
            if (context.NIL() != null)
                return _builder.Add((byte)PascalAstNodeKind.LiteralInt, 0, _strings.Intern("0"));
            return _builder.Add((byte)PascalAstNodeKind.Nop, 0, 0);
        }

        public override object VisitUnsignedNumber(PascalParser.UnsignedNumberContext context)
        {
            if (context.unsignedInteger() != null)
                return VisitUnsignedInteger(context.unsignedInteger());
            if (context.unsignedReal() != null)
                return VisitUnsignedReal(context.unsignedReal());

            return _builder.Add((byte)PascalAstNodeKind.Nop, 0, 0);
        }

        public override object VisitUnsignedInteger(PascalParser.UnsignedIntegerContext context)
        {
            uint offset = _strings.Intern(context.GetText());
            return _builder.Add((byte)PascalAstNodeKind.LiteralInt, 0, offset);
        }
        public override object VisitUnsignedReal(PascalParser.UnsignedRealContext context)
        {
            uint offset = _strings.Intern(context.GetText());
            return _builder.Add((byte)PascalAstNodeKind.LiteralInt, 0, offset);
        }

        public override object VisitString(PascalParser.StringContext context)
        {
            string text = context.GetText();
            if (text.Length >= 2 && text.StartsWith('\'') && text.EndsWith('\''))
                text = text.Substring(1, text.Length - 2);

            uint offset = _strings.Intern(text);
            return _builder.Add((byte)PascalAstNodeKind.LiteralString, 0, offset);
        }

        public override object VisitBool_(PascalParser.Bool_Context context)
        {
            bool value = context.GetText().ToLower() == "true";
            uint offset = _strings.Intern(value ? "true" : "false");
            return _builder.Add((byte)PascalAstNodeKind.LiteralBool, 0, offset);
        }

        public override object VisitConstantChr(PascalParser.ConstantChrContext context)
        {
            int val = 32;
            if (context.unsignedInteger() != null && int.TryParse(context.unsignedInteger().GetText(), out int parsed))
                val = parsed;
            return _builder.Add((byte)PascalAstNodeKind.LiteralInt, 0, _strings.Intern(val.ToString()));
        }

        public override object VisitVariable(PascalParser.VariableContext context)
        {
            string varName = context.GetText().Split('.', '[', '(', ' ')[0].Trim();
            if (string.IsNullOrEmpty(varName))
                return _builder.Add((byte)PascalAstNodeKind.Nop, 0, 0);

            // Use first identifier as the variable name
            var firstIdent = context.identifier(0);
            string identName = firstIdent?.GetText() ?? varName;
            uint offset = _strings.Intern(identName);
            int nameIdx = _builder.Add((byte)PascalAstNodeKind.Identifier, 0, offset);

            // Check for array subscript or record field (. or [)
            if (context.DOT().Length > 0)
            {
                // Field access: var.field — emit as binary Nop chain
                var fieldIdent = context.identifier(1);
                if (fieldIdent != null)
                {
                    uint fieldOffset = _strings.Intern(fieldIdent.GetText());
                    int fieldIdx = _builder.Add((byte)PascalAstNodeKind.Identifier, 0, fieldOffset);
                    return _builder.Add((byte)PascalAstNodeKind.BinaryOp, 0, (uint)'.', nameIdx, fieldIdx);
                }
            }

            if (context.LBRACK().Length > 0 || context.LBRACK2().Length > 0)
            {
                // Array access: var[expr]
                var exprs = context.expression();
                if (exprs.Length > 0)
                {
                    int exprIdx = Visit(exprs[0]) is int ei ? ei : _builder.Add((byte)PascalAstNodeKind.Nop, 0, 0);
                    return _builder.Add((byte)PascalAstNodeKind.BinaryOp, 0, (uint)'[', nameIdx, exprIdx);
                }
            }

            return nameIdx;
        }

        /// <summary>Gets the built AstTree. Must be called after visiting the parse tree root.</summary>
        public AstTree BuildTree() => _builder.Build();
    }
}
