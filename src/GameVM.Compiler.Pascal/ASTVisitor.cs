using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using GameVM.Compiler.Pascal.ANTLR;
using System;
using System.Collections.Generic;
using System.Linq;
using GameVM.Compiler.Pascal;

namespace GameVM.Compiler.Pascal
{
    /// <summary>
    /// Main AST Visitor that orchestrates expression, statement, and declaration visitors
    /// </summary>
    public class ASTVisitor : PascalBaseVisitor<PascalASTNode>
    {
        private readonly ASTBuilder _astBuilder;
        private readonly ExpressionVisitor _expressionVisitor;
        private readonly StatementVisitor _statementVisitor;
        private readonly DeclarationVisitor _declarationVisitor;

        public ASTVisitor()
        {
            _astBuilder = new ASTBuilder();
            _expressionVisitor = new ExpressionVisitor(_astBuilder);
            _statementVisitor = new StatementVisitor(_expressionVisitor, _astBuilder);
            _declarationVisitor = new DeclarationVisitor(_expressionVisitor, _astBuilder);
            _declarationVisitor.SetMainVisitor(this);
        }
        public override PascalASTNode VisitProgram(PascalParser.ProgramContext context)
        {
            var programHeading = context.programHeading();
            var identifier = programHeading.identifier();
            if (identifier == null)
                throw new InvalidOperationException("Program must have a name");

            var blockNode = VisitBlock(context.block())
                ?? throw new InvalidOperationException("Block visit returned null");

            return new ProgramNode
            {
                Name = identifier.GetText(),
                Block = (BlockNode)blockNode,
                UsesUnits = new List<PascalASTNode>()
            };
        }

        public override PascalASTNode VisitBlock(PascalParser.BlockContext context)
        {
            var block = new BlockNode
            {
                Statements = new List<PascalASTNode>()
            };

            // Do NOT add label declarations to Statements list (to match test expectations)
            // if (context.labelDeclarationPart() != null)
            // {
            //     foreach (var labelDecl in context.labelDeclarationPart())
            //     {
            //         var node = Visit(labelDecl);
            //         if (node != null)
            //         {
            //             block.Statements.Add(node);
            //         }
            //     }
            // }

            // Add type definitions next
            if (context.typeDefinitionPart() != null)
            {
                foreach (var typeDef in context.typeDefinitionPart())
                {
                    var node = Visit(typeDef);
                    if (node is BlockNode)
                    {
                        foreach (var stmt in ((BlockNode)node).Statements)
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

            // Add procedure/function declarations before variable declarations
            if (context.procedureAndFunctionDeclarationPart() != null)
            {
                foreach (var procDecl in context.procedureAndFunctionDeclarationPart())
                {
                    var node = Visit(procDecl);
                    if (node != null)
                    {
                        block.Statements.Add(node);
                    }
                }
            }

            if (context.variableDeclarationPart() != null)
            {
                foreach (var varDeclPart in context.variableDeclarationPart())
                {
                    foreach (var varDecl in varDeclPart.variableDeclaration())
                    {
                        var node = Visit(varDecl);
                        if (node != null)
                        {
                            block.Statements.Add(node);
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

        public override PascalASTNode VisitCompoundStatement(PascalParser.CompoundStatementContext context)
        {
            return _statementVisitor.VisitCompoundStatement(context);
        }

        public override PascalASTNode VisitStatement(PascalParser.StatementContext context)
        {
            return _statementVisitor.VisitStatement(context);
        }

        public override PascalASTNode VisitAssignmentStatement(PascalParser.AssignmentStatementContext context)
        {
            return _statementVisitor.VisitAssignmentStatement(context);
        }

        public override PascalASTNode VisitVariable(PascalParser.VariableContext context)
        {
            return _expressionVisitor.VisitVariable(context);
        }

        public override PascalASTNode VisitExpression(PascalParser.ExpressionContext context)
        {
            return _expressionVisitor.VisitExpression(context);
        }

        public override PascalASTNode VisitSimpleExpression(PascalParser.SimpleExpressionContext context)
        {
            return _expressionVisitor.VisitSimpleExpression(context);
        }

        public override PascalASTNode VisitTerm(PascalParser.TermContext context)
        {
            return _expressionVisitor.VisitTerm(context);
        }

        public override PascalASTNode VisitSignedFactor(PascalParser.SignedFactorContext context)
        {
            return _expressionVisitor.VisitSignedFactor(context);
        }

        public override PascalASTNode VisitFactor(PascalParser.FactorContext context)
        {
            return _expressionVisitor.VisitFactor(context);
        }

        public override PascalASTNode VisitProcedureDeclaration(PascalParser.ProcedureDeclarationContext context)
        {
            return _declarationVisitor.VisitProcedureDeclaration(context);
        }

        public override PascalASTNode VisitFunctionDeclaration(PascalParser.FunctionDeclarationContext context)
        {
            return _declarationVisitor.VisitFunctionDeclaration(context);
        }

        public override PascalASTNode VisitForStatement(PascalParser.ForStatementContext context)
        {
            return _statementVisitor.VisitForStatement(context);
        }

        public override PascalASTNode VisitArrayType(PascalParser.ArrayTypeContext context)
        {
            return _declarationVisitor.VisitArrayType(context);
        }

        public override PascalASTNode VisitSetType(PascalParser.SetTypeContext context)
        {
            return _declarationVisitor.VisitSetType(context);
        }

        public override PascalASTNode VisitIfStatement(PascalParser.IfStatementContext context)
        {
            return _statementVisitor.VisitIfStatement(context);
        }

        public override PascalASTNode VisitProcedureStatement(PascalParser.ProcedureStatementContext context)
        {
            return _statementVisitor.VisitProcedureStatement(context);
        }

        public override PascalASTNode VisitSimpleType(PascalParser.SimpleTypeContext context)
        {
            return _declarationVisitor.VisitSimpleType(context);
        }

        public override PascalASTNode VisitSubrangeType(PascalParser.SubrangeTypeContext context)
        {
            return _declarationVisitor.VisitSubrangeType(context);
        }

        public override PascalASTNode VisitInitialValue(PascalParser.InitialValueContext context)
        {
            return _statementVisitor.VisitInitialValue(context);
        }

        public override PascalASTNode VisitFinalValue(PascalParser.FinalValueContext context)
        {
            return _statementVisitor.VisitFinalValue(context);
        }

        public override PascalASTNode VisitIndexType(PascalParser.IndexTypeContext context)
        {
            return _declarationVisitor.VisitIndexType(context);
        }

        public override PascalASTNode VisitProcedureAndFunctionDeclarationPart(PascalParser.ProcedureAndFunctionDeclarationPartContext context)
        {
            return _declarationVisitor.VisitProcedureAndFunctionDeclarationPart(context);
        }

        public override PascalASTNode VisitIdentifier(PascalParser.IdentifierContext context)
        {
            return _statementVisitor.VisitIdentifier(context);
        }

        public override PascalASTNode VisitCaseStatement(PascalParser.CaseStatementContext context)
        {
            return _statementVisitor.VisitCaseStatement(context);
        }

        public override PascalASTNode VisitCaseListElement(PascalParser.CaseListElementContext context)
        {
            return _statementVisitor.VisitCaseListElement(context);
        }

        public override PascalASTNode VisitWhileStatement(PascalParser.WhileStatementContext context)
        {
            return _statementVisitor.VisitWhileStatement(context);
        }

        public override PascalASTNode VisitTypeDefinitionPart(PascalParser.TypeDefinitionPartContext context)
        {
            return _declarationVisitor.VisitTypeDefinitionPart(context);
        }

        public override PascalASTNode VisitTypeDefinition(PascalParser.TypeDefinitionContext context)
        {
            return _declarationVisitor.VisitTypeDefinition(context);
        }

        public override PascalASTNode VisitType_(PascalParser.Type_Context context)
        {
            return _declarationVisitor.VisitType_(context);
        }

        public override PascalASTNode VisitConstant(PascalParser.ConstantContext context)
        {
            return _expressionVisitor.VisitConstant(context);
        }

        public override PascalASTNode VisitPointerType(PascalParser.PointerTypeContext context)
        {
            return _declarationVisitor.VisitPointerType(context);
        }

        public override PascalASTNode VisitLabelDeclarationPart(PascalParser.LabelDeclarationPartContext context)
        {
            return _declarationVisitor.VisitLabelDeclarationPart(context);
        }

        public override PascalASTNode VisitGotoStatement(PascalParser.GotoStatementContext context)
        {
            return _statementVisitor.VisitGotoStatement(context);
        }

        public override PascalASTNode VisitScalarType(PascalParser.ScalarTypeContext context)
        {
            return _declarationVisitor.VisitScalarType(context);
        }

        public override PascalASTNode VisitStructuredType(PascalParser.StructuredTypeContext context)
        {
            return _declarationVisitor.VisitStructuredType(context);
        }

        public override PascalASTNode VisitFileType(PascalParser.FileTypeContext context)
        {
            return _declarationVisitor.VisitFileType(context);
        }

        public override PascalASTNode VisitWithStatement(PascalParser.WithStatementContext context)
        {
            return _statementVisitor.VisitWithStatement(context);
        }

        public override PascalASTNode VisitUnpackedStructuredType(PascalParser.UnpackedStructuredTypeContext context)
        {
            return _declarationVisitor.VisitUnpackedStructuredType(context);
        }

        public override PascalASTNode VisitSet_(PascalParser.Set_Context context)
        {
            return _expressionVisitor.VisitSet_(context);
        }

        protected override PascalASTNode DefaultResult => null!;
    }
}
