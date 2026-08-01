using GameVM.Compiler.Core.Enums;
using GameVM.Compiler.Core.IR;
using GameVM.Compiler.Core.IR.Interfaces;
using GameVM.Compiler.Core.IR.Slab;
using GameVM.Compiler.Core.IR.SlabProcessing;
using GameVM.Compiler.Core.IR.Buffers;
using System;
using System.Collections.Generic;

namespace GameVM.Compiler.Backend.Atari2600
{
    public class Atari2600CodeGenerator : ICodeGenerator, ICapabilityProvider
    {
        private const int RomSize = 4096; // 4K ROM
        
        private const int VectorBaseOffset = 0x0FFC; // Offset from RomStartAddress for vectors ($FFFC - $F000)

        // Legacy OOP methods - not implemented in DOD pipeline
#pragma warning disable S2325 // Method cannot be static as it implements interface member
        public byte[] Generate(LowLevelIR ir, CodeGenOptions options)
        {
            throw new NotSupportedException("Legacy OOP method. Use GenerateFromSlab for DOD pipeline.");
        }

        public byte[] GenerateBytecode(LowLevelIR ir, CodeGenOptions options)
        {
            throw new NotSupportedException("Legacy OOP method. Use GenerateFromSlab for DOD pipeline.");
        }
#pragma warning restore S2325

        // DOD pipeline method - Generate from LLIR slab
        public byte[] GenerateFromSlab(uint[] llirSlab, StringPool stringPool, CodeGenOptions options)
        {
            return GenerateFromLlirSlab(llirSlab);
        }

        private static byte[] GenerateFromLlirSlab(uint[] llirSlab)
        {
            if (llirSlab == null || llirSlab.Length == 0)
                return Array.Empty<byte>();

            var rom = new byte[RomSize]; // 4K ROM
            Array.Clear(rom, 0, rom.Length);
            var currentAddress = 0; // Offset within ROM (0 = $F000)
            int offset = 0;

            while (offset < llirSlab.Length)
            {
                uint metadata = llirSlab[offset++];
                LlirInstructionKind kind = (LlirInstructionKind)(metadata & 0xFFFF);

                switch (kind)
                {
                    case LlirInstructionKind.Load:
                        if (offset < llirSlab.Length && currentAddress + 2 <= RomSize)
                        {
                            rom[currentAddress++] = 0xA9; // LDA #immediate
                            rom[currentAddress++] = (byte)llirSlab[offset++];
                        }
                        break;
                    case LlirInstructionKind.Store:
                        if (offset + 2 <= llirSlab.Length && currentAddress + 3 <= RomSize)
                        {
                            uint addrLo = llirSlab[offset++];
                            uint addrHi = llirSlab[offset++];
                            int address = (int)(addrLo | (addrHi << 8));
                            
                            if (address < 0x100)
                            {
                                // Zero-page addressing (STA zp)
                                rom[currentAddress++] = 0x85; // STA zp
                                rom[currentAddress++] = (byte)address;
                            }
                            else
                            {
                                // Absolute addressing (STA abs)
                                rom[currentAddress++] = 0x8D; // STA absolute
                                rom[currentAddress++] = (byte)addrLo;
                                rom[currentAddress++] = (byte)addrHi;
                            }
                        }
                        break;
                    case LlirInstructionKind.Call:
                        if (offset + 1 < llirSlab.Length && currentAddress + 3 <= RomSize)
                        {
                            rom[currentAddress++] = 0x20; // JSR absolute
                            rom[currentAddress++] = (byte)llirSlab[offset++];
                            rom[currentAddress++] = (byte)llirSlab[offset++];
                        }
                        break;
                    case LlirInstructionKind.Label:
                        // Skip labels - no code generated
                        break;
                    case LlirInstructionKind.Jump:
                        if (offset < llirSlab.Length && currentAddress + 3 <= RomSize)
                        {
                            rom[currentAddress++] = 0x4C; // JMP absolute
                            rom[currentAddress++] = (byte)llirSlab[offset++];
                            rom[currentAddress++] = (byte)llirSlab[offset++];
                        }
                        break;
                    case LlirInstructionKind.Branch:
                        if (offset < llirSlab.Length && currentAddress + 2 <= RomSize)
                        {
                            rom[currentAddress++] = 0x90; // BCC (generic branch)
                            rom[currentAddress++] = (byte)llirSlab[offset++];
                        }
                        break;
                    case LlirInstructionKind.Return:
                        if (currentAddress < RomSize)
                        {
                            rom[currentAddress++] = 0x60; // RTS
                        }
                        break;
                    case LlirInstructionKind.Syscall:
                        if (offset < llirSlab.Length && currentAddress + 3 <= RomSize)
                        {
                            rom[currentAddress++] = 0x20; // JSR
                            rom[currentAddress++] = (byte)llirSlab[offset++];
                            rom[currentAddress++] = (byte)(llirSlab[offset++] >> 8);
                        }
                        break;
                    default:
                        break;
                }
            }

            // Emit a self-loop (JMP *) so the program stays at its final state.
            // Atari 2600 programs never return; the CPU loops forever.
            if (currentAddress + 3 <= RomSize)
            {
                int loopAddr = 0xF000 + currentAddress; // CPU address of this JMP instruction
                rom[currentAddress] = 0x4C;             // JMP absolute
                rom[currentAddress + 1] = (byte)(loopAddr & 0xFF);         // low byte
                rom[currentAddress + 2] = (byte)((loopAddr >> 8) & 0xFF);  // high byte
            }

            // Set up interrupt vectors at the end of ROM (indices 4092-4095 correspond to $FFFC-$FFFF)
            // Array indices: 0 = $F000, so $FFFC = index 4092 (0xFFFC - 0xF000 = 0x0FFC = 4092)
            if (RomSize >= VectorBaseOffset + 4)
            {
                Array.Clear(rom, VectorBaseOffset, 4);
                rom[VectorBaseOffset]     = 0x00; // IRQ vector low
                rom[VectorBaseOffset + 1] = 0xF0; // IRQ vector high
                rom[VectorBaseOffset + 2] = 0x00; // Reset vector low
                rom[VectorBaseOffset + 3] = 0xF0; // Reset vector high
            }

            return rom;
        }

        // ICapabilityProvider implementation
        public CapabilityProfile GetCapabilityProfile()
        {
            return new CapabilityProfile
            {
                BaseLevel = CapabilityLevel.L1,
                Extensions = new HashSet<string> 
                { 
                    "Ext.Math.Fast",      // DPC chip math acceleration
                    "Ext.Snd.Polyphonic"  // DPC chip polyphonic audio
                },
                InjectedCapabilities = new Dictionary<string, CapabilityLevel>
                {
                    { "Ext.Math.Fast", CapabilityLevel.L3 },  // Fast math operations
                    { "Ext.Snd.Polyphonic", CapabilityLevel.L4 }  // Multi-channel audio
                }
            };
        }

        public IEnumerable<string> GetSupportedExtensions()
        {
            return new[] 
            { 
                "Ext.Math.Fast",      // DPC chip math acceleration
                "Ext.Snd.Polyphonic"  // DPC chip polyphonic audio
            };
        }
    }
}
