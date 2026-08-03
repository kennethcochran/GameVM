using GameVM.Compiler.Application.Services;
using GameVM.Compiler.Core.IR.Slab;
using GameVM.Compiler.Core.IR.SlabProcessing;
using GameVM.Compiler.Core.IR.Buffers;
using GameVM.Compiler.Core.IR.Transformers;
using GameVM.Compiler.Core.Enums;

namespace GameVM.Compiler.Optimizers.MidLevel
{
    /// <summary>
    /// DOD mid-level optimizer that processes MLIR slabs using linear iteration.
    /// Replaces visitor patterns with switch-based instruction processing on decoded metadata.
    /// </summary>
    public sealed class DefaultMidLevelOptimizer : IMidLevelOptimizer
    {
        private readonly ArenaAllocator _arena;
        private readonly HlirSlabToMlirSlabTransformer _hlirSlabToMlirSlabTransformer;

        public DefaultMidLevelOptimizer()
        {
            _arena = new ArenaAllocator();
            _hlirSlabToMlirSlabTransformer = new HlirSlabToMlirSlabTransformer(_arena);
        }

        public DefaultMidLevelOptimizer(ArenaAllocator arena)
        {
            _arena = arena ?? throw new ArgumentNullException(nameof(arena));
            _hlirSlabToMlirSlabTransformer = new HlirSlabToMlirSlabTransformer(_arena);
        }

        /// <summary>
        /// Optimizes the given HLIR slab using linear iteration and switch-based processing.
        /// First transforms HLIR to MLIR, then applies optimization passes on the MLIR slab.
        /// </summary>
        public uint[] OptimizeSlab(uint[] hlirSlab, StringPool stringPool, OptimizationLevel optimizationLevel)
        {
            if (hlirSlab == null || hlirSlab.Length < SlabHeader.HeaderIndex.Length)
            {
                throw new ArgumentException("Invalid HLIR slab: too small or null", nameof(hlirSlab));
            }

            var header = SlabHeader.Read(hlirSlab);
            if (!header.HasValidMagic())
            {
                throw new ArgumentException("Invalid HLIR slab: invalid magic number");
            }

            if (header.IrStage != 1) // Stage 1 = HLIR
            {
                throw new ArgumentException($"Expected HLIR slab (stage 1), got stage {header.IrStage}");
            }

            // Transform HLIR slab to MLIR slab using the dedicated transformer
            var mlirSlab = _hlirSlabToMlirSlabTransformer.Transform(hlirSlab, stringPool);
            
            if (mlirSlab == null || mlirSlab.Length == 0)
            {
                throw new InvalidOperationException("HlirSlabToMlirSlabTransformer returned null or empty slab");
            }

            // Validate that we got an MLIR slab (stage 2)
            var mlirHeader = SlabHeader.Read(mlirSlab);
            if (!mlirHeader.HasValidMagic())
            {
                throw new InvalidOperationException("Transformed slab has invalid magic number");
            }

            if (mlirHeader.IrStage != 2) // Stage 2 = MLIR
            {
                throw new InvalidOperationException($"Expected MLIR slab (stage 2) after transformation, got stage {mlirHeader.IrStage}");
            }

            // Now optimize the MLIR slab
            _arena.Reset();

            int functionCount = 0;
            int offset = SlabHeader.HeaderIndex.Length;

            // Write new header with placeholder function count
            var newHeaderOffset = _arena.Allocate(SlabHeader.HeaderIndex.Length);
            var headerData = SlabHeader.ForStage(2, 0);
            var headerBytes = new uint[SlabHeader.HeaderIndex.Length];
            headerData.WriteTo(headerBytes);
            _arena.Write(newHeaderOffset, headerBytes);

            // Process each instruction in the MLIR slab
            while (offset < mlirSlab.Length)
            {
                var metadata = mlirSlab[offset];
                var size = InstructionMetadata.DecodeSize(metadata);
                var kind = InstructionMetadata.DecodeKind(metadata);

                if (size == 0 || offset + size > mlirSlab.Length)
                    break;

                ProcessInstruction(mlirSlab, offset, kind, optimizationLevel);
                if (kind == InstructionMetadataFlags.MLIR_LABEL)
                {
                    functionCount++;
                }

                offset += size;
            }

            // Update header with actual function count
            var finalHeader = SlabHeader.ForStage(2, (uint)functionCount);
            var finalHeaderData = new uint[SlabHeader.HeaderIndex.Length];
            finalHeader.WriteTo(finalHeaderData);
            _arena.Write(newHeaderOffset, finalHeaderData);

            return _arena.ToContiguousArray();
        }

        /// <summary>
        /// Processes a single MLIR instruction using switch-based dispatch on decoded metadata.
        /// This replaces the visitor pattern with data-oriented switch statements.
        /// </summary>
        private void ProcessInstruction(uint[] slab, int offset, byte kind, OptimizationLevel level)
        {
            // Switch-based instruction processing replaces virtual dispatch/visitor pattern
            switch (kind)
            {
                case InstructionMetadataFlags.MLIR_ASSIGN:
                    ProcessAssign(slab, offset);
                    break;
                case InstructionMetadataFlags.MLIR_LABEL:
                    ProcessLabel(slab, offset);
                    break;
                case InstructionMetadataFlags.MLIR_BRANCH:
                    ProcessBranch(slab, offset, level);
                    break;
                case InstructionMetadataFlags.MLIR_CALL:
                    ProcessCall(slab, offset);
                    break;
                case InstructionMetadataFlags.RETURN_STATEMENT:
                    ProcessReturn(slab, offset);
                    break;
                case InstructionMetadataFlags.VARIABLE_DECLARATION:
                    ProcessVariable(slab, offset);
                    break;
                case InstructionMetadataFlags.BLOCK:
                    ProcessBlock(slab, offset);
                    break;
                case InstructionMetadataFlags.EXPRESSION_STATEMENT:
                    ProcessExpressionStatement(slab, offset);
                    break;
                default:
                    // Unknown instruction - preserve as-is or tombstone
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

        private void ProcessAssign(uint[] slab, int offset)
        {
            // MLIR_ASSIGN: [metadata, targetHash, valueHash]
            if (offset + 2 >= slab.Length) return;

            // For now, just copy the instruction
            CopyInstruction(slab, offset);
        }

        private void ProcessLabel(uint[] slab, int offset)
        {
            // MLIR_LABEL: [metadata, labelHash]
            // For now, keep all labels
            CopyInstruction(slab, offset);
        }

        private void ProcessBranch(uint[] slab, int offset, OptimizationLevel level)
        {
            // MLIR_BRANCH: [metadata, conditionHash, targetLabelHash]
            if (offset + 2 >= slab.Length) return;

            if (level >= OptimizationLevel.Aggressive)
            {
                // Dead code elimination: check if branch is unconditional and target is unreachable
                uint conditionHash = slab[offset + 1];
                if (conditionHash == 0) // Unconditional branch
                {
                    // Could tombstone subsequent unreachable code
                    // For now, keep branch
                }
            }
            CopyInstruction(slab, offset);
        }

        private void ProcessCall(uint[] slab, int offset)
        {
            // MLIR_CALL: [metadata, functionHash, argHashes...]
            CopyInstruction(slab, offset);
        }

        private void ProcessReturn(uint[] slab, int offset)
        {
            CopyInstruction(slab, offset);
        }

        private void ProcessVariable(uint[] slab, int offset)
        {
            CopyInstruction(slab, offset);
        }

        private void ProcessBlock(uint[] slab, int offset)
        {
            // BLOCK: [metadata, statementOffset1, statementOffset2, ...]
            CopyInstruction(slab, offset);
        }

        private void ProcessExpressionStatement(uint[] slab, int offset)
        {
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
    }
}
