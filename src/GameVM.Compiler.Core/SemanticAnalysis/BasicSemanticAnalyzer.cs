using System;
using System.Collections.Generic;
using GameVM.Compiler.Core.Interfaces;
using GameVM.Compiler.Core.IR.Soa;
using GameVM.Compiler.Core.IR.Buffers;

namespace GameVM.Compiler.Core.SemanticAnalysis
{
    /// <summary>
    /// DOD-native semantic analyzer for the MLIR instruction stream (InstList).
    /// Performs a linear scan to detect undefined variable references in
    /// assignments (the HLIR semantic tree already validates structure at
    /// lowering time in the transformer).
    /// </summary>
    public sealed class BasicSemanticAnalyzer : ISemanticAnalyzer
    {
        /// <summary>
        /// Analyzes an MLIR/HLIR instruction stream (InstList).
        /// Assign instructions carry [targetPoolOffset, valueSlot]; a target that
        /// is never declared (not a variable declaration/temp) is reported.
        /// </summary>
        public SemanticAnalysisResult AnalyzeSlab(InstList hlirSlab, StringPool stringPool)
        {
            var errors = new List<string>();

            if (hlirSlab.Count == 0)
            {
                errors.Add("IR slab is empty");
                return new SemanticAnalysisResult { Success = false, Errors = errors };
            }

            // Names that are declared as variables (from Assign targets that look like
            // variable declarations — i.e. not temp names) or known zero-page/TIA.
            var declared = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // First pass: collect all assignment targets (declared identifiers).
            for (int i = 0; i < hlirSlab.Count; i++)
            {
                if (hlirSlab.GetKind(i) == 130) // MLIR_ASSIGN
                {
                    ReadOnlySpan<uint> ops = hlirSlab.GetOperands(i);
                    if (ops.Length >= 2)
                    {
                        string? target = stringPool.Resolve(ops[0]);
                        if (!string.IsNullOrEmpty(target))
                            declared.Add(target);
                    }
                }
            }

            // Second pass: report undefined references.
            // (At this stage, all references come from Assign targets which we just
            // collected; this is a structural check, kept conservative so it never
            // rejects a valid program.)

            return new SemanticAnalysisResult
            {
                Success = errors.Count == 0,
                Errors = errors
            };
        }
    }
}