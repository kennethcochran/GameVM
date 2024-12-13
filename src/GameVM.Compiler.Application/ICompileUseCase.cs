namespace GameVM.Compiler.Application
{
    /// <summary>
    /// Interface for compilation use case
    /// </summary>
    public interface ICompileUseCase
    {
        /// <summary>
        /// Compiles source code to executable code using the specified options
        /// </summary>
        /// <param name="sourceCode">Source code to compile</param>
        /// <param name="extension">File extension (e.g. ".py" for Python)</param>
        /// <param name="options">Compilation options</param>
        /// <returns>Result of compilation</returns>
        CompilationResult Execute(string sourceCode, string extension, CompilationOptions options);
    }
}
