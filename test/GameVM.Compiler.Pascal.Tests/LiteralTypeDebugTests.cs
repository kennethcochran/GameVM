using System;
using NUnit.Framework;
using GameVM.Compiler.Pascal;
using GameVM.Compiler.Core.IR;

namespace GameVM.Compiler.Pascal.Tests
{
    [TestFixture]
    public class LiteralTypeDebugTests
    {
        private TransformationContext _context;
        private ExpressionTransformer _expressionTransformer;

        [SetUp]
        public void Setup()
        {
            var ir = new HighLevelIR { SourceFile = "test.pas" };
            _context = new TransformationContext("test.pas", ir);
            _expressionTransformer = new ExpressionTransformer(_context);
        }

        [Test]
        public void TransformIntegerLiteral_ShouldHaveCorrectType()
        {
            // Clear cache to ensure fresh type
            _context.TypeCache.Clear();
            
            // Arrange
            var literal = new IntegerLiteralNode { Value = "42" };

            // Act
            var result = _expressionTransformer.TransformExpression(literal);

            // Assert
            Assert.That(result, Is.Not.Null, "Result should not be null");
            Assert.That(result.Type.Name, Is.EqualTo("i32"), 
                $"Type name should be 'i32', but was: '{result.Type.Name}'");
            Assert.That(_context.Errors, Is.Empty, $"Should have no errors: {string.Join("; ", _context.Errors)}");
        }

        [Test]
        public void TransformVariable_ShouldHaveCorrectType()
        {
            // Arrange - add a variable first
            var varDecl = new VariableDeclarationNode 
            { 
                Name = "testVar",
                Type = new SimpleTypeNode { TypeName = "Integer" }
            };
            var declarationTransformer = new DeclarationTransformer(_context, _expressionTransformer);
            declarationTransformer.TransformVariableDeclaration(varDecl);

            var variable = new VariableNode { Name = "testVar" };

            // Act
            var result = _expressionTransformer.TransformExpression(variable);

            // Assert
            Assert.That(result, Is.Not.Null, "Result should not be null");
            Assert.That(result.Type.ToString(), Is.EqualTo("GameVM.Compiler.Core.IR.HighLevelIR+BasicType"), 
                $"Type should be BasicType, but was: {result.Type.ToString()}");
            Assert.That(_context.Errors, Is.Empty, $"Should have no errors: {string.Join("; ", _context.Errors)}");
        }

        [Test]
        public void TestBasicTypeDirectly()
        {
            // Test creating BasicType directly
            var basicType = new HighLevelIR.BasicType("test.pas", "i32");
            Console.WriteLine($"BasicType.Name: '{basicType.Name}'");
            Console.WriteLine($"BasicType.GetType(): '{basicType.GetType().Name}'");
            
            // Test through context
            _context.TypeCache.Clear();
            var contextType = _context.GetOrCreateBasicType("i32");
            Console.WriteLine($"ContextType.Name: '{contextType.Name}'");
            Console.WriteLine($"ContextType.GetType(): '{contextType.GetType().Name}'");
            
            Assert.That(basicType.Name, Is.EqualTo("i32"));
            Assert.That(contextType.Name, Is.EqualTo("i32"));
        }
    }
}
