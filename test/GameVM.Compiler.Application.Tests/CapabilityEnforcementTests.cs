using NUnit.Framework;
using Moq;
using GameVM.Compiler.Application;
using GameVM.Compiler.Application.Services;
using GameVM.Compiler.Core.IR;
using GameVM.Compiler.Core.Enums;
using GameVM.Compiler.Core.Interfaces;
using GameVM.Compiler.Core.IR.Interfaces;
using GameVM.Compiler.Core.IR.Buffers;

namespace UnitTests.Application
{
    [TestFixture]
    public class CapabilityEnforcementTests
    {
        private Mock<ILanguageFrontend> _frontendMock = null!;
        private Mock<IMidLevelOptimizer> _midLevelOptimizerMock = null!;
        private Mock<ILowLevelOptimizer> _lowLevelOptimizerMock = null!;
        private Mock<IIRSlabTransformer> _mlirToLlirMock = null!;
        private Mock<ICodeGenerator> _codeGeneratorMock = null!;
        private Mock<ICapabilityProvider> _capabilityProviderMock = null!;
        private Mock<ICapabilityValidatorService> _capabilityValidatorMock = null!;
        private Mock<ISemanticAnalyzer> _semanticAnalyzerMock = null!;
        private CompileUseCase _useCase = null!;

        [SetUp]
        public void Setup()
        {
            _frontendMock = new Mock<ILanguageFrontend>();
            _midLevelOptimizerMock = new Mock<IMidLevelOptimizer>();
            _lowLevelOptimizerMock = new Mock<ILowLevelOptimizer>();
            _mlirToLlirMock = new Mock<IIRSlabTransformer>();
            _codeGeneratorMock = new Mock<ICodeGenerator>();
            _capabilityProviderMock = new Mock<ICapabilityProvider>();
            _capabilityValidatorMock = new Mock<ICapabilityValidatorService>();
            _semanticAnalyzerMock = new Mock<ISemanticAnalyzer>();
            _semanticAnalyzerMock.Setup(x => x.AnalyzeSlab(It.IsAny<uint[]>()))
                .Returns(SemanticAnalysisResult.CreateSuccess());

            _useCase = new CompileUseCase(
                _frontendMock.Object,
                _midLevelOptimizerMock.Object,
                _lowLevelOptimizerMock.Object,
                _mlirToLlirMock.Object,
                _codeGeneratorMock.Object,
                _capabilityProviderMock.Object,
                _capabilityValidatorMock.Object,
                _semanticAnalyzerMock.Object
            );
        }

        [Test]
        public void Execute_WhenProfileIsL1_AndBackendViolation_ReturnsFailure()
        {
            // Arrange
            var sourceCode = "procedure DrawScroll; begin end;";
            var options = new CompilationOptions
            {
                Target = Architecture.Atari2600,
                Profile = CapabilityLevel.L3, // Request L3 but backend only supports L1
                Enforcement = EnforcementLevel.Strict
            };

            var astSlab = new uint[] { 0x47564D56, 0, 1, 1, 0, 0, 0, 0 }; // AST slab with 1 element
            var hlirSlab = new uint[] { 0x47564D56, 1, 1, 1, 0, 0, 0, 0 }; // HLIR slab with 1 element
            var mlirSlab = new uint[] { 0x47564D56, 2, 1, 1, 0, 0, 0, 0 }; // MLIR slab with 1 element
            var llirSlab = new uint[] { 0x47564D56, 3, 1, 1, 0, 0, 0, 0 }; // LLIR slab with 1 element
            var bytecode = new byte[4096];
            
                        // Mock the frontend to return AST slab and HLIR slab, and a StringPool
            _frontendMock.Setup(f => f.ParseToSlab(It.IsAny<string>()))
                .Returns(astSlab);
            _frontendMock.Setup(f => f.ConvertToHlirSlab(It.IsAny<uint[]>()))
                .Returns(hlirSlab);
            _frontendMock.Setup(f => f.StringPool)
                .Returns(new StringPool());

            // Mock mid-level optimizer to return MLIR slab
            _midLevelOptimizerMock.Setup(o => o.OptimizeSlab(It.IsAny<uint[]>(), It.IsAny<StringPool>(), It.IsAny<OptimizationLevel>()))
                .Returns(mlirSlab);

            // Mock MLIR to LLIR transformer - use TransformSlab method
            _mlirToLlirMock.Setup(t => t.TransformSlab(It.IsAny<uint[]>(), It.IsAny<StringPool>()))
                .Returns(llirSlab);

            // Mock low-level optimizer
            _lowLevelOptimizerMock.Setup(o => o.OptimizeSlab(It.IsAny<uint[]>(), It.IsAny<StringPool>(), It.IsAny<OptimizationLevel>()))
                .Returns(llirSlab);

            // Mock code generator
            _codeGeneratorMock.Setup(g => g.GenerateFromSlab(It.IsAny<uint[]>(), It.IsAny<StringPool>(), It.IsAny<CodeGenOptions>()))
                .Returns(bytecode);

            // Mock capability provider to return backend capabilities (L1 only)
            var backendProfile = new CapabilityProfile { BaseLevel = CapabilityLevel.L1 };
            _capabilityProviderMock.Setup(p => p.GetCapabilityProfile())
                .Returns(backendProfile);
            _capabilityProviderMock.Setup(p => p.GetSupportedExtensions())
                .Returns(new List<string>());

            // Act
            var result = _useCase.Execute(sourceCode, ".pas", options);

            // Assert - Should fail because requested profile L3 exceeds backend L1
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("exceeds backend base capability"));
        }

        [Test]
        public void Execute_WithValidProfile_ReturnsSuccess()
        {
            // Arrange
            var sourceCode = "program Test; begin end.";
            var options = new CompilationOptions
            {
                Target = Architecture.Atari2600,
                Profile = CapabilityLevel.L1,
                Enforcement = EnforcementLevel.Strict
            };

            var astSlab = new uint[] { 0x47564D56, 0, 1, 1, 0, 0, 0, 0 };
            var hlirSlab = new uint[] { 0x47564D56, 1, 1, 1, 0, 0, 0, 0 };
            var mlirSlab = new uint[] { 0x47564D56, 2, 1, 1, 0, 0, 0, 0 };
            var llirSlab = new uint[] { 0x47564D56, 3, 1, 1, 0, 0, 0, 0 };
            var bytecode = new byte[4096];
            
_frontendMock.Setup(f => f.ParseToSlab(It.IsAny<string>()))
                 .Returns(astSlab);
             _frontendMock.Setup(f => f.ConvertToHlirSlab(It.IsAny<uint[]>()))
                 .Returns(hlirSlab);
             _frontendMock.Setup(f => f.StringPool)
                 .Returns(new StringPool());

             _midLevelOptimizerMock.Setup(o => o.OptimizeSlab(It.IsAny<uint[]>(), It.IsAny<StringPool>(), It.IsAny<OptimizationLevel>()))
                .Returns(mlirSlab);

            _mlirToLlirMock.Setup(t => t.TransformSlab(It.IsAny<uint[]>(), It.IsAny<StringPool>()))
                .Returns(llirSlab);

            _lowLevelOptimizerMock.Setup(o => o.OptimizeSlab(It.IsAny<uint[]>(), It.IsAny<StringPool>(), It.IsAny<OptimizationLevel>()))
                .Returns(llirSlab);

            _codeGeneratorMock.Setup(g => g.GenerateFromSlab(It.IsAny<uint[]>(), It.IsAny<StringPool>(), It.IsAny<CodeGenOptions>()))
                .Returns(bytecode);

            var backendProfile = new CapabilityProfile { BaseLevel = CapabilityLevel.L1 };
            _capabilityProviderMock.Setup(p => p.GetCapabilityProfile())
                .Returns(backendProfile);
            _capabilityProviderMock.Setup(p => p.GetSupportedExtensions())
                .Returns(new List<string>());

            // Act
            var result = _useCase.Execute(sourceCode, ".pas", options);

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.Code, Is.EqualTo(bytecode));
        }
    }
}
