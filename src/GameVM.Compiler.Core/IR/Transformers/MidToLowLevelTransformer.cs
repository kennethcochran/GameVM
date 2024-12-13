using GameVM.Compiler.Core.IR.Interfaces;

namespace GameVM.Compiler.Core.IR.Transformers
{
    public class MidToLowLevelTransformer : IIRTransformer<MidLevelIR, LowLevelIR>
    {
        public LowLevelIR Transform(MidLevelIR input)
        {
            // TODO: Implement actual transformation logic
            return new LowLevelIR();
        }
    }
}
