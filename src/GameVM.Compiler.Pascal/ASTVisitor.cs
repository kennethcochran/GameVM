using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GameVM.Compiler.Pascal
{
    public class ASTVisitor : PascalBaseVisitor<PascalASTNode>
    {
        public override PascalASTNode VisitProgram(PascalParser.ProgramContext context)
        {
            var programHeading = context.programHeading();
            var identifier = programHeading.identifier();
            if (identifier == null)
                throw new InvalidOperationException("Program must have a name");

            var blockNode = VisitBlock(context.block()) as BlockNode 
                ?? throw new InvalidOperationException("Block visit returned null");

            return new ProgramNode
            {
                Name = identifier.GetText(),
                Block = blockNode,
                UsesUnits = new List<PascalASTNode>()
            };
        }

        public override PascalASTNode VisitBlock(PascalParser.BlockContext context)
        {
            var block = new BlockNode
            {
                Statements = new List<PascalASTNode>()
            };

            // Visit all variable declarations
            if (context.variableDeclarationPart() != null)
            {
                foreach (var varDecl in context.variableDeclarationPart())
                {
                    if (Visit(varDecl) is PascalASTNode node)
                        block.Statements.Add(node);
                }
            }

            // Visit all procedure and function declarations
            if (context.procedureAndFunctionDeclarationPart() != null)
            {
                foreach (var procDecl in context.procedureAndFunctionDeclarationPart())
                {
                    if (Visit(procDecl) is PascalASTNode node)
                        block.Statements.Add(node);
                }
            }

            // Visit the compound statement (main program code)
            if (context.compoundStatement() != null)
            {
                if (Visit(context.compoundStatement()) is PascalASTNode node)
                    block.Statements.Add(node);
            }

            return block;
        }

        public override PascalASTNode VisitCompoundStatement(PascalParser.CompoundStatementContext context)
        {
            var statements = new List<PascalASTNode>();
            
            // Visit all statements in the compound statement
            foreach (var stmt in context.statements().statement())
            {
                if (Visit(stmt) is PascalASTNode node)
                    statements.Add(node);
            }

            return new BlockNode { Statements = statements };
        }

        public override PascalASTNode VisitStatement(PascalParser.StatementContext context)
        {
            if (context.unlabelledStatement() != null)
                return Visit(context.unlabelledStatement());
            
            return base.VisitStatement(context);
        }

        public override PascalASTNode VisitAssignmentStatement(PascalParser.AssignmentStatementContext context)
        {
            var left = Visit(context.variable());
            var right = Visit(context.expression());

            if (left == null || right == null)
            {
                // Gracefully handle incomplete assignment statements
                return new ErrorNode("Incomplete assignment statement");
            }

            return new AssignmentNode
            {
                Left = left,
                Right = right
            };
        }

        public override PascalASTNode VisitVariable(PascalParser.VariableContext context)
        {
            var identifier = context.identifier();
            if (identifier == null || identifier.Length == 0)
                throw new InvalidOperationException("Variable must have an identifier");

            return new VariableNode { Name = identifier[0].GetText() };
        }

        public override PascalASTNode VisitExpression(PascalParser.ExpressionContext context)
        {
            if (context.relationaloperator() != null)
            {
                var left = Visit(context.simpleExpression()) as ExpressionNode;
                var right = Visit(context.expression()) as ExpressionNode;

                if (left == null || right == null)
                {
                    // Gracefully handle missing operands in relational operations
                    return new ErrorNode("Relational operation missing operand");
                }

                return new RelationalOperatorNode
                {
                    Operator = context.relationaloperator().GetText(),
                    Left = left,
                    Right = right
                };
            }
            return Visit(context.simpleExpression());
        }

        public override PascalASTNode VisitProcedureDeclaration(PascalParser.ProcedureDeclarationContext context)
        {
            var block = Visit(context.block());
            var identifier = context.identifier();

            if (block == null || identifier == null)
            {
                // Gracefully handle missing procedure block or name
                return new ErrorNode("Procedure declaration is incomplete");
            }

            var procNode = new ProcedureNode
            {
                Name = identifier.GetText(),
                Parameters = new List<VariableNode>(),
                Block = block
            };

            var formalParams = context.formalParameterList()?.formalParameterSection();
            if (formalParams != null)
            {
                foreach (var param in formalParams)
                {
                    var paramGroup = param.parameterGroup();
                    var identList = paramGroup?.identifierList();
                    var identifiers = identList?.identifier();
                    if (identifiers != null)
                    {
                        foreach (var id in identifiers)
                        {
                            procNode.Parameters.Add(new VariableNode { Name = id.GetText() });
                        }
                    }
                }
            }

            return procNode;
        }

        // ... similar pattern for other visitor methods ...
        
        protected override PascalASTNode DefaultResult => null!;
    }

    public class ErrorNode : PascalASTNode
    {
        public string Message { get; set; }

        public ErrorNode(string message)
        {
            Message = message;
        }
    }
}
