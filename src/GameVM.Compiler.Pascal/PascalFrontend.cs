using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Dfa;
using Antlr4.Runtime.Sharpen;
using GameVM.Compiler.Core.Interfaces;
using GameVM.Compiler.Core.IR;
using GameVM.Compiler.Core.IR.Soa;
using GameVM.Compiler.Core.IR.SlabProcessing;
using GameVM.Compiler.Core.IR.Buffers;
using GameVM.Compiler.Core.IR.Transformers;
using GameVM.Compiler.Pascal.ANTLR;
using GameVM.Compiler.Pascal.Transformers;

namespace GameVM.Compiler.Pascal
{
    public class PascalFrontend : ILanguageFrontend
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

        /// <summary>
        /// Parse source code into AST slab (DOD pipeline) - returns SoA InstList
        /// </summary>
        public InstList ParseToSlab(string sourceCode)
        {
            try
            {
                _lastParseErrors.Clear();

                var inputStream = new AntlrInputStream(sourceCode);
                var lexer = new PascalLexer(inputStream);
                var commonTokenStream = new CommonTokenStream(lexer);
                var parser = new PascalParser(commonTokenStream);

                var errorListener = new CollectingErrorListener();
                lexer.AddErrorListener(errorListener);
                parser.AddErrorListener(errorListener);

                var context = parser.program();

                if (errorListener.Errors.Any())
                {
                    _lastParseErrors = errorListener.Errors;
                    return default;
                }

                _stringPool = new StringPool();

                var builder = new InstListBuilder();
                var visitor = new PascalToSlabVisitor(builder, _stringPool);
                visitor.Visit(context);

                return builder.Build();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ParseToSlab] Error: {ex.Message}");
                Console.Error.WriteLine(ex.StackTrace);
                return default;
            }
        }

        /// <summary>
        /// Convert AST slab to HLIR slab (DOD pipeline) - takes/returns InstList
        /// </summary>
        public InstList ConvertToHlirSlab(InstList astSlab)
        {
            if (astSlab.Count == 0)
                return default;

            var transformer = new AstSlabToHlirSlabTransformer(_stringPool);
            return transformer.Transform(astSlab);
        }

    }
}
