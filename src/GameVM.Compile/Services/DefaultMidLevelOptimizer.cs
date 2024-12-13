using GameVM.Compiler.Application.Services;
using GameVM.Compiler.Core.IR;

namespace GameVM.Compiler.Services
{
    public class DefaultMidLevelOptimizer : IMidLevelOptimizer
    {
        public MidLevelIR Optimize(MidLevelIR ir, Core.Enums.OptimizationLevel optimizationLevel)
        {
            // TODO: Implement mid-level optimizations
            return ir;
        }
    }
}
