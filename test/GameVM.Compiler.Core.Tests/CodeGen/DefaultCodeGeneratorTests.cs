using GameVM.Compiler.Core.CodeGen;
using GameVM.Compiler.Core.IR.Buffers;
using GameVM.Compiler.Core.IR.Interfaces;
using GameVM.Compiler.Core.IR.Soa;

namespace GameVM.Compiler.Core.Tests.CodeGen
{
    [TestFixture]
    public class DefaultCodeGeneratorTests
    {
        private readonly DefaultCodeGenerator _generator = new DefaultCodeGenerator();

        /// <summary>Builds an InstList containing <paramref name="instructionCount"/> Nop instructions.</summary>
        private static InstList BuildSlab(int instructionCount)
        {
            var builder = new InstListBuilder();
            for (int i = 0; i < instructionCount; i++)
            {
                builder.Add((byte)0x00, InstructionFlag.None, 0);
            }
            return builder.Build();
        }

        #region GenerateFromSlab Method Tests

        [Test]
        public void GenerateFromSlab_WithValidLlirSlab_ReturnsByteArray()
        {
            // Arrange
            var llirSlab = BuildSlab(3);
            var options = new CodeGenOptions();

            // Act
            var result = _generator.GenerateFromSlab(llirSlab, new StringPool(), options);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Length, Is.EqualTo(llirSlab.Count * 4)); // 4 bytes per instruction
        }

        [Test]
        public void GenerateFromSlab_WithDefaultLlirSlab_ReturnsEmptyArray()
        {
            // Arrange
            var llirSlab = default(InstList);
            var options = new CodeGenOptions();

            // Act
            var result = _generator.GenerateFromSlab(llirSlab, new StringPool(), options);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void GenerateFromSlab_WithEmptyLlirSlab_ReturnsEmptyArray()
        {
            // Arrange
            var llirSlab = BuildSlab(0);
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
            var llirSlab = BuildSlab(2);

            // Act & Assert - Should not throw (options parameter not used in current implementation)
            Assert.DoesNotThrow(() => _generator.GenerateFromSlab(llirSlab, new StringPool(), null!));
        }

        #endregion

        #region Edge Case Tests

        [Test]
        public void GenerateFromSlab_CalledMultipleTimes_ReturnsConsistentResults()
        {
            // Arrange
            var llirSlab = BuildSlab(3);
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
            var llirSlabSmall = BuildSlab(5);
            var llirSlabLarge = BuildSlab(15);
            var options = new CodeGenOptions();

            // Act
            var resultSmall = _generator.GenerateFromSlab(llirSlabSmall, new StringPool(), options);
            var resultLarge = _generator.GenerateFromSlab(llirSlabLarge, new StringPool(), options);

            // Assert
            Assert.That(resultSmall.Length, Is.EqualTo(llirSlabSmall.Count * 4));
            Assert.That(resultLarge.Length, Is.EqualTo(llirSlabLarge.Count * 4));
        }

        #endregion
    }
}