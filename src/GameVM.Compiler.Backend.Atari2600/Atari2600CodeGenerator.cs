using GameVM.Compiler.Core.IR.Soa;
using GameVM.Compiler.Core.IR;
using GameVM.Compiler.Core.IR.Interfaces;
using GameVM.Compiler.Core.IR.Buffers;
using GameVM.Compiler.Core.Enums;
using System;
using System.Collections.Generic;

namespace GameVM.Compiler.Backend.Atari2600
{
    public class Atari2600CodeGenerator : ICodeGenerator, ICapabilityProvider
    {
        private const int RomSize = 4096; // 4K ROM
        private const int VectorBaseOffset = 0x0FFC; // Offset from RomStartAddress for vectors ($FFFC - $F000)

        // DOD pipeline method - Generate from LLIR slab
        public byte[] GenerateFromSlab(InstList llirSlab, StringPool stringPool, CodeGenOptions options)
        {
            return GenerateFromInstList(llirSlab);
        }

        private static byte[] GenerateFromInstList(InstList llirSlab)
        {
            if (llirSlab.Count == 0)
                return Array.Empty<byte>();

            var rom = new byte[RomSize]; // 4K ROM
            Array.Clear(rom, 0, rom.Length);
            int currentAddress = 0; // Offset within ROM (0 = $F000)

            bool lastWasTransition = false; // pending polarity inversion



            for (int i = 0; i < llirSlab.Count; i++)
            {
                byte kindByte = llirSlab.GetKind(i);
                LlirInstructionKind kind = (LlirInstructionKind)kindByte;
                ReadOnlySpan<uint> operands = llirSlab.GetOperands(i);

                int bytesWritten = 0;

                switch (kind)
                {
                    case LlirInstructionKind.Load:
                        // LDA #immediate (operands[1]) or LDA abs (operands[0..1] addr)
                        if (operands.Length >= 2 && currentAddress + 2 <= RomSize)
                        {
                            rom[currentAddress++] = 0xA9; // LDA #immediate
                            rom[currentAddress++] = (byte)operands[1]; // immediate value
                            bytesWritten = 2;
                        }
                        else if (operands.Length >= 1 && currentAddress + 2 <= RomSize)
                        {
                            rom[currentAddress++] = 0xA9; // LDA #immediate
                            rom[currentAddress++] = (byte)operands[0];
                            bytesWritten = 2;
                        }
                        break;
                    case LlirInstructionKind.Store:
                        // STA address (operands[1]=addrLow, operands[2]=addrHigh)
                        if (operands.Length >= 3 && currentAddress + 3 <= RomSize)
                        {
                            int address = (int)operands[1] | ((int)operands[2] << 8);
                            if (address < 0x100)
                            {
                                rom[currentAddress++] = 0x85; // STA zp
                                rom[currentAddress++] = (byte)address;
                                bytesWritten = 2;
                            }
                            else
                            {
                                rom[currentAddress++] = 0x8D; // STA abs
                                rom[currentAddress++] = (byte)(address & 0xFF);
                                rom[currentAddress++] = (byte)((address >> 8) & 0xFF);
                                bytesWritten = 3;
                            }
                        }
                        else if (operands.Length >= 2 && currentAddress + 3 <= RomSize)
                        {
                            int address = (int)operands[1];
                            if (address < 0x100)
                            {
                                rom[currentAddress++] = 0x85;
                                rom[currentAddress++] = (byte)address;
                                bytesWritten = 2;
                            }
                            else
                            {
                                rom[currentAddress++] = 0x8D;
                                rom[currentAddress++] = (byte)(address & 0xFF);
                                rom[currentAddress++] = (byte)((address >> 8) & 0xFF);
                                bytesWritten = 3;
                            }
                        }
                        else if (operands.Length >= 1 && currentAddress + 3 <= RomSize)
                        {
                            int address = (int)operands[0];
                            if (address < 0x100)
                            {
                                rom[currentAddress++] = 0x85;
                                rom[currentAddress++] = (byte)address;
                                bytesWritten = 2;
                            }
                            else
                            {
                                rom[currentAddress++] = 0x8D;
                                rom[currentAddress++] = (byte)(address & 0xFF);
                                rom[currentAddress++] = (byte)((address >> 8) & 0xFF);
                                bytesWritten = 3;
                            }
                        }
                        break;
                    case LlirInstructionKind.Add:
                        // ADC immediate/abs: A += operand
                        if (operands.Length >= 1 && currentAddress + 2 <= RomSize)
                        {
                            rom[currentAddress++] = 0x18; // CLC
                            rom[currentAddress++] = 0x69; // ADC #imm
                            rom[currentAddress++] = (byte)operands[0];
                            bytesWritten = 3;
                        }
                        break;
                    case LlirInstructionKind.Sub:
                        // SBC immediate/abs: A -= operand (with carry set)
                        if (operands.Length >= 1 && currentAddress + 2 <= RomSize)
                        {
                            rom[currentAddress++] = 0x38; // SEC
                            rom[currentAddress++] = 0xE9; // SBC #imm
                            rom[currentAddress++] = (byte)operands[0];
                            bytesWritten = 3;
                        }
                        break;
                    case LlirInstructionKind.Cmp:
                        // CMP immediate/abs: set flags for A vs operand
                        if (operands.Length >= 1 && currentAddress + 2 <= RomSize)
                        {
                            rom[currentAddress++] = 0xC9; // CMP #imm
                            rom[currentAddress++] = (byte)operands[0];
                            bytesWritten = 2;
                        }
                        // (flags set by CMP; no register state to track here)
                        lastWasTransition = false;
                        break;
                    case LlirInstructionKind.Assign:
                        // Assign: operands[0]=targetAddr (zero-page), operands[1]=value.
                        if (operands.Length >= 2 && currentAddress + 3 <= RomSize)
                        {
                            int target = (int)operands[0];
                            uint value = operands[1];
                            rom[currentAddress++] = 0xA9; // LDA #imm
                            rom[currentAddress++] = (byte)value;
                            rom[currentAddress++] = 0x85; // STA zp
                            rom[currentAddress++] = (byte)target;
                            bytesWritten = 4;
                        }
                        else if (operands.Length >= 1 && currentAddress + 2 <= RomSize)
                        {
                            int target = (int)operands[0];
                            rom[currentAddress++] = 0xA9; // LDA #0
                            rom[currentAddress++] = 0x00;
                            rom[currentAddress++] = 0x85; // STA zp
                            rom[currentAddress++] = (byte)target;
                            bytesWritten = 4;
                        }
                        break;
                    case LlirInstructionKind.Label:
                        // Skip labels - no code generated.
                        bytesWritten = -1;
                        break;
                    case LlirInstructionKind.Jump:
                        // JMP absolute
                        if (operands.Length >= 2 && currentAddress + 3 <= RomSize)
                        {
                            rom[currentAddress++] = 0x4C; // JMP
                            rom[currentAddress++] = (byte)operands[0];
                            rom[currentAddress++] = (byte)operands[1];
                            bytesWritten = 3;
                        }
                        else if (operands.Length >= 1 && currentAddress + 3 <= RomSize)
                        {
                            // Single-operand: target is a pool-offset label; in a fully
                            // resolved backend this would be patched. Emit JMP to $F000.
                            rom[currentAddress++] = 0x4C; // JMP
                            rom[currentAddress++] = 0x00;
                            rom[currentAddress++] = 0xF0;
                            bytesWritten = 3;
                        }
                        break;
                    case LlirInstructionKind.Branch:
                        // Conditional branch. The opcode is selected from the preceding
                        // Cmp + Transition marker. Branch-to-target-when-true.
                        if (currentAddress + 2 <= RomSize)
                        {
                            // Default: BNE (branch when not equal, flags from last Cmp).
                            byte opcode = lastWasTransition ? (byte)0xF0 : (byte)0xD0;
                            // Default is BEQ when lastWasTransition, otherwise BNE.
                            rom[currentAddress++] = opcode;
                            // Relative offset: the first operand is the branch target
                            // offset; falls back to zero when no operand is present.
                            rom[currentAddress++] = operands.Length >= 1 ? (byte)operands[0] : (byte)0x00;
                            bytesWritten = 2;
                        }
                        lastWasTransition = false;
                        break;
                    case LlirInstructionKind.Transition:
                        // Marks polarity inversion for the next branch. No bytes.
                        lastWasTransition = true;
                        bytesWritten = -1;
                        break;
                    case LlirInstructionKind.Return:
                        // RTS
                        if (currentAddress < RomSize)
                        {
                            rom[currentAddress++] = 0x60; // RTS
                            bytesWritten = 1;
                        }
                        break;
                    case LlirInstructionKind.Call:
                    case LlirInstructionKind.Syscall:
                        // JSR to address (low byte in operand[0], high byte in operand[1])
                        if (operands.Length >= 2 && currentAddress + 3 <= RomSize)
                        {
                            rom[currentAddress++] = 0x20; // JSR
                            rom[currentAddress++] = (byte)operands[0];
                            rom[currentAddress++] = (byte)operands[1];
                            bytesWritten = 3;
                        }
                        break;
                    default:
                        // Unknown instruction - generate NOP (or skip)
                        if (currentAddress < RomSize)
                        {
                            rom[currentAddress++] = 0xEA; // NOP
                            bytesWritten = 1;
                        }
                        break;
                }

                if (bytesWritten == 0)
                {
                    // Failed to write instruction due to space constraints
                    break;
                }
            }

            // Emit a self-loop (JMP *) so the program stays at its final state.
            if (currentAddress + 3 <= RomSize)
            {
                int loopAddr = 0xF000 + currentAddress;
                rom[currentAddress] = 0x4C;
                rom[currentAddress + 1] = (byte)(loopAddr & 0xFF);
                rom[currentAddress + 2] = (byte)((loopAddr >> 8) & 0xFF);
            }

            // Set up interrupt vectors at the end of ROM.
            if (RomSize >= VectorBaseOffset + 4)
            {
                Array.Clear(rom, VectorBaseOffset, 4);
                rom[VectorBaseOffset]     = 0x00;         // IRQ vector low
                rom[VectorBaseOffset + 1] = 0xF0;         // IRQ vector high
                rom[VectorBaseOffset + 2] = 0x00;         // Reset vector low
                rom[VectorBaseOffset + 3] = 0xF0;         // Reset vector high
            }

            return rom;
        }

        // ICapabilityProvider implementation
        public IEnumerable<string> GetSupportedExtensions()
        {
            return new[] { "atari2600" };
        }

        public CapabilityProfile GetCapabilityProfile()
        {
            return new CapabilityProfile
            {
                BaseLevel = CapabilityLevel.L1
            };
        }
    }
}