using System;
using System.Collections.Generic;
using System.Linq;
using GameVM.Compiler.Pascal.Ast;
using GameVM.Compiler.Core.IR.Soa;
using GameVM.Compiler.Pascal.ANTLR;
using GameVM.Compiler.Core.IR.Buffers;
using Antlr4.Runtime;
namespace GameVM.Compiler.Pascal.Transformers
{
    public class PascalToAstVisitor : PascalBaseVisitor<object>
    {
        private readonly AstBuilder _builder;
        private readonly StringPool _strings;
        private readonly List<(int Line, int Column)> _positions = new();

        /// <summary>
        /// Per-node source-position side table, parallel to the built AstTree's node
        /// indices (one line/column per node, added in node-allocation order). Consumed
        /// by the semantic-analysis pass so it can report where a violation occurs,
        /// without changing the fixed-size AstNode layout.
        /// </summary>
        public IReadOnlyList<(int Line, int Column)>? Positions => _positions.Count > 0 ? _positions : null;

        /// <summary>
        /// Adds a node to the tree and records a source position for it. The position is
        /// taken from the given ANTLR token's start location (line is 1-based, column is
        /// converted to 1-based). Must be called exactly once per node, in the same order
        /// as <see cref="AstBuilder.Add"/> so the positions list stays index-parallel.
        /// </summary>
        private int Add(byte kind, ushort flags, uint payload, IToken token, params int[] childIndices)
        {
            int idx = _builder.Add(kind, flags, payload, childIndices);
            _positions.Add((token?.Line ?? 0, (token?.Column ?? 0) + 1));
            return idx;
        }
        public PascalToAstVisitor()
        {
            _builder = new AstBuilder();
            _strings = new StringPool();
        }

        public PascalToAstVisitor(AstBuilder builder, StringPool strings)
        {
            _builder = builder ?? new AstBuilder();
            _strings = strings ?? new StringPool();
        }

        public PascalToAstVisitor(StringPool strings)
        {
            _builder = new AstBuilder();
            _strings = strings ?? new StringPool();
        }

        public StringPool StringPool => _strings;

        public StringPool? ExternalPool { get; set; }
        public AstTree GetTree() => _builder.Build();



        public AstTree BuildTree() => _builder.Build();

        public AstTree BuildTree(uint minCapacity) => _builder.Build(minCapacity);

        public override object VisitProgram(PascalParser.ProgramContext context)
        {
            var block = context.block();
            if (block != null)
            {
                var blockResult = VisitBlock(block);
                if (blockResult is int bi)
                {
                    uint mainOffset = _strings.Intern("main");
            return Add((byte)PascalAstNodeKind.Program, 0, mainOffset, context.Start, 0, bi);
                }
            }
            return Add((byte)PascalAstNodeKind.Program, 0, 0, context.Start, 0, 0);
        }

        public override object VisitBlock(PascalParser.BlockContext context)
        {
            var childIndices = new List<int>();

            foreach (var cdp in context.constantDefinitionPart().Where(c => c != null))
            {
                foreach (var cd in cdp.constantDefinition())
                {
                    var cstResult = Visit(cd);
                    if (cstResult is int ci)
                        childIndices.Add(ci);
                    else if (cstResult is List<int> cis)
                        childIndices.AddRange(cis);
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

            return Add((byte)PascalAstNodeKind.Block, 0, 0, context.Start, childIndices.ToArray());
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

                    int nameIdx = Add((byte)PascalAstNodeKind.Identifier, 0, nameOffset, id.Start);
                    int typeIdx = Add((byte)PascalAstNodeKind.TypeDefinition, 0, _strings.Intern(typeText), context.type_()?.Start ?? context.Start);
                    indices.Add(Add((byte)PascalAstNodeKind.VariableDeclaration, 0, 0, context.Start, nameIdx, typeIdx));
                }
            }

            return indices.Count == 1 ? indices[0] : indices;
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

            return Add((byte)PascalAstNodeKind.Nop, 0, 0, context.Start);
        }

        public override object VisitAssignmentStatement(PascalParser.AssignmentStatementContext context)
        {
            int targetIdx = Visit(context.variable()) is int t ? t : Add((byte)PascalAstNodeKind.Identifier, 0, 0, context.variable()?.Start ?? context.Start);
            int valueIdx = Visit(context.expression()) is int v ? v : Add((byte)PascalAstNodeKind.Nop, 0, 0, context.expression()?.Start ?? context.Start);

            return Add((byte)PascalAstNodeKind.Assignment, 0, 0, context.Start, targetIdx, valueIdx);
        }

        public override object VisitIfStatement(PascalParser.IfStatementContext context)
        {
            int conditionIdx = Visit(context.expression()) is int c ? c : Add((byte)PascalAstNodeKind.Nop, 0, 0, context.expression()?.Start ?? context.Start);
            int thenIdx = Visit(context.statement(0)) is int t ? t : Add((byte)PascalAstNodeKind.Nop, 0, 0, context.statement(0)?.Start ?? context.Start);

            if (context.statement().Length > 1)
            {
                int elseIdx = Visit(context.statement(1)) is int e ? e : Add((byte)PascalAstNodeKind.Nop, 0, 0, context.statement(1)?.Start ?? context.Start);
                return Add((byte)PascalAstNodeKind.IfStatement, 0, 0, context.Start, conditionIdx, thenIdx, elseIdx);
            }

            return Add((byte)PascalAstNodeKind.IfStatement, 0, 0, context.Start, conditionIdx, thenIdx);
        }

        public override object VisitWhileStatement(PascalParser.WhileStatementContext context)
        {
            int conditionIdx = Visit(context.expression()) is int c ? c : Add((byte)PascalAstNodeKind.Nop, 0, 0, context.expression()?.Start ?? context.Start);
            int bodyIdx = Visit(context.statement()) is int b ? b : Add((byte)PascalAstNodeKind.Nop, 0, 0, context.statement()?.Start ?? context.Start);

            return Add((byte)PascalAstNodeKind.WhileStatement, 0, 0, context.Start, conditionIdx, bodyIdx);
        }

        public override object VisitForStatement(PascalParser.ForStatementContext context)
        {
            string varName = context.identifier().GetText();
            uint varNameOffset = _strings.Intern(varName);

            var forList = context.forList();
            int initialValueIdx = Visit(forList.initialValue().expression()) is int ivi ? ivi : Add((byte)PascalAstNodeKind.Nop, 0, 0, forList.initialValue()?.Start ?? context.Start);
            int finalValueIdx = Visit(forList.finalValue().expression()) is int fvi ? fvi : Add((byte)PascalAstNodeKind.Nop, 0, 0, forList.finalValue()?.Start ?? context.Start);

            uint direction = forList.TO() != null ? 0u : 1u;
            int varNameIdx = Add((byte)PascalAstNodeKind.Identifier, 0, varNameOffset, context.identifier().Start);
            int statementIdx = Visit(context.statement()) is int si ? si : Add((byte)PascalAstNodeKind.Nop, 0, 0, context.statement()?.Start ?? context.Start);

            return Add((byte)PascalAstNodeKind.ForStatement, 0, direction, context.Start, varNameIdx, initialValueIdx, finalValueIdx, statementIdx);
        }

        public override object VisitRepeatStatement(PascalParser.RepeatStatementContext context)
        {
            return Add((byte)PascalAstNodeKind.Nop, 0, 0, context.Start);
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

            return Add((byte)PascalAstNodeKind.Block, 0, 0, context.Start, statementIndices.ToArray());
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
                    int leftIdx = left is int l ? l : Add((byte)PascalAstNodeKind.Nop, 0, 0, context.simpleExpression()?.Start ?? context.Start);
                    int rightIdx = right is int r ? r : Add((byte)PascalAstNodeKind.Nop, 0, 0, context.expression()?.Start ?? context.Start);
                    int opIdx = Add((byte)PascalAstNodeKind.BinaryOp, 0, (uint)op, context.relationaloperator()?.Start ?? context.Start);
                    return Add((byte)PascalAstNodeKind.BinaryOp, 0, 0, context.Start, leftIdx, rightIdx, opIdx);
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
                    _ => '+'
                };
                int leftIdx = left is int l ? l : Add((byte)PascalAstNodeKind.Nop, 0, 0, context.term()?.Start ?? context.Start);
                int rightIdx = right is int r ? r : Add((byte)PascalAstNodeKind.Nop, 0, 0, context.simpleExpression()?.Start ?? context.Start);
                return Add((byte)PascalAstNodeKind.BinaryOp, 0, (uint)op, context.additiveoperator()?.Start ?? context.Start, leftIdx, rightIdx);
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
                    "DIV" => '/',
                    "MOD" => '%',
                    "AND" => '&',
                    _ => '*'
                };
                int leftIdx = left is int l ? l : Add((byte)PascalAstNodeKind.Nop, 0, 0, context.signedFactor()?.Start ?? context.Start);
                int rightIdx = right is int r ? r : Add((byte)PascalAstNodeKind.Nop, 0, 0, context.term()?.Start ?? context.Start);
                return Add((byte)PascalAstNodeKind.BinaryOp, 0, (uint)op, context.multiplicativeoperator()?.Start ?? context.Start, leftIdx, rightIdx);
            }
            return left;
        }

        public override object VisitSignedFactor(PascalParser.SignedFactorContext context)
        {
            var factor = VisitFactor(context.factor());
            if (context.MINUS() != null)
            {
                int factorIdx = VisitFactor(context.factor()) is int o ? o : Add((byte)PascalAstNodeKind.Nop, 0, 0, context.factor()?.Start ?? context.Start);
                return Add((byte)PascalAstNodeKind.BinaryOp, 0, (uint)'-', context.factor()?.Start ?? context.Start, factorIdx);
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
                int nameIdx = Add((byte)PascalAstNodeKind.Identifier, 0, _strings.Intern(fd.identifier().GetText()), fd.identifier().Start);
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
                int argsIdx = Add((byte)PascalAstNodeKind.Block, 0, 0, fd.parameterList()?.Start ?? context.Start, argIndices.ToArray());
                return Add((byte)PascalAstNodeKind.MethodCall, 0, 0, fd.Start, nameIdx, argsIdx);
            }
            if (context.LPAREN() != null && context.expression() != null)
                return VisitExpression(context.expression());
            if (context.NOT() != null && context.factor() != null)
            {
                int operandIdx = VisitFactor(context.factor()) is int o ? o : Add((byte)PascalAstNodeKind.Nop, 0, 0, context.factor()?.Start ?? context.Start);
                return Add((byte)PascalAstNodeKind.BinaryOp, 0, (uint)'!', context.NOT().Symbol, operandIdx);
            }
            return Add((byte)PascalAstNodeKind.Nop, 0, 0, context.Start);
        }

        public override object VisitConstantDefinition(PascalParser.ConstantDefinitionContext context)
        {
            // constantDefinition: identifier EQUAL constant
            // Emit ConstantDefinition(name=Identifier, value=literal) so the HLIR
            // transformer can register the name and inline the value.
            string name = context.identifier().GetText();
            int nameIdx = Add((byte)PascalAstNodeKind.Identifier, 0, _strings.Intern(name), context.identifier().Start);
            int valueIdx = Visit(context.constant()) is int v ? v : Add((byte)PascalAstNodeKind.Nop, 0, 0, context.constant()?.Start ?? context.Start);
            return Add((byte)PascalAstNodeKind.ConstantDefinition, 0, 0, context.Start, nameIdx, valueIdx);
        }


        public override object VisitUnsignedInteger(PascalParser.UnsignedIntegerContext context)
        {
            uint offset = _strings.Intern(context.GetText());
            return Add((byte)PascalAstNodeKind.LiteralInt, 0, offset, context.Start);
        }

        public override object VisitUnsignedReal(PascalParser.UnsignedRealContext context)
        {
            uint offset = _strings.Intern(context.GetText());
            return Add((byte)PascalAstNodeKind.LiteralInt, 0, offset, context.Start);
        }

        public override object VisitString(PascalParser.StringContext context)
        {
            string text = context.GetText();
            if (text.Length >= 2 && text.StartsWith('\'') && text.EndsWith('\''))
                text = text.Substring(1, text.Length - 2);

            uint offset = _strings.Intern(text);
            return Add((byte)PascalAstNodeKind.LiteralString, 0, offset, context.Start);
        }

        public override object VisitBool_(PascalParser.Bool_Context context)
        {
            bool value = context.GetText().ToLower() == "true";
            uint offset = _strings.Intern(value ? "true" : "false");
            return Add((byte)PascalAstNodeKind.LiteralBool, 0, offset, context.Start);
        }

        public override object VisitConstantChr(PascalParser.ConstantChrContext context)
        {
            int val = 32;
            if (context.unsignedInteger() != null && int.TryParse(context.unsignedInteger().GetText(), out int parsed))
                val = parsed;

            char ch = (char)val;
            uint offset = _strings.Intern(ch.ToString());
            return Add((byte)PascalAstNodeKind.LiteralString, 0, offset, context.Start);
        }

        public override object VisitVariable(PascalParser.VariableContext context)
        {
            string varName = context.GetText().Split('.', '[', '(', ' ')[0].Trim();
            if (string.IsNullOrEmpty(varName))
                return Add((byte)PascalAstNodeKind.Nop, 0, 0, context.Start);

            uint nameOffset = _strings.Intern(varName);
            int nameIdx = Add((byte)PascalAstNodeKind.Identifier, 0, nameOffset, context.Start);

            if (context.expression() != null && context.expression().Length > 0)
            {
                var exprResult = Visit(context.expression()[0]);
                int exprIdx = exprResult is int ei ? ei : Add((byte)PascalAstNodeKind.Nop, 0, 0, context.expression()[0]?.Start ?? context.Start);
                return Add((byte)PascalAstNodeKind.BinaryOp, 0, (uint)'[', context.Start, nameIdx, exprIdx);
            }

            return nameIdx;
        }


    }
}