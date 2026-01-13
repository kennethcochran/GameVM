using GameVM.Compiler.Pascal.ANTLR;
using System.Collections.Generic;
using System.Linq;

namespace GameVM.Compiler.Pascal
{
    /// <summary>
    /// Visitor for statement-related AST nodes in Pascal
    /// </summary>
    public class StatementVisitor : PascalBaseVisitor<PascalASTNode>
    {
        private readonly ExpressionVisitor _expressionVisitor;
        private readonly ASTBuilder _astBuilder;

        public StatementVisitor(ExpressionVisitor expressionVisitor, ASTBuilder astBuilder = null)
        {
            _expressionVisitor = expressionVisitor;
            _astBuilder = astBuilder ?? new ASTBuilder();
        }

        public override PascalASTNode VisitCompoundStatement(PascalParser.CompoundStatementContext context)
        {
            var statements = new List<PascalASTNode>();

            foreach (var stmt in context.statements().statement())
            {
                var node = Visit(stmt);
                if (node != null)
                {
                    statements.Add(node);
                }
            }

            return new BlockNode { Statements = statements! };
        }

        public override PascalASTNode VisitStatement(PascalParser.StatementContext context)
        {
            if (context.unlabelledStatement() != null)
                return Visit(context.unlabelledStatement());

            return base.VisitStatement(context);
        }

        public override PascalASTNode VisitAssignmentStatement(PascalParser.AssignmentStatementContext context)
        {
            var left = Visit(context.variable()) as VariableNode;
            var right = _expressionVisitor.Visit(context.expression());
            if (left == null || right == null)
            {
                return new ErrorNode("Incomplete assignment statement");
            }
            return new AssignmentNode
            {
                Left = left!,
                Right = right!
            };
        }

        public override PascalASTNode VisitIfStatement(PascalParser.IfStatementContext context)
        {
            var condition = _expressionVisitor.Visit(context.expression()) as ExpressionNode;
            var thenBlock = Visit(context.statement(0)); // Removed strict casting to BlockNode
            var elseBlock = context.statement().Length > 1 ? Visit(context.statement(1)) : null; // Removed strict casting to BlockNode

            if (condition == null || thenBlock == null)
            {
                return new ErrorNode("Incomplete if statement");
            }

            return new IfNode
            {
                Condition = condition,
                ThenBlock = thenBlock,
                ElseBlock = elseBlock
            };
        }

        public override PascalASTNode VisitWhileStatement(PascalParser.WhileStatementContext context)
        {
            var condition = _expressionVisitor.Visit(context.expression()) as ExpressionNode;
            var block = Visit(context.statement()); // Remove strict casting to BlockNode

            if (condition == null || block == null)
            {
                return new ErrorNode("Incomplete while statement");
            }

            return new WhileNode
            {
                Condition = condition,
                Block = block
            };
        }

        public override PascalASTNode VisitForStatement(PascalParser.ForStatementContext context)
        {
            var variable = Visit(context.identifier()) as VariableNode;
            var fromExpression = _expressionVisitor.Visit(context.forList().initialValue()) as ExpressionNode;
            var toExpression = _expressionVisitor.Visit(context.forList().finalValue()) as ExpressionNode;

            if (variable == null || fromExpression == null || toExpression == null)
            {
                return new ErrorNode("Incomplete for statement");
            }

            var block = Visit(context.statement()) as BlockNode;
            if (block == null)
            {
                block = new BlockNode { Statements = new List<PascalASTNode>() };
            }

            return new ForNode
            {
                Variable = variable,
                FromExpression = fromExpression,
                ToExpression = toExpression,
                Block = block
            };
        }

        public override PascalASTNode VisitCaseStatement(PascalParser.CaseStatementContext context)
        {
            var selector = _expressionVisitor.Visit(context.expression()) as ExpressionNode;
            if (selector == null)
            {
                return new ErrorNode("Invalid case selector");
            }

            var branches = new List<CaseBranchNode>();

            // Process the case list elements
            foreach (var caseListElement in context.caseListElement())
            {
                var branch = Visit(caseListElement) as CaseBranchNode;
                if (branch != null)
                {
                    branches.Add(branch);
                }
            }

            // Process the else part if it exists
            PascalASTNode? elseBlock = null;
            if (context.statements() != null)
            {
                var statements = new List<PascalASTNode>();
                foreach (var stmt in context.statements().statement())
                {
                    var stmtNode = Visit(stmt);
                    if (stmtNode != null)
                    {
                        statements.Add(stmtNode);
                    }
                }

                if (statements.Any())
                {
                    elseBlock = new BlockNode { Statements = statements };
                }
            }

            return new CaseNode
            {
                Selector = selector,
                Branches = branches,
                ElseBlock = elseBlock
            };
        }

        public override PascalASTNode VisitCaseListElement(PascalParser.CaseListElementContext context)
        {
            var constList = context.constList();
            if (constList == null)
            {
                return new ErrorNode("Missing constant list in case list element");
            }

            var labels = new List<ExpressionNode>();
            foreach (var constant in constList.constant())
            {
                var constantNode = _expressionVisitor.Visit(constant);
                if (constantNode is ConstantNode || constantNode is LiteralNode)
                {
                    labels.Add((ExpressionNode)constantNode);
                }
            }

            var statement = Visit(context.statement());
            if (statement == null)
            {
                return new ErrorNode("Invalid case statement block");
            }

            if (labels.Count == 0)
            {
                return new ErrorNode("Case branch must have at least one label");
            }

            return new CaseBranchNode
            {
                Labels = labels,
                Statement = statement
            };
        }

        public override PascalASTNode VisitProcedureStatement(PascalParser.ProcedureStatementContext context)
        {
            var name = context.identifier()?.GetText();
            var arguments = new List<PascalASTNode>();
            
            var paramList = context.parameterList();
            if (paramList != null)
            {
                var actualParams = paramList.actualParameter();
                if (actualParams != null)
                {
                    foreach (var arg in actualParams)
                    {
                        var visitedArg = _expressionVisitor.Visit(arg);
                        if (visitedArg != null)
                        {
                            arguments.Add(visitedArg);
                        }
                    }
                }
            }

            if (name == null)
            {
                return new ErrorNode("Incomplete procedure call");
            }

            return new ProcedureCallNode
            {
                Name = name,
                Arguments = arguments
            };
        }

        public override PascalASTNode VisitWithStatement(PascalParser.WithStatementContext context)
        {
            // Parse the record variable list
            var recordVars = new List<VariableNode>();
            var recordVarList = context.recordVariableList();
            if (recordVarList != null)
            {
                foreach (var varCtx in recordVarList.variable())
                {
                    var varNode = Visit(varCtx) as VariableNode;
                    if (varNode != null)
                        recordVars.Add(varNode);
                }
            }

            // Parse the block (statement)
            var block = Visit(context.statement());
            if (recordVars.Count == 0 || block == null)
            {
                return new ErrorNode("Invalid with statement");
            }

            return new WithNode
            {
                RecordVariables = recordVars,
                Block = block
            };
        }

        public override PascalASTNode VisitGotoStatement(PascalParser.GotoStatementContext context)
        {
            // gotoStatement: GOTO label;
            var labelCtx = context.label();
            if (labelCtx == null)
                return new ErrorNode("Goto statement missing label");
            if (!int.TryParse(labelCtx.GetText(), out int labelValue))
                return new ErrorNode("Invalid label in goto statement");
            return new GotoNode { TargetLabel = labelValue };
        }

        public override PascalASTNode VisitIdentifier(PascalParser.IdentifierContext context)
        {
            if (context == null || string.IsNullOrEmpty(context.GetText()))
            {
                return new ErrorNode("Invalid identifier");
            }

            return new VariableNode { Name = context.GetText() };
        }

        public override PascalASTNode VisitVariable(PascalParser.VariableContext context)
        {
            var identifier = context.identifier();
            if (identifier == null || identifier.Length == 0)
            {
                return new ErrorNode("Variable must have an identifier");
            }
            return new VariableNode { Name = identifier[0].GetText() };
        }

        // Custom implementation for VisitInitialValue
        public override PascalASTNode VisitInitialValue(PascalParser.InitialValueContext context)
        {
            return _expressionVisitor.Visit(context.expression()) ?? new ErrorNode("Invalid initial value in for loop");
        }

        // Custom implementation for VisitFinalValue
        public override PascalASTNode VisitFinalValue(PascalParser.FinalValueContext context)
        {
            return _expressionVisitor.Visit(context.expression()) ?? new ErrorNode("Invalid final value in for loop");
        }

        protected override PascalASTNode DefaultResult => null!;
    }
}
