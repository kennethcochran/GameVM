namespace GameVM.Compiler.Core.Enums
{
    /// <summary>
    /// Classifies the role of a symbol in the symbol table.
    /// </summary>
    public enum SymbolKind : byte
    {
        Variable = 0,
        Constant = 1,
        Function = 2,
        Type = 3,
        Label = 4,
        Parameter = 5
    }
}
