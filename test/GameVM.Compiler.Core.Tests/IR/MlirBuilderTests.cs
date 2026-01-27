using NUnit.Framework;
using GameVM.Compiler.Core.IR;
using GameVM.Compiler.Core.IR.Interfaces;

namespace GameVM.Compiler.Core.Tests.IR
{
    [TestFixture]
    public class MlirBuilderTests
    {
        private MlirBuilder _builder = null!;

        [SetUp]
        public void Setup()
        {
            _builder = new MlirBuilder();
        }

        [Test]
        public void Transform_WithValidHighLevelIR_ReturnsMidLevelIR()
        {
            // Arrange
            var input = new HighLevelIR { SourceFile = "test.pas" };

            // Act
            var result = _builder.Transform(input);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.SourceFile, Is.EqualTo("test.pas"));
        }

        [Test]
        public void Transform_WithEmptySourceFile_ReturnsMidLevelIRWithEmptySourceFile()
        {
            // Arrange
            var input = new HighLevelIR { SourceFile = "" };

            // Act
            var result = _builder.Transform(input);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.SourceFile, Is.EqualTo(""));
        }

        [Test]
        public void Transform_WithComplexSourceFile_ReturnsMidLevelIRWithSameSourceFile()
        {
            // Arrange
            var input = new HighLevelIR { SourceFile = "/path/to/complex/file.pas" };

            // Act
            var result = _builder.Transform(input);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.SourceFile, Is.EqualTo("/path/to/complex/file.pas"));
        }

        [Test]
        public void Transform_MultipleCalls_ReturnsDistinctInstances()
        {
            // Arrange
            var input = new HighLevelIR { SourceFile = "test.pas" };

            // Act
            var result1 = _builder.Transform(input);
            var result2 = _builder.Transform(input);

            // Assert
            Assert.That(result1, Is.Not.SameAs(result2));
            Assert.That(result1.SourceFile, Is.EqualTo(result2.SourceFile));
        }

        [Test]
        public void Transform_WithDifferentInputs_ReturnsCorrectSourceFiles()
        {
            // Arrange
            var input1 = new HighLevelIR { SourceFile = "file1.pas" };
            var input2 = new HighLevelIR { SourceFile = "file2.pas" };

            // Act
            var result1 = _builder.Transform(input1);
            var result2 = _builder.Transform(input2);

            // Assert
            Assert.That(result1.SourceFile, Is.EqualTo("file1.pas"));
            Assert.That(result2.SourceFile, Is.EqualTo("file2.pas"));
        }
    }
}
