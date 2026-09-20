using GameVM.Compiler.Core.Interfaces;
using NUnit.Framework;

namespace GameVM.Compiler.Core.Tests
{
    [TestFixture]
    public class SemanticAnalysisResultTests
    {
        [Test]
        public void EmptyErrors_IsEmpty_ReturnsTrue()
        {
            // Arrange & Act
            var diagnostics = new SemanticDiagnostics(Array.Empty<SemanticError>());

            // Assert
            Assert.That(diagnostics.IsEmpty);
        }

        [Test]
        public void ErrorsPresent_IsEmpty_ReturnsFalse()
        {
            // Arrange & Act
            var error = new SemanticError(1, 2, "Test error");
            var diagnostics = new SemanticDiagnostics(new List<SemanticError> { error });

            // Assert
            Assert.That(!diagnostics.IsEmpty);
        }

        [Test]
        public void Summary_FormatsCorrectly()
        {
            // Arrange
            var error1 = new SemanticError(1, 2, "Error at line 1");
            var error2 = new SemanticError(10, 20, "Error at line 10");

            // Act
            var diagnostics = new SemanticDiagnostics(new List<SemanticError> { error1, error2 });

            // Assert
            Assert.That(diagnostics.Summary, Is.EqualTo("1:2: Error at line 1\n10:20: Error at line 10"));
        }

        [Test]
        public void ErrorsList_ContainsErrors()
        {
            // Arrange
            var error = new SemanticError(1, 2, "Test error");

            // Act
            var diagnostics = new SemanticDiagnostics(new List<SemanticError> { error });

            // Assert
            Assert.That(diagnostics.Errors.Count, Is.EqualTo(1));
            Assert.That(diagnostics.Errors[0].Line, Is.EqualTo(1));
            Assert.That(diagnostics.Errors[0].Column, Is.EqualTo(2));
            Assert.That(diagnostics.Errors[0].Message, Is.EqualTo("Test error"));
        }
    }
}
