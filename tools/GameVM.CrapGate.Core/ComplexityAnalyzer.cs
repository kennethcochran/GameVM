using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GameVM.CrapGate.Core;

/// <summary>
/// Computes cyclomatic complexity for C# methods using Roslyn.
/// Complexity starts at 1 and increments for each decision point:
/// if, while, for, foreach, case, catch, ternary, switch arm, &&, ||, ??
/// </summary>
public static class ComplexityAnalyzer
{
    public static int ComputeMethodComplexity(MethodDeclarationSyntax method)
    {
        var complexity = 1;
        complexity += method.DescendantNodes().Count(n =>
            n is IfStatementSyntax ||
            n is WhileStatementSyntax ||
            n is ForStatementSyntax ||
            n is ForEachStatementSyntax ||
            n is CaseSwitchLabelSyntax ||
            n is CatchClauseSyntax ||
            n is ConditionalExpressionSyntax ||
            (n is SwitchExpressionArmSyntax arm && !IsDiscardPattern(arm)));
        complexity += method.DescendantTokens().Count(t =>
            t.IsKind(SyntaxKind.AmpersandAmpersandToken) ||
            t.IsKind(SyntaxKind.BarBarToken) ||
            t.IsKind(SyntaxKind.QuestionQuestionToken));
        return complexity;
    }

    private static bool IsDiscardPattern(SwitchExpressionArmSyntax arm) =>
        arm.Pattern is DiscardPatternSyntax;

    public static Dictionary<string, int> AnalyzeDirectory(string srcDir)
    {
        var result = new Dictionary<string, int>();
        var csFiles = Directory.GetFiles(srcDir, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains("/obj/") && !f.Contains("/bin/") && !f.Contains("/ANTLR/"));

        foreach (var file in csFiles)
        {
            var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(file));
            var root = tree.GetRoot();
            foreach (var classDecl in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                var className = classDecl.Identifier.Text;
                foreach (var method in classDecl.DescendantNodes().OfType<MethodDeclarationSyntax>())
                {
                    var key = $"{className}.{method.Identifier.Text}";
                    var complexity = ComputeMethodComplexity(method);
                    if (!result.TryGetValue(key, out var existing) || complexity > existing)
                        result[key] = complexity;
                }
            }
        }
        return result;
    }
}
