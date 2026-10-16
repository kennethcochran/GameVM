using GameVM.Compiler.Core.IR.Buffers;

namespace GameVM.Compiler.Core.Tests.IR
{
    [TestFixture]
    public class StringPoolTests
    {
        [Test]
        public void FromByteArray_RoundTrip_PreservesInternedStrings()
        {
            // Arrange
            var original = new StringPool();
            uint offsetHello = original.Intern("hello");
            uint offsetWorld = original.Intern("world");
            byte[] data = original.ToByteArray();

            // Act
            var restored = StringPool.FromByteArray(data);

            // Assert: the strings resolve correctly at their original offsets
            Assert.Multiple(() =>
            {
                Assert.That(restored.Resolve(offsetHello), Is.EqualTo("hello"));
                Assert.That(restored.Resolve(offsetWorld), Is.EqualTo("world"));
                Assert.That(restored.Resolve(0), Is.EqualTo(""));
            });
        }

        [Test]
        public void FromByteArray_EmptyArray_ReturnsPoolWithEmptyString()
        {
            // Arrange: a pool with only the empty string
            var original = new StringPool();
            byte[] data = original.ToByteArray();

            // Act
            var restored = StringPool.FromByteArray(data);

            // Assert
            Assert.That(restored.Resolve(0), Is.EqualTo(""));
        }

        [Test]
        public void FromByteArray_DuplicateStrings_Deduplicates()
        {
            // Arrange
            var original = new StringPool();
            uint offset1 = original.Intern("duplicate");
            uint offset2 = original.Intern("duplicate");
            Assert.That(offset1, Is.EqualTo(offset2), "Precondition: duplicates share offset");
            byte[] data = original.ToByteArray();

            // Act
            var restored = StringPool.FromByteArray(data);

            // Assert
            Assert.That(restored.Resolve(offset1), Is.EqualTo("duplicate"));
        }

        [Test]
        public void FromByteArray_TruncatedData_DoesNotThrow()
        {
            // Arrange: truncated buffer should not throw, just stop parsing
            byte[] truncated = new byte[] { 0, 0, 0, 0, 0, 5 };

            // Act & Assert: should not throw
            Assert.DoesNotThrow(() => StringPool.FromByteArray(truncated));
        }

        [Test]
        public void FromByteArray_UnicodeStrings_RoundTrips()
        {
            // Arrange
            var original = new StringPool();
            uint offset = original.Intern("héllo wörld");
            byte[] data = original.ToByteArray();

            // Act
            var restored = StringPool.FromByteArray(data);

            // Assert
            Assert.That(restored.Resolve(offset), Is.EqualTo("héllo wörld"));
        }
    }
}
