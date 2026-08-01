using System;
using System.Collections.Generic;
using GameVM.Compiler.Application.Services;
using GameVM.Compiler.Core.IR;
using GameVM.Compiler.Core.IR.Slab;
using GameVM.Compiler.Core.IR.SlabProcessing;
using GameVM.Compiler.Core.Enums;
using GameVM.Compiler.Core.Utilities;
using GameVM.Compiler.Core.IR.Buffers;

namespace GameVM.Compiler.Optimizers.LowLevel
{
    /// <summary>
    /// Default implementation of low-level IR optimizer.
    /// Performs optimizations such as register allocation, instruction peepholing,
    /// and branch optimization on low-level IR.
    /// </summary>
    public class DefaultLowLevelOptimizer : ILowLevelOptimizer
    {
        private readonly ArenaAllocator _arena;

        public DefaultLowLevelOptimizer()
        {
            _arena = new ArenaAllocator();
        }

        public DefaultLowLevelOptimizer(ArenaAllocator arena)
        {
            _arena = arena ?? throw new ArgumentNullException(nameof(arena));
        }

        /// <summary>
        /// Optimizes the given LLIR slab using linear iteration and switch-based processing.
        /// </summary>
        public uint[] OptimizeSlab(uint[] llirSlab, StringPool stringPool, Core.Enums.OptimizationLevel optimizationLevel)
        {
            if (llirSlab == null || llirSlab.Length < SlabHeader.HeaderIndex.Length)
            {
                throw new ArgumentException("Invalid LLIR slab: too small or null", nameof(llirSlab));
            }

            var header = SlabHeader.Read(llirSlab);
            if (!header.HasValidMagic())
            {
                throw new ArgumentException("Invalid LLIR slab: invalid magic number");
            }

            if (header.IrStage != 3) // Stage 3 = LLIR
            {
                throw new ArgumentException($"Expected LLIR slab (stage 3), got stage {header.IrStage}");
            }

            _arena.Reset();

            int functionCount = 0;
            int offset = SlabHeader.HeaderIndex.Length;

            // Write new header with placeholder function count
            var newHeaderOffset = _arena.Allocate(SlabHeader.HeaderIndex.Length);
            var headerData = SlabHeader.ForStage(3, 0);
            var headerBytes = new uint[SlabHeader.HeaderIndex.Length];
            headerData.WriteTo(headerBytes);
            _arena.Write(newHeaderOffset, headerBytes);

            // Process each instruction in the LLIR slab
            while (offset < llirSlab.Length)
            {
                var metadata = llirSlab[offset];
                var size = InstructionMetadata.DecodeSize(metadata);
                var kind = InstructionMetadata.DecodeKind(metadata);

                if (size == 0 || offset + size > llirSlab.Length)
                    break;

                ProcessInstruction(llirSlab, offset, kind, optimizationLevel);
                if (kind == InstructionMetadataFlags.LLIR_LABEL)
                {
                    functionCount++;
                }

                offset += size;
            }

            // Update header with actual function count
            var finalHeader = SlabHeader.ForStage(3, (uint)functionCount);
            var finalHeaderData = new uint[SlabHeader.HeaderIndex.Length];
            finalHeader.WriteTo(finalHeaderData);
            _arena.Write(newHeaderOffset, finalHeaderData);

            return _arena.ToContiguousArray();
        }

        /// <summary>
        /// Processes a single LLIR instruction using switch-based dispatch on decoded metadata.
        /// This replaces the visitor pattern with data-oriented switch statements.
        /// </summary>
        private void ProcessInstruction(uint[] slab, int offset, byte kind, OptimizationLevel level)
        {
            switch (kind)
            {
                case InstructionMetadataFlags.LLIR_LOAD:
                    ProcessLoad(slab, offset);
                    break;
                case InstructionMetadataFlags.LLIR_STORE:
                    ProcessStore(slab, offset);
                    break;
                case InstructionMetadataFlags.LLIR_LABEL:
                    ProcessLabel(slab, offset);
                    break;
                case InstructionMetadataFlags.LLIR_CALL:
                    ProcessCall(slab, offset);
                    break;
                case InstructionMetadataFlags.LLIR_JUMP:
                    ProcessJump(slab, offset, level);
                    break;
                default:
                    // Unknown instruction - preserve as-is or tombstone based on optimization level
                    if (level >= OptimizationLevel.Aggressive)
                    {
                        TombstoneInstruction();
                    }
                    else
                    {
                        CopyInstruction(slab, offset);
                    }
                    break;
            }
        }

        private void ProcessLoad(uint[] slab, int offset)
        {
            // LLIR_LOAD: [metadata, registerHash, valueHash]
            CopyInstruction(slab, offset);
        }

        private void ProcessStore(uint[] slab, int offset)
        {
            // LLIR_STORE: [metadata, addressHash, registerHash]
            CopyInstruction(slab, offset);
        }

        private void ProcessLabel(uint[] slab, int offset)
        {
            // LLIR_LABEL: [metadata, labelHash]
            CopyInstruction(slab, offset);
        }

        private void ProcessCall(uint[] slab, int offset)
        {
            // LLIR_CALL: [metadata, labelHash]
            CopyInstruction(slab, offset);
        }

        private void ProcessJump(uint[] slab, int offset, OptimizationLevel level)
        {
            // LLIR_JUMP: [metadata, targetLabelHash, conditionHash (0 if unconditional)]
            if (level >= OptimizationLevel.Aggressive && offset + 2 < slab.Length && slab[offset + 2] == 0)
            {
                // Unconditional jump - reserved for future optimization
            }
            CopyInstruction(slab, offset);
        }

        /// <summary>
        /// Tombstones an instruction by replacing it with NOP encoding.
        /// Used for dead code elimination without changing slab offsets.
        /// </summary>
        private void TombstoneInstruction()
        {
            // Write NOP instruction (metadata with kind=0, size=1)
            var nopMetadata = InstructionMetadata.Encode(kind: 0, size: 1, argCount: 0, isTerminator: false, hasDiagnostic: false);
            var destOffset = _arena.Allocate(1);
            _arena.Write(destOffset, nopMetadata);
        }

        /// <summary>
        /// Copies an instruction from source slab to arena at current write position.
        /// Used for out-of-place transformation where we build a new optimized slab.
        /// </summary>
        private void CopyInstruction(uint[] sourceSlab, int offset)
        {
            if (offset >= sourceSlab.Length) return;

            var metadata = sourceSlab[offset];
            var size = InstructionMetadata.DecodeSize(metadata);

            if (size == 0) return;

            var destOffset = _arena.Allocate(size);
            var buffer = new uint[size];
            Array.Copy(sourceSlab, offset, buffer, 0, size);
            _arena.Write(destOffset, buffer);
        }

        /// <summary>
        /// Optimizes the OOP LowLevelIR (legacy interface).
        /// </summary>
        public LowLevelIR Optimize(LowLevelIR ir, OptimizationLevel optimizationLevel)
        {
            ArgumentNullException.ThrowIfNull(ir);

            var optimized = new LowLevelIR
            {
                SourceFile = ir.SourceFile,
                Instructions = new List<LowLevelIR.LLInstruction>()
            };

            if (optimizationLevel >= OptimizationLevel.Basic)
            {
                optimized.Instructions = RemoveRedundantLoadStores(ir.Instructions);
            }
            else
            {
                optimized.Instructions = new List<LowLevelIR.LLInstruction>(ir.Instructions);
            }

            return optimized;
        }

        /// <summary>
        /// Removes redundant load/store sequences where a register is loaded
        /// and immediately stored to the same address without being used.
        /// </summary>
        private static List<LowLevelIR.LLInstruction> RemoveRedundantLoadStores(List<LowLevelIR.LLInstruction> instructions)
        {
            if (instructions == null || instructions.Count == 0)
                return new List<LowLevelIR.LLInstruction>();

            var result = new List<LowLevelIR.LLInstruction>();

            int i = 0;
            while (i < instructions.Count)
            {
                var instruction = instructions[i];

                // Check if this is a load followed immediately by a store of the same register
                if (instruction is LowLevelIR.LLLoad load && i + 1 < instructions.Count)
                {
                    var next = instructions[i + 1];
                    if (next is LowLevelIR.LLStore store && 
                        store.Register == load.Register && 
                        store.Address == load.Value)
                    {
                        // This is redundant: Load from address X, Store back to same address X
                        // Replace with just the store (with direct addressing)
                        result.Add(store);
                        i += 2; // Skip both instructions
                        continue;
                    }
                }

                result.Add(instruction);
                i++;
            }

            return result;
        }
    }
}