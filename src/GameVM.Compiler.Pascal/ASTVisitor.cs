using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using GameVM.Compiler.Pascal.ANTLR;
using System;
using System.Collections.Generic;
using System.Linq;
using GameVM.Compiler.Pascal;

namespace GameVM.Compiler.Pascal
{
    public class AstVisitor : PascalBaseVisitor<PascalAstNode>
    {
        private readonly ExpressionVisitor _expressionVisitor;
        private readonly StatementVisitor _statementVisitor;
        private readonly DeclarationVisitor _declarationVisitor;

        public AstVisitor()
        {
            var astBuilder = new AstBuilder();
            _expressionVisitor = new ExpressionVisitor(astBuilder);
            _statementVisitor = new StatementVisitor(_expressionVisitor, astBuilder);
            _declarationVisitor = new DeclarationVisitor(_expressionVisitor, astBuilder);
            _declarationVisitor.SetMainVisitor(this);
        }
        public override PascalAstNode VisitProgram(PascalParser.ProgramContext context)
        {
            if (context == null)
                return new ErrorNode("Null program context");

            var programHeading = context.programHeading();
            if (programHeading == null)
                return new ErrorNode("Missing program heading");

            var identifier = programHeading.identifier();
            if (identifier == null)
                return new ErrorNode("Program must have a name");

            var blockContext = context.block();
            if (blockContext == null)
                return new ErrorNode("Missing program block");

            var blockNode = VisitBlock(blockContext);
            if (blockNode == null || blockNode is ErrorNode)
                return blockNode ?? new ErrorNode("Block visit returned null");

            return new ProgramNode
            {
                Name = identifier.GetText(),
                Block = (BlockNode)blockNode,
                UsesUnits = new List<PascalAstNode>()
            };
        }

        public override PascalAstNode VisitBlock(PascalParser.BlockContext context)
        {
            if (context == null)
                return new ErrorNode("Null block context");

            var block = new BlockNode
            {
                Statements = new List<PascalAstNode>()
            };


            var constantDefinitionPart = context.constantDefinitionPart();
            if (constantDefinitionPart != null)
            {
                foreach (var constDefPart in constantDefinitionPart)
                {
                    var node = _declarationVisitor.Visit(constDefPart);
                    if (node is BlockNode blockNode)
                    {
                        foreach (var stmt in blockNode.Statements)
                        {
                            block.Statements.Add(stmt);
                        }
                    }
                    else if (node != null)
                    {
                        block.Statements.Add(node);
                    }
                }
            }

            var typeDefinitionPart = context.typeDefinitionPart();
            if (typeDefinitionPart != null)
            {
                foreach (var typeDef in typeDefinitionPart)
                {
                    var node = _declarationVisitor.Visit(typeDef);
                    if (node is BlockNode blockNode)
                    {
                        foreach (var stmt in blockNode.Statements)
                        {
                            block.Statements.Add(stmt);
                        }
                    }
                    else if (node != null)
                    {
                        block.Statements.Add(node);
                    }
                }
            }

            var procedureAndFunctionPart = context.procedureAndFunctionDeclarationPart();
            if (procedureAndFunctionPart != null)
            {
                foreach (var procDecl in procedureAndFunctionPart)
                {
                    var node = _declarationVisitor.Visit(procDecl);
                    if (node != null)
                    {
                        block.Statements.Add(node);
                    }
                }
            }

            var variableDeclarationPart = context.variableDeclarationPart();
            if (variableDeclarationPart != null)
            {
                foreach (var varDeclPart in variableDeclarationPart)
                {
                    var varDecls = varDeclPart?.variableDeclaration();
                    if (varDecls != null)
                    {
                        foreach (var varDecl in varDecls)
                    {
                        var node = _declarationVisitor.Visit(varDecl);
                        if (node is BlockNode b)
                        {
                            foreach (var s in b.Statements)
                            {
                                block.Statements.Add(s);
                            }
                        }
                        else if (node != null)
                        {
                            block.Statements.Add(node);
                            }
                        }
                    }
                }
            }

            if (context.compoundStatement() != null)
            {
                var compoundNode = Visit(context.compoundStatement());
                if (compoundNode != null)
                {
                    block.Statements.Add(compoundNode);
                }
            }

            return block;
        }

        public override PascalAstNode VisitCompoundStatement(PascalParser.CompoundStatementContext context)
        {
            return _statementVisitor.VisitCompoundStatement(context);
        }

        public override PascalAstNode VisitStatement(PascalParser.StatementContext context)
        {
            return _statementVisitor.VisitStatement(context);
        }

        public override PascalAstNode VisitAssignmentStatement(PascalParser.AssignmentStatementContext context)
        {
            return _statementVisitor.VisitAssignmentStatement(context);
        }

        public override PascalAstNode VisitVariable(PascalParser.VariableContext context)
        {
            return _expressionVisitor.VisitVariable(context);
        }

        public override PascalAstNode VisitExpression(PascalParser.ExpressionContext context)
        {
            return _expressionVisitor.VisitExpression(context);
        }

        public override PascalAstNode VisitSimpleExpression(PascalParser.SimpleExpressionContext context)
        {
            return _expressionVisitor.VisitSimpleExpression(context);
        }

        public override PascalAstNode VisitTerm(PascalParser.TermContext context)
        {
            return _expressionVisitor.VisitTerm(context);
        }

        public override PascalAstNode VisitSignedFactor(PascalParser.SignedFactorContext context)
        {
            return _expressionVisitor.VisitSignedFactor(context);
        }

        public override PascalAstNode VisitFactor(PascalParser.FactorContext context)
        {
            return _expressionVisitor.VisitFactor(context);
        }

        public override PascalAstNode VisitProcedureDeclaration(PascalParser.ProcedureDeclarationContext context)
        {
            return _declarationVisitor.VisitProcedureDeclaration(context);
        }

        public override PascalAstNode VisitFunctionDeclaration(PascalParser.FunctionDeclarationContext context)
        {
            return _declarationVisitor.VisitFunctionDeclaration(context);
        }

        public override PascalAstNode VisitForStatement(PascalParser.ForStatementContext context)
        {
            return _statementVisitor.VisitForStatement(context);
        }

        public override PascalAstNode VisitArrayType(PascalParser.ArrayTypeContext context)
        {
            return _declarationVisitor.VisitArrayType(context);
        }

        public override PascalAstNode VisitSetType(PascalParser.SetTypeContext context)
        {
            return _declarationVisitor.VisitSetType(context);
        }

        public override PascalAstNode VisitIfStatement(PascalParser.IfStatementContext context)
        {
            return _statementVisitor.VisitIfStatement(context);
        }

        public override PascalAstNode VisitProcedureStatement(PascalParser.ProcedureStatementContext context)
        {
            return _statementVisitor.VisitProcedureStatement(context);
        }

        public override PascalAstNode VisitSimpleType(PascalParser.SimpleTypeContext context)
        {
            return _declarationVisitor.VisitSimpleType(context);
        }

        public override PascalAstNode VisitSubrangeType(PascalParser.SubrangeTypeContext context)
        {
            return _declarationVisitor.VisitSubrangeType(context);
        }

        public override PascalAstNode VisitInitialValue(PascalParser.InitialValueContext context)
        {
            return _statementVisitor.VisitInitialValue(context);
        }

        public override PascalAstNode VisitFinalValue(PascalParser.FinalValueContext context)
        {
            return _statementVisitor.VisitFinalValue(context);
        }

        public override PascalAstNode VisitIndexType(PascalParser.IndexTypeContext context)
        {
            return _declarationVisitor.VisitIndexType(context);
        }

        public override PascalAstNode VisitProcedureAndFunctionDeclarationPart(PascalParser.ProcedureAndFunctionDeclarationPartContext context)
        {
            return _declarationVisitor.VisitProcedureAndFunctionDeclarationPart(context) ?? new ErrorNode("Invalid procedure and function declaration part");
        }

        public override PascalAstNode VisitIdentifier(PascalParser.IdentifierContext context)
        {
            return _statementVisitor.VisitIdentifier(context);
        }

        public override PascalAstNode VisitCaseStatement(PascalParser.CaseStatementContext context)
        {
            return _statementVisitor.VisitCaseStatement(context);
        }

        public override PascalAstNode VisitCaseListElement(PascalParser.CaseListElementContext context)
        {
            return _statementVisitor.VisitCaseListElement(context);
        }

        public override PascalAstNode VisitWhileStatement(PascalParser.WhileStatementContext context)
        {
            return _statementVisitor.VisitWhileStatement(context);
        }

        public override PascalAstNode VisitTypeDefinitionPart(PascalParser.TypeDefinitionPartContext context)
        {
            return _declarationVisitor.VisitTypeDefinitionPart(context);
        }

        public override PascalAstNode VisitTypeDefinition(PascalParser.TypeDefinitionContext context)
        {
            return _declarationVisitor.VisitTypeDefinition(context);
        }

        public override PascalAstNode VisitType_(PascalParser.Type_Context context)
        {
            return _declarationVisitor.VisitType_(context);
        }

        public override PascalAstNode VisitConstant(PascalParser.ConstantContext context)
        {
            return _expressionVisitor.VisitConstant(context);
        }

        public override PascalAstNode VisitPointerType(PascalParser.PointerTypeContext context)
        {
            return _declarationVisitor.VisitPointerType(context);
        }

        public override PascalAstNode VisitLabelDeclarationPart(PascalParser.LabelDeclarationPartContext context)
        {
            return _declarationVisitor.VisitLabelDeclarationPart(context);
        }

        public override PascalAstNode VisitGotoStatement(PascalParser.GotoStatementContext context)
        {
            return _statementVisitor.VisitGotoStatement(context);
        }

        public override PascalAstNode VisitScalarType(PascalParser.ScalarTypeContext context)
        {
            return _declarationVisitor.VisitScalarType(context);
        }

        public override PascalAstNode VisitStructuredType(PascalParser.StructuredTypeContext context)
        {
            return _declarationVisitor.VisitStructuredType(context);
        }

        public override PascalAstNode VisitFileType(PascalParser.FileTypeContext context)
        {
            return _declarationVisitor.VisitFileType(context);
        }

        public override PascalAstNode VisitWithStatement(PascalParser.WithStatementContext context)
        {
            return _statementVisitor.VisitWithStatement(context);
        }

        public override PascalAstNode VisitUnpackedStructuredType(PascalParser.UnpackedStructuredTypeContext context)
        {
            return _declarationVisitor.VisitUnpackedStructuredType(context);
        }

        public override PascalAstNode VisitSet_(PascalParser.Set_Context context)
        {
            return _expressionVisitor.VisitSet_(context);
        }

        protected override PascalAstNode DefaultResult => null!;
    }
}
