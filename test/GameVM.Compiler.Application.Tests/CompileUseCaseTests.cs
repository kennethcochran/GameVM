/******************************************************************************
* This file contains tests for the CompileUseCase class.
******************************************************************************/
using NUnit.Framework;
using Moq;
using GameVM.Compiler.Application;
using GameVM.Compiler.Application.Services;
using GameVM.Compiler.Core.IR;
using GameVM.Compiler.Core.IR.Interfaces;
using GameVM.Compiler.Core.IR.Buffers;
using Moq.AutoMock;
using GameVM.Compiler.Core.Interfaces;
using GameVM.Compiler.Core.Enums;
using GameVM.Compiler.Core.SemanticAnalysis;
using GameVM.Compiler.Core.IR.Soa;
using GameVM.Compiler.Core.IR.Slab;
using System.Collections.Generic;

namespace UnitTests.Application
{
    public class CompileUseCaseTests
    {
        private AutoMocker _mocker = null!;
        private CompileUseCase _compileUseCase = null!;
        private string _tempFilePath = null!;

        [SetUp]
        public void Setup()
        {
            _mocker = new AutoMocker();

            // Get all mocks first to ensure we set up the same instances that will be injected
            var frontendMock = _mocker.GetMock<ILanguageFrontend>();
            var midLevelOptimizerMock = _mocker.GetMock<IMidLevelOptimizer>();
            var lowLevelOptimizerMock = _mocker.GetMock<ILowLevelOptimizer>();
            var mlirToLlirMock = _mocker.GetMock<IIRSlabTransformer>();
            var codeGeneratorMock = _mocker.GetMock<ICodeGenerator>();
            var capabilityProviderMock = _mocker.GetMock<ICapabilityProvider>();
            var capabilityValidatorMock = _mocker.GetMock<ICapabilityValidatorService>();
            var semanticAnalyzerMock = _mocker.GetMock<ISemanticAnalyzer>();

            // Set up common mocks that both tests need
            frontendMock.Setup(x => x.ParseToSlab(It.IsAny<string>()))
                .Returns(new InstList(
                    new byte[] { 0x01 }, // tags (dummy AST_ASSIGN)
                    new ushort[] { 0x0000 }, // flags
                    new ushort[] { 0x0002 }, // argCount=2
                    new uint[] { 0x00000000, 0x00000000 }, // fixedOps (2 slots)
                    new uint[] { 0x00000001, 0x00000002 }, // extra pool (2 operands)
                    new uint[] { 0x00000004 }, // extraOffsets[0] = 4 (start of operands)
                    new int[] { 0 }, // blockIds[0] = 0
                    1, // count
                    2  // extraUsed
                ));

            frontendMock.Setup(x => x.ConvertToHlirSlab(It.IsAny<InstList>()))
                .Returns(new InstList(
                    new byte[] { 0x01 }, // tags (dummy HLIR_ASSIGN)
                    new ushort[] { 0x0000 }, // flags
                    new ushort[] { 0x0002 }, // argCount=2
                    new uint[] { 0x00000000, 0x00000000 }, // fixedOps (2 slots)
                    new uint[] { 0x00000001, 0x00000002 }, // extra pool (2 operands)
                    new uint[] { 0x00000004 }, // extraOffsets[0] = 4 (start of operands)
                    new int[] { 0 }, // blockIds[0] = 0
                    1, // count
                    2  // extraUsed
                ));

            frontendMock.Setup(x => x.StringPool).Returns(new StringPool());

            midLevelOptimizerMock.Setup(x => x.OptimizeSlab(It.IsAny<InstList>(), It.IsAny<StringPool>(), It.IsAny<OptimizationLevel>()))
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

            mlirToLlirMock.Setup(x => x.TransformSlab(It.IsAny<InstList>(), It.IsAny<StringPool>()))
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

            lowLevelOptimizerMock.Setup(x => x.OptimizeSlab(It.IsAny<InstList>(), It.IsAny<StringPool>(), It.IsAny<OptimizationLevel>()))
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

            codeGeneratorMock.Setup(x => x.GenerateFromSlab(It.IsAny<InstList>(), It.IsAny<StringPool>(), It.IsAny<CodeGenOptions>()))
                .Returns(new byte[] { 1, 2, 3 });

            semanticAnalyzerMock.Setup(x => x.AnalyzeSlab(It.IsAny<InstList>(), It.IsAny<StringPool>()))
                .Returns(SemanticAnalysisResult.CreateSuccess());

            // Set up capability provider and validator to avoid backend violations
            var backendProfile = new CapabilityProfile { BaseLevel = CapabilityLevel.L3 };
            capabilityProviderMock.Setup(p => p.GetCapabilityProfile())
                .Returns(backendProfile);
            capabilityProviderMock.Setup(p => p.GetSupportedExtensions())
                .Returns(new List<string>());

            capabilityValidatorMock.Setup(v => v.Validate(It.IsAny<uint[]>(), It.IsAny<CapabilityLevel>(), It.IsAny<List<string>>()))
                .Returns(new List<string>());

            // Create a temporary file for testing
            _tempFilePath = System.IO.Path.GetTempFileName();
            System.IO.File.WriteAllText(_tempFilePath, "test content");

            // Now create the instance with the configured mocks
            _compileUseCase = _mocker.CreateInstance<CompileUseCase>();
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up temporary file
            if (System.IO.File.Exists(_tempFilePath))
            {
                System.IO.File.Delete(_tempFilePath);
            }
        }

        [Test]
        public void Execute_WhenCompilationSucceeds_ReturnsSuccessfulResult()
        {
            // Arrange - all dependencies are already set up in Setup method
            var options = new CompilationOptions
            {
                Target = Architecture.Genesis,
                DispatchStrategy = DispatchStrategy.DirectThreadedCode,
                GenerateDebugInfo = false,
                Optimize = true
            };

            // Act
            var result = _compileUseCase.Execute("print(1)", _tempFilePath, options);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.True);
                Assert.That(result.Code, Is.Not.Null);
                Assert.That(result.ErrorMessage, Is.Empty);
            });
        }

        [Test]
        public void Execute_ValidFile_ReturnsSuccess()
        {
            // Arrange - all dependencies are already set up in Setup method
            var options = new CompilationOptions
            {
                Target = Architecture.Genesis,
                DispatchStrategy = DispatchStrategy.DirectThreadedCode,
                GenerateDebugInfo = false,
                Optimize = true
            };

            // Act
            var result = _compileUseCase.Execute(_tempFilePath, options);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.True);
                Assert.That(result.Code, Is.Not.Null);
                Assert.That(result.ErrorMessage, Is.Empty);
            });
        }
    }
}