using GameVM.Compiler.Core.IR;

namespace GameVM.Compiler.Application.Services
{
    public interface IFinalIROptimizer
    {
        FinalIR Optimize(FinalIR ir, Core.Enums.OptimizationLevel optimizationLevel);
    }
}
