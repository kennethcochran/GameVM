using System.Collections.Generic;

namespace GameVM.Compiler.Python.AST
{
    public abstract class AstNode { }
    public class ModuleNode : AstNode
    {
        public List<StatementNode> Body { get; set; }
        public List<TypeIgnoreNode> TypeIgnores { get; set; }
    }
    public class InteractiveNode : AstNode
    {
        public List<StatementNode> Body { get; set; }
    }
    public class ExpressionNode : AstNode
    {
        public Expr Body { get; set; }
    }
    public class FunctionType : AstNode
    {
        public List<Expr> ArgTypes { get; set; }
        public Expr Returns { get; set; }
    }

    public abstract class StatementNode: AstNode { }
    public class FunctionDef : StatementNode
    {
        public string Name { get; set; }
        public Arguments Args { get; set; }
        public List<StatementNode> Body { get; set; }
        public List<Expr> DecoratorList { get; set; }
        public Expr Returns { get; set; }
        public string TypeComment { get; set; }
        public List<TypeParam> TypeParams { get; set; }
    }
    public class AsyncFunctionDef : StatementNode
    {
        public string Name { get; set; }
        public Arguments Args { get; set; }
        public List<StatementNode> Body { get; set; }
        public List<Expr> DecoratorList { get; set; }
        public Expr Returns { get; set; }
        public string TypeComment { get; set; }
        public List<TypeParam> TypeParams { get; set; }
    }
    public class ClassDef : StatementNode
    {
        public string Name { get; set; }
        public List<Expr> Bases { get; set; }
        public List<Keyword> Keywords { get; set; }
        public List<StatementNode> Body { get; set; }
        public List<Expr> DecoratorList { get; set; }
        public List<TypeParam> TypeParams { get; set; }
    }
    public class Return : StatementNode
    {
        public Expr Value { get; set; }
    }
    public class Delete : StatementNode
    {
        public List<Expr> Targets { get; set; }
    }
    public class Assign : StatementNode
    {
        public List<Expr> Targets { get; set; }
        public Expr Value { get; set; }
        public string TypeComment { get; set; }
    }
    public class TypeAlias : StatementNode
    {
        public Expr Name { get; set; }
        public List<TypeParam> TypeParams { get; set; }
        public Expr Value { get; set; }
    }
    public class AugAssign : StatementNode
    {
        public Expr Target { get; set; }
        public Operator Op { get; set; }
        public Expr Value { get; set; }
    }
    public class AnnAssign : StatementNode
    {
        public Expr Target { get; set; }
        public Expr Annotation { get; set; }
        public Expr Value { get; set; }
        public int Simple { get; set; }
    }
    public class ForStatement : StatementNode
    {
        public Expr Target { get; set; }
        public Expr Iter { get; set; }
        public List<StatementNode> Body { get; set; }
        public List<StatementNode> OrElse { get; set; }
        public string TypeComment { get; set; }
    }
    public class AsyncFor : StatementNode
    {
        public Expr Target { get; set; }
        public Expr Iter { get; set; }
        public List<StatementNode> Body { get; set; }
        public List<StatementNode> OrElse { get; set; }
        public string TypeComment { get; set; }
    }
    public class WhileStatement : StatementNode
    {
        public Expr Test { get; set; }
        public List<StatementNode> Body { get; set; }
        public List<StatementNode> OrElse { get; set; }
    }
    public class IfStatement : StatementNode
    {
        public Expr Test { get; set; }
        public List<StatementNode> Body { get; set; }
        public List<StatementNode> OrElse { get; set; }
    }
    public class With : StatementNode
    {
        public List<WithItem> Items { get; set; }
        public List<StatementNode> Body { get; set; }
        public string TypeComment { get; set; }
    }
    public class AsyncWith : StatementNode
    {
        public List<WithItem> Items { get; set; }
        public List<StatementNode> Body { get; set; }
        public string TypeComment { get; set; }
    }
    public class Match : StatementNode
    {
        public Expr Subject { get; set; }
        public List<MatchCase> Cases { get; set; }
    }
    public class Raise : StatementNode
    {
        public Expr Exc { get; set; }
        public Expr Cause { get; set; }
    }
    public class Try : StatementNode
    {
        public List<StatementNode> Body { get; set; }
        public List<ExceptHandler> Handlers { get; set; }
        public List<StatementNode> OrElse { get; set; }
        public List<StatementNode> FinalBody { get; set; }
    }
    public class TryStar : StatementNode
    {
        public List<StatementNode> Body { get; set; }
        public List<ExceptHandler> Handlers { get; set; }
        public List<StatementNode> OrElse { get; set; }
        public List<StatementNode> FinalBody { get; set; }
    }
    public class AssertStatement : StatementNode
    {
        public Expr Test { get; set; }
        public Expr Msg { get; set; }
    }
    public class Import : StatementNode
    {
        public List<Alias> Names { get; set; }
    }
    public class ImportFrom : StatementNode
    {
        public string Module { get; set; }
        public List<Alias> Names { get; set; }
        public int? Level { get; set; }
    }
    public class Global : StatementNode
    {
        public List<string> Names { get; set; }
    }
    public class Nonlocal : StatementNode
    {
        public List<string> Names { get; set; }
    }
    public class ExprStmt : StatementNode
    {
        public Expr Value { get; set; }
    }
    public class Pass : StatementNode { }
    public class Break : StatementNode { }
    public class Continue : StatementNode { }

    public abstract class Expr { }
    public class BoolOp : Expr
    {
        public BoolOpType Op { get; set; }
        public List<Expr> Values { get; set; }
    }
    public class NamedExpr : Expr
    {
        public Expr Target { get; set; }
        public Expr Value { get; set; }
    }
    public class BinOp : Expr
    {
        public Expr Left { get; set; }
        public Operator Op { get; set; }
        public Expr Right { get; set; }
    }
    public class UnaryOp : Expr
    {
        public UnaryOpType Op { get; set; }
        public Expr Operand { get; set; }
    }
    public class Lambda : Expr
    {
        public Arguments Args { get; set; }
        public Expr Body { get; set; }
    }
    public class IfExp : Expr
    {
        public Expr Test { get; set; }
        public Expr Body { get; set; }
        public Expr OrElse { get; set; }
    }
    public class Dict : Expr
    {
        public List<Expr> Keys { get; set; }
        public List<Expr> Values { get; set; }
    }
    public class Set : Expr
    {
        public List<Expr> Elts { get; set; }
    }
    public class ListComp : Expr
    {
        public Expr Elt { get; set; }
        public List<Comprehension> Generators { get; set; }
    }
    public class SetComp : Expr
    {
        public Expr Elt { get; set; }
        public List<Comprehension> Generators { get; set; }
    }
    public class DictComp : Expr
    {
        public Expr Key { get; set; }
        public Expr Value { get; set; }
        public List<Comprehension> Generators { get; set; }
    }
    public class GeneratorExp : Expr
    {
        public Expr Elt { get; set; }
        public List<Comprehension> Generators { get; set; }
    }
    public class Await : Expr
    {
        public Expr Value { get; set; }
    }
    public class Yield : Expr
    {
        public Expr Value { get; set; }
    }
    public class YieldFrom : Expr
    {
        public Expr Value { get; set; }
    }
    public class Compare : Expr
    {
        public Expr Left { get; set; }
        public List<CmpOp> Ops { get; set; }
        public List<Expr> Comparators { get; set; }
    }
    public class Call : Expr
    {
        public Expr Func { get; set; }
        public List<Expr> Args { get; set; }
        public List<Keyword> Keywords { get; set; }
    }
    public class FormattedValue : Expr
    {
        public Expr Value { get; set; }
        public int Conversion { get; set; }
        public Expr FormatSpec { get; set; }
    }
    public class JoinedStr : Expr
    {
        public List<Expr> Values { get; set; }
    }
    public class Constant : Expr
    {
        public object Value { get; set; }
        public string Kind { get; set; }
    }
    public class Attribute : Expr
    {
        public Expr Value { get; set; }
        public string Attr { get; set; }
        public ExprContext Ctx { get; set; }
    }
    public class Subscript : Expr
    {
        public Expr Value { get; set; }
        public Expr Slice { get; set; }
        public ExprContext Ctx { get; set; }
    }
    public class Starred : Expr
    {
        public Expr Value { get; set; }
        public ExprContext Ctx { get; set; }
    }
    public class Name : Expr
    {
        public string Id { get; set; }
        public ExprContext Ctx { get; set; }
    }
    public class List : Expr
    {
        public List<Expr> Elts { get; set; }
        public ExprContext Ctx { get; set; }
    }
    public class Tuple : Expr
    {
        public List<Expr> Elts { get; set; }
        public ExprContext Ctx { get; set; }
    }
    public class Slice : Expr
    {
        public Expr Lower { get; set; }
        public Expr Upper { get; set; }
        public Expr Step { get; set; }
    }

    public enum ExprContext
    {
        Load,
        Store,
        Del
    }

    public enum BoolOpType
    {
        And,
        Or
    }

    public enum Operator
    {
        Add,
        Sub,
        Mult,
        MatMult,
        Div,
        Mod,
        Pow,
        LShift,
        RShift,
        BitOr,
        BitXor,
        BitAnd,
        FloorDiv
    }

    public enum UnaryOpType
    {
        Invert,
        Not,
        UAdd,
        USub
    }

    public enum CmpOp
    {
        Eq,
        NotEq,
        Lt,
        LtE,
        Gt,
        GtE,
        Is,
        IsNot,
        In,
        NotIn
    }

    public class Comprehension
    {
        public Expr Target { get; set; }
        public Expr Iter { get; set; }
        public List<Expr> Ifs { get; set; }
        public int IsAsync { get; set; }
    }

    public class ExceptHandler
    {
        public Expr Type { get; set; }
        public string Name { get; set; }
        public List<StatementNode> Body { get; set; }
        public int Lineno { get; set; }
        public int ColOffset { get; set; }
        public int? EndLineno { get; set; }
        public int? EndColOffset { get; set; }
    }

    public class Arguments
    {
        public List<Arg> PosOnlyArgs { get; set; }
        public List<Arg> Args { get; set; }
        public Arg VarArg { get; set; }
        public List<Arg> KwOnlyArgs { get; set; }
        public List<Expr> KwDefaults { get; set; }
        public Arg KwArg { get; set; }
        public List<Expr> Defaults { get; set; }
    }

    public class Arg
    {
        public string Argument { get; set; }
        public Expr Annotation { get; set; }
        public string TypeComment { get; set; }
        public int Lineno { get; set; }
        public int ColOffset { get; set; }
        public int? EndLineno { get; set; }
        public int? EndColOffset { get; set; }
    }

    public class Keyword
    {
        public string Arg { get; set; }
        public Expr Value { get; set; }
        public int Lineno { get; set; }
        public int ColOffset { get; set; }
        public int? EndLineno { get; set; }
        public int? EndColOffset { get; set; }
    }

    public class Alias
    {
        public string Name { get; set; }
        public string AsName { get; set; }
        public int Lineno { get; set; }
        public int ColOffset { get; set; }
        public int? EndLineno { get; set; }
        public int? EndColOffset { get; set; }
    }

    public class WithItem
    {
        public Expr ContextExpr { get; set; }
        public Expr OptionalVars { get; set; }
    }

    public class MatchCase
    {
        public Pattern Pattern { get; set; }
        public Expr Guard { get; set; }
        public List<StatementNode> Body { get; set; }
    }

    public abstract class Pattern { }
    public class MatchValue : Pattern
    {
        public Expr Value { get; set; }
    }
    public class MatchSingleton : Pattern
    {
        public object Value { get; set; }
    }
    public class MatchSequence : Pattern
    {
        public List<Pattern> Patterns { get; set; }
    }
    public class MatchMapping : Pattern
    {
        public List<Expr> Keys { get; set; }
        public List<Pattern> Patterns { get; set; }
        public string Rest { get; set; }
    }
    public class MatchClass : Pattern
    {
        public Expr Cls { get; set; }
        public List<Pattern> Patterns { get; set; }
        public List<string> KwdAttrs { get; set; }
        public List<Pattern> KwdPatterns { get; set; }
    }
    public class MatchStar : Pattern
    {
        public string Name { get; set; }
    }
    public class MatchAs : Pattern
    {
        public Pattern Pattern { get; set; }
        public string Name { get; set; }
    }
    public class MatchOr : Pattern
    {
        public List<Pattern> Patterns { get; set; }
    }

    public class TypeIgnoreNode
    {
        public int Lineno { get; set; }
        public string Tag { get; set; }
    }

    public abstract class TypeParam { }
    public class TypeVar : TypeParam
    {
        public string Name { get; set; }
        public Expr Bound { get; set; }
        public Expr DefaultValue { get; set; }
    }
    public class ParamSpec : TypeParam
    {
        public string Name { get; set; }
        public Expr DefaultValue { get; set; }
    }
    public class TypeVarTuple : TypeParam
    {
        public string Name { get; set; }
        public Expr DefaultValue { get; set; }
    }
}
