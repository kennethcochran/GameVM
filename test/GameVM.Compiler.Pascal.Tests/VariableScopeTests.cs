using System;
using NUnit.Framework;
using GameVM.Compiler.Pascal;
using GameVM.Compiler.Core.IR;

namespace GameVM.Compiler.Pascal.Tests
{
    [TestFixture]
    public class VariableScopeTests
    {
        private TransformationContext _context;
        private DeclarationTransformer _declarationTransformer;
        private ExpressionTransformer _expressionTransformer;

        [SetUp]
        public void Setup()
        {
            var ir = new HighLevelIR { SourceFile = "test.pas" };
            _context = new TransformationContext("test.pas", ir);
            _expressionTransformer = new ExpressionTransformer(_context);
            _declarationTransformer = new DeclarationTransformer(_context, _expressionTransformer);
        }

        [Test]
        public void TransformVariableDeclaration_GlobalVariable_ShouldCreateSymbolWithCorrectType()
        {
            // Arrange
            var varDecl = new VariableDeclarationNode 
            { 
                Name = "global",
                Type = new SimpleTypeNode { TypeName = "Integer" }
            };

            // Act
            _declarationTransformer.TransformVariableDeclaration(varDecl);

            // Assert
            Assert.That(_context.TryGetSymbol("global", out var symbol), Is.True);
            Assert.That(symbol!.Type.ToString(), Is.EqualTo("GameVM.Compiler.Core.IR.HighLevelIR+BasicType"));
        }

        [Test]
        public void TransformVariableDeclaration_LocalVariableInProcedure_ShouldCreateSymbolInLocalScope()
        {
            // Arrange
            var varDecl = new VariableDeclarationNode 
            { 
                Name = "local",
                Type = new SimpleTypeNode { TypeName = "Integer" }
            };

            // Act - simulate procedure transformation
            _context.PushScope();
            _declarationTransformer.TransformVariableDeclaration(varDecl);

            // Assert
            Assert.That(_context.TryGetSymbol("local", out var symbol), Is.True);
            Assert.That(symbol!.Type.ToString(), Is.EqualTo("GameVM.Compiler.Core.IR.HighLevelIR+BasicType"));

            // Cleanup
            _context.PopScope();
        }

        [Test]
        public void TransformVariableDeclaration_GlobalVariableInProcedure_ShouldFindGlobalVariable()
        {
            // Arrange
            var globalVar = new VariableDeclarationNode 
            { 
                Name = "global",
                Type = new SimpleTypeNode { TypeName = "Integer" }
            };
            var localVar = new VariableDeclarationNode 
            { 
                Name = "local",
                Type = new SimpleTypeNode { TypeName = "Integer" }
            };
            
            // Add global variable first
            _declarationTransformer.TransformVariableDeclaration(globalVar);

            // Act - simulate procedure with local variable
            _context.PushScope();
            _declarationTransformer.TransformVariableDeclaration(localVar);

            // Both variables should be accessible
            Assert.That(_context.TryGetSymbol("global", out var globalSymbol), Is.True);
            Assert.That(globalSymbol!.Type.ToString(), Is.EqualTo("GameVM.Compiler.Core.IR.HighLevelIR+BasicType"));

            Assert.That(_context.TryGetSymbol("local", out var localSymbol), Is.True);
            Assert.That(localSymbol!.Type.ToString(), Is.EqualTo("GameVM.Compiler.Core.IR.HighLevelIR+BasicType"));

            // Cleanup
            _context.PopScope();
        }

        [Test]
        public void TransformVariable_UndefinedVariable_ShouldReturnErrorExpressionWithUnknownType()
        {
            // Arrange
            var varNode = new VariableNode { Name = "undefined" };

            // Act
            var result = _expressionTransformer.TransformExpression(varNode);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Type.ToString(), Is.EqualTo("GameVM.Compiler.Core.IR.HighLevelIR+HlType"));
            Assert.That(_context.Errors.Count, Is.GreaterThan(0));
            Assert.That(_context.Errors[0], Does.Contain("Undefined variable: undefined"));
        }

        [Test]
        public void TransformVariable_ScopedVariableAccess_ShouldResolveCorrectly()
        {
            // Arrange
            var globalVar = new VariableDeclarationNode 
            { 
                Name = "x",
                Type = new SimpleTypeNode { TypeName = "Integer" }
            };
            var localVar = new VariableDeclarationNode 
            { 
                Name = "x", // Same name, different scope
                Type = new SimpleTypeNode { TypeName = "Integer" }
            };
            
            // Add global variable
            _declarationTransformer.TransformVariableDeclaration(globalVar);

            // Act - simulate procedure with local variable of same name
            _context.PushScope();
            _declarationTransformer.TransformVariableDeclaration(localVar);

            // Local variable should shadow global variable
            var varNode = new VariableNode { Name = "x" };
            var result = _expressionTransformer.TransformExpression(varNode);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Type.ToString(), Is.EqualTo("GameVM.Compiler.Core.IR.HighLevelIR+BasicType")); // Should be BasicType, not HlType

            // Cleanup
            _context.PopScope();
        }
    }
}
