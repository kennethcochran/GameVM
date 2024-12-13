using GameVM.Compiler.Core.IR.Interfaces;

namespace GameVM.Compiler.Core.IR
{
    public class MLIRBuilder : IIRTransformer<HighLevelIR, MidLevelIR>
    {
        public MidLevelIR Transform(HighLevelIR input)
        {
            // TODO: Implement the actual transformation
            return new MidLevelIR { SourceFile = input.SourceFile };
        }
    }
}
