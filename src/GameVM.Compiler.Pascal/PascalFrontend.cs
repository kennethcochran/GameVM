using System.Collections.Generic;
using System.IO;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Dfa;
using Antlr4.Runtime.Sharpen;
using GameVM.Compiler.Core.Interfaces;
using GameVM.Compiler.Core.IR;
using GameVM.Compiler.Core.IR.Transformers;
using GameVM.Compiler.Core.IR.Slab;
using GameVM.Compiler.Core.IR.SlabProcessing;
using GameVM.Compiler.Core.IR.Buffers;
using GameVM.Compiler.Pascal.ANTLR;
using GameVM.Compiler.Pascal.Transformers;

namespace GameVM.Compiler.Pascal
{
    public class PascalFrontend : ILanguageFrontend
    {
        private readonly HlirToMlirTransformer _hlirToMlir = new HlirToMlirTransformer();
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

#pragma warning disable S1133
        [System.Obsolete("Use ParseToSlab for DOD pipeline. Will be removed in future version.")]
        public HighLevelIR Parse(string sourceCode)
        {
            try
            {
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
                    var hlir = new HighLevelIR { SourceFile = "<source>" };
                    hlir.Errors.Add(string.Join("; ", _lastParseErrors));
                    return hlir;
                }

                var tempStringPool = new StringPool();
                var visitor = new PascalToSlabVisitor(new ArenaAllocator(), tempStringPool);
                visitor.Visit(context);

                uint[] astSlab = visitor.GetSlab();

                var astToHlirTransformer = new PascalAstToHlirTransformer("<source>");
                return astToHlirTransformer.Transform(astSlab);
            }
            catch (Exception ex)
            {
                _lastParseErrors.Clear();
                var hlir = new HighLevelIR { SourceFile = "<source>" };
                hlir.Errors.Add($"Failed to parse Pascal code: {ex.Message}");
                return hlir;
            }
        }
#pragma warning restore S1133

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

#pragma warning disable S1133
        [System.Obsolete("Use ConvertToHlirSlab for DOD pipeline. Will be removed in future version.")]
        public MidLevelIR ConvertToMidLevelIR(HighLevelIR hlir)
        {
            return _hlirToMlir.Transform(hlir);
        }
#pragma warning restore S1133
    }
}
