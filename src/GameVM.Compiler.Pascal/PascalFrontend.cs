using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Dfa;
using Antlr4.Runtime.Sharpen;
using GameVM.Compiler.Core.Interfaces;
using GameVM.Compiler.Core.IR.Buffers;
using GameVM.Compiler.Core.IR.Soa;
using GameVM.Compiler.Pascal.Ast;
using GameVM.Compiler.Core.IR.Hlir;
using GameVM.Compiler.Pascal.ANTLR;
using GameVM.Compiler.Pascal.Transformers;

namespace GameVM.Compiler.Pascal
{
    public class PascalFrontend : ILanguageFrontend
    {
        private readonly StringPool _stringPool = new StringPool();
        private readonly List<string> _lastParseErrors = new List<string>();

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
        /// Parse source code and transform to HLIR semantic tree (DOD pipeline).
        /// </summary>
        public ParseResult ParseToHlir(string sourceCode)
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
                    _lastParseErrors.AddRange(errorListener.Errors);
                    return new ParseResult(HlirTree.Empty, default);
                }

                var visitor = new PascalToAstVisitor(_stringPool);
                visitor.Visit(context);
                var astTree = visitor.BuildTree();

                if (astTree.Count == 0)
                    return new ParseResult(HlirTree.Empty, default);

                var transformer = new PascalAstToHlirTransformer(_stringPool);
                var hlir = transformer.Transform(astTree);
                var symbols = transformer.BuildSymbolTable();
                return new ParseResult(hlir, symbols);
            }
            catch (InvalidOperationException ex)
            {
                _lastParseErrors.Clear();
                _lastParseErrors.Add(ex.Message);
                return new ParseResult(HlirTree.Empty, default);
            }
            catch (Exception)
            {
                _lastParseErrors.Clear();
                return new ParseResult(HlirTree.Empty, default);
            }
        }
    }
}