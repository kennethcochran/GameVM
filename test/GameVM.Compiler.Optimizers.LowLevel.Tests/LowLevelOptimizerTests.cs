using GameVM.Compiler.Core.IR;
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
        private const byte LLIR_MULTIPLY = (byte)LlirInstructionKind.Multiply;

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

        [Test]
        public void OptimizeSlab_WithOptimizationLevelNone_AndRedundantLoadStore_ShouldNotOptimize()
        {
            // Arrange: Redundant load-store pair (same operands)
            var instList = CreateLlirInstList(
                new uint[] { LLIR_LOAD, 1u, 2u },  // LLLoad A, $80
                new uint[] { LLIR_STORE, 1u, 2u }   // LLStore $80, A (redundant - same address)
            );

            var stringPool = new StringPool();
            stringPool.Intern("A");
            stringPool.Intern("$80");

            // Act
            var resultList = _optimizer.OptimizeSlab(instList, stringPool, OptimizationLevel.None);

            // Assert: With OptimizationLevel.None, no optimizations should be applied
            Assert.That(resultList.Count, Is.EqualTo(2));
            Assert.That(resultList.GetKind(0), Is.EqualTo(LLIR_LOAD));
            Assert.That(resultList.GetKind(1), Is.EqualTo(LLIR_STORE));
        }

        [Test]
        public void OptimizeSlab_WithConsecutiveProducts_KeepsLoadConsumedByMultiply()
        {
            // Two consecutive products share their left operand: a gets x
            // times y and b gets x times z. The first Load of x feeds the
            // first Multiply through the accumulator, so it must survive
            // Basic optimization even though a second Load of x follows it.
            var instList = CreateLlirInstList(
                new uint[] { LLIR_LOAD, 0x80u, 0u },                          // Load x
                new uint[] { LLIR_MULTIPLY, 0x81u, 0u, 0x85u, 0x86u },       // Multiply y (scratch $85/$86)
                new uint[] { LLIR_STORE, 0x83u },                            // Store a
                new uint[] { LLIR_LOAD, 0x80u, 0u },                          // Load x
                new uint[] { LLIR_MULTIPLY, 0x82u, 0u, 0x85u, 0x86u },       // Multiply z
                new uint[] { LLIR_STORE, 0x84u }                             // Store b
            );

            var resultList = _optimizer.OptimizeSlab(instList, new StringPool(), OptimizationLevel.Basic);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(resultList.Count, Is.EqualTo(6));
                Assert.That(resultList.GetKind(0), Is.EqualTo(LLIR_LOAD));
                Assert.That(resultList.GetKind(1), Is.EqualTo(LLIR_MULTIPLY));
                Assert.That(resultList.GetKind(2), Is.EqualTo(LLIR_STORE));
                Assert.That(resultList.GetKind(3), Is.EqualTo(LLIR_LOAD));
                Assert.That(resultList.GetKind(4), Is.EqualTo(LLIR_MULTIPLY));
                Assert.That(resultList.GetKind(5), Is.EqualTo(LLIR_STORE));
            }
        }

        [Test]
        public void OptimizeSlab_LoadMultiplyLoadSameAddress_KeepsFirstLoad()
        {
            // Load; Multiply; Load with the same address in both loads
            // and no Store between the Multiply and the second Load: the
            // first Load feeds the Multiply through the accumulator, so
            // it must survive Basic optimization even though a second
            // Load of the same address follows it.
            var instList = CreateLlirInstList(
                new uint[] { LLIR_LOAD, 0x80u, 0u },                          // Load x
                new uint[] { LLIR_MULTIPLY, 0x81u, 0u, 0x85u, 0x86u },       // Multiply y (scratch $85/$86)
                new uint[] { LLIR_LOAD, 0x80u, 0u }                           // Load x again
            );

            var resultList = _optimizer.OptimizeSlab(instList, new StringPool(), OptimizationLevel.Basic);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(resultList.Count, Is.EqualTo(3));
                Assert.That(resultList.GetKind(0), Is.EqualTo(LLIR_LOAD));
                Assert.That(resultList.GetKind(1), Is.EqualTo(LLIR_MULTIPLY));
                Assert.That(resultList.GetKind(2), Is.EqualTo(LLIR_LOAD));
            }
        }

        [Test]
        public void OptimizeSlab_WithConsecutiveCopies_KeepsLoadConsumedByStore()
        {
            // Two consecutive copies share their source: a gets x and b gets
            // x. The first Load of x feeds the first Store through the
            // accumulator even though the store targets a different address,
            // so it must survive even though a second Load of x follows it.
            var instList = CreateLlirInstList(
                new uint[] { LLIR_LOAD, 0x80u, 0u },  // Load x
                new uint[] { LLIR_STORE, 0x81u },     // Store a
                new uint[] { LLIR_LOAD, 0x80u, 0u },  // Load x
                new uint[] { LLIR_STORE, 0x82u }      // Store b
            );

            var resultList = _optimizer.OptimizeSlab(instList, new StringPool(), OptimizationLevel.Basic);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(resultList.Count, Is.EqualTo(4));
                Assert.That(resultList.GetKind(0), Is.EqualTo(LLIR_LOAD));
                Assert.That(resultList.GetKind(1), Is.EqualTo(LLIR_STORE));
                Assert.That(resultList.GetKind(2), Is.EqualTo(LLIR_LOAD));
                Assert.That(resultList.GetKind(3), Is.EqualTo(LLIR_STORE));
            }
        }

        [Test]
        public void OptimizeSlab_ShouldPreserveInstructionFlags()
        {
            // Arrange: a callee Return (198) whose CalleeReturn flag tells the
            // backend to emit RTS instead of the entry halt. The optimizer's
            // copy pass must not strip flags — before this regression test it
            // rebuilt every instruction with InstructionFlag.None, so a called
            // procedure's return silently became a jump to the halt loop.
            var builder = new InstListBuilder();
            builder.Append(198, InstructionFlag.CalleeReturn, 0, 0, ReadOnlySpan<uint>.Empty);
            var instList = builder.Build();
            var stringPool = new StringPool();

            // Act — flags must survive at every optimization level, including
            // None (the copy loop runs unconditionally).
            var resultNone = _optimizer.OptimizeSlab(instList, stringPool, OptimizationLevel.None);
            var resultBasic = _optimizer.OptimizeSlab(instList, stringPool, OptimizationLevel.Basic);

            // Assert
            using (Assert.EnterMultipleScope())
            {
                Assert.That(resultNone.Count, Is.EqualTo(1));
                Assert.That((InstructionFlag)resultNone.GetFlags(0), Is.EqualTo(InstructionFlag.CalleeReturn));
                Assert.That(resultBasic.Count, Is.EqualTo(1));
                Assert.That((InstructionFlag)resultBasic.GetFlags(0), Is.EqualTo(InstructionFlag.CalleeReturn));
            }
        }

    }
}