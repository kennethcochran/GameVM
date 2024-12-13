using GameVM.Compiler.Core.IR.Interfaces;

namespace GameVM.Compiler.Core.IR.Transformers
{
    public class LowToFinalTransformer : IIRTransformer<LowLevelIR, FinalIR>
    {
        public FinalIR Transform(LowLevelIR input)
        {
            // TODO: Implement actual transformation logic
            return new FinalIR();
        }
    }
}
