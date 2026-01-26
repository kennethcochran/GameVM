using System;
using System.Collections.Generic;
using GameVM.Compiler.Core.IR;
using GameVM.Compiler.Core.IR.Interfaces;

namespace GameVM.Compiler.Backend.Atari2600
{
    public class MidToLowLevelTransformer : IIRTransformer<MidLevelIR, LowLevelIR>
    {
        private readonly Dictionary<string, string> _addressMap = new(StringComparer.OrdinalIgnoreCase);
        private int _nextAvailableAddress = 0x80;

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
    }
}
