/*
 * CompileUseCase.cs
 * 
 * Primary use case for compiling source code to GameVM final IR.
 * Orchestrates the compilation process:
 * - Source file validation
 * - Language frontend selection
 * - IR generation pipeline
 * - Optimization passes
 * - Code generation
 * - Output file creation
 * 
 * Central coordinator for the compilation workflow.
 */

using System;
using System.IO;
using GameVM.Compiler.Core.IR;
using GameVM.Compiler.Core.IR.Interfaces;
using GameVM.Compiler.Core.IR.Transformers;
using GameVM.Compiler.Core.CodeGen;
using GameVM.Compiler.Core.Enums;
using GameVM.Compiler.Core.Exceptions;
using GameVM.Compiler.Application.Services;
using GameVM.Compiler.Core.Interfaces;

namespace GameVM.Compiler.Application
{
    /// <summary>
    /// Orchestrates the compilation pipeline from source code to GameVM final IR.
    /// </summary>
    public class CompileUseCase : ICompileUseCase
    {
        private readonly ILanguageFrontend _frontend;
        private readonly IMidLevelOptimizer _midLevelOptimizer;
        private readonly ILowLevelOptimizer _lowLevelOptimizer;
        private readonly IFinalIROptimizer _finalOptimizer;
        private readonly IIRTransformer<MidLevelIR, LowLevelIR> _mlirToLlir;
        private readonly IIRTransformer<LowLevelIR, FinalIR> _llirToFinal;
        private readonly ICodeGenerator _codeGenerator;

        public CompileUseCase(
            ILanguageFrontend frontend,
            IMidLevelOptimizer midLevelOptimizer,
            ILowLevelOptimizer lowLevelOptimizer,
            IFinalIROptimizer finalOptimizer,
            IIRTransformer<MidLevelIR, LowLevelIR> mlirToLlir,
            IIRTransformer<LowLevelIR, FinalIR> llirToFinal,
            ICodeGenerator codeGenerator)
        {
            _frontend = frontend ?? throw new ArgumentNullException(nameof(frontend));
            _midLevelOptimizer = midLevelOptimizer ?? throw new ArgumentNullException(nameof(midLevelOptimizer));
            _lowLevelOptimizer = lowLevelOptimizer ?? throw new ArgumentNullException(nameof(lowLevelOptimizer));
            _finalOptimizer = finalOptimizer ?? throw new ArgumentNullException(nameof(finalOptimizer));
            _mlirToLlir = mlirToLlir;
            _llirToFinal = llirToFinal;
            _codeGenerator = codeGenerator;
        }

        private CompilationResult CompileInternal(string sourceCode, string extension, CompilationOptions options)
        {
            try
            {
                // Parse source code to HLIR
                var hlir = _frontend.Parse(sourceCode);

                // Convert HLIR to MLIR
                var mlir = _frontend.ConvertToMidLevelIR(hlir);

                // Optimize MLIR
                if (options.Optimize)
                {
                    mlir = _midLevelOptimizer.Optimize(mlir, options.OptimizationLevel);
                }

                // Convert to LLIR
                var llir = _mlirToLlir.Transform(mlir);

                // Optimize LLIR
                if (options.Optimize)
                {
                    llir = _lowLevelOptimizer.Optimize(llir, options.OptimizationLevel);
                }

                // Convert to final IR
                var finalIR = _llirToFinal.Transform(llir);

                // Optimize final IR
                if (options.Optimize)
                {
                    finalIR = _finalOptimizer.Optimize(finalIR, options.OptimizationLevel);
                }

                // Generate code and bytecode
                var codeGenOptions = new CodeGenOptions
                {
                    Target = options.Target,
                    DispatchStrategy = options.DispatchStrategy,
                    GenerateDebugInfo = options.GenerateDebugInfo,
                    Optimize = options.Optimize
                };

                var code = _codeGenerator.Generate(finalIR, codeGenOptions);
                var bytecode = _codeGenerator.GenerateBytecode(finalIR, codeGenOptions);

                return new CompilationResult
                {
                    Success = true,
                    Code = code,
                    SourceFile = extension,
                    Target = options.Target,
                    ErrorMessage = string.Empty
                };
            }
            catch (CompilerException ex)
            {
                return new CompilationResult
                {
                    Success = false,
                    Code = Array.Empty<byte>(),
                    SourceFile = extension,
                    Target = options.Target,
                    ErrorMessage = ex.Message
                };
            }
            catch (Exception ex)
            {
                var error = $"Compilation failed: {ex.Message}";
                return new CompilationResult
                {
                    Success = false,
                    Code = Array.Empty<byte>(),
                    SourceFile = extension,
                    Target = options.Target,
                    ErrorMessage = error
                };
            }
        }

        /// <summary>
        /// Implementation of the compile use case
        /// </summary>
        public CompilationResult Execute(string sourceCode, string extension, CompilationOptions options)
        {
            return CompileInternal(sourceCode, extension, options);
        }

        /// <summary>
        /// Compiles source code to executable code using the specified options
        /// </summary>
        /// <param name="sourceFile">Source file to compile</param>
        /// <param name="options">Compilation options</param>
        /// <returns>Compilation result with generated code</returns>
        public CompilationResult Execute(string sourceFile, CompilationOptions options)
        {
            try
            {
                var extension = Path.GetExtension(sourceFile);
                var sourceCode = File.ReadAllText(sourceFile);
                return CompileInternal(sourceCode, extension, options);
            }
            catch (IOException ex)
            {
                return new CompilationResult
                {
                    Success = false,
                    Code = Array.Empty<byte>(),
                    SourceFile = sourceFile,
                    Target = options.Target,
                    ErrorMessage = $"Failed to read source file: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                return new CompilationResult
                {
                    Success = false,
                    Code = Array.Empty<byte>(),
                    SourceFile = sourceFile,
                    Target = options.Target,
                    ErrorMessage = $"Unexpected error: {ex.Message}"
                };
            }
        }
    }

    /// <summary>
    /// Options for compilation
    /// </summary>
    public class CompilationOptions
    {
        /// <summary>
        /// Target architecture to generate code for
        /// </summary>
        public Architecture Target { get; set; }

        /// <summary>
        /// Code dispatch strategy to use
        /// </summary>
        public DispatchStrategy DispatchStrategy { get; set; }

        /// <summary>
        /// Whether to generate debug information
        /// </summary>
        public bool GenerateDebugInfo { get; set; }

        /// <summary>
        /// Whether to optimize the generated code
        /// </summary>
        public bool Optimize { get; set; }

        /// <summary>
        /// Optimization level to use
        /// </summary>
        public OptimizationLevel OptimizationLevel { get; set; }
    }

    /// <summary>
    /// Result of compilation
    /// </summary>
    public class CompilationResult
    {
        /// <summary>
        /// Whether compilation was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Generated code
        /// </summary>
        public required byte[] Code { get; set; }

        /// <summary>
        /// Source file that was compiled
        /// </summary>
        public required string SourceFile { get; set; }

        /// <summary>
        /// Target architecture
        /// </summary>
        public Architecture Target { get; set; }

        /// <summary>
        /// Error message if compilation failed
        /// </summary>
        public required string ErrorMessage { get; set; }
    }
}
