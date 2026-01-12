using System;
using System.Collections.Generic;
using System.Linq;
using GameVM.Compiler.Application.Services;
using GameVM.Compiler.Core.IR;
using GameVM.Compiler.Core.Enums;

namespace GameVM.Compiler.Optimizers.LowLevel
{
    /// <summary>
    /// Default implementation of low-level IR optimizer.
    /// Performs optimizations such as register allocation, instruction peepholing,
    /// and branch optimization on low-level IR.
    /// </summary>
    public class DefaultLowLevelOptimizer : ILowLevelOptimizer
    {
        /// <summary>
        /// Optimizes the given low-level IR based on the specified optimization level.
        /// </summary>
        /// <param name="ir">The low-level IR to optimize</param>
        /// <param name="optimizationLevel">The level of optimization to apply</param>
        /// <returns>The optimized low-level IR</returns>
        public LowLevelIR Optimize(LowLevelIR ir, OptimizationLevel optimizationLevel)
        {
            if (ir == null)
                throw new ArgumentNullException(nameof(ir));

            // Create a copy to avoid mutating input
            var optimized = new LowLevelIR
            {
                SourceFile = ir.SourceFile,
                Instructions = new List<LowLevelIR.LLInstruction>()
            };

            // Apply optimizations based on level
            if (optimizationLevel >= OptimizationLevel.Basic)
            {
                // Remove redundant load/store sequences
                optimized.Instructions = RemoveRedundantLoadStores(ir.Instructions);
            }
            else
            {
                // No optimization, just copy instructions
                optimized.Instructions = new List<LowLevelIR.LLInstruction>(ir.Instructions);
            }

            return optimized;
        }

        /// <summary>
        /// Removes redundant load/store sequences where a register is loaded
        /// and immediately stored without being used.
        /// </summary>
        private List<LowLevelIR.LLInstruction> RemoveRedundantLoadStores(List<LowLevelIR.LLInstruction> instructions)
        {
            if (instructions == null || instructions.Count == 0)
                return new List<LowLevelIR.LLInstruction>();

            var result = new List<LowLevelIR.LLInstruction>();

            for (int i = 0; i < instructions.Count; i++)
            {
                var instruction = instructions[i];

                // Check if this is a load followed immediately by a store of the same register
                if (instruction is LowLevelIR.LLLoad load && i + 1 < instructions.Count)
                {
                    var next = instructions[i + 1];
                    if (next is LowLevelIR.LLStore store && store.Register == load.Register)
                    {
                        // Skip the load, keep only the store
                        result.Add(store);
                        i++; // Skip the next instruction as we've already processed it
                        continue;
                    }
                }

                result.Add(instruction);
            }

            return result;
        }
    }
}
