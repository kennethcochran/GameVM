using System;
using System.Collections.Generic;
using GameVM.Compiler.Core.IR;
using GameVM.Compiler.Core.IR.Interfaces;

namespace GameVM.Compiler.Backend.Atari2600
{
    public class MidToLowLevelTransformer : IIRTransformer<MidLevelIR, LowLevelIR>
    {
        private Dictionary<string, string> _addressMap = new(StringComparer.OrdinalIgnoreCase);
        private int _nextAvailableAddress = 0x80;

        public LowLevelIR Transform(MidLevelIR mlir)
        {
            _addressMap.Clear();
            _nextAvailableAddress = 0x80;
            
            // Pre-fill known registers
            _addressMap["COLUBK"] = "$09";
            _addressMap["COLUPF"] = "$08";
            _addressMap["COLUP0"] = "$06";
            _addressMap["COLUP1"] = "$07";

            var llir = new LowLevelIR { SourceFile = mlir.SourceFile };
            llir.Modules.Clear();
            
            // Process each module in the input
            foreach (var module in mlir.Modules)
            {
                var outputModule = new LowLevelIR.LLModule { Name = module.Name };

                // Process each function in the input module
                foreach (var mlFunc in module.Functions)
                {
                    var llFunc = new LowLevelIR.LLFunction { Name = mlFunc.Name };
                    
                    // Process each instruction in the function
                    foreach (var instr in mlFunc.Instructions)
                    {
                        if (instr is MidLevelIR.MLLabel label)
                        {
                            llFunc.Instructions.Add(new LowLevelIR.LLLabel { Name = label.Name });
                        }
                        else if (instr is MidLevelIR.MLAssign assign)
                        {
                            var targetAddr = MapToAddress(assign.Target);
                            llFunc.Instructions.Add(new LowLevelIR.LLLoad { Register = "A", Value = assign.Source });
                            llFunc.Instructions.Add(new LowLevelIR.LLStore { Address = targetAddr, Register = "A" });
                        }
                        else if (instr is MidLevelIR.MLCall call)
                        {
                            llFunc.Instructions.Add(new LowLevelIR.LLCall { Label = call.Name });
                        }
                        else if (instr is MidLevelIR.MLBranch branch)
                        {
                            llFunc.Instructions.Add(new LowLevelIR.LLJump { Target = branch.Target, Condition = branch.Condition });
                        }
                    }
                    
                    outputModule.Functions.Add(llFunc);
                    
                    // Legacy: Add function label for tests that expect it at the beginning of top-level instructions
                    llir.Instructions.Add(new LowLevelIR.LLLabel { Name = llFunc.Name });

                    // Flatten instructions into the top-level list (for legacy tests)
                    foreach (var llInstr in llFunc.Instructions)
                    {
                        llir.Instructions.Add(llInstr);
                    }
                }
                
                llir.Modules.Add(outputModule);
            }

            return llir;
        }

        private string MapToAddress(string target)
        {
            if (_addressMap.TryGetValue(target, out var addr))
                return addr;

            if (target.StartsWith("$"))
                return target;

            // Allocate new address
            var newAddr = $"${_nextAvailableAddress:X2}";
            _addressMap[target] = newAddr;
            _nextAvailableAddress++;
            return newAddr;
        }
    }
}
