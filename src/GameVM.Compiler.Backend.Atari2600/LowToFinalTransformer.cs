using GameVM.Compiler.Core.IR;
using GameVM.Compiler.Core.IR.Interfaces;

namespace GameVM.Compiler.Backend.Atari2600
{
    public class LowToFinalTransformer : IIRTransformer<LowLevelIR, FinalIR>
    {
        public FinalIR Transform(LowLevelIR llir)
        {
            var finalIR = new FinalIR { SourceFile = llir.SourceFile };

            foreach (var instr in llir.Instructions)
            {
                if (instr is LowLevelIR.LLLoad load)
                {
                    finalIR.AssemblyLines.Add($"LDA #{load.Value}");
                }
                else if (instr is LowLevelIR.LLStore store)
                {
                    finalIR.AssemblyLines.Add($"STA {store.Address}");
                }
                else if (instr is LowLevelIR.LLCall call)
                {
                    finalIR.AssemblyLines.Add($"JSR {call.Label}");
                }
                else if (instr is LowLevelIR.LLLabel label)
                {
                    finalIR.AssemblyLines.Add($"{label.Name}:");
                }
            }

            return finalIR;
        }
    }
}
