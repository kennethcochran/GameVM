using System;
using GameVM.Compiler.Core.IR.Slab;
using GameVM.Compiler.Core.IR.SlabProcessing;
using static GameVM.Compiler.Core.IR.Slab.InstructionMetadataFlags;
using GameVM.Compiler.Pascal.ANTLR;
using GameVM.Compiler.Pascal.Transformers;
using Antlr4.Runtime;
using NUnit.Framework;

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
            var arena = new ArenaAllocator();
            var visitor = new PascalToSlabVisitor(arena);
            visitor.Visit(context);

            uint[] slab = visitor.GetSlab();
            Assert.That(slab.Length, Is.GreaterThan(SlabHeader.HeaderIndex.Length)); // Header + at least one instruction

            // Verify header is valid
            var header = SlabHeader.Read(slab);
            Assert.That(header.HasValidMagic(), Is.True);
            Assert.That(header.IrStage, Is.EqualTo(1u)); // HLIR
            
            Console.WriteLine(new SlabPrinter(slab).Print());
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
            var arena = new ArenaAllocator();
            var visitor = new PascalToSlabVisitor(arena);
            visitor.Visit(context);

            uint[] slab = visitor.GetSlab();
            Assert.That(slab.Length, Is.GreaterThan(SlabHeader.HeaderIndex.Length));

            var header = SlabHeader.Read(slab);
            Assert.That(header.HasValidMagic(), Is.True);

            // First instruction after header should be VARIABLE_DECLARATION (kind 8)
            int headerLength = SlabHeader.HeaderIndex.Length;
            uint varDeclMetadata = slab[headerLength];
            byte kind = InstructionMetadata.DecodeKind(varDeclMetadata);
            Assert.That(kind, Is.EqualTo(VARIABLE_DECLARATION), "First instruction should be variable declaration");
            
            byte argCount = InstructionMetadata.DecodeArgCount(varDeclMetadata);
            Assert.That(argCount, Is.EqualTo(2), "Variable declaration should have 2 arguments (type, name)");
            
            Console.WriteLine(new SlabPrinter(slab).Print());
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
            var arena = new ArenaAllocator();
            var visitor = new PascalToSlabVisitor(arena);
            visitor.Visit(context);

            uint[] slab = visitor.GetSlab();
            Assert.That(slab.Length, Is.GreaterThan(SlabHeader.HeaderIndex.Length));

            var header = SlabHeader.Read(slab);
            Assert.That(header.HasValidMagic(), Is.True);
            
            Console.WriteLine(new SlabPrinter(slab).Print());
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
            var arena = new ArenaAllocator();
            var visitor = new PascalToSlabVisitor(arena);
            visitor.Visit(context);

            uint[] slab = visitor.GetSlab();
            Assert.That(slab.Length, Is.GreaterThan(SlabHeader.HeaderIndex.Length));

            var header = SlabHeader.Read(slab);
            Assert.That(header.HasValidMagic(), Is.True);
            
            Console.WriteLine(new SlabPrinter(slab).Print());
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
            var arena = new ArenaAllocator();
            var visitor = new PascalToSlabVisitor(arena);
            visitor.Visit(context);

            uint[] slab = visitor.GetSlab();
            Assert.That(slab.Length, Is.GreaterThan(SlabHeader.HeaderIndex.Length));

            var header = SlabHeader.Read(slab);
            Assert.That(header.HasValidMagic(), Is.True);
            
            Console.WriteLine(new SlabPrinter(slab).Print());
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
                var arena = new ArenaAllocator();
                var visitor = new PascalToSlabVisitor(arena);
                visitor.Visit(context);

                uint[] slab = visitor.GetSlab();
                Assert.That(slab.Length, Is.GreaterThan(SlabHeader.HeaderIndex.Length));

                var header = SlabHeader.Read(slab);
                Assert.That(header.HasValidMagic(), Is.True, $"Header should be valid for code: {code}");
                    
                Console.WriteLine($"=== Code: {code} ===");
                Console.WriteLine(new SlabPrinter(slab).Print());
            }
        }
    }
}