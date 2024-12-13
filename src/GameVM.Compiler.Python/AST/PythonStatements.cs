/*
 * PythonStatements.cs
 * 
 * This file defines the AST nodes for Python statements. Statements are language
 * constructs that perform actions or control program flow, such as assignments,
 * function definitions, and control structures.
 * 
 * Key components:
 * 1. PythonFunctionDef: Function definitions with parameters and body
 * 2. PythonAssignment: Variable assignments
 * 3. PythonReturn: Function return statements
 * 4. PythonIf: Conditional branching with then/else blocks
 * 5. PythonWhile: Loop constructs
 * 6. PythonExpressionStatement: Expressions used as statements
 * 
 * Dependencies:
 * - PythonASTNode.cs: Provides base PythonStatement class
 * - PythonExpressions.cs: Used in conditions and expressions
 * 
 * These statement nodes are created by PythonParseTreeToAST and converted to
 * HLIR statements by PythonASTToHLIR.
 */

using System;
using System.Collections.Generic;

namespace GameVM.Compiler.Python.AST
{
    public class PythonFunctionDef : PythonStatement
    {
        public string Name { get; }
        public IReadOnlyList<string> Parameters { get; }
        public IReadOnlyList<PythonStatement> Body => _body;
        
        private readonly List<PythonStatement> _body = new();

        public PythonFunctionDef(string name, IEnumerable<string> parameters, string sourceFile) 
            : base(sourceFile)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Parameters = new List<string>(parameters ?? throw new ArgumentNullException(nameof(parameters)));
        }

        public void AddStatement(PythonStatement statement)
        {
            if (statement == null) throw new ArgumentNullException(nameof(statement));
            _body.Add(statement);
            AddChild(statement);
        }
    }

    public class PythonAssignment : PythonStatement
    {
        public string Target { get; }
        public PythonExpression Value { get; }

        public PythonAssignment(string target, PythonExpression value, string sourceFile) 
            : base(sourceFile)
        {
            Target = target ?? throw new ArgumentNullException(nameof(target));
            Value = value ?? throw new ArgumentNullException(nameof(value));
            AddChild(value);
        }
    }

    public class PythonReturn : PythonStatement
    {
        public PythonExpression Value { get; }

        public PythonReturn(PythonExpression value, string sourceFile) 
            : base(sourceFile)
        {
            Value = value;
            if (value != null)
            {
                AddChild(value);
            }
        }
    }

    public class PythonIf : PythonStatement
    {
        public PythonExpression Condition { get; }
        public IReadOnlyList<PythonStatement> ThenBlock => _thenBlock;
        public IReadOnlyList<PythonStatement> ElseBlock => _elseBlock;

        private readonly List<PythonStatement> _thenBlock = new();
        private readonly List<PythonStatement> _elseBlock = new();

        public PythonIf(PythonExpression condition, string sourceFile) 
            : base(sourceFile)
        {
            Condition = condition ?? throw new ArgumentNullException(nameof(condition));
            AddChild(condition);
        }

        public void AddThenStatement(PythonStatement statement)
        {
            if (statement == null) throw new ArgumentNullException(nameof(statement));
            _thenBlock.Add(statement);
            AddChild(statement);
        }

        public void AddElseStatement(PythonStatement statement)
        {
            if (statement == null) throw new ArgumentNullException(nameof(statement));
            _elseBlock.Add(statement);
            AddChild(statement);
        }
    }

    public class PythonWhile : PythonStatement
    {
        public PythonExpression Condition { get; }
        public IReadOnlyList<PythonStatement> Body => _body;

        private readonly List<PythonStatement> _body = new();

        public PythonWhile(PythonExpression condition, string sourceFile) 
            : base(sourceFile)
        {
            Condition = condition ?? throw new ArgumentNullException(nameof(condition));
            AddChild(condition);
        }

        public void AddStatement(PythonStatement statement)
        {
            if (statement == null) throw new ArgumentNullException(nameof(statement));
            _body.Add(statement);
            AddChild(statement);
        }
    }

    public class PythonExpressionStatement : PythonStatement
    {
        public PythonExpression Expression { get; }

        public PythonExpressionStatement(PythonExpression expression, string sourceFile) 
            : base(sourceFile)
        {
            Expression = expression ?? throw new ArgumentNullException(nameof(expression));
            AddChild(expression);
        }
    }
}
