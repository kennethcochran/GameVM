using System;
using System.Collections.Generic;
using System.Linq;

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
        /// Errors encountered during transformation
        /// </summary>
        public List<string> Errors { get; set; } = new();

        /// <summary>
        /// List of modules in high-level representation
        /// </summary>
        public List<HLModule> Modules { get; set; } = new();

        /// <summary>
        /// Global functions (kept for backward compatibility)
        /// </summary>
        [System.Obsolete("Use Modules instead")]
        public Dictionary<string, Function> GlobalFunctions { get; set; } = new();
        
        [System.Obsolete("Use Modules instead")]
        public Dictionary<string, Function> Functions { get => GlobalFunctions; set => GlobalFunctions = value; }

        /// <summary>
        /// Type definitions (kept for backward compatibility)
        /// </summary>
        [System.Obsolete("Use Modules instead")]
        public Dictionary<string, IRType> GlobalTypes { get; set; } = new();

        [System.Obsolete("Use Modules instead")]
        public Dictionary<string, IRType> Types { get => GlobalTypes; set => GlobalTypes = value; }

        /// <summary>
        /// Represents a module in the high-level IR
        /// </summary>
        public class HLModule
        {
            /// <summary>
            /// The name of the module
            /// </summary>
            public string Name { get; set; } = string.Empty;
            
            /// <summary>
            /// Functions defined in this module
            /// </summary>
            public List<Function> Functions { get; set; } = new();
            
            /// <summary>
            /// Type definitions in this module
            /// </summary>
            public List<HLType> Types { get; set; } = new();

            /// <summary>
            /// Global variables in this module
            /// </summary>
            public List<Variable> Variables { get; set; } = new();
        }

        public class Variable : IRSymbol
        {
            public string SourceFile { get; set; } = string.Empty;
            public new HLType Type { get; set; }

            public Variable() { }
            public Variable(string name, HLType type, string sourceFile)
            {
                Name = name;
                Type = type;
                SourceFile = sourceFile;
            }

            public Variable(string name, string typeName, string sourceFile)
            {
                Name = name;
                Type = new BasicType(sourceFile, typeName);
                SourceFile = sourceFile;
            }

            public Variable(string name, string typeName) : this(name, typeName, "unknown") { }
        }

        // Nested types for High Level IR
        public class HLType : IRType
        {
            public new string Name { get; set; } = string.Empty;
            public string SourceFile { get; set; } = string.Empty;

            public HLType() { }
            public HLType(string sourceFile, string name)
            {
                SourceFile = sourceFile;
                Name = name;
            }

            public HLType(string name) : this("unknown", name) { }

            public static implicit operator HLType(string name) => new HLType("unknown", name);
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
            
            // For property-based initialization
            public List<Statement> statements { get => Statements; set => Statements = value; }

            public Block() : base("unknown") { }
            public Block(string sourceFile) : base(sourceFile)
            {
            }

            public Block(string sourceFile, List<Statement> statements) : base(sourceFile)
            {
                Statements = statements;
            }

            public void AddStatement(Statement statement)
            {
                Statements.Add(statement);
            }
        }

        public class Statement : IRNode
        {
            public new string SourceFile { get; set; } = string.Empty;

            public Statement() : base("unknown") { }
            public Statement(string sourceFile)
            {
                SourceFile = sourceFile;
            }
        }

        public class Function : IRFunction
        {
            public string SourceFile { get; set; } = string.Empty;
            public new HLType ReturnType { get => (HLType)base.ReturnType; set => base.ReturnType = value; }
            public new Block Body { get => (Block)base.Body[0]; set { base.Body.Clear(); base.Body.Add(value); } }
            public List<Parameter> Parameters { get; set; } = new();
            
            // For property-based initialization
            public string name { get => Name; set => Name = value; }
            public HLType returnType { get => ReturnType; set => ReturnType = value; }
            public Block body { get => Body; set => Body = value; }

            public Function()
            {
                base.Body.Add(new Block("unknown"));
            }

            public Function(string sourceFile, string name, HLType returnType, Block body)
            {
                SourceFile = sourceFile;
                Name = name;
                ReturnType = returnType;
                Body = body;
            }

            public Function(string name, string sourceFile, string returnTypeName, Block body)
            {
                Name = name;
                SourceFile = sourceFile;
                ReturnType = new BasicType(sourceFile, returnTypeName);
                Body = body;
            }

            public void AddParameter(Parameter parameter)
            {
                Parameters.Add(parameter);
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

            public Parameter(string name, string typeName, string sourceFile)
            {
                Name = name;
                Type = new BasicType(sourceFile, typeName);
                SourceFile = sourceFile;
            }

            public Parameter(string name, string typeName) : this(name, typeName, "unknown") { }
        }

        public class Expression : IRNode
        {
            public new string SourceFile { get => base.SourceFile; set => base.SourceFile = value; }
            public HLType Type { get; set; }

            public Expression() : base("unknown") { }
            public Expression(string sourceFile) : base(sourceFile)
            {
            }
        }

        public class Assignment : Statement
        {
            public string Target { get; set; } = string.Empty;
            public Expression Value { get; set; }
            
            // For property-based initialization
            public string target { get => Target; set => Target = value; }
            public Expression value { get => Value; set => Value = value; }

            public Assignment() : base("unknown") { }
            public Assignment(string target, Expression value, string sourceFile) : base(sourceFile)
            {
                Target = target;
                Value = value;
            }

            public Assignment(string target, Expression value) : this(target, value, "unknown") { }

            public Assignment(Expression target, Expression value, string sourceFile) : base(sourceFile)
            {
                Target = target.ToString(); // Simplified for now
                Value = value;
            }
        }

        public class Return : Statement
        {
            public Expression? Value { get; set; }
            
            // For property-based initialization
            public Expression? value { get => Value; set => Value = value; }

            public Return() : base("unknown") { }
            public Return(Expression? value) : base(value?.SourceFile ?? string.Empty)
            {
                Value = value;
            }
        }
        
        public class ReturnStatement : Return
        {
            public ReturnStatement() { }
            public ReturnStatement(Expression? value) : base(value) { }
        }

        public class IfStatement : Statement
        {
            public Expression Condition { get; set; }
            public List<IRNode> ThenBlock { get; set; } = new();
            public List<IRNode>? ElseBlock { get; set; }
            public Block Then { get => new Block(SourceFile, ThenBlock.Cast<Statement>().ToList()); set => ThenBlock = value.Statements.Cast<IRNode>().ToList(); }
            public Block? Else { get => ElseBlock == null ? null : new Block(SourceFile, ElseBlock.Cast<Statement>().ToList()); set => ElseBlock = value?.Statements.Cast<IRNode>().ToList(); }
            
            // For property-based initialization
            public Expression condition { get => Condition; set => Condition = value; }
            public Block then { get => Then; set => Then = value; }
            public Block? @else { get => Else; set => Else = value; }

            public IfStatement() : base("unknown") { }
            public IfStatement(Expression condition, List<IRNode> thenBlock, List<IRNode>? elseBlock = null)
                : base(condition.SourceFile)
            {
                Condition = condition;
                ThenBlock = thenBlock;
                ElseBlock = elseBlock;
            }
        }
        
        public class If : IfStatement
        {
            public If() { }
            public If(Expression condition, List<IRNode> thenBlock, List<IRNode>? elseBlock = null)
                : base(condition, thenBlock, elseBlock) { }
        }

        public class While : Statement
        {
            public Expression Condition { get; set; }
            public Block Body { get; set; }
            
            // For property-based initialization
            public Expression condition { get => Condition; set => Condition = value; }
            public Block body { get => Body; set => Body = value; }

            public While() : base("unknown") { }
            public While(Expression condition, Block body, string sourceFile) : base(sourceFile)
            {
                Condition = condition;
                Body = body;
            }
        }

        public class ForStatement : Statement
        {
            public string Iterator { get; set; } = string.Empty;
            public Expression InitialValue { get; set; }
            public Expression LimitValue { get; set; }
            public bool Ascending { get; set; }
            public Block Body { get; set; }

            public ForStatement() : base("unknown") { }
            public ForStatement(string iterator, Expression initial, Expression limit, bool ascending, Block body, string sourceFile)
                : base(sourceFile)
            {
                Iterator = iterator;
                InitialValue = initial;
                LimitValue = limit;
                Ascending = ascending;
                Body = body;
            }
        }

        public class RepeatStatement : Statement
        {
            public Expression Condition { get; set; }
            public Block Body { get; set; }

            public RepeatStatement() : base("unknown") { }
            public RepeatStatement(Expression condition, Block body, string sourceFile) : base(sourceFile)
            {
                Condition = condition;
                Body = body;
            }
        }

        public class ExpressionStatement : Statement
        {
            public Expression Expression { get; set; }

            public ExpressionStatement() : base("unknown") { }
            public ExpressionStatement(Expression expression, string sourceFile) : base(sourceFile)
            {
                Expression = expression;
            }
        }

        public class Literal : Expression
        {
            public object Value { get; set; }
            
            // For property-based initialization
            public object value { get => Value; set => Value = value; }
            public HLType type { get => Type; set => Type = value; }

            public Literal() : base("unknown") { }
            public Literal(object value, HLType type, string sourceFile) : base(sourceFile)
            {
                Value = value;
                Type = type;
            }

            public Literal(string value, HLType type, string sourceFile) : base(sourceFile)
            {
                Value = value;
                Type = type;
            }
        }

        public class Identifier : Expression
        {
            public string Name { get; set; } = string.Empty;

            public Identifier() : base("unknown") { }
            public Identifier(string name, HLType type, string sourceFile) : base(sourceFile)
            {
                Name = name;
                Type = type;
            }

            public Identifier(string name, string sourceFile) : base(sourceFile)
            {
                Name = name;
                Type = new BasicType(sourceFile, "unknown");
            }
        }

        public class FunctionCall : Expression
        {
            public Expression Function { get; }
            public IEnumerable<Expression> Arguments { get; }
            public string FunctionName { get; }

            public FunctionCall(Expression function, IEnumerable<Expression> arguments)
                : base(function.SourceFile)
            {
                Function = function;
                Arguments = arguments;
                FunctionName = (function as Identifier)?.Name ?? string.Empty;
            }

            public FunctionCall(string functionName, IEnumerable<Expression> arguments, string sourceFile) : base(sourceFile)
            {
                FunctionName = functionName;
                Function = new Identifier(functionName, sourceFile);
                Arguments = arguments;
            }
        }

        public class BinaryOp : Expression
        {
            public string Operator { get; set; } = string.Empty;
            public Expression Left { get; set; }
            public Expression Right { get; set; }

            public BinaryOp() : base("unknown") { }
            public BinaryOp(string op, Expression left, Expression right, string sourceFile)
                : base(sourceFile)
            {
                Operator = op;
                Left = left;
                Right = right;
            }

            public BinaryOp(Expression left, string op, Expression right, string sourceFile)
                : base(sourceFile)
            {
                Operator = op;
                Left = left;
                Right = right;
            }

            public BinaryOp(Expression left, string op, Expression right)
                : base(left?.SourceFile ?? "unknown")
            {
                Operator = op;
                Left = left;
                Right = right;
            }
            
            // For property-based initialization where "left" might be assigned to "Left" property
            public Expression left { get => Left; set => Left = value; }
            public Expression right { get => Right; set => Right = value; }
            public string op { get => Operator; set => Operator = value; }
            public string @operator { get => Operator; set => Operator = value; }
        }

        public class UnaryOp : Expression
        {
            public string Operator { get; }
            public Expression Operand { get; }

            public UnaryOp(string op, Expression operand, string sourceFile) : base(sourceFile)
            {
                Operator = op;
                Operand = operand;
            }
        }

        public class ArrayAccess : Expression
        {
            public Expression Array { get; }
            public Expression Index { get; }

            public ArrayAccess(Expression array, Expression index, string sourceFile) : base(sourceFile)
            {
                Array = array;
                Index = index;
            }
        }
    }

    /// <summary>
    /// Base class for all IR nodes
    /// </summary>
    public abstract class IRNode
    {
        public string SourceFile { get; set; } = string.Empty;

        public IRNode() { }
        public IRNode(string sourceFile)
        {
            SourceFile = sourceFile;
        }

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
