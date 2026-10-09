using System.Xml.Linq;

namespace GameVM.CrapGate.Core;

/// <summary>
/// Parses dotnet-coverage XML output into per-method line coverage fractions.
/// Keys are "TypeName.MethodName" (compiler-generated methods excluded).
/// </summary>
public static class CoverageParser
{
    public static Dictionary<string, double> ParseDotNetCoverage(string xmlPath)
    {
        var result = new Dictionary<string, double>();
        var doc = XDocument.Load(xmlPath);
        foreach (var func in doc.Descendants("function"))
        {
            var typeName = func.Attribute("type_name")?.Value ?? "";
            var methodName = func.Attribute("name")?.Value ?? "";
            var paren = methodName.IndexOf('(');
            var simpleName = paren >= 0 ? methodName[..paren] : methodName;
            simpleName = System.Net.WebUtility.HtmlDecode(simpleName);
            if (simpleName.Contains('<') || simpleName.Contains('$'))
                continue;
            var key = $"{typeName}.{simpleName}";
            if (double.TryParse(func.Attribute("line_coverage")?.Value, out var pct))
                result[key] = pct / 100.0;
        }
        return result;
    }
}
