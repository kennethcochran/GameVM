using System;
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

            if (context.SemanticModel.GetSymbolInfo(invocation).Symbol is not IMethodSymbol methodSymbol)
                return;

            if (!IsCfgTableNamespace(methodSymbol.ContainingNamespace?.ToDisplayString()))
                return;

            var args = invocation.ArgumentList?.Arguments;
            if (args == null)
                return;

            ReportRawIntLiterals(context, methodSymbol, args.Value);
        }

        internal static void ReportRawIntLiterals(
            SyntaxNodeAnalysisContext context,
            IMethodSymbol methodSymbol,
            SeparatedSyntaxList<ArgumentSyntax> args)
        {
            var count = Math.Min(args.Count, methodSymbol.Parameters.Length);
            for (int i = 0; i < count; i++)
            {
                var paramType = methodSymbol.Parameters[i].Type;
                if (args[i].Expression is LiteralExpressionSyntax literal &&
                    IsSystemInt32(paramType.Name, paramType.ContainingNamespace?.ToString()))
                {
                    var diagnostic = Diagnostic.Create(Rule, literal.GetLocation(), literal.Token.ValueText);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        internal static bool IsCfgTableNamespace(string? methodNamespace)
        {
            return methodNamespace != null && methodNamespace.Contains("CfgTable");
        }

        internal static bool IsSystemInt32(string typeName, string? typeNamespace)
        {
            return typeName == "Int32" && typeNamespace == "System";
        }
    }
}