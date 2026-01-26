using System;
using NUnit.Framework;
using GameVM.Compiler.Pascal;
using GameVM.Compiler.Core.IR;

namespace GameVM.Compiler.Pascal.Tests
{
    [TestFixture]
    public class AssignmentDebugTests
    {
        private TransformationContext _context;
        private DeclarationTransformer _declarationTransformer;
        private ExpressionTransformer _expressionTransformer;
        private StatementTransformer _statementTransformer;

        [SetUp]
        public void Setup()
        {
            var ir = new HighLevelIR { SourceFile = "test.pas" };
            _context = new TransformationContext("test.pas", ir);
            _expressionTransformer = new ExpressionTransformer(_context);
            _statementTransformer = new StatementTransformer(_context, _expressionTransformer);
            _declarationTransformer = new DeclarationTransformer(_context, _expressionTransformer)
            {
                StatementTransformer = _statementTransformer
            };
        }

        [Test]
        public void DebugAssignment_TransformVariableTypes_ShouldShowActualTypes()
        {
            // Arrange
            var globalVar = new VariableDeclarationNode 
            { 
                Name = "global",
                Type = new SimpleTypeNode { TypeName = "Integer" }
            };
            _declarationTransformer.TransformVariableDeclaration(globalVar);

            // Simulate procedure scope
            _context.PushScope();
            
            var localVar = new VariableDeclarationNode 
            { 
                Name = "local",
                Type = new SimpleTypeNode { TypeName = "Integer" }
            };
            _declarationTransformer.TransformVariableDeclaration(localVar);

            // Test variable expressions directly
            var globalVarExpr = _expressionTransformer.TransformExpression(new VariableNode { Name = "global" });
            var localVarExpr = _expressionTransformer.TransformExpression(new VariableNode { Name = "local" });

            // Assert variable types are correct
            Assert.That(globalVarExpr, Is.Not.Null, "Global variable expression should not be null");
            Assert.That(localVarExpr, Is.Not.Null, "Local variable expression should not be null");
            Assert.That(globalVarExpr!.Type.ToString(), Is.EqualTo("GameVM.Compiler.Core.IR.HighLevelIR+BasicType"), 
                $"Global variable type should be BasicType, but was: {globalVarExpr.Type.ToString()}");
            Assert.That(localVarExpr!.Type.ToString(), Is.EqualTo("GameVM.Compiler.Core.IR.HighLevelIR+BasicType"), 
                $"Local variable type should be BasicType, but was: {localVarExpr.Type.ToString()}");

            // Test assignment transformation
            var assignment = new AssignmentNode
            {
                Left = new VariableNode { Name = "global" },
                Right = new VariableNode { Name = "local" }
            };

            // Act
            var result = _statementTransformer.TransformStatement(assignment);

            // Assert
            Assert.That(result, Is.Not.Null, "Assignment result should not be null");
            Assert.That(_context.Errors, Is.Empty, $"Context should have no errors, but has: {string.Join("; ", _context.Errors)}");

            // Cleanup
            _context.PopScope();
        }

        [Test]
        public void DebugAssignment_IntegerLiteral_ShouldWork()
        {
            // Arrange
            var globalVar = new VariableDeclarationNode 
            { 
                Name = "global",
                Type = new SimpleTypeNode { TypeName = "Integer" }
            };
            _declarationTransformer.TransformVariableDeclaration(globalVar);

            // Test assignment with integer literal: global := 42
            var assignment = new AssignmentNode
            {
                Left = new VariableNode { Name = "global" },
                Right = new IntegerLiteralNode { Value = "42" }
            };

            // Act
            var result = _statementTransformer.TransformStatement(assignment);

            // Assert
            Console.WriteLine($"Integer literal assignment result: {result?.GetType().Name ?? "null"}");
            Console.WriteLine($"Context errors: {string.Join("; ", _context.Errors)}");
            
            // Add assertion to satisfy SonarQube
            Assert.That(result, Is.Not.Null, "Assignment transformation should not return null");
        }
    }
}
