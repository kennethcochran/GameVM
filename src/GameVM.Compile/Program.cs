namespace GameVM.Compile
{
    using Compiler.Application;
    using Compiler.Application.Services;
    using Compiler.Core.IR.Interfaces;
    using Compiler.Core.IR;
    using Compiler.Optimizers.MidLevel;
    using Compiler.Optimizers.LowLevel;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Compiler.Core.Interfaces;
    using Compiler.Pascal;
    using Compiler.Backend.Atari2600;
    using System.CommandLine;
    using System.IO;

    public static class Program
    {
        public static void Main(string[] args)
        {
            var builder = Host.CreateDefaultBuilder(args);

            builder.ConfigureServices(services =>
            {
                // Register optimizers
                services.AddSingleton<IMidLevelOptimizer, DefaultMidLevelOptimizer>();
                services.AddSingleton<ILowLevelOptimizer, DefaultLowLevelOptimizer>();

                // Register transformers
                services.AddSingleton<IIRTransformer<MidLevelIR, LowLevelIR>, MidToLowLevelTransformer>();

                // Register frontend
                services.AddSingleton<ILanguageFrontend, PascalFrontend>();

                // Register code generator
                services.AddSingleton<ICodeGenerator, Atari2600CodeGenerator>();

                // Register the main use case
                services.AddSingleton<ICompileUseCase, CompileUseCase>();
            });

            var host = builder.Build();

            // Command line argument handling
            var inputOption = new Option<string>("--input", "Input file to compile");
            var outputOption = new Option<string>("--output", "Output file for the compiled code");

            var rootCommand = new RootCommand("GameVM Compiler");
            rootCommand.Options.Add(inputOption);
            rootCommand.Options.Add(outputOption);

            rootCommand.SetAction(parseResult =>
            {
                var input = parseResult.GetValue(inputOption);
                var output = parseResult.GetValue(outputOption);
                
                if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(output))
                {
                    Console.WriteLine("Error: Both --input and --output are required.");
                    return;
                }

                using (var scope = host.Services.CreateScope())
                {
                    var sourceCode = File.ReadAllText(input);
                    var extension = Path.GetExtension(input);
                    var useCase = scope.ServiceProvider.GetRequiredService<ICompileUseCase>();
                    var result = useCase.Execute(sourceCode, extension, new CompilationOptions
                    {
                        // Set any compilation options here
                    });

                    if (result.Success)
                    {
                        File.WriteAllBytes(output, result.Code);
                        Console.WriteLine($"Successfully compiled {input} to {output}");
                    }
                    else
                    {
                        Console.WriteLine($"Compilation failed for {input}:");
                        Console.WriteLine($"- {result.ErrorMessage}");
                    }
                }
            });

            var parseResult = rootCommand.Parse(args);
            parseResult.Invoke();
        }
    }
}
