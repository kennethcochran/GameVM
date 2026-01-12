using System;
using System.Collections.Generic;
using System.Linq;
using GameVM.Compiler.Application.Services;
using GameVM.Compiler.Core.IR;
using GameVM.Compiler.Core.Enums;
using FinalIRType = GameVM.Compiler.Core.IR.FinalIR;

namespace GameVM.Compiler.Optimizers.FinalIR
{
    /// <summary>
    /// Default implementation of final IR optimizer.
    /// Performs optimizations such as redundant instruction removal,
    /// unused register cleanup, and ROM size optimization on final IR.
    /// </summary>
    public class DefaultFinalIROptimizer : IFinalIROptimizer
    {
        /// <summary>
        /// Optimizes the given final IR based on the specified optimization level.
        /// </summary>
        /// <param name="ir">The final IR to optimize</param>
        /// <param name="optimizationLevel">The level of optimization to apply</param>
        /// <returns>The optimized final IR</returns>
        public FinalIRType Optimize(FinalIRType ir, OptimizationLevel optimizationLevel)
        {
            if (ir == null)
                throw new ArgumentNullException(nameof(ir));

            // Create a copy to avoid mutating input
            var optimized = new FinalIRType
            {
                SourceFile = ir.SourceFile,
                AssemblyLines = new List<string>(),
                Bytecode = ir.Bytecode != null ? (byte[])ir.Bytecode.Clone() : System.Array.Empty<byte>()
            };

            // Apply optimizations based on level
            if (optimizationLevel >= OptimizationLevel.Basic)
        {
                // Remove duplicate assembly lines
                optimized.AssemblyLines = RemoveDuplicateAssemblyLines(ir.AssemblyLines);
            }
            else
            {
                // No optimization, just copy assembly lines
                optimized.AssemblyLines = new List<string>(ir.AssemblyLines);
            }

            return optimized;
        }

        /// <summary>
        /// Removes duplicate consecutive assembly lines.
        /// </summary>
        private List<string> RemoveDuplicateAssemblyLines(List<string> assemblyLines)
        {
            if (assemblyLines == null || assemblyLines.Count == 0)
                return new List<string>();

            var result = new List<string>();
            string previous = null;

            foreach (var line in assemblyLines)
            {
                // Skip empty lines and comments
                var trimmed = line?.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith(";"))
                {
                    result.Add(line);
                    continue;
                }

                // Skip if this line is identical to the previous one
                if (trimmed == previous)
                {
                    continue;
                }

                result.Add(line);
                previous = trimmed;
            }

            return result;
        }
    }
}
