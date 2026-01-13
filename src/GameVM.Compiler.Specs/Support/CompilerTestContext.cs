using GameVM.Compiler.Application;
using GameVM.Compiler.Core.IR;
using GameVM.Compiler.Core.IR.Interfaces;
using GameVM.Compiler.Core.CodeGen;
using GameVM.Compiler.Core.IR.Transformers;
using GameVM.Compiler.Application.Services;
using GameVM.Compiler.Core.Interfaces;
using GameVM.Compiler.Pascal;
using GameVM.Compiler.Optimizers.MidLevel;
using GameVM.Compiler.Optimizers.LowLevel;
using GameVM.Compiler.Backend.Atari2600;

namespace GameVM.Compiler.Specs.Support
{
    /// <summary>
    /// Provides a shared context for compiler-related BDD tests
    /// </summary>
    public class CompilerTestContext
    {
        public ICompileUseCase CompileUseCase { get; }
        public string SourceCode { get; set; } = string.Empty;
        public CompilationResult? CompilationResult { get; set; }
        public string? ErrorMessage { get; set; }

        public CompilerTestContext()
        {
            // Create real compiler with Pascal frontend
            CompileUseCase = new CompileUseCase(
                new PascalFrontend(),
                new DefaultMidLevelOptimizer(),
                new DefaultLowLevelOptimizer(),
                new MidToLowLevelTransformer(),
                new Atari2600CodeGenerator());
        }
    }
}
