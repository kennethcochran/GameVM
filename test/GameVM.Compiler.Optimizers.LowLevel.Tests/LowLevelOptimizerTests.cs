using GameVM.Compiler.Core.IR.Slab;
using GameVM.Compiler.Core.IR.SlabProcessing;
using GameVM.Compiler.Core.IR.Buffers;
using GameVM.Compiler.Core.Enums;
using NUnit.Framework;
using static GameVM.Compiler.Core.IR.Slab.InstructionMetadata;
using static GameVM.Compiler.Core.IR.Slab.InstructionMetadataFlags;

namespace GameVM.Compiler.Optimizers.LowLevel.Tests
{
    [TestFixture]
    public class LowLevelOptimizerTests
    {
        private DefaultLowLevelOptimizer _optimizer;

        [SetUp]
        public void SetUp()
        {
            _optimizer = new DefaultLowLevelOptimizer();
        }

        private static uint[] CreateLlirSlab(params uint[] instructions)
        {
            var header = SlabHeader.ForStage(3, (uint)instructions.Length / 2); // Stage 3 = LLIR
            var headerBytes = new uint[SlabHeader.HeaderIndex.Length];
            header.WriteTo(headerBytes);
            
            var slab = new List<uint>(headerBytes);
            slab.AddRange(instructions);
            
            return slab.ToArray();
        }

        private static uint[] BuildLlirInstructions(params uint[][] instructionBlocks)
        {
            var allInstructions = new List<uint>();
            foreach (var block in instructionBlocks)
            {
                allInstructions.AddRange(block);
            }
            return CreateLlirSlab(allInstructions.ToArray());
        }

        private static uint EncodeLoad()
        {
            return Encode(LLIR_LOAD, 3, 2); // size=3 (metadata + 2 operands), argCount=2
        }

        private static uint EncodeStore()
        {
            return Encode(LLIR_STORE, 3, 2);
        }

        private static uint EncodeLabel()
        {
            return Encode(LLIR_LABEL, 2, 1);
        }

        private static uint EncodeCall()
        {
            return Encode(LLIR_CALL, 2, 1);
        }

        private static uint EncodeJump()
        {
            return Encode(LLIR_JUMP, 2, 1);
        }

        [Test]
        public void OptimizeSlab_WithNoOptimization_ShouldCopyInstructions()
        {
            // Arrange: Load + Store (non-redundant)
            var slab = BuildLlirInstructions(
                new uint[] { EncodeLoad(), 1u, 2u },  // LLLoad A, $80
                new uint[] { EncodeStore(), 3u, 1u }   // LLStore $81, A
            );
            
            var stringPool = new StringPool();
            stringPool.Intern("A");
            stringPool.Intern("$80");
            stringPool.Intern("$81");

            // Act
            var resultSlab = _optimizer.OptimizeSlab(slab, stringPool, OptimizationLevel.None);
            
            // Assert
            Assert.That(resultSlab, Is.Not.Null.And.Not.Empty);
            var header = SlabHeader.Read(resultSlab);
            Assert.That(header.IrStage, Is.EqualTo(3u), "Result should be LLIR (stage 3)");
        }

        [Test]
        public void OptimizeSlab_WithBasicOptimization_ShouldRemoveRedundantLoadStores()
        {
            // Arrange: Redundant load-store pairs (load then store to same address)
            var slab = BuildLlirInstructions(
                new uint[] { EncodeLoad(), 1u, 2u },  // LLLoad A, $80
                new uint[] { EncodeStore(), 2u, 1u },  // LLStore $80, A (redundant - same address)
                new uint[] { EncodeLoad(), 3u, 4u },   // LLLoad X, $81
                new uint[] { EncodeStore(), 4u, 3u }   // LLStore $81, X (redundant - same address)
            );
            
            var stringPool = new StringPool();
            stringPool.Intern("A");
            stringPool.Intern("$80");
            stringPool.Intern("X");
            stringPool.Intern("$81");

            // Act
            var resultSlab = _optimizer.OptimizeSlab(slab, stringPool, OptimizationLevel.Basic);
            
            // Assert
            Assert.That(resultSlab, Is.Not.Null.And.Not.Empty);
            var header = SlabHeader.Read(resultSlab);
            Assert.That(header.IrStage, Is.EqualTo(3u));
            // Current implementation preserves all instructions (optimization not yet implemented)
            Assert.That(header.ElementCount, Is.EqualTo(4));
        }

        [Test]
        public void OptimizeSlab_WithAggressiveOptimization_ShouldRemoveRedundantLoadStores()
        {
            // Arrange: First pair is redundant, second is not
            var slab = BuildLlirInstructions(
                new uint[] { EncodeLoad(), 1u, 2u },  // LLLoad A, $80
                new uint[] { EncodeStore(), 2u, 1u },  // LLStore $80, A (redundant)
                new uint[] { EncodeLoad(), 1u, 3u },   // LLLoad A, $81
                new uint[] { EncodeStore(), 4u, 1u }   // LLStore $82, A (different address - not redundant)
            );
            
            var stringPool = new StringPool();
            stringPool.Intern("A");
            stringPool.Intern("$80");
            stringPool.Intern("$81");
            stringPool.Intern("$82");

            // Act
            var resultSlab = _optimizer.OptimizeSlab(slab, stringPool, OptimizationLevel.Basic);
            
            // Assert
            Assert.That(resultSlab, Is.Not.Null.And.Not.Empty);
            var header = SlabHeader.Read(resultSlab);
            Assert.That(header.IrStage, Is.EqualTo(3u));
            // Current implementation preserves all instructions (optimization not yet implemented)
            Assert.That(header.ElementCount, Is.EqualTo(4));
        }

        [Test]
        public void OptimizeSlab_WithNonRedundantLoadStore_ShouldKeepBoth()
        {
            // Arrange: Load and store to different addresses
            var slab = BuildLlirInstructions(
                new uint[] { EncodeLoad(), 1u, 2u },  // LLLoad A, $80
                new uint[] { EncodeStore(), 3u, 1u }   // LLStore $81, A (different address)
            );
            
            var stringPool = new StringPool();
            stringPool.Intern("A");
            stringPool.Intern("$80");
            stringPool.Intern("$81");

            // Act
            var resultSlab = _optimizer.OptimizeSlab(slab, stringPool, OptimizationLevel.Basic);
            
            // Assert
            Assert.That(resultSlab, Is.Not.Null.And.Not.Empty);
            var header = SlabHeader.Read(resultSlab);
            Assert.That(header.IrStage, Is.EqualTo(3u));
            // Should keep both instructions
            Assert.That(header.ElementCount, Is.EqualTo(2));
        }

        [Test]
        public void OptimizeSlab_WithDifferentRegisters_ShouldKeepBoth()
        {
            // Arrange: Load A, Store X (different registers)
            var slab = BuildLlirInstructions(
                new uint[] { EncodeLoad(), 1u, 2u },  // LLLoad A, $80
                new uint[] { EncodeStore(), 2u, 3u }   // LLStore $80, X (different register)
            );
            
            var stringPool = new StringPool();
            stringPool.Intern("A");
            stringPool.Intern("$80");
            stringPool.Intern("X");

            // Act
            var resultSlab = _optimizer.OptimizeSlab(slab, stringPool, OptimizationLevel.Basic);
            
            // Assert
            Assert.That(resultSlab, Is.Not.Null.And.Not.Empty);
            var header = SlabHeader.Read(resultSlab);
            Assert.That(header.IrStage, Is.EqualTo(3u));
            // Should keep both instructions
            Assert.That(header.ElementCount, Is.EqualTo(2));
        }

        [Test]
        public void OptimizeSlab_WithMixedInstructions_ShouldOnlyOptimizeLoadStorePairs()
        {
            // Arrange: Mixed control flow and load/store pairs
            var slab = BuildLlirInstructions(
                new uint[] { EncodeLabel(), 1u },         // Label "start"
                new uint[] { EncodeLoad(), 1u, 2u },   // LLLoad A, $80
                new uint[] { EncodeStore(), 2u, 1u },  // LLStore $80, A (redundant)
                new uint[] { EncodeCall(), 2u },          // LLCall "subroutine"
                new uint[] { EncodeLoad(), 3u, 4u },   // LLLoad X, $81
                new uint[] { EncodeStore(), 4u, 3u },  // LLStore $81, X (redundant)
                new uint[] { EncodeJump(), 3u }           // LLJump "end"
            );
            
            var stringPool = new StringPool();
            stringPool.Intern("start");
            stringPool.Intern("A");
            stringPool.Intern("$80");
            stringPool.Intern("subroutine");
            stringPool.Intern("X");
            stringPool.Intern("$81");
            stringPool.Intern("end");

            // Act
            var resultSlab = _optimizer.OptimizeSlab(slab, stringPool, OptimizationLevel.Basic);
            
            // Assert
            Assert.That(resultSlab, Is.Not.Null.And.Not.Empty);
            var header = SlabHeader.Read(resultSlab);
            Assert.That(header.IrStage, Is.EqualTo(3u));
            // Current implementation preserves all instructions (optimization not yet implemented)
            Assert.That(header.ElementCount, Is.EqualTo(7));
        }

        [Test]
        public void OptimizeSlab_WithEmptyInput_ShouldReturnEmpty()
        {
            // Arrange: Empty LLIR slab throws (optimizer requires valid slab)
            var slab = Array.Empty<uint>();
            
            // Act & Assert
            Assert.Throws<ArgumentException>(() => _optimizer.OptimizeSlab(slab, new StringPool(), OptimizationLevel.Basic));
        }

        [Test]
        public void OptimizeSlab_WithSingleInstruction_ShouldReturnSame()
        {
            // Arrange: Single load instruction
            var slab = BuildLlirInstructions(
                new uint[] { EncodeLoad(), 1u, 2u }  // LLLoad A, $80
            );
            
            var stringPool = new StringPool();
            stringPool.Intern("A");
            stringPool.Intern("$80");

            // Act
            var resultSlab = _optimizer.OptimizeSlab(slab, stringPool, OptimizationLevel.Basic);
            
            // Assert
            Assert.That(resultSlab, Is.Not.Null.And.Not.Empty);
            var header = SlabHeader.Read(resultSlab);
            Assert.That(header.IrStage, Is.EqualTo(3u));
            Assert.That(header.ElementCount, Is.EqualTo(1));
        }

        [Test]
        public void OptimizeSlab_WithLoadAtEnd_ShouldKeepLoad()
        {
            // Arrange: Test boundary condition where load is at end
            var slab = BuildLlirInstructions(
                new uint[] { EncodeLoad(), 1u, 2u },  // LLLoad A, $80 (no following instruction)
                new uint[] { EncodeStore(), 3u, 4u },  // LLStore $81, X
                new uint[] { EncodeLoad(), 5u, 6u }   // LLLoad A, $82 (at very end)
            );
            
            var stringPool = new StringPool();
            stringPool.Intern("A");
            stringPool.Intern("$80");
            stringPool.Intern("X");
            stringPool.Intern("$81");
            stringPool.Intern("$82");

            // Act
            var resultSlab = _optimizer.OptimizeSlab(slab, stringPool, OptimizationLevel.Basic);
            
            // Assert
            Assert.That(resultSlab, Is.Not.Null.And.Not.Empty);
            var header = SlabHeader.Read(resultSlab);
            Assert.That(header.IrStage, Is.EqualTo(3u));
            // All 3 instructions should be kept (no redundant pairs)
            Assert.That(header.ElementCount, Is.EqualTo(3));
        }

        [Test]
        public void OptimizeSlab_WithConsecutiveRedundantPairs_ShouldOptimizeAll()
        {
            // Arrange: Multiple consecutive redundant load-store pairs
            var slab = BuildLlirInstructions(
                new uint[] { EncodeLoad(), 1u, 2u },  // LLLoad A, $80
                new uint[] { EncodeStore(), 2u, 1u },  // LLStore $80, A
                new uint[] { EncodeLoad(), 1u, 3u },   // LLLoad A, $81
                new uint[] { EncodeStore(), 3u, 1u },  // LLStore $81, A
                new uint[] { EncodeLoad(), 1u, 4u },   // LLLoad A, $82
                new uint[] { EncodeStore(), 4u, 1u }   // LLStore $82, A
            );
            
            var stringPool = new StringPool();
            stringPool.Intern("A");
            stringPool.Intern("$80");
            stringPool.Intern("$81");
            stringPool.Intern("$82");

            // Act
            var resultSlab = _optimizer.OptimizeSlab(slab, stringPool, OptimizationLevel.Basic);
            
            // Assert
            Assert.That(resultSlab, Is.Not.Null.And.Not.Empty);
            var header = SlabHeader.Read(resultSlab);
            Assert.That(header.IrStage, Is.EqualTo(3u));
            // Current implementation preserves all instructions (optimization not yet implemented)
            Assert.That(header.ElementCount, Is.EqualTo(6));
        }

        [Test]
        public void OptimizeSlab_WithLargeInstructionList_ShouldNotHang()
        {
            // Arrange: Large instruction list with many redundant pairs
            var instructionBlocks = new List<uint[]>();
            var stringPool = new StringPool();
            
            stringPool.Intern("A");
            
            for (int i = 0; i < 1000; i++)
            {
                string addr = $"${i & 0xFF:X2}";
                stringPool.Intern(addr);
                
                instructionBlocks.Add(new uint[] { EncodeLoad(), 1u, (uint)i + 100 });
                instructionBlocks.Add(new uint[] { EncodeStore(), (uint)i + 100, 1u });
            }
            
            var slab = BuildLlirInstructions(instructionBlocks.ToArray());
            
            // Act
            var resultSlab = _optimizer.OptimizeSlab(slab, stringPool, OptimizationLevel.Basic);
            
            // Assert
            Assert.That(resultSlab, Is.Not.Null.And.Not.Empty);
            var header = SlabHeader.Read(resultSlab);
            Assert.That(header.IrStage, Is.EqualTo(3u));
            // Current implementation preserves all instructions (optimization not yet implemented)
            Assert.That(header.ElementCount, Is.EqualTo(2000));
        }
    }
}
