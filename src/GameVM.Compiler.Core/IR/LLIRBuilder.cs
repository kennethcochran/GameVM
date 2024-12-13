using GameVM.Compiler.Core.IR.Interfaces;

namespace GameVM.Compiler.Core.IR
{
    public class LLIRBuilder : IIRTransformer<MidLevelIR, LowLevelIR>
    {
        public LowLevelIR Transform(MidLevelIR input)
        {
            // TODO: Implement the actual transformation
            return new LowLevelIR { SourceFile = input.SourceFile };
        }
    }
}
