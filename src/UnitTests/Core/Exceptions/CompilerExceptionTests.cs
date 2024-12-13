using System;
using GameVM.Compiler.Core.Exceptions;
using NUnit.Framework;

namespace UnitTests.Core.Exceptions
{
    [TestFixture]
    public class CompilerExceptionTests
    {
        private const string TestMessage = "Test error message";
        private readonly Exception _innerException = new InvalidOperationException("Inner exception");

        [Test]
        public void Constructor_WithMessage_SetsMessage()
        {
            // Act
            var exception = new CompilerException(TestMessage);

            // Assert
            Assert.That(exception.Message, Is.EqualTo(TestMessage));
            Assert.That(exception.InnerException, Is.Null);
        }

        [Test]
        public void Constructor_WithMessageAndInnerException_SetsMessageAndInnerException()
        {
            // Act
            var exception = new CompilerException(TestMessage, _innerException);

            // Assert
            Assert.That(exception.Message, Is.EqualTo(TestMessage));
            Assert.That(exception.InnerException, Is.EqualTo(_innerException));
        }
    }
}
