
namespace GameVM.Compiler.Core.IR
{
    /// <summary>
    /// AST instruction kind enumeration for type discrimination.
    /// Values correspond to InstructionMetadataFlags.KindMask for AST nodes.
    /// </summary>
    public enum AstNodeKind : byte
    {
        /// <summary>Unknown or invalid instruction</summary>
        Unknown = 0,

        /// <summary>NOP / empty instruction</summary>
        Nop = 0,

        /// <summary>Literal integer</summary>
        LiteralInt = 1,

        /// <summary>Literal string</summary>
        LiteralString = 2,

        /// <summary>Literal boolean</summary>
        LiteralBool = 3,

        /// <summary>Identifier (variable reference)</summary>
        Identifier = 4,

        /// <summary>Binary operation</summary>
        BinaryOp = 5,

        /// <summary>Unary operation</summary>
        UnaryOp = 6,

        /// <summary>Assignment statement</summary>
        Assignment = 7,

        /// <summary>Variable declaration</>
        VariableDeclaration = 8,

        /// <summary>Type declaration</summary>
        TypeDeclaration = 9,

        /// <summary>Method/function declaration</summary>
        MethodDeclaration = 10,

        /// <summary>Method/function call</summary>
        MethodCall = 11,

        /// <summary>If statement</summary>
        IfStatement = 12,

        /// <summary>While loop</summary>
        WhileStatement = 13,

        /// <summary>For loop</summary>
        ForStatement = 14,

        /// <summary>Block statement</summary>
        Block = 15,

        /// <summary>Return statement</summary>
        ReturnStatement = 16,

        /// <summary>Expression statement</summary>
        ExpressionStatement = 17,

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
