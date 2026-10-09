using System;
using System.Collections.Generic;
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

            var options = context.Options.AnalyzerConfigOptionsProvider.GetOptions(invocation.SyntaxTree);
            if (!options.TryGetValue(EditorConfigKey, out var rawList))
                return;

            if (context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol is not IMethodSymbol methodSymbol)
                return;

            if (ShouldReportInvocation(
                rawList,
                methodSymbol.IsVirtual,
                methodSymbol.IsAbstract,
                methodSymbol.IsOverride,
                methodSymbol.ContainingNamespace?.ToDisplayString()))
            {
                var diagnostic = Diagnostic.Create(Rule, invocation.GetLocation());
                context.ReportDiagnostic(diagnostic);
            }
        }

        internal static bool ShouldReportInvocation(
            string? rawConfig,
            bool isVirtual,
            bool isAbstract,
            bool isOverride,
            string? containingNamespace)
        {
            if (string.IsNullOrWhiteSpace(rawConfig))
                return false;

            var targetNamespaces = ParseNamespaces(rawConfig!);
            if (targetNamespaces.Count == 0)
                return false;

            if (!IsVirtualDispatch(isVirtual, isAbstract, isOverride))
                return false;

            return IsInTargetNamespace(containingNamespace ?? "", targetNamespaces);
        }

        internal static IReadOnlyList<string> ParseNamespaces(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return Array.Empty<string>();

            return raw!
                .Split(',')
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .ToList();
        }

        internal static bool IsVirtualDispatch(bool isVirtual, bool isAbstract, bool isOverride)
        {
            return isVirtual || isAbstract || isOverride;
        }

        internal static bool IsInTargetNamespace(string containingNamespace, IReadOnlyList<string> targetNamespaces)
        {
            foreach (var target in targetNamespaces)
            {
                if (containingNamespace == target ||
                    containingNamespace.StartsWith(target + ".", StringComparison.Ordinal))
                    return true;
            }
            return false;
        }
}
}