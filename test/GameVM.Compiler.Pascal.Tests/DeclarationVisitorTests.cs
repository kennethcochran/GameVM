using System.Collections.Generic;
using NUnit.Framework;
using GameVM.Compiler.Pascal;

namespace GameVM.Compiler.Pascal.Tests
{
    [TestFixture]
    public class DeclarationVisitorTests
    {
        [Test]
        public void DeclarationVisitor_CanBeInstantiated()
        {
            // Test that DeclarationVisitor can be instantiated with proper parameters
            var expressionVisitor = new ExpressionVisitor(new AstBuilder());
            var visitor = new DeclarationVisitor(expressionVisitor);
            
            Assert.That(visitor, Is.Not.Null);
        }

        [Test]
        public void DeclarationVisitor_CanBeInstantiatedWithAstBuilder()
        {
            // Test that DeclarationVisitor can be instantiated with both parameters
            var expressionVisitor = new ExpressionVisitor(new AstBuilder());
            var astBuilder = new AstBuilder();
            var visitor = new DeclarationVisitor(expressionVisitor, astBuilder);
            
            Assert.That(visitor, Is.Not.Null);
        }

        [Test]
        public void DeclarationVisitor_StaticMethodsExist()
        {
            // Test that the static methods exist by checking their accessibility
            // This is a simple test to ensure the methods are accessible
            var methodNames = new[] { "ProcessParameters", "ExtractVariantPart" };
            
            foreach (var methodName in methodNames)
            {
                var method = typeof(DeclarationVisitor).GetMethod(methodName, 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                
                Assert.That(method, Is.Not.Null, $"Method {methodName} should exist as static");
            }
        }
    }
}
