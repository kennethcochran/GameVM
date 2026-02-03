/*
 * CompileUseCase.cs
 * 
 * Primary use case for compiling source code to GameVM final IR.
 * Orchestrates the compilation process:
 * - Source file validation
 * - IR generation pipeline
 * - Optimization passes
 * - Code generation
 * - Output file creation
 * 
 * Central coordinator for the compilation workflow.
 */

using GameVM.Compiler.Core.IR;
using GameVM.Compiler.Core.IR.Interfaces;
using GameVM.Compiler.Core.Enums;
using GameVM.Compiler.Core.Exceptions;
using GameVM.Compiler.Application.Services;
using GameVM.Compiler.Core.Interfaces;
using GameVM.Compiler.Core;
using System.Linq;

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
        private readonly IIRTransformer<MidLevelIR, LowLevelIR> _mlirToLlir;
        private readonly ICodeGenerator _codeGenerator;
        private readonly ICapabilityProvider _capabilityProvider;
        private readonly ICapabilityValidatorService _capabilityValidator;

        public CompileUseCase(
            ILanguageFrontend frontend,
            IMidLevelOptimizer midLevelOptimizer,
            ILowLevelOptimizer lowLevelOptimizer,
            IIRTransformer<MidLevelIR, LowLevelIR> mlirToLlir,
            ICodeGenerator codeGenerator,
            ICapabilityProvider capabilityProvider,
            ICapabilityValidatorService capabilityValidator)
        {
            _frontend = frontend ?? throw new ArgumentNullException(nameof(frontend));
            _midLevelOptimizer = midLevelOptimizer ?? throw new ArgumentNullException(nameof(midLevelOptimizer));
            _lowLevelOptimizer = lowLevelOptimizer ?? throw new ArgumentNullException(nameof(lowLevelOptimizer));
            _mlirToLlir = mlirToLlir;
            _codeGenerator = codeGenerator ?? throw new ArgumentNullException(nameof(codeGenerator));
            _capabilityProvider = capabilityProvider ?? throw new ArgumentNullException(nameof(capabilityProvider));
            _capabilityValidator = capabilityValidator ?? throw new ArgumentNullException(nameof(capabilityValidator));
        }

        private CompilationResult CompileInternal(string sourceCode, string extension, CompilationOptions options)
        {
            try
            {
                // Parse source code to HLIR
                var hlir = _frontend.Parse(sourceCode);

                if (hlir.Errors.Count > 0)
                {
                    return new CompilationResult
                    {
                        Success = false,
                        Code = Array.Empty<byte>(),
                        SourceFile = extension,
                        Target = options.Target,
                        ErrorMessage = string.Join("; ", hlir.Errors)
                    };
                }

                // Validate Capability Profile
                if (options.Enforcement == EnforcementLevel.Strict)
                {
                    // First, validate that the backend supports the requested profile and extensions
                    var backendProfile = _capabilityProvider.GetCapabilityProfile();
                    var backendExtensions = _capabilityProvider.GetSupportedExtensions();
                    
                    var backendViolations = ValidateBackendCapabilities(options, backendProfile, backendExtensions);
                    if (backendViolations.Any())
                    {
                        return new CompilationResult
                        {
                            Success = false,
                            Code = Array.Empty<byte>(),
                            SourceFile = extension,
                            Target = options.Target,
                            ErrorMessage = $"Backend capability violations: {string.Join("; ", backendViolations)}"
                        };
                    }

                    // Then validate the code against the backend's actual capabilities
                    var violations = _capabilityValidator.Validate(hlir, backendProfile.BaseLevel, backendProfile.Extensions.ToList());

                    if (violations.Any())
                    {
                        return new CompilationResult
                        {
                            Success = false,
                            Code = Array.Empty<byte>(),
                            SourceFile = extension,
                            Target = options.Target,
                            ErrorMessage = $"Capability violations: {string.Join("; ", violations)}"
                        };
                    }
                }

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

                // Generate code and bytecode
                var codeGenOptions = new CodeGenOptions
                {
                    Target = options.Target,
                    DispatchStrategy = options.DispatchStrategy,
                    GenerateDebugInfo = options.GenerateDebugInfo,
                    Optimize = options.Optimize
                };

                var code = _codeGenerator.Generate(llir, codeGenOptions);

                return new CompilationResult
                {
                    Success = true,
                    Code = code,
                    SourceFile = extension,
                    Target = options.Target,
                    Profile = options.Profile,
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
        /// Validates that the requested compilation options are supported by the backend
        /// </summary>
        private static IEnumerable<string> ValidateBackendCapabilities(CompilationOptions options, CapabilityProfile backendProfile, IEnumerable<string> backendExtensions)
        {
            var violations = new List<string>();

            // Check if requested profile exceeds backend's base capability level
            if (options.Profile > backendProfile.BaseLevel)
            {
                violations.Add($"Requested profile {options.Profile} exceeds backend base capability {backendProfile.BaseLevel}");
            }

            // Check if requested extensions are supported by backend
            var backendExtensionSet = new HashSet<string>(backendExtensions);
            var unsupportedExtensions = options.SystemExtensions.Where(ext => !backendExtensionSet.Contains(ext));
            
            foreach (var unsupportedExtension in unsupportedExtensions)
            {
                violations.Add($"Backend does not support extension '{unsupportedExtension}'");
            }

            return violations;
        }

        /// <summary>
        /// Implementation of the compile use case
        /// </summary>
        public CompilationResult Execute(string sourceCode, string extension, CompilationOptions options)
        {
            if (sourceCode == null)
            {
                throw new ArgumentNullException(nameof(sourceCode));
            }
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

        /// <summary>
        /// The hardware capability profile to target
        /// </summary>
        public CapabilityLevel Profile { get; set; } = CapabilityLevel.L1;

        /// <summary>
        /// How strictly to enforce the capability profile
        /// </summary>
        public EnforcementLevel Enforcement { get; set; } = EnforcementLevel.Strict;

        /// <summary>
        /// Hardware extensions (injections) enabled for this project
        /// </summary>
        public List<string> SystemExtensions { get; set; } = new List<string>();
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
        /// The profile used for this compilation
        /// </summary>
        public CapabilityLevel Profile { get; set; }

        /// <summary>
        /// Error message if compilation failed
        /// </summary>
        public required string ErrorMessage { get; set; }
    }
}
