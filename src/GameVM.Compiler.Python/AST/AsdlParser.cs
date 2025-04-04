using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

public static class AsdlParser
{
    public static readonly HashSet<string> BuiltinTypes = new HashSet<string> { "identifier", "string", "int", "constant" };

    public static Module Parse(string filename)
    {
        var parser = new ASDLParser();
        return parser.Parse(File.ReadAllText(filename));
    }
}

public abstract class AST
{
    public abstract override string ToString();
}

public class Module : AST
{
    public string Name { get; }
    public List<Type> Definitions { get; }
    public Dictionary<string, AST> Types { get; }

    public Module(string name, List<Type> definitions)
    {
        Name = name;
        Definitions = definitions;
        Types = definitions.ToDictionary(d => d.Name, d => d.Value);
    }

    public override string ToString() => $"Module({Name}, {Definitions})";
}

public class Type : AST
{
    public string Name { get; }
    public AST Value { get; }

    public Type(string name, AST value)
    {
        Name = name;
        Value = value;
    }

    public override string ToString() => $"Type({Name}, {Value})";
}

public class Constructor : AST
{
    public string Name { get; }
    public List<Field> Fields { get; }

    public Constructor(string name, List<Field> fields = null)
    {
        Name = name;
        Fields = fields ?? new List<Field>();
    }

    public override string ToString() => $"Constructor({Name}, {Fields})";
}

public class Field : AST
{
    public string Type { get; }
    public string Name { get; }
    public bool IsSequence { get; }
    public bool IsOptional { get; }

    public Field(string type, string name = null, bool isSequence = false, bool isOptional = false)
    {
        Type = type;
        Name = name;
        IsSequence = isSequence;
        IsOptional = isOptional;
    }

    public override string ToString()
    {
        var extra = IsSequence ? "*" : IsOptional ? "?" : "";
        return $"{Type}{extra} {Name}";
    }
}

public class Sum : AST
{
    public List<Constructor> Types { get; }
    public List<Field> Attributes { get; }

    public Sum(List<Constructor> types, List<Field> attributes = null)
    {
        Types = types;
        Attributes = attributes ?? new List<Field>();
    }

    public override string ToString() => Attributes.Any() ? $"Sum({Types}, {Attributes})" : $"Sum({Types})";
}

public class Product : AST
{
    public List<Field> Fields { get; }
    public List<Field> Attributes { get; }

    public Product(List<Field> fields, List<Field> attributes = null)
    {
        Fields = fields;
        Attributes = attributes ?? new List<Field>();
    }

    public override string ToString() => Attributes.Any() ? $"Product({Fields}, {Attributes})" : $"Product({Fields})";
}

public abstract class VisitorBase
{

    private readonly ConcurrentDictionary<System.Type, Delegate> _cache = new ConcurrentDictionary<System.Type, Delegate>();

public void Visit(AST obj, params object[] args)
{
    System.Type klass = obj.GetType();
    var methname = "Visit" + klass.Name;
    var meth = (Action<AST, object[]>)_cache.GetOrAdd(klass, t => (Action<AST, object[]>)Delegate.CreateDelegate(typeof(Action<AST, object[]>), this, methname, false));
    meth?.Invoke(obj, args);
}
}

public class Check : VisitorBase
{
    private readonly Dictionary<string, string> _constructors = new Dictionary<string, string>();
    private readonly Dictionary<string, List<string>> _types = new Dictionary<string, List<string>>();
    public int Errors { get; private set; }

    public void VisitModule(Module mod)
    {
        foreach (var dfn in mod.Definitions)
        {
            Visit(dfn);
        }
    }

    public void VisitType(Type type)
    {
        Visit(type.Value, type.Name);
    }

    public void VisitSum(Sum sum, string name)
    {
        foreach (var t in sum.Types)
        {
            Visit(t, name);
        }
    }

    public void VisitConstructor(Constructor cons, string name)
    {
        var key = cons.Name;
        if (_constructors.TryGetValue(key, out var conflict))
        {
            Console.WriteLine($"Redefinition of constructor {key}");
            Console.WriteLine($"Defined in {conflict} and {name}");
            Errors++;
        }
        else
        {
            _constructors[key] = name;
        }

        foreach (var f in cons.Fields)
        {
            Visit(f, key);
        }
    }

    public void VisitField(Field field, string name)
    {
        var key = field.Type;
        if (!_types.TryGetValue(key, out var list))
        {
            list = new List<string>();
            _types[key] = list;
        }
        list.Add(name);
    }

    public void VisitProduct(Product prod, string name)
    {
        foreach (var f in prod.Fields)
        {
            Visit(f, name);
        }
    }

    public static bool CheckModule(Module mod)
    {
        var checker = new Check();
        checker.Visit(mod);

        foreach (var t in checker._types.Keys)
        {
            if (!mod.Types.ContainsKey(t) && !AsdlParser.BuiltinTypes.Contains(t))
            {
                checker.Errors++;
                var uses = string.Join(", ", checker._types[t]);
                Console.WriteLine($"Undefined type {t}, used in {uses}");
            }
        }
        return checker.Errors == 0;
    }
}

public class ASDLParser
{
    private IEnumerator<Token> _tokenizer;
    private Token _curToken;

    private readonly TokenKind[] IdKinds = { TokenKind.ConstructorId, TokenKind.TypeId };

    public static readonly Dictionary<string, TokenKind> OperatorTable = new Dictionary<string, TokenKind>
    {
        { "=", TokenKind.Equals },
        { ",", TokenKind.Comma },
        { "?", TokenKind.Question },
        { "|", TokenKind.Pipe },
        { "(", TokenKind.LParen },
        { ")", TokenKind.RParen },
        { "*", TokenKind.Asterisk },
        { "{", TokenKind.LBrace },
        { "}", TokenKind.RBrace }
    };

    public Module Parse(string buf)
    {
        _tokenizer = TokenizeAsdl(buf).GetEnumerator();
        Advance();
        return ParseModule();
    }

    private Module ParseModule()
    {
        if (AtKeyword("module"))
        {
            Advance();
        }
        else
        {
            throw new ASDLSyntaxError($"Expected \"module\" (found {_curToken.Value})", _curToken.LineNumber);
        }

        var name = Match(this.IdKinds);
        Match(TokenKind.LBrace);
        var defs = ParseDefinitions();
        Match(TokenKind.RBrace);
        return new Module(name, defs);
    }

    private List<Type> ParseDefinitions()
    {
        var defs = new List<Type>();
        while (_curToken.Kind == TokenKind.TypeId)
        {
            var typename = Advance();
            Match(TokenKind.Equals);
            var type = ParseType();
            defs.Add(new Type(typename, type));
        }
        return defs;
    }

    private AST ParseType()
    {
        if (_curToken.Kind == TokenKind.LParen)
        {
            return ParseProduct();
        }
        else
        {
            var sumlist = new List<Constructor> { new Constructor(Match(TokenKind.ConstructorId), ParseOptionalFields()) };
            while (_curToken.Kind == TokenKind.Pipe)
            {
                Advance();
                sumlist.Add(new Constructor(Match(TokenKind.ConstructorId), ParseOptionalFields()));
            }
            return new Sum(sumlist, ParseOptionalAttributes());
        }
    }

    private Product ParseProduct()
    {
        return new Product(ParseFields(), ParseOptionalAttributes());
    }

    private List<Field> ParseFields()
    {
        var fields = new List<Field>();
        Match(TokenKind.LParen);
        while (_curToken.Kind == TokenKind.TypeId)
        {
            var typename = Advance();
            var (isSeq, isOpt) = ParseOptionalFieldQuantifier();
            var id = _curToken.Kind.In(this.IdKinds) ? Advance() : null;
            fields.Add(new Field(typename, id, isSeq, isOpt));
            if (_curToken.Kind == TokenKind.RParen)
            {
                break;
            }
            else if (_curToken.Kind == TokenKind.Comma)
            {
                Advance();
            }
        }
        Match(TokenKind.RParen);
        return fields;
    }

    private List<Field> ParseOptionalFields()
    {
        return _curToken.Kind == TokenKind.LParen ? ParseFields() : null;
    }

    private List<Field> ParseOptionalAttributes()
    {
        return AtKeyword("attributes") ? ParseFields() : null;
    }

    private (bool isSeq, bool isOpt) ParseOptionalFieldQuantifier()
    {
        var isSeq = false;
        var isOpt = false;
        if (_curToken.Kind == TokenKind.Asterisk)
        {
            isSeq = true;
            Advance();
        }
        else if (_curToken.Kind == TokenKind.Question)
        {
            isOpt = true;
            Advance();
        }
        return (isSeq, isOpt);
    }

    private string Advance()
    {
        var curVal = _curToken?.Value;
        _curToken = _tokenizer.MoveNext() ? _tokenizer.Current : null;
        return curVal;
    }

    private string Match(params TokenKind[] kinds)
    {
        if (kinds.Contains(_curToken.Kind))
        {
            var value = _curToken.Value;
            Advance();
            return value;
        }
        else
        {
            throw new ASDLSyntaxError($"Unmatched {string.Join(", ", kinds)} (found {_curToken.Kind})", _curToken.LineNumber);
        }
    }

    private bool AtKeyword(string keyword)
    {
        return _curToken.Kind == TokenKind.TypeId && _curToken.Value == keyword;
    }

    private static IEnumerable<Token> TokenizeAsdl(string buf)
    {
        var lines = buf.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        for (var lineno = 0; lineno < lines.Length; lineno++)
        {
            var line = lines[lineno];
            foreach (Match m in Regex.Matches(line, @"\s*(\w+|--.*|.)"))
            {
                var c = m.Groups[1].Value;
                if (char.IsLetter(c[0]))
                {
                    yield return new Token(char.IsUpper(c[0]) ? TokenKind.ConstructorId : TokenKind.TypeId, c, lineno + 1);
                }
                else if (c.StartsWith("--"))
                {
                    break;
                }
                else
                {
                    if (!OperatorTable.TryGetValue(c, out var opKind))
                    {
                        throw new ASDLSyntaxError($"Invalid operator {c}", lineno + 1);
                    }
                    yield return new Token(opKind, c, lineno + 1);
                }
            }
        }
    }
}

public class Token
{
    public TokenKind Kind { get; }
    public string Value { get; }
    public int LineNumber { get; }

    public Token(TokenKind kind, string value, int lineNumber)
    {
        Kind = kind;
        Value = value;
        LineNumber = lineNumber;
    }
}

public enum TokenKind
{
    ConstructorId,
    TypeId,
    Equals,
    Comma,
    Question,
    Pipe,
    Asterisk,
    LParen,
    RParen,
    LBrace,
    RBrace
}

public static class TokenKindExtensions
{

    public static readonly TokenKind[] IdKinds = { TokenKind.ConstructorId, TokenKind.TypeId };

    public static bool In(this TokenKind kind, params TokenKind[] kinds)
    {
        return kinds.Contains(kind);
    }
}

public class ASDLSyntaxError : Exception
{
    public int LineNumber { get; }

    public ASDLSyntaxError(string message, int lineNumber) : base($"Syntax error on line {lineNumber}: {message}")
    {
        LineNumber = lineNumber;
    }
}
