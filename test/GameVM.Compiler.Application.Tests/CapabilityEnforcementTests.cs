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
using GameVM.Compiler.Core.IR.Ast;
using GameVM.Compiler.Core.IR.Hlir;
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

            // Create a simple AstTree with one node (METHOD_DECLARATION)
            var testAstTree = new AstTree(new GameVM.Compiler.Core.IR.Ast.AstNode[]
            {
                new GameVM.Compiler.Core.IR.Ast.AstNode(
                    kind: 10, // MethodDeclaration
                    flags: 0,
                    payload: 0,
                    firstChild: -1,
                    childCount: 0
                )
            }, new int[] { 0 }, 1);

            _frontendMock.Setup(x => x.ParseToSlab(It.IsAny<string>())).Returns(testAstTree);

            var hlirBuilder = new HlirBuilder();
            hlirBuilder.Add((byte)HlirNodeKind.Nop);
            _frontendMock.Setup(x => x.ConvertToHlirSlab(It.IsAny<AstTree>()))
                .Returns(hlirBuilder.Build());
            _frontendMock.Setup(x => x.StringPool).Returns(new StringPool());

            _midLevelOptimizerMock.Setup(x => x.OptimizeSlab(It.IsAny<InstList>(), It.IsAny<StringPool>(), It.IsAny<OptimizationLevel>()))
                .Returns(new InstList(
                    new byte[] { 0x80 }, // tags (MLIR_LABEL)
                    new ushort[] { 0x0000 }, // flags
                    new ushort[] { 0x0000 }, // argCount=0
                    new uint[] { 0x00000000 }, // fixedOps
                    new uint[] { }, // empty extra pool
                    new uint[] { 0x00000000 }, // empty extraOffsets
                    new int[] { 0 }, // blockIds
                    1, // count
                    0  // no extra data
                ));

            _mlirToLlirMock.Setup(x => x.TransformSlab(It.IsAny<InstList>(), It.IsAny<StringPool>()))
                .Returns(new InstList(
                    new byte[] { 0x47, 0x49, 0x4D, 0x4C, 1, 3, 0, 0 }, // minimal valid MLIR slab
                    new ushort[] { 0x0000 }, // flags
                    new ushort[] { 0x0000 }, // argCount=0
                    new uint[] { 0x00000000 }, // fixedOps
                    new uint[] { }, // empty extra pool
                    new uint[] { 0x00000000 }, // empty extraOffsets
                    new int[] { 0 }, // blockIds
                    1, // count
                    0  // no extra data
                ));

            _lowLevelOptimizerMock.Setup(x => x.OptimizeSlab(It.IsAny<InstList>(), It.IsAny<StringPool>(), It.IsAny<OptimizationLevel>()))
                .Returns(new InstList(
                    new byte[] { 0x47, 0x49, 0x4D, 0x4C, 1, 3, 0, 0 }, // LLIR slab tag + metadata
                    new ushort[] { 0x0000 },
                    new ushort[] { 0x0000 },
                    new uint[] { 0x00000000, 0x00000000, 0x00000000, 0x00000000 },
                    new uint[] { },
                    new uint[] { 0x00000000 },
                    new int[] { 0 },
                    1,
                    0
                ));

            _codeGeneratorMock = new Mock<ICodeGenerator>();
            _codeGeneratorMock.Setup(x => x.GenerateFromSlab(It.IsAny<InstList>(), It.IsAny<StringPool>(), It.IsAny<CodeGenOptions>()))
                .Returns(new byte[] { 1, 2, 3 });

            _capabilityProviderMock = new Mock<ICapabilityProvider>();
            _capabilityValidatorMock = new Mock<ICapabilityValidatorService>();
            _semanticAnalyzerMock = new Mock<ISemanticAnalyzer>();
            _semanticAnalyzerMock.Setup(x => x.AnalyzeSlab(It.IsAny<InstList>(), It.IsAny<StringPool>()))
                .Returns(SemanticAnalysisResult.CreateSuccess());

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
                _capabilityValidatorMock.Object,
                _semanticAnalyzerMock.Object
            );
        }

        [Test]
        public void Execute_WhenProfileIsL1_AndBackendViolation_ReturnsFailure()
        {
            // Arrange - all dependencies are already set up in Setup method
            var options = new CompilationOptions
            {
                Target = Architecture.Genesis,
                DispatchStrategy = DispatchStrategy.DirectThreadedCode,
                GenerateDebugInfo = false,
                Optimize = true,
                Profile = CapabilityLevel.L4, // Exceeds backend L3
                Enforcement = EnforcementLevel.Strict
            };

            var result = _useCase.Execute("program Test; begin end.", ".pas", options);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("capability profile"));
        }

        [Test]
        public void Execute_WithValidProfile_ReturnsSuccess()
        {
            // Arrange - all dependencies are already set up in Setup method
            var options = new CompilationOptions
            {
                Target = Architecture.Genesis,
                DispatchStrategy = DispatchStrategy.DirectThreadedCode,
                GenerateDebugInfo = false,
                Optimize = true,
                Profile = CapabilityLevel.L2, // Within backend L3
                Enforcement = EnforcementLevel.Strict
            };

            var result = _useCase.Execute("program Test; begin end.", ".pas", options);

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.Code, Is.Not.Null);
            Assert.That(result.Code.Length, Is.GreaterThan(0));
            Assert.That(result.ErrorMessage, Is.Empty);
        }
    }
}