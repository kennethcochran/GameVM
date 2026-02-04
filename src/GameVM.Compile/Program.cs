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
    using Compiler.Capabilities;
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
                services.AddSingleton<ICapabilityProvider, Atari2600CodeGenerator>();

                // Register capability validator
                services.AddSingleton<ICapabilityValidatorService, CapabilityValidatorService>();

                // Register the main use case
                services.AddSingleton<ICompileUseCase, CompileUseCase>();
            });

            var host = builder.Build();

            // Command line argument handling
            var inputOption = new Option<string>("--input");
            inputOption.Description = "Input file to compile";
            var outputOption = new Option<string>("--output");
            outputOption.Description = "Output file for the compiled code";

            var rootCommand = new RootCommand("GameVM Compiler");
            rootCommand.Options.Add(inputOption);
            rootCommand.Options.Add(outputOption);

            rootCommand.SetAction(parseResult =>
            {
                var input = parseResult.GetValue(inputOption);
                var output = parseResult.GetValue(outputOption);
                
                if (!ValidateArguments(input, output))
                {
                    return;
                }

                try
                {
                    var sourceCode = File.ReadAllText(input!);
                    using (var scope = host.Services.CreateScope())
                    {
                        var success = CompileFile(input!, output!, sourceCode, scope);
                        if (success)
                        {
                            Console.WriteLine($"Successfully compiled {input} to {output}");
                        }
                    }
                }
                catch (FileNotFoundException ex)
                {
                    Console.WriteLine($"Error: File not found: {ex.FileName}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            });

            var parseResult = rootCommand.Parse(args);
            parseResult.Invoke();
        }

        /// <summary>
        /// Validates command line arguments
        /// </summary>
        public static bool ValidateArguments(string? input, string? output)
        {
            if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(output))
            {
                Console.WriteLine("Error: Both --input and --output are required.");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Compiles a file using the provided service scope
        /// </summary>
        public static bool CompileFile(string inputFile, string outputFile, string sourceCode, IServiceScope scope)
        {
            var extension = Path.GetExtension(inputFile);
            var useCase = scope.ServiceProvider.GetRequiredService<ICompileUseCase>();
            var result = useCase.Execute(sourceCode, extension, new CompilationOptions
            {
                // Set any compilation options here
            });

            if (result.Success)
            {
                File.WriteAllBytes(outputFile, result.Code);
                return true;
            }
            else
            {
                Console.WriteLine($"Compilation failed for {inputFile}:");
                Console.WriteLine($"- {result.ErrorMessage}");
                return false;
            }
        }
    }
}
