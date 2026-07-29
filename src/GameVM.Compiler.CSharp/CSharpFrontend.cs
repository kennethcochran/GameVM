using Antlr4.Runtime;
using GameVM.Compiler.Core.Interfaces;
using GameVM.Compiler.Core.IR;
using GameVM.Compiler.Core.IR.Transformers;
using GameVM.Compiler.Core.IR.SlabProcessing;
using GameVM.Compiler.CSharp.ANTLR;
using GameVM.Compiler.CSharp.Transformers;
using System.Collections.Generic;

namespace GameVM.Compiler.CSharp
{
    public class CSharpFrontend : ILanguageFrontend
    {
        private readonly HlirToMlirTransformer _hlirToMlir = new HlirToMlirTransformer();

        public HighLevelIR Parse(string sourceCode)
        {
            try
            {
                var inputStream = new AntlrInputStream(sourceCode);
                var lexer = new CSharpLexer(inputStream);
                var commonTokenStream = new CommonTokenStream(lexer);
                var parser = new CSharpParser(commonTokenStream);
                
                var context = parser.program();

                // Check for syntax errors
                if (parser.NumberOfSyntaxErrors > 0)
                {
                    var hlir = new HighLevelIR { SourceFile = "<source>" };
                    hlir.Errors.Add($"Syntax error: {parser.NumberOfSyntaxErrors} error(s)");
                    return hlir;
                }

                // Use our DOD visitor to convert parse tree directly to AST slab
                var visitor = new CSharpToSlabVisitor(new ArenaAllocator());
                visitor.Visit(context);
                
                // Get the AST slab
                uint[] astSlab = visitor.GetSlab();
                
                // Transform AST slab to HLIR using our new transformer
                var astToHlirTransformer = new CSharpAstToHlirTransformer("<source>");
                return astToHlirTransformer.Transform(astSlab);
            }
            catch (Exception ex)
            {
                var hlir = new HighLevelIR { SourceFile = "<source>" };
                hlir.Errors.Add($"Failed to parse C# code: {ex.Message}");
                return hlir;
            }
        }

        public MidLevelIR ConvertToMidLevelIR(HighLevelIR hlir)
        {
            return _hlirToMlir.Transform(hlir);
        }
    }
}