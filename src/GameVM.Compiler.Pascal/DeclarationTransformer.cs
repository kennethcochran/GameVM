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
        public StatementTransformer StatementTransformer { get; set; }

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
                    case ConstantDeclarationNode constDeclNode:
                        TransformConstantDeclaration(constDeclNode);
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
        /// Transforms a constant declaration
        /// </summary>
        private void TransformConstantDeclaration(ConstantDeclarationNode constDeclNode)
        {
            if (constDeclNode == null)
                return;

            var hlValue = _expressionTransformer.TransformExpression(constDeclNode.Value);
            object initialValue = null;

            if (hlValue is HighLevelIR.Literal literal)
            {
                initialValue = literal.Value;
            }

            var constType = _context.GetOrCreateBasicType("i32"); // Default for now
            var symbol = new IRSymbol
            {
                Name = constDeclNode.Name,
                Type = constType,
                IsConstant = true,
                InitialValue = initialValue
            };
            _context.SymbolTable[constDeclNode.Name] = symbol;

            // Also add to global IR if we are at top level
            if (_context.FunctionScope.Count <= 1)
            {
                _context.IR.Globals[constDeclNode.Name] = symbol;
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

            // Register in global IR
            _context.IR.Functions[procNode.Name] = procedure;

            // Push scope for procedure
            _context.PushScope();

            // Push onto function scope stack
            _context.FunctionScope.Push(procedure);

            // Process procedure parameters
            foreach (var param in procNode.Parameters)
            {
                var paramType = _context.GetOrCreateBasicType("i32"); // Default to i32 for now
                var irParam = new HighLevelIR.Parameter(param.Name, paramType, _context.SourceFile);
                procedure.AddParameter(irParam);

                var symbol = new IRSymbol
                {
                    Name = param.Name,
                    Type = irParam.Type
                };
                _context.SymbolTable[param.Name] = symbol;
            }

            // Process nested block if it exists
            if (procNode.Block is BlockNode blockNode)
            {
                foreach (var stmt in blockNode.Statements)
                {
                    if (stmt is ProcedureNode or FunctionNode or VariableDeclarationNode or TypeDefinitionNode)
                    {
                        TransformDeclaration(stmt);
                    }
                    else
                    {
                        var transformedStmt = StatementTransformer?.TransformStatement(stmt);
                        if (transformedStmt != null)
                        {
                            procedure.Body.AddStatement(transformedStmt);
                        }
                    }
                }
            }

            // Pop from function scope stack
            _context.FunctionScope.Pop();

            // Pop scope
            _context.PopScope();
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

            // Register in global IR
            _context.IR.Functions[funcNode.Name] = function;

            // Push scope for function
            _context.PushScope();

            // Register function name as a variable in the local scope for return values (Pascal style)
            var returnSymbol = new IRSymbol
            {
                Name = funcNode.Name,
                Type = returnType
            };
            _context.SymbolTable[funcNode.Name] = returnSymbol;

            // Push onto function scope stack
            _context.FunctionScope.Push(function);

            // Process function parameters
            foreach (var param in funcNode.Parameters)
            {
                var paramType = _context.GetOrCreateBasicType("i32"); // Default to i32 for now
                var irParam = new HighLevelIR.Parameter(param.Name, paramType, _context.SourceFile);
                function.AddParameter(irParam);

                var symbol = new IRSymbol
                {
                    Name = param.Name,
                    Type = irParam.Type
                };
                _context.SymbolTable[param.Name] = symbol;
            }

            // Process nested block if it exists
            if (funcNode.Block is BlockNode blockNode)
            {
                foreach (var stmt in blockNode.Statements)
                {
                    if (stmt is ProcedureNode or FunctionNode or VariableDeclarationNode or TypeDefinitionNode)
                    {
                        TransformDeclaration(stmt);
                    }
                    else
                    {
                        var transformedStmt = StatementTransformer?.TransformStatement(stmt);
                        if (transformedStmt != null)
                        {
                            function.Body.AddStatement(transformedStmt);
                        }
                    }
                }
            }

            // Pop from function scope stack
            _context.FunctionScope.Pop();

            // Pop scope
            _context.PopScope();
        }

        /// <summary>
        /// Transforms a variable declaration
        /// </summary>
        public void TransformVariableDeclaration(VariableDeclarationNode varDeclNode)
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

            // Also add to global IR if we are at top level (or current function is main and we only have 1 function scope)
            if (_context.FunctionScope.Count <= 1)
            {
                _context.IR.Globals[varDeclNode.Name] = symbol;
            }
            else
            {
                // Local variable in a function
                var currentFunction = _context.FunctionScope.Peek();
                // IRFunction doesn't have a LocalVariables list, but we can add them to some metadata if needed.
                // For now, they are just in the symbol table which is enough for transformation.
            }
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
