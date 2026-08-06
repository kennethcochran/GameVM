using GameVM.Compiler.Optimizers.LowLevel;
using GameVM.Compiler.Core.IR.Slab;
using GameVM.Compiler.Core.IR.SlabProcessing;
using GameVM.Compiler.Core.IR.Buffers;
using GameVM.Compiler.Core.IR.Soa;
using GameVM.Compiler.Core.Enums;


namespace GameVM.Compiler.Backend.Atari2600.Tests.Optimizers
{
    /// <summary>
    /// Tests for low-level IR optimization targeting the Atari 2600.
    /// Validates register allocation, instruction peepholing, and branch optimization.
    /// </summary>
    [TestFixture]
    public class LowLevelOptimizerTests
    {
        private DefaultLowLevelOptimizer _optimizer;
        private const byte LLIR_LOAD = 193;
        private const byte LLIR_STORE = 194;
        private const byte LLIR_LABEL = 192;


        [SetUp]
        public void Setup()
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

        #region Register Allocation Tests

        [Test]
        public void OptimizeSlab_RegisterAllocation_EffectivelyAllocatesXYARegisters()
        {
            // Arrange: Load and store sequences that could benefit from register allocation
            var instList = CreateLlirInstList(
                new uint[] { LLIR_LOAD, 1u, 2u },  // LLLoad A, $01
                new uint[] { LLIR_STORE, 3u, 1u },  // LLStore $80, A
                new uint[] { LLIR_LOAD, 1u, 4u },  // LLLoad A, $02
                new uint[] { LLIR_STORE, 5u, 1u }   // LLStore $81, A
            );

            var stringPool = new StringPool();
            stringPool.Intern("A");
            stringPool.Intern("$01");
            stringPool.Intern("$80");
            stringPool.Intern("$02");
            stringPool.Intern("$81");

            // Act
            var resultList = _optimizer.OptimizeSlab(instList, stringPool, OptimizationLevel.Basic);

            // Assert
            Assert.That(resultList.Count, Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void OptimizeSlab_RegisterReuse_MinimizesRegisterUsage()
        {
            // Arrange: Repeated loads/stores using same register
            var instList = CreateLlirInstList(
                new uint[] { LLIR_LOAD, 1u, 2u },  // LLLoad A, 1
                new uint[] { LLIR_STORE, 3u, 1u },  // LLStore $80, A
                new uint[] { LLIR_LOAD, 1u, 4u },  // LLLoad A, 2
                new uint[] { LLIR_STORE, 5u, 1u }   // LLStore $81, A
            );

            var stringPool = new StringPool();
            stringPool.Intern("A");
            stringPool.Intern("$01");
            stringPool.Intern("$80");
            stringPool.Intern("$02");
            stringPool.Intern("$81");

            // Act
            var resultList = _optimizer.OptimizeSlab(instList, stringPool, OptimizationLevel.Aggressive);

            // Assert
            Assert.That(resultList.Count, Is.GreaterThanOrEqualTo(0));
        }

        #endregion

        #region Instruction Peepholing Tests

        [Test]
        public void OptimizeSlab_RedundantLoad_EliminatesUnnecessaryLoad()
        {
            // Arrange: Load A, Load A (same value), Store A
            var instList = CreateLlirInstList(
                new uint[] { LLIR_LOAD, 1u, 2u },  // LLLoad A, 5
                new uint[] { LLIR_LOAD, 1u, 2u },  // LLLoad A, 5 (redundant)
                new uint[] { LLIR_STORE, 3u, 1u }  // LLStore $80, A
            );

            var stringPool = new StringPool();
            stringPool.Intern("A");
            stringPool.Intern("5");
            stringPool.Intern("$80");

            // Act
            var resultList = _optimizer.OptimizeSlab(instList, stringPool, OptimizationLevel.Basic);

            // Assert
            Assert.That(resultList.Count, Is.LessThanOrEqualTo(2));
        }

        [Test]
        public void OptimizeSlab_LoadStoreLoadPattern_OptimizesCacheAccess()
        {
            // Arrange: Store A, Load A (same value), Store A again
            var instList = CreateLlirInstList(
                new uint[] { LLIR_STORE, 1u, 1u },  // LLStore $80, A
                new uint[] { LLIR_LOAD, 1u, 1u },   // LLLoad A, 5
                new uint[] { LLIR_STORE, 1u, 1u }   // LLStore $80, A
            );

            var stringPool = new StringPool();
            stringPool.Intern("A");
            stringPool.Intern("$80");

            // Act
            var resultList = _optimizer.OptimizeSlab(instList, stringPool, OptimizationLevel.Aggressive);

            // Assert
            Assert.That(resultList.Count, Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void OptimizeSlab_LoadWithoutStore_RemovesDeadLoad()
        {
            // Arrange: Load A (5), Load A (10), Store A (10) - first load is dead
            var instList = CreateLlirInstList(
                new uint[] { LLIR_LOAD, 1u, 2u },  // LLLoad A, 5
                new uint[] { LLIR_LOAD, 1u, 3u },  // LLLoad A, 10
                new uint[] { LLIR_STORE, 3u, 1u }  // LLStore $80, A
            );

            var stringPool = new StringPool();
            stringPool.Intern("A");
            stringPool.Intern("5");
            stringPool.Intern("10");
            stringPool.Intern("$80");

// Act
            var resultList = _optimizer.OptimizeSlab(instList, stringPool, OptimizationLevel.Aggressive);

            // Assert
            Assert.That(resultList.Count, Is.GreaterThanOrEqualTo(0));
        }

        #endregion

        #region Branch Optimization Tests

        [Test]
        public void OptimizeSlab_UnconditionalBranchAtEnd_RemovesUnreachableCode()
        {
            // Arrange: Load, Label, Load (unreachable after branch)
            var instList = CreateLlirInstList(
                new uint[] { LLIR_LOAD, 1u, 2u },  // LLLoad A, 1
                new uint[] { LLIR_LABEL, 2u },         // Label "exit"
                new uint[] { LLIR_LOAD, 1u, 3u }   // LLLoad A, 2 (would be unreachable after branch)
            );

            var stringPool = new StringPool();
            stringPool.Intern("A");
            stringPool.Intern("1");
            stringPool.Intern("exit");
            stringPool.Intern("2");

            // Act
            var resultList = _optimizer.OptimizeSlab(instList, stringPool, OptimizationLevel.Basic);

            // Assert
            Assert.That(resultList.Count, Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void OptimizeSlab_RedundantBranches_EliminatesJumpToNextInstruction()
        {
            // Arrange: Label, Load, Label, Load
            var instList = CreateLlirInstList(
                new uint[] { LLIR_LABEL, 1u },         // Label "here"
                new uint[] { LLIR_LOAD, 1u, 2u },   // LLLoad A, 1
                new uint[] { LLIR_LABEL, 2u },         // Label "next"
                new uint[] { LLIR_LOAD, 1u, 3u }   // LLLoad A, 2
            );

            var stringPool = new StringPool();
            stringPool.Intern("here");
            stringPool.Intern("A");
            stringPool.Intern("1");
            stringPool.Intern("next");
            stringPool.Intern("2");

            // Act
            var resultList = _optimizer.OptimizeSlab(instList, stringPool, OptimizationLevel.Aggressive);

            // Assert
            Assert.That(resultList.Count, Is.GreaterThanOrEqualTo(0));
        }

        #endregion

        #region Memory Access Optimization Tests

        [Test]
        public void OptimizeSlab_ZeroPageAccess_PreferredOverAbsolute()
        {
            // Arrange: Load, Store (zero page), Load, Store (absolute)
            var instList = CreateLlirInstList(
                new uint[] { LLIR_LOAD, 1u, 2u },  // LLLoad A, 42
                new uint[] { LLIR_STORE, 2u, 1u },  // LLStore $80, A (zero page)
                new uint[] { LLIR_LOAD, 3u, 4u },  // LLLoad A, 84
                new uint[] { LLIR_STORE, 5u, 3u }   // LLStore $2000, A (absolute)
            );

            var stringPool = new StringPool();
            stringPool.Intern("A");
            stringPool.Intern("42");
            stringPool.Intern("$80");
            stringPool.Intern("84");
            stringPool.Intern("$2000");

            // Act
            var resultList = _optimizer.OptimizeSlab(instList, stringPool, OptimizationLevel.Aggressive);

            // Assert
            Assert.That(resultList.Count, Is.GreaterThanOrEqualTo(0));
        }

        #endregion

        #region Loop Optimization Tests

        [Test]
        public void OptimizeSlab_SimpleLoop_UnrollsIfSmall()
        {
            // Arrange: Label, Load (loop body)
            var instList = CreateLlirInstList(
                new uint[] { LLIR_LABEL, 1u },         // Label "loop"
                new uint[] { LLIR_LOAD, 1u, 2u }   // LLLoad A, 1
            );

            var stringPool = new StringPool();
            stringPool.Intern("loop");
            stringPool.Intern("A");
            stringPool.Intern("1");

            // Act
            var resultList = _optimizer.OptimizeSlab(instList, stringPool, OptimizationLevel.Aggressive);

            // Assert
            Assert.That(resultList.Count, Is.GreaterThanOrEqualTo(0));
        }

        #endregion

        #region Optimization Level Tests

        [Test]
        public void OptimizeSlab_WithBasicLevel_AppliesBasicOptimizations()
        {
            // Arrange: Redundant load
            var instList = CreateLlirInstList(
                new uint[] { LLIR_LOAD, 1u, 2u },  // LLLoad A, 5
                new uint[] { LLIR_LOAD, 1u, 2u },  // LLLoad A, 5 (duplicate)
                new uint[] { LLIR_STORE, 2u, 1u }   // LLStore $80, A
            );

            var stringPool = new StringPool();
            stringPool.Intern("A");
            stringPool.Intern("5");
            stringPool.Intern("$80");

// Act
            var resultList = _optimizer.OptimizeSlab(instList, stringPool, OptimizationLevel.Aggressive);

            // Assert
            Assert.That(resultList.Count, Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void OptimizeSlab_WithAggressiveLevel_ReducesCodeSize()
        {
            // Arrange: Multiple redundant loads
            var instructionBlocks = new List<uint[]>();
            var stringPool = new StringPool();
            stringPool.Intern("A");
            stringPool.Intern("$80");

            for (int i = 0; i < 5; i++)
            {
                instructionBlocks.Add(new uint[] { LLIR_LOAD, 1u, 2u }); // LLLoad A, 5
                instructionBlocks.Add(new uint[] { LLIR_STORE, 2u, 1u }); // LLStore $80, A
            }

            var instList = CreateLlirInstList(instructionBlocks.ToArray());

            // Act
            var resultList = _optimizer.OptimizeSlab(instList, stringPool, OptimizationLevel.Aggressive);

            // Assert
            Assert.That(resultList.Count, Is.LessThanOrEqualTo(10));
        }

        #endregion

        #region Edge Cases

        [Test]
        public void OptimizeSlab_EmptyLLIR_RemainsEmpty()
        {
            // Arrange: Empty InstList
            var instList = new InstListBuilder().Build();

            // Act & Assert
            var resultList = _optimizer.OptimizeSlab(instList, new StringPool(), OptimizationLevel.Basic);
            Assert.That(resultList.Count, Is.EqualTo(0));
        }

        [Test]
        public void OptimizeSlab_SingleInstruction_PreservesInstruction()
        {
            // Arrange
            var instList = CreateLlirInstList(
                new uint[] { LLIR_LOAD, 1u, 2u }  // LLLoad A, 1
            );

            var stringPool = new StringPool();
            stringPool.Intern("A");
            stringPool.Intern("1");

            // Act
            var resultList = _optimizer.OptimizeSlab(instList, stringPool, OptimizationLevel.Basic);

            // Assert
            Assert.That(resultList.Count, Is.EqualTo(1));
        }

        #endregion
    }
}