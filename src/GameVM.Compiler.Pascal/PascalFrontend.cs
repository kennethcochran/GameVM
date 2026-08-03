using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Dfa;
using Antlr4.Runtime.Sharpen;
using GameVM.Compiler.Core.Interfaces;
using GameVM.Compiler.Core.IR;
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
        /// Parse source code into AST slab (DOD pipeline)
        /// </summary>
        public uint[] ParseToSlab(string sourceCode)
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
                    return Array.Empty<uint>();
                }

                _stringPool = new StringPool();

                var visitor = new PascalToSlabVisitor(new ArenaAllocator(), _stringPool);
                visitor.Visit(context);

                uint[] astSlab = visitor.GetSlab();

                return astSlab;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ParseToSlab] Error: {ex.Message}");
                Console.Error.WriteLine(ex.StackTrace);
                return Array.Empty<uint>();
            }
        }

        /// <summary>
        /// Parse source code into HighLevelIR (legacy OOP API for backward compatibility)
        /// </summary>
        [Obsolete("Use ParseToSlab for DOD pipeline. This method is kept for backward compatibility.")]
        public HighLevelIR Parse(string sourceCode)
        {
            var astSlab = ParseToSlab(sourceCode);
            if (astSlab == null || astSlab.Length == 0)
                return new HighLevelIR { SourceFile = "<unknown>" };

            _ = ConvertToHlirSlab(astSlab);
            return ConvertSlabToHighLevelIR();
        }

        /// <summary>
        /// Convert AST slab to HLIR slab (DOD pipeline)
        /// </summary>
        public uint[] ConvertToHlirSlab(uint[] astSlab)
        {
            if (astSlab == null || astSlab.Length == 0)
                return Array.Empty<uint>();

            var arena = new ArenaAllocator();
            var transformer = new AstSlabToHlirSlabTransformer(arena, _stringPool);
            return transformer.Transform(astSlab);
        }

        /// <summary>
        /// Convert HLIR to MidLevelIR (legacy OOP API for backward compatibility)
        /// </summary>
        [Obsolete("Use DOD pipeline with slabs. This method is kept for backward compatibility.")]
        public MidLevelIR ConvertToMidLevelIR(HighLevelIR hlir)
        {
            // For now, return a basic MidLevelIR for backward compatibility
            // In a full implementation, this would use the DOD pipeline
            return new MidLevelIR
            {
                SourceFile = hlir?.SourceFile ?? "<unknown>",
                Modules = new List<MidLevelIR.MLModule>()
            };
        }

        /// <summary>
        /// Convert HLIR slab to HighLevelIR object (for backward compatibility)
        /// </summary>
        private HighLevelIR ConvertSlabToHighLevelIR()
        {
            // Basic conversion - in practice this would deserialize the slab
            return new HighLevelIR
            {
                SourceFile = "<parsed>",
                Modules = new List<HlModule>()
            };
        }
    }
}
