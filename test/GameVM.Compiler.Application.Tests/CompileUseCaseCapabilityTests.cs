using GameVM.Compiler.Application;
using GameVM.Compiler.Core.Interfaces;
using NSubstitute;
using NUnit.Framework;
using System;

namespace GameVM.Compiler.Application.Tests
{
    [TestFixture]
    public class CompileUseCaseCapabilityTests
    {
        [Test]
        public void Compile_ValidProgram_CompilesSuccessfully()
        {
            // Arrange
            var frontend = Substitute.For<ILanguageFrontend>();
            var midLevelOptimizer = Substitute.For<IMidLevelOptimizer>();
            var lowLevelOptimizer = Substitute.For<ILowLevelOptimizer>();
            var codeGenerator = Substitute.For<ICodeGenerator>();
            var capabilityProvider = Substitute.For<ICapabilityProvider>();
            var capabilityValidator = Substitute.For<ICapabilityValidatorService>();

            var compileUseCase = new CompileUseCase(
                frontend,
                midLevelOptimizer,
                lowLevelOptimizer,
                codeGenerator,
                capabilityProvider,
                capabilityValidator);

            var pascalFrontend = new PascalFrontend();
            var pascalMidLevelOptimizer = new GameVM.Compiler.Pascal.PascalMidLevelOptimizer();
            var atari2600LowLevelOptimizer = new GameVM.Compiler.Backend.Atari2600.MidToLowLevelTransformer();
            var pascalCodeGenerator = new GameVM.Compiler.Pascal.PascalCodeGenerator();

            var useCase = new CompileUseCase(
                pascalFrontend,
                pascalMidLevelOptimizer,
                atari2600LowLevelOptimizer,
                pascalCodeGenerator,
                capabilityProvider,
                capabilityValidator);

            // Act
            var result = useCase.Compile("program test; begin end.", "pas", new CompilationOptions { Target = TargetPlatform.Atari2600 });

            // Assert
            Assert.That(result.Success, Is.True);
        }

        [Test]
        public void Compile_InvalidProgram_ReturnsCompilationResultWithErrors()
        {
            // Arrange
            var frontend = Substitute.For<ILanguageFrontend>();
            var midLevelOptimizer = Substitute.For<IMidLevelOptimizer>();
            var lowLevelOptimizer = Substitute.For<ILowLevelOptimizer>();
            var codeGenerator = Substitute.For<ICodeGenerator>();
            var capabilityProvider = Substitute.For<ICapabilityProvider>();
            var capabilityValidator = Substitute.For<ICapabilityValidatorService>();

            var compileUseCase = new CompileUseCase(
                frontend,
                midLevelOptimizer,
                lowLevelOptimizer,
                codeGenerator,
                capabilityProvider,
                capabilityValidator);

            var pascalFrontend = new PascalFrontend();
            var pascalMidLevelOptimizer = new GameVM.Compiler.Pascal.PascalMidLevelOptimizer();
            var atari2600LowLevelOptimizer = new GameVM.Compiler.Backend.Atari2600.MidToLowLevelTransformer();
            var pascalCodeGenerator = new GameVM.Compiler.Pascal.PascalCodeGenerator();

            var useCase = new CompileUseCase(
                pascalFrontend,
                pascalMidLevelOptimizer,
                atari2600LowLevelOptimizer,
                pascalCodeGenerator,
                capabilityProvider,
                capabilityValidator);

            // Act
            var result = useCase.Compile("program invalid; begin", "pas", new CompilationOptions { Target = TargetPlatform.Atari2600 });

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Is.Not.NullOrEmpty);
        }
    }
}
