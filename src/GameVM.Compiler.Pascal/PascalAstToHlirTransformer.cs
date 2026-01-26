using System;
using System.Collections.Generic;
using GameVM.Compiler.Core.IR;

namespace GameVM.Compiler.Pascal
{
    /// <summary>
    /// Transforms a Pascal AST to High-Level IR using delegation pattern
    /// Orchestrates ExpressionTransformer, StatementTransformer, and DeclarationTransformer
    /// </summary>
    public class PascalAstToHlirTransformer
    {
        private readonly string _sourceFile;
        private readonly HighLevelIR _ir;
        private readonly TransformationContext _context;

        // Sub-transformers
        private readonly StatementTransformer _statementTransformer;
        private readonly DeclarationTransformer _declarationTransformer;

        public PascalAstToHlirTransformer(string? sourceFile = null)
        {
            _sourceFile = sourceFile ?? "<unknown>";
            _ir = new HighLevelIR { SourceFile = _sourceFile };

            // Initialize context
            _context = new TransformationContext(_sourceFile, _ir);

            // Initialize sub-transformers with context
            var expressionTransformer = new ExpressionTransformer(_context);
            _statementTransformer = new StatementTransformer(_context, expressionTransformer);
            _declarationTransformer = new DeclarationTransformer(_context, expressionTransformer);
            _declarationTransformer.StatementTransformer = _statementTransformer;
        }

        /// <summary>
        /// Transforms the given Pascal AST to High-Level IR
        /// </summary>
        public HighLevelIR Transform(ProgramNode programNode)
        {
            if (programNode == null)
                throw new ArgumentNullException(nameof(programNode));

            ProcessProgram(programNode);

            // Sync errors to IR
            _ir.Errors.AddRange(_context.Errors);

            return _ir;
        }

        /// <summary>
        /// Processes the program node
        /// </summary>
        private void ProcessProgram(ProgramNode programNode)
        {
            if (programNode == null)
                return;

            _context.PushScope();

            ProcessGlobalDeclarations(programNode);
            var mainFunction = CreateMainFunction(programNode);
            ProcessProgramStatements(programNode, mainFunction);

            _context.AddGlobalFunction(mainFunction);
        }

        private void ProcessGlobalDeclarations(ProgramNode programNode)
        {
            if (programNode.Block == null) return;

            foreach (var stmt in programNode.Block.Statements)
            {
                if (stmt is VariableDeclarationNode varDecl)
                {
                    _declarationTransformer.TransformVariableDeclaration(varDecl);
                }
                else if (stmt is ConstantDeclarationNode constDecl)
                {
                    _declarationTransformer.TransformDeclaration(constDecl);
                }
            }
        }

        private HighLevelIR.Function CreateMainFunction(ProgramNode programNode)
        {
            var returnType = _context.GetOrCreateBasicType("void");
            var body = new HighLevelIR.Block(_sourceFile);
            return new HighLevelIR.Function(
                _sourceFile,
                programNode.Name ?? "main",
                returnType,
                body
            );
        }

        private void ProcessProgramStatements(ProgramNode programNode, HighLevelIR.Function mainFunction)
        {
            if (programNode.Block == null) return;

            _context.FunctionScope.Push(mainFunction);
            var body = mainFunction.Body;

            foreach (var stmt in programNode.Block.Statements)
            {
                if (stmt is VariableDeclarationNode or ConstantDeclarationNode) continue;

                if (stmt is ProcedureNode or FunctionNode or TypeDefinitionNode)
                {
                    _declarationTransformer.TransformDeclaration(stmt);
                }
                else if (stmt is ErrorNode errorNode)
                {
                    _context.AddError(errorNode.Message);
                }
                else
                {
                    var transformedStmt = _statementTransformer.TransformStatement(stmt);
                    if (transformedStmt != null)
                    {
                        body.AddStatement(transformedStmt);
                    }
                }
            }

            _context.FunctionScope.Pop();
        }

    }
}
