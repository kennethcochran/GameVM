namespace GameVM.Compiler.Core.Enums
{
    /// <summary>
    /// Severity categories for compiler diagnostics, per the documented
    /// convention: Fatal / Error / Warning / Info.
    /// </summary>
    public enum ErrorSeverity
    {
        /// <summary>Unrecoverable; compilation cannot continue.</summary>
        Fatal,

        /// <summary>Compilation of the current unit fails, but the compiler itself is healthy.</summary>
        Error,

        /// <summary>Suspicious but compilable; reported for visibility.</summary>
        Warning,

        /// <summary>Informational diagnostic.</summary>
        Info,
    }
}
