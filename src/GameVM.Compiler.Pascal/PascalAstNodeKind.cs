namespace GameVM.Compiler.Pascal
{
    /// <summary>
    /// Pascal-specific AST node kinds.
    /// Each frontend defines its own kind enum to avoid assembly circular dependencies.
    /// The container (AstNode/AstTree/AstBuilder) is language-neutral and lives in Core.
    /// </summary>
    public enum PascalAstNodeKind : byte
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

        // Pascal-specific
        Program = 30,
        Begin = 31,
        End = 32,
        Case = 33,
        With = 34,
        Goto = 35,
        Repeat = 36,
        Until = 37,
        ProcedureCall = 38,
        ConstantDeclaration = 39,
        TypeDefinition = 40,
        RecordType = 41,
        ArrayType = 42,
        SetType = 43,
        FileType = 44,
        PointerType = 45,
        LabelDeclaration = 46,
        Label = 47,
        CaseBranch = 48,
        VariantCase = 49,
        ConstantDefinition = 58,
    }
}