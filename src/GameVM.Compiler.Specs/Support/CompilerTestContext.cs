using GameVM.Compiler.Application;
using GameVM.Compiler.Core.IR;
using GameVM.Compiler.Core.IR.Interfaces;
using GameVM.Compiler.Core.CodeGen;
using GameVM.Compiler.Core.IR.Transformers;
using GameVM.Compiler.Application.Services;
using GameVM.Compiler.Core.Interfaces;
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
            // TODO: Update this to use a different language frontend or remove if not needed
            throw new NotImplementedException("Python compiler has been removed. Please update the test context to use a different language frontend.");
        }
    }
}
