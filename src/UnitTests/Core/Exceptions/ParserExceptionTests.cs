using System;
using GameVM.Compiler.Core.Exceptions;
using NUnit.Framework;

namespace UnitTests.Core.Exceptions
{
    [TestFixture]
    public class ParserExceptionTests
    {
        private const string TestMessage = "Test parser error message";
        private readonly Exception _innerException = new InvalidOperationException("Inner exception");

        [Test]
        public void Constructor_WithMessage_SetsMessage()
        {
            // Act
            var exception = new ParserException(TestMessage);

            // Assert
            Assert.That(exception.Message, Is.EqualTo(TestMessage));
            Assert.That(exception.InnerException, Is.Null);
        }

        [Test]
        public void Constructor_WithMessageAndInnerException_SetsMessageAndInnerException()
        {
            // Act
            var exception = new ParserException(TestMessage, _innerException);

            // Assert
            Assert.That(exception.Message, Is.EqualTo(TestMessage));
            Assert.That(exception.InnerException, Is.EqualTo(_innerException));
        }

        [Test]
        public void IsInstanceOf_CompilerException()
        {
            // Act
            var exception = new ParserException(TestMessage);

            // Assert
            Assert.That(exception, Is.InstanceOf<CompilerException>());
        }
    }
}
