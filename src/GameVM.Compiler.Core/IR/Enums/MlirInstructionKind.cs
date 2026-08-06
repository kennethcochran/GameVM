using System;

namespace GameVM.Compiler.Core.IR
{
    /// <summary>
    /// MLIR instruction kind enumeration for type discrimination.
    /// Values correspond to InstructionMetadataFlags.KindMask.
    /// </summary>
    public enum MlirInstructionKind : byte
    {
        /// <summary>Unknown or invalid instruction</summary>
        Unknown = 0,
        
        /// <summary>NOP / empty instruction</summary>
        Nop = 0,
        
        /// <summary>Label instruction for control flow</summary>
        Label = 128,
        
        /// <summary>Branch instruction (conditional/unconditional)</summary>
        Branch = 129,
        
        /// <summary>Assignment instruction (MLIR style)</summary>
        Assign = 130,
        
        /// <summary>Call instruction (MLIR style)</summary>
        Call = 131,
        
        /// <summary>Return instruction</summary>
        Return = 16,
        
        /// <summary>Variable declaration</summary>
        Variable = 8,
        
        /// <summary>Block statement</summary>
        Block = 15,
        
        /// <summary>Expression statement</summary>
        ExpressionStatement = 17,
        
        /// <summary>Function declaration</summary>
        FunctionDeclaration = 10,
        
        /// <summary>Function call</summary>
        FunctionCall = 11,
        
        /// <summary>Function parameter</summary>
        FunctionParameter = 18,
        
        /// <summary>Function body</summary>
        FunctionBody = 19,
        
        /// <summary>Function signature</summary>
        FunctionSignature = 20,
        
        /// <summary>Class declaration</summary>
        ClassDeclaration = 21,
        
        /// <summary>Namespace declaration</summary>
        NamespaceDeclaration = 22
    }
}