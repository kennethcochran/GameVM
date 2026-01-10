using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using GameVM.Compiler.Core.HLIR.Nodes;
using GameVM.Compiler.Core.HLIR.Nodes.Expressions;
using GameVM.Compiler.Core.HLIR.Nodes.Statements;
using GameVM.Compiler.Core.HLIR.Symbols;
using GameVM.Compiler.Core.HLIR.Types;
using PascalParser; // Generated ANTLR parser
using PascalParser.Parser; // Generated ANTLR parser

namespace GameVM.Compiler.CSharp.Transformers.Pascal
{
    /// <summary>
    /// Transforms Pascal AST to HLIR
    /// </summary>
    public class PascalToHlirTransformer : AstToHlirTransformer
    {
        private readonly PascalParser.Parser _parser;

        public PascalToHlirTransformer(PascalParser.Parser parser)
        {
            _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        }

        public override HLIRNode Visit(IParseTree tree)
        {
            return tree.Accept(this);
        }

        public override HLIRNode VisitChildren(IRuleNode node)
        {
            // Default implementation visits all children and returns the last result
            HLIRNode result = null;
            for (int i = 0; i < node.ChildCount; i++)
            {
                result = Visit(node.GetChild(i));
            }
            return result;
        }

        public override HLIRNode VisitTerminal(ITerminalNode node)
        {
            // Terminals are usually handled in their parent rules
            return null;
        }

        public override HLIRNode VisitErrorNode(IErrorNode node)
        {
            // Error nodes are already reported by the parser
            return null;
        }

        // Pascal-specific visit methods
        public HLIRNode VisitProgram(PascalParser.ProgramContext context)
        {
            var programName = context.identifier().GetText();
            var programNode = new BlockStatement(new List<StatementNode>());
            
            // Enter program scope
            SymbolTable.EnterScope(programName);
            
            try
            {
                // Process declarations
                if (context.block()?.declarations() != null)
                {
                    foreach (var declaration in context.block().declarations().declaration())
                    {
                        var result = Visit(declaration);
                        if (result is StatementNode stmt)
                        {
                            (programNode.Statements as List<StatementNode>).Add(stmt);
                        }
                    }
                }
                
                // Process main block
                if (context.block()?.compoundStatement() != null)
                {
                    var mainBlock = Visit(context.block().compoundStatement()) as BlockStatement;
                    if (mainBlock != null)
                    {
                        (programNode.Statements as List<StatementNode>).AddRange(mainBlock.Statements);
                    }
                }
                
                return programNode;
            }
            finally
            {
                SymbolTable.ExitScope();
            }
        }

        public HLIRNode VisitVariableDeclaration(PascalParser.VariableDeclarationContext context)
        {
            var typeNode = Visit(context.type()) as TypeNode;
            if (typeNode == null)
            {
                Error("Invalid type in variable declaration", context);
                return null;
            }

            var declarations = new List<StatementNode>();
            
            foreach (var id in context.identifier())
            {
                var varName = id.GetText();
                var varSymbol = new VariableSymbol(varName, typeNode.Type);
                
                if (!SymbolTable.Define(varSymbol))
                {
                    Error($"Duplicate variable declaration: {varName}", context);
                    continue;
                }
                
                // Create declaration with optional initialization
                ExpressionNode initialValue = null;
                if (context.expression() != null)
                {
                    initialValue = Visit(context.expression()) as ExpressionNode;
                    if (initialValue == null)
                    {
                        Error("Invalid initializer expression", context);
                        continue;
                    }
                }
                
                var varDecl = new VariableDeclaration(varName, typeNode.Type, initialValue);
                declarations.Add(varDecl);
            }
            
            return declarations.Count == 1 ? declarations[0] : new BlockStatement(declarations);
        }

        public HLIRNode VisitAssignment(PascalParser.AssignmentContext context)
        {
            var target = Visit(context.variable()) as ExpressionNode;
            var value = Visit(context.expression()) as ExpressionNode;
            
            if (target == null || value == null)
            {
                Error("Invalid assignment", context);
                return null;
            }
            
            // TODO: Add type checking
            return new AssignmentStatement(target, value);
        }

        public HLIRNode VisitIfStatement(PascalParser.IfStatementContext context)
        {
            var condition = Visit(context.expression()) as ExpressionNode;
            var thenBlock = Visit(context.statement(0)) as StatementNode;
            
            if (condition == null || thenBlock == null)
            {
                return null;
            }
            
            StatementNode elseBlock = null;
            if (context.ELSE() != null && context.statement().Length > 1)
            {
                elseBlock = Visit(context.statement(1)) as StatementNode;
            }
            
            return new IfStatement(condition, thenBlock, elseBlock);
        }

        // Add more visit methods for other Pascal constructs...
        
        // Helper method to create a type node
        private class TypeNode : HLIRNode
        {
            public HLIRType Type { get; }
            
            public TypeNode(HLIRType type)
            {
                Type = type;
            }
            
            public override void Accept(IHLIRVisitor visitor)
            {
                throw new NotSupportedException("TypeNode should not be visited directly");
            }
        }
    }
}
