using System;
using NUnit.Framework;
using GameVM.Compiler.Pascal;
using GameVM.Compiler.Core.IR;

namespace GameVM.Compiler.Pascal.Tests
{
    [TestFixture]
    public class BDDVariableScopeTests
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
        public void TransformBDDScenario_GlobalAndLocalVariables_ShouldCompileSuccessfully()
        {
            // This reproduces the exact BDD scenario that's failing with global and local variables

            // Arrange - Add global variable
            var globalVar = new VariableDeclarationNode 
            { 
                Name = "global",
                Type = new SimpleTypeNode { TypeName = "Integer" }
            };
            _declarationTransformer.TransformVariableDeclaration(globalVar);

            // Arrange - Create procedure
            var procNode = new ProcedureNode 
            { 
                Name = "TestProc",
                Parameters = new List<VariableNode>(),
                Block = new BlockNode 
                { 
                    Statements = new List<PascalAstNode>
                    {
                        new VariableDeclarationNode 
                        { 
                            Name = "local",
                            Type = new SimpleTypeNode { TypeName = "Integer" }
                        },
                        new ExpressionStatementNode
                        {
                            Expression = new AssignmentNode
                            {
                                Left = new VariableNode { Name = "local" },
                                Right = new IntegerLiteralNode { Value = "42" }
                            }
                        },
                        new ExpressionStatementNode
                        {
                            Expression = new AssignmentNode
                            {
                                Left = new VariableNode { Name = "global" },
                                Right = new VariableNode { Name = "local" }
                            }
                        }
                    }
                }
            };

            // Act - Transform the procedure using reflection since TransformProcedure is private
            var method = typeof(DeclarationTransformer).GetMethod("TransformProcedure", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(_declarationTransformer, new object[] { procNode });

            // Assert - No errors should have occurred
            Assert.That(_context.Errors, Is.Empty, $"Compilation errors: {string.Join("; ", _context.Errors)}");

            // Verify symbols exist
            Assert.That(_context.TryGetSymbol("global", out var globalSymbol), Is.True, "Global variable should be accessible");
            Assert.That(globalSymbol!.Type.ToString(), Is.EqualTo("GameVM.Compiler.Core.IR.HighLevelIR+BasicType"));

            // The procedure should have been created
            Assert.That(_context.IR.Modules.Count, Is.GreaterThan(0));
            var module = _context.IR.Modules[0];
            Assert.That(module.Functions.Count, Is.GreaterThan(0));
            
            var procedure = module.Functions[0];
            Assert.That(procedure.Name, Is.EqualTo("TestProc"));
        }

        [Test]
        public void TransformAssignment_LocalToGlobal_ShouldHaveCorrectTypes()
        {
            // This specifically tests the assignment that's failing: global := local
            
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

            // Create assignment: global := local
            var assignment = new AssignmentNode
            {
                Left = new VariableNode { Name = "global" },
                Right = new VariableNode { Name = "local" }
            };

            // Act
            var result = _statementTransformer.TransformStatement(assignment);

            // Assert
            Assert.That(result, Is.Not.Null, "Assignment statement should be transformed");
            Assert.That(_context.Errors, Is.Empty, $"Transformation errors: {string.Join("; ", _context.Errors)}");

            // Cleanup
            _context.PopScope();
        }
    }
}
