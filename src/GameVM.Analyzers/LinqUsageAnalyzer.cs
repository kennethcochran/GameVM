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

        private void HandleInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;
            var expression = invocation.Expression;
            var symbolInfo = context.SemanticModel.GetSymbolInfo(expression, context.CancellationToken);
            
            if (symbolInfo.Symbol is IMethodSymbol methodSymbol)
            {
                if (IsLinqMethod(methodSymbol))
                {
                    var location = invocation.GetLocation();
                    var diagnostic = Diagnostic.Create(Rule, location, methodSymbol.Name);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        private bool IsLinqMethod(IMethodSymbol method)
        {
            var containingType = method.ContainingType;
            var containingNamespace = containingType?.ContainingNamespace;
            
            // Check if it's from System.Linq namespace
            if (containingNamespace != null)
            {
                var fullNamespace = containingNamespace.ToDisplayString();
                if (fullNamespace == "System.Linq" || fullNamespace.StartsWith("System.Linq."))
                {
                    return true;
                }
            }
            
            // Also check for Enumerable extension methods by name
            var methodName = method.Name;
            var linqMethods = new[] { "Where", "Select", "OrderBy", "GroupBy", "ToList", "ToArray", "Any", "All", "Count", "Min", "Max", "Average", "First", "Last", "Single", "ElementAt", "Skip", "Take", "Distinct", "Union", "Intersect", "Except", "Join", "GroupJoin", "SelectMany", "Reverse", "Concat", "Zip", "Aggregate", "Sum", "MinBy", "MaxBy" };
            
            if (linqMethods.Contains(methodName) && method.IsExtensionMethod)
            {
                // Verify it's from System.Linq.Enumerable
                var extendedType = method.Parameters[0].Type;
                return extendedType?.ToDisplayString() == "System.Collections.Generic.IEnumerable<T>" ||
                       extendedType?.OriginalDefinition?.ToDisplayString() == "System.Collections.Generic.IEnumerable<T>";
            }
            
            return false;
        }
    }
}