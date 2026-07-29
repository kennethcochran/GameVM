using System;
using System.Collections.Generic;
using GameVM.Compiler.Core.IR;
using GameVM.Compiler.Core.IR.Interfaces;
using GameVM.Compiler.Core.IR.Slab;
using GameVM.Compiler.Core.IR.SlabProcessing;
using static GameVM.Compiler.Core.IR.Slab.InstructionMetadataFlags;
using static GameVM.Compiler.Core.IR.Slab.InstructionMetadata;
using GameVM.Compiler.Core.Utilities;

namespace GameVM.Compiler.Backend.Atari2600
{
    public class MidToLowLevelTransformer : IIRTransformer<MidLevelIR, LowLevelIR>
    {
        private readonly Dictionary<string, string> _addressMap = new(StringComparer.OrdinalIgnoreCase);
        private int _nextAvailableAddress = 0x80;
        private readonly ArenaAllocator _arena;

        private void InitializeAddressMap()
        {
            _addressMap.Clear();
            _nextAvailableAddress = 0x80;
            
            // Pre-fill known registers
            _addressMap["COLUBK"] = "$09";
            _addressMap["COLUPF"] = "$08";
            _addressMap["COLUP0"] = "$06";
            _addressMap["COLUP1"] = "$07";
        }

        public MidToLowLevelTransformer(ArenaAllocator arena)
        {
            _arena = arena ?? throw new ArgumentNullException(nameof(arena));
        }

        public MidToLowLevelTransformer()
        {
            _arena = new ArenaAllocator();
        }

        public LowLevelIR Transform(MidLevelIR mlir)
        {
            // Always start with a fresh address map for each compilation
            InitializeAddressMap();
            
            var llir = new LowLevelIR { SourceFile = mlir.SourceFile };
            llir.Modules.Clear();
            
            // Process each module in the input
            foreach (var module in mlir.Modules)
            {
                var outputModule = ProcessModule(module);
                llir.Modules.Add(outputModule);
                
                // Flatten all function instructions to top level for test compatibility
                foreach (var function in outputModule.Functions)
                {
                    // Add function label to top level
                    llir.Instructions.Add(new LowLevelIR.LLLabel { Name = function.Name });
                    
                    // Add function instructions to top level
                    foreach (var instr in function.Instructions)
                    {
                        llir.Instructions.Add(instr);
                    }
                }
            }
            
            return llir;
        }

        /// <summary>
        /// DOD interface: Transforms MLIR slab (uint[]) to LLIR object hierarchy.
        /// Uses linear iteration with switch-based dispatch on decoded metadata.
        /// </summary>
        public LowLevelIR TransformSlab(uint[] mlirSlab)
        {
            if (mlirSlab == null || mlirSlab.Length < SlabHeader.HeaderIndex.Length)
            {
                throw new ArgumentException("Invalid MLIR slab: too small or null", nameof(mlirSlab));
            }

            var header = SlabHeader.Read(mlirSlab);
            if (!header.HasValidMagic())
            {
                throw new ArgumentException("Invalid MLIR slab: invalid magic number");
            }

            if (header.IrStage != 2) // Stage 2 = MLIR
            {
                throw new ArgumentException($"Expected MLIR slab (stage 2), got stage {header.IrStage}");
            }

            InitializeAddressMap();
            _arena.Reset();

            var llir = new LowLevelIR { SourceFile = "slab" };
            llir.Modules.Clear();

            int offset = SlabHeader.HeaderIndex.Length;
            int functionCount = 0;

            // Write new header with placeholder function count
            var newHeaderOffset = _arena.Allocate(SlabHeader.HeaderIndex.Length);
            var headerData = SlabHeader.ForStage(2, 0);
            var headerBytes = new uint[SlabHeader.HeaderIndex.Length];
            headerData.WriteTo(headerBytes);
            _arena.Write(newHeaderOffset, headerBytes);

            // Process each function in the MLIR slab
            while (offset < mlirSlab.Length)
            {
                var metadata = mlirSlab[offset];
                var size = InstructionMetadata.DecodeSize(metadata);
                var kind = InstructionMetadata.DecodeKind(metadata);

                if (size == 0 || offset + size > mlirSlab.Length)
                    break;

                if (kind == InstructionMetadataFlags.METHOD_DECLARATION)
                {
                    ProcessFunctionFromSlab(mlirSlab, offset, size, llir, ref functionCount);
                }

                offset += size;
            }

            // Update header with actual function count
            var finalHeader = SlabHeader.ForStage(2, (uint)functionCount, 0);
            var finalHeaderData = new uint[SlabHeader.HeaderIndex.Length];
            finalHeader.WriteTo(finalHeaderData);
            _arena.Write(newHeaderOffset, finalHeaderData);

            return llir;
        }

        private LowLevelIR.LLModule ProcessModule(MidLevelIR.MLModule module)
        {
            var outputModule = new LowLevelIR.LLModule { Name = module.Name };

            // Process each function in the input module
            foreach (var mlFunc in module.Functions)
            {
                var llFunc = ProcessFunction(mlFunc);
                outputModule.Functions.Add(llFunc);
            }

            return outputModule;
        }

        private LowLevelIR.LLFunction ProcessFunction(MidLevelIR.MLFunction mlFunc)
        {
            var llFunc = new LowLevelIR.LLFunction { Name = mlFunc.Name };
            
            // Process each instruction in the function
            foreach (var instr in mlFunc.Instructions)
            {
                ProcessInstruction(instr, llFunc);
            }
            
            return llFunc;
        }

        private void ProcessInstruction(MidLevelIR.MLInstruction instr, LowLevelIR.LLFunction llFunc)
        {
            LowLevelIR.LLInstruction? llInstr = null;
            
            switch (instr)
            {
                case MidLevelIR.MLLabel label:
                    llInstr = new LowLevelIR.LLLabel { Name = label.Name };
                    break;
                case MidLevelIR.MLAssign assign:
                    ProcessAssignment(assign, llFunc);
                    return; // Assignment adds multiple instructions, handled separately
                case MidLevelIR.MLCall call:
                    llInstr = new LowLevelIR.LLCall { Label = call.Name };
                    break;
                case MidLevelIR.MLBranch branch:
                    llInstr = new LowLevelIR.LLJump { Target = branch.Target, Condition = branch.Condition };
                    break;
            }
            
            if (llInstr != null)
            {
                llFunc.Instructions.Add(llInstr);
            }
        }

        private void ProcessAssignment(MidLevelIR.MLAssign assign, LowLevelIR.LLFunction llFunc)
        {
            var targetAddr = MapToAddress(assign.Target);
            llFunc.Instructions.Add(new LowLevelIR.LLLoad { Register = "A", Value = assign.Source });
            llFunc.Instructions.Add(new LowLevelIR.LLStore { Address = targetAddr, Register = "A" });
        }

        private string MapToAddress(string target)
        {
            if (_addressMap.TryGetValue(target, out var addr))
                return addr;

            if (target.StartsWith('$'))
                return target;

            // Allocate new address
            var newAddr = $"${_nextAvailableAddress:X2}";
            _addressMap[target] = newAddr;
            _nextAvailableAddress++;
            return newAddr;
        }

        private void ProcessFunctionFromSlab(uint[] mlirSlab, int funcOffset, int funcSize, LowLevelIR llir, ref int functionCount)
        {
            var funcNameHash = mlirSlab[funcOffset + 1];
            var bodyOffset = funcOffset + 2;

            // Store function name
            var nameHashSlot = _arena.Allocate(1);
            _arena.Write(nameHashSlot, funcNameHash);

            int bodyEndOffset = funcOffset + funcSize;
            int currentOffset = bodyOffset;

            // Get or create the first function in the LLIR
            var llFunc = new LowLevelIR.LLFunction { Name = "main" };
            llir.Modules.Add(new LowLevelIR.LLModule { Name = "module", Functions = new List<LowLevelIR.LLFunction> { llFunc } });

            while (currentOffset < bodyEndOffset && currentOffset < mlirSlab.Length)
            {
                var metadata = mlirSlab[currentOffset];
                var size = InstructionMetadata.DecodeSize(metadata);
                var kind = InstructionMetadata.DecodeKind(metadata);

                if (size == 0 || currentOffset + size > mlirSlab.Length)
                    break;

                ProcessSlabInstruction(mlirSlab, currentOffset, kind, llFunc);
                currentOffset += size;
            }

            functionCount++;
        }

        private void ProcessSlabInstruction(uint[] slab, int offset, byte kind, LowLevelIR.LLFunction llFunc)
        {
            switch (kind)
            {
                case MLIR_LABEL:
                    ProcessLabelFromSlab(slab, offset, llFunc);
                    break;
                case MLIR_ASSIGN:
                    ProcessAssignmentFromSlab(slab, offset, llFunc);
                    break;
                case MLIR_CALL:
                    ProcessCallFromSlab(slab, offset, llFunc);
                    break;
                case MLIR_BRANCH:
                    ProcessJumpFromSlab(slab, offset, llFunc);
                    break;
                default:
                    CopyInstruction(slab, offset);
                    break;
            }
        }

        private void ProcessLabelFromSlab(uint[] slab, int offset, LowLevelIR.LLFunction llFunc)
        {
            var labelName = $"label_{slab[offset + 1]:X}";
            
            var instrSize = 2;
            var startOffset = _arena.Allocate(instrSize);
            _arena.Write(startOffset, Encode(InstructionMetadataFlags.LLIR_LABEL, (byte)instrSize, 1), (uint)labelName.GetHashCode());
            
            llFunc.Instructions.Add(new LowLevelIR.LLLabel { Name = labelName });
        }

        private void ProcessAssignmentFromSlab(uint[] slab, int offset, LowLevelIR.LLFunction llFunc)
        {
            var targetHash = slab[offset + 1];
            var sourceHash = slab[offset + 2];
            
            var loadSize = (byte)2;
            var loadOffset = _arena.Allocate(loadSize);
            _arena.Write(loadOffset, Encode(InstructionMetadataFlags.LLIR_LOAD, loadSize, 2), 
                (uint)"A".GetHashCode(), sourceHash);
            
            var storeSize = (byte)3;
            var storeOffset = _arena.Allocate(storeSize);
            _arena.Write(storeOffset, Encode(InstructionMetadataFlags.LLIR_STORE, storeSize, 3), 
                targetHash, (uint)"A".GetHashCode());
            
            llFunc.Instructions.Add(new LowLevelIR.LLLoad { Register = "A", Value = sourceHash.ToString() });
            llFunc.Instructions.Add(new LowLevelIR.LLStore { Address = targetHash.ToString(), Register = "A" });
        }

        private void ProcessCallFromSlab(uint[] slab, int offset, LowLevelIR.LLFunction llFunc)
        {
            var callNameHash = slab[offset + 1];
            
            var instrSize = (byte)2;
            var callOffset = _arena.Allocate(instrSize);
            _arena.Write(callOffset, Encode(InstructionMetadataFlags.LLIR_CALL, instrSize, 1), callNameHash);
            
            llFunc.Instructions.Add(new LowLevelIR.LLCall { Label = $"call_{callNameHash:X}" });
        }

        private void ProcessJumpFromSlab(uint[] slab, int offset, LowLevelIR.LLFunction llFunc)
        {
            var conditionHash = slab[offset + 1];
            var targetHash = slab[offset + 2];
            
            var instrSize = 3;
            var jumpOffset = _arena.Allocate(instrSize);
            _arena.Write(jumpOffset, Encode(InstructionMetadataFlags.LLIR_JUMP, (byte)instrSize, 2), conditionHash, targetHash);
            
            llFunc.Instructions.Add(new LowLevelIR.LLJump { Target = $"jump_{targetHash:X}", Condition = $"cond_{conditionHash:X}" });
        }

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

        private static uint Encode(byte kind, byte size, byte argCount, bool isTerminator = false, bool hasDiagnostic = false)
        {
            return InstructionMetadata.Encode(kind, size, argCount, isTerminator, hasDiagnostic);
        }
    }
}
