using GameVM.Compiler.Application;
using GameVM.Compiler.Application.Services;
using GameVM.Compiler.Core.CodeGen;
using GameVM.Compiler.Core.IR.Interfaces;
using GameVM.Compiler.Core.IR;
using GameVM.Compiler.Core.IR.Transformers;
using GameVM.Compiler.Python;
using GameVM.Compiler.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using GameVM.Compiler.Core.Interfaces;

namespace GameVM.Compile
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = Host.CreateDefaultBuilder(args);

            builder.ConfigureServices(services =>
            {
                // Register language frontends
                services.AddSingleton<PythonParseTreeToAST>();
                services.AddSingleton<PythonASTToHLIR>();
                services.AddSingleton<ILanguageFrontend, PythonFrontend>();

                // Register optimizers
                services.AddSingleton<IMidLevelOptimizer, DefaultMidLevelOptimizer>();
                services.AddSingleton<ILowLevelOptimizer, DefaultLowLevelOptimizer>();
                services.AddSingleton<IFinalIROptimizer, DefaultFinalIROptimizer>();

                // Register transformers
                services.AddSingleton<IIRTransformer<MidLevelIR, LowLevelIR>, MidToLowLevelTransformer>();
                services.AddSingleton<IIRTransformer<LowLevelIR, FinalIR>, LowToFinalTransformer>();

                // Register code generator
                services.AddSingleton<ICodeGenerator, DefaultCodeGenerator>();

                // Register the main use case
                services.AddSingleton<ICompileUseCase, CompileUseCase>();
            });

            var host = builder.Build();

            // Run the application
            using (var scope = host.Services.CreateScope())
            {
                var useCase = scope.ServiceProvider.GetRequiredService<ICompileUseCase>();
                // TODO: Add command line argument handling and actual compilation
            }
        }
    }
}
