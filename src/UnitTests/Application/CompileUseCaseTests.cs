/******************************************************************************
* This file contains tests for the CompileUseCase class.
******************************************************************************/
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
        private AutoMocker _mocker;
        private CompileUseCase _compileUseCase;
        private string _tempFilePath;

        [SetUp]
        public void Setup()
        {
            _mocker = new AutoMocker();
            _compileUseCase = _mocker.CreateInstance<CompileUseCase>();
            
            // Create a temporary file for testing
            _tempFilePath = System.IO.Path.GetTempFileName();
            System.IO.File.WriteAllText(_tempFilePath, "test content");
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
            var finalIr = new Mock<FinalIR>().Object;
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
            _mocker.GetMock<IIRTransformer<LowLevelIR, FinalIR>>()
                .Setup(x => x.Transform(llir))
                .Returns(finalIr);
            _mocker.GetMock<ICodeGenerator>()
                .Setup(x => x.Generate(finalIr, It.IsAny<CodeGenOptions>()))
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
            var hlir = new HighLevelIR();
            var mlir = new MidLevelIR();
            var llir = new LowLevelIR();
            var finalIr = new FinalIR();
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
            _mocker.GetMock<IIRTransformer<LowLevelIR, FinalIR>>()
                .Setup(b => b.Transform(llir))
                .Returns(finalIr);
            _mocker.GetMock<ICodeGenerator>()
                .Setup(g => g.Generate(finalIr, It.IsAny<CodeGenOptions>()))
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
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Is.Not.Empty);
        }
    }
}
