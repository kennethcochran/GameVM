using GameVM.Compiler.Core.IR;

namespace GameVM.Compiler.Application.Services
{
    public interface ILowLevelOptimizer
    {
        LowLevelIR Optimize(LowLevelIR ir, Core.Enums.OptimizationLevel optimizationLevel);
    }
}
