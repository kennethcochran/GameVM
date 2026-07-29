using System;

namespace GameVM.Compiler.Core.IR.Slab
{
    /// <summary>
    /// Flag constants for instruction metadata bitfields.
    /// These mirror the masks defined in <c>InstructionMetadata</c>.
    /// They are exposed separately for consumers that only need flag values.
    /// </summary>
    public struct InstructionMetadataFlags
    {
        // Metadata bitfield masks (from InstructionMetadata)
        public const uint TerminatorMask = InstructionMetadata.TerminatorMask;
        public const uint DiagnosticMask = InstructionMetadata.DiagnosticMask;
        public const uint ReservedMask   = InstructionMetadata.ReservedMask;

        // AST Instruction Kinds (for C# AST Slab)
        public const byte NOP = 0; // Already defined in InstructionMetadata as kind 0
        public const byte LITERAL_INT = 1;
        public const byte LITERAL_STRING = 2;
        public const byte LITERAL_BOOL = 3;
        public const byte IDENTIFIER = 4;
        public const byte BINARY_OP = 5;
        public const byte UNARY_OP = 6;
        public const byte ASSIGNMENT = 7;
        public const byte VARIABLE_DECLARATION = 8;
        public const byte TYPE_DECLARATION = 9;
        public const byte METHOD_DECLARATION = 10;
        public const byte METHOD_CALL = 11;
        public const byte IF_STATEMENT = 12;
        public const byte WHILE_STATEMENT = 13;
        public const byte FOR_STATEMENT = 14;
        public const byte BLOCK = 15;
        public const byte RETURN_STATEMENT = 16;
        public const byte EXPRESSION_STATEMENT = 17;
        public const byte FUNCTION_PARAMETER = 18;
        public const byte FUNCTION_BODY = 19;
        public const byte FUNCTION_SIGNATURE = 20;
        public const byte CLASS_DECLARATION = 21;
        public const byte NAMESPACE_DECLARATION = 22;

        // HLIR Instruction Kinds (for HLIR Slab)
        public const byte HLIR_LABEL = 64;
        public const byte HLIR_BRANCH = 65;
        public const byte HLIR_ASSIGN = 66;
        public const byte HLIR_CALL = 67;
        public const byte HLIR_RETURN = 68;
        public const byte HLIR_VARIABLE = 69;
        public const byte HLIR_LITERAL = 70;

        // MLIR Instruction Kinds (for MLIR Slab)
        public const byte MLIR_LABEL = 128;
        public const byte MLIR_BRANCH = 129;
        public const byte MLIR_ASSIGN = 130;
        public const byte MLIR_CALL = 131;

        // LLIR Instruction Kinds (for LLIR Slab)
        public const byte LLIR_LABEL = 192;
        public const byte LLIR_LOAD = 193;
        public const byte LLIR_STORE = 194;
        public const byte LLIR_CALL = 195;
        public const byte LLIR_JUMP = 196;
    }
}
