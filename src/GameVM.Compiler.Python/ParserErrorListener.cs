using System.Text;
using System.IO;
using Antlr4.Runtime;

namespace GameVM.Compiler.Python
{
    /// <summary>
    /// Custom error listener for ANTLR parser to capture syntax errors
    /// </summary>
    internal class ParserErrorListener : IAntlrErrorListener<IToken>
    {
        private readonly StringBuilder _errorMessages = new();
        
        public bool HasErrors => _errorMessages.Length > 0;

        void IAntlrErrorListener<IToken>.SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol, int line,
            int charPositionInLine, string msg, RecognitionException e)
        {
            _errorMessages.AppendLine($"line {line}:{charPositionInLine} {msg}");
        }

        public string GetErrorMessage()
        {
            return _errorMessages.ToString().TrimEnd();
        }
    }
}
