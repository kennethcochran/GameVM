#r "System.Text.Json"
#r "System.Xml.Linq"
#r "System.Linq"

using System.Text.Json;
using System.IO;
using System.Xml.Linq;
using System.Linq;

var reportPath = Args.Count > 0 ? Args[0] : "TestResults/crap-check";
var threshold = Args.Count > 1 ? int.Parse(Args[1]) : 30;

Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("🔍 Checking CRAP Score Quality Gate...");
Console.ForegroundColor = ConsoleColor.Gray;
Console.WriteLine($"📊 Report: {reportPath} | 🎯 Threshold: {threshold}");

if (!Directory.Exists(reportPath)) {
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"❌ Coverage report directory not found: {reportPath}");
    Console.WriteLine("💡 Run tests with coverage first: dotnet test --collect:'XPlat Code Coverage'");
    return 1;
}

try {
    var violations = new System.Collections.Generic.List<(string Class, string Method, double CrapScore, int Complexity)>();
    
    // Parse all XML files in the report directory
    var xmlFiles = Directory.GetFiles(reportPath, "*.xml", SearchOption.AllDirectories);
    
    foreach (var xmlFile in xmlFiles) {
        try {
            var xml = XDocument.Load(xmlFile);
            
            // Get the class name from the filename (remove .xml extension)
            var fileName = Path.GetFileNameWithoutExtension(xmlFile);
            var className = fileName.Replace("_", ".");
            
            // Find all method elements with CRAP scores
            var methodElements = xml.Descendants("Element")
                .Where(e => e.Element("CrapScore") != null);
            
            foreach (var methodElement in methodElements) {
                var methodName = methodElement.Attribute("name")?.Value ?? "Unknown";
                var crapScoreElement = methodElement.Element("CrapScore");
                var complexityElement = methodElement.Element("Cyclomaticcomplexity");
                
                if (crapScoreElement != null && complexityElement != null) {
                    var crapScore = double.Parse(crapScoreElement.Value);
                    var complexity = int.Parse(complexityElement.Value);
                    
                    if (crapScore > threshold) {
                        violations.Add((
                            className,
                            methodName,
                            crapScore,
                            complexity
                        ));
                    }
                }
            }
        } catch {
            // Skip files that can't be parsed
        }
    }

    if (violations.Count > 0) {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"\n❌ CRAP Score Quality Gate FAILED");
        Console.WriteLine($"🚨 Found {violations.Count} methods exceeding CRAP threshold of {threshold}");
        
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("\n🔥 High-Risk Methods:");
        
        // Sort violations by CRAP score
        var sortedViolations = violations.ToArray();
        System.Array.Sort(sortedViolations, (a, b) => b.CrapScore.CompareTo(a.CrapScore));
        
        foreach (var violation in sortedViolations) {
            Console.WriteLine($"  • {violation.Class}.{violation.Method}: CRAP={violation.CrapScore:F1} (CC={violation.Complexity})");
        }
        
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("\n💡 To fix: Reduce complexity or increase test coverage");
        return 1;
    } else {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"✅ CRAP Score Quality Gate PASSED");
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine($"📊 All methods have CRAP ≤ {threshold}");
        return 0;
    }
} catch (Exception ex) {
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"❌ Failed to parse coverage report: {ex.Message}");
    return 1;
}
