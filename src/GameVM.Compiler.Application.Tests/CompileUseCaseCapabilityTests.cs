using Xunit;
using Moq;
using GameVM.Compiler.Application;
using GameVM.Compiler.Application.Services;
using GameVM.Compiler.Core.IR;
using GameVM.Compiler.Core.IR.Interfaces;
using GameVM.Compiler.Core.Interfaces;
using GameVM.Compiler.Core.Enums;
using GameVM.Compiler.Backend.Atari2600;

namespace GameVM.Compiler.Application.Tests;

public class CompileUseCaseCapabilityTests
{
    [Fact]
    public void CompileUseCase_ShouldUseBackendCapabilities_WhenValidating()
    {
        // Arrange
        var mockFrontend = new Mock<ILanguageFrontend>();
        var mockMidOptimizer = new Mock<IMidLevelOptimizer>();
        var mockLowOptimizer = new Mock<ILowLevelOptimizer>();
        var mockTransformer = new Mock<IIRTransformer<MidLevelIR, LowLevelIR>>();
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
            mockValidator.Object);

        var options = new CompilationOptions
        {
            Target = Architecture.Atari2600,
            Profile = CapabilityLevel.L1, // Should match backend
            Enforcement = EnforcementLevel.Strict,
            SystemExtensions = new List<string> { "Ext.Math.Fast" } // Should be supported
        };

        var hlir = new HighLevelIR { Errors = new List<string>() };
        var mlir = new MidLevelIR();
        var llir = new LowLevelIR();
        
        mockFrontend.Setup(f => f.Parse(It.IsAny<string>())).Returns(hlir);
        mockFrontend.Setup(f => f.ConvertToMidLevelIR(hlir)).Returns(mlir);
        mockMidOptimizer.Setup(o => o.Optimize(mlir, It.IsAny<OptimizationLevel>())).Returns(mlir);
        mockTransformer.Setup(t => t.Transform(mlir)).Returns(llir);
        mockLowOptimizer.Setup(o => o.Optimize(llir, It.IsAny<OptimizationLevel>())).Returns(llir);
        mockValidator.Setup(v => v.Validate(It.IsAny<HighLevelIR>(), It.IsAny<CapabilityLevel>(), It.IsAny<List<string>>()))
                    .Returns(new List<string>());

        // Act
        var result = compileUseCase.Execute("test code", ".pas", options);

        // Assert
        Assert.True(result.Success, $"Expected success but got error: {result.ErrorMessage}");
    }

    [Fact]
    public void CompileUseCase_ShouldFail_WhenRequestedProfileExceedsBackendCapabilities()
    {
        // Arrange
        var mockFrontend = new Mock<ILanguageFrontend>();
        var mockMidOptimizer = new Mock<IMidLevelOptimizer>();
        var mockLowOptimizer = new Mock<ILowLevelOptimizer>();
        var mockTransformer = new Mock<IIRTransformer<MidLevelIR, LowLevelIR>>();
        var mockValidator = new Mock<ICapabilityValidatorService>();
        
        var atari2600Generator = new Atari2600CodeGenerator();
        
        var compileUseCase = new CompileUseCase(
            mockFrontend.Object,
            mockMidOptimizer.Object,
            mockLowOptimizer.Object,
            mockTransformer.Object,
            atari2600Generator,
            atari2600Generator,
            mockValidator.Object);

        var options = new CompilationOptions
        {
            Target = Architecture.Atari2600,
            Profile = CapabilityLevel.L5, // Exceeds Atari2600 L1 capabilities
            Enforcement = EnforcementLevel.Strict
        };

        var hlir = new HighLevelIR { Errors = new List<string>() };
        mockFrontend.Setup(f => f.Parse(It.IsAny<string>())).Returns(hlir);

        // Act
        var result = compileUseCase.Execute("test code", ".pas", options);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Backend capability violations", result.ErrorMessage);
    }

    [Fact]
    public void CompileUseCase_ShouldFail_WhenRequestedExtensionNotSupportedByBackend()
    {
        // Arrange
        var mockFrontend = new Mock<ILanguageFrontend>();
        var mockMidOptimizer = new Mock<IMidLevelOptimizer>();
        var mockLowOptimizer = new Mock<ILowLevelOptimizer>();
        var mockTransformer = new Mock<IIRTransformer<MidLevelIR, LowLevelIR>>();
        var mockValidator = new Mock<ICapabilityValidatorService>();
        
        var atari2600Generator = new Atari2600CodeGenerator();
        
        var compileUseCase = new CompileUseCase(
            mockFrontend.Object,
            mockMidOptimizer.Object,
            mockLowOptimizer.Object,
            mockTransformer.Object,
            atari2600Generator,
            atari2600Generator,
            mockValidator.Object);

        var options = new CompilationOptions
        {
            Target = Architecture.Atari2600,
            Profile = CapabilityLevel.L1,
            Enforcement = EnforcementLevel.Strict,
            SystemExtensions = new List<string> { "Ext.Gfx.3D" } // Not supported by Atari2600
        };

        var hlir = new HighLevelIR { Errors = new List<string>() };
        mockFrontend.Setup(f => f.Parse(It.IsAny<string>())).Returns(hlir);

        // Act
        var result = compileUseCase.Execute("test code", ".pas", options);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Backend capability violations", result.ErrorMessage);
    }
}
