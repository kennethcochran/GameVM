using NUnit.Framework;
using Moq;
using GameVM.Compiler.Application;
using GameVM.Compiler.Application.Services;
using GameVM.Compiler.Core.IR;
using GameVM.Compiler.Core.Enums;
using GameVM.Compiler.Core.Interfaces;
using GameVM.Compiler.Core.IR.Interfaces;
using System.Collections.Generic;

namespace UnitTests.Application
{
    [TestFixture]
    public class CapabilityEnforcementTests
    {
        private Mock<ILanguageFrontend> _frontendMock = null!;
        private Mock<IMidLevelOptimizer> _midLevelOptimizerMock = null!;
        private Mock<ILowLevelOptimizer> _lowLevelOptimizerMock = null!;
        private Mock<IIRTransformer<MidLevelIR, LowLevelIR>> _mlirToLlirMock = null!;
        private Mock<ICodeGenerator> _codeGeneratorMock = null!;
        private Mock<ICapabilityProvider> _capabilityProviderMock = null!;
        private Mock<ICapabilityValidatorService> _capabilityValidatorMock = null!;
        private CompileUseCase _useCase = null!;

        [SetUp]
        public void Setup()
        {
            _frontendMock = new Mock<ILanguageFrontend>();
            _midLevelOptimizerMock = new Mock<IMidLevelOptimizer>();
            _lowLevelOptimizerMock = new Mock<ILowLevelOptimizer>();
            _mlirToLlirMock = new Mock<IIRTransformer<MidLevelIR, LowLevelIR>>();
            _codeGeneratorMock = new Mock<ICodeGenerator>();
            _capabilityProviderMock = new Mock<ICapabilityProvider>();
            _capabilityValidatorMock = new Mock<ICapabilityValidatorService>();

            _useCase = new CompileUseCase(
                _frontendMock.Object,
                _midLevelOptimizerMock.Object,
                _lowLevelOptimizerMock.Object,
                _mlirToLlirMock.Object,
                _codeGeneratorMock.Object,
                _capabilityProviderMock.Object,
                _capabilityValidatorMock.Object
            );
        }

        [Test]
        public void Execute_WhenProfileIsL1_AndHlirCallsL3Function_ReturnsFailure()
        {
            // Arrange
            var sourceCode = "procedure DrawScroll; begin end;";
            var options = new CompilationOptions
            {
                Target = Architecture.Atari2600,
                Profile = CapabilityLevel.L1,
                Enforcement = EnforcementLevel.Strict
            };

            var hlir = new HighLevelIR();
            
            // Mock the frontend to return our HLIR
            _frontendMock.Setup(f => f.Parse(It.IsAny<string>()))
                .Returns(hlir);

            // Mock capability provider to return backend capabilities
            var backendProfile = new CapabilityProfile { BaseLevel = CapabilityLevel.L1 };
            _capabilityProviderMock.Setup(p => p.GetCapabilityProfile())
                .Returns(backendProfile);
            _capabilityProviderMock.Setup(p => p.GetSupportedExtensions())
                .Returns(new List<string>());

            // Mock capability validation to return L3 requirement
            _capabilityValidatorMock.Setup(v => v.Validate(hlir, backendProfile.BaseLevel, backendProfile.Extensions.ToList()))
                .Returns(new List<string> { "Function 'DrawScroll' requires L3" });

            // Act
            var result = _useCase.Execute(sourceCode, ".pas", options);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("requires L3"));
        }
    }
}
