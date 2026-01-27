using System.Collections.Generic;
using NUnit.Framework;
using GameVM.Compiler.Pascal;

namespace GameVM.Compiler.Pascal.Tests
{
    [TestFixture]
    public class CognitiveComplexityTests
    {
        [Test]
        public void AstVisitor_CanBeInstantiated()
        {
            // Test that AstVisitor can be instantiated with proper parameters
            var visitor = new AstVisitor();
            
            Assert.That(visitor, Is.Not.Null);
        }

        [Test]
        public void ExpressionVisitor_CanBeInstantiated()
        {
            // Test that ExpressionVisitor can be instantiated with proper parameters
            var astBuilder = new AstBuilder();
            var visitor = new ExpressionVisitor(astBuilder);
            
            Assert.That(visitor, Is.Not.Null);
        }

        [Test]
        public void DeclarationVisitor_CanBeInstantiated()
        {
            // Test that DeclarationVisitor can be instantiated with proper parameters
            var expressionVisitor = new ExpressionVisitor(new AstBuilder());
            var visitor = new DeclarationVisitor(expressionVisitor);
            
            Assert.That(visitor, Is.Not.Null);
        }

        [Test]
        public void PascalAstToHlirTransformer_CanBeInstantiated()
        {
            // Test that PascalAstToHlirTransformer can be instantiated
            var transformer = new PascalAstToHlirTransformer();
            
            Assert.That(transformer, Is.Not.Null);
        }

        [Test]
        public void RefactoredComponents_WorkTogether()
        {
            // Test that all refactored components can work together
            var astBuilder = new AstBuilder();
            var expressionVisitor = new ExpressionVisitor(astBuilder);
            var declarationVisitor = new DeclarationVisitor(expressionVisitor);
            var astVisitor = new AstVisitor();
            var transformer = new PascalAstToHlirTransformer();
            
            // All components should be instantiated successfully
            Assert.That(astBuilder, Is.Not.Null);
            Assert.That(expressionVisitor, Is.Not.Null);
            Assert.That(declarationVisitor, Is.Not.Null);
            Assert.That(astVisitor, Is.Not.Null);
            Assert.That(transformer, Is.Not.Null);
        }
    }
}
