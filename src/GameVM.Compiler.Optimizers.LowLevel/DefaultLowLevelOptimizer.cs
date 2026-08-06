using System;
using GameVM.Compiler.Application.Services;
using GameVM.Compiler.Core.Enums;
using GameVM.Compiler.Core.IR;
using GameVM.Compiler.Core.IR.Buffers;
using GameVM.Compiler.Core.IR.Slab;
using GameVM.Compiler.Core.IR.SlabProcessing;
using GameVM.Compiler.Core.IR.Soa;

namespace GameVM.Compiler.Optimizers.LowLevel
{
    /// <summary>
    /// DOD low-level optimizer that processes LLIR slabs using linear iteration.
    /// Implements basic peephole optimizations like redundant load/store elimination.
    /// </summary>
    public sealed class DefaultLowLevelOptimizer : ILowLevelOptimizer
    {
        private const byte LLIR_LOAD = (byte)LlirInstructionKind.Load;
        private const byte LLIR_STORE = (byte)LlirInstructionKind.Store;

        /// <summary>
        /// Optimizes the given LLIR slab using InstList (DOD SoA format).
        /// </summary>
        public InstList OptimizeSlab(InstList llirSlab, StringPool stringPool, OptimizationLevel optimizationLevel)
        {
            if (llirSlab.Count == 0)
            {
                return llirSlab;
            }

            var builder = new InstListBuilder(llirSlab.Count);

            for (int i = 0; i < llirSlab.Count; i++)
            {
                byte kind = llirSlab.GetKind(i);
                ushort argCount = llirSlab.GetArgCount(i);
                ReadOnlySpan<uint> operands = llirSlab.GetOperands(i);

                bool skip = false;

                if (optimizationLevel != OptimizationLevel.None)
                {
                    switch (kind)
                    {
                        case LLIR_LOAD:
                            if (optimizationLevel == OptimizationLevel.Basic || optimizationLevel == OptimizationLevel.Aggressive)
                            {
                                skip = IsOverwrittenLoad(llirSlab, i, operands);
                            }
                            break;

                        case LLIR_STORE:
                            if (optimizationLevel == OptimizationLevel.Basic || optimizationLevel == OptimizationLevel.Aggressive)
                            {
                                skip = IsRedundantStore(llirSlab, i, operands);
                            }
                            break;
                    }
                }

                if (!skip)
                {
                    builder.Append(kind, InstructionFlag.None, argCount, 0, operands);
                }
            }

            return builder.Build();
        }

        /// <summary>
        /// Legacy API for backward compatibility - converts uint[] to InstList, optimizes, converts back.
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

            if (header.IrStage != 3)
            {
                throw new ArgumentException($"Expected LLIR slab (stage 3), got stage {header.IrStage}");
            }

            var instList = ConvertFromLegacySlab(llirSlab);
            var optimized = OptimizeSlab(instList, stringPool, optimizationLevel);
            return ConvertToLegacySlab(optimized);
        }

        /// <summary>
        /// Checks if a STORE is redundant: the previous instruction is a LOAD with
        /// identical operands (load-store pair reduction — the store writes back
        /// the value that was just loaded, which is a no-op).
        /// </summary>
        private static bool IsRedundantStore(InstList llirSlab, int instIndex, ReadOnlySpan<uint> operands)
        {
            if (operands.Length < 2 || instIndex <= 0)
                return false;

            int prevIndex = instIndex - 1;
            if (llirSlab.GetKind(prevIndex) != LLIR_LOAD)
                return false;

            if (llirSlab.GetArgCount(prevIndex) < 2)
                return false;

            uint prevReg = llirSlab.GetOperand(prevIndex, 0);
            uint prevVal = llirSlab.GetOperand(prevIndex, 1);
            uint currReg = operands[0];
            uint currVal = operands[1];

            return currReg == prevReg && currVal == prevVal;
        }

        /// <summary>
        /// Checks if a LOAD is overwritten: a subsequent LOAD to the same register occurs
        /// before any STORE consumes its value.
        /// </summary>
        private static bool IsOverwrittenLoad(InstList llirSlab, int instIndex, ReadOnlySpan<uint> operands)
        {
            if (operands.Length < 1)
                return false;

            uint reg = operands[0];

            for (int i = instIndex + 1; i < llirSlab.Count; i++)
            {
                byte nextKind = llirSlab.GetKind(i);
                ushort nextArgCount = llirSlab.GetArgCount(i);

                if (nextKind == LLIR_STORE && nextArgCount >= 1)
                {
                    uint storeReg = llirSlab.GetOperand(i, 0);
                    if (storeReg == reg)
                    {
                        return false; // Load's value IS used by this store
                    }
                }
                else if (nextKind == LLIR_LOAD && nextArgCount >= 1)
                {
                    uint loadReg = llirSlab.GetOperand(i, 0);
                    if (loadReg == reg)
                    {
                        return true; // Overwritten before use — dead
                    }
                }
            }

            // No subsequent load or store found — keep the load (might be consumed externally)
            return false;
        }

        /// <summary>
        /// Converts a legacy uint[] LLIR slab to InstList.
        /// </summary>
        private static InstList ConvertFromLegacySlab(uint[] slab)
        {
            var header = SlabHeader.Read(slab);
            int count = (int)header.ElementCount;

            var tags = new byte[count];
            var flags = new ushort[count];
            var argCounts = new ushort[count];
            var fixedOps = new uint[count * InstConstants.MAX_FIXED_OPS];
            var extraOffsets = new uint[count];

            int opIndex = 0;
            int dataOffset = SlabHeader.HeaderIndex.Length;
            for (int i = 0; i < count; i++)
            {
                uint meta = slab[dataOffset + i];
                tags[i] = (byte)(meta & 0xFF);
                argCounts[i] = (ushort)((meta >> 14) & 0x3F);

                for (int j = 0; j < argCounts[i] && j < InstConstants.MAX_FIXED_OPS; j++)
                {
                    fixedOps[i * InstConstants.MAX_FIXED_OPS + j] = slab[dataOffset + count + opIndex + j];
                }
                opIndex += (int)argCounts[i];
                extraOffsets[i] = 0;
            }

            var actualOperands = new uint[opIndex];
            Array.Copy(slab, dataOffset + count, actualOperands, 0, opIndex);

            return new InstList(
                tags,
                flags,
                argCounts,
                fixedOps,
                actualOperands,
                extraOffsets,
                new int[count],
                count,
                (uint)opIndex
            );
        }

        /// <summary>
        /// Converts an InstList to a legacy uint[] LLIR slab.
        /// </summary>
        private static uint[] ConvertToLegacySlab(InstList slab)
        {
            int count = slab.Count;
            int operandCount = 0;
            for (int i = 0; i < count; i++)
            {
                operandCount += slab.GetArgCount(i);
            }

            int headerSize = SlabHeader.HeaderIndex.Length;
            int metaSize = count;
            int totalSize = headerSize + metaSize + operandCount;

            var result = new uint[totalSize];

            var newHeader = SlabHeader.ForStage(3, (uint)count, (uint)operandCount);
            newHeader.WriteTo(result);

            int opIndex = 0;
            for (int i = 0; i < count; i++)
            {
                byte kind = slab.GetKind(i);
                ushort argCount = slab.GetArgCount(i);
                uint meta = (uint)kind | ((uint)argCount << 14);
                result[headerSize + i] = meta;

                var operands = slab.GetOperands(i);
                for (int j = 0; j < argCount && j < operands.Length; j++)
                {
                    result[headerSize + metaSize + opIndex++] = operands[j];
                }
            }

            return result;
        }
    }
}