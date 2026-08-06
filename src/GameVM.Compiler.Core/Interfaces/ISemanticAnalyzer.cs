using System.Collections.Generic;
using GameVM.Compiler.Core.IR.Buffers;
using GameVM.Compiler.Core.IR.Soa;
namespace GameVM.Compiler.Core.Interfaces
{
    /// <summary>
    /// Interface for semantic analysis of High-Level IR
    /// </summary>
    public interface ISemanticAnalyzer
    {
        /// <summary>
        /// Analyzes a DOD HLIR slab using linear iteration and switch-based processing.
        /// </summary>
        /// <param name="hlirSlab">The HLIR slab to analyze</param>
        /// <returns>Result of semantic analysis including any errors found</returns>
        SemanticAnalysisResult AnalyzeSlab(InstList hlirSlab, StringPool stringPool);
    }

    /// <summary>
    /// Result of semantic analysis
    /// </summary>
    public class SemanticAnalysisResult
    {
        public bool Success { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();

        public static SemanticAnalysisResult CreateSuccess() => new() { Success = true };
        public static SemanticAnalysisResult Failure(params string[] errors) => new() { Success = false, Errors = new List<string>(errors) };
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
