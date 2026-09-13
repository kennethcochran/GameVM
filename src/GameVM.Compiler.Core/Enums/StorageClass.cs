namespace GameVM.Compiler.Core.Enums
{
    /// <summary>
    /// Describes where a symbol's storage resides at runtime.
    /// </summary>
    public enum StorageClass : byte
    {
        Local = 0,
        Global = 1,
        Parameter = 2,
        Register = 3
    }
}
