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

        // Reset prologue emitted at the start of every ROM (13 bytes):
        //   SEI                  ; disable interrupts (the 6507 has none wired, but be explicit)
        //   CLD                  ; clear decimal mode
        //   LDX #$FF / TXS       ; init stack pointer to $01FF
        //   LDX #$00 / TXA       ; A = 0
        // clear: STA $80,X / INX / BNE clear   ; zero all 256 zero-page bytes
        //                        ($80-$FF RIOT RAM, then $00-$2C TIA: VSYNC=0, VBLANK=0,
        //                        silence, black, motion cleared — all safe at reset)
        private static readonly byte[] ResetPrologue = new byte[]
        {
            0x78,        // SEI
            0xD8,        // CLD
            0xA2, 0xFF,  // LDX #$FF
            0x9A,        // TXS
            0xA2, 0x00,  // LDX #$00
            0x8A,        // TXA
            0x95, 0x80,  // STA $80,X
            0xE8,        // INX
            0xD0, 0xFB   // BNE -5 (back to STA $80,X)
        };

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
            // Reset entry: the vectors point at $F000, so execution starts here.
            Array.Copy(ResetPrologue, rom, ResetPrologue.Length);
            int currentAddress = ResetPrologue.Length; // Offset within ROM (0 = $F000)
            bool lastWasTransition = false; // pending polarity inversion
            // Positions of Return placeholders to patch to JMP <halt-loop> once
            // the self-loop address is known.
            var returnPatchPositions = new List<int>();

            // Collect exit labels (targets of conditional branches). Pass 1 will assign them the self-loop address.
            var exitLabels = CollectExitLabels(llirSlab);

            var (insnAddr, labelOffsets) = ComputeInstructionAddresses(llirSlab, exitLabels);

            for (int i = 0; i < llirSlab.Count; i++)
            {
                byte kindByte = llirSlab.GetKind(i);
                LlirInstructionKind kind = (LlirInstructionKind)kindByte;
                ReadOnlySpan<uint> operands = llirSlab.GetOperands(i);
                int bytesWritten = kind switch
                {
                    LlirInstructionKind.Load => EmitLoad(rom, ref currentAddress, operands),
                    LlirInstructionKind.Store => EmitStore(rom, ref currentAddress, operands),
                    LlirInstructionKind.Add => EmitAdd(rom, ref currentAddress, operands),
                    LlirInstructionKind.Sub => EmitSub(rom, ref currentAddress, operands),
                    LlirInstructionKind.Cmp => EmitCmp(rom, ref currentAddress, operands, ref lastWasTransition),
                    LlirInstructionKind.Assign => EmitAssign(rom, ref currentAddress, operands),
                    LlirInstructionKind.Label => EmitLabel(),
                    LlirInstructionKind.Jump => EmitJump(rom, ref currentAddress, operands, labelOffsets),
                    LlirInstructionKind.Branch => EmitBranch(rom, ref currentAddress, operands, labelOffsets, insnAddr[i], ref lastWasTransition),
                    LlirInstructionKind.Transition => EmitTransition(ref lastWasTransition),
                    LlirInstructionKind.Return => EmitReturn(rom, ref currentAddress, returnPatchPositions),
                    LlirInstructionKind.Call => EmitCall(rom, ref currentAddress, operands),
                    LlirInstructionKind.Syscall => EmitCall(rom, ref currentAddress, operands),
                    _ => EmitNop(rom, ref currentAddress),
                };

                if (bytesWritten == 0)
                {
                    // Failed to write instruction due to space constraints
                    break;
                }
            }
            // Patch every Return to jump to the halt loop, then emit the
            // self-loop (JMP *) so the program stays at its final state.
            int haltAddr = 0xF000 + currentAddress;
            foreach (int pos in returnPatchPositions)
            {
                rom[pos + 1] = (byte)(haltAddr & 0xFF);
                rom[pos + 2] = (byte)((haltAddr >> 8) & 0xFF);
            }
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

        private static (int[] insnAddr, Dictionary<uint, int> labelOffsets) ComputeInstructionAddresses(
            InstList llirSlab, HashSet<uint> exitLabels)
        {
            var insnAddr = new int[llirSlab.Count];
            var labelOffsets = new Dictionary<uint, int>();
            int addr = 0;
            for (int i = 0; i < llirSlab.Count; i++)
            {
                insnAddr[i] = addr;
                byte k = llirSlab.GetKind(i);
                ReadOnlySpan<uint> op = llirSlab.GetOperands(i);
                if ((LlirInstructionKind)k == LlirInstructionKind.Label && op.Length >= 1)
                {
                    uint lbl = op[0];
                    labelOffsets[lbl] = exitLabels.Contains(lbl) ? addr + EmitSize(k, op) : addr;
                }
                addr += EmitSize(k, op);
            }
            return (insnAddr, labelOffsets);
        }

        private static int EmitLoad(byte[] rom, ref int currentAddress, ReadOnlySpan<uint> operands)
        {
            // Two operands: [addrLow, addrHigh] -> LDA zp (0xA5) / LDA abs (0xAD).
            // Single operand: LDA #immediate (0xA9).
            if (operands.Length >= 2 && currentAddress + 3 <= RomSize)
            {
                int address = (int)operands[0] | ((int)operands[1] << 8);
                if (address < 0x100)
                {
                    rom[currentAddress++] = 0xA5; // LDA zp
                    rom[currentAddress++] = (byte)address;
                    return 2;
                }
                rom[currentAddress++] = 0xAD; // LDA abs
                rom[currentAddress++] = (byte)(address & 0xFF);
                rom[currentAddress++] = (byte)((address >> 8) & 0xFF);
                return 3;
            }
            if (operands.Length >= 1 && currentAddress + 2 <= RomSize)
            {
                rom[currentAddress++] = 0xA9; // LDA #immediate
                rom[currentAddress++] = (byte)operands[0];
                return 2;
            }
            return 0;
        }

        private static int EmitSta(byte[] rom, ref int currentAddress, int address)
        {
            if (address < 0x100)
            {
                rom[currentAddress++] = 0x85; // STA zp
                rom[currentAddress++] = (byte)address;
                return 2;
            }
            rom[currentAddress++] = 0x8D; // STA abs
            rom[currentAddress++] = (byte)(address & 0xFF);
            rom[currentAddress++] = (byte)((address >> 8) & 0xFF);
            return 3;
        }

        private static int EmitStore(byte[] rom, ref int currentAddress, ReadOnlySpan<uint> operands)
        {
            // STA address (operands[1]=addrLow, operands[2]=addrHigh)
            if (operands.Length >= 3 && currentAddress + 3 <= RomSize)
                return EmitSta(rom, ref currentAddress, (int)operands[1] | ((int)operands[2] << 8));
            if (operands.Length >= 2 && currentAddress + 3 <= RomSize)
                return EmitSta(rom, ref currentAddress, (int)operands[1]);
            if (operands.Length >= 1 && currentAddress + 3 <= RomSize)
                return EmitSta(rom, ref currentAddress, (int)operands[0]);
            return 0;
        }

        private static int EmitAdd(byte[] rom, ref int currentAddress, ReadOnlySpan<uint> operands)
        {
            // ADC immediate/abs: A += operand
            if (operands.Length >= 1 && currentAddress + 3 <= RomSize)
            {
                rom[currentAddress++] = 0x18; // CLC
                rom[currentAddress++] = 0x69; // ADC #imm
                rom[currentAddress++] = (byte)operands[0];
                return 3;
            }
            return 0;
        }

        private static int EmitSub(byte[] rom, ref int currentAddress, ReadOnlySpan<uint> operands)
        {
            // SBC immediate/abs: A -= operand (with carry set)
            if (operands.Length >= 1 && currentAddress + 3 <= RomSize)
            {
                rom[currentAddress++] = 0x38; // SEC
                rom[currentAddress++] = 0xE9; // SBC #imm
                rom[currentAddress++] = (byte)operands[0];
                return 3;
            }
            return 0;
        }

        private static int EmitCmp(byte[] rom, ref int currentAddress, ReadOnlySpan<uint> operands, ref bool lastWasTransition)
        {
            // Cmp always arrives 1-operand from the MidToLowLevelTransformer:
            // MapArithmetic splits the MLIR 2-operand Cmp into Load(left) + Cmp(right).
            int written;
            if (operands.Length >= 1 && currentAddress + 2 <= RomSize)
            {
                rom[currentAddress++] = 0xC9; // CMP #imm
                rom[currentAddress++] = (byte)operands[0];
                written = 2;
            }
            else if (currentAddress + 1 <= RomSize)
            {
                // Handle unexpected cases
                rom[currentAddress++] = 0xEA; // NOP
                written = 1;
            }
            else
            {
                written = 0;
            }
            lastWasTransition = false;
            return written;
        }

        private static int EmitAssign(byte[] rom, ref int currentAddress, ReadOnlySpan<uint> operands)
        {
            // Assign: operands[0]=targetAddr (zero-page), operands[1]=value.
            if (operands.Length >= 2 && currentAddress + 4 <= RomSize)
            {
                int target = (int)operands[0];
                uint value = operands[1];
                rom[currentAddress++] = 0xA9; // LDA #imm
                rom[currentAddress++] = (byte)value;
                rom[currentAddress++] = 0x85; // STA zp
                rom[currentAddress++] = (byte)target;
                return 4;
            }
            if (operands.Length >= 1 && currentAddress + 4 <= RomSize)
            {
                int target = (int)operands[0];
                rom[currentAddress++] = 0xA9; // LDA #0
                rom[currentAddress++] = 0x00;
                rom[currentAddress++] = 0x85; // STA zp
                rom[currentAddress++] = (byte)target;
                return 4;
            }
            return 0;
        }

        private static int EmitLabel()
        {
            // Skip labels - no code generated.
            return -1;
        }

        private static int EmitJump(byte[] rom, ref int currentAddress, ReadOnlySpan<uint> operands, Dictionary<uint, int> labelOffsets)
        {
            // JMP absolute
            if (operands.Length >= 2 && currentAddress + 3 <= RomSize)
            {
                rom[currentAddress++] = 0x4C; // JMP
                rom[currentAddress++] = (byte)operands[0];
                rom[currentAddress++] = (byte)operands[1];
                return 3;
            }
            if (operands.Length >= 1 && currentAddress + 3 <= RomSize)
            {
                // Single-operand: operand is a label pool-offset; resolve to
                // its ROM byte address ($F000-relative → absolute).
                rom[currentAddress++] = 0x4C; // JMP
                if (labelOffsets.TryGetValue(operands[0], out int targetAddr))
                {
                    int abs = 0xF000 + targetAddr;
                    rom[currentAddress++] = (byte)(abs & 0xFF);
                    rom[currentAddress++] = (byte)((abs >> 8) & 0xFF);
                }
                else
                {
                    // Unresolved target; fall back to ROM start.
                    rom[currentAddress++] = 0x00;
                    rom[currentAddress++] = 0xF0;
                }
                return 3;
            }
            return 0;
        }

        private static int EmitBranch(byte[] rom, ref int currentAddress, ReadOnlySpan<uint> operands,
            Dictionary<uint, int> labelOffsets, int insnAddress, ref bool lastWasTransition)
        {
            // Conditional branch. The opcode is selected from the preceding
            // Cmp + Transition marker. Branch-to-target-when-true.
            int written = 0;
            if (currentAddress + 2 <= RomSize)
            {
                // Default: BNE (branch when not equal, flags from last Cmp).
                // BEQ when lastWasTransition, otherwise BNE.
                rom[currentAddress++] = lastWasTransition ? (byte)0xF0 : (byte)0xD0;
                // Relative offset: resolved label byte-address, else operand[0].
                int displacement = 0;
                if (operands.Length >= 1 && labelOffsets.TryGetValue(operands[0], out int targetAddr))
                    displacement = targetAddr - (insnAddress + 2);
                else if (operands.Length >= 1)
                    displacement = (sbyte)(byte)operands[0];
                rom[currentAddress++] = (byte)(displacement & 0xFF);
                written = 2;
            }
            lastWasTransition = false;
            return written;
        }

        private static int EmitTransition(ref bool lastWasTransition)
        {
            // Marks polarity inversion for the next branch. No bytes.
            lastWasTransition = true;
            return -1;
        }

        private static int EmitReturn(byte[] rom, ref int currentAddress, List<int> patchPositions)
        {
            // The program entry never returns. There is no caller, and the stack
            // was just initialized by the prologue, so an RTS would pop garbage
            // and jump to a random address. The entry path therefore ends in a JMP
            // to the halt loop, patched below once the self-loop address is known.
            // (Function returns via RTS arrive with the B3 calling convention, so
            // until then every Return is the main-path exit.)
            if (currentAddress + 3 <= RomSize)
            {
                patchPositions.Add(currentAddress);
                rom[currentAddress++] = 0x4C; // JMP
                rom[currentAddress++] = 0x00; // patched below
                rom[currentAddress++] = 0x00; // patched below
                return 3;
            }
            return 0;
        }

        private static int EmitCall(byte[] rom, ref int currentAddress, ReadOnlySpan<uint> operands)
        {
            // JSR to address (low byte in operand[0], high byte in operand[1])
            if (operands.Length >= 2 && currentAddress + 3 <= RomSize)
            {
                rom[currentAddress++] = 0x20; // JSR
                rom[currentAddress++] = (byte)operands[0];
                rom[currentAddress++] = (byte)operands[1];
                return 3;
            }
            return 0;
        }

        private static int EmitNop(byte[] rom, ref int currentAddress)
        {
            // Unknown instruction - generate NOP (or skip)
            if (currentAddress < RomSize)
            {
                rom[currentAddress++] = 0xEA; // NOP
                return 1;
            }
            return 0;
        }

        private static HashSet<uint> CollectExitLabels(InstList llirSlab)
        {
            var exitLabels = new HashSet<uint>();
            for (int i = 0; i < llirSlab.Count; i++)
            {
                byte k = llirSlab.GetKind(i);
                if ((LlirInstructionKind)k == LlirInstructionKind.Branch && llirSlab.GetOperands(i).Length >= 1
                    && i > 0 && (LlirInstructionKind)llirSlab.GetKind(i - 1) == LlirInstructionKind.Transition
                    && i > 1 && (LlirInstructionKind)llirSlab.GetKind(i - 2) == LlirInstructionKind.Cmp)
                {
                    exitLabels.Add(llirSlab.GetOperands(i)[0]);
                }
            }
            return exitLabels;
        }

        private static int EmitSize(byte k, ReadOnlySpan<uint> op)
        {
            LlirInstructionKind kind = (LlirInstructionKind)k;
            return kind switch
            {
                LlirInstructionKind.Label => 0,
                LlirInstructionKind.Load when op.Length >= 2 => (int)(op[0] | (op[1] << 8)) < 0x100 ? 2 : 3,
                LlirInstructionKind.Load => 2,
                LlirInstructionKind.Store when op.Length >= 3 => (int)(op[1] | (op[2] << 8)) < 0x100 ? 2 : 3,
                LlirInstructionKind.Store when op.Length >= 2 => (int)op[1] < 0x100 ? 2 : 3,
                LlirInstructionKind.Store when op.Length == 0 => 0,
                LlirInstructionKind.Store => (int)op[0] < 0x100 ? 2 : 3,
                LlirInstructionKind.Add or LlirInstructionKind.Sub => 3,
                LlirInstructionKind.Cmp => op.Length >= 1 ? 2 : 1,
                LlirInstructionKind.Assign => op.Length >= 1 ? 4 : 0,
                LlirInstructionKind.Branch => 2,
                LlirInstructionKind.Jump => 3,
                LlirInstructionKind.Return => 3, // JMP <halt-loop>, patched after emission
                LlirInstructionKind.Call or LlirInstructionKind.Syscall => 3,
                LlirInstructionKind.Transition => 0,
                _ => 1,
            };
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