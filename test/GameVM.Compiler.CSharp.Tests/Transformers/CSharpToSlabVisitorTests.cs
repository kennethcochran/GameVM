using System;
using GameVM.Compiler.Core.IR.Slab;
using GameVM.Compiler.Core.IR.Soa;
using GameVM.Compiler.Core.IR.SlabProcessing;
using GameVM.Compiler.CSharp.ANTLR;
using GameVM.Compiler.CSharp.Transformers;
using Antlr4.Runtime;
using NUnit.Framework;

namespace GameVM.Compiler.CSharp.Tests.Transformers
{
    [TestFixture]
    public class CSharpToSlabVisitorTests
    {
        private const byte VARIABLE_DECLARATION = 8;
        private const byte LITERAL_INT = 1;
        private const byte LITERAL_STRING = 2;
        private const byte LITERAL_BOOL = 3;

        private static CSharpParser.ProgramContext Parse(string code)
        {
            var inputStream = new AntlrInputStream(code);
            var lexer = new CSharpLexer(inputStream);
            var tokenStream = new CommonTokenStream(lexer);
            var parser = new CSharpParser(tokenStream);
            return parser.program();
        }

        private static InstList Visit(string code)
        {
            var context = Parse(code);
            var builder = new InstListBuilder();
            var stringPool = new StringPool();
            var visitor = new CSharpToSlabVisitor(builder, stringPool);
            visitor.Visit(context);
            return visitor.GetSlab();
        }

        private static int FindKind(InstList instList, byte kind)
        {
            for (int i = 0; i < instList.Count; i++)
            {
                if (instList.GetKind(i) == kind)
                    return i;
            }
            return -1;
        }

        [Test]
        public void Visitor_HandlesVariableDeclarationWithoutInitializer()
        {
            string code = @"
                int x;
            ";

            var instList = Visit(code);
            Assert.That(instList.Count, Is.GreaterThanOrEqualTo(1)); // At least one instruction

            // The variable declaration is the only emitted instruction.
            int declIdx = FindKind(instList, VARIABLE_DECLARATION);
            Assert.That(declIdx, Is.GreaterThanOrEqualTo(0), "Should emit a variable declaration");

            Console.WriteLine(new SlabPrinter(instList).Print());
        }

        [Test]
        public void Visitor_HandlesVariableDeclarationWithIntegerInitializer()
        {
            string code = @"
                int x = 42;
            ";

            var instList = Visit(code);

            // The literal is visited first (index 0), then the variable declaration.
            int declIdx = FindKind(instList, VARIABLE_DECLARATION);
            Assert.That(declIdx, Is.GreaterThanOrEqualTo(0), "First instruction should be variable declaration");

            // Variable declaration has 3 args: typeKind, nameOffset, initValue
            ushort argCount = instList.GetArgCount(declIdx);
            Assert.That(argCount, Is.EqualTo(3), "Variable declaration should have 3 arguments (type, name, init)");

            // First arg is type kind (int = 1); second is name pool offset (nonzero)
            uint typeKind = instList.GetOperand(declIdx, 0);
            uint nameOffset = instList.GetOperand(declIdx, 1);
            Assert.That(typeKind, Is.EqualTo(1u), "Int variable type kind should be 1");
            Assert.That(nameOffset, Is.GreaterThan(0), "Name should be interned in the string pool");

            // The literal value must be present somewhere in the list.
            int litIdx = FindKind(instList, LITERAL_INT);
            Assert.That(litIdx, Is.GreaterThanOrEqualTo(0), "Integer literal should be emitted");
            Assert.That(instList.GetOperand(litIdx, 0), Is.EqualTo(42u), "Literal value should be 42");

            Console.WriteLine(new SlabPrinter(instList).Print());
        }

        [Test]
        public void Visitor_HandlesDifferentLiteralTypes()
        {
            string[] testCases = {
                @"int x = 123;",
                @"string x = ""hello"";",
                @"bool x = true;"
            };

            var expectedKinds = new byte[] { LITERAL_INT, LITERAL_STRING, LITERAL_BOOL };

            for (int i = 0; i < testCases.Length; i++)
            {
                var code = testCases[i];
                var instList = Visit(code);
                Assert.That(instList.Count, Is.GreaterThanOrEqualTo(1),
                    $"Should produce at least one instruction for code: {code}");

                int literalIdx = FindKind(instList, expectedKinds[i]);
                Assert.That(literalIdx, Is.GreaterThanOrEqualTo(0),
                    $"Expected literal kind {expectedKinds[i]} for code: {code}");

                Console.WriteLine($"=== Code: {code} ===");
                Console.WriteLine(new SlabPrinter(instList).Print());
            }
        }
    }
}