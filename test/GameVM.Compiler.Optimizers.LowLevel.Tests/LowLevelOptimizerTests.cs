using GameVM.Compiler.Core.IR.Slab;
using GameVM.Compiler.Core.IR.SlabProcessing;
using GameVM.Compiler.Core.IR.Buffers;
using GameVM.Compiler.Core.IR.Soa;
using GameVM.Compiler.Core.Enums;
using NUnit.Framework;


namespace GameVM.Compiler.Optimizers.LowLevel.Tests
{
    [TestFixture]
    public class LowLevelOptimizerTests
    {
        private const byte LLIR_LOAD = 193;
        private const byte LLIR_STORE = 194;
        private const byte LLIR_LABEL = 192;
        private const byte LLIR_CALL = 195;
        private const byte LLIR_JUMP = 196;

        private DefaultLowLevelOptimizer _optimizer;

        [SetUp]
        public void SetUp()
        {
            _optimizer = new DefaultLowLevelOptimizer();
        }

        private static InstList CreateLlirInstList(params uint[][] instructionBlocks)
        {
            var builder = new InstListBuilder();
            foreach (var block in instructionBlocks)
            {
                byte kind = (byte)block[0];
                ushort argCount = (ushort)(block.Length - 1);
                ReadOnlySpan<uint> operands = new ReadOnlySpan<uint>(block, 1, argCount);
                builder.Append(kind, InstructionFlag.None, argCount, 0, operands);
            }
            return builder.Build();
        }

        [Test]
        public void OptimizeSlab_WithNoOptimization_ShouldCopyInstructions()
        {
            // Arrange: Load + Store (non-redundant)
            var instList = CreateLlirInstList(
                new uint[] { LLIR_LOAD, 1u, 2u },  // LLLoad A, $80
                new uint[] { LLIR_STORE, 3u, 1u }   // LLStore $81, A
            );
            
            var stringPool = new StringPool();
            stringPool.Intern("A");
            stringPool.Intern("$80");
            stringPool.Intern("$81");

            // Act
            var resultList = _optimizer.OptimizeSlab(instList, stringPool, OptimizationLevel.None);
            
            // Assert
            Assert.That(resultList.Count, Is.EqualTo(2));
            Assert.That(resultList.GetKind(0), Is.EqualTo(LLIR_LOAD));
            Assert.That(resultList.GetKind(1), Is.EqualTo(LLIR_STORE));
        }

        [Test]
        public void OptimizeSlab_WithBasicOptimization_ShouldRemoveRedundantLoadStores()
        {
            // Arrange: Redundant load-store pairs (load then store to same address)
            var instList = CreateLlirInstList(
                new uint[] { LLIR_LOAD, 1u, 2u },  // LLLoad A, $80
                new uint[] { LLIR_STORE, 1u, 2u },  // LLStore $80, A (redundant - same address)
                new uint[] { LLIR_LOAD, 3u, 4u },   // LLLoad X, $81
                new uint[] { LLIR_STORE, 3u, 4u }   // LLStore $81, X (redundant - same address)
            );
            
            var stringPool = new StringPool();
            stringPool.Intern("A");
            stringPool.Intern("$80");
            stringPool.Intern("X");
            stringPool.Intern("$81");

            // Act
            var resultList = _optimizer.OptimizeSlab(instList, stringPool, OptimizationLevel.Basic);
            
            // Assert
            Assert.That(resultList.Count, Is.EqualTo(2)); // Should be 2 loads (stores removed)
            Assert.That(resultList.GetKind(0), Is.EqualTo(LLIR_LOAD));
            Assert.That(resultList.GetKind(1), Is.EqualTo(LLIR_LOAD));
        }

        [Test]
        public void OptimizeSlab_WithAggressiveOptimization_ShouldRemoveRedundantLoadStores()
        {
            // Arrange: First pair is redundant, second is not
            var instList = CreateLlirInstList(
                new uint[] { LLIR_LOAD, 1u, 2u },  // LLLoad A, $80
                new uint[] { LLIR_STORE, 1u, 2u },  // LLStore $80, A (redundant)
                new uint[] { LLIR_LOAD, 1u, 3u },   // LLLoad A, $81
                new uint[] { LLIR_STORE, 4u, 1u }   // LLStore $82, A (different address - not redundant)
            );
            
            var stringPool = new StringPool();
            stringPool.Intern("A");
            stringPool.Intern("$80");
            stringPool.Intern("$81");
            stringPool.Intern("$82");

            // Act
            var resultList = _optimizer.OptimizeSlab(instList, stringPool, OptimizationLevel.Aggressive);
            
            // Assert
            Assert.That(resultList.Count, Is.EqualTo(3)); // Should be Load, Load, Store
            Assert.That(resultList.GetKind(0), Is.EqualTo(LLIR_LOAD));
            Assert.That(resultList.GetKind(1), Is.EqualTo(LLIR_LOAD));
            Assert.That(resultList.GetKind(2), Is.EqualTo(LLIR_STORE));
        }

        [Test]
        public void OptimizeSlab_WithNonRedundantLoadStore_ShouldKeepBoth()
        {
            // Arrange: Load and store to different addresses
            var instList = CreateLlirInstList(
                new uint[] { LLIR_LOAD, 1u, 2u },  // LLLoad A, $80
                new uint[] { LLIR_STORE, 3u, 1u }   // LLStore $81, A (different address)
            );
            
            var stringPool = new StringPool();
            stringPool.Intern("A");
            stringPool.Intern("$80");
            stringPool.Intern("$81");

            // Act
            var resultList = _optimizer.OptimizeSlab(instList, stringPool, OptimizationLevel.Basic);
            
            // Assert
            Assert.That(resultList.Count, Is.EqualTo(2));
            Assert.That(resultList.GetKind(0), Is.EqualTo(LLIR_LOAD));
            Assert.That(resultList.GetKind(1), Is.EqualTo(LLIR_STORE));
        }

        [Test]
        public void OptimizeSlab_WithDifferentRegisters_ShouldKeepBoth()
        {
            // Arrange: Load A, Store X (different registers)
            var instList = CreateLlirInstList(
                new uint[] { LLIR_LOAD, 1u, 2u },  // LLLoad A, $80
                new uint[] { LLIR_STORE, 2u, 3u }   // LLStore $80, X (different register)
            );
            
            var stringPool = new StringPool();
            stringPool.Intern("A");
            stringPool.Intern("$80");
            stringPool.Intern("X");

            // Act
            var resultList = _optimizer.OptimizeSlab(instList, stringPool, OptimizationLevel.Basic);
            
            // Assert
            Assert.That(resultList.Count, Is.EqualTo(2));
            Assert.That(resultList.GetKind(0), Is.EqualTo(LLIR_LOAD));
            Assert.That(resultList.GetKind(1), Is.EqualTo(LLIR_STORE));
        }

        [Test]
        public void OptimizeSlab_WithMixedInstructions_ShouldOnlyOptimizeLoadStorePairs()
        {
            // Arrange: Mixed control flow and load/store pairs
            var instList = CreateLlirInstList(
                new uint[] { LLIR_LABEL, 1u },         // Label "start"
                new uint[] { LLIR_LOAD, 1u, 2u },   // LLLoad A, $80
                new uint[] { LLIR_STORE, 1u, 2u },  // LLStore $80, A (redundant)
                new uint[] { LLIR_CALL, 2u },          // LLCall "subroutine"
                new uint[] { LLIR_LOAD, 3u, 4u },   // LLLoad X, $81
                new uint[] { LLIR_STORE, 3u, 4u },  // LLStore $81, X (redundant)
                new uint[] { LLIR_JUMP, 3u }           // LLJump "end"
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
            var resultList = _optimizer.OptimizeSlab(instList, stringPool, OptimizationLevel.Basic);
            
            // Assert
            Assert.That(resultList.Count, Is.EqualTo(5)); // Label, Load, Call, Load, Jump
            Assert.That(resultList.GetKind(0), Is.EqualTo(LLIR_LABEL));
            Assert.That(resultList.GetKind(1), Is.EqualTo(LLIR_LOAD));
            Assert.That(resultList.GetKind(2), Is.EqualTo(LLIR_CALL));
            Assert.That(resultList.GetKind(3), Is.EqualTo(LLIR_LOAD));
            Assert.That(resultList.GetKind(4), Is.EqualTo(LLIR_JUMP));
        }

        [Test]
        public void OptimizeSlab_WithEmptyInput_ShouldReturnEmpty()
        {
            // Arrange: Empty InstList
            var instList = new InstListBuilder().Build();
            
            // Act
            var resultList = _optimizer.OptimizeSlab(instList, new StringPool(), OptimizationLevel.Basic);
            
            // Assert
            Assert.That(resultList.Count, Is.EqualTo(0));
        }

        [Test]
        public void OptimizeSlab_WithSingleInstruction_ShouldReturnSame()
        {
            // Arrange: Single load instruction
            var instList = CreateLlirInstList(
                new uint[] { LLIR_LOAD, 1u, 2u }  // LLLoad A, $80
            );
            
            var stringPool = new StringPool();
            stringPool.Intern("A");
            stringPool.Intern("$80");

            // Act
            var resultList = _optimizer.OptimizeSlab(instList, stringPool, OptimizationLevel.Basic);
            
            // Assert
            Assert.That(resultList.Count, Is.EqualTo(1));
            Assert.That(resultList.GetKind(0), Is.EqualTo(LLIR_LOAD));
        }

        [Test]
        public void OptimizeSlab_WithLoadAtEnd_ShouldKeepLoad()
        {
            // Arrange: Test boundary condition where load is at end
            var instList = CreateLlirInstList(
                new uint[] { LLIR_LOAD, 1u, 2u },  // LLLoad A, $80 (no following instruction)
                new uint[] { LLIR_STORE, 3u, 4u },  // LLStore $81, X
                new uint[] { LLIR_LOAD, 5u, 6u }   // LLLoad A, $82 (at very end)
            );
            
            var stringPool = new StringPool();
            stringPool.Intern("A");
            stringPool.Intern("$80");
            stringPool.Intern("X");
            stringPool.Intern("$81");
            stringPool.Intern("$82");

            // Act
            var resultList = _optimizer.OptimizeSlab(instList, stringPool, OptimizationLevel.Basic);
            
            // Assert
            Assert.That(resultList.Count, Is.EqualTo(3));
            Assert.That(resultList.GetKind(0), Is.EqualTo(LLIR_LOAD));
            Assert.That(resultList.GetKind(1), Is.EqualTo(LLIR_STORE));
            Assert.That(resultList.GetKind(2), Is.EqualTo(LLIR_LOAD));
        }

        [Test]
        public void OptimizeSlab_WithConsecutiveRedundantPairs_ShouldOptimizeAll()
        {
            // Arrange: Multiple consecutive redundant load-store pairs
            var instList = CreateLlirInstList(
                new uint[] { LLIR_LOAD, 1u, 2u },  // LLLoad A, $80
                new uint[] { LLIR_STORE, 1u, 2u },  // LLStore $80, A
                new uint[] { LLIR_LOAD, 1u, 3u },   // LLLoad A, $81
                new uint[] { LLIR_STORE, 1u, 3u },  // LLStore $81, A
                new uint[] { LLIR_LOAD, 1u, 4u },   // LLLoad A, $82
                new uint[] { LLIR_STORE, 1u, 4u }   // LLStore $82, A
            );
            
            var stringPool = new StringPool();
            stringPool.Intern("A");
            stringPool.Intern("$80");
            stringPool.Intern("$81");
            stringPool.Intern("$82");

            // Act
            var resultList = _optimizer.OptimizeSlab(instList, stringPool, OptimizationLevel.Basic);
            
            // Assert
            Assert.That(resultList.Count, Is.EqualTo(3)); // Only loads remain
            Assert.That(resultList.GetKind(0), Is.EqualTo(LLIR_LOAD));
            Assert.That(resultList.GetKind(1), Is.EqualTo(LLIR_LOAD));
            Assert.That(resultList.GetKind(2), Is.EqualTo(LLIR_LOAD));
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
                
                instructionBlocks.Add(new uint[] { LLIR_LOAD, 1u, (uint)i + 100 });
                instructionBlocks.Add(new uint[] { LLIR_STORE, 1u, (uint)i + 100 });
            }
            
            var instList = CreateLlirInstList(instructionBlocks.ToArray());
            
            // Act
            var resultList = _optimizer.OptimizeSlab(instList, stringPool, OptimizationLevel.Basic);
            
            // Assert
            Assert.That(resultList.Count, Is.EqualTo(1000)); // Should be 1000 loads
        }
    }
}