using GameVM.CrapGate.Core;

// CRAP score quality gate.
// Usage: GameVM.CrapGate <coverage.xml> <srcDir> [threshold]

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

var coverage = CoverageParser.ParseDotNetCoverage(coveragePath);
var complexities = ComplexityAnalyzer.AnalyzeDirectory(srcDir);

var violations = new List<(string Method, double Crap, int Complexity, double Coverage)>();
foreach (var (key, comp) in complexities)
{
    var cov = coverage.GetValueOrDefault(key, 0.0);
    if (CrapCalculator.ExceedsThreshold(comp, cov, threshold))
        violations.Add((key, CrapCalculator.Compute(comp, cov), comp, cov));
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
    return 1;
}

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine($"\n✅ CRAP Score Quality Gate PASSED ({complexities.Count} methods analyzed)");
Console.ResetColor();
return 0;
