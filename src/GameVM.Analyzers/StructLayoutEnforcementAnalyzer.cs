using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace GameVM.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class StructLayoutEnforcementAnalyzer : DiagnosticAnalyzer
    {
        internal const string EditorConfigKey = "gamevm_enforce_struct_layout_in";

        private const string DiagnosticId = "GVM002";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "DOD Struct Must Have Explicit StructLayout",
            messageFormat: "Struct '{0}' in namespace '{1}' must be decorated with [StructLayout(LayoutKind.Sequential)] or [StructLayout(LayoutKind.Explicit)] for Data-Oriented Design compliance",
            category: "Design",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Structs in Data-Oriented Design namespaces must have explicit StructLayout to guarantee predictable memory layout for serialization and block copying.");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
        }

        private static void AnalyzeSymbol(SymbolAnalysisContext context)
        {
            var namedType = (INamedTypeSymbol)context.Symbol;

            if (namedType.TypeKind != TypeKind.Struct)
                return;

            var namespaceName = GetNamespaceName(namedType);
            if (namespaceName == null)
                return;

            var targetNamespaces = GetTargetNamespaces(context, namedType);
            if (targetNamespaces.Count == 0)
                return;

            ReportIfInTargetNamespace(context, namedType, namespaceName, targetNamespaces);
        }

        private static void ReportIfInTargetNamespace(
            SymbolAnalysisContext context,
            INamedTypeSymbol namedType,
            string namespaceName,
            System.Collections.Generic.IReadOnlyList<string> targetNamespaces)
        {
            foreach (var target in targetNamespaces)
            {
                if (!IsOrUnderNamespace(namespaceName, target))
                    continue;

                if (!HasValidStructLayout(namedType))
                {
                    ReportInvalidLayout(context, namedType, namespaceName);
                }
                return;
            }
        }

        private static string? GetNamespaceName(INamedTypeSymbol namedType)
        {
            var ns = namedType.ContainingNamespace;
            if (ns == null || ns.IsGlobalNamespace)
                return null;
            return ns.ToDisplayString();
        }

        private static IReadOnlyList<string> GetTargetNamespaces(SymbolAnalysisContext context, INamedTypeSymbol namedType)
        {
            var syntaxRef = namedType.DeclaringSyntaxReferences.FirstOrDefault();
            if (syntaxRef == null)
                return Array.Empty<string>();

            var options = context.Options.AnalyzerConfigOptionsProvider.GetOptions(syntaxRef.SyntaxTree);
            if (!options.TryGetValue(EditorConfigKey, out var rawList) || string.IsNullOrWhiteSpace(rawList))
                return Array.Empty<string>();

            return ParseNamespaces(rawList);
        }

        private static void ReportInvalidLayout(SymbolAnalysisContext context, INamedTypeSymbol namedType, string namespaceName)
        {
            var syntaxRef = namedType.DeclaringSyntaxReferences.FirstOrDefault();
            var location = syntaxRef?.GetSyntax().GetLocation();
            var diagnostic = Diagnostic.Create(Rule, location, namedType.Name, namespaceName);
            context.ReportDiagnostic(diagnostic);
        }

        private static bool HasValidStructLayout(INamedTypeSymbol type)
        {
            var layoutKind = GetStructLayoutKind(type);
            return layoutKind == LayoutKind.Sequential || layoutKind == LayoutKind.Explicit;
        }

        private static LayoutKind? GetStructLayoutKind(INamedTypeSymbol type)
        {
            foreach (var attr in type.GetAttributes())
            {
                if (attr.AttributeClass?.ToDisplayString() != "System.Runtime.InteropServices.StructLayoutAttribute")
                    continue;

                if (attr.ConstructorArguments.Length == 0)
                    continue;

                if (attr.ConstructorArguments[0].Value is LayoutKind kind)
                    return kind;
            }
            return null;
        }

        private static IReadOnlyList<string> ParseNamespaces(string raw)
        {
            return raw
                .Split(',')
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .ToList();
        }

        private static bool IsOrUnderNamespace(string namespaceName, string target)
        {
            if (namespaceName.Length < target.Length)
                return false;
            if (!namespaceName.StartsWith(target, StringComparison.Ordinal))
                return false;
            return namespaceName.Length == target.Length || namespaceName[target.Length] == '.';
        }
    }
}