using System.IO;
using System.Threading.Tasks;
using GameVM.Compiler.Application;
using GameVM.Compiler.Core.IR.Interfaces;
using GameVM.Compiler.Core.Enums;

namespace GameVM.Compiler.Interaction
{
    public class Controller
    {
        private readonly CompileUseCase _compileUseCase;

        public Controller(CompileUseCase compileUseCase)
        {
            _compileUseCase = compileUseCase;
        }

        public void Compile(FileInfo sourceFile)
        {
            var options = new CompilationOptions
            {
                Target = Architecture.Genesis,
                DispatchStrategy = DispatchStrategy.DirectThreadedCode,
                GenerateDebugInfo = false,
                Optimize = true
            };

            _compileUseCase.Execute(sourceFile.FullName, options);
        }

        public Task<int> Run(string[] args)
        {
            if (args.Length == 0)
            {
                System.Console.WriteLine("Error: No input file specified");
                return Task.FromResult(1);
            }

            var sourceFile = new FileInfo(args[0]);
            if (!sourceFile.Exists)
            {
                System.Console.WriteLine($"Error: File not found: {args[0]}");
                return Task.FromResult(1);
            }

            var options = new CompilationOptions
            {
                Target = Architecture.Genesis,
                DispatchStrategy = DispatchStrategy.DirectThreadedCode,
                GenerateDebugInfo = false,
                Optimize = true
            };

            var result = _compileUseCase.Execute(sourceFile.FullName, options);
            return Task.FromResult(result.Success ? 0 : 1);
        }
    }
}