using GameVM.Compiler.Core.IR.Ast;
using GameVM.Compiler.Core.Exceptions;
using GameVM.Compiler.Core.IR.Soa;
using GameVM.Compiler.Core.IR.Interfaces;
using GameVM.Compiler.Core.Interfaces;
using GameVM.Compiler.Application.Services;
using GameVM.Compiler.Core.Enums;

namespace GameVM.Compiler.Application
{
    /// <summary>
    /// Orchestrates the compilation pipeline from source code to GameVM final IR.
    /// </>
    public class CompileUseCase : ICompileUseCase
    {
        private readonly ILanguageFrontend _frontend;
        private readonly IMidLevelOptimizer _midLevelOptimizer;
        private readonly ILowLevelOptimizer _lowLevelOptimizer;
        private readonly IIRSlabTransformer _mlirToLlir;
        private readonly ICodeGenerator _codeGenerator;
        private readonly ICapabilityProvider _capabilityProvider;
        private readonly ICapabilityValidatorService _capabilityValidator;
        private readonly ISemanticAnalyzer _semanticAnalyzer;

        public CompileUseCase(
            ILanguageFrontend frontend,
            IMidLevelOptimizer midLevelOptimizer,
            ILowLevelOptimizer lowLevelOptimizer,
            IIRSlabTransformer mlirToLlir,
            ICodeGenerator codeGenerator,
            ICapabilityProvider capabilityProvider,
            ICapabilityValidatorService capabilityValidator,
            ISemanticAnalyzer semanticAnalyzer)
        {
            _frontend = frontend ?? throw new ArgumentNullException(nameof(frontend));
            _midLevelOptimizer = midLevelOptimizer ?? throw new ArgumentNullException(nameof(midLevelOptimizer));
            _lowLevelOptimizer = lowLevelOptimizer ?? throw new ArgumentNullException(nameof(lowLevelOptimizer));
            _mlirToLlir = mlirToLlir ?? throw new ArgumentNullException(nameof(mlirToLlir));
            _codeGenerator = codeGenerator ?? throw new ArgumentNullException(nameof(codeGenerator));
            _capabilityProvider = capabilityProvider ?? throw new ArgumentNullException(nameof(capabilityProvider));
            _capabilityValidator = capabilityValidator ?? throw new ArgumentNullException(nameof(capabilityValidator));
            _semanticAnalyzer = semanticAnalyzer ?? throw new ArgumentNullException(nameof(semanticAnalyzer));
        }

        private CompilationResult CompileInternal(string sourceCode, string extension, CompilationOptions options)
        {
            if (sourceCode == null)
                throw new ArgumentNullException(nameof(sourceCode));

            try
            {
                // Parse source code to AST tree (DOD pipeline)
                AstTree astTree = _frontend.ParseToSlab(sourceCode);
                if (astTree.Count == 0)
                {
                    string errorMsg = "Failed to parse source code to AST tree";
                    if (_frontend.LastParseErrors != null && _frontend.LastParseErrors.Any())
                    {
                        errorMsg = string.Join("; ", _frontend.LastParseErrors);
                    }
                    return new CompilationResult
                    {
                        Success = false,
                        Code = Array.Empty<byte>(),
                        SourceFile = extension,
                        Target = options.Target,
                        ErrorMessage = errorMsg
                    };
                }

                // Convert AST tree to HLIR slab (DOD pipeline)
                InstList hlirSlab = _frontend.ConvertToHlirSlab(astTree);
                if (hlirSlab.Count == 0)
                {
                    return new CompilationResult
                    {
                        Success = false,
                        Code = Array.Empty<byte>(),
                        SourceFile = extension,
                        Target = options.Target,
                        ErrorMessage = "Failed to convert AST tree to HLIR slab"
                    };
                }

                // Get the string pool from the frontend for identifier resolution in later stages
                var stringPool = _frontend.StringPool;
                if (stringPool == null)
                {
                    return new CompilationResult
                    {
                        Success = false,
                        Code = Array.Empty<byte>(),
                        SourceFile = extension,
                        Target = options.Target,
                        ErrorMessage = "String pool not available from frontend"
                    };
                }

                // Perform semantic analysis on HLIR slab - use InstList directly
                var semanticResult = _semanticAnalyzer.AnalyzeSlab(hlirSlab, stringPool);
                if (!semanticResult.Success)
                {
                    return new CompilationResult
                    {
                        Success = false,
                        Code = Array.Empty<byte>(),
                        SourceFile = extension,
                        Target = options.Target,
                        ErrorMessage = string.Join("; ", semanticResult.Errors)
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
                    // For slab-based validation, we need to convert slab to IR for validation?
                    // For now, we skip HLIR validation and rely on later stages
                    _ = _capabilityValidator; // suppress unused field warning until slab validation is implemented
                }

                // Optimize HLIR slab to MLIR slab (DOD pipeline) - use InstList directly
                InstList mlirSlab = _midLevelOptimizer.OptimizeSlab(hlirSlab, stringPool, options.OptimizationLevel);
                if (mlirSlab.Count == 0)
                {
                    return new CompilationResult
                    {
                        Success = false,
                        Code = Array.Empty<byte>(),
                        SourceFile = extension,
                        Target = options.Target,
                        ErrorMessage = "Failed to optimize HLIR slab to MLIR slab"
                    };
                }

                // Convert MLIR slab to LLIR slab (DOD pipeline) - now using InstList directly
                InstList llirSlab = _mlirToLlir.TransformSlab(mlirSlab, stringPool);
                if (llirSlab.Count == 0)
                {
                    return new CompilationResult
                    {
                        Success = false,
                        Code = Array.Empty<byte>(),
                        SourceFile = extension,
                        Target = options.Target,
                        ErrorMessage = "Failed to convert MLIR slab to LLIR slab"
                    };
                }

                // Optimize LLIR slab (DOD pipeline) - bridge InstList to InstList and back
                llirSlab = _lowLevelOptimizer.OptimizeSlab(llirSlab, stringPool, options.OptimizationLevel);
                // No need to convert back to uint[] since GenerateFromSlab accepts InstList

                // Generate bytecode from LLIR slab (DOD pipeline)
                var codeGenOptions = new CodeGenOptions
                {
                    Target = options.Target,
                    DispatchStrategy = options.DispatchStrategy,
                    GenerateDebugInfo = options.GenerateDebugInfo,
                    Optimize = options.Optimize
                };

                var code = _codeGenerator.GenerateFromSlab(llirSlab, stringPool, codeGenOptions);

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
                var error = $"Complication failed: {ex.Message}";
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

            if (options.Profile > backendProfile.BaseLevel)
            {
                violations.Add($"Requested capability profile {options.Profile} exceeds backend base level {backendProfile.BaseLevel}");
            }

            violations.AddRange(options.SystemExtensions
                .Where(ext => !backendExtensions.Contains(ext))
                .Select(ext => $"Requested extension '{ext}' is not supported by backend"));

            return violations;
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
            string sourceCode;
            try
            {
                sourceCode = System.IO.File.ReadAllText(sourceFile);
            }
            catch (FileNotFoundException ex)
            {
                return new CompilationResult
                {
                    Success = false,
                    Code = Array.Empty<byte>(),
                    SourceFile = sourceFile,
                    Target = options.Target,
                    ErrorMessage = $"File not found: {ex.FileName}"
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
                    ErrorMessage = $"Failed to read source file: {ex.Message}"
                };
            }

            return CompileInternal(sourceCode, sourceFile, options);
        }
    }

    /// <summary>
    /// Options for compilation
    /// </summary>
    public class CompilationOptions
    {
        /// <summary>
        /// Target architecture to compile for
        /// </summary>
        public Architecture Target { get; set; } = Architecture.Atari2600;

        /// <summary>
        /// Code dispatch strategy to use
        /// </summary>
        public DispatchStrategy DispatchStrategy { get; set; } = DispatchStrategy.DirectThreadedCode;

        /// <summary>
        /// Whether to generate debug information
        /// </summary>
        public bool GenerateDebugInfo { get; set; }

        /// <summary>
        /// Whether to optimize the generated code
        /// </summary>
        public bool Optimize { get; set; } = true;

        /// <summary>
        /// Optimization level
        /// </summary>
        public OptimizationLevel OptimizationLevel { get; set; } = OptimizationLevel.Aggressive;

        /// <summary>
        /// Capability profile to enforce
        /// </summary>
        public CapabilityLevel Profile { get; set; } = CapabilityLevel.L1;

        /// <summary>
        /// Hardware system extensions to enable
        /// </summary>
        public List<string> SystemExtensions { get; set; } = new();

        /// <summary>
        /// Enforcement level for capability validation
        /// </summary>
        public EnforcementLevel Enforcement { get; set; } = EnforcementLevel.Strict;
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
        /// Generated bytecode
        /// </summary>
        public byte[] Code { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Source file that was compiled
        /// </summary>
        public string SourceFile { get; set; } = string.Empty;

        /// <summary>
        /// Target architecture
        /// </summary>
        public Architecture Target { get; set; }

        /// <summary>
        /// Capability profile used
        /// </summary>
        public CapabilityLevel Profile { get; set; }

        /// <summary>
        /// Error message if compilation failed
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;
    }
}