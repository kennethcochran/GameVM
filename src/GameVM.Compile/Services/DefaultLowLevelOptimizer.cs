using GameVM.Compiler.Application.Services;
using GameVM.Compiler.Core.IR;

namespace GameVM.Compiler.Services
{
    public class DefaultLowLevelOptimizer : ILowLevelOptimizer
    {
        public LowLevelIR Optimize(LowLevelIR ir, Core.Enums.OptimizationLevel optimizationLevel)
        {
            // TODO: Implement low-level optimizations
            return ir;
        }
    }
}
