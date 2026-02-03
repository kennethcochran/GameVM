namespace GameVM.Compiler.Core.Enums
{
    /// <summary>
    /// Defines how strictly the compiler enforces capability profile boundaries.
    /// </summary>
    public enum EnforcementLevel
    {
        /// <summary>
        /// Strict: Violations result in a compiler error.
        /// </summary>
        Strict,

        /// <summary>
        /// Advisory: Violations result in a warning but allow compilation (if a fallback exists).
        /// </summary>
        Advisory
    }
}
