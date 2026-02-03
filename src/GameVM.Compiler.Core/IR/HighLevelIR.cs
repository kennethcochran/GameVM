using System.Collections.Generic;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
using GameVM.Compiler.Core.Enums;

namespace GameVM.Compiler.Core.IR
{
    /// <summary>
    /// High-level intermediate representation of source code.
    /// This is the first IR level after parsing, closest to the source language.
    /// </summary>
    public class HighLevelIR : IIntermediateRepresentation
    {
        private const string UnknownSourceFile = "unknown";
        private const string UnknownType = "unknown";

        /// <summary>
        /// The source file this IR was generated from
        /// </summary>
        public string SourceFile { get; set; } = string.Empty;

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
        public List<HlModule> Modules { get; set; } = new();

        /// <summary>
        /// Type definitions (kept for backward compatibility)
        /// </summary>
        public Dictionary<string, IRType> GlobalTypes { get; set; } = new();

        public Dictionary<string, IRType> Types { get => GlobalTypes; set => GlobalTypes = value; }

        /// <summary>
        /// Global functions (kept for backward compatibility)
        /// </summary>
        public Dictionary<string, Function> GlobalFunctions { get; set; } = new();

        public class Variable : IRSymbol
        {
            public new string SourceFile { get; set; } = string.Empty;
            public new required HlType Type { get; set; }

            public Variable() { }
            
            [SetsRequiredMembers]
            public Variable(string name, HlType type, string sourceFile)
            {
                Name = name;
                Type = type;
                SourceFile = sourceFile;
            }
        }

        // Nested types for High Level IR
        public class HlType : IRType
        {
            public new string SourceFile { get; set; } = string.Empty;

            public HlType() { }
            [SetsRequiredMembers]
            public HlType(string sourceFile, string name)
            {
                SourceFile = sourceFile;
                Name = name;
            }

            [SetsRequiredMembers]
            public HlType(string name) : this(UnknownSourceFile, name) { }

            public static implicit operator HlType(string name) => new HlType(UnknownSourceFile, name);
        }

        public class BasicType : HlType
        {
            [SetsRequiredMembers]
            public BasicType(string sourceFile, string name) : base(sourceFile, name) { }
        }

        public sealed class Module : IRNode
        {
            public new string SourceFile { get; }
            public string Name { get; }
            private readonly List<Function> _functions = new();
            public IReadOnlyList<Function> Functions => _functions;

            public Module(string sourceFile, string name)
            {
                SourceFile = sourceFile;
                Name = name;
            }

            public void AddFunction(Function function)
            {
                _functions.Add(function);
            }
        }

        public class Block : Statement
        {
            public List<Statement> Statements { get; set; } = new();

            public Block() : base(UnknownSourceFile) { }
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

            public Statement() : base(UnknownSourceFile) { }
            public Statement(string sourceFile)
            {
                SourceFile = sourceFile;
            }
        }

        public class Function : IRFunction
        {
            public new string SourceFile { get; set; } = string.Empty;
            public new HlType ReturnType { get => (HlType)base.ReturnType; set => base.ReturnType = value; }
            public new Block Body { get => (Block)base.Body[0]; set { base.Body.Clear(); base.Body.Add(value); } }
            public new List<Parameter> Parameters { get; set; } = new();

            /// <summary>
            /// The minimum capability level required by this function.
            /// </summary>
            public CapabilityLevel RequiredLevel { get; set; } = CapabilityLevel.L1;

            /// <summary>
            /// The hardware extension (if any) required by this function.
            /// </summary>
            public string? RequiredExtensionId { get; set; }

            public Function()
            {
                base.Body.Add(new Block(UnknownSourceFile));
            }

            public Function(string sourceFile, string name, HlType returnType, Block body)
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
                ReturnType = new BasicType(sourceFile, returnTypeName) { Name = returnTypeName };
                Body = body;
            }

            public void AddParameter(Parameter parameter)
            {
                Parameters.Add(parameter);
            }
        }

        public sealed class Parameter : IRParameter
        {
            public new string Name { get; }
            public new HlType Type { get; }
            public new string SourceFile { get; }

            public Parameter(string name, HlType type, string sourceFile)
            {
                Name = name;
                Type = type;
                SourceFile = sourceFile;
            }

            public Parameter(string name, string typeName, string sourceFile)
            {
                Name = name;
                Type = new BasicType(sourceFile, typeName) { Name = typeName };
                SourceFile = sourceFile;
            }

            public Parameter(string name, string typeName) : this(name, typeName, UnknownSourceFile) { }
        }

        public class Expression : IRNode
        {
            public new string SourceFile { get => base.SourceFile; set => base.SourceFile = value; }
            public HlType Type { get; set; } = new HlType() { Name = UnknownType };

            public Expression() : base(UnknownSourceFile) { }
            public Expression(string sourceFile) : base(sourceFile)
            {
            }
        }

        public class Assignment : Statement
        {
            public string Target { get; set; } = string.Empty;
            public Expression Value { get; set; } = new Expression();

            public Assignment() : base(UnknownSourceFile) { }
            public Assignment(string target, Expression value, string sourceFile) : base(sourceFile)
            {
                Target = target;
                Value = value;
            }

            public Assignment(string target, Expression value) : this(target, value, UnknownSourceFile) { }

            public Assignment(Expression target, Expression value, string sourceFile) : base(sourceFile)
            {
                Target = target is Identifier identifier ? identifier.Name : target.ToString() ?? string.Empty;
                Value = value;
            }
        }

        public class Return : Statement
        {
            public Expression? Value { get; set; }

            public Return() : base(UnknownSourceFile) { }
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
            public Expression Condition { get; set; } = new Expression();
            private List<IRNode> _thenBlock = new();
            private List<IRNode>? _elseBlock;

            public List<IRNode> GetThenBlock() => _thenBlock;
            public List<IRNode>? GetElseBlock() => _elseBlock;
            public void SetThenBlock(List<IRNode> block) => _thenBlock = block;
            public void SetElseBlock(List<IRNode>? block) => _elseBlock = block;

            public Block GetThenBlockAsBlock() => new Block(SourceFile, _thenBlock.Cast<Statement>().ToList());
            public Block? GetElseBlockAsBlock() => _elseBlock == null ? null : new Block(SourceFile, _elseBlock.Cast<Statement>().ToList());
            public void SetThenBlockFromBlock(Block block) => _thenBlock = block.Statements.Cast<IRNode>().ToList();
            public void SetElseBlockFromBlock(Block? block) => _elseBlock = block?.Statements.Cast<IRNode>().ToList();

            public IfStatement() : base(UnknownSourceFile) { }
            public IfStatement(Expression condition, List<IRNode> thenBlock, List<IRNode>? elseBlock = null)
                : base(condition.SourceFile)
            {
                Condition = condition;
                SetThenBlock(thenBlock);
                SetElseBlock(elseBlock);
            }
        }

        public class While : Statement
        {
            public required Expression Condition { get; set; }
            public required Block Body { get; set; }

            public While() : base(UnknownSourceFile) { }
        }

        public class ForStatement : Statement
        {
            public string Iterator { get; set; } = string.Empty;
            public Expression InitialValue { get; set; } = new Expression();
            public Expression LimitValue { get; set; } = new Expression();
            public bool Ascending { get; set; }
            public Block Body { get; set; } = new Block();

            public ForStatement() : base(UnknownSourceFile) { }
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
            public Expression Condition { get; set; } = new Expression();
            public Block Body { get; set; } = new Block();

            public RepeatStatement() : base(UnknownSourceFile) { }
            public RepeatStatement(Expression condition, Block body, string sourceFile) : base(sourceFile)
            {
                Condition = condition;
                Body = body;
            }
        }

        public class ExpressionStatement : Statement
        {
            public required Expression Expression { get; set; }

            public ExpressionStatement() : base(UnknownSourceFile) { }
        }

        public class Literal : Expression
        {
            public object Value { get; set; } = string.Empty;
            public new HlType Type { get; set; } = new HlType() { Name = UnknownType };

            public Literal() : base(UnknownSourceFile) { }
            public Literal(object value, HlType type, string sourceFile) : base(sourceFile)
            {
                Value = value;
                Type = type;
            }

            public Literal(string value, HlType type, string sourceFile) : base(sourceFile)
            {
                Value = value;
                Type = type;
            }
        }

        public class Identifier : Expression
        {
            public string Name { get; set; } = string.Empty;

            public Identifier() : base(UnknownSourceFile) { }
            public Identifier(string name, HlType type, string sourceFile) : base(sourceFile)
            {
                Name = name;
                Type = type;
            }

            public Identifier(string name, string sourceFile) : base(sourceFile)
            {
                Name = name;
                Type = new BasicType(sourceFile, UnknownSourceFile) { Name = UnknownSourceFile };
            }
        }

        public class FunctionCall : Expression
        {
            public Expression CallTarget { get; }
            public IEnumerable<Expression> Arguments { get; }
            public string FunctionName { get; }

            public FunctionCall(Expression function, IEnumerable<Expression> arguments)
                : base(function.SourceFile)
            {
                CallTarget = function;
                Arguments = arguments;
                FunctionName = (function as Identifier)?.Name ?? string.Empty;
            }
        }

        public class BinaryOp : Expression
        {
            public string Operator { get; set; } = string.Empty;
            public Expression Left { get; set; } = new Expression();
            public Expression Right { get; set; } = new Expression();

            public BinaryOp() : base(UnknownSourceFile) { }
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
                : base(left?.SourceFile ?? UnknownSourceFile)
            {
                Operator = op;
                Left = left ?? new Expression();
                Right = right ?? new Expression();
            }
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
    /// Represents a module in the high-level IR
    /// </summary>
    public class HlModule
    {
        /// <summary>
        /// The name of the module
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Functions defined in this module
        /// </summary>
        public List<HighLevelIR.Function> Functions { get; set; } = new();

        /// <summary>
        /// Type definitions in this module
        /// </summary>
        public List<HighLevelIR.HlType> Types { get; set; } = new();

        /// <summary>
        /// Global variables in this module
        /// </summary>
        public List<HighLevelIR.Variable> Variables { get; set; } = new();
    }
}
