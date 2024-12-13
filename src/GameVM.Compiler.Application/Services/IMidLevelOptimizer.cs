using GameVM.Compiler.Core.IR;

namespace GameVM.Compiler.Application.Services
{
    public interface IMidLevelOptimizer
    {
        MidLevelIR Optimize(MidLevelIR ir, Core.Enums.OptimizationLevel optimizationLevel);
    }
}
