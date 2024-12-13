using GameVM.Compiler.Application.Services;
using GameVM.Compiler.Core.IR;

namespace GameVM.Compiler.Services
{
    public class DefaultFinalIROptimizer : IFinalIROptimizer
    {
        public FinalIR Optimize(FinalIR ir, Core.Enums.OptimizationLevel optimizationLevel)
        {
            // TODO: Implement final IR optimizations
            return ir;
        }
    }
}
