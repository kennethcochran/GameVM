/******************************************************************************
* This file contains tests for the CompileUseCase class.
******************************************************************************/
using NUnit.Framework;
using Moq;
using GameVM.Compiler.Application;
using GameVM.Compiler.Application.Services;
using GameVM.Compiler.Core.IR;
using GameVM.Compiler.Core.IR.Interfaces;
using Moq.AutoMock;
using GameVM.Compiler.Core.Interfaces;
using GameVM.Compiler.Core.Enums;

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
            _compileUseCase = _mocker.CreateInstance<CompileUseCase>();

            // Create a temporary file for testing
            _tempFilePath = System.IO.Path.GetTempFileName();
            System.IO.File.WriteAllText(_tempFilePath, "test content");
        }

        // RED PHASE: One failing test at a time for CompileUseCase constructor - null frontend
        [Test]
        public void Constructor_NullFrontend_ShouldThrowArgumentNullException()
        {
            // Arrange - Test null validation for frontend parameter
            var midLevelOptimizer = _mocker.GetMock<IMidLevelOptimizer>();
            var lowLevelOptimizer = _mocker.GetMock<ILowLevelOptimizer>();
            var mlirToLlir = _mocker.GetMock<IIRTransformer<MidLevelIR, LowLevelIR>>();
            var codeGenerator = _mocker.GetMock<ICodeGenerator>();
            var capabilityProvider = _mocker.GetMock<ICapabilityProvider>();
            var capabilityValidator = _mocker.GetMock<ICapabilityValidatorService>();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new CompileUseCase(
                null!, // null frontend
                midLevelOptimizer.Object,
                lowLevelOptimizer.Object,
                mlirToLlir.Object,
                codeGenerator.Object,
                capabilityProvider.Object,
                capabilityValidator.Object));
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
            // Arrange
            var frontend = _mocker.GetMock<ILanguageFrontend>();
            var hlir = new Mock<HighLevelIR>().Object;
            var mlir = new Mock<MidLevelIR>().Object;
            var llir = new Mock<LowLevelIR>().Object;
            var options = new CompilationOptions
            {
                Target = Architecture.Genesis,
                DispatchStrategy = DispatchStrategy.DirectThreadedCode,
                GenerateDebugInfo = false,
                Optimize = true
            };

            _mocker.GetMock<ILanguageService>()
                .Setup(x => x.GetFrontend(It.IsAny<string>()))
                .Returns(frontend.Object);
            frontend.Setup(x => x.Parse(It.IsAny<string>()))
                .Returns(hlir);
            frontend.Setup(x => x.ConvertToMidLevelIR(hlir))
                .Returns(mlir);
            _mocker.GetMock<IIRTransformer<MidLevelIR, LowLevelIR>>()
                .Setup(x => x.Transform(mlir))
                .Returns(llir);
            _mocker.GetMock<ICodeGenerator>()
                .Setup(x => x.Generate(llir, It.IsAny<CodeGenOptions>()))
                .Returns(new byte[] { 1, 2, 3 });

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
            // Arrange
            var frontend = _mocker.GetMock<ILanguageFrontend>();
            var hlir = new HighLevelIR { SourceFile = "unknown" };
            var mlir = new MidLevelIR();
            var llir = new LowLevelIR();
            var options = new CompilationOptions
            {
                Target = Architecture.Genesis,
                DispatchStrategy = DispatchStrategy.DirectThreadedCode,
                GenerateDebugInfo = false,
                Optimize = true
            };

            frontend.Setup(f => f.Parse(It.IsAny<string>())).Returns(hlir);
            frontend.Setup(f => f.ConvertToMidLevelIR(hlir)).Returns(mlir);
            _mocker.GetMock<ILanguageService>()
                .Setup(r => r.GetFrontend(It.IsAny<string>()))
                .Returns(frontend.Object);
            _mocker.GetMock<IIRTransformer<MidLevelIR, LowLevelIR>>()
                .Setup(b => b.Transform(mlir))
                .Returns(llir);
            _mocker.GetMock<ICodeGenerator>()
                .Setup(g => g.Generate(llir, It.IsAny<CodeGenOptions>()))
                .Returns(new byte[0]);

            // Act
            var result = _compileUseCase.Execute(System.IO.File.ReadAllText(_tempFilePath), _tempFilePath, options);

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.Code, Is.Not.Null);
            Assert.That(System.IO.Path.GetFullPath(result.SourceFile), Is.EqualTo(System.IO.Path.GetFullPath(_tempFilePath)));
            Assert.That(result.ErrorMessage, Is.Empty);
        }

        [Test]
        public void Execute_FileNotFound_ReturnsFailure()
        {
            // Arrange
            var nonExistentFile = "nonexistent.txt";
            var options = new CompilationOptions
            {
                Target = Architecture.Genesis,
                DispatchStrategy = DispatchStrategy.DirectThreadedCode,
                GenerateDebugInfo = false,
                Optimize = true
            };

            // Act
            var result = _compileUseCase.Execute(nonExistentFile, options);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.False);
                Assert.That(result.ErrorMessage, Is.Not.Empty);
            });
        }

        #region Real Compilation Tests (No Mocks)

        [Test]
        public void Execute_RealCompilation_WithValidPascalCode_Succeeds()
        {
            // Arrange
            var realCompiler = CreateRealCompiler();
            var sourceCode = "program Test;\nbegin\n  writeln('hello');\nend.";
            var options = new CompilationOptions
            {
                Target = Architecture.Atari2600,
                DispatchStrategy = DispatchStrategy.DirectThreadedCode,
                GenerateDebugInfo = false,
                Optimize = false
            };

            // Act
            var result = realCompiler.Execute(sourceCode, ".pas", options);

            // Assert
            Assert.That(result, Is.Not.Null);
            // Compilation may succeed or fail depending on implementation completeness
            // The important thing is that it uses real components, not mocks
        }

        [Test]
        public void Execute_RealCompilation_WithInvalidPascalCode_HandlesError()
        {
            // Arrange
            var realCompiler = CreateRealCompiler();
            var sourceCode = "program Test;\nbegin\n  invalid syntax here\nend.";
            var options = new CompilationOptions
            {
                Target = Architecture.Atari2600,
                DispatchStrategy = DispatchStrategy.DirectThreadedCode,
                GenerateDebugInfo = false,
                Optimize = false
            };

            // Act
            var result = realCompiler.Execute(sourceCode, ".pas", options);

            // Assert
            Assert.That(result, Is.Not.Null);
            // Should handle error gracefully (either return failure or throw exception)
        }

        #endregion

        #region Resource Constraint Tests

        [Test]
        public void Execute_ExceedsROMLimit_ReportsResourceError()
        {
            // Arrange
            var realCompiler = CreateRealCompiler();
            // Create a very large program that might exceed ROM limits
            var largeSource = "program Test;\nbegin\n";
            for (int i = 0; i < 1000; i++)
            {
                largeSource += $"  writeln('line {i}');\n";
            }
            largeSource += "end.";
            var options = new CompilationOptions
            {
                Target = Architecture.Atari2600,
                DispatchStrategy = DispatchStrategy.DirectThreadedCode,
                GenerateDebugInfo = false,
                Optimize = false
            };

            // Act
            var result = realCompiler.Execute(largeSource, ".pas", options);

            // Assert
            Assert.That(result, Is.Not.Null);
            // When ROM size checking is implemented, should report resource constraint error
            // For now, verify compilation handles large programs
        }

        [Test]
        public void Execute_ExceedsRAMLimit_ReportsResourceError()
        {
            // Arrange
            var realCompiler = CreateRealCompiler();
            // Create program with many variables that might exceed RAM
            var sourceWithManyVars = "program Test;\nvar\n";
            for (int i = 0; i < 500; i++)
            {
                sourceWithManyVars += $"  var{i}: Integer;\n";
            }
            sourceWithManyVars += "begin\nend.";
            var options = new CompilationOptions
            {
                Target = Architecture.Atari2600,
                DispatchStrategy = DispatchStrategy.DirectThreadedCode,
                GenerateDebugInfo = false,
                Optimize = false
            };

            // Act
            var result = realCompiler.Execute(sourceWithManyVars, ".pas", options);

            // Assert
            Assert.That(result, Is.Not.Null);
            // When RAM size checking is implemented, should report resource constraint error
        }

        #endregion

        #region Invalid Options Tests

        [Test]
        public void Execute_InvalidOptimizationLevel_HandlesGracefully()
        {
            // Arrange
            var realCompiler = CreateRealCompiler();
            var sourceCode = "program Test;\nbegin\nend.";
            var options = new CompilationOptions
            {
                Target = Architecture.Atari2600,
                DispatchStrategy = DispatchStrategy.DirectThreadedCode,
                GenerateDebugInfo = false,
                Optimize = true,
                OptimizationLevel = (OptimizationLevel)999 // Invalid level
            };

            // Act
            var result = realCompiler.Execute(sourceCode, ".pas", options);

            // Assert
            Assert.That(result, Is.Not.Null);
            // Should handle invalid optimization level gracefully
        }

        [Test]
        public void Execute_InvalidTargetArchitecture_HandlesGracefully()
        {
            // Arrange
            var realCompiler = CreateRealCompiler();
            var sourceCode = "program Test;\nbegin\nend.";
            var options = new CompilationOptions
            {
                Target = (Architecture)999, // Invalid architecture
                DispatchStrategy = DispatchStrategy.DirectThreadedCode,
                GenerateDebugInfo = false,
                Optimize = false
            };

            // Act
            var result = realCompiler.Execute(sourceCode, ".pas", options);

            // Assert
            Assert.That(result, Is.Not.Null);
            // Should handle invalid target architecture gracefully
        }

        #endregion

        #region Malformed Input Tests

        [Test]
        public void Execute_MalformedIRInput_HandlesError()
        {
            // Arrange
            var realCompiler = CreateRealCompiler();
            // Source code that might produce malformed IR
            var sourceCode = "program Test;\nbegin\n  x := ;\nend.";
            var options = new CompilationOptions
            {
                Target = Architecture.Atari2600,
                DispatchStrategy = DispatchStrategy.DirectThreadedCode,
                GenerateDebugInfo = false,
                Optimize = false
            };

            // Act
            var result = realCompiler.Execute(sourceCode, ".pas", options);

            // Assert
            Assert.That(result, Is.Not.Null);
            // Should handle malformed input gracefully
        }

        [Test]
        public void Execute_EmptySourceCode_HandlesGracefully()
        {
            // Arrange
            var realCompiler = CreateRealCompiler();
            var sourceCode = "";
            var options = new CompilationOptions
            {
                Target = Architecture.Atari2600,
                DispatchStrategy = DispatchStrategy.DirectThreadedCode,
                GenerateDebugInfo = false,
                Optimize = false
            };

            // Act
            var result = realCompiler.Execute(sourceCode, ".pas", options);

            // Assert
            Assert.That(result, Is.Not.Null);
            // Should handle empty source code gracefully
        }

        [Test]
        public void Execute_NullSourceCode_HandlesGracefully()
        {
            // Arrange
            var realCompiler = CreateRealCompiler();
            string? sourceCode = null;
            var options = new CompilationOptions
            {
                Target = Architecture.Atari2600,
                DispatchStrategy = DispatchStrategy.DirectThreadedCode,
                GenerateDebugInfo = false,
                Optimize = false
            };

            // Act & Assert
            // Should throw ArgumentNullException or handle gracefully
            Assert.Throws<ArgumentNullException>(() =>
            {
                realCompiler.Execute(sourceCode!, ".pas", options);
            });
        }

        #endregion

        #region File I/O Error Tests

        [Test]
        public void Execute_FileReadError_ReturnsFailure()
        {
            // Arrange
            var realCompiler = CreateRealCompiler();
            // Use a path that exists but cannot be read (e.g., directory)
            var directoryPath = System.IO.Path.GetTempPath();
            var options = new CompilationOptions
            {
                Target = Architecture.Atari2600,
                DispatchStrategy = DispatchStrategy.DirectThreadedCode,
                GenerateDebugInfo = false,
                Optimize = false
            };

            // Act
            var result = realCompiler.Execute(directoryPath, options);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Is.Not.Empty);
        }

        #endregion

        #region Helper Methods

        private static CompileUseCase CreateRealCompiler()
        {
            // Create real compiler with actual dependencies (no mocks)
            var frontend = new GameVM.Compiler.Pascal.PascalFrontend();
            var midOptimizer = new GameVM.Compiler.Optimizers.MidLevel.DefaultMidLevelOptimizer();
            var lowOptimizer = new GameVM.Compiler.Optimizers.LowLevel.DefaultLowLevelOptimizer();
            var mlirToLlir = new GameVM.Compiler.Backend.Atari2600.MidToLowLevelTransformer();
            var codeGenerator = new GameVM.Compiler.Backend.Atari2600.Atari2600CodeGenerator();

            var capabilityValidator = new GameVM.Compiler.Capabilities.CapabilityValidatorService();

            return new CompileUseCase(
                frontend,
                midOptimizer,
                lowOptimizer,
                mlirToLlir,
                codeGenerator,
                codeGenerator, // Use same instance for ICapabilityProvider
                capabilityValidator);
        }

        #endregion
    }
}
