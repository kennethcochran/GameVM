
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
        Nop = 0,
        
        /// <summary>Label instruction for control flow</summary>
        Label = 192,
        
        /// <summary>Load instruction</summary>
        Load = 193,
        
        /// <summary>Store instruction</summary>
        Store = 194,
        
        /// <summary>Call instruction</summary>
        Call = 195,
        
        /// <summary>Jump instruction (conditional/unconditional)</summary>
        Jump = 196,
        
        /// <summary>Branch instruction</summary>
        Branch = 197,
        
        /// <summary>Return instruction</summary>
        Return = 198,
        
        /// <summary>Syscall instruction</summary>
        Syscall = 199,
    }
}
