using GameVM.Compiler.Core.CodeGen;
using GameVM.Compiler.Core.IR.Buffers;
using GameVM.Compiler.Core.IR.Interfaces;

namespace GameVM.Compiler.Core.Tests.CodeGen
{
    [TestFixture]
    public class DefaultCodeGeneratorTests
    {
        private readonly DefaultCodeGenerator _generator = new DefaultCodeGenerator();

        #region GenerateFromSlab Method Tests

        [Test]
        public void GenerateFromSlab_WithValidLlirSlab_ReturnsByteArray()
        {
            // Arrange
            var llirSlab = new uint[] { 0x47564D56, 3, 1, 10, 0, 0, 0, 0 }; // Valid LLIR slab header
            var options = new CodeGenOptions();

            // Act
             var result = _generator.GenerateFromSlab(llirSlab, new StringPool(), options);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Length, Is.EqualTo(llirSlab.Length * 4)); // 4 bytes per uint
        }

        [Test]
        public void GenerateFromSlab_WithNullLlirSlab_ReturnsEmptyArray()
        {
            // Arrange
            var options = new CodeGenOptions();

            // Act
            var result = _generator.GenerateFromSlab(null!, new StringPool(), options);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void GenerateFromSlab_WithEmptyLlirSlab_ReturnsEmptyArray()
        {
            // Arrange
            var llirSlab = Array.Empty<uint>();
            var options = new CodeGenOptions();

            // Act
            var result = _generator.GenerateFromSlab(llirSlab, new StringPool(), options);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void GenerateFromSlab_WithNullOptions_DoesNotThrow()
        {
            // Arrange
            var llirSlab = new uint[] { 0x47564D56, 3, 1, 10, 0, 0, 0, 0 };

            // Act & Assert - Should not throw (options parameter not used in current implementation)
            Assert.DoesNotThrow(() => _generator.GenerateFromSlab(llirSlab, new StringPool(), null!));
        }

        #endregion

        #region Edge Case Tests

        [Test]
        public void GenerateFromSlab_CalledMultipleTimes_ReturnsConsistentResults()
        {
            // Arrange
            var llirSlab = new uint[] { 0x47564D56, 3, 1, 10, 0, 0, 0, 0 };
            var options = new CodeGenOptions();

            // Act
            var result1 = _generator.GenerateFromSlab(llirSlab, new StringPool(), options);
            var result2 = _generator.GenerateFromSlab(llirSlab, new StringPool(), options);

            // Assert
            Assert.That(result1, Is.EqualTo(result2));
        }

        [Test]
        public void GenerateFromSlab_WithDifferentSlabSizes_ReturnsCorrectByteArray()
        {
            // Arrange
            var llirSlabSmall = new uint[] { 0x47564D56, 3, 1, 5, 0, 0, 0, 0 };
            var llirSlabLarge = new uint[20];
            llirSlabLarge[0] = 0x47564D56;
            llirSlabLarge[1] = 3;
            llirSlabLarge[2] = 1;
            llirSlabLarge[3] = 15;
            var options = new CodeGenOptions();

            // Act
            var resultSmall = _generator.GenerateFromSlab(llirSlabSmall, new StringPool(), options);
            var resultLarge = _generator.GenerateFromSlab(llirSlabLarge, new StringPool(), options);

            // Assert
            Assert.That(resultSmall.Length, Is.EqualTo(llirSlabSmall.Length * 4));
            Assert.That(resultLarge.Length, Is.EqualTo(llirSlabLarge.Length * 4));
        }

        #endregion
    }
}
