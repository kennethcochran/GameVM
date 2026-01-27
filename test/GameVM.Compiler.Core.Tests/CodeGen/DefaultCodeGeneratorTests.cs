using NUnit.Framework;
using GameVM.Compiler.Core.CodeGen;
using GameVM.Compiler.Core.IR;
using GameVM.Compiler.Core.IR.Interfaces;
using GameVM.Compiler.Core.Exceptions;

namespace GameVM.Compiler.Core.Tests.CodeGen
{
    [TestFixture]
    public class DefaultCodeGeneratorTests
    {
        private readonly DefaultCodeGenerator _generator = new DefaultCodeGenerator();

        #region Generate Method Tests

        [Test]
        public void Generate_WithValidLowLevelIR_ReturnsEmptyByteArray()
        {
            // Arrange
            var ir = new LowLevelIR();
            var options = new CodeGenOptions();

            // Act
            var result = _generator.Generate(ir, options);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void Generate_WithNullLowLevelIR_ThrowsCompilerException()
        {
            // Arrange
            var options = new CodeGenOptions();

            // Act & Assert
            var ex = Assert.Throws<CompilerException>(() => _generator.Generate(null!, options));
            Assert.That(ex.Message, Is.EqualTo("Cannot generate code from null IR"));
        }

        [Test]
        public void Generate_WithNullOptions_DoesNotThrow()
        {
            // Arrange
            var ir = new LowLevelIR();

            // Act & Assert - Should not throw (options parameter not used in current implementation)
            Assert.DoesNotThrow(() => _generator.Generate(ir, null!));
        }

        [Test]
        public void Generate_WithLowLevelIRContainingSourceFile_ReturnsEmptyByteArray()
        {
            // Arrange
            var ir = new LowLevelIR { SourceFile = "test.pas" };
            var options = new CodeGenOptions();

            // Act
            var result = _generator.Generate(ir, options);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
        }

        #endregion

        #region GenerateBytecode Method Tests

        [Test]
        public void GenerateBytecode_WithValidLowLevelIR_ReturnsEmptyByteArray()
        {
            // Arrange
            var ir = new LowLevelIR();
            var options = new CodeGenOptions();

            // Act
            var result = _generator.GenerateBytecode(ir, options);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void GenerateBytecode_WithNullLowLevelIR_ThrowsCompilerException()
        {
            // Arrange
            var options = new CodeGenOptions();

            // Act & Assert
            var ex = Assert.Throws<CompilerException>(() => _generator.GenerateBytecode(null!, options));
            Assert.That(ex.Message, Is.EqualTo("Cannot generate bytecode from null IR"));
        }

        [Test]
        public void GenerateBytecode_WithNullOptions_DoesNotThrow()
        {
            // Arrange
            var ir = new LowLevelIR();

            // Act & Assert - Should not throw (options parameter not used in current implementation)
            Assert.DoesNotThrow(() => _generator.GenerateBytecode(ir, null!));
        }

        [Test]
        public void GenerateBytecode_WithLowLevelIRContainingSourceFile_ReturnsEmptyByteArray()
        {
            // Arrange
            var ir = new LowLevelIR { SourceFile = "test.pas" };
            var options = new CodeGenOptions();

            // Act
            var result = _generator.GenerateBytecode(ir, options);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
        }

        #endregion

        #region Edge Case Tests

        [Test]
        public void Generate_CalledMultipleTimes_ReturnsConsistentResults()
        {
            // Arrange
            var ir = new LowLevelIR();
            var options = new CodeGenOptions();

            // Act
            var result1 = _generator.Generate(ir, options);
            var result2 = _generator.Generate(ir, options);

            // Assert
            Assert.That(result1, Is.EqualTo(result2));
            Assert.That(result1, Is.Empty);
        }

        [Test]
        public void GenerateBytecode_CalledMultipleTimes_ReturnsConsistentResults()
        {
            // Arrange
            var ir = new LowLevelIR();
            var options = new CodeGenOptions();

            // Act
            var result1 = _generator.GenerateBytecode(ir, options);
            var result2 = _generator.GenerateBytecode(ir, options);

            // Assert
            Assert.That(result1, Is.EqualTo(result2));
            Assert.That(result1, Is.Empty);
        }

        #endregion
    }
}
