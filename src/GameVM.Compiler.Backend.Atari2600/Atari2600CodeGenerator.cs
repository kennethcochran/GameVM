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

            // Process top-level instructions
            foreach (var instr in ir.Instructions)
            {
                ProcessInstruction(instr, assemblyLines);
            }

            // Process instructions within modules and functions
            foreach (var module in ir.Modules)
            {
                foreach (var function in module.Functions)
                {
                    foreach (var instr in function.Instructions)
                    {
                        ProcessInstruction(instr, assemblyLines);
                    }
                }
            }

            var code = M6502Emitter.Emit(assemblyLines);
            
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

        private static void ProcessInstruction(LowLevelIR.LLInstruction instr, List<string> assemblyLines)
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
    }
}
