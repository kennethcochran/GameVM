using GameVM.Compiler.Core.IR;
using GameVM.Compiler.Core.Enums;
using GameVM.Compiler.Core.IR.Buffers;
using System.Diagnostics.CodeAnalysis;

namespace GameVM.Compiler.Application.Services
{
    public interface ILowLevelOptimizer
    {
        /// <summary>
        /// Optimizes the OOP LowLevelIR (legacy interface).
        /// </summary>
        /// <param name="ir">LowLevelIR to optimize</param>
        /// <param name="optimizationLevel">Optimization level to apply</param>
        /// <returns>Optimized LowLevelIR</returns>
        [System.Obsolete("Use OptimizeSlab for DOD pipeline. Will be removed in future version.")]
        LowLevelIR Optimize(LowLevelIR ir, Core.Enums.OptimizationLevel optimizationLevel);

        /// <summary>
        /// Optimizes the given LLIR slab using linear iteration and switch-based processing (DOD interface).
        /// </summary>
        /// <param name="llirSlab">The LLIR slab as a uint[] array.</param>
        /// <param name="stringPool">String pool for identifier resolution</param>
        /// <param name="optimizationLevel">The optimization level to apply.</param>
        /// <returns>The optimized LLIR slab as a uint[] array.</returns>
        uint[] OptimizeSlab(uint[] llirSlab, StringPool stringPool, Core.Enums.OptimizationLevel optimizationLevel);
    }
}