using NUnit.Framework;
using Moq;
using GameVM.Compiler.Application;
using GameVM.Compiler.Application.Services;
using GameVM.Compiler.Core.IR;
using GameVM.Compiler.Core.Enums;
using GameVM.Compiler.Core.Interfaces;
using GameVM.Compiler.Core.IR.Interfaces;
using GameVM.Compiler.Core.IR.Buffers;
using GameVM.Compiler.Core.IR.Soa;
using GameVM.Compiler.Core.IR.Slab;
using System.Collections.Generic;

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
            _semanticAnalyzerMock.Setup(x => x.AnalyzeSlab(It.IsAny<InstList>(), It.IsAny<StringPool>()))
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

            var astSlab = new InstList(
                new byte[] { 0x01 },
                new ushort[] { 0x00 },
                new ushort[] { 0x02 },
                new uint[] { 0x00000000, 0x00000000 },
                new uint[] { 0x00000001, 0x00000002 },
                new uint[] { 0x00000004 },
                new int[] { 0 },
                1,
                2);
            var hlirSlab = new InstList(
                new byte[] { 0x01 },
                new ushort[] { 0x00 },
                new ushort[] { 0x02 },
                new uint[] { 0x00000000, 0x00000000 },
                new uint[] { 0x00000001, 0x00000002 },
                new uint[] { 0x00000004 },
                new int[] { 0 },
                1,
                2);
            
            _frontendMock.Setup(f => f.ParseToSlab(It.IsAny<string>()))
                .Returns(astSlab);
            _frontendMock.Setup(f => f.ConvertToHlirSlab(It.IsAny<InstList>()))
                .Returns(hlirSlab);
            _frontendMock.Setup(f => f.StringPool)
                .Returns(new StringPool());

            var mlirSlab = new InstList(
                new byte[] { 0x47, 0x56, 0x4D, 0x56, 2, 1, 1, 1 },
                new ushort[] { 0x0000 },
                new ushort[] { 0x0000 },
                new uint[] { 0x00000000, 0x00000000, 0x00000000, 0x00000000 },
                new uint[] { },
                new uint[] { 0x00000000 },
                new int[] { 0 },
                1,
                0);
            var llirSlab = new InstList(
                new byte[] { 0x47, 0x56, 0x4D, 0x56, 3, 1, 1, 1 },
                new ushort[] { 0x0000 },
                new ushort[] { 0x0000 },
                new uint[] { 0x00000000, 0x00000000, 0x00000000, 0x00000000 },
                new uint[] { },
                new uint[] { 0x00000000 },
                new int[] { 0 },
                1,
                0);
            var bytecode = new byte[4096];

            // Mock mid-level optimizer to return MLIR slab
            _midLevelOptimizerMock.Setup(o => o.OptimizeSlab(It.IsAny<InstList>(), It.IsAny<StringPool>(), It.IsAny<OptimizationLevel>()))
                .Returns(mlirSlab);

            // Mock MLIR to LLIR transformer - use TransformSlab method
            _mlirToLlirMock.Setup(t => t.TransformSlab(It.IsAny<InstList>(), It.IsAny<StringPool>()))
                .Returns(llirSlab);

            // Mock low-level optimizer
            _lowLevelOptimizerMock.Setup(o => o.OptimizeSlab(It.IsAny<InstList>(), It.IsAny<StringPool>(), It.IsAny<OptimizationLevel>()))
                .Returns(llirSlab);

            // Mock code generator
            _codeGeneratorMock.Setup(g => g.GenerateFromSlab(It.IsAny<InstList>(), It.IsAny<StringPool>(), It.IsAny<CodeGenOptions>()))
                .Returns(bytecode);

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

            var astSlab = new InstList(
                new byte[] { 0x01 },
                new ushort[] { 0x00 },
                new ushort[] { 0x02 },
                new uint[] { 0x00000000, 0x00000000 },
                new uint[] { 0x00000001, 0x00000002 },
                new uint[] { 0x00000004 },
                new int[] { 0 },
                1,
                2);
            var hlirSlab = new InstList(
                new byte[] { 0x01 },
                new ushort[] { 0x00 },
                new ushort[] { 0x02 },
                new uint[] { 0x00000000, 0x00000000 },
                new uint[] { 0x00000001, 0x00000002 },
                new uint[] { 0x00000004 },
                new int[] { 0 },
                1,
                2);
            
            _frontendMock.Setup(f => f.ParseToSlab(It.IsAny<string>()))
                .Returns(astSlab);
            _frontendMock.Setup(f => f.ConvertToHlirSlab(It.IsAny<InstList>()))
                .Returns(hlirSlab);
            _frontendMock.Setup(f => f.StringPool)
                .Returns(new StringPool());

            var mlirSlab = new InstList(
                new byte[] { 0x47, 0x56, 0x4D, 0x56, 2, 1, 1, 1 },
                new ushort[] { 0x0000 },
                new ushort[] { 0x0000 },
                new uint[] { 0x00000000, 0x00000000, 0x00000000, 0x00000000 },
                new uint[] { },
                new uint[] { 0x00000000 },
                new int[] { 0 },
                1,
                0);
            var llirSlab = new InstList(
                new byte[] { 0x47, 0x56, 0x4D, 0x56, 3, 1, 1, 1 },
                new ushort[] { 0x0000 },
                new ushort[] { 0x0000 },
                new uint[] { 0x00000000, 0x00000000, 0x00000000, 0x00000000 },
                new uint[] { },
                new uint[] { 0x00000000 },
                new int[] { 0 },
                1,
                0);
            var bytecode = new byte[4096];

            _midLevelOptimizerMock.Setup(o => o.OptimizeSlab(It.IsAny<InstList>(), It.IsAny<StringPool>(), It.IsAny<OptimizationLevel>()))
                .Returns(mlirSlab);
            _mlirToLlirMock.Setup(t => t.TransformSlab(It.IsAny<InstList>(), It.IsAny<StringPool>()))
                .Returns(llirSlab);
            _lowLevelOptimizerMock.Setup(o => o.OptimizeSlab(It.IsAny<InstList>(), It.IsAny<StringPool>(), It.IsAny<OptimizationLevel>()))
                .Returns(llirSlab);
            _codeGeneratorMock.Setup(g => g.GenerateFromSlab(It.IsAny<InstList>(), It.IsAny<StringPool>(), It.IsAny<CodeGenOptions>()))
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