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
using GameVM.Compiler.Core.IR.Soa;
using GameVM.Compiler.CSharp.ANTLR;
using GameVM.Compiler.CSharp.Transformers;
using GameVM.Compiler.Core.IR.Transformers;

namespace GameVM.Compiler.CSharp
{
    public class CSharpFrontend : ILanguageFrontend
    {
        private readonly StringPool _stringPool = new StringPool();
        /// <summary>
        /// Gets the string pool from the last parse attempt (DOD pipeline).
        /// Populated after successful ParseToSlab.
        /// </summary>
        public StringPool? StringPool => _stringPool;
        public IReadOnlyList<string>? LastParseErrors => _lastParseErrors;
        private readonly List<string> _lastParseErrors = new();
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

        public InstList ParseToSlab(string sourceCode)
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
                    return default;

                var builder = new InstListBuilder();
                var visitor = new CSharpToSlabVisitor(builder, _stringPool);
                visitor.Visit(context);

                return builder.Build();
            }
            catch (Exception)
            {
                _lastParseErrors.Clear();
                return default;
            }
        }

        public InstList ConvertToHlirSlab(InstList astSlab)
        {
            if (astSlab.Count == 0)
                return default;

            var transformer = new AstSlabToHlirSlabTransformer(_stringPool);
            return transformer.Transform(astSlab);
        }
    }
}