namespace GameVM.Compiler.CSharp
{
    /// <summary>
    /// C#-specific AST node kinds.
    /// Each frontend defines its own kind enum to avoid assembly circular dependencies.
    /// The container (AstNode/AstTree/AstBuilder) is language-neutral and lives in Core.
    /// </summary>
    public enum CSharpAstNodeKind : byte
    {
        Unknown = 0,
        Nop = 0,

        LiteralInt = 1,
        LiteralString = 2,
        LiteralBool = 3,
        Identifier = 4,
        BinaryOp = 5,

        Assignment = 7,
        VariableDeclaration = 8,
        TypeDeclaration = 9,
        MethodDeclaration = 10,
        MethodCall = 11,
        IfStatement = 12,
        WhileStatement = 13,
        ForStatement = 14,
        Block = 15,
        ReturnStatement = 16,
        ExpressionStatement = 17,
        FunctionParameter = 18,
        FunctionBody = 19,
        FunctionSignature = 20,
        ClassDeclaration = 21,
        NamespaceDeclaration = 22,

        // C#-specific (examples)
        Class = 30,
        Struct = 31,
        Interface = 32,
        Enum = 33,
        Property = 34,
        Event = 35,
        Delegate = 36,
        Lambda = 37,
        Switch = 38,
        Case = 39,
        Default = 40,
        Break = 41,
        Continue = 42,
        Throw = 43,
        Try = 44,
        Catch = 45,
        Finally = 46,
        Using = 47,
        Lock = 48,
        Foreach = 49,
    }
}