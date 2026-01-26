using GameVM.Compiler.Core.IR.Interfaces;

namespace GameVM.Compiler.Core.IR
{
    public class MlirBuilder : IIRTransformer<HighLevelIR, MidLevelIR>
    {
        public MidLevelIR Transform(HighLevelIR input)
        {
            var transformer = new Transformers.HlirToMlirTransformer();
            return transformer.Transform(input);
        }
    }
}
