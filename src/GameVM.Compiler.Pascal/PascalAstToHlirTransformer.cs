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
        private readonly ExpressionTransformer _expressionTransformer;
        private readonly StatementTransformer _statementTransformer;
        private readonly DeclarationTransformer _declarationTransformer;

        public PascalAstToHlirTransformer(string sourceFile = null)
        {
            _sourceFile = sourceFile ?? "<unknown>";
            _ir = new HighLevelIR { SourceFile = _sourceFile };

            // Initialize context
            _context = new TransformationContext(_sourceFile, _ir);

            // Initialize sub-transformers with context
            _expressionTransformer = new ExpressionTransformer(_context);
            _statementTransformer = new StatementTransformer(_context, _expressionTransformer);
            _declarationTransformer = new DeclarationTransformer(_context, _expressionTransformer);
        }

        /// <summary>
        /// Transforms the given Pascal AST to High-Level IR
        /// </summary>
        public HighLevelIR Transform(ProgramNode programNode)
        {
            if (programNode == null)
                throw new ArgumentNullException(nameof(programNode));

            try
            {
                ProcessProgram(programNode);
                return _ir;
            }
            catch (Exception ex)
            {
                // Log error and return partial IR
                _context.AddError($"Error transforming program: {ex.Message}");
                return _ir;
            }
        }

        /// <summary>
        /// Processes the program node
        /// </summary>
        private void ProcessProgram(ProgramNode programNode)
        {
            if (programNode == null)
                return;

            // Create main function for the program
            var returnType = _context.GetOrCreateBasicType("void");
            var body = new HighLevelIR.Block(_sourceFile);
            var mainFunction = new HighLevelIR.Function(
                _sourceFile,
                programNode.Name ?? "main",
                returnType,
                body
            );

            _context.FunctionScope.Push(mainFunction);

            // Process the program block
            if (programNode.Block != null)
            {
                ProcessBlock(programNode.Block, body);
            }

            _context.FunctionScope.Pop();
            _ir.Functions[mainFunction.Name] = mainFunction;
        }

        /// <summary>
        /// Processes a block node
        /// </summary>
        private void ProcessBlock(BlockNode blockNode, HighLevelIR.Block irBlock)
        {
            if (blockNode == null || irBlock == null)
                return;

            foreach (var stmt in blockNode.Statements)
            {
                // Delegate to appropriate transformer
                if (stmt is ProcedureNode or FunctionNode or VariableDeclarationNode or TypeDefinitionNode)
                {
                    _declarationTransformer.TransformDeclaration(stmt);
                }
                else
                {
                    var transformedStmt = _statementTransformer.TransformStatement(stmt);
                    if (transformedStmt != null)
                    {
                        irBlock.AddStatement(transformedStmt);
                    }
                }
            }
        }
    }
}
