using GameVM.Compiler.Core.IR.Ast;
using GameVM.Compiler.Core.IR.Soa;
using GameVM.Compiler.Pascal.ANTLR;
using GameVM.Compiler.Pascal.Transformers;
using GameVM.Compiler.Core.IR.Buffers;
using Antlr4.Runtime;

namespace GameVM.Compiler.Pascal.Tests.Transformers
{
    [TestFixture]
    public class PascalToAstVisitorTests
    {
        private static PascalParser.ProgramContext Parse(string code)
        {
            var inputStream = new AntlrInputStream(code);
            var lexer = new PascalLexer(inputStream);
            var tokenStream = new CommonTokenStream(lexer);
            var parser = new PascalParser(tokenStream);
            return parser.program();
        }

        [Test]
        public void Visitor_HandlesSimpleProgram()
        {
            string code = @"
                program Test;
                begin
                end.
            ";

            var context = Parse(code);
            var builder = new AstBuilder();
            var visitor = new PascalToAstVisitor(builder, new StringPool());
            visitor.Visit(context);

            AstTree tree = visitor.GetTree();
            Assert.That(tree.Count, Is.GreaterThan(0));

            Console.WriteLine($"Node count: {tree.Count}");
            for (int i = 0; i < tree.Count; i++)
            {
                var node = tree[i];
                var children = tree.Children(i);
                Console.WriteLine($"  [{i}] Kind: {(PascalAstNodeKind)node.Kind}, Flags: {node.Flags}, Payload: {node.Payload}, FirstChild: {node.FirstChild}, ChildCount: {node.ChildCount}");
                if (children.Length > 0)
                {
                    Console.Write("    Children: ");
                    for (int c = 0; c < children.Length; c++)
                    {
                        Console.Write($"{(PascalAstNodeKind)children[c].Kind} ");
                    }
                    Console.WriteLine();
                }
            }
        }

        [Test]
        public void Visitor_HandlesVariableDeclaration()
        {
            string code = @"
                program Test;
                var x: integer;
                begin
                end.
            ";

            var context = Parse(code);
            var builder = new AstBuilder();
            var visitor = new PascalToAstVisitor(builder, new StringPool());
            visitor.Visit(context);

            AstTree tree = visitor.GetTree();
            Assert.That(tree.Count, Is.GreaterThan(0));

            // Check for METHOD_DECLARATION (kind 10)
            bool foundMethodDecl = false;
            for (int i = 0; i < tree.Count; i++)
            {
                if (tree.GetKind(i) == (byte)PascalAstNodeKind.MethodDeclaration)
                {
                    foundMethodDecl = true;
                    break;
                }
            }
            Assert.That(foundMethodDecl, Is.True, "First instruction should be method declaration");

            // Check for VARIABLE_DECLARATION (kind 8)
            bool foundVarDecl = false;
            for (int i = 0; i < tree.Count; i++)
            {
                if (tree.GetKind(i) == (byte)PascalAstNodeKind.VariableDeclaration)
                {
                    foundVarDecl = true;
                    var node = tree[i];
                    Assert.That(node.ChildCount, Is.EqualTo(1), "Variable declaration should have 1 child (identifier)");
                    break;
                }
            }
            Assert.That(foundVarDecl, Is.True, "Variable declaration should be present in the method body");

            Console.WriteLine($"Node count: {tree.Count}");
            for (int i = 0; i < tree.Count; i++)
            {
                var node = tree[i];
                var children = tree.Children(i);
                Console.WriteLine($"  [{i}] Kind: {(PascalAstNodeKind)node.Kind}, Flags: {node.Flags}, Payload: {node.Payload}, Children: {children.Length}");
            }
        }

        [Test]
        public void Visitor_HandlesAssignmentStatement()
        {
            string code = @"
                program Test;
                var x: integer;
                begin
                    x := 42;
                end.
            ";

            var context = Parse(code);
            var builder = new AstBuilder();
            var visitor = new PascalToAstVisitor(builder, new StringPool());
            visitor.Visit(context);

            AstTree tree = visitor.GetTree();
            Assert.That(tree.Count, Is.GreaterThan(0));

            // Check for ASSIGNMENT (kind 7)
            bool foundAssignment = false;
            for (int i = 0; i < tree.Count; i++)
            {
                if (tree.GetKind(i) == (byte)PascalAstNodeKind.Assignment)
                {
                    foundAssignment = true;
                    var node = tree[i];
                    Assert.That(node.ChildCount, Is.EqualTo(2), "Assignment should have 2 children (target, value)");
                    break;
                }
            }
            Assert.That(foundAssignment, Is.True, "Assignment should be present in the method body");

            Console.WriteLine($"Node count: {tree.Count}");
            for (int i = 0; i < tree.Count; i++)
            {
                var node = tree[i];
                var children = tree.Children(i);
                Console.WriteLine($"  [{i}] Kind: {(PascalAstNodeKind)node.Kind}, Flags: {node.Flags}, Payload: {node.Payload}, Children: {children.Length}");
            }
        }

        [Test]
        public void Visitor_HandlesIfStatement()
        {
            string code = @"
                program Test;
                var x: integer;
                begin
                    if x > 0 then
                        x := 1
                    else
                        x := -1;
                end.
            ";

            var context = Parse(code);
            var builder = new AstBuilder();
            var visitor = new PascalToAstVisitor(builder, new StringPool());
            visitor.Visit(context);

            AstTree tree = visitor.GetTree();
            Assert.That(tree.Count, Is.GreaterThan(0));

            // Check for IF_STATEMENT (kind 12)
            bool foundIf = false;
            for (int i = 0; i < tree.Count; i++)
            {
                if (tree.GetKind(i) == (byte)PascalAstNodeKind.IfStatement)
                {
                    foundIf = true;
                    var node = tree[i];
                    Assert.That(node.ChildCount >= 2, "If statement should have at least 2 children (condition, then)");
                    break;
                }
            }
            Assert.That(foundIf, Is.True, "If statement should be present in the method body");

            Console.WriteLine($"Node count: {tree.Count}");
            for (int i = 0; i < tree.Count; i++)
            {
                var node = tree[i];
                var children = tree.Children(i);
                Console.WriteLine($"  [{i}] Kind: {(PascalAstNodeKind)node.Kind}, Flags: {node.Flags}, Payload: {node.Payload}, Children: {children.Length}");
            }
        }

        [Test]
        public void Visitor_HandlesWhileLoop()
        {
            string code = @"
                program Test;
                var x: integer;
                begin
                    while x > 0 do
                        x := x - 1;
                end.
            ";

            var context = Parse(code);
            var builder = new AstBuilder();
            var visitor = new PascalToAstVisitor(builder, new StringPool());
            visitor.Visit(context);

            AstTree tree = visitor.GetTree();
            Assert.That(tree.Count, Is.GreaterThan(0));

            // Check for WHILE_STATEMENT (kind 13)
            bool foundWhile = false;
            for (int i = 0; i < tree.Count; i++)
            {
                if (tree.GetKind(i) == (byte)PascalAstNodeKind.WhileStatement)
                {
                    foundWhile = true;
                    var node = tree[i];
                    Assert.That(node.ChildCount == 2, "While statement should have 2 children (condition, body)");
                    break;
                }
            }
            Assert.That(foundWhile, Is.True, "While statement should be present in the method body");

            Console.WriteLine($"Node count: {tree.Count}");
            for (int i = 0; i < tree.Count; i++)
            {
                var node = tree[i];
                var children = tree.Children(i);
                Console.WriteLine($"  [{i}] Kind: {(PascalAstNodeKind)node.Kind}, Flags: {node.Flags}, Payload: {node.Payload}, Children: {children.Length}");
            }
        }

        [Test]
        public void Visitor_HandlesLiteralTypes()
        {
            string code = @"
                program Test;
                var x: integer;
                var s: string;
                var b: boolean;
                begin
                    x := 42;
                    s := 'hello';
                    b := true;
                end.
            ";

            var context = Parse(code);
            var builder = new AstBuilder();
            var visitor = new PascalToAstVisitor(builder, new StringPool());
            visitor.Visit(context);

            AstTree tree = visitor.GetTree();
            Assert.That(tree.Count, Is.GreaterThan(0));

            // Check for literal types
            bool foundIntLiteral = false;
            bool foundStringLiteral = false;
            bool foundBoolLiteral = false;

            for (int i = 0; i < tree.Count; i++)
            {
                var kind = (PascalAstNodeKind)tree.GetKind(i);
                if (kind == PascalAstNodeKind.LiteralInt)
                    foundIntLiteral = true;
                else if (kind == PascalAstNodeKind.LiteralString)
                    foundStringLiteral = true;
                else if (kind == PascalAstNodeKind.LiteralBool)
                    foundBoolLiteral = true;
            }

            Assert.That(foundIntLiteral, Is.True, "Integer literal should be present");
            Assert.That(foundStringLiteral, Is.True, "String literal should be present");
            Assert.That(foundBoolLiteral, Is.True, "Boolean literal should be present");

            Console.WriteLine($"Node count: {tree.Count}");
            for (int i = 0; i < tree.Count; i++)
            {
                var node = tree[i];
                var children = tree.Children(i);
                Console.WriteLine($"  [{i}] Kind: {(PascalAstNodeKind)node.Kind}, Flags: {node.Flags}, Payload: {node.Payload}, Children: {children.Length}");
            }
        }
    }
}