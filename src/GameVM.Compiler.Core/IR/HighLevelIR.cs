using System;
using System.Collections.Generic;

namespace GameVM.Compiler.Core.IR
{
    /// <summary>
    /// High-level intermediate representation of source code.
    /// This is the first IR level after parsing, closest to the source language.
    /// </summary>
    public class HighLevelIR : IIntermediateRepresentation
    {
        /// <summary>
        /// The source file this IR was generated from
        /// </summary>
        public string SourceFile { get; set; }

        /// <summary>
        /// The top-level statements and declarations
        /// </summary>
        public List<IRNode> TopLevel { get; set; } = new();

        /// <summary>
        /// Global variables and constants
        /// </summary>
        public Dictionary<string, IRSymbol> Globals { get; set; } = new();

        /// <summary>
        /// Function definitions
        /// </summary>
        public Dictionary<string, IRFunction> Functions { get; set; } = new();

        /// <summary>
        /// Type definitions
        /// </summary>
        public Dictionary<string, IRType> Types { get; set; } = new();

        // Nested types for High Level IR
        public class HLType : IRType
        {
            public new string Name { get; set; }
            public string SourceFile { get; set; }

            public HLType(string sourceFile, string name)
            {
                SourceFile = sourceFile;
                Name = name;
            }
        }

        public class BasicType : HLType
        {
            public BasicType(string sourceFile, string name) : base(sourceFile, name) { }
        }

        public class Module : IRNode
        {
            public string SourceFile { get; }
            public string Name { get; }
            private List<Function> functions = new();

            public Module(string sourceFile, string name)
            {
                SourceFile = sourceFile;
                Name = name;
            }

            public void AddFunction(Function function)
            {
                functions.Add(function);
            }
        }

        public class Block : Statement
        {
            public List<Statement> Statements { get; set; } = new();

            public Block(string sourceFile) : base(sourceFile)
            {
            }

            public void AddStatement(Statement statement)
            {
                Statements.Add(statement);
            }
        }

        public class Statement : IRNode
        {
            public string SourceFile { get; }

            public Statement(string sourceFile)
            {
                SourceFile = sourceFile;
            }
        }

        public class Function : IRFunction
        {
            public string SourceFile { get; }
            public new HLType ReturnType { get => (HLType)base.ReturnType; set => base.ReturnType = value; }
            public new Block Body { get => (Block)base.Body[0]; set { base.Body.Clear(); base.Body.Add(value); } }
            private List<Parameter> parameters = new();

            public Function(string sourceFile, string name, HLType returnType, Block body)
            {
                SourceFile = sourceFile;
                Name = name;
                ReturnType = returnType;
                Body = body;
            }

            public void AddParameter(Parameter parameter)
            {
                parameters.Add(parameter);
            }
        }

        public class Parameter : IRParameter
        {
            public new string Name { get; }
            public new HLType Type { get; }
            public string SourceFile { get; }

            public Parameter(string name, HLType type, string sourceFile)
            {
                Name = name;
                Type = type;
                SourceFile = sourceFile;
            }
        }

        public class Expression : IRNode
        {
            public string SourceFile { get; }

            public Expression(string sourceFile)
            {
                SourceFile = sourceFile;
            }
        }

        public class Assignment : Statement
        {
            public string Target { get; }
            public Expression Value { get; }

            public Assignment(string target, Expression value, string sourceFile) : base(sourceFile)
            {
                Target = target;
                Value = value;
            }
        }

        public class Return : Statement
        {
            public Expression? Value { get; }

            public Return(Expression? value) : base(value?.SourceFile ?? string.Empty)
            {
                Value = value;
            }
        }

        public class IfStatement : Statement
        {
            public Expression Condition { get; }
            public List<IRNode> ThenBlock { get; }
            public List<IRNode>? ElseBlock { get; }

            public IfStatement(Expression condition, List<IRNode> thenBlock, List<IRNode>? elseBlock = null)
                : base(condition.SourceFile)
            {
                Condition = condition;
                ThenBlock = thenBlock;
                ElseBlock = elseBlock;
            }
        }

        public class While : Statement
        {
            public Expression Condition { get; }
            public Block Body { get; }

            public While(Expression condition, Block body, string sourceFile) : base(sourceFile)
            {
                Condition = condition;
                Body = body;
            }
        }

        public class ExpressionStatement : Statement
        {
            public Expression Expression { get; }

            public ExpressionStatement(Expression expression, string sourceFile) : base(sourceFile)
            {
                Expression = expression;
            }
        }

        public class Literal : Expression
        {
            public object Value { get; }
            public HLType Type { get; }

            public Literal(object value, HLType type, string sourceFile) : base(sourceFile)
            {
                Value = value;
                Type = type;
            }
        }

        public class Identifier : Expression
        {
            public string Name { get; }
            public HLType Type { get; }

            public Identifier(string name, HLType type, string sourceFile) : base(sourceFile)
            {
                Name = name;
                Type = type;
            }
        }

        public class FunctionCall : Expression
        {
            public Expression Function { get; }
            public IEnumerable<Expression> Arguments { get; }

            public FunctionCall(Expression function, IEnumerable<Expression> arguments)
                : base(function.SourceFile)
            {
                Function = function;
                Arguments = arguments;
            }
        }

        public class BinaryOp : Expression
        {
            public string Operator { get; }
            public Expression Left { get; }
            public Expression Right { get; }

            public BinaryOp(string op, Expression left, Expression right, string sourceFile)
                : base(sourceFile)
            {
                Operator = op;
                Left = left;
                Right = right;
            }
        }
    }

    /// <summary>
    /// Base class for all IR nodes
    /// </summary>
    public abstract class IRNode
    {
        /// <summary>
        /// Source location information
        /// </summary>
        public IRSourceLocation Location { get; set; }
    }

    /// <summary>
    /// Source code location information
    /// </summary>
    public class IRSourceLocation
    {
        public string File { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }
    }

    /// <summary>
    /// Symbol information for variables and constants
    /// </summary>
    public class IRSymbol : IRNode
    {
        public string Name { get; set; }
        public IRType Type { get; set; }
        public bool IsConstant { get; set; }
        public object InitialValue { get; set; }
    }

    /// <summary>
    /// Function definition in IR
    /// </summary>
    public class IRFunction : IRNode
    {
        public string Name { get; set; }
        public IRType ReturnType { get; set; }
        public List<IRParameter> Parameters { get; set; } = new();
        public List<IRNode> Body { get; set; } = new();
    }

    /// <summary>
    /// Function parameter
    /// </summary>
    public class IRParameter : IRNode
    {
        public string Name { get; set; }
        public IRType Type { get; set; }
        public bool HasDefaultValue { get; set; }
        public object DefaultValue { get; set; }
    }

    /// <summary>
    /// Type information in IR
    /// </summary>
    public class IRType : IRNode
    {
        public string Name { get; set; }
        public bool IsBuiltin { get; set; }
        public List<IRField> Fields { get; set; } = new();
        public List<IRFunction> Methods { get; set; } = new();
    }

    /// <summary>
    /// Field in a type definition
    /// </summary>
    public class IRField : IRNode
    {
        public string Name { get; set; }
        public IRType Type { get; set; }
        public bool IsStatic { get; set; }
    }
}
