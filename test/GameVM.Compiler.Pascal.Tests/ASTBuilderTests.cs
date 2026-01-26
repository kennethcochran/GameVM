using System;
using System.Collections.Generic;
using NUnit.Framework;
using GameVM.Compiler.Pascal;

namespace GameVM.Compiler.Pascal.Tests
{
    [TestFixture]
    public class ASTBuilderTests
    {
        private AstBuilder _builder = null!;

        [SetUp]
        public void Setup()
        {
            _builder = new AstBuilder();
        }

        [Test]
        public void CreateProgram_ValidInput_ReturnsProgramNode()
        {
            var block = new BlockNode { Statements = new List<PascalAstNode>() };
            var node = _builder.CreateProgram("Test", block);

            Assert.That(node, Is.Not.Null);
            Assert.That(node.Name, Is.EqualTo("Test"));
            Assert.That(node.Block, Is.SameAs(block));
        }

        [Test]
        public void CreateVariable_ValidInput_ReturnsVariableNode()
        {
            var node = _builder.CreateVariable("x");

            Assert.That(node, Is.Not.Null);
            Assert.That(node.Name, Is.EqualTo("x"));
        }

        [Test]
        public void CreateIntegerLiteral_ValidString_ReturnsNodeWithCorrectValue()
        {
            var node = _builder.CreateIntegerLiteral("123");

            Assert.That(node, Is.Not.Null);
            Assert.That(node.Value, Is.EqualTo(123));
        }

        [Test]
        public void CreateStringLiteral_WithQuotes_StripsQuotes()
        {
            var node = _builder.CreateStringLiteral("'hello'");

            Assert.That(node, Is.Not.Null);
            Assert.That(node.Value, Is.EqualTo("hello"));
        }

        [Test]
        public void CreateAssignment_ValidInputs_ReturnsAssignmentNode()
        {
            var left = new VariableNode { Name = "x" };
            var right = new IntegerLiteralNode { Value = 42 };
            var node = _builder.CreateAssignment(left, right);

            Assert.That(node, Is.Not.Null);
            Assert.That(node.Left, Is.SameAs(left));
            Assert.That(node.Right, Is.SameAs(right));
        }

        [Test]
        public void CreateAdditive_ValidInputs_ReturnsOperatorNode()
        {
            var left = new IntegerLiteralNode { Value = 1 };
            var right = new IntegerLiteralNode { Value = 2 };
            var node = _builder.CreateAdditive(left, "+", right);

            Assert.That(node, Is.Not.Null);
            Assert.That(node.Left, Is.SameAs(left));
            Assert.That(node.Operator, Is.EqualTo("+"));
            Assert.That(node.Right, Is.SameAs(right));
        }
    }
}
