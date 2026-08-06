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
            var currentAddress = 0; // Offset within ROM (0 = $F000)

            for (int i = 0; i < llirSlab.Count; i++)
            {
                byte kindByte = llirSlab.GetKind(i);
                LlirInstructionKind kind = (LlirInstructionKind)kindByte;
                ReadOnlySpan<uint> operands = llirSlab.GetOperands(i);

                int bytesWritten = 0;

                switch (kind)
                {
                    case LlirInstructionKind.Load:
                        // LDA #immediate
                        // Operands: [target_register, immediate_value] - use immediate_value
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
                        // STA address
                        // Operands: [target_register, address_low, address_high] - combine to form address
                        if (operands.Length >= 3 && currentAddress + 3 <= RomSize)
                        {
                            int address = (int)operands[1] | ((int)operands[2] << 8);
                            if (address < 0x100)
                            {
                                // Zero-page addressing (STA zp)
                                rom[currentAddress++] = 0x85; // STA zp
                                rom[currentAddress++] = (byte)address;
                                bytesWritten = 2;
                            }
                            else
                            {
                                // Absolute addressing (STA abs)
                                rom[currentAddress++] = 0x8D; // STA absolute
                                rom[currentAddress++] = (byte)(address & 0xFF); // Low byte
                                rom[currentAddress++] = (byte)((address >> 8) & 0xFF); // High byte
                                bytesWritten = 3;
                            }
                        }
                        else if (operands.Length >= 2 && currentAddress + 3 <= RomSize)
                        {
                            // Fallback: treat operands[1] as address (zero-page)
                            int address = (int)operands[1];
                            if (address < 0x100)
                            {
                                // Zero-page addressing (STA zp)
                                rom[currentAddress++] = 0x85; // STA zp
                                rom[currentAddress++] = (byte)address;
                                bytesWritten = 2;
                            }
                            else
                            {
                                // Absolute addressing (STA abs)
                                rom[currentAddress++] = 0x8D; // STA absolute
                                rom[currentAddress++] = (byte)(address & 0xFF); // Low byte
                                rom[currentAddress++] = (byte)((address >> 8) & 0xFF); // High byte
                                bytesWritten = 3;
                            }
                        }
                        else if (operands.Length >= 1 && currentAddress + 3 <= RomSize)
                        {
                            // Legacy fallback: treat operands[0] as address
                            int address = (int)operands[0];
                            if (address < 0x100)
                            {
                                // Zero-page addressing (STA zp)
                                rom[currentAddress++] = 0x85; // STA zp
                                rom[currentAddress++] = (byte)address;
                                bytesWritten = 2;
                            }
                            else
                            {
                                // Absolute addressing (STA abs)
                                rom[currentAddress++] = 0x8D; // STA absolute
                                rom[currentAddress++] = (byte)(address & 0xFF); // Low byte
                                rom[currentAddress++] = (byte)((address >> 8) & 0xFF); // High byte
                                bytesWritten = 3;
                            }
                        }
                        break;
                    case LlirInstructionKind.Call:
                        // JSR absolute
                        if (operands.Length >= 2 && currentAddress + 3 <= RomSize)
                        {
                            rom[currentAddress++] = 0x20; // JSR absolute
                            rom[currentAddress++] = (byte)operands[0];
                            rom[currentAddress++] = (byte)operands[1];
                            bytesWritten = 3;
                        }
                        break;
                    case LlirInstructionKind.Label:
                        // Skip labels - no code generated. Use -1 sentinel to distinguish from real errors.
                        bytesWritten = -1;
                        break;
                    case LlirInstructionKind.Jump:
                        // JMP absolute
                        if (operands.Length >= 2 && currentAddress + 3 <= RomSize)
                        {
                            rom[currentAddress++] = 0x4C; // JMP absolute
                            rom[currentAddress++] = (byte)operands[0];
                            rom[currentAddress++] = (byte)operands[1];
                            bytesWritten = 3;
                        }
                        break;
                    case LlirInstructionKind.Branch:
                        // BCC (generic branch) - relative branch, offset is signed byte
                        if (operands.Length >= 1 && currentAddress + 2 <= RomSize)
                        {
                            rom[currentAddress++] = 0x90; // BCC
                            rom[currentAddress++] = (byte)operands[0]; // offset
                            bytesWritten = 2;
                        }
                        break;
                    case LlirInstructionKind.Return:
                        // RTS
                        if (currentAddress < RomSize)
                        {
                            rom[currentAddress++] = 0x60; // RTS
                            bytesWritten = 1;
                        }
                        break;
                    case LlirInstructionKind.Syscall:
                        // JSR to address (low byte in operand[0], high byte in operand[1])
                        if (operands.Length >= 2 && currentAddress + 3 <= RomSize)
                        {
                            rom[currentAddress++] = 0x20; // JSR
                            rom[currentAddress++] = (byte)operands[0]; // Low byte
                            rom[currentAddress++] = (byte)operands[1]; // High byte
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
                // If we couldn't write the full instruction due to lack of space, break.
                // -1 means "intentionally skipped" (e.g. label) - continue.
                if (bytesWritten == 0)
                {
                    // Failed to write instruction due to space constraints or missing operands
                    break;
                }
                if (bytesWritten == -1)
                {
                    // Skip label
                }
            }

            // Emit a self-loop (JMP *) so the program stays at its final state.
            // Atari 2600 programs never return; the CPU loops forever.
            if (currentAddress + 3 <= RomSize)
            {
                // The CPU address of this JMP instruction is $F000 + currentAddress.
                // We jump to that same address, creating an infinite loop.
                int loopAddr = 0xF000 + currentAddress;
                rom[currentAddress] = 0x4C;             // JMP absolute
                rom[currentAddress + 1] = (byte)(loopAddr & 0xFF);         // low byte
                rom[currentAddress + 2] = (byte)((loopAddr >> 8) & 0xFF);  // high byte
            }

            // Set up interrupt vectors at the end of ROM (indices 4092-4095 correspond to $FFFC-$FFFF)
            // Array indices: 0 = $F000, so $FFFC = index 4092 (0xFFFC - 0xF000 = 0x0FFC = 4092)
            if (RomSize >= VectorBaseOffset + 4)
            {
                // Clear the vector table area
                Array.Clear(rom, VectorBaseOffset, 4);
                // Set both IRQ and Reset vectors to point to start of ROM ($F000)
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
            return new[] 
            { 
                "Ext.Math.Fast",      // DPC chip math acceleration
                "Ext.Snd.Polyphonic"  // DPC chip polyphonic audio
            };
        }

        public CapabilityProfile GetCapabilityProfile()
        {
            return new CapabilityProfile
            {
                BaseLevel = CapabilityLevel.L1,
                Extensions = new HashSet<string>
                {
                    "Ext.Math.Fast",
                    "Ext.Snd.Polyphonic"
                },
                InjectedCapabilities = new Dictionary<string, CapabilityLevel>
                {
                    { "Ext.Math.Fast", CapabilityLevel.L4 },
                    { "Ext.Snd.Polyphonic", CapabilityLevel.L4 }
                }
            };
        }
    }
}