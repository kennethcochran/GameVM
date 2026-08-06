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
    }
}