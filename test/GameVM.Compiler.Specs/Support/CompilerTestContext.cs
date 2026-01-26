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
    public class CompilerTestContext : IDisposable
    {
        private bool _disposed = false;
        
        public ICompileUseCase CompileUseCase { get; }
        public string SourceCode { get; set; } = string.Empty;
        public CompilationResult? CompilationResult { get; set; }
        public string? ErrorMessage { get; set; }
        public string? MameOutput { get; set; }

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

        /// <summary>
        /// Cleanup method to prevent memory leaks between test scenarios
        /// </summary>
        public void Cleanup()
        {
            SourceCode = string.Empty;
            CompilationResult = null;
            ErrorMessage = null;
            MameOutput = null;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                Cleanup();
                _disposed = true;
            }
        }
    }
}
