using NUnit.Framework;
using GameVM.Compiler.Core.SemanticAnalysis;
using GameVM.Compiler.Pascal;

namespace GameVM.Compiler.Core.Tests
{
    [TestFixture]
    public class BasicSemanticAnalyzerTests
    {
        private PascalFrontend _frontend = null!;
        private BasicSemanticAnalyzer _analyzer = null!;

        [SetUp]
        public void Setup()
        {
            _frontend = new PascalFrontend();
            _analyzer = new BasicSemanticAnalyzer();
        }

        [Test]
        public void AnalyzeSlab_ReturnsSuccess_ForValidSimpleProgram()
        {
            // Arrange
            var sourceCode = "program Test;\nvar x: Integer;\nbegin\n  x := 42;\nend.";
            var astSlab = _frontend.ParseToSlab(sourceCode);
            var hlirSlab = _frontend.ConvertToHlirSlab(astSlab);
            Assert.That(hlirSlab, Is.Not.Null.And.Not.Empty);

            // Act
            var result = _analyzer.AnalyzeSlab(hlirSlab);

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.Errors, Is.Empty);
        }

        [Test]
        public void AnalyzeSlab_ReturnsSuccess_ForCompatibleTypes()
        {
            // Arrange - Integer to Real assignment is compatible
            var sourceCode = "program Test;\nvar x: Real;\nbegin\n  x := 42;\nend.";
            var astSlab = _frontend.ParseToSlab(sourceCode);
            var hlirSlab = _frontend.ConvertToHlirSlab(astSlab);
            Assert.That(hlirSlab, Is.Not.Null.And.Not.Empty);

            // Act
            var result = _analyzer.AnalyzeSlab(hlirSlab);

            // Assert
            Assert.That(result.Success, Is.True);
        }

        [Test]
        public void AnalyzeSlab_ReturnsSuccess_ForFunctionWithReturn()
        {
            // Arrange
            var sourceCode = @"
                program Test;
                function Add(a, b: Integer): Integer;
                begin
                  Add := a + b;
                end;
                begin
                end.";
            var astSlab = _frontend.ParseToSlab(sourceCode);
            var hlirSlab = _frontend.ConvertToHlirSlab(astSlab);
            Assert.That(hlirSlab, Is.Not.Null.And.Not.Empty);

            // Act
            var result = _analyzer.AnalyzeSlab(hlirSlab);

            // Assert
            Assert.That(result.Success, Is.True);
        }

        [Test]
        public void AnalyzeSlab_ReturnsError_ForEmptySlab()
        {
            // Arrange
            var emptySlab = new uint[0];

            // Act
            var result = _analyzer.AnalyzeSlab(emptySlab);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.Errors, Is.Not.Empty);
        }

        [Test]
        public void AnalyzeSlab_ReturnsError_ForInvalidMagicNumber()
        {
            // Arrange - invalid magic number
            var invalidSlab = new uint[] { 0x12345678, 1, 0, 0, 0, 0 };

            // Act
            var result = _analyzer.AnalyzeSlab(invalidSlab);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.Errors, Is.Not.Empty);
        }

        [Test]
        public void AnalyzeSlab_ReturnsError_ForWrongStage()
        {
            // Arrange - stage 0 (AST) instead of stage 1 (HLIR)
            var wrongStageSlab = new uint[] { 0x47494D00, 0, 1, 0, 0, 0 }; // Magic="GIM", stage=0

            // Act
            var result = _analyzer.AnalyzeSlab(wrongStageSlab);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.Errors, Is.Not.Empty);
        }
    }
}