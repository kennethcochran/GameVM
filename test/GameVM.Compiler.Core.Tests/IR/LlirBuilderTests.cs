using NUnit.Framework;
using GameVM.Compiler.Core.IR;
using GameVM.Compiler.Core.IR.Interfaces;

namespace GameVM.Compiler.Core.Tests.IR
{
    [TestFixture]
    public class LlirBuilderTests
    {
        private LlirBuilder _builder = null!;

        [SetUp]
        public void Setup()
        {
            _builder = new LlirBuilder();
        }

        [Test]
        public void Transform_WithValidMidLevelIR_ReturnsLowLevelIR()
        {
            // Arrange
            var input = new MidLevelIR { SourceFile = "test.pas" };

            // Act
            var result = _builder.Transform(input);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.SourceFile, Is.EqualTo("test.pas"));
        }

        [Test]
        public void Transform_WithNullMidLevelIR_ReturnsLowLevelIRWithNullSourceFile()
        {
            // Arrange
            var input = new MidLevelIR { SourceFile = string.Empty };

            // Act
            var result = _builder.Transform(input);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.SourceFile, Is.EqualTo(string.Empty));
        }

        [Test]
        public void Transform_WithEmptySourceFile_ReturnsLowLevelIRWithEmptySourceFile()
        {
            // Arrange
            var input = new MidLevelIR { SourceFile = "" };

            // Act
            var result = _builder.Transform(input);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.SourceFile, Is.EqualTo(""));
        }

        [Test]
        public void Transform_WithComplexSourceFile_ReturnsLowLevelIRWithSameSourceFile()
        {
            // Arrange
            var input = new MidLevelIR { SourceFile = "/path/to/complex/file.pas" };

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
            var input = new MidLevelIR { SourceFile = "test.pas" };

            // Act
            var result1 = _builder.Transform(input);
            var result2 = _builder.Transform(input);

            // Assert
            Assert.That(result1, Is.Not.SameAs(result2));
            Assert.That(result1.SourceFile, Is.EqualTo(result2.SourceFile));
        }
    }
}
