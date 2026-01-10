using System;
using System.Collections.Generic;
using GameVM.Compiler.Core.IR;

namespace GameVM.Compiler.Pascal
{
    /// <summary>
    /// Transforms declaration AST nodes to High-Level IR declarations
    /// </summary>
    public class DeclarationTransformer
    {
        private readonly TransformationContext _context;
        private readonly ExpressionTransformer _expressionTransformer;

        public DeclarationTransformer(
            TransformationContext context,
            ExpressionTransformer expressionTransformer)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _expressionTransformer = expressionTransformer ?? throw new ArgumentNullException(nameof(expressionTransformer));
        }

        /// <summary>
        /// Transforms a declaration node
        /// </summary>
        public void TransformDeclaration(PascalASTNode declNode)
        {
            if (declNode == null)
                return;

            try
            {
                switch (declNode)
                {
                    case ProcedureNode procNode:
                        TransformProcedure(procNode);
                        break;
                    case FunctionNode funcNode:
                        TransformFunction(funcNode);
                        break;
                    case VariableDeclarationNode varDeclNode:
                        TransformVariableDeclaration(varDeclNode);
                        break;
                    case TypeDefinitionNode typeDefNode:
                        TransformTypeDefinition(typeDefNode);
                        break;
                }
            }
            catch (Exception ex)
            {
                // Log error but continue processing
                _context.AddError($"Error transforming declaration: {ex.Message}");
            }
        }

        /// <summary>
        /// Transforms a procedure declaration
        /// </summary>
        private void TransformProcedure(ProcedureNode procNode)
        {
            if (procNode == null)
                return;

            // Create a procedure function in the IR
            var returnType = _context.GetOrCreateBasicType("void");
            var body = new HighLevelIR.Block(_context.SourceFile);
            var procedure = new HighLevelIR.Function(_context.SourceFile, procNode.Name, returnType, body);

            // Push onto function scope stack
            _context.FunctionScope.Push(procedure);

            // Process procedure parameters
            foreach (var param in procNode.Parameters)
            {
                var paramType = _context.GetOrCreateBasicType("i32"); // Default to i32 for now
                var symbol = new IRSymbol
                {
                    Name = param.Name,
                    Type = paramType
                };
                _context.SymbolTable[param.Name] = symbol;
            }

            // Pop from function scope stack
            _context.FunctionScope.Pop();
        }

        /// <summary>
        /// Transforms a function declaration
        /// </summary>
        private void TransformFunction(FunctionNode funcNode)
        {
            if (funcNode == null)
                return;

            // Create a function in the IR
            var returnType = _context.GetOrCreateBasicType("i32"); // Default return type
            var body = new HighLevelIR.Block(_context.SourceFile);
            var function = new HighLevelIR.Function(_context.SourceFile, funcNode.Name, returnType, body);

            // Push onto function scope stack
            _context.FunctionScope.Push(function);

            // Process function parameters
            foreach (var param in funcNode.Parameters)
            {
                var paramType = _context.GetOrCreateBasicType("i32"); // Default to i32 for now
                var symbol = new IRSymbol
                {
                    Name = param.Name,
                    Type = paramType
                };
                _context.SymbolTable[param.Name] = symbol;
            }

            // Pop from function scope stack
            _context.FunctionScope.Pop();
        }

        /// <summary>
        /// Transforms a variable declaration
        /// </summary>
        private void TransformVariableDeclaration(VariableDeclarationNode varDeclNode)
        {
            if (varDeclNode == null)
                return;

            var varType = _context.GetOrCreateBasicType("i32"); // Default to i32 for now
            var symbol = new IRSymbol
            {
                Name = varDeclNode.Name,
                Type = varType
            };
            _context.SymbolTable[varDeclNode.Name] = symbol;
        }

        /// <summary>
        /// Transforms a type definition
        /// </summary>
        private void TransformTypeDefinition(TypeDefinitionNode typeDefNode)
        {
            if (typeDefNode == null)
                return;

            // Register the type in the type cache
            if (typeDefNode.Type != null)
            {
                var hlType = _context.GetOrCreateBasicType(typeDefNode.Name);
                _context.TypeCache[typeDefNode.Name] = hlType;
            }
        }
    }
}
