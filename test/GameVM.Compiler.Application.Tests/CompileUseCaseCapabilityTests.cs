using GameVM.Compiler.Application;
using GameVM.Compiler.Core.Interfaces;
using Moq;
using NUnit.Framework;
using System;
using GameVM.Compiler.Core.IR.Hlir;
using GameVM.Compiler.Core.IR.Interfaces;
using GameVM.Compiler.Core.IR.Buffers;

namespace GameVM.Compiler.Application.Tests
{
    [TestFixture]
    public class CompileUseCaseCapabilityTests
    {
        [Test]
        public void Compile_ValidProgram_CompilesSuccessfully()
        {
            // Arrange
            var frontendMock = new Mock<ILanguageFrontend>();
            var midLevelOptimizerMock = new Mock<IMidLevelOptimizer>();
            var lowLevelOptimizerMock = new Mock<ILowLevelOptimizer>();
            var codeGeneratorMock = new Mock<ICodeGenerator>();
            var capabilityProviderMock = new Mock<ICapabilityProvider>();
            var capabilityValidatorMock = new Mock<ICapabilityValidatorService>();

            var compileUseCase = new CompileUseCase(
                frontendMock.Object,
                midLevelOptimizerMock.Object,
                lowLevelOptimizerMock.Object,
                codeGeneratorMock.Object,
                capabilityProviderMock.Object,
                capabilityValidatorMock.Object);

            var pascalFrontend = new PascalFrontend();
            var pascalMidLevelOptimizer = new GameVM.Compiler.Pascal.PascalMidLevelOptimizer();
            var atari2600LowLevelOptimizer = new GameVM.Compiler.Backend.Atari2600.MidToLowLevelTransformer();
            var pascalCodeGenerator = new GameVM.Compiler.Pascal.PascalCodeGenerator();

            var useCase = new CompileUseCase(
                pascalFrontend,
                pascalMidLevelOptimizer,
                atari2600LowLevelOptimizer,
                pascalCodeGenerator,
                capabilityProviderMock.Object,
                capabilityValidatorMock.Object);

            // Act
            var result = useCase.Compile("program test; begin end.", "pas", new CompilationOptions { Target = TargetPlatform.Atari2600 });

            // Assert
            Assert.That(result.Success, Is.True);
        }

        [Test]
        public void Compile_InvalidProgram_ReturnsCompilationResultWithErrors()
        {
            // Arrange
            var frontendMock = new Mock<ILanguageFrontend>();
            var midLevelOptimizerMock = new Mock<IMidLevelOptimizer>();
            var lowLevelOptimizerMock = new Mock<ILowLevelOptimizer>();
            var codeGeneratorMock = new Mock<ICodeGenerator>();
            var capabilityProviderMock = new Mock<ICapabilityProvider>();
            var capabilityValidatorMock = new Mock<ICapabilityValidatorService>();

            var compileUseCase = new CompileUseCase(
                frontendMock.Object,
                midLevelOptimizerMock.Object,
                lowLevelOptimizerMock.Object,
                codeGeneratorMock.Object,
                capabilityProviderMock.Object,
                capabilityValidatorMock.Object);

            var pascalFrontend = new PascalFrontend();
            var pascalMidLevelOptimizer = new GameVM.Compiler.Pascal.PascalMidLevelOptimizer();
            var atari2600LowLevelOptimizer = new GameVM.Compiler.Backend.Atari2600.MidToLowLevelTransformer();
            var pascalCodeGenerator = new GameVM.Compiler.Pascal.PascalCodeGenerator();

            var useCase = new CompileUseCase(
                pascalFrontend,
                pascalMidLevelOptimizer,
                atari2600LowLevelOptimizer,
                pascalCodeGenerator,
                capabilityProviderMock.Object,
                capabilityValidatorMock.Object);

            // Act
            var result = useCase.Compile("program invalid; begin", "pas", new CompilationOptions { Target = TargetPlatform.Atari2600 });

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Is.Not.NullOrEmpty);
        }
    }
}
