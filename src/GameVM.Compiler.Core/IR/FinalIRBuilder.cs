using GameVM.Compiler.Core.IR.Interfaces;

namespace GameVM.Compiler.Core.IR
{
    public class FinalIRBuilder : IIRTransformer<LowLevelIR, FinalIR>
    {
        public FinalIR Transform(LowLevelIR input)
        {
            // TODO: Implement the actual transformation
            return new FinalIR { SourceFile = input.SourceFile };
        }
    }
}
