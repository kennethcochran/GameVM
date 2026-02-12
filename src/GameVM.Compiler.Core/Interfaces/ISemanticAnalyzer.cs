using GameVM.Compiler.Core.IR;

namespace GameVM.Compiler.Core.Interfaces
{
    /// <summary>
    /// Interface for semantic analysis of High-Level IR
    /// </summary>
    public interface ISemanticAnalyzer
    {
        /// <summary>
        /// Performs semantic analysis on the given High-Level IR
        /// </summary>
        /// <param name="hlir">The High-Level IR to analyze</param>
        /// <returns>Result of semantic analysis including any errors found</returns>
        SemanticAnalysisResult Analyze(HighLevelIR hlir);
    }

    /// <summary>
    /// Result of semantic analysis
    /// </summary>
    public class SemanticAnalysisResult
    {
        public bool Success { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();

        public static SemanticAnalysisResult Success() => new() { Success = true };
        public static SemanticAnalysisResult Failure(params string[] errors) => new() { Success = false, Errors = errors.ToList() };
    }

    /// <summary>
    /// Semantic error information
    /// </summary>
    public class SemanticError
    {
        public string Message { get; }
        public string ErrorCode { get; }
        public int Line { get; }
        public int Column { get; }

        public SemanticError(string message, string errorCode = "SEMANTIC_ERROR", int line = 0, int column = 0)
        {
            Message = message;
            ErrorCode = errorCode;
            Line = line;
            Column = column;
        }
    }
}