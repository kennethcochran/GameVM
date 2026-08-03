using GameVM.Compiler.Application.Services;
using GameVM.Compiler.Core.IR.Slab;
using GameVM.Compiler.Core.IR.SlabProcessing;
using GameVM.Compiler.Core.IR.Buffers;
using GameVM.Compiler.Core.Enums;
using static GameVM.Compiler.Core.IR.Slab.InstructionMetadataFlags;

namespace GameVM.Compiler.Optimizers.LowLevel
{
    /// <summary>
    /// DOD low-level optimizer that processes LLIR slabs using linear iteration.
    /// Implements basic peephole optimizations like redundant load/store elimination.
    /// </summary>
    public sealed class DefaultLowLevelOptimizer : ILowLevelOptimizer
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
        public uint[] OptimizeSlab(uint[] llirSlab, StringPool stringPool, OptimizationLevel optimizationLevel)
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

            // Reset arena and prepare to build optimized slab
            _arena.Reset();

            // Copy header (we'll update the instruction count later)
            var headerBytes = new uint[SlabHeader.HeaderIndex.Length];
            Array.Copy(llirSlab, 0, headerBytes, 0, SlabHeader.HeaderIndex.Length);
            var headerOffset = _arena.Allocate(SlabHeader.HeaderIndex.Length);
            _arena.Write(headerOffset, headerBytes);

            int instructionCount = 0;
            int offset = SlabHeader.HeaderIndex.Length;

            // Process each instruction
            while (offset < llirSlab.Length)
            {
                var metadata = llirSlab[offset];
                var size = InstructionMetadata.DecodeSize(metadata);
                var kind = InstructionMetadata.DecodeKind(metadata);

                if (size == 0 || offset + size > llirSlab.Length)
                    break;

                // Apply optimizations based on level
                bool skip = false;
                switch (optimizationLevel)
                {
                    case OptimizationLevel.None:
                        // No optimization - copy as-is
                        CopyInstruction(llirSlab, offset);
                        break;
                    case OptimizationLevel.Basic:
                        skip = ApplyBasicOptimizations(llirSlab, ref offset, size, kind);
                        break;
                    case OptimizationLevel.Aggressive:
                        skip = ApplyAggressiveOptimizations(llirSlab, ref offset, size, kind);
                        break;
                }

                if (!skip)
                {
                    instructionCount++;
                }

                // Move to next instruction (whether we copied it or skipped it)
                offset += size;
            }

            // Update header with actual instruction count
            var updatedHeader = SlabHeader.ForStage(3, (uint)instructionCount);
            var updatedHeaderBytes = new uint[SlabHeader.HeaderIndex.Length];
            updatedHeader.WriteTo(updatedHeaderBytes);
            _arena.Write(0, updatedHeaderBytes); // Write at start of arena

            return _arena.ToContiguousArray();
        }

        private bool ApplyBasicOptimizations(uint[] slab, ref int offset, int size, byte kind)
        {
            switch (kind)
            {
                case LLIR_STORE:
                    // Basic optimization: eliminate redundant load-store pairs
                    return EliminateRedundantStore(slab, ref offset, size);
                default:
                    CopyInstruction(slab, offset);
                    return false;
            }
        }

        private bool ApplyAggressiveOptimizations(uint[] slab, ref int offset, int size, byte kind)
        {
            switch (kind)
            {
                case LLIR_LOAD:
                    // Aggressive: eliminate dead loads (simplified)
                    return EliminateDeadLoad(slab, ref offset, size);
                case LLIR_STORE:
                    // Aggressive: eliminate redundant stores
                    return EliminateRedundantStore(slab, ref offset, size);
                default:
                    CopyInstruction(slab, offset);
                    return false;
            }
        }

        private bool EliminateRedundantStore(uint[] slab, ref int offset, int size)
        {
            // Simple check: if this store is preceded by a load to same register with same value
            if (offset < SlabHeader.HeaderIndex.Length + size)
                return false;

            int prevOffset = offset - size;
            if (prevOffset < SlabHeader.HeaderIndex.Length)
                return false;

            var prevMetadata = slab[prevOffset];
            var prevKind = InstructionMetadata.DecodeKind(prevMetadata);

            if (prevKind == LLIR_LOAD)
            {
                // Check if same register and value
                var currReg = slab[offset + 1];
                var currVal = slab[offset + 2];
                var prevReg = slab[prevOffset + 1];
                var prevVal = slab[prevOffset + 2];
                
                if (currReg == prevReg && currVal == prevVal)
                {
                    // Skip this store - it's redundant
                    return true;
                }
            }
            
            CopyInstruction(slab, offset);
            return false;
        }

        private bool EliminateDeadLoad(uint[] slab, ref int offset, int size)
        {
            // Simple dead load elimination: if load is not followed by a store to same register
            if (offset + size * 2 > slab.Length) // Not enough space for load + potential store
            {
                CopyInstruction(slab, offset);
                return false;
            }

            _ = slab[offset];
            var reg = slab[offset + 1];

            // Check if next instruction is a store of the same register
            int nextOffset = offset + size;
            if (nextOffset < slab.Length)
            {
                var nextMetadata = slab[nextOffset];
                var nextSize = InstructionMetadata.DecodeSize(nextMetadata);
                var nextKind = InstructionMetadata.DecodeKind(nextMetadata);
                
                if (nextKind == LLIR_STORE && 
                    nextSize == 3 && // metadata + 2 operands
                    slab[nextOffset + 1] == reg) // Same register
                {
                    // This load's value is immediately stored - not dead
                    CopyInstruction(slab, offset);
                    return false;
                }
            }

            // This load is dead - skip it
            offset += size;
            return true;
        }

        private void CopyInstruction(uint[] slab, int offset)
        {
            if (offset >= slab.Length) return;

            var metadata = slab[offset];
            var size = InstructionMetadata.DecodeSize(metadata);

            if (size == 0) return;

            var destOffset = _arena.Allocate(size);
            var buffer = new uint[size];
            Array.Copy(slab, offset, buffer, 0, size);
            _arena.Write(destOffset, buffer);
        }
    }
}
