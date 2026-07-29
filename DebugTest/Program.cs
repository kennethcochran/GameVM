using System;
using GameVM.Compiler.Core.IR.Slab;
using GameVM.Compiler.Core.IR.SlabProcessing;
using static GameVM.Compiler.Core.IR.Slab.InstructionMetadataFlags;
using GameVM.Compiler.Pascal.ANTLR;
using GameVM.Compiler.Pascal.Transformers;
using Antlr4.Runtime;

namespace DebugTest
{
    class Program
    {
        static void Main(string[] args)
        {
            string code = @"
                program Test;
                var x: integer;
                begin
                    x := 42;
                end.
            ";

            var inputStream = new AntlrInputStream(code);
            var lexer = new PascalLexer(inputStream);
            var tokenStream = new CommonTokenStream(lexer);
            var parser = new PascalParser(tokenStream);
            var context = parser.program();

            var arena = new ArenaAllocator();
            var visitor = new PascalToSlabVisitor(arena);
            visitor.Visit(context);

            uint[] slab = visitor.GetSlab();
            Console.WriteLine($"Slab length: {slab.Length}");
            Console.WriteLine($"Header length: {SlabHeader.HeaderIndex.Length}");
            
            var header = SlabHeader.Read(slab);
            Console.WriteLine($"Header magic: {header.MagicNumber:X8} (expected: {SlabHeader.Magic:X8})");
            Console.WriteLine($"Header valid: {header.HasValidMagic()}");
            Console.WriteLine($"IR Stage: {header.IrStage}");
            Console.WriteLine($"Element count: {header.ElementCount}");
            
            Console.WriteLine("\nInstructions:");
            int offset = SlabHeader.HeaderIndex.Length;
            int index = 0;
            while (offset < slab.Length)
            {
                uint metadata = slab[offset];
                byte kind = InstructionMetadata.DecodeKind(metadata);
                byte size = InstructionMetadata.DecodeSize(metadata);
                byte argCount = InstructionMetadata.DecodeArgCount(metadata);
                bool isTerminator = (metadata & InstructionMetadata.TerminatorMask) != 0;
                
                Console.WriteLine($"  [{index}] Offset {offset}: Kind={kind} (0x{kind:X2}), Size={size}, Args={argCount}, Terminator={isTerminator}");
                
                if (size == 0) break;
                offset += (int)size;
                index++;
            }
            
            Console.WriteLine("\n" + new SlabPrinter(slab).Print());
        }
    }
}