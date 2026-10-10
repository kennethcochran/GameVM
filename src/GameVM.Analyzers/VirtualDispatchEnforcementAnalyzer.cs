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
    public sealed class VirtualDispatchEnforcementAnalyzer : DiagnosticAnalyzer
    {
        internal const string EditorConfigKey = "gamevm_enforce_no_virtual_in";

        private const string DiagnosticId = "GVM006";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Virtual/Interface Dispatch Prohibited in Optimization Pass",
            messageFormat: "Virtual or interface dispatch is prohibited in configured namespaces to prevent indirect call overhead",
            category: "Performance",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Virtual and interface method calls are prohibited in performance-critical optimization passes.");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;

            var targetNamespaces = GetTargetNamespaces(context, invocation);
            if (targetNamespaces.Length == 0)
                return;

            var semanticModel = context.SemanticModel;
            var symbolInfo = semanticModel.GetSymbolInfo(invocation);

            if (symbolInfo.Symbol is IMethodSymbol methodSymbol &&
                IsVirtualDispatchInTargetNamespace(methodSymbol, targetNamespaces))
            {
                var diagnostic = Diagnostic.Create(Rule, invocation.GetLocation());
                context.ReportDiagnostic(diagnostic);
            }
        }

        private static string[] GetTargetNamespaces(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocation)
        {
            var syntaxTree = invocation.SyntaxTree;
            var options = context.Options.AnalyzerConfigOptionsProvider.GetOptions(syntaxTree);
            if (!options.TryGetValue(EditorConfigKey, out var rawList) || string.IsNullOrWhiteSpace(rawList))
                return Array.Empty<string>();

            return ((string)rawList).Split(',').Select(s => s.Trim()).Where(s => s.Length > 0).ToArray();
        }

        private static bool IsVirtualDispatchInTargetNamespace(IMethodSymbol methodSymbol, string[] targetNamespaces)
        {
            if (!IsVirtualOrOverride(methodSymbol))
                return false;

            var containingNs = methodSymbol.ContainingNamespace?.ToDisplayString() ?? "";
            return IsInTargetNamespace(containingNs, targetNamespaces);
        }

        private static bool IsVirtualOrOverride(IMethodSymbol methodSymbol)
        {
            return methodSymbol.IsVirtual || methodSymbol.IsAbstract || methodSymbol.IsOverride;
        }

        private static bool IsInTargetNamespace(string containingNs, string[] targetNamespaces)
        {
            foreach (var target in targetNamespaces)
            {
                if (containingNs == target || containingNs.StartsWith(target + "."))
                    return true;
            }
            return false;
        }
}
}