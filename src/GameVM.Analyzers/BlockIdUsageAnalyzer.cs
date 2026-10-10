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

            var methodSymbol = GetCfgTableMethodSymbol(context, invocation);
            if (methodSymbol == null)
                return;

            CheckInt32LiteralArguments(context, invocation, methodSymbol);
        }

        private static IMethodSymbol? GetCfgTableMethodSymbol(
            SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocation)
        {
            var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation);
            if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
                return null;

            var methodNs = methodSymbol.ContainingNamespace?.ToDisplayString() ?? "";
            if (!methodNs.Contains("CfgTable"))
                return null;

            return methodSymbol;
        }

        private static void CheckInt32LiteralArguments(
            SyntaxNodeAnalysisContext context,
            InvocationExpressionSyntax invocation,
            IMethodSymbol methodSymbol)
        {
            var args = invocation.ArgumentList?.Arguments;
            if (args == null)
                return;

            for (int i = 0; i < args.Value.Count && i < methodSymbol.Parameters.Length; i++)
            {
                ReportIfInt32Literal(context, args.Value[i], methodSymbol.Parameters[i].Type);
            }
        }

        private static void ReportIfInt32Literal(
            SyntaxNodeAnalysisContext context, ArgumentSyntax arg, ITypeSymbol paramType)
        {
            if (paramType.Name != "Int32")
                return;
            if (arg.Expression is not LiteralExpressionSyntax literal)
                return;
            if (paramType.ContainingNamespace?.ToString() != "System")
                return;

            var diagnostic = Diagnostic.Create(Rule, literal.GetLocation(), literal.Token.ValueText);
            context.ReportDiagnostic(diagnostic);
        }
    }
}