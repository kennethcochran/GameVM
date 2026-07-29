using System.Collections.Generic;
using NUnit.Framework;
using GameVM.Compiler.Pascal;
using GameVM.Compiler.Pascal.ANTLR;
using Antlr4.Runtime;

namespace GameVM.Compiler.Pascal.Tests
{
    [TestFixture]
    public class DeclarationVisitorTests
    {
        private DeclarationVisitor _visitor = null!;
        private ExpressionVisitor _expressionVisitor = null!;
        private AstVisitor _mainVisitor = null!;

        [SetUp]
        public void SetUp()
        {
            _expressionVisitor = new ExpressionVisitor(new AstBuilder());
            _visitor = new DeclarationVisitor(_expressionVisitor);
            _mainVisitor = new AstVisitor();
            _visitor.SetMainVisitor(_mainVisitor);
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
        public void DeclarationVisitor_CanBeInstantiatedWithAstBuilder()
        {
            // Test that DeclarationVisitor can be instantiated with both parameters
            var expressionVisitor = new ExpressionVisitor(new AstBuilder());
            var astBuilder = new AstBuilder();
            var visitor = new DeclarationVisitor(expressionVisitor, astBuilder);
            
            Assert.That(visitor, Is.Not.Null);
        }

        // RED PHASE: Tests for ProcessParameters (tested indirectly via VisitProcedureDeclaration)
        [Test]
        public void VisitProcedureDeclaration_ShouldHandleProcedureWithNoParameters()
        {
            // Arrange
            var input = "procedure Test; begin end;";
            var inputStream = new AntlrInputStream(input);
            var lexer = new PascalLexer(inputStream);
            var tokenStream = new CommonTokenStream(lexer);
            var parser = new PascalParser(tokenStream);
            var context = parser.procedureDeclaration();

            // Act
            var result = _visitor.VisitProcedureDeclaration(context);

            // Assert
            Assert.That(result, Is.InstanceOf<ProcedureNode>());
            var procNode = result as ProcedureNode;
            Assert.That(procNode!.Name, Is.EqualTo("Test"));
            Assert.That(procNode.Parameters, Is.Empty);
        }

        // RED PHASE: One failing test at a time for VisitConstantDefinition - success case first
        [Test]
        public void VisitConstantDefinition_ValidConstant_ShouldReturnConstantDeclarationNode()
        {
            // Arrange - Test the success path to understand current behavior
            var input = "x = 42"; // Valid constant definition
            var inputStream = new AntlrInputStream(input);
            var lexer = new PascalLexer(inputStream);
            var tokenStream = new CommonTokenStream(lexer);
            var parser = new PascalParser(tokenStream);
            var context = parser.constantDefinition();

            // Act
            var result = _visitor.VisitConstantDefinition(context);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.InstanceOf<ConstantDeclarationNode>(), "Should return ConstantDeclarationNode for valid constant");
            var constNode = result as ConstantDeclarationNode;
            Assert.That(constNode!.Name, Is.EqualTo("x"));
            Assert.That(constNode.Value, Is.Not.Null);
        }

        [Test]
        public void VisitProcedureDeclaration_ShouldHandleProcedureWithSingleParameter()
        {
            // Arrange
            var input = "procedure Test(x: integer); begin end;";
            var inputStream = new AntlrInputStream(input);
            var lexer = new PascalLexer(inputStream);
            var tokenStream = new CommonTokenStream(lexer);
            var parser = new PascalParser(tokenStream);
            var context = parser.procedureDeclaration();

            // Act
            var result = _visitor.VisitProcedureDeclaration(context);

            // Assert
            Assert.That(result, Is.InstanceOf<ProcedureNode>());
            var procNode = result as ProcedureNode;
            Assert.That(procNode!.Name, Is.EqualTo("Test"));
            Assert.That(procNode.Parameters, Has.Count.EqualTo(1));
            Assert.That(procNode.Parameters[0].Name, Is.EqualTo("x"));
        }

        [Test]
        public void VisitProcedureDeclaration_ShouldHandleProcedureWithMultipleParameters()
        {
            // Arrange
            var input = "procedure Test(x, y, z: integer); begin end;";
            var inputStream = new AntlrInputStream(input);
            var lexer = new PascalLexer(inputStream);
            var tokenStream = new CommonTokenStream(lexer);
            var parser = new PascalParser(tokenStream);
            var context = parser.procedureDeclaration();

            // Act
            var result = _visitor.VisitProcedureDeclaration(context);

            // Assert
            Assert.That(result, Is.InstanceOf<ProcedureNode>());
            var procNode = result as ProcedureNode;
            Assert.That(procNode!.Name, Is.EqualTo("Test"));
            Assert.That(procNode.Parameters, Has.Count.EqualTo(3));
            Assert.That(procNode.Parameters[0].Name, Is.EqualTo("x"));
            Assert.That(procNode.Parameters[1].Name, Is.EqualTo("y"));
            Assert.That(procNode.Parameters[2].Name, Is.EqualTo("z"));
        }

        [Test]
        public void VisitProcedureDeclaration_ShouldHandleProcedureWithMultipleParameterSections()
        {
            // Arrange
            var input = "procedure Test(x: integer; y: string; z: boolean); begin end;";
            var inputStream = new AntlrInputStream(input);
            var lexer = new PascalLexer(inputStream);
            var tokenStream = new CommonTokenStream(lexer);
            var parser = new PascalParser(tokenStream);
            var context = parser.procedureDeclaration();

            // Act
            var result = _visitor.VisitProcedureDeclaration(context);

            // Assert
            Assert.That(result, Is.InstanceOf<ProcedureNode>());
            var procNode = result as ProcedureNode;
            Assert.That(procNode!.Name, Is.EqualTo("Test"));
            Assert.That(procNode.Parameters, Has.Count.EqualTo(3));
            Assert.That(procNode.Parameters[0].Name, Is.EqualTo("x"));
            Assert.That(procNode.Parameters[1].Name, Is.EqualTo("y"));
            Assert.That(procNode.Parameters[2].Name, Is.EqualTo("z"));
        }

        [Test]
        public void VisitProcedureDeclaration_ShouldHandleProcedureWithValidStructure()
        {
            // Arrange - Test that ProcessParameters is called correctly through valid procedure
            var input = "procedure Test(x: integer; y: string); begin end;";
            var inputStream = new AntlrInputStream(input);
            var lexer = new PascalLexer(inputStream);
            var tokenStream = new CommonTokenStream(lexer);
            var parser = new PascalParser(tokenStream);
            var context = parser.procedureDeclaration();

            // Act
            var result = _visitor.VisitProcedureDeclaration(context);

            // Assert
            Assert.That(result, Is.InstanceOf<ProcedureNode>());
            var procNode = result as ProcedureNode;
            Assert.That(procNode!.Name, Is.EqualTo("Test"));
            Assert.That(procNode.Parameters, Has.Count.EqualTo(2));
            Assert.That(procNode.Parameters[0].Name, Is.EqualTo("x"));
            Assert.That(procNode.Parameters[1].Name, Is.EqualTo("y"));
        }
    }
}
