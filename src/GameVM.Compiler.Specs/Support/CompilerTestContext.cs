using GameVM.Compiler.Application;
using GameVM.Compiler.Core.IR;
using GameVM.Compiler.Core.IR.Interfaces;
using GameVM.Compiler.Core.CodeGen;
using GameVM.Compiler.Core.IR.Transformers;
using GameVM.Compiler.Application.Services;
using GameVM.Compiler.Core.Interfaces;
using GameVM.Compiler.Python;
using GameVM.Compiler.Services;

namespace GameVM.Compiler.Specs.Support
{
    /// <summary>
    /// Provides a shared context for compiler-related BDD tests
    /// </summary>
    public class CompilerTestContext
    {
        public ICompileUseCase CompileUseCase { get; }

        public CompilerTestContext()
        {
            // Create the compilation pipeline components
            var parseTreeToAST = new PythonParseTreeToAST("");
            var frontend = new PythonFrontend(parseTreeToAST);
            var mlirToLlir = new MidToLowLevelTransformer();
            var llirToFinal = new LowToFinalTransformer();
            var codeGenerator = new DefaultCodeGenerator();
            var midLevelOptimizer = new DefaultMidLevelOptimizer();
            var lowLevelOptimizer = new DefaultLowLevelOptimizer();
            var finalOptimizer = new DefaultFinalIROptimizer();

            // Create the main use case
            CompileUseCase = new CompileUseCase(
                frontend,
                midLevelOptimizer,
                lowLevelOptimizer,
                finalOptimizer,
                mlirToLlir,
                llirToFinal,
                codeGenerator);
        }
    }
}
