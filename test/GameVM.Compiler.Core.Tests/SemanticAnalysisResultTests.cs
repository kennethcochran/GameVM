using NUnit.Framework;
using GameVM.Compiler.Core.Interfaces;
using System.Collections.Generic;

namespace GameVM.Compiler.Core.Tests
{
    [TestFixture]
    public class SemanticAnalysisResultTests
    {
        [Test]
        public void CreateSuccess_ReturnsSuccessfulResult()
        {
            // Act
            var result = SemanticAnalysisResult.CreateSuccess();

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.Warnings, Is.Empty);
        }

        [Test]
        public void Failure_ReturnsFailedResult_WithErrors()
        {
            // Act
            var result = SemanticAnalysisResult.Failure("error1", "error2");

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.Errors, Has.Count.EqualTo(2));
            Assert.That(result.Errors[0], Is.EqualTo("error1"));
            Assert.That(result.Errors[1], Is.EqualTo("error2"));
        }

        [Test]
        public void Failure_WithNoErrors_ReturnsFailedResult_WithEmptyErrorList()
        {
            // Act
            var result = SemanticAnalysisResult.Failure();

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.Errors, Is.Empty);
        }

        [Test]
        public void NewResult_HasDefaultSuccessValueFalse()
        {
            // Arrange & Act
            var result = new SemanticAnalysisResult();

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.Errors, Is.Not.Null);
            Assert.That(result.Warnings, Is.Not.Null);
        }
    }
}
