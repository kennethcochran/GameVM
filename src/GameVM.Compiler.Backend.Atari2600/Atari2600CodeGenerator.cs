using System;
using System.Collections.Generic;
using GameVM.Compiler.Core.IR;
using GameVM.Compiler.Core.IR.Interfaces;
using GameVM.Compiler.Core.Enums;

namespace GameVM.Compiler.Backend.Atari2600
{
    public class Atari2600CodeGenerator : ICodeGenerator
    {
        private readonly M6502Emitter _emitter = new M6502Emitter();

        public byte[] Generate(FinalIR ir, CodeGenOptions options)
        {
            return GenerateBytecode(ir, options);
        }

        public byte[] GenerateBytecode(FinalIR ir, CodeGenOptions options)
        {
            var code = _emitter.Emit(ir.AssemblyLines);
            
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
