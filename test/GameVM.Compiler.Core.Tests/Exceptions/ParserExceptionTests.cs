using System;
using GameVM.Compiler.Core.Exceptions;
using NUnit.Framework;

namespace GameVM.Compiler.Core.Tests.Exceptions
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
            Assert.That(exception, Is.InstanceOf<CompilerException>());
        }

        [Test]
        public void Constructor_WithMessageAndInnerException_SetsMessageAndInnerException()
        {
            // Act
            var exception = new ParserException(TestMessage, _innerException);

            // Assert
            Assert.That(exception.Message, Is.EqualTo(TestMessage));
            Assert.That(exception.InnerException, Is.EqualTo(_innerException));
            Assert.That(exception, Is.InstanceOf<CompilerException>());
        }

        [Test]
        public void ParserException_IsSubclassOfCompilerException()
        {
            // Act
            var exception = new ParserException(TestMessage);

            // Assert
            Assert.That(exception, Is.AssignableTo<CompilerException>());
        }

        [Test]
        public void ParserException_WithNullMessage_DoesNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => new ParserException(null!));
        }

        [Test]
        public void ParserException_WithEmptyMessage_DoesNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => new ParserException(string.Empty));
        }

        [Test]
        public void ParserException_WithNullInnerException_DoesNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => new ParserException(TestMessage, null!));
        }
    }
}
