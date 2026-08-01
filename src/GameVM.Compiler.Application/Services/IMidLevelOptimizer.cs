using GameVM.Compiler.Core.IR;
using GameVM.Compiler.Core.Enums;
using GameVM.Compiler.Core.IR.Buffers;
using System.Diagnostics.CodeAnalysis;

namespace GameVM.Compiler.Application.Services
{
    /// <summary>
    /// Interface for mid-level IR optimizers.
    /// </summary>
    public interface IMidLevelOptimizer
    {
        /// <summary>
        /// Optimize mid-level IR (legacy OOP interface)
        /// </summary>
        /// <param name="ir">Mid-level IR to optimize</param>
        /// <param name="optimizationLevel">Optimization level to apply</param>
#pragma warning disable S1133
        /// <returns>Optimized mid-level IR</returns>
        [System.Obsolete("Use OptimizeSlab for DOD pipeline. Will be removed in future version.")]
        MidLevelIR Optimize(MidLevelIR ir, OptimizationLevel optimizationLevel);
#pragma warning restore S1133

        /// <summary>
        /// Optimize mid-level IR slab (DOD pipeline)
        /// </summary>
        /// <param name="hlirSlab">HLIR slab to optimize and transform to MLIR</param>
        /// <param name="stringPool">String pool for identifier resolution</param>
        /// <param name="optimizationLevel">Optimization level to apply</param>
        /// <returns>Optimized MLIR slab as uint array</returns>
        uint[] OptimizeSlab(uint[] hlirSlab, StringPool stringPool, OptimizationLevel optimizationLevel);
    }
}