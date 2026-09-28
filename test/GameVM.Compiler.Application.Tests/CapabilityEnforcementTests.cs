using System;
using System.Collections.Generic;
using Moq;
using NUnit.Framework;
using GameVM.Compiler.Application;
using GameVM.Compiler.Application.Services;
using GameVM.Compiler.Core.IR;
using GameVM.Compiler.Core.Enums;
using GameVM.Compiler.Core.Interfaces;
using GameVM.Compiler.Core.IR.Interfaces;
using GameVM.Compiler.Core.IR.Buffers;
using GameVM.Compiler.Core.IR.Soa;
using GameVM.Compiler.Core.IR.Hlir;

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
            var hlirBuilder = new HlirBuilder();
            hlirBuilder.Add((byte)HlirNodeKind.Nop);
            _frontendMock.Setup(x => x.ParseToHlir(It.IsAny<string>()))
                .Returns(new ParseResult(hlirBuilder.Build(), default));
            _frontendMock.Setup(x => x.StringPool).Returns(new StringPool());
            _midLevelOptimizerMock.Setup(x => x.OptimizeSlab(It.IsAny<InstList>(), It.IsAny<StringPool>(), It.IsAny<OptimizationLevel>()))
                .Returns(new InstList(
                    new byte[] { 0x80 },
                    new ushort[] { 0x0000 },
                    new ushort[] { 0x0000 },
                    new uint[] { 0x00000000 },
                    new uint[] { },
                    new uint[] { 0x00000000 },
                    new int[] { 0 },
                    1,
                    0));
            _mlirToLlirMock.Setup(x => x.TransformSlab(It.IsAny<InstList>(), It.IsAny<StringPool>()))
                .Returns(new InstList(
                    new byte[] { 0x47, 0x49, 0x4D, 0x4C, 1, 3, 0, 0 },
                    new ushort[] { 0x0000 },
                    new ushort[] { 0x0000 },
                    new uint[] { 0x00000000 },
                    new uint[] { },
                    new uint[] { 0x00000000 },
                    new int[] { 0 },
                    1,
                    0));
            _lowLevelOptimizerMock.Setup(x => x.OptimizeSlab(It.IsAny<InstList>(), It.IsAny<StringPool>(), It.IsAny<OptimizationLevel>()))
                .Returns(new InstList(
                    new byte[] { 0x47, 0x49, 0x4D, 0x4C, 1, 3, 0, 0 },
                    new ushort[] { 0x0000 },
                    new ushort[] { 0x0000 },
                    new uint[] { 0x00000000, 0x00000000, 0x00000000, 0x00000000 },
                    new uint[] { },
                    new uint[] { 0x00000000 },
                    new int[] { 0 },
                    1,
                    0));
            _codeGeneratorMock = new Mock<ICodeGenerator>();
            _codeGeneratorMock.Setup(x => x.GenerateFromSlab(It.IsAny<InstList>(), It.IsAny<StringPool>(), It.IsAny<CodeGenOptions>()))
                .Returns(new byte[] { 1, 2, 3 });
            _capabilityProviderMock = new Mock<ICapabilityProvider>();
            _capabilityValidatorMock = new Mock<ICapabilityValidatorService>();
            var backendProfile = new CapabilityProfile { BaseLevel = CapabilityLevel.L3 };
            _capabilityProviderMock.Setup(p => p.GetCapabilityProfile()).Returns(backendProfile);
            _capabilityProviderMock.Setup(p => p.GetSupportedExtensions()).Returns(new List<string>());
            _useCase = new CompileUseCase(
                _frontendMock.Object,
                _midLevelOptimizerMock.Object,
                _lowLevelOptimizerMock.Object,
                _mlirToLlirMock.Object,
                _codeGeneratorMock.Object,
                _capabilityProviderMock.Object,
                _capabilityValidatorMock.Object);
        }

        [Test]
        public void Execute_WhenProfileIsL1_AndBackendViolation_ReturnsFailure()
        {
            var options = new CompilationOptions
            {
                Target = Architecture.Genesis,
                DispatchStrategy = DispatchStrategy.DirectThreadedCode,
                GenerateDebugInfo = false,
                Optimize = true,
                Profile = CapabilityLevel.L4,
                Enforcement = EnforcementLevel.Strict
            };

            var result = _useCase.Execute("program Test; begin end.", ".pas", options);

            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("capability profile"));
        }

        [Test]
        public void Execute_WithValidProfile_ReturnsSuccess()
        {
            var options = new CompilationOptions
            {
                Target = Architecture.Genesis,
                DispatchStrategy = DispatchStrategy.DirectThreadedCode,
                GenerateDebugInfo = false,
                Optimize = true,
                Profile = CapabilityLevel.L2,
                Enforcement = EnforcementLevel.Strict
            };

            var result = _useCase.Execute("program Test; begin end.", ".pas", options);

            Assert.That(result.Success, Is.True);
        }
    }
}
