using System.Text;

namespace GameVM.Compiler.Pascal.Tests.Fixtures;

/// <summary>
/// Builder for creating Pascal program source code in tests.
/// Simplifies program construction for test scenarios.
/// </summary>
public class PascalProgramBuilder
{
    private readonly StringBuilder _code = new();
    private bool _hasProgram = false;
    private bool _hasVar = false;
    private bool _hasBegin = false;

    public PascalProgramBuilder Program(string name = "Test")
    {
        _code.AppendLine($"program {name};");
        _hasProgram = true;
        return this;
    }

    public PascalProgramBuilder Var()
    {
        if (!_hasProgram)
            Program();
        _code.AppendLine("var");
        _hasVar = true;
        return this;
    }

    public PascalProgramBuilder Variable(string name, string type)
    {
        if (!_hasVar)
            Var();
        _code.AppendLine($"  {name}: {type};");
        return this;
    }

    public PascalProgramBuilder Variables(params (string name, string type)[] variables)
    {
        if (!_hasVar)
            Var();
        foreach (var (name, type) in variables)
        {
            _code.AppendLine($"  {name}: {type};");
        }
        return this;
    }

    public PascalProgramBuilder Begin()
    {
        if (!_hasProgram)
            Program();
        _code.AppendLine("begin");
        _hasBegin = true;
        return this;
    }

    public PascalProgramBuilder Statement(string statement)
    {
        if (!_hasBegin)
            Begin();
        _code.AppendLine($"  {statement};");
        return this;
    }

    public PascalProgramBuilder Statements(params string[] statements)
    {
        foreach (var stmt in statements)
        {
            Statement(stmt);
        }
        return this;
    }

    public PascalProgramBuilder End()
    {
        if (!_hasBegin)
            Begin();
        _code.AppendLine("end.");
        return this;
    }

    public PascalProgramBuilder Write(string value)
    {
        return Statement($"write('{value}')");
    }

    public PascalProgramBuilder Writeln(string value)
    {
        return Statement($"writeln('{value}')");
    }

    public PascalProgramBuilder Assignment(string variable, string value)
    {
        return Statement($"{variable} := {value}");
    }

    public PascalProgramBuilder IfThen(string condition, string thenStatement)
    {
        if (!_hasBegin)
            Begin();
        _code.AppendLine($"  if {condition} then");
        _code.AppendLine($"    {thenStatement};");
        return this;
    }

    public PascalProgramBuilder IfThenElse(string condition, string thenStatement, string elseStatement)
    {
        if (!_hasBegin)
            Begin();
        _code.AppendLine($"  if {condition} then");
        _code.AppendLine($"    {thenStatement}");
        _code.AppendLine($"  else");
        _code.AppendLine($"    {elseStatement};");
        return this;
    }

    public PascalProgramBuilder WhileDo(string condition, params string[] statements)
    {
        if (!_hasBegin)
            Begin();
        _code.AppendLine($"  while {condition} do");
        _code.AppendLine("  begin");
        foreach (var stmt in statements)
        {
            _code.AppendLine($"    {stmt};");
        }
        _code.AppendLine("  end;");
        return this;
    }

    public PascalProgramBuilder ForTo(string variable, string start, string end, params string[] statements)
    {
        if (!_hasBegin)
            Begin();
        _code.AppendLine($"  for {variable} := {start} to {end} do");
        if (statements.Length == 1)
        {
            _code.AppendLine($"    {statements[0]};");
        }
        else
        {
            _code.AppendLine("  begin");
            foreach (var stmt in statements)
            {
                _code.AppendLine($"    {stmt};");
            }
            _code.AppendLine("  end;");
        }
        return this;
    }

    public PascalProgramBuilder Function(string name, string returnType, params (string name, string type)[] parameters)
    {
        if (!_hasProgram)
            Program();
        _code.Append($"function {name}");
        if (parameters.Length > 0)
        {
            _code.Append("(");
            var paramList = new List<string>();
            foreach (var (paramName, paramType) in parameters)
            {
                paramList.Add($"{paramName}: {paramType}");
            }
            _code.Append(string.Join("; ", paramList));
            _code.Append(")");
        }
        _code.AppendLine($": {returnType};");
        _code.AppendLine("begin");
        _code.AppendLine($"  {name} := 0;");
        _code.AppendLine("end;");
        return this;
    }

    public PascalProgramBuilder Procedure(string name, params (string name, string type)[] parameters)
    {
        if (!_hasProgram)
            Program();
        _code.Append($"procedure {name}");
        if (parameters.Length > 0)
        {
            _code.Append("(");
            var paramList = new List<string>();
            foreach (var (paramName, paramType) in parameters)
            {
                paramList.Add($"{paramName}: {paramType}");
            }
            _code.Append(string.Join("; ", paramList));
            _code.Append(")");
        }
        _code.AppendLine(";");
        _code.AppendLine("begin");
        _code.AppendLine("end;");
        return this;
    }

    public string Build()
    {
        if (!_hasBegin)
            Begin();
        if (!_code.ToString().EndsWith("end."))
            End();
        return _code.ToString();
    }

    public override string ToString() => Build();

    // Static factory methods for common patterns
    public static string SimpleProgram(string statement)
    {
        return new PascalProgramBuilder()
            .Program()
            .Begin()
            .Statement(statement)
            .End()
            .Build();
    }

    public static string ProgramWithVariable(string varName, string varType, string statement)
    {
        return new PascalProgramBuilder()
            .Program()
            .Variable(varName, varType)
            .Begin()
            .Statement(statement)
            .End()
            .Build();
    }

    public static string HelloWorld()
    {
        return new PascalProgramBuilder()
            .Program("HelloWorld")
            .Begin()
            .Writeln("Hello, World!")
            .End()
            .Build();
    }
}

