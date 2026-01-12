using Antlr4.Runtime;
using GameVM.Compiler.Core.Interfaces;
using GameVM.Compiler.Core.IR;
using GameVM.Compiler.Core.IR.Interfaces;
using GameVM.Compiler.Core.IR.Transformers;
using GameVM.Compiler.Pascal.ANTLR;

namespace GameVM.Compiler.Pascal
{
    public class PascalFrontend : ILanguageFrontend
    {
        private readonly HlirToMlirTransformer _hlirToMlir = new HlirToMlirTransformer();

        public HighLevelIR Parse(string sourceCode)
        {
            // Simple pre-processor to ignore lines starting with // (helpful for lit testing and modern Pascal style)
            var lines = sourceCode.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].TrimStart().StartsWith("//"))
                {
                    lines[i] = ""; // Replace with empty line to preserve line numbers if needed
                }
            }
            var filteredSource = string.Join('\n', lines);

            var inputStream = new AntlrInputStream(filteredSource);
            var lexer = new PascalLexer(inputStream);
            var commonTokenStream = new CommonTokenStream(lexer);
            var parser = new PascalParser(commonTokenStream);

            var context = parser.program();
            var visitor = new ASTVisitor();
            var result = visitor.Visit(context);

            if (result is ErrorNode errorNode)
            {
                // Return a minimal HLIR with error information
                var hlir = new HighLevelIR { SourceFile = "<source>" };
                // Note: In a real implementation, we'd want to collect errors properly
                return hlir;
            }

            if (result is not ProgramNode programNode)
            {
                // Return a minimal HLIR if we didn't get a program node
                return new HighLevelIR { SourceFile = "<source>" };
            }

            var transformer = new PascalAstToHlirTransformer("<source>");
            return transformer.Transform(programNode);
        }

        public MidLevelIR ConvertToMidLevelIR(HighLevelIR hlir)
        {
            return _hlirToMlir.Transform(hlir);
        }
    }
}
