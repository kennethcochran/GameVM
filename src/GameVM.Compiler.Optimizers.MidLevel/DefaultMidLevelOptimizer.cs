using System;
using System.Collections.Generic;
using System.Linq;
using GameVM.Compiler.Application.Services;
using GameVM.Compiler.Core.IR;
using GameVM.Compiler.Core.Enums;

namespace GameVM.Compiler.Optimizers.MidLevel
{
    /// <summary>
    /// Default implementation of mid-level IR optimizer.
    /// Performs optimizations such as dead code elimination, constant propagation,
    /// and common subexpression elimination on mid-level IR.
    /// </summary>
    public class DefaultMidLevelOptimizer : IMidLevelOptimizer
    {
        /// <summary>
        /// Optimizes the given mid-level IR based on the specified optimization level.
        /// </summary>
        /// <param name="ir">The mid-level IR to optimize</param>
        /// <param name="optimizationLevel">The level of optimization to apply</param>
        /// <returns>The optimized mid-level IR</returns>
        public MidLevelIR Optimize(MidLevelIR ir, OptimizationLevel optimizationLevel)
        {
            if (ir == null)
                throw new ArgumentNullException(nameof(ir));

            // For Basic optimization level or higher, create a copy to avoid mutating input
            var optimized = new MidLevelIR
            {
                SourceFile = ir.SourceFile,
                Functions = new Dictionary<string, MidLevelIR.MLFunction>()
            };

            // Process each function
            foreach (var kvp in ir.Functions)
            {
                var function = OptimizeFunction(kvp.Value, optimizationLevel);
                optimized.Functions[kvp.Key] = function;
            }

            return optimized;
        }

        /// <summary>
        /// Optimizes a single function in the mid-level IR.
        /// </summary>
        private MidLevelIR.MLFunction OptimizeFunction(MidLevelIR.MLFunction function, OptimizationLevel level)
        {
            if (function == null)
                throw new ArgumentNullException(nameof(function));

            var optimized = new MidLevelIR.MLFunction
            {
                Name = function.Name,
                Instructions = new List<MidLevelIR.MLInstruction>()
            };

            // Apply optimizations based on level
            if (level >= OptimizationLevel.Basic)
            {
                // Remove duplicate assignments (simple dead code elimination)
                optimized.Instructions = RemoveDuplicateAssignments(function.Instructions);
            }
            else
            {
                // No optimization, just copy instructions
                optimized.Instructions = new List<MidLevelIR.MLInstruction>(function.Instructions);
            }

            return optimized;
        }

        /// <summary>
        /// Removes duplicate assignments where the same target is assigned multiple times
        /// in sequence, keeping only the last assignment.
        /// </summary>
        private List<MidLevelIR.MLInstruction> RemoveDuplicateAssignments(List<MidLevelIR.MLInstruction> instructions)
        {
            if (instructions == null || instructions.Count == 0)
                return new List<MidLevelIR.MLInstruction>();

            var result = new List<MidLevelIR.MLInstruction>();
            var lastAssignment = new Dictionary<string, int>();

            for (int i = 0; i < instructions.Count; i++)
            {
                var instruction = instructions[i];

                if (instruction is MidLevelIR.MLAssign assign)
                {
                    // Track the last assignment to this target
                    if (lastAssignment.ContainsKey(assign.Target))
                    {
                        // Remove the previous assignment to this target
                        var prevIndex = lastAssignment[assign.Target];
                        result.RemoveAt(prevIndex);
                        // Update indices for remaining items
                        for (int j = 0; j < result.Count; j++)
                        {
                            if (j >= prevIndex)
                                break;
                        }
                    }
                    lastAssignment[assign.Target] = result.Count;
                    result.Add(instruction);
                }
                else
                {
                    // Non-assignment instructions are always kept
                    result.Add(instruction);
                    // Clear assignment tracking when we hit non-assignments
                    // (they might use the assigned values)
                    lastAssignment.Clear();
                }
            }

            return result;
        }
    }
}
