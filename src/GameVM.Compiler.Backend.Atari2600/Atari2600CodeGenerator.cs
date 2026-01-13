using System;
using System.Collections.Generic;
using GameVM.Compiler.Core.IR;
using GameVM.Compiler.Core.IR.Interfaces;
using GameVM.Compiler.Core.Enums;

namespace GameVM.Compiler.Backend.Atari2600
{
    public class Atari2600CodeGenerator : ICodeGenerator
    {
        public byte[] Generate(LowLevelIR ir, CodeGenOptions options)
        {
            return GenerateBytecode(ir, options);
        }

        public byte[] GenerateBytecode(LowLevelIR ir, CodeGenOptions options)
        {
            var assemblyLines = new List<string>();

            foreach (var instr in ir.Instructions)
            {
                if (instr is LowLevelIR.LLLoad load)
                {
                    assemblyLines.Add($"LDA #{load.Value}");
                }
                else if (instr is LowLevelIR.LLStore store)
                {
                    assemblyLines.Add($"STA {store.Address}");
                }
                else if (instr is LowLevelIR.LLCall call)
                {
                    assemblyLines.Add($"JSR {call.Label}");
                }
                else if (instr is LowLevelIR.LLLabel label)
                {
                    assemblyLines.Add($"{label.Name}:");
                }
            }

            var emitter = new M6502Emitter();
            var code = emitter.Emit(assemblyLines);
            
            var rom = new byte[4096];
            Array.Copy(code, 0, rom, 0, Math.Min(code.Length, rom.Length - 6));

            // Reset vector at $FFFC-$FFFD (points to $F000 which is where we load)
            rom[4090] = 0x00; // IRQ
            rom[4091] = 0xF0;
            rom[4092] = 0x00; // Reset
            rom[4093] = 0xF0;
            rom[4094] = 0x00; // NMI
            rom[4095] = 0xF0;

            return rom;
        }
    }
}
