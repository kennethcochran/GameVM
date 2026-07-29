using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace GameVM.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class BlockIdUsageAnalyzer : DiagnosticAnalyzer
    {
        private const string DiagnosticId = "GVM004";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Raw Integer Used Where BlockId Expected",
            messageFormat: "Raw integer used where BlockId expected in CFG API. Use BlockId.FromInt({0}) instead.",
            category: "Design",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "CFG APIs expect BlockId types, not raw integers. Use BlockId.FromInt() for type-safe block references.");

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

            var semanticModel = context.SemanticModel;
            var symbolInfo = semanticModel.GetSymbolInfo(invocation);
            if (symbolInfo.Symbol is IMethodSymbol methodSymbol)
            {
                var methodNs = methodSymbol.ContainingNamespace?.ToDisplayString() ?? "";
                if (!methodNs.Contains("CfgTable"))
                    return;

                var args = invocation.ArgumentList?.Arguments;
                if (args == null)
                    return;

                for (int i = 0; i < args.Value.Count && i < methodSymbol.Parameters.Length; i++)
                {
                    var arg = args.Value[i];
                    var paramType = methodSymbol.Parameters[i].Type;
                    if (paramType.Name == "Int32" && arg.Expression is LiteralExpressionSyntax literal)
                    {
                        if (paramType.ContainingNamespace?.ToString() == "System")
                        {
                            var diagnostic = Diagnostic.Create(Rule, literal.GetLocation(), literal.Token.ValueText);
                            context.ReportDiagnostic(diagnostic);
                        }
                    }
                }
            }
        }
    }
}