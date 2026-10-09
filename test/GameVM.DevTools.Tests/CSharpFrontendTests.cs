using NUnit.Framework;
using GameVM.Compiler.CSharp;

namespace GameVM.DevTools.Tests;

public class CSharpFrontendTests
{
    [Test]
    public void ParseToHlir_WithSyntaxErrors_PopulatesLastParseErrors()
    {
        var frontend = new CSharpFrontend();

        frontend.ParseToHlir("this is not valid C# code {{{");

        Assert.That(frontend.LastParseErrors, Is.Not.Null);
        Assert.That(frontend.LastParseErrors, Is.Not.Empty);
    }

    [Test]
    public void ParseToHlir_WithValidCode_LastParseErrorsIsEmpty()
    {
        var frontend = new CSharpFrontend();

        frontend.ParseToHlir("int x = 42;");

        Assert.That(frontend.LastParseErrors, Is.Not.Null);
        Assert.That(frontend.LastParseErrors, Is.Empty);
    }
}
