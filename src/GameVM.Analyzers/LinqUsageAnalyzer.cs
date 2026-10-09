using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace GameVM.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class LinqUsageAnalyzer : DiagnosticAnalyzer
    {
        internal const string EditorConfigKey = "gamevm_enforce_no_linq_in";

        private const string DiagnosticId = "GVM003";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "LINQ Usage Prohibited in Optimization Pass",
            messageFormat: "LINQ usage (System.Linq.Enumerable methods) is prohibited in optimization passes to prevent hidden heap allocations",
            category: "Performance",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "LINQ methods create hidden allocations in performance-critical paths. Use manual loops or array operations instead."
        );

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            
            context.RegisterSyntaxNodeAction(HandleInvocation, SyntaxKind.InvocationExpression);
        }

        private static void HandleInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;
            var expression = invocation.Expression;
            var symbolInfo = context.SemanticModel.GetSymbolInfo(expression, context.CancellationToken);

            if (symbolInfo.Symbol is IMethodSymbol methodSymbol && IsLinqMethod(methodSymbol))
            {
                var location = invocation.GetLocation();
                var diagnostic = Diagnostic.Create(Rule, location, methodSymbol.Name);
                context.ReportDiagnostic(diagnostic);
            }
        }

        internal static bool IsLinqMethod(IMethodSymbol method)
        {
            var containingNamespace = method.ContainingType?.ContainingNamespace?.ToDisplayString();

            // Check if it's from System.Linq namespace
            if (IsSystemLinqNamespace(containingNamespace))
                return true;

            var extendedType = method.Parameters.Length > 0 ? method.Parameters[0].Type : null;
            return IsLinqExtensionMethod(
                method.Name,
                method.IsExtensionMethod,
                extendedType?.ToDisplayString(),
                extendedType?.OriginalDefinition?.ToDisplayString());
        }

        internal static bool IsSystemLinqNamespace(string? fullNamespace)
        {
            return fullNamespace == "System.Linq" ||
                (fullNamespace != null && fullNamespace.StartsWith("System.Linq.", StringComparison.Ordinal));
        }

        internal static bool IsLinqExtensionMethod(
            string methodName,
            bool isExtensionMethod,
            string? extendedTypeDisplay,
            string? extendedTypeOriginalDefinition)
        {
            if (!isExtensionMethod)
                return false;

            if (!LinqMethodNames.Contains(methodName))
                return false;

            // Verify it's from System.Linq.Enumerable
            return extendedTypeDisplay == "System.Collections.Generic.IEnumerable<T>" ||
                extendedTypeOriginalDefinition == "System.Collections.Generic.IEnumerable<T>";
        }

        internal static readonly string[] LinqMethodNames = new[]
        {
            "Where", "Select", "OrderBy", "GroupBy", "ToList", "ToArray",
            "Any", "All", "Count", "Min", "Max", "Average",
            "First", "Last", "Single", "ElementAt", "Skip", "Take",
            "Distinct", "Union", "Intersect", "Except", "Join", "GroupJoin",
            "SelectMany", "Reverse", "Concat", "Zip", "Aggregate", "Sum",
            "MinBy", "MaxBy"
        };
    }
}