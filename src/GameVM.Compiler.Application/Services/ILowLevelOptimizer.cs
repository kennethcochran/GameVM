using GameVM.Compiler.Core.IR;

namespace GameVM.Compiler.Application.Services
{
public interface ILowLevelOptimizer
{
    /// <summary>
    /// Optimizes the OOP LowLevelIR (legacy interface).
    /// </summary>
    LowLevelIR Optimize(LowLevelIR ir, Core.Enums.OptimizationLevel optimizationLevel);

    /// <summary>
    /// Optimizes the given LLIR slab using linear iteration and switch-based processing (DOD interface).
    /// </summary>
    /// <param name="llirSlab">The LLIR slab as a uint[] array.</param>
    /// <param name="optimizationLevel">The optimization level to apply.</param>
    /// <returns>The optimized LLIR slab as a uint[] array.</returns>
    uint[] OptimizeSlab(uint[] llirSlab, Core.Enums.OptimizationLevel optimizationLevel);
}
}
