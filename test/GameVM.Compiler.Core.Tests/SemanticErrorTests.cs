using GameVM.Compiler.Core.Interfaces;

namespace GameVM.Compiler.Core.Tests
{
    [TestFixture]
    public class SemanticErrorTests
    {
        [Test]
        public void Constructor_WithMessageOnly_SetsPropertiesCorrectly()
        {
            // Arrange & Act
            var error = new SemanticError("Test message");

            // Assert
            Assert.That(error.Message, Is.EqualTo("Test message"));
            Assert.That(error.ErrorCode, Is.EqualTo("SEMANTIC_ERROR"));
            Assert.That(error.Line, Is.EqualTo(0));
            Assert.That(error.Column, Is.EqualTo(0));
        }

        [Test]
        public void Constructor_WithAllParameters_SetsPropertiesCorrectly()
        {
            // Arrange & Act
            var error = new SemanticError("Test message", "CUSTOM_ERROR", 42, 10);

            // Assert
            Assert.That(error.Message, Is.EqualTo("Test message"));
            Assert.That(error.ErrorCode, Is.EqualTo("CUSTOM_ERROR"));
            Assert.That(error.Line, Is.EqualTo(42));
            Assert.That(error.Column, Is.EqualTo(10));
        }
    }
}