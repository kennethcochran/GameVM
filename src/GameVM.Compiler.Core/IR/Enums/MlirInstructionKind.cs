using GameVM.Compiler.Core.IR.Slab;

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
        Nop = InstructionMetadataFlags.NOP,
        
        /// <summary>Label instruction for control flow</summary>
        Label = InstructionMetadataFlags.MLIR_LABEL,
        
        /// <summary>Branch instruction (conditional/unconditional)</summary>
        Branch = InstructionMetadataFlags.MLIR_BRANCH,
        
        /// <summary>Assignment instruction (MLIR style)</summary>
        Assign = InstructionMetadataFlags.MLIR_ASSIGN,
        
        /// <summary>Call instruction (MLIR style)</summary>
        Call = InstructionMetadataFlags.MLIR_CALL,
        
        /// <summary>Return instruction</summary>
        Return = InstructionMetadataFlags.RETURN_STATEMENT,
        
        /// <summary>Variable declaration</summary>
        Variable = InstructionMetadataFlags.VARIABLE_DECLARATION,
        
        /// <summary>Block statement</summary>
        Block = InstructionMetadataFlags.BLOCK,
        
        /// <summary>Expression statement</summary>
        ExpressionStatement = InstructionMetadataFlags.EXPRESSION_STATEMENT,
        
        /// <summary>Function declaration</summary>
        FunctionDeclaration = InstructionMetadataFlags.METHOD_DECLARATION,
        
        /// <summary>Function call</summary>
        FunctionCall = InstructionMetadataFlags.METHOD_CALL,
        
        /// <summary>Function parameter</summary>
        FunctionParameter = InstructionMetadataFlags.FUNCTION_PARAMETER,
        
        /// <summary>Function body</summary>
        FunctionBody = InstructionMetadataFlags.FUNCTION_BODY,
        
        /// <summary>Function signature</summary>
        FunctionSignature = InstructionMetadataFlags.FUNCTION_SIGNATURE,
        
        /// <summary>Class declaration</summary>
        ClassDeclaration = InstructionMetadataFlags.CLASS_DECLARATION,
        
        /// <summary>Namespace declaration</summary>
        NamespaceDeclaration = InstructionMetadataFlags.NAMESPACE_DECLARATION
    }
}