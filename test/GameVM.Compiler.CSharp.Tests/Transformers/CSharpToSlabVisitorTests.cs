using System;
using GameVM.Compiler.Core.IR.Slab;
using GameVM.Compiler.Core.IR.SlabProcessing;
using static GameVM.Compiler.Core.IR.Slab.InstructionMetadataFlags;
using GameVM.Compiler.CSharp.ANTLR;
using GameVM.Compiler.CSharp.Transformers;
using Antlr4.Runtime;
using NUnit.Framework;

namespace GameVM.Compiler.CSharp.Tests.Transformers
{
    [TestFixture]
    public class CSharpToSlabVisitorTests
    {
        private static CSharpParser.ProgramContext Parse(string code)
        {
            var inputStream = new AntlrInputStream(code);
            var lexer = new CSharpLexer(inputStream);
            var tokenStream = new CommonTokenStream(lexer);
            var parser = new CSharpParser(tokenStream);
            return parser.program();
        }

        [Test]
        public void Visitor_HandlesVariableDeclarationWithoutInitializer()
        {
            string code = @"
                int x;
            ";

            var context = Parse(code);
            var arena = new ArenaAllocator();
            var visitor = new CSharpToSlabVisitor(arena);
            visitor.Visit(context);

            uint[] slab = visitor.GetSlab();
            Assert.That(slab.Length, Is.GreaterThan(SlabHeader.HeaderIndex.Length)); // Header + at least one instruction

            // Verify header is valid
            var header = SlabHeader.Read(slab);
            Assert.That(header.HasValidMagic(), Is.True);
            Assert.That(header.IrStage, Is.EqualTo(1u)); // HLIR

            // Print slab for debugging (optional)
            Console.WriteLine(new SlabPrinter(slab).Print());
        }

        [Test]
        public void Visitor_HandlesVariableDeclarationWithIntegerInitializer()
        {
            string code = @"
                int x = 42;
            ";

            var context = Parse(code);
            var arena = new ArenaAllocator();
            var visitor = new CSharpToSlabVisitor(arena);
            visitor.Visit(context);

            uint[] slab = visitor.GetSlab();
            Assert.That(slab.Length, Is.GreaterThan(SlabHeader.HeaderIndex.Length));

            // Verify header is valid
            var header = SlabHeader.Read(slab);
            Assert.That(header.HasValidMagic(), Is.True);

            // Check that we have the expected instruction structure
            // First instruction after header should be VARIABLE_DECLARATION (kind 8)
            int headerLength = SlabHeader.HeaderIndex.Length;
            uint varDeclMetadata = slab[headerLength];
            byte kind = InstructionMetadata.DecodeKind(varDeclMetadata);
            Assert.That(kind, Is.EqualTo(VARIABLE_DECLARATION), "First instruction should be variable declaration");
            
            byte argCount = InstructionMetadata.DecodeArgCount(varDeclMetadata);
            Assert.That(argCount, Is.EqualTo(2), "Variable declaration should have 2 arguments (type, name)");
            
            // Second argument should be the offset to the initializer expression
            uint varNameHash = slab[headerLength + 1]; // First arg is type kind
            uint initExprOffset = slab[headerLength + 2]; // Second arg is init expr offset
            
            Assert.That(initExprOffset, Is.GreaterThan((uint)headerLength), 
                "Initializer offset should point past the header");
                
            Console.WriteLine(new SlabPrinter(slab).Print());
        }

        [Test]
        public void Visitor_HandlesDifferentLiteralTypes()
        {
            string[] testCases = {
                @"int x = 123;",
                @"string x = ""hello"";",
                @"bool x = true;"
            };

            foreach (var code in testCases)
            {
                var context = Parse(code);
                var arena = new ArenaAllocator();
                var visitor = new CSharpToSlabVisitor(arena);
                visitor.Visit(context);

                uint[] slab = visitor.GetSlab();
                Assert.That(slab.Length, Is.GreaterThan(SlabHeader.HeaderIndex.Length));

                var header = SlabHeader.Read(slab);
                Assert.That(header.HasValidMagic(), Is.True, 
                    $"Header should be valid for code: {code}");
                    
                Console.WriteLine($"=== Code: {code} ===");
                Console.WriteLine(new SlabPrinter(slab).Print());
            }
        }
    }
}