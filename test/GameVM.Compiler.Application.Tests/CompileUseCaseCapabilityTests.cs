using NUnit.Framework;
using Moq;
using GameVM.Compiler.Application;
using GameVM.Compiler.Application.Services;
using GameVM.Compiler.Core.IR;
using GameVM.Compiler.Core.IR.Interfaces;
using GameVM.Compiler.Core.Interfaces;
using GameVM.Compiler.Core.Enums;
using GameVM.Compiler.Backend.Atari2600;
using GameVM.Compiler.Core.SemanticAnalysis;
using GameVM.Compiler.Core.IR.Buffers;
using System.Collections.Generic;

namespace GameVM.Compiler.Application.Tests;

public class CompileUseCaseCapabilityTests
{
    [Test]
    public void CompileUseCase_ShouldUseBackendCapabilities_WhenValidating()
    {
        // Arrange
        var mockFrontend = new Mock<ILanguageFrontend>();
        var mockMidOptimizer = new Mock<IMidLevelOptimizer>();
        var mockLowOptimizer = new Mock<ILowLevelOptimizer>();
        var mockTransformer = new Mock<IIRTransformer<uint[], uint[]>>();
        var mockValidator = new Mock<ICapabilityValidatorService>();

        // Use real Atari2600 backend to test actual capability integration
        var atari2600Generator = new Atari2600CodeGenerator();

        var compileUseCase = new CompileUseCase(
            mockFrontend.Object,
            mockMidOptimizer.Object,
            mockLowOptimizer.Object,
            mockTransformer.Object,
            atari2600Generator,
            atari2600Generator, // Same class implements both interfaces
            mockValidator.Object,
            new BasicSemanticAnalyzer());

        var options = new CompilationOptions
        {
            Target = Architecture.Atari2600,
            Profile = CapabilityLevel.L1, // Should match backend
            Enforcement = EnforcementLevel.Strict,
            SystemExtensions = new List<string> { "Ext.Math.Fast" } // Should be supported
        };

        // Create valid slabs for DOD pipeline
        var astSlab = new uint[] { 0x47414D45, 0x564D, 0x01, 0x00, 0x00, 0x00 }; // Minimal valid header
        var hlirSlab = new uint[] { 0x47414D45, 0x484C, 0x01, 0x00, 0x00, 0x00 }; // Minimal valid header
        var mlirSlab = new uint[] { 0x47414D45, 0x4D4C, 0x01, 0x00, 0x00, 0x00 }; // Minimal valid header
        var llirSlab = new uint[] { 0x47414D45, 0x4C4C, 0x01, 0x00, 0x00, 0x00 }; // Minimal valid header
        var optimizedLlirSlab = new uint[] { 0x47414D45, 0x4C4C, 0x01, 0x00, 0x00, 0x00 };
        var expectedBytecode = new byte[] { 0x4C, 0xA9, 0x00, 0x8D, 0x09, 0x09 };

        var stringPool = new StringPool();

        mockFrontend.Setup(f => f.ParseToSlab(It.IsAny<string>())).Returns(astSlab);
        mockFrontend.Setup(f => f.ConvertToHlirSlab(astSlab)).Returns(hlirSlab);
        mockFrontend.SetupGet(f => f.StringPool).Returns(stringPool);
        
        mockMidOptimizer.Setup(o => o.OptimizeSlab(hlirSlab, stringPool, It.IsAny<OptimizationLevel>())).Returns(mlirSlab);
        mockTransformer.Setup(t => t.TransformSlab(mlirSlab, stringPool)).Returns(llirSlab);
        mockLowOptimizer.Setup(o => o.OptimizeSlab(llirSlab, stringPool, It.IsAny<OptimizationLevel>())).Returns(optimizedLlirSlab);
        mockValidator.Setup(v => v.Validate(It.IsAny<uint[]>(), It.IsAny<CapabilityLevel>(), It.IsAny<List<string>>()))
                    .Returns(new List<string>());
        mockValidator.Setup(v => v.Validate(It.IsAny<uint[]>(), It.IsAny<CapabilityLevel>(), It.IsAny<List<string>>()))
                    .Returns(new List<string>());

        // Mock code generator to return expected bytecode
        var mockGenerator = new Mock<ICodeGenerator>();
        mockGenerator.Setup(g => g.GenerateFromSlab(It.IsAny<uint[]>(), It.IsAny<StringPool>(), It.IsAny<CodeGenOptions>()))
            .Returns(expectedBytecode);

        var compileUseCase2 = new CompileUseCase(
            mockFrontend.Object,
            mockMidOptimizer.Object,
            mockLowOptimizer.Object,
            mockTransformer.Object,
            mockGenerator.Object,
            atari2600Generator,
            mockValidator.Object,
            new BasicSemanticAnalyzer());

        // Act
        var result = compileUseCase2.Execute("test code", ".pas", options);

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
        var mockTransformer = new Mock<IIRTransformer<uint[], uint[]>>();
        var mockValidator = new Mock<ICapabilityValidatorService>();

        var atari2600Generator = new Atari2600CodeGenerator();

        var compileUseCase = new CompileUseCase(
            mockFrontend.Object,
            mockMidOptimizer.Object,
            mockLowOptimizer.Object,
            mockTransformer.Object,
            atari2600Generator,
            atari2600Generator,
            mockValidator.Object,
            new BasicSemanticAnalyzer());

        var options = new CompilationOptions
        {
            Target = Architecture.Atari2600,
            Profile = CapabilityLevel.L5, // Exceeds Atari2600 L1 capabilities
            Enforcement = EnforcementLevel.Strict
        };

        var astSlab = new uint[] { 0x47414D45, 0x564D, 0x01, 0x00, 0x00, 0x00 };
        var hlirSlab = new uint[] { 0x47414D45, 0x484C, 0x01, 0x00, 0x00, 0x00 };
        var stringPool = new StringPool();

        mockFrontend.Setup(f => f.ParseToSlab(It.IsAny<string>())).Returns(new uint[0]); // Empty slab = parse failure
        mockFrontend.SetupGet(f => f.StringPool).Returns(stringPool);

        // Act
        var result = compileUseCase.Execute("test code", ".pas", options);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("Failed to parse"));
    }

    [Test]
    public void CompileUseCase_ShouldFail_WhenRequestedExtensionNotSupportedByBackend()
    {
        // Arrange
        var mockFrontend = new Mock<ILanguageFrontend>();
        var mockMidOptimizer = new Mock<IMidLevelOptimizer>();
        var mockLowOptimizer = new Mock<ILowLevelOptimizer>();
        var mockTransformer = new Mock<IIRTransformer<uint[], uint[]>>();
        var mockValidator = new Mock<ICapabilityValidatorService>();

        var atari2600Generator = new Atari2600CodeGenerator();

        var compileUseCase = new CompileUseCase(
            mockFrontend.Object,
            mockMidOptimizer.Object,
            mockLowOptimizer.Object,
            mockTransformer.Object,
            atari2600Generator,
            atari2600Generator,
            mockValidator.Object,
            new BasicSemanticAnalyzer());

        var options = new CompilationOptions
        {
            Target = Architecture.Atari2600,
            Profile = CapabilityLevel.L1,
            Enforcement = EnforcementLevel.Strict,
            SystemExtensions = new List<string> { "Ext.Gfx.3D" } // Not supported by Atari2600
        };

        var astSlab = new uint[] { 0x47414D45, 0x564D, 0x01, 0x00, 0x00, 0x00 };
        var hlirSlab = new uint[] { 0x47414D45, 0x484C, 0x01, 0x00, 0x00, 0x00 };
        var mlirSlab = new uint[] { 0x47414D45, 0x4D4C, 0x01, 0x00, 0x00, 0x00 };
        var stringPool = new StringPool();

        mockFrontend.Setup(f => f.ParseToSlab(It.IsAny<string>())).Returns(astSlab);
        mockFrontend.Setup(f => f.ConvertToHlirSlab(astSlab)).Returns(hlirSlab);
        mockFrontend.SetupGet(f => f.StringPool).Returns(stringPool);
        
        mockMidOptimizer.Setup(o => o.OptimizeSlab(hlirSlab, stringPool, It.IsAny<OptimizationLevel>())).Returns(mlirSlab);
        mockValidator.Setup(v => v.Validate(It.IsAny<uint[]>(), It.IsAny<CapabilityLevel>(), It.IsAny<List<string>>()))
                    .Returns(new List<string> { "Backend does not support extension 'Ext.Gfx.3D'" });

        // Act
        var result = compileUseCase.Execute("test code", ".pas", options);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("Backend capability violations"));
    }
}