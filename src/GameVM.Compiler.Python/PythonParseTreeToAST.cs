/*
 * PythonParseTreeToAST.cs
 * 
 * This class converts ANTLR-generated parse trees into a language-specific Abstract Syntax Tree (AST).
 * The AST provides a more structured and semantically meaningful representation of the Python code
 * that is easier to analyze and transform than the raw parse tree.
 * 
 * Key responsibilities:
 * 1. Visits parse tree nodes using ANTLR visitor pattern
 * 2. Creates corresponding AST nodes for each Python language construct
 * 3. Maintains source file information for error reporting
 * 4. Validates basic syntactic structure
 * 
 * Dependencies:
 * - PythonParser.cs: Provides the parse tree nodes to visit
 * - AST/PythonASTNode.cs: Base class for all AST nodes
 * - AST/PythonExpressions.cs: AST nodes for expressions
 * - AST/PythonStatements.cs: AST nodes for statements
 * 
 * This class is used by PythonLanguageParser as part of the compilation pipeline.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using GameVM.Compiler.Core.IR;
using GameVM.Compiler.Python.ANTLR;
using GameVM.Compiler.Python.AST;
using GameVM.Compiler.Core.Exceptions;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

namespace GameVM.Compiler.Python
{
    /// <summary>
    /// Converts Python parse tree to Python AST
    /// </summary>
    public class PythonParseTreeToAST : PythonParserBaseVisitor<PythonASTNode>, IPythonASTBuilder
    {
        private readonly string _sourceFile;

        public PythonParseTreeToAST(string sourceFile)
        {
            _sourceFile = sourceFile ?? throw new ArgumentNullException(nameof(sourceFile));
        }

        public override PythonASTNode VisitFile_input(PythonParser.File_inputContext context)
        {
            var module = new PythonModule(_sourceFile);

            if (context.statements() != null)
            {
                foreach (var stmt in context.statements().statement())
                {
                    var node = Visit(stmt);
                    if (node is PythonStatement statement)
                    {
                        module.AddStatement(statement);
                    }
                }
            }

            return module;
        }

        public override PythonASTNode VisitFunction_def_raw(PythonParser.Function_def_rawContext context)
        {
            if (context == null)
                return null;

            var name = context.NAME()?.GetText() ?? "unnamed_function";
            var parameters = new List<string>();

            if (context.@params()?.parameters() != null)
            {
                var parametersContext = context.@params().parameters();
                
                // Handle parameters with no default values
                var noDefaultParams = parametersContext.param_no_default();
                if (noDefaultParams != null)
                {
                    foreach (var param in noDefaultParams)
                    {
                        var paramName = param.param()?.NAME()?.GetText();
                        if (!string.IsNullOrEmpty(paramName))
                        {
                            parameters.Add(paramName);
                        }
                    }
                }

                // Handle parameters with default values
                var defaultParams = parametersContext.param_with_default();
                if (defaultParams != null)
                {
                    foreach (var param in defaultParams)
                    {
                        var paramName = param.param()?.NAME()?.GetText();
                        if (!string.IsNullOrEmpty(paramName))
                        {
                            parameters.Add(paramName);
                        }
                    }
                }
            }

            var function = new PythonFunctionDef(name, parameters, _sourceFile);

            if (context.block()?.statements() != null)
            {
                foreach (var stmt in context.block().statements().statement())
                {
                    var node = Visit(stmt);
                    if (node is PythonStatement statement)
                    {
                        function.AddStatement(statement);
                    }
                }
            }

            return function;
        }

        public override PythonASTNode VisitAssignment(PythonParser.AssignmentContext context)
        {
            if (context == null)
                return null;

            string target = null;
            if (context.NAME() != null)
            {
                target = context.NAME().GetText();
            }
            else if (context.single_target() != null)
            {
                var singleTarget = context.single_target();
                if (singleTarget.NAME() != null)
                {
                    target = singleTarget.NAME().GetText();
                }
                else if (singleTarget.GetText() != null)
                {
                    target = singleTarget.GetText();
                }
            }

            PythonExpression value = null;
            var annotatedRhs = context.annotated_rhs();
            if (annotatedRhs?.star_expressions() != null)
            {
                value = Visit(annotatedRhs.star_expressions()) as PythonExpression;
            }

            if (target == null)
            {
                throw new InvalidOperationException($"Assignment target cannot be null. Context: {context.GetText()}");
            }

            return new PythonAssignment(target, value ?? new PythonLiteral(null, PythonLiteralType.None, _sourceFile), _sourceFile);
        }

        public override PythonASTNode VisitPrimary(PythonParser.PrimaryContext context)
        {
            // Base case: visit the atom
            var expr = Visit(context.atom()) as PythonExpression;
            if (expr == null) return null;

            // Handle each primary suffix recursively (function calls, attribute access, subscripts)
            if (context.DOT() != null && context.NAME() != null)
            {
                expr = new PythonAttribute(expr, context.NAME().GetText(), _sourceFile);
            }
            else if (context.genexp() != null)
            {
                // Handle generator expressions
                var genExpr = Visit(context.genexp()) as PythonExpression;
                if (genExpr != null)
                {
                    expr = new PythonFunctionCall(expr, new List<PythonExpression> { genExpr }, _sourceFile);
                }
            }
            else if (context.LPAR() != null)
            {
                // Handle function calls with arguments
                var arguments = new List<PythonExpression>();
                var argsContext = context.arguments();
                if (argsContext?.args() != null)
                {
                    // Handle positional and keyword arguments
                    foreach (var arg in argsContext.args().starred_expression())
                    {
                        var argExpr = Visit(arg) as PythonExpression;
                        if (argExpr != null)
                        {
                            arguments.Add(argExpr);
                        }
                    }
                }
                expr = new PythonFunctionCall(expr, arguments, _sourceFile);
            }
            else if (context.LSQB() != null && context.slices() != null)
            {
                // Handle subscript operations
                var slice = Visit(context.slices()) as PythonExpression;
                if (slice != null)
                {
                    expr = new PythonSubscript(expr, slice, _sourceFile);
                }
            }

            return expr;
        }

        public override PythonASTNode VisitAtom(PythonParser.AtomContext context)
        {
            if (context.NAME() != null)
            {
                return new PythonIdentifier(context.NAME().GetText(), _sourceFile);
            }
            else if (context.NUMBER() != null)
            {
                var text = context.NUMBER().GetText();
                if (text.Contains('.'))
                {
                    return new PythonLiteral(double.Parse(text), PythonLiteralType.Float, _sourceFile);
                }
                else
                {
                    return new PythonLiteral(int.Parse(text), PythonLiteralType.Integer, _sourceFile);
                }
            }
            else if (context.strings() != null)
            {
                // Handle both regular strings and f-strings
                var stringContext = context.strings();
                var text = stringContext.GetText();
                // Remove quotes if present
                if (text.StartsWith("\"") || text.StartsWith("'"))
                {
                    text = text.Substring(1, text.Length - 2);
                }
                return new PythonLiteral(text, PythonLiteralType.String, _sourceFile);
            }
            else if (context.TRUE() != null)
            {
                return new PythonLiteral(true, PythonLiteralType.Boolean, _sourceFile);
            }
            else if (context.FALSE() != null)
            {
                return new PythonLiteral(false, PythonLiteralType.Boolean, _sourceFile);
            }
            else if (context.NONE() != null)
            {
                return new PythonLiteral(null, PythonLiteralType.None, _sourceFile);
            }
            else if (context.tuple() != null)
            {
                return Visit(context.tuple());
            }
            else if (context.group() != null)
            {
                return Visit(context.group());
            }
            else if (context.genexp() != null)
            {
                return Visit(context.genexp());
            }
            else if (context.list() != null)
            {
                return Visit(context.list());
            }
            else if (context.listcomp() != null)
            {
                return Visit(context.listcomp());
            }
            else if (context.dict() != null)
            {
                return Visit(context.dict());
            }
            else if (context.set() != null)
            {
                return Visit(context.set());
            }
            else if (context.dictcomp() != null)
            {
                return Visit(context.dictcomp());
            }
            else if (context.setcomp() != null)
            {
                return Visit(context.setcomp());
            }
            else if (context.ELLIPSIS() != null)
            {
                return new PythonLiteral(null, PythonLiteralType.None, _sourceFile); // Treat ellipsis as None for now
            }

            return null;
        }

        public override PythonASTNode VisitReturn_stmt(PythonParser.Return_stmtContext context)
        {
            PythonExpression value = null;
            if (context.star_expressions() != null)
            {
                value = Visit(context.star_expressions()) as PythonExpression;
            }
            return new PythonReturn(value, _sourceFile);
        }

        public override PythonASTNode VisitIf_stmt(PythonParser.If_stmtContext context)
        {
            var condition = Visit(context.named_expression()) as PythonExpression;
            var ifStmt = new PythonIf(condition, _sourceFile);

            if (context.block()?.statements() != null)
            {
                foreach (var stmt in context.block().statements().statement())
                {
                    var node = Visit(stmt);
                    if (node is PythonStatement statement)
                    {
                        ifStmt.AddThenStatement(statement);
                    }
                }
            }

            if (context.else_block()?.block()?.statements() != null)
            {
                foreach (var stmt in context.else_block().block().statements().statement())
                {
                    var node = Visit(stmt);
                    if (node is PythonStatement statement)
                    {
                        ifStmt.AddElseStatement(statement);
                    }
                }
            }

            return ifStmt;
        }

        public override PythonASTNode VisitWhile_stmt(PythonParser.While_stmtContext context)
        {
            var condition = Visit(context.named_expression()) as PythonExpression;
            var whileStmt = new PythonWhile(condition, _sourceFile);

            if (context.block()?.statements() != null)
            {
                foreach (var stmt in context.block().statements().statement())
                {
                    var node = Visit(stmt);
                    if (node is PythonStatement statement)
                    {
                        whileStmt.AddStatement(statement);
                    }
                }
            }

            return whileStmt;
        }

        public PythonASTNode Build(IParseTree parseTree)
        {
            return Visit(parseTree);
        }

    }
}
