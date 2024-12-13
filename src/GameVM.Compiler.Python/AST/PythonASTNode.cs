/*
 * PythonASTNode.cs
 * 
 * This file defines the base classes for Python's Abstract Syntax Tree (AST) nodes.
 * The AST provides a structured representation of Python code that captures its
 * syntactic and semantic meaning while abstracting away parsing details.
 * 
 * Key components:
 * 1. PythonASTNode: Base class for all AST nodes
 * 2. PythonExpression: Base class for expression nodes
 * 3. PythonStatement: Base class for statement nodes
 * 
 * Dependencies:
 * - PythonExpressions.cs: Concrete expression node implementations
 * - PythonStatements.cs: Concrete statement node implementations
 * 
 * These AST nodes are created by PythonParseTreeToAST and consumed by PythonASTToHLIR.
 */

using System;
using System.Collections.Generic;

namespace GameVM.Compiler.Python.AST
{
    /// <summary>
    /// Base class for all Python AST nodes
    /// </summary>
    public abstract class PythonASTNode
    {
        public string SourceFile { get; }
        public IReadOnlyList<PythonASTNode> Children => _children;
        
        private readonly List<PythonASTNode> _children = new();

        protected PythonASTNode(string sourceFile)
        {
            SourceFile = sourceFile ?? throw new ArgumentNullException(nameof(sourceFile));
        }

        protected void AddChild(PythonASTNode child)
        {
            if (child == null) throw new ArgumentNullException(nameof(child));
            _children.Add(child);
        }
    }

    public class PythonModule : PythonASTNode
    {
        public IReadOnlyList<PythonStatement> Statements => _statements;
        private readonly List<PythonStatement> _statements = new();

        public PythonModule(string sourceFile) : base(sourceFile) { }

        public void AddStatement(PythonStatement statement)
        {
            if (statement == null) throw new ArgumentNullException(nameof(statement));
            _statements.Add(statement);
            AddChild(statement);
        }
    }

    public abstract class PythonStatement : PythonASTNode
    {
        protected PythonStatement(string sourceFile) : base(sourceFile) { }
    }

    public abstract class PythonExpression : PythonASTNode
    {
        protected PythonExpression(string sourceFile) : base(sourceFile) { }
    }
}
