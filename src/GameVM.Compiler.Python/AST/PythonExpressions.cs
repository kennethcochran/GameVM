/*
 * PythonExpressions.cs
 * 
 * This file defines the AST nodes for Python expressions. Expressions are language
 * constructs that compute values, such as literals, identifiers, function calls,
 * and binary operations.
 * 
 * Key components:
 * 1. PythonLiteral: Represents constant values (numbers, strings, etc.)
 * 2. PythonIdentifier: Represents variable and function names
 * 3. PythonFunctionCall: Represents function invocations
 * 4. PythonBinaryOp: Represents binary operations (+, -, *, etc.)
 * 5. PythonOperator: Enum defining supported binary operators
 * 
 * Dependencies:
 * - PythonASTNode.cs: Provides base PythonExpression class
 * 
 * These expression nodes are created by PythonParseTreeToAST and converted to
 * HLIR expressions by PythonASTToHLIR.
 */

using System;
using System.Collections.Generic;

namespace GameVM.Compiler.Python.AST
{
    public enum PythonLiteralType
    {
        Integer,
        Float,
        String,
        Boolean,
        None
    }

    public class PythonLiteral : PythonExpression
    {
        public object Value { get; }
        public PythonLiteralType Type { get; }

        public PythonLiteral(object value, PythonLiteralType type, string sourceFile) 
            : base(sourceFile)
        {
            Value = value;
            Type = type;
        }
    }

    public class PythonIdentifier : PythonExpression
    {
        public string Name { get; }

        public PythonIdentifier(string name, string sourceFile) 
            : base(sourceFile)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }
    }

    public class PythonFunctionCall : PythonExpression
    {
        public PythonExpression Function { get; }
        public IReadOnlyList<PythonExpression> Arguments { get; }

        public PythonFunctionCall(
            PythonExpression function,
            IEnumerable<PythonExpression> arguments,
            string sourceFile) 
            : base(sourceFile)
        {
            Function = function ?? throw new ArgumentNullException(nameof(function));
            Arguments = new List<PythonExpression>(arguments ?? throw new ArgumentNullException(nameof(arguments)));

            AddChild(function);
            foreach (var arg in Arguments)
            {
                AddChild(arg);
            }
        }
    }

    public class PythonBinaryOp : PythonExpression
    {
        public PythonOperator Operator { get; }
        public PythonExpression Left { get; }
        public PythonExpression Right { get; }

        public PythonBinaryOp(
            PythonOperator op,
            PythonExpression left,
            PythonExpression right,
            string sourceFile) 
            : base(sourceFile)
        {
            Operator = op;
            Left = left ?? throw new ArgumentNullException(nameof(left));
            Right = right ?? throw new ArgumentNullException(nameof(right));

            AddChild(left);
            AddChild(right);
        }
    }

    public enum PythonOperator
    {
        Add,
        Subtract,
        Multiply,
        Divide,
        FloorDivide,
        Modulo,
        Power,
        Equal,
        NotEqual,
        LessThan,
        LessEqual,
        GreaterThan,
        GreaterEqual,
        And,
        Or
    }
}
