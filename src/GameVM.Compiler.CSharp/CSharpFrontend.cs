using System.Collections.Generic;
using System.IO;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Dfa;
using Antlr4.Runtime.Sharpen;
using GameVM.Compiler.Core.Interfaces;
using GameVM.Compiler.Core.IR.Slab;
using GameVM.Compiler.Core.IR.SlabProcessing;
using GameVM.Compiler.Core.IR.Buffers;
using GameVM.Compiler.CSharp.ANTLR;
using GameVM.Compiler.CSharp.Transformers;

namespace GameVM.Compiler.CSharp
{
    public class CSharpFrontend : ILanguageFrontend
    {
        private StringPool _stringPool = new StringPool();
        private List<string> _lastParseErrors = new List<string>();

        /// <summary>
        /// Gets the syntax error messages from the last parse attempt.
        /// </summary>
        public IReadOnlyList<string>? LastParseErrors => _lastParseErrors.Count > 0 ? _lastParseErrors : null;

        /// <summary>
        /// Gets the string pool from the last parse attempt (DOD pipeline).
        /// Populated after successful ParseToSlab.
        /// </summary>
        public StringPool? StringPool => _stringPool;

        // Custom ANTLR error listener to capture syntax error messages
        private sealed class CollectingErrorListener : IParserErrorListener, IAntlrErrorListener<int>
        {
            public List<string> Errors { get; } = new List<string>();

            public void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
            {
                Errors.Add($"line {line}:{charPositionInLine} {msg}");
            }

            public void SyntaxError(TextWriter output, IRecognizer recognizer, int offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
            {
                Errors.Add($"line {line}:{charPositionInLine} {msg}");
            }

            public void ReportAmbiguity(Parser recognizer, DFA dfa, int startIndex, int stopIndex, bool exact, BitSet ambigAlts, ATNConfigSet configs) { }
            public void ReportAttemptingFullContext(Parser recognizer, DFA dfa, int startIndex, int stopIndex, BitSet conflictingAlts, ATNConfigSet configs) { }
            public void ReportContextSensitivity(Parser recognizer, DFA dfa, int startIndex, int stopIndex, int prediction, ATNConfigSet configs) { }
        }

        public uint[] ParseToSlab(string sourceCode)
        {
            try
            {
                _lastParseErrors.Clear();

                var inputStream = new AntlrInputStream(sourceCode);
                var lexer = new CSharpLexer(inputStream);
                var commonTokenStream = new CommonTokenStream(lexer);
                var parser = new CSharpParser(commonTokenStream);

                var errorListener = new CollectingErrorListener();
                lexer.AddErrorListener(errorListener);
                parser.AddErrorListener(errorListener);

                var context = parser.program();

                if (_lastParseErrors.Any())
                    return Array.Empty<uint>();

                var visitor = new CSharpToSlabVisitor(new ArenaAllocator());
                visitor.Visit(context);
                return visitor.GetSlab();
            }
            catch (Exception)
            {
                _lastParseErrors.Clear();
                return Array.Empty<uint>();
            }
        }

        public uint[] ConvertToHlirSlab(uint[] astSlab)
        {
            if (astSlab == null || astSlab.Length == 0)
                return Array.Empty<uint>();

            var arena = new ArenaAllocator();
            var transformer = new AstSlabToHlirSlabTransformer(arena);
            return transformer.Transform(astSlab);
        }
    }
}