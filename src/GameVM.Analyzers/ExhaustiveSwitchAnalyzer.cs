using System.Collections.Immutable;
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
            var typeInfo = semanticModel.GetTypeInfo(switchStmt.Expression);

            if (!IsTargetSwitchType(typeInfo.Type))
                return;

            var (handledKinds, hasDefault) = CollectHandledKinds(switchStmt, semanticModel);

            if (!hasDefault)
                ReportIfNotExhaustive(context, switchStmt, handledKinds);
        }

        private static bool IsTargetSwitchType(ITypeSymbol? type)
        {
            if (type == null)
                return false;

            var typeName = type.Name;
            if (typeName != "Byte" && typeName != "InstructionKind")
                return false;

            var ns = type.ContainingNamespace?.ToDisplayString() ?? "";
            return ns.Contains("GameVM");
        }

        private static (System.Collections.Generic.HashSet<string> handledKinds, bool hasDefault) CollectHandledKinds(
            SwitchStatementSyntax switchStmt, SemanticModel semanticModel)
        {
            var handledKinds = new System.Collections.Generic.HashSet<string>();
            bool hasDefault = false;

            foreach (var section in switchStmt.Sections)
            {
                foreach (var label in section.Labels)
                {
                    if (label is DefaultSwitchLabelSyntax)
                    {
                        hasDefault = true;
                    }
                    else
                    {
                        string? kind = GetLabelKind(label, semanticModel);
                        if (kind != null)
                            handledKinds.Add(kind);
                    }
                }
            }

            return (handledKinds, hasDefault);
        }

        private static string? GetLabelKind(SwitchLabelSyntax label, SemanticModel semanticModel)
        {
            if (label is CasePatternSwitchLabelSyntax caseLabel)
                return GetPatternLabelKind(caseLabel, semanticModel);
            if (label is CaseSwitchLabelSyntax simpleLabel)
                return simpleLabel.Value.ToString();
            return null;
        }

        private static string? GetPatternLabelKind(CasePatternSwitchLabelSyntax caseLabel, SemanticModel semanticModel)
        {
            if (caseLabel.Pattern == null)
                return null;
            var constValue = semanticModel.GetConstantValue(caseLabel.Pattern);
            if (constValue.HasValue && constValue.Value != null)
                return constValue.Value.ToString();
            return null;
        }

        private static void ReportIfNotExhaustive(
            SyntaxNodeAnalysisContext context,
            SwitchStatementSyntax switchStmt,
            System.Collections.Generic.HashSet<string> handledKinds)
        {
            int missingCount = 0;
            for (int i = 0; i < KnownInstructionKinds.Length; i++)
            {
                if (!handledKinds.Contains(KnownInstructionKinds[i]))
                    missingCount++;
            }
            if (missingCount > KnownInstructionKinds.Length / 2)
            {
                var diagnostic = Diagnostic.Create(Rule, switchStmt.SwitchKeyword.GetLocation());
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}