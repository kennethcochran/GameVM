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
    public sealed class ExhaustiveSwitchAnalyzer : DiagnosticAnalyzer
    {
        private const string DiagnosticId = "GVM005";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Switch on InstructionKind Not Exhaustive",
            messageFormat: "Switch on InstructionKind does not handle all known instruction types. Add cases for missing kinds.",
            category: "Design",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Switches on InstructionKind enums should be exhaustive to catch unhandled instruction types during compilation.");

        private static readonly string[] KnownInstructionKinds = new[]
        {
            "NOP", "LITERAL_INT", "LITERAL_STRING", "LITERAL_BOOL",
            "IDENTIFIER", "BINARY_OP", "UNARY_OP", "ASSIGNMENT",
            "VARIABLE_DECLARATION", "METHOD_CALL", "IF_STATEMENT",
            "WHILE_STATEMENT", "RETURN_STATEMENT", "BLOCK"
        };

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeSwitch, SyntaxKind.SwitchStatement);
        }

        private static void AnalyzeSwitch(SyntaxNodeAnalysisContext context)
        {
            var switchStmt = (SwitchStatementSyntax)context.Node;
            var semanticModel = context.SemanticModel;

            if (!IsGameVmInstructionSwitch(switchStmt, semanticModel))
                return;

            var (handledKinds, hasDefault) = CollectHandledKinds(switchStmt, semanticModel);

            if (!hasDefault && HasTooManyMissingKinds(handledKinds))
            {
                var diagnostic = Diagnostic.Create(Rule, switchStmt.SwitchKeyword.GetLocation());
                context.ReportDiagnostic(diagnostic);
            }
        }

        internal static bool IsGameVmInstructionSwitch(SwitchStatementSyntax switchStmt, SemanticModel semanticModel)
        {
            var typeInfo = semanticModel.GetTypeInfo(switchStmt.Expression);
            if (typeInfo.Type == null)
                return false;

            var typeName = typeInfo.Type.Name;
            if (typeName != "Byte" && typeName != "InstructionKind")
                return false;

            var ns = typeInfo.Type.ContainingNamespace?.ToDisplayString() ?? "";
            return ns.Contains("GameVM");
        }

        internal static (HashSet<string> HandledKinds, bool HasDefault) CollectHandledKinds(
            SwitchStatementSyntax switchStmt, SemanticModel semanticModel)
        {
            var handledKinds = new HashSet<string>();
            var hasDefault = false;

            foreach (var section in switchStmt.Sections)
            {
                foreach (var label in section.Labels)
                {
                    if (ProcessLabel(label, semanticModel, handledKinds))
                        hasDefault = true;
                }
            }

            return (handledKinds, hasDefault);
        }

        internal static bool ProcessLabel(SyntaxNode label, SemanticModel semanticModel, HashSet<string> handledKinds)
        {
            if (label is DefaultSwitchLabelSyntax)
                return true;

            if (label is CasePatternSwitchLabelSyntax caseLabel)
            {
                var kind = GetConstantKind(caseLabel, semanticModel);
                if (kind != null)
                    handledKinds.Add(kind);
            }
            else if (label is CaseSwitchLabelSyntax simpleLabel)
            {
                handledKinds.Add(simpleLabel.Value.ToString());
            }

            return false;
        }

        internal static string? GetConstantKind(CasePatternSwitchLabelSyntax label, SemanticModel semanticModel)
        {
            if (label.Pattern == null)
                return null;
            var constValue = semanticModel.GetConstantValue(label.Pattern);
            return constValue.HasValue && constValue.Value != null
                ? constValue.Value.ToString()
                : null;
        }

        internal static bool HasTooManyMissingKinds(HashSet<string> handledKinds)
        {
            var missingCount = KnownInstructionKinds.Count(k => !handledKinds.Contains(k));
            return missingCount > KnownInstructionKinds.Length / 2;
        }
    }
}