using NUnit.Framework;
using Moq;
using GameVM.Compiler.Application.Services;
using GameVM.Compiler.Core.IR.Interfaces;
using GameVM.Compiler.Core.Interfaces;
using GameVM.Compiler.Core.Enums;
using GameVM.Compiler.Backend.Atari2600;
using GameVM.Compiler.Core.SemanticAnalysis;
using GameVM.Compiler.Core.IR.Buffers;
using GameVM.Compiler.Core.IR.Slab;
using GameVM.Compiler.Core.IR.Soa;

namespace GameVM.Compiler.Application.Tests
{
    public class CompileUseCaseCapabilityTests
    {
        private static InstList CreateInstList(byte[] tags)
        {
            return new InstList(
                tags,
                new ushort[tags.Length],
                new ushort[tags.Length],
                new uint[tags.Length * 4],
                Array.Empty<uint>(),
                new uint[tags.Length],
                new int[tags.Length],
                tags.Length,
                0);
        }

        private static InstList CreateAstSlab()
        {
            return CreateInstList(new byte[] { 0x01 }); // dummy AST_ASSIGN
        }

        private static InstList CreateHlirSlab()
        {
            // Create a valid HLIR slab with a real ASSIGN instruction
            // Use MLIR_ASSIGN (130) to avoid being flagged as invalid AST-level instruction
            return CreateInstList(new byte[] { 130 }); // MLIR_ASSIGN
        }

        private static InstList CreateMlirSlab()
        {
            return CreateInstList(new byte[] { 0x80 }); // MLIR_LABEL
        }

        private static InstList CreateLlirSlab()
        {
            return CreateInstList(new byte[] { 0x20 }); // LLIR_LABEL
        }

        [Test]
        public void CompileUseCase_ShouldUseBackendCapabilities_WhenValidating()
        {
            // Arrange
            var mockFrontend = new Mock<ILanguageFrontend>();
            var mockMidOptimizer = new Mock<IMidLevelOptimizer>();
            var mockLowOptimizer = new Mock<ILowLevelOptimizer>();
            var mockTransformer = new Mock<IIRSlabTransformer>();
            var mockValidator = new Mock<ICapabilityValidatorService>();

            // Use real Atari2600 backend to test actual capability integration
            var atari2600Generator = new Atari2600CodeGenerator();

            var options = new CompilationOptions
            {
                Target = Architecture.Atari2600,
                Profile = CapabilityLevel.L1, // Should match backend
                Enforcement = EnforcementLevel.Strict,
                SystemExtensions = new List<string> { "Ext.Math.Fast" } // Should be supported
            };

            // Create valid slabs for DOD pipeline
            var astSlab = CreateAstSlab();
            var hlirSlab = CreateHlirSlab();
            var mlirSlab = CreateMlirSlab();
            var optimizedLlirSlab = CreateLlirSlab();
            var expectedBytecode = new byte[] { 0x4C, 0xA9, 0x00, 0x8D, 0x09, 0x09 };

            var stringPool = new StringPool();

            mockFrontend.Setup(f => f.ParseToSlab(It.IsAny<string>())).Returns(astSlab);
            mockFrontend.Setup(f => f.ConvertToHlirSlab(It.IsAny<InstList>())).Returns(hlirSlab);
            mockFrontend.SetupGet(f => f.StringPool).Returns(stringPool);
            
            mockMidOptimizer.Setup(o => o.OptimizeSlab(It.IsAny<InstList>(), It.IsAny<StringPool>(), It.IsAny<OptimizationLevel>())).Returns(mlirSlab);
            mockTransformer.Setup(t => t.TransformSlab(It.IsAny<InstList>(), It.IsAny<StringPool>())).Returns(optimizedLlirSlab);
            mockLowOptimizer.Setup(o => o.OptimizeSlab(It.IsAny<InstList>(), It.IsAny<StringPool>(), It.IsAny<OptimizationLevel>())).Returns(optimizedLlirSlab);
            
            // Mock validator - we don't care what it returns for this test
            mockValidator.Setup(v => v.Validate(It.IsAny<uint[]>(), It.IsAny<CapabilityLevel>(), It.IsAny<List<string>>()))
                        .Returns(new List<string>());

            // Mock code generator
            var mockGenerator = new Mock<ICodeGenerator>();
            mockGenerator.Setup(g => g.GenerateFromSlab(It.IsAny<InstList>(), It.IsAny<StringPool>(), It.IsAny<CodeGenOptions>()))
                .Returns(expectedBytecode);
            
            var compileUseCase = new CompileUseCase(
                mockFrontend.Object,
                mockMidOptimizer.Object,
                mockLowOptimizer.Object,
                mockTransformer.Object,
                mockGenerator.Object,
                atari2600Generator,
                mockValidator.Object,
                new BasicSemanticAnalyzer());

            // Act
            var result = compileUseCase.Execute("test code", ".pas", options);

            // Assert
            Assert.That(result.Success, Is.True, $"Expected success but got error: {result.ErrorMessage}");
        }

        [Test]
        public void CompileUseCase_ShouldFail_WhenRequestedProfileExceedsBackendCapabilities()
        {
            // Arrange
            var mockFrontend = new Mock<ILanguageFrontend>();
            var mockMidOptimizer = new Mock<IMidLevelOptimizer>();
            var mockLowOptimizer = new Mock<ILowLevelOptimizer>();
            var mockTransformer = new Mock<IIRSlabTransformer>();
            var mockValidator = new Mock<ICapabilityValidatorService>();

            var atari2600Generator = new Atari2600CodeGenerator();

            // Request a profile higher than what the backend supports (Atari2600 is L1)
            var options = new CompilationOptions
            {
                Target = Architecture.Atari2600,
                Profile = CapabilityLevel.L3, // Higher than Atari2600's L1
                Enforcement = EnforcementLevel.Strict,
                SystemExtensions = new List<string>() // No extensions needed to trigger this failure
            };

            var astSlab = CreateAstSlab();
            var hlirSlab = CreateHlirSlab();
            var mlirSlab = CreateMlirSlab();
            var optimizedLlirSlab = CreateLlirSlab();

            var stringPool = new StringPool();

            mockFrontend.Setup(f => f.ParseToSlab(It.IsAny<string>())).Returns(astSlab);
            mockFrontend.Setup(f => f.ConvertToHlirSlab(It.IsAny<InstList>())).Returns(hlirSlab);
            mockFrontend.SetupGet(f => f.StringPool).Returns(stringPool);
            
            mockMidOptimizer.Setup(o => o.OptimizeSlab(It.IsAny<InstList>(), It.IsAny<StringPool>(), It.IsAny<OptimizationLevel>())).Returns(mlirSlab);
            mockTransformer.Setup(t => t.TransformSlab(It.IsAny<InstList>(), It.IsAny<StringPool>())).Returns(optimizedLlirSlab);
            mockLowOptimizer.Setup(o => o.OptimizeSlab(It.IsAny<InstList>(), It.IsAny<StringPool>(), It.IsAny<OptimizationLevel>())).Returns(optimizedLlirSlab);
            
            // Mock validator to return specific error about profile exceeding backend
            mockValidator.Setup(v => v.Validate(It.IsAny<uint[]>(), It.IsAny<CapabilityLevel>(), It.IsAny<List<string>>()))
                        .Returns(new List<string> { $"Requested profile {options.Profile} exceeds backend base capability" });

            // Mock code generator
            var mockGenerator = new Mock<ICodeGenerator>();
            
            var compileUseCase = new CompileUseCase(
                mockFrontend.Object,
                mockMidOptimizer.Object,
                mockLowOptimizer.Object,
                mockTransformer.Object,
                mockGenerator.Object,
                atari2600Generator,
                mockValidator.Object,
                new BasicSemanticAnalyzer());

            // Act
            var result = compileUseCase.Execute("test code", ".pas", options);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("exceeds backend base capability"));
        }
    }
}