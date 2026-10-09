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

            var syntaxRef = namedType.DeclaringSyntaxReferences.FirstOrDefault();
            if (syntaxRef == null)
                return;

            var options = context.Options.AnalyzerConfigOptionsProvider.GetOptions(syntaxRef.SyntaxTree);
            if (!options.TryGetValue(EditorConfigKey, out var rawList))
                return;

            if (ShouldReportStruct(namedType, rawList, out var namespaceName))
            {
                var location = syntaxRef.GetSyntax().GetLocation();
                var diagnostic = Diagnostic.Create(Rule, location, namedType.Name, namespaceName);
                context.ReportDiagnostic(diagnostic);
            }
        }

        internal static bool ShouldReportStruct(INamedTypeSymbol namedType, string? rawConfig, out string namespaceName)
        {
            namespaceName = "";
            if (namedType.TypeKind != TypeKind.Struct)
                return false;

            if (!TryGetNamespaceName(namedType, out namespaceName))
                return false;

            if (!IsNamespaceTargeted(namespaceName, rawConfig))
                return false;

            return !HasValidStructLayout(namedType);
        }

        internal static bool TryGetNamespaceName(INamedTypeSymbol namedType, out string namespaceName)
        {
            namespaceName = "";
            var ns = namedType.ContainingNamespace;
            if (ns == null || ns.IsGlobalNamespace)
                return false;

            namespaceName = ns.ToDisplayString();
            return true;
        }

        internal static bool IsNamespaceTargeted(string namespaceName, string? rawConfig)
        {
            if (string.IsNullOrWhiteSpace(rawConfig))
                return false;

            var targetNamespaces = ParseNamespaces(rawConfig!);
            return targetNamespaces.Any(t => IsOrUnderNamespace(namespaceName, t));
        }

        internal static bool HasValidStructLayout(INamedTypeSymbol type)
        {
            foreach (var attr in type.GetAttributes())
            {
                if (IsValidStructLayoutAttribute(attr))
                    return true;
            }
            return false;
        }

        internal static bool IsValidStructLayoutAttribute(AttributeData attr)
        {
            if (attr.AttributeClass?.ToDisplayString() != "System.Runtime.InteropServices.StructLayoutAttribute")
                return false;

            if (attr.ConstructorArguments.Length == 0)
                return false;

            return IsValidLayoutKind(attr.ConstructorArguments[0].Value);
        }

        internal static bool IsValidLayoutKind(object? value)
        {
            return value is LayoutKind kind &&
                (kind == LayoutKind.Sequential || kind == LayoutKind.Explicit);
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

        internal static bool IsOrUnderNamespace(string namespaceName, string target)
        {
            if (namespaceName.Length < target.Length)
                return false;
            if (!namespaceName.StartsWith(target, StringComparison.Ordinal))
                return false;
            return namespaceName.Length == target.Length || namespaceName[target.Length] == '.';
        }
    }
}