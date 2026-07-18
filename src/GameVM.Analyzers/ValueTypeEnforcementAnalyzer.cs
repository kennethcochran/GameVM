using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace GameVM.Analyzers
{
    /// <summary>
    /// Enforces that types declared in configured namespaces are value types (structs), not
    /// reference types (classes), to support Data-Oriented Design (DOD) compliance.
    ///
    /// Target namespaces are supplied dynamically through .editorconfig:
    ///   gamevm_enforce_value_types_in = GameVM.Compiler.Core.DOD,GameVM.Compiler.Core.IR
    ///
    /// No attributes or hard-coded namespace strings are used; the list is read per-syntax-tree
    /// from the applicable analyzer config options.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ValueTypeEnforcementAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>.editorconfig key listing the target namespaces (comma-separated).</summary>
        internal const string EditorConfigKey = "gamevm_enforce_value_types_in";

        private const string DiagnosticId = "GVM001";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "DOD Structure Must Be A Value Type",
            messageFormat: "Type '{0}' in namespace '{1}' must be a struct for Data-Oriented Design compliance",
            category: "Design",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Types in Data-Oriented Design namespaces must be value types (structs) to guarantee contiguous, blittable memory layouts.");

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

            if (namedType.TypeKind != TypeKind.Class)
                return;

            // Resolve the declared namespace (full dotted name). Skip types with no namespace
            // (global namespace) since they can never match a configured prefix.
            var ns = namedType.ContainingNamespace;
            if (ns == null || ns.IsGlobalNamespace)
                return;

            var namespaceName = ns.ToDisplayString();

            // Need the syntax tree to read per-tree .editorconfig options. Metadata symbols
            // (no syntax) have no applicable options and are skipped.
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

                var location = syntaxRef.GetSyntax().GetLocation();
                var diagnostic = Diagnostic.Create(Rule, location, namedType.Name, namespaceName);
                context.ReportDiagnostic(diagnostic);
                return; // one diagnostic per violating type is sufficient
            }
        }

        private static IReadOnlyList<string> ParseNamespaces(string raw)
        {
            return raw
                .Split(',')
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .ToList();
        }

        /// <summary>
        /// Returns true if <paramref name="namespaceName"/> equals <paramref name="target"/> or is
        /// nested directly under it (segment-aware, so <c>Foo.Bar.Baz</c> matches <c>Foo.Bar</c>
        /// but <c>Foo.Barbaz</c> does not).
        /// </summary>
        private static bool IsOrUnderNamespace(string namespaceName, string target)
        {
            if (namespaceName.Length < target.Length)
                return false;
            if (!namespaceName.StartsWith(target, StringComparison.Ordinal))
                return false;
            // Exact match, or followed by a namespace segment separator.
            return namespaceName.Length == target.Length || namespaceName[target.Length] == '.';
        }
    }
}
