using System;

namespace GameVM.Compiler.Core.Interfaces
{
    /// <summary>
    /// Semantic error information
    /// </summary>
    public class SemanticError
    {
        public string Message { get; }
        public string ErrorCode { get; }
        public int Line { get; }
        public int Column { get; }

        public SemanticError(string message, string errorCode, int line, int column)
        {
            Message = message;
            ErrorCode = errorCode;
            Line = line;
            Column = column;
        }
    }
}