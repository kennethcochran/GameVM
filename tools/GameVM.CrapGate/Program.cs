using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

// CRAP score quality gate.
// Computes CRAP = complexity^2 * (1 - coverage)^3 + complexity for each method
// and fails if any method exceeds the threshold.
//
// Usage: GameVM.CrapGate <coverage.xml> <srcDir> [threshold]
//   coverage.xml : dotnet-coverage XML output (from `dotnet-coverage collect`)
//   srcDir       : root of C# source files to analyze for complexity
//   threshold    : max allowed CRAP score (default 30)

if (args.Length < 2)
{
    Console.WriteLine("Usage: GameVM.CrapGate <coverage.xml> <srcDir> [threshold]");
    return 1;
}

var coveragePath = args[0];
var srcDir = args[1];
var threshold = args.Length > 2 ? int.Parse(args[2]) : 30;

Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("🔍 Checking CRAP Score Quality Gate...");
Console.ResetColor();
Console.WriteLine($"📊 Coverage: {coveragePath} | 📁 Source: {srcDir} | 🎯 Threshold: {threshold}");

// 1. Parse coverage: "TypeName.MethodName" -> line coverage fraction
var coverage = new Dictionary<string, double>();
var covDoc = XDocument.Load(coveragePath);
foreach (var func in covDoc.Descendants("function"))
{
    var typeName = func.Attribute("type_name")?.Value ?? "";
    var methodName = func.Attribute("name")?.Value ?? "";
    var paren = methodName.IndexOf('(');
    var simpleName = paren >= 0 ? methodName[..paren] : methodName;
    simpleName = System.Net.WebUtility.HtmlDecode(simpleName);
    // Skip compiler-generated methods
    if (simpleName.Contains('<') || simpleName.Contains('$'))
        continue;
    var key = $"{typeName}.{simpleName}";
    if (double.TryParse(func.Attribute("line_coverage")?.Value, out var pct))
        coverage[key] = pct / 100.0;
}

// 2. Compute cyclomatic complexity per method via Roslyn
var complexities = new Dictionary<string, int>();
var csFiles = Directory.GetFiles(srcDir, "*.cs", SearchOption.AllDirectories)
    .Where(f => !f.Contains("/obj/") && !f.Contains("/bin/") && !f.Contains("/ANTLR/"))
    .ToList();

foreach (var file in csFiles)
{
    var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(file));
    var root = tree.GetRoot();
    foreach (var classDecl in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
    {
        var className = classDecl.Identifier.Text;
        foreach (var method in classDecl.DescendantNodes().OfType<MethodDeclarationSyntax>())
        {
            var methodName = method.Identifier.Text;
            var complexity = 1;
            complexity += method.DescendantNodes().Count(n =>
                n is IfStatementSyntax ||
                n is WhileStatementSyntax ||
                n is ForStatementSyntax ||
                n is ForEachStatementSyntax ||
                n is CaseSwitchLabelSyntax ||
                n is CatchClauseSyntax ||
                n is ConditionalExpressionSyntax ||
                n is SwitchExpressionArmSyntax);
            complexity += method.DescendantTokens().Count(t =>
                t.IsKind(SyntaxKind.AmpersandAmpersandToken) ||
                t.IsKind(SyntaxKind.BarBarToken) ||
                t.IsKind(SyntaxKind.QuestionQuestionToken));

            var key = $"{className}.{methodName}";
            if (!complexities.TryGetValue(key, out var existing) || complexity > existing)
                complexities[key] = complexity;
        }
    }
}

// 3. Compute CRAP and check threshold
var violations = new List<(string Method, double Crap, int Complexity, double Coverage)>();
foreach (var (key, comp) in complexities)
{
    var cov = coverage.GetValueOrDefault(key, 0.0);
    var crap = comp * comp * Math.Pow(1 - cov, 3) + comp;
    if (crap > threshold)
        violations.Add((key, crap, comp, cov));
}

if (violations.Count > 0)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"\n❌ CRAP Score Quality Gate FAILED");
    Console.WriteLine($"🚨 Found {violations.Count} methods exceeding CRAP threshold of {threshold}");
    Console.ResetColor();
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("\n🔥 High-Risk Methods:");
    foreach (var (method, crap, comp, cov) in violations.OrderByDescending(v => v.Crap))
        Console.WriteLine($"  • {method}: CRAP={crap:F1} (CC={comp}, Cov={cov:P0})");
    Console.ResetColor();
    Console.WriteLine("\n📖 Why this gate exists:");
    Console.WriteLine("  CRAP = f(complexity, lack of coverage). A high score means code that is");
    Console.WriteLine("  BOTH complicated and untested — the exact code that breaks in production.");
    Console.WriteLine("\n🔧 Do this:");
    Console.WriteLine("  1. Extract concepts into named methods; prefer deleting branches over moving them.");
    Console.WriteLine("  2. Write tests pinning behavior BEFORE refactoring.");
    Console.WriteLine("  3. If inherently complex, cover it thoroughly — full coverage still passes.");
    return 1;
}

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine($"\n✅ CRAP Score Quality Gate PASSED ({complexities.Count} methods analyzed)");
Console.ResetColor();
return 0;
