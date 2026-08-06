using GameVM.Compiler.Core.IR.Soa;
using GameVM.Compiler.Core.IR.Buffers;

namespace GameVM.Compiler.Application.Services
{
    /// <summary>
    /// Interface for low-level IR optimizers.
    /// </summary>
    public interface ILowLevelOptimizer
    {
        /// <summary>
        /// Optimizes the LLIR slab (DOD pipeline).
        /// </summary>
        /// <param name="llirSlab">The LLIR slab to optimize</param>
        /// <param name="stringPool">String pool for identifier resolution</param>
        /// <param name="optimizationLevel">Optimization level to apply</param>
        /// <returns>Optimized LLIR slab</returns>
        InstList OptimizeSlab(InstList llirSlab, StringPool stringPool, Core.Enums.OptimizationLevel optimizationLevel);
    }
}