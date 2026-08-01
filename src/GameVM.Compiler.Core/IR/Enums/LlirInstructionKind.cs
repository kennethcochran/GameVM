using System;
using GameVM.Compiler.Core.IR.Slab;

namespace GameVM.Compiler.Core.IR
{
    /// <summary>
    /// LLIR instruction kind enumeration for type discrimination.
    /// Values correspond to InstructionMetadataFlags.KindMask.
    /// </summary>
    public enum LlirInstructionKind : byte
    {
        /// <summary>Unknown or invalid instruction</summary>
        Unknown = 0,
        
        /// <summary>NOP / empty instruction</summary>
        Nop = InstructionMetadataFlags.NOP,
        
        /// <summary>Label instruction for control flow</summary>
        Label = InstructionMetadataFlags.LLIR_LABEL,
        
        /// <summary>Load instruction</summary>
        Load = InstructionMetadataFlags.LLIR_LOAD,
        
        /// <summary>Store instruction</summary>
        Store = InstructionMetadataFlags.LLIR_STORE,
        
        /// <summary>Call instruction</summary>
        Call = InstructionMetadataFlags.LLIR_CALL,
        
        /// <summary>Jump instruction (conditional/unconditional)</summary>
        Jump = InstructionMetadataFlags.LLIR_JUMP,
        
        /// <summary>Branch instruction</summary>
        Branch = InstructionMetadataFlags.LLIR_BRANCH,
        
        /// <summary>Return instruction</summary>
        Return = InstructionMetadataFlags.LLIR_RETURN,
        
        /// <summary>Syscall instruction</summary>
        Syscall = InstructionMetadataFlags.LLIR_SYSCALL,
    }
}
