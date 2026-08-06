using GameVM.Compiler.Core.IR;
using GameVM.Compiler.Core.IR.Soa;
using GameVM.Compiler.Pascal.ANTLR;
using GameVM.Compiler.Pascal.Transformers;
using GameVM.Compiler.Core.IR.Buffers;
using Antlr4.Runtime;

namespace GameVM.Compiler.Pascal.Tests.Transformers
{
    [TestFixture]
    public class PascalToSlabVisitorTests
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
            var builder = new InstListBuilder();
            var visitor = new PascalToSlabVisitor(builder, new StringPool());
            visitor.Visit(context);

            InstList slab = visitor.GetSlab();
            Assert.That(slab.Count, Is.GreaterThan(0));

            // Verify we have at least some instructions
            Assert.That(slab.Tags.Length, Is.GreaterThan(0));
            
            Console.WriteLine($"Instruction count: {slab.Count}");
            for (int i = 0; i < slab.Count; i++)
            {
                Console.WriteLine($"  [{i}] Kind: {(AstNodeKind)slab.GetKind(i)}, Args: {slab.GetArgCount(i)}, Operands: {string.Join(", ", slab.GetOperands(i).ToArray())}");
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
            var builder = new InstListBuilder();
            var visitor = new PascalToSlabVisitor(builder, new StringPool());
            visitor.Visit(context);

            InstList slab = visitor.GetSlab();
            Assert.That(slab.Count, Is.GreaterThan(0));

            // Check for METHOD_DECLARATION (kind 10)
            bool foundMethodDecl = false;
            for (int i = 0; i < slab.Count; i++)
            {
                if (slab.GetKind(i) == (byte)AstNodeKind.MethodDeclaration)
                {
                    foundMethodDecl = true;
                    break;
                }
            }
            Assert.That(foundMethodDecl, Is.True, "First instruction should be method declaration");

            // Check for VARIABLE_DECLARATION (kind 8)
            bool foundVarDecl = false;
            for (int i = 0; i < slab.Count; i++)
            {
                if (slab.GetKind(i) == (byte)AstNodeKind.VariableDeclaration)
                {
                    foundVarDecl = true;
                    ushort argCount = slab.GetArgCount(i);
                    Assert.That(argCount, Is.EqualTo(2), "Variable declaration should have 2 arguments (type, name)");
                    break;
                }
            }
            Assert.That(foundVarDecl, Is.True, "Variable declaration should be present in the method body");

            Console.WriteLine($"Instruction count: {slab.Count}");
            for (int i = 0; i < slab.Count; i++)
            {
                Console.WriteLine($"  [{i}] Kind: {(AstNodeKind)slab.GetKind(i)}, Args: {slab.GetArgCount(i)}, Operands: {string.Join(", ", slab.GetOperands(i).ToArray())}");
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
            var builder = new InstListBuilder();
            var visitor = new PascalToSlabVisitor(builder, new StringPool());
            visitor.Visit(context);

            InstList slab = visitor.GetSlab();
            Assert.That(slab.Count, Is.GreaterThan(0));

            // Check for ASSIGNMENT (kind 7)
            bool foundAssignment = false;
            for (int i = 0; i < slab.Count; i++)
            {
                if (slab.GetKind(i) == (byte)AstNodeKind.Assignment)
                {
                    foundAssignment = true;
                    ushort argCount = slab.GetArgCount(i);
                    Assert.That(argCount, Is.EqualTo(2), "Assignment should have 2 arguments (target, value)");
                    break;
                }
            }
            Assert.That(foundAssignment, Is.True, "Assignment should be present in the method body");

            Console.WriteLine($"Instruction count: {slab.Count}");
            for (int i = 0; i < slab.Count; i++)
            {
                Console.WriteLine($"  [{i}] Kind: {(AstNodeKind)slab.GetKind(i)}, Args: {slab.GetArgCount(i)}, Operands: {string.Join(", ", slab.GetOperands(i).ToArray())}");
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
                        x := 0;
                end.
            ";

            var context = Parse(code);
            var builder = new InstListBuilder();
            var visitor = new PascalToSlabVisitor(builder, new StringPool());
            visitor.Visit(context);

            InstList slab = visitor.GetSlab();
            Assert.That(slab.Count, Is.GreaterThan(0));

            // Check for IF_STATEMENT (kind 12)
            bool foundIf = false;
            for (int i = 0; i < slab.Count; i++)
            {
                if (slab.GetKind(i) == (byte)AstNodeKind.IfStatement)
                {
                    foundIf = true;
                    ushort argCount = slab.GetArgCount(i);
                    // IfStatement has condition, then, and optional else
                    Assert.That(argCount >= 2, Is.True, "IfStatement should have at least 2 arguments (condition, then)");
                    break;
                }
            }
            Assert.That(foundIf, Is.True, "IfStatement should be present in the method body");

            Console.WriteLine($"Instruction count: {slab.Count}");
            for (int i = 0; i < slab.Count; i++)
            {
                Console.WriteLine($"  [{i}] Kind: {(AstNodeKind)slab.GetKind(i)}, Args: {slab.GetArgCount(i)}, Operands: {string.Join(", ", slab.GetOperands(i).ToArray())}");
            }
        }

        [Test]
        public void Visitor_HandlesWhileLoop()
        {
            string code = @"
                program Test;
                var x: integer;
                begin
                    while x < 10 do
                        x := x + 1;
                end.
            ";

            var context = Parse(code);
            var builder = new InstListBuilder();
            var visitor = new PascalToSlabVisitor(builder, new StringPool());
            visitor.Visit(context);

            InstList slab = visitor.GetSlab();
            Assert.That(slab.Count, Is.GreaterThan(0));

            // Check for WHILE_STATEMENT (kind 13)
            bool foundWhile = false;
            for (int i = 0; i < slab.Count; i++)
            {
                if (slab.GetKind(i) == (byte)AstNodeKind.WhileStatement)
                {
                    foundWhile = true;
                    ushort argCount = slab.GetArgCount(i);
                    Assert.That(argCount, Is.EqualTo(2), "WhileStatement should have 2 arguments (condition, body)");
                    break;
                }
            }
            Assert.That(foundWhile, Is.True, "WhileStatement should be present in the method body");

            Console.WriteLine($"Instruction count: {slab.Count}");
            for (int i = 0; i < slab.Count; i++)
            {
                Console.WriteLine($"  [{i}] Kind: {(AstNodeKind)slab.GetKind(i)}, Args: {slab.GetArgCount(i)}, Operands: {string.Join(", ", slab.GetOperands(i).ToArray())}");
            }
        }

        [Test]
        public void Visitor_HandlesLiteralTypes()
        {
            string[] testCases = {
                @"program Test; begin x := 123; end.",
                @"program Test; begin x := 'hello'; end.",
                @"program Test; begin x := true; end."
            };

            foreach (var code in testCases)
            {
                var context = Parse(code);
                var builder = new InstListBuilder();
                var visitor = new PascalToSlabVisitor(builder, new StringPool());
                visitor.Visit(context);

                InstList slab = visitor.GetSlab();
                Assert.That(slab.Count, Is.GreaterThan(0), $"Instruction count should be > 0 for code: {code}");

                // Check for LITERAL types
                bool foundLiteral = false;
                for (int i = 0; i < slab.Count; i++)
                {
                    var kind = (AstNodeKind)slab.GetKind(i);
                    if (kind == AstNodeKind.LiteralInt || kind == AstNodeKind.LiteralString || kind == AstNodeKind.LiteralBool)
                    {
                        foundLiteral = true;
                        break;
                    }
                }
                Assert.That(foundLiteral, Is.True, $"Should find a literal instruction for code: {code}");
                    
                Console.WriteLine($"=== Code: {code} ===");
                Console.WriteLine($"Instruction count: {slab.Count}");
                for (int i = 0; i < slab.Count; i++)
                {
                    Console.WriteLine($"  [{i}] Kind: {(AstNodeKind)slab.GetKind(i)}, Args: {slab.GetArgCount(i)}, Operands: {string.Join(", ", slab.GetOperands(i).ToArray())}");
                }
            }
        }
    }
}