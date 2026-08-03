using GameVM.Compiler.Core.IR.Buffers;
using GameVM.Compiler.Core.Enums;

namespace GameVM.Compiler.Application.Services
{
    /// <summary>
    /// Interface for mid-level IR optimizers.
    /// </summary>
    public interface IMidLevelOptimizer
    {
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