using GameVM.CrapGate.Core;
using NUnit.Framework;

namespace GameVM.DevTools.Tests;

[TestFixture]
public class CoverageParserTests
{
    private static string WriteTempXml(string content)
    {
        var path = Path.Combine(Path.GetTempPath(), $"coverage-{Guid.NewGuid()}.xml");
        File.WriteAllText(path, content);
        return path;
    }

    [Test]
    public void Parse_SingleFunction_ReturnsCoverageFraction()
    {
        var path = WriteTempXml(@"<?xml version=""1.0""?>
<coverage>
  <function name=""DoWork()"" type_name=""MyClass"" line_coverage=""75.0"" />
</coverage>");
        try
        {
            var result = CoverageParser.ParseDotNetCoverage(path);
            Assert.That(result["MyClass.DoWork"], Is.EqualTo(0.75).Within(0.001));
        }
        finally { File.Delete(path); }
    }

    [Test]
    public void Parse_MultipleFunctions_ReturnsAll()
    {
        var path = WriteTempXml(@"<?xml version=""1.0""?>
<coverage>
  <function name=""A()"" type_name=""T1"" line_coverage=""100.0"" />
  <function name=""B(int)"" type_name=""T2"" line_coverage=""50.0"" />
</coverage>");
        try
        {
            var result = CoverageParser.ParseDotNetCoverage(path);
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result["T1.A"], Is.EqualTo(1.0));
            Assert.That(result["T2.B"], Is.EqualTo(0.5).Within(0.001));
        }
        finally { File.Delete(path); }
    }

    [Test]
    public void Parse_CompilerGeneratedMethods_AreSkipped()
    {
        var path = WriteTempXml(@"<?xml version=""1.0""?>
<coverage>
  <function name=""&lt;DoWork&gt;d__1.MoveNext()"" type_name=""MyClass"" line_coverage=""80.0"" />
  <function name=""DoWork()"" type_name=""MyClass"" line_coverage=""90.0"" />
</coverage>");
        try
        {
            var result = CoverageParser.ParseDotNetCoverage(path);
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result.ContainsKey("MyClass.DoWork"), Is.True);
        }
        finally { File.Delete(path); }
    }

    [Test]
    public void Parse_EmptyCoverage_ReturnsEmpty()
    {
        var path = WriteTempXml(@"<?xml version=""1.0""?><coverage></coverage>");
        try
        {
            var result = CoverageParser.ParseDotNetCoverage(path);
            Assert.That(result, Is.Empty);
        }
        finally { File.Delete(path); }
    }
}
