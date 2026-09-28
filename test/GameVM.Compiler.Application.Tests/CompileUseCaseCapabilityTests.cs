using GameVM.Compiler.Application;
using GameVM.Compiler.Application.Services;
using GameVM.Compiler.Core.Interfaces;
using GameVM.Compiler.Core.Enums;
using Moq;
using NUnit.Framework;
using GameVM.Compiler.Core.IR.Interfaces;
using System.Collections.Generic;
using GameVM.Compiler.Core.IR.Hlir;
using GameVM.Compiler.Core.IR.Buffers;
using GameVM.Compiler.Core.IR.Soa;

namespace GameVM.Compiler.Application.Tests
{
    [TestFixture]
    public class CompileUseCaseCapabilityTests
    {
        private Mock<ILanguageFrontend> _frontendMock = null!;
        private Mock<IMidLevelOptimizer> _midOptimizerMock = null!;
        private Mock<ILowLevelOptimizer> _lowOptimizerMock = null!;
        private Mock<IIRSlabTransformer> _transformerMock = null!;
        private Mock<ICodeGenerator> _codeGenMock = null!;
        private Mock<ICapabilityProvider> _capProviderMock = null!;
        private Mock<ICapabilityValidatorService> _capValidatorMock = null!;
        private CompileUseCase _useCase = null!;

        [SetUp]
        public void Setup()
        {
            _frontendMock = new Mock<ILanguageFrontend>();
            _midOptimizerMock = new Mock<IMidLevelOptimizer>();
            _lowOptimizerMock = new Mock<ILowLevelOptimizer>();
            _transformerMock = new Mock<IIRSlabTransformer>();
            _codeGenMock = new Mock<ICodeGenerator>();
            _capProviderMock = new Mock<ICapabilityProvider>();
            _capValidatorMock = new Mock<ICapabilityValidatorService>();

            var hlirBuilder = new HlirBuilder();
            hlirBuilder.Add((byte)HlirNodeKind.Nop);
            _frontendMock.Setup(x => x.ParseToHlir(It.IsAny<string>()))
                .Returns(new ParseResult(hlirBuilder.Build(), default));
            _frontendMock.Setup(x => x.StringPool).Returns(new StringPool());

            _midOptimizerMock.Setup(x => x.OptimizeSlab(It.IsAny<InstList>(), It.IsAny<StringPool>(), It.IsAny<OptimizationLevel>()))
                .Returns(new InstList(new byte[] { 0x80 }, new ushort[] { 0 }, new ushort[] { 0 },
                    new uint[] { 0 }, new uint[] { }, new uint[] { 0 }, new int[] { 0 }, 1, 0));
            _transformerMock.Setup(x => x.TransformSlab(It.IsAny<InstList>(), It.IsAny<StringPool>()))
                .Returns(new InstList(new byte[] { 0x47, 0x49, 0x4D, 0x4C, 1, 3, 0, 0 },
                    new ushort[] { 0 }, new ushort[] { 0 },
                    new uint[] { 0, 0, 0, 0 }, new uint[] { }, new uint[] { 0 }, new int[] { 0 }, 1, 0));
            _lowOptimizerMock.Setup(x => x.OptimizeSlab(It.IsAny<InstList>(), It.IsAny<StringPool>(), It.IsAny<OptimizationLevel>()))
                .Returns(new InstList(new byte[] { 0x47, 0x49, 0x4D, 0x4C, 1, 3, 0, 0 },
                    new ushort[] { 0 }, new ushort[] { 0 },
                    new uint[] { 0, 0, 0, 0 }, new uint[] { }, new uint[] { 0 }, new int[] { 0 }, 1, 0));
            _codeGenMock.Setup(x => x.GenerateFromSlab(It.IsAny<InstList>(), It.IsAny<StringPool>(), It.IsAny<CodeGenOptions>()))
                .Returns(new byte[] { 1, 2, 3 });

            _capProviderMock.Setup(p => p.GetCapabilityProfile()).Returns(new CapabilityProfile { BaseLevel = CapabilityLevel.L3 });
            _capProviderMock.Setup(p => p.GetSupportedExtensions()).Returns(new List<string>());

            _useCase = new CompileUseCase(
                _frontendMock.Object,
                _midOptimizerMock.Object,
                _lowOptimizerMock.Object,
                _transformerMock.Object,
                _codeGenMock.Object,
                _capProviderMock.Object,
                _capValidatorMock.Object);
        }

        [Test]
        public void Compile_ValidProgram_CompilesSuccessfully()
        {
            var result = _useCase.Execute("program Test; begin end.", ".pas", new CompilationOptions { Target = Architecture.Genesis, Profile = CapabilityLevel.L2, Enforcement = EnforcementLevel.Strict });

            Assert.That(result.Success, Is.True);
        }

        [Test]
        public void Compile_InvalidProgram_ReturnsCompilationResultWithErrors()
        {
            _frontendMock.Setup(x => x.ParseToHlir(It.IsAny<string>()))
                .Returns(new ParseResult(HlirTree.Empty, default));
            _frontendMock.Setup(x => x.LastParseErrors).Returns(new List<string> { "Syntax error" });

            var result = _useCase.Execute("program invalid;\nbegin", ".pas", new CompilationOptions { Target = Architecture.Atari2600 });

            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Is.Not.Empty);
        }
    }
}