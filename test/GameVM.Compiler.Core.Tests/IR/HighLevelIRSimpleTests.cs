using NUnit.Framework;
using System.Collections.Generic;
using GameVM.Compiler.Core.IR;
using GameVM.Compiler.Core.Enums;

namespace GameVM.Compiler.Core.Tests.IR
{
    [TestFixture]
    public class HighLevelIRSimpleTests
    {
        #region Variable Tests

        [Test]
        public void Variable_ConstructorWithNameTypeAndSourceFile_SetsProperties()
        {
            // Arrange
            var type = new HighLevelIR.BasicType("test.pas", "integer");

            // Act
            var variable = new HighLevelIR.Variable("counter", type, "test.pas");

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(variable.Name, Is.EqualTo("counter"));
                Assert.That(variable.Type, Is.SameAs(type));
                Assert.That(variable.SourceFile, Is.EqualTo("test.pas"));
            });
        }

        #endregion

        #region HlType Tests

        [Test]
        public void HlType_DefaultConstructor_CreatesEmptyType()
        {
            // Arrange & Act
            var type = new HighLevelIR.HlType { Name = "unknown" };

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(type, Is.Not.Null);
                Assert.That(type.Name, Is.EqualTo("unknown"));
                Assert.That(type.SourceFile, Is.EqualTo(string.Empty));
            });
        }

        [Test]
        public void HlType_ConstructorWithSourceFileAndName_SetsProperties()
        {
            // Act
            var type = new HighLevelIR.HlType("test.pas", "MyType");

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(type.SourceFile, Is.EqualTo("test.pas"));
                Assert.That(type.Name, Is.EqualTo("MyType"));
            });
        }

        [Test]
        public void HlType_ConstructorWithName_SetsNameAndDefaultsSourceFile()
        {
            // Act
            var type = new HighLevelIR.HlType("MyType");

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(type.Name, Is.EqualTo("MyType"));
                Assert.That(type.SourceFile, Is.EqualTo("unknown"));
            });
        }

        [Test]
        public void HlType_ImplicitOperatorFromString_CreatesType()
        {
            // Arrange & Act
            HighLevelIR.HlType type = "MyType";

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(type.Name, Is.EqualTo("MyType"));
                Assert.That(type.SourceFile, Is.EqualTo("unknown"));
            });
        }

        #endregion

        #region Function Capability Tests

        [Test]
        public void Function_DefaultConstructor_SetsCapabilityDefaults()
        {
            // Arrange & Act
            var function = new HighLevelIR.Function();

            // Assert
            Assert.That(function.RequiredLevel, Is.EqualTo(CapabilityLevel.L1));
            Assert.That(function.RequiredExtensionId, Is.Null);
        }

        [Test]
        public void Function_SetCapabilityProperties_SetsValues()
        {
            // Arrange
            var function = new HighLevelIR.Function();

            // Act
            function.RequiredLevel = CapabilityLevel.L3;
            function.RequiredExtensionId = "Ext.Math.Fast";

            // Assert
            Assert.That(function.RequiredLevel, Is.EqualTo(CapabilityLevel.L3));
            Assert.That(function.RequiredExtensionId, Is.EqualTo("Ext.Math.Fast"));
        }

        #endregion

        #region Assignment Tests

        [Test]
        public void Assignment_DefaultConstructor_SetsDefaults()
        {
            // Arrange & Act
            var assignment = new HighLevelIR.Assignment();

            // Assert
            Assert.That(assignment.Target, Is.EqualTo(string.Empty));
            Assert.That(assignment.Value, Is.Not.Null);
            Assert.That(assignment.SourceFile, Is.EqualTo("unknown"));
        }

        [Test]
        public void Assignment_ConstructorWithTargetValueAndSourceFile_SetsProperties()
        {
            // Arrange
            var value = new HighLevelIR.Expression("test.pas");

            // Act
            var assignment = new HighLevelIR.Assignment("target", value, "test.pas");

            // Assert
            Assert.That(assignment.Target, Is.EqualTo("target"));
            Assert.That(assignment.Value, Is.SameAs(value));
            Assert.That(assignment.SourceFile, Is.EqualTo("test.pas"));
        }

        [Test]
        public void Assignment_ConstructorWithTargetAndValue_SetsSourceFileToUnknown()
        {
            // Arrange
            var value = new HighLevelIR.Expression("test.pas");

            // Act
            var assignment = new HighLevelIR.Assignment("target", value);

            // Assert
            Assert.That(assignment.Target, Is.EqualTo("target"));
            Assert.That(assignment.Value, Is.SameAs(value));
            Assert.That(assignment.SourceFile, Is.EqualTo("unknown"));
        }

        [Test]
        public void Assignment_ConstructorWithExpressionTargetValueAndSourceFile_SetsTargetFromExpression()
        {
            // Arrange
            var target = new HighLevelIR.Identifier("targetVar", "test.pas");
            var value = new HighLevelIR.Expression("test.pas");

            // Act
            var assignment = new HighLevelIR.Assignment(target, value, "test.pas");

            // Assert
            Assert.That(assignment.Target, Is.EqualTo("targetVar"));
            Assert.That(assignment.Value, Is.SameAs(value));
            Assert.That(assignment.SourceFile, Is.EqualTo("test.pas"));
        }

        #endregion

        #region Literal Tests

        [Test]
        public void Literal_DefaultConstructor_SetsDefaults()
        {
            // Arrange & Act
            var literal = new HighLevelIR.Literal();

            // Assert
            Assert.That(literal.Value, Is.EqualTo(string.Empty));
            Assert.That(literal.Type, Is.Not.Null);
            Assert.That(literal.Type.Name, Is.EqualTo("unknown"));
            Assert.That(literal.SourceFile, Is.EqualTo("unknown"));
        }

        [Test]
        public void Literal_ConstructorWithObjectValue_SetsProperties()
        {
            // Arrange
            var type = new HighLevelIR.BasicType("test.pas", "integer");

            // Act
            var literal = new HighLevelIR.Literal(42, type, "test.pas");

            // Assert
            Assert.That(literal.Value, Is.EqualTo(42));
            Assert.That(literal.Type, Is.SameAs(type));
            Assert.That(literal.SourceFile, Is.EqualTo("test.pas"));
        }

        [Test]
        public void Literal_ConstructorWithStringValue_SetsProperties()
        {
            // Arrange
            var type = new HighLevelIR.BasicType("test.pas", "string");

            // Act
            var literal = new HighLevelIR.Literal("hello", type, "test.pas");

            // Assert
            Assert.That(literal.Value, Is.EqualTo("hello"));
            Assert.That(literal.Type, Is.SameAs(type));
            Assert.That(literal.SourceFile, Is.EqualTo("test.pas"));
        }

        [Test]
        public void Literal_ConstructorWithDifferentValueTypes_SetsProperties()
        {
            // Arrange
            var intType = new HighLevelIR.BasicType("test.pas", "integer");
            var stringType = new HighLevelIR.BasicType("test.pas", "string");

            // Act
            var intLiteral = new HighLevelIR.Literal(42, intType, "test.pas");
            var stringLiteral = new HighLevelIR.Literal("hello", stringType, "test.pas");

            // Assert
            Assert.That(intLiteral.Value, Is.EqualTo(42));
            Assert.That(intLiteral.Type, Is.SameAs(intType));
            Assert.That(intLiteral.SourceFile, Is.EqualTo("test.pas"));

            Assert.That(stringLiteral.Value, Is.EqualTo("hello"));
            Assert.That(stringLiteral.Type, Is.SameAs(stringType));
            Assert.That(stringLiteral.SourceFile, Is.EqualTo("test.pas"));
        }

        #endregion

        #region Identifier Tests

        [Test]
        public void Identifier_DefaultConstructor_SetsDefaults()
        {
            // Arrange & Act
            var identifier = new HighLevelIR.Identifier();

            // Assert
            Assert.That(identifier.Name, Is.EqualTo(string.Empty));
            Assert.That(identifier.Type, Is.Not.Null);
            Assert.That(identifier.SourceFile, Is.EqualTo("unknown"));
        }

        [Test]
        public void Identifier_ConstructorWithNameTypeAndSourceFile_SetsProperties()
        {
            // Arrange
            var type = new HighLevelIR.BasicType("test.pas", "integer");

            // Act
            var identifier = new HighLevelIR.Identifier("myVar", type, "test.pas");

            // Assert
            Assert.That(identifier.Name, Is.EqualTo("myVar"));
            Assert.That(identifier.Type, Is.SameAs(type));
            Assert.That(identifier.SourceFile, Is.EqualTo("test.pas"));
        }

        [Test]
        public void Identifier_ConstructorWithNameAndSourceFile_SetsTypeToUnknown()
        {
            // Act
            var identifier = new HighLevelIR.Identifier("myVar", "test.pas");

            // Assert
            Assert.That(identifier.Name, Is.EqualTo("myVar"));
            Assert.That(identifier.Type, Is.Not.Null);
            Assert.That(identifier.Type.Name, Is.EqualTo("unknown"));
            Assert.That(identifier.SourceFile, Is.EqualTo("test.pas"));
        }

        #endregion

        #region FunctionCall Tests

        [Test]
        public void FunctionCall_ConstructorWithExpressionAndArguments_SetsProperties()
        {
            // Arrange
            var function = new HighLevelIR.Identifier("testFunc", "test.pas");
            var arguments = new List<HighLevelIR.Expression>
            {
                new HighLevelIR.Literal(42, new HighLevelIR.BasicType("test.pas", "integer"), "test.pas"),
                new HighLevelIR.Literal("hello", new HighLevelIR.BasicType("test.pas", "string"), "test.pas")
            };

            // Act
            var functionCall = new HighLevelIR.FunctionCall(function, arguments);

            // Assert
            Assert.That(functionCall.CallTarget, Is.SameAs(function));
            Assert.That(functionCall.Arguments, Is.EqualTo(arguments));
            Assert.That(functionCall.FunctionName, Is.EqualTo("testFunc"));
            Assert.That(functionCall.SourceFile, Is.EqualTo("test.pas"));
        }

        [Test]
        public void FunctionCall_ConstructorWithNonIdentifierTarget_SetsFunctionNameToEmpty()
        {
            // Arrange
            var function = new HighLevelIR.Expression("test.pas"); // Not an Identifier
            var arguments = new List<HighLevelIR.Expression>();

            // Act
            var functionCall = new HighLevelIR.FunctionCall(function, arguments);

            // Assert
            Assert.That(functionCall.CallTarget, Is.SameAs(function));
            Assert.That(functionCall.Arguments, Is.EqualTo(arguments));
            Assert.That(functionCall.FunctionName, Is.EqualTo(string.Empty));
        }

        #endregion

        #region UnaryOp Tests

        [Test]
        public void UnaryOp_Constructor_SetsProperties()
        {
            // Arrange
            var operand = new HighLevelIR.Literal(42, new HighLevelIR.BasicType("test.pas", "integer"), "test.pas");

            // Act
            var unaryOp = new HighLevelIR.UnaryOp("-", operand, "test.pas");

            // Assert
            Assert.That(unaryOp.Operator, Is.EqualTo("-"));
            Assert.That(unaryOp.Operand, Is.SameAs(operand));
            Assert.That(unaryOp.SourceFile, Is.EqualTo("test.pas"));
        }

        [Test]
        public void UnaryOp_DifferentOperators_SetsCorrectly()
        {
            // Arrange
            var operand = new HighLevelIR.Literal(5, new HighLevelIR.BasicType("test.pas", "integer"), "test.pas");

            // Act
            var notOp = new HighLevelIR.UnaryOp("!", operand, "test.pas");
            var negOp = new HighLevelIR.UnaryOp("-", operand, "test.pas");
            var plusOp = new HighLevelIR.UnaryOp("+", operand, "test.pas");

            // Assert
            Assert.That(notOp.Operator, Is.EqualTo("!"));
            Assert.That(negOp.Operator, Is.EqualTo("-"));
            Assert.That(plusOp.Operator, Is.EqualTo("+"));
            Assert.That(notOp.Operand, Is.SameAs(operand));
            Assert.That(negOp.Operand, Is.SameAs(operand));
            Assert.That(plusOp.Operand, Is.SameAs(operand));
        }

        #endregion

        #region BinaryOp Tests

        [Test]
        public void BinaryOp_DefaultConstructor_SetsDefaults()
        {
            // Arrange & Act
            var binaryOp = new HighLevelIR.BinaryOp();

            // Assert
            Assert.That(binaryOp.Operator, Is.EqualTo(string.Empty));
            Assert.That(binaryOp.Left, Is.Not.Null);
            Assert.That(binaryOp.Right, Is.Not.Null);
            Assert.That(binaryOp.SourceFile, Is.EqualTo("unknown"));
        }

        [Test]
        public void BinaryOp_ConstructorWithOpLeftRightAndSourceFile_SetsProperties()
        {
            // Arrange
            var left = new HighLevelIR.Expression("test.pas");
            var right = new HighLevelIR.Expression("test.pas");

            // Act
            var binaryOp = new HighLevelIR.BinaryOp("+", left, right, "test.pas");

            // Assert
            Assert.That(binaryOp.Operator, Is.EqualTo("+"));
            Assert.That(binaryOp.Left, Is.SameAs(left));
            Assert.That(binaryOp.Right, Is.SameAs(right));
            Assert.That(binaryOp.SourceFile, Is.EqualTo("test.pas"));
        }

        [Test]
        public void BinaryOp_ConstructorWithLeftOpRightAndSourceFile_SetsProperties()
        {
            // Arrange
            var left = new HighLevelIR.Expression("test.pas");
            var right = new HighLevelIR.Expression("test.pas");

            // Act
            var binaryOp = new HighLevelIR.BinaryOp(left, "*", right, "test.pas");

            // Assert
            Assert.That(binaryOp.Operator, Is.EqualTo("*"));
            Assert.That(binaryOp.Left, Is.SameAs(left));
            Assert.That(binaryOp.Right, Is.SameAs(right));
            Assert.That(binaryOp.SourceFile, Is.EqualTo("test.pas"));
        }

        [Test]
        public void BinaryOp_ConstructorWithLeftOpRight_SetsSourceFileFromLeft()
        {
            // Arrange
            var left = new HighLevelIR.Expression("source.pas");
            var right = new HighLevelIR.Expression("test.pas");

            // Act
            var binaryOp = new HighLevelIR.BinaryOp(left, "-", right);

            // Assert
            Assert.That(binaryOp.Operator, Is.EqualTo("-"));
            Assert.That(binaryOp.Left, Is.SameAs(left));
            Assert.That(binaryOp.Right, Is.SameAs(right));
            Assert.That(binaryOp.SourceFile, Is.EqualTo("source.pas"));
        }

        #endregion

        #region Redundant Classes Tests

        [Test]
        public void IfStatement_ShouldWorkWithoutRedundantIfClass()
        {
            // Arrange & Act - This test documents that the If class was removed
            var ifStatement = new HighLevelIR.IfStatement();

            // Assert - IfStatement should work fine without the redundant If class
            Assert.That(ifStatement, Is.Not.Null);
            Assert.That(ifStatement, Is.InstanceOf<HighLevelIR.IfStatement>());
        }

        #endregion

        #region ArrayAccess Tests

        [Test]
        public void ArrayAccess_Constructor_SetsProperties()
        {
            // Arrange
            var array = new HighLevelIR.Identifier("myArray", new HighLevelIR.BasicType("test.pas", "array"), "test.pas");
            var index = new HighLevelIR.Literal(5, new HighLevelIR.BasicType("test.pas", "integer"), "test.pas");

            // Act
            var arrayAccess = new HighLevelIR.ArrayAccess(array, index, "test.pas");

            // Assert
            Assert.That(arrayAccess.Array, Is.SameAs(array));
            Assert.That(arrayAccess.Index, Is.SameAs(index));
            Assert.That(arrayAccess.SourceFile, Is.EqualTo("test.pas"));
        }

        [Test]
        public void ArrayAccess_DifferentIndexTypes_SetsCorrectly()
        {
            // Arrange
            var array = new HighLevelIR.Identifier("data", new HighLevelIR.BasicType("test.pas", "array"), "test.pas");
            var intIndex = new HighLevelIR.Literal(3, new HighLevelIR.BasicType("test.pas", "integer"), "test.pas");
            var varIndex = new HighLevelIR.Identifier("i", new HighLevelIR.BasicType("test.pas", "integer"), "test.pas");

            // Act
            var intAccess = new HighLevelIR.ArrayAccess(array, intIndex, "test.pas");
            var varAccess = new HighLevelIR.ArrayAccess(array, varIndex, "test.pas");

            // Assert
            Assert.That(intAccess.Array, Is.SameAs(array));
            Assert.That(intAccess.Index, Is.SameAs(intIndex));
            Assert.That(varAccess.Array, Is.SameAs(array));
            Assert.That(varAccess.Index, Is.SameAs(varIndex));
        }

        #endregion

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
