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

            var ns = namedType.ContainingNamespace;
            if (ns == null || ns.IsGlobalNamespace)
                return;

            var namespaceName = ns.ToDisplayString();

            var syntaxRef = namedType.DeclaringSyntaxReferences.FirstOrDefault();
            if (syntaxRef == null)
                return;

            var syntaxTree = syntaxRef.SyntaxTree;
            var options = context.Options.AnalyzerConfigOptionsProvider.GetOptions(syntaxTree);
            if (!options.TryGetValue(EditorConfigKey, out var rawList) || string.IsNullOrWhiteSpace(rawList))
                return;

            var targetNamespaces = ParseNamespaces(rawList);
            if (targetNamespaces.Count == 0)
                return;

            foreach (var target in targetNamespaces)
            {
                if (!IsOrUnderNamespace(namespaceName, target))
                    continue;

                if (!HasValidStructLayout(namedType))
                {
                    var location = syntaxRef.GetSyntax().GetLocation();
                    var diagnostic = Diagnostic.Create(Rule, location, namedType.Name, namespaceName);
                    context.ReportDiagnostic(diagnostic);
                }
                return;
            }
        }

        private static bool HasValidStructLayout(INamedTypeSymbol type)
        {
            foreach (var attr in type.GetAttributes())
            {
                if (attr.AttributeClass?.ToDisplayString() == "System.Runtime.InteropServices.StructLayoutAttribute")
                {
                    if (attr.ConstructorArguments.Length > 0)
                    {
                        var layoutKind = attr.ConstructorArguments[0].Value;
                        if (layoutKind is LayoutKind kind)
                        {
                            return kind == LayoutKind.Sequential || kind == LayoutKind.Explicit;
                        }
                    }
                }
            }
            return false;
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