namespace GameVM.Compiler.Core.Enums
{
    /// <summary>
    /// Classifies the structural role of a declaration context (scope).
    /// </summary>
    public enum ContextKind : byte
    {
        Module = 0,
        Function = 1,
        Block = 2
    }
}
