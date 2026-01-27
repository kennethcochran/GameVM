using NUnit.Framework;
using System.Collections.Generic;
using GameVM.Compiler.Core.IR;

namespace GameVM.Compiler.Core.Tests.IR
{
    [TestFixture]
    public class HighLevelIRSimpleTests
    {
        #region Function Tests

        [Test]
        public void Function_AddParameter_AddsToParametersList()
        {
            // Arrange
            var function = new HighLevelIR.Function();
            var parameter = new HighLevelIR.Parameter("x", "integer", "test.pas");

            // Act
            function.AddParameter(parameter);

            // Assert
            Assert.That(function.Parameters, Has.Count.EqualTo(1));
            Assert.That(function.Parameters[0], Is.EqualTo(parameter));
        }

        #endregion

        #region Block Tests

        [Test]
        public void Block_AddStatement_AddsToStatementsList()
        {
            // Arrange
            var block = new HighLevelIR.Block();
            var statement = new HighLevelIR.Statement("test.pas");

            // Act
            block.AddStatement(statement);

            // Assert
            Assert.That(block.Statements, Has.Count.EqualTo(1));
            Assert.That(block.Statements[0], Is.EqualTo(statement));
        }

        #endregion

        #region SourceFile Tests

        [Test]
        public void HighLevelIR_SourceFileProperty_CanBeSetAndGet()
        {
            // Arrange
            var highLevelIR = new HighLevelIR();

            // Act
            highLevelIR.SourceFile = "test.pas";

            // Assert
            Assert.That(highLevelIR.SourceFile, Is.EqualTo("test.pas"));
        }

        [Test]
        public void HighLevelIR_SourceFileProperty_DefaultsToEmpty()
        {
            // Arrange & Act
            var highLevelIR = new HighLevelIR();

            // Assert
            Assert.That(highLevelIR.SourceFile, Is.EqualTo(string.Empty));
        }

        #endregion

        #region ReturnStatement Tests

        [Test]
        public void ReturnStatement_DefaultConstructor_CreatesEmptyReturn()
        {
            // Arrange & Act
            var returnStatement = new HighLevelIR.ReturnStatement();

            // Assert
            Assert.That(returnStatement.Value, Is.Null);
        }

        [Test]
        public void ReturnStatement_ConstructorWithValue_SetsValue()
        {
            // Arrange
            var expression = new HighLevelIR.Expression("test.pas");

            // Act
            var returnStatement = new HighLevelIR.ReturnStatement(expression);

            // Assert
            Assert.That(returnStatement.Value, Is.EqualTo(expression));
        }

        [Test]
        public void ReturnStatement_ConstructorWithNullValue_SetsValueToNull()
        {
            // Arrange & Act
            var returnStatement = new HighLevelIR.ReturnStatement(null);

            // Assert
            Assert.That(returnStatement.Value, Is.Null);
        }

        #endregion

        #region ForStatement Tests

        [Test]
        public void ForStatement_DefaultConstructor_SetsDefaults()
        {
            // Arrange & Act
            var forStatement = new HighLevelIR.ForStatement();

            // Assert
            Assert.That(forStatement.Iterator, Is.EqualTo(string.Empty));
            Assert.That(forStatement.Ascending, Is.False);
            Assert.That(forStatement.Body, Is.Not.Null);
            Assert.That(forStatement.SourceFile, Is.EqualTo("unknown"));
        }

        [Test]
        public void ForStatement_ConstructorWithAllParameters_SetsProperties()
        {
            // Arrange
            var initial = new HighLevelIR.Expression("test.pas");
            var limit = new HighLevelIR.Expression("test.pas");
            var body = new HighLevelIR.Block("test.pas");

            // Act
            var forStatement = new HighLevelIR.ForStatement("i", initial, limit, true, body, "test.pas");

            // Assert
            Assert.That(forStatement.Iterator, Is.EqualTo("i"));
            Assert.That(forStatement.Ascending, Is.True);
            Assert.That(forStatement.Body, Is.EqualTo(body));
            Assert.That(forStatement.SourceFile, Is.EqualTo("test.pas"));
        }

        [Test]
        public void ForStatement_ConstructorWithDescendingLoop_SetsAscendingToFalse()
        {
            // Arrange
            var initial = new HighLevelIR.Expression("test.pas");
            var limit = new HighLevelIR.Expression("test.pas");
            var body = new HighLevelIR.Block("test.pas");

            // Act
            var forStatement = new HighLevelIR.ForStatement("i", initial, limit, false, body, "test.pas");

            // Assert
            Assert.That(forStatement.Ascending, Is.False);
        }

        #endregion
    }
}
