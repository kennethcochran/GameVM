using Antlr4.Runtime;
using NUnit.Framework;

namespace GameVM.Compiler.Pascal.Tests
{
    [TestFixture]
    internal class AstTests
    {
        private PascalASTNode ParseProgram(string input)
        {
            var stream = new AntlrInputStream(input);
            var lexer = new PascalLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new PascalParser(tokens);
            var tree = parser.program();
            Console.WriteLine("Parse tree: " + tree.ToStringTree(parser)); // Debug log
            var visitor = new ASTVisitor();
            var result = visitor.Visit(tree);
            Console.WriteLine("AST Result: " + result); // Debug log
            return result;
        }

        [Test]
        public void AST_Builds_Correctly_From_Pascal_Program()
        {
            var input = @"program main;
                        begin
                            writeln('Hello, World!');
                        end.";
            
            var result = ParseProgram(input);

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.InstanceOf<ProgramNode>());
            var program = (ProgramNode)result;
            Assert.That(program.Name, Is.EqualTo("main"));
            Assert.That(program.Block, Is.Not.Null);
            Assert.That(program.Block.Statements, Has.Count.GreaterThan(0));
        }

        [Test]
        public void Test_Variable_Declaration()
        {
            var input = @"
                program Test;
                var
                    x: integer;
                begin
                end.";
            
            var result = ParseProgram(input);
            Assert.That(result, Is.InstanceOf<ProgramNode>());
            var program = (ProgramNode)result;
            Assert.That(program.Block.Statements, Has.Count.GreaterThan(0));
            Assert.That(program.Name, Is.EqualTo("Test"));
        }

        [Test]
        public void Test_Procedure_Declaration()
        {
            var input = @"
                program Test;
                procedure Foo;
                begin
                    writeln('Hello');
                end;
                begin
                end.";
            
            var result = ParseProgram(input);
            Assert.That(result, Is.InstanceOf<ProgramNode>());
            var program = (ProgramNode)result;
            Assert.That(program.Block.Statements, Has.Count.GreaterThan(0));
            var procDecl = program.Block.Statements[0] as ProcedureNode;
            Assert.That(procDecl, Is.Not.Null);
            Assert.That(procDecl!.Name, Is.EqualTo("Foo"));
        }

        [Test]
        public void Test_Function_Declaration()
        {
            var input = @"
                program Test;
                function Bar: integer;
                begin
                    Bar := 42;
                end;
                begin
                end.";
            
            var result = ParseProgram(input);
            Assert.That(result, Is.InstanceOf<ProgramNode>());
            var program = (ProgramNode)result;
            var funcDecl = program.Block.Statements[0] as FunctionNode;
            Assert.That(funcDecl, Is.Not.Null);
            Assert.That(funcDecl!.Name, Is.EqualTo("Bar"));
        }

        [Test]
        public void Test_Assignment_Statement()
        {
            var input = @"
                program Test;
                begin
                    x := 5;
                end.";
            
            var result = ParseProgram(input);
            Assert.That(result, Is.InstanceOf<ProgramNode>());
            var program = (ProgramNode)result;
            Assert.That(program.Block.Statements, Is.Not.Empty);
            Assert.That(program.Block.Statements[0], Is.InstanceOf<BlockNode>());
            var block = program.Block.Statements[0] as BlockNode;
            Assert.That(block, Is.Not.Null);
            Assert.That(block!.Statements, Is.Not.Empty);
            Assert.That(block.Statements[0], Is.InstanceOf<AssignmentNode>());
            var assignment = block.Statements[0] as AssignmentNode;
            Assert.That(assignment, Is.Not.Null);
            Assert.That(assignment!.Left, Is.InstanceOf<VariableNode>());
        }

        [Test]
        public void Test_If_Statement()
        {
            var input = @"
                program Test;
                begin
                    if x > 5 then
                        writeln('Hello');
                end.";
            
            var result = ParseProgram(input);
            Assert.That(result, Is.InstanceOf<ProgramNode>());
            var program = (ProgramNode)result;
            Assert.That(program.Block.Statements, Is.Not.Empty);
            Assert.That(program.Block.Statements[0], Is.InstanceOf<BlockNode>());
            var block = program.Block.Statements[0] as BlockNode;
            Assert.That(block, Is.Not.Null);
            Assert.That(block!.Statements, Is.Not.Empty);
            Assert.That(block.Statements[0], Is.InstanceOf<IfNode>());
            var ifStmt = block.Statements[0] as IfNode;
            Assert.That(ifStmt, Is.Not.Null);
            Assert.That(ifStmt!.Condition, Is.InstanceOf<RelationalOperatorNode>());
            Assert.That(ifStmt.ThenBlock, Is.Not.Null);
        }

        [Test]
        public void Test_While_Loop()
        {
            var input = @"
                program Test;
                begin
                    while x < 10 do
                        writeln('Hello');
                end.";
            
            var result = ParseProgram(input);
            Assert.That(result, Is.InstanceOf<ProgramNode>());
            var program = (ProgramNode)result;
            Assert.That(program.Block.Statements, Is.Not.Empty);
            Assert.That(program.Block.Statements[0], Is.InstanceOf<BlockNode>());
            var block = program.Block.Statements[0] as BlockNode;
            Assert.That(block, Is.Not.Null);
            Assert.That(block!.Statements, Is.Not.Empty);
            var whileStmt = block.Statements[0] as WhileNode;
            Assert.That(whileStmt, Is.Not.Null);
            Assert.That(whileStmt!.Condition, Is.InstanceOf<RelationalOperatorNode>());
            Assert.That(whileStmt.Block, Is.Not.Null);
        }

        [Test]
        public void Test_For_Loop()
        {
            var input = @"
                program Test;
                begin
                    for i := 1 to 10 do
                        writeln('Hello');
                end.";
            
            var result = ParseProgram(input);
            Assert.That(result, Is.InstanceOf<ProgramNode>());
            var program = (ProgramNode)result;
            Assert.That(program.Block.Statements, Is.Not.Empty);
            Assert.That(program.Block.Statements[0], Is.InstanceOf<BlockNode>());
            var block = program.Block.Statements[0] as BlockNode;
            Assert.That(block, Is.Not.Null);
            Assert.That(block!.Statements, Is.Not.Empty);
            var forStmt = block.Statements[0] as ForNode;
            Assert.That(forStmt, Is.Not.Null);
            Assert.That(forStmt!.Variable, Is.InstanceOf<VariableNode>());
            Assert.That(forStmt.FromExpression, Is.Not.Null);
            Assert.That(forStmt.ToExpression, Is.Not.Null);
        }

        [Test]
        public void Test_Procedure_Call()
        {
            var input = @"
                program Test;
                begin
                    writeln('Hello');
                end.";
            
            var result = ParseProgram(input);
            Assert.That(result, Is.InstanceOf<ProgramNode>());
            var program = (ProgramNode)result;
            Assert.That(program.Block.Statements, Is.Not.Empty);
            var block = program.Block.Statements[0] as BlockNode;
            Assert.That(block, Is.Not.Null);
            Assert.That(block!.Statements, Is.Not.Empty);
            var procCall = block.Statements[0] as ProcedureCallNode;
            Assert.That(procCall, Is.Not.Null);
            Assert.That(procCall!.Name, Is.EqualTo("writeln"));
            Assert.That(procCall.Arguments, Has.Count.EqualTo(1));
        }

        [Test]
        public void Test_Case_Statement()
        {
            var input = @"
                program Test;
                begin
                    case x of
                        1: writeln('One');
                        2: writeln('Two');
                        else writeln('Other');
                    end;
                end.";
            
            var result = ParseProgram(input);
            Assert.That(result, Is.InstanceOf<ProgramNode>());
            var program = (ProgramNode)result;
            Assert.That(program.Block.Statements, Is.Not.Empty);
            var block = program.Block.Statements[0] as BlockNode;
            Assert.That(block, Is.Not.Null);
            Assert.That(block!.Statements, Is.Not.Empty);
            var caseStmt = block.Statements[0] as CaseNode;
            Assert.That(caseStmt, Is.Not.Null);
            Assert.That(caseStmt!.Selector, Is.InstanceOf<VariableNode>());
            Assert.That(caseStmt.Branches, Has.Count.EqualTo(2));
            Assert.That(caseStmt.ElseBlock, Is.Not.Null);
        }

        [Test]
        public void Test_Record_Type()
        {
            var input = @"
                program Test;
                type
                    Person = record
                        name: string;
                        age: integer;
                    end;
                begin
                end.";
            
            var result = ParseProgram(input);
            var program = (ProgramNode)result;
            var type = program.Block.Statements[0] as RecordTypeNode;
            Assert.That(type, Is.Not.Null);
            Assert.That(type.Fields, Has.Count.EqualTo(2));
            Assert.That(type.Fields[0].Name, Is.EqualTo("name"));
            Assert.That(type.Fields[1].Name, Is.EqualTo("age"));
        }

        [Test]
        public void Test_Array_Type()
        {
            var input = @"
                program Test;
                type
                    IntArray = array[1..10] of integer;
                begin
                end.";
            
            var result = ParseProgram(input);
            var program = (ProgramNode)result;
            var type = program.Block.Statements[0] as ArrayTypeNode;
            Assert.That(type, Is.Not.Null);
            Assert.That(type.Dimensions, Has.Count.EqualTo(1));
            Assert.That(type.ElementType, Is.Not.Null);
            Assert.That(type.ElementType.TypeName, Is.EqualTo("integer"));
        }

        [Test]
        public void Test_With_Statement()
        {
            var input = @"
                program Test;
                var
                    p: Person;
                begin
                    with p do
                    begin
                        name := 'John';
                        age := 30;
                    end;
                end.";
            
            var result = ParseProgram(input);
            Assert.That(result, Is.InstanceOf<ProgramNode>());
            var program = (ProgramNode)result;
            Assert.That(program.Block.Statements, Is.Not.Empty);
            Assert.That(program.Block.Statements[1], Is.InstanceOf<BlockNode>());
            var block = program.Block.Statements[1] as BlockNode;
            Assert.That(block, Is.Not.Null);
            Assert.That(block!.Statements, Is.Not.Empty);
            var withStmt = block.Statements[0] as WithNode;
            Assert.That(withStmt, Is.Not.Null);
            Assert.That(withStmt!.RecordVariables, Has.Count.EqualTo(1));
            Assert.That(withStmt.Block, Is.Not.Null);
        }

        [Test]
        public void Test_Set()
        {
            var input = @"
                program Test;
                var
                    s: set of 1..10;
                begin
                    s := [1, 3..5, 7];
                end.";
            
            var result = ParseProgram(input);
            Assert.That(result, Is.InstanceOf<ProgramNode>());
            var program = (ProgramNode)result;
            Assert.That(program.Block.Statements, Is.Not.Empty);
            Assert.That(program.Block.Statements[1], Is.InstanceOf<BlockNode>());
            var block = program.Block.Statements[1] as BlockNode;
            Assert.That(block, Is.Not.Null);
            Assert.That(block!.Statements, Is.Not.Empty);
            var assignment = block.Statements[0] as AssignmentNode;
            Assert.That(assignment, Is.Not.Null);
            Assert.That(assignment!.Right, Is.InstanceOf<SetNode>());
            var setExpr = assignment.Right as SetNode;
            Assert.That(setExpr, Is.Not.Null);
            Assert.That(setExpr!.Elements, Has.Count.EqualTo(4));
        }

        [Test]
        public void Test_Complete_Program()
        {
            var input = @"
                program Calculator;
                var
                    x, y: integer;
                    result: real;
                
                function Add(a, b: integer): integer;
                begin
                    Add := a + b;
                end;
                
                begin
                    x := 5;
                    y := 3;
                    result := Add(x, y);
                    if result > 7 then
                        writeln('Large result')
                    else
                        writeln('Small result');
                end.";

            var result = ParseProgram(input);
            Assert.That(result, Is.InstanceOf<ProgramNode>());
            var program = (ProgramNode)result;
            
            Assert.That(program.Name, Is.EqualTo("Calculator"));
            Assert.That(program.Block, Is.Not.Null);
            Assert.That(program.Block.Statements, Is.Not.Empty);
            
            // Verify function declaration
            var funcDecl = program.Block.Statements[0] as FunctionNode;
            Assert.That(funcDecl, Is.Not.Null);
            Assert.That(funcDecl!.Name, Is.EqualTo("Add"));
            Assert.That(funcDecl.Parameters, Has.Count.EqualTo(2));
        }
    }
}
