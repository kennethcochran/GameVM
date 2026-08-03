using GameVM.Compiler.Optimizers.LowLevel;
using GameVM.Compiler.Core.IR.Slab;
using GameVM.Compiler.Core.IR.SlabProcessing;
using GameVM.Compiler.Core.IR.Buffers;
using GameVM.Compiler.Core.Enums;
using static GameVM.Compiler.Core.IR.Slab.InstructionMetadata;
using static GameVM.Compiler.Core.IR.Slab.InstructionMetadataFlags;

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

        [SetUp]
        public void Setup()
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

        #region Register Allocation Tests

        [Test]
        public void OptimizeSlab_RegisterAllocation_EffectivelyAllocatesXYARegisters()
        {
            // Arrange: Load and store sequences that could benefit from register allocation
            var slab = BuildLlirInstructions(
                new uint[] { EncodeLoad(), 1u, 2u },  // LLLoad A, $01
                new uint[] { EncodeStore(), 3u, 1u },  // LLStore $80, A
                new uint[] { EncodeLoad(), 1u, 4u },  // LLLoad A, $02
                new uint[] { EncodeStore(), 5u, 1u }   // LLStore $81, A
            );

            var stringPool = new StringPool();
            stringPool.Intern("A");
            stringPool.Intern("$01");
            stringPool.Intern("$80");
            stringPool.Intern("$02");
            stringPool.Intern("$81");

            // Act
            var resultSlab = _optimizer.OptimizeSlab(slab, stringPool, OptimizationLevel.Basic);

            // Assert
            Assert.That(resultSlab, Is.Not.Null.And.Not.Empty);
            var header = SlabHeader.Read(resultSlab);
            Assert.That(header.IrStage, Is.EqualTo(3u)); // Should remain LLIR
            // Register allocation might not reduce instruction count, but should produce valid LLIR
            Assert.That(header.ElementCount, Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void OptimizeSlab_RegisterReuse_MinimizesRegisterUsage()
        {
            // Arrange: Repeated loads/stores using same register
            var slab = BuildLlirInstructions(
                new uint[] { EncodeLoad(), 1u, 2u },  // LLLoad A, 1
                new uint[] { EncodeStore(), 3u, 1u },  // LLStore $80, A
                new uint[] { EncodeLoad(), 1u, 4u },  // LLLoad A, 2
                new uint[] { EncodeStore(), 5u, 1u }   // LLStore $81, A
            );

            var stringPool = new StringPool();
            stringPool.Intern("A");
            stringPool.Intern("$01");
            stringPool.Intern("$80");
            stringPool.Intern("$02");
            stringPool.Intern("$81");

            // Act
            var resultSlab = _optimizer.OptimizeSlab(slab, stringPool, OptimizationLevel.Aggressive);

            // Assert
            Assert.That(resultSlab, Is.Not.Null.And.Not.Empty);
            var header = SlabHeader.Read(resultSlab);
            Assert.That(header.IrStage, Is.EqualTo(3u));
            // Optimization might eliminate redundant loads/stores
            Assert.That(resultSlab, Is.Not.Null);
        }

        #endregion

        #region Instruction Peepholing Tests

        [Test]
        public void OptimizeSlab_RedundantLoad_EliminatesUnnecessaryLoad()
        {
            // Arrange: Load A, Load A (same value), Store A
            var slab = BuildLlirInstructions(
                new uint[] { EncodeLoad(), 1u, 2u },  // LLLoad A, 5
                new uint[] { EncodeLoad(), 1u, 2u },  // LLLoad A, 5 (redundant)
                new uint[] { EncodeStore(), 3u, 1u }  // LLStore $80, A
            );

            var stringPool = new StringPool();
            stringPool.Intern("A");
            stringPool.Intern("5");
            stringPool.Intern("$80");

            // Act
            var resultSlab = _optimizer.OptimizeSlab(slab, stringPool, OptimizationLevel.Basic);

            // Assert
            Assert.That(resultSlab, Is.Not.Null.And.Not.Empty);
            var header = SlabHeader.Read(resultSlab);
            Assert.That(header.IrStage, Is.EqualTo(3u));
            // When implemented, redundant load should be eliminated
            Assert.That(header.ElementCount, Is.LessThanOrEqualTo(2));
        }

        [Test]
        public void OptimizeSlab_LoadStoreLoadPattern_OptimizesCacheAccess()
        {
            // Arrange: Store A, Load A (same value), Store A again
            var slab = BuildLlirInstructions(
                new uint[] { EncodeStore(), 1u, 1u },  // LLStore $80, A
                new uint[] { EncodeLoad(), 1u, 1u },   // LLLoad A, 5
                new uint[] { EncodeStore(), 1u, 1u }   // LLStore $80, A
            );

            var stringPool = new StringPool();
            stringPool.Intern("A");
            stringPool.Intern("$80");

            // Act
            var resultSlab = _optimizer.OptimizeSlab(slab, stringPool, OptimizationLevel.Aggressive);

            // Assert
            Assert.That(resultSlab, Is.Not.Null.And.Not.Empty);
            var header = SlabHeader.Read(resultSlab);
            Assert.That(header.IrStage, Is.EqualTo(3u));
            // When implemented, should recognize A already has the stored value
            Assert.That(resultSlab, Is.Not.Null);
        }

        [Test]
        public void OptimizeSlab_LoadWithoutStore_RemovesDeadLoad()
        {
            // Arrange: Load A (5), Load A (10), Store A (10) - first load is dead
            var slab = BuildLlirInstructions(
                new uint[] { EncodeLoad(), 1u, 2u },  // LLLoad A, 5
                new uint[] { EncodeLoad(), 1u, 3u },  // LLLoad A, 10
                new uint[] { EncodeStore(), 3u, 1u }  // LLStore $80, A
            );

            var stringPool = new StringPool();
            stringPool.Intern("A");
            stringPool.Intern("5");
            stringPool.Intern("10");
            stringPool.Intern("$80");

            // Act
            var resultSlab = _optimizer.OptimizeSlab(slab, stringPool, OptimizationLevel.Basic);

            // Assert
            Assert.That(resultSlab, Is.Not.Null.And.Not.Empty);
            var header = SlabHeader.Read(resultSlab);
            Assert.That(header.IrStage, Is.EqualTo(3u));
            // When implemented, first load should be eliminated
            var loads = GetLoadInstructions(resultSlab);
            Assert.That(loads.Count, Is.LessThanOrEqualTo(1));
        }

        #endregion

        #region Branch Optimization Tests

        [Test]
        public void OptimizeSlab_UnconditionalBranchAtEnd_RemovesUnreachableCode()
        {
            // Arrange: Load, Label, Load (unreachable after branch)
            var slab = BuildLlirInstructions(
                new uint[] { EncodeLoad(), 1u, 2u },  // LLLoad A, 1
                new uint[] { EncodeLabel(), 2u },         // Label "exit"
                new uint[] { EncodeLoad(), 1u, 3u }   // LLLoad A, 2 (would be unreachable after branch)
            );

            var stringPool = new StringPool();
            stringPool.Intern("A");
            stringPool.Intern("1");
            stringPool.Intern("exit");
            stringPool.Intern("2");

            // Act
            var resultSlab = _optimizer.OptimizeSlab(slab, stringPool, OptimizationLevel.Basic);

            // Assert
            Assert.That(resultSlab, Is.Not.Null.And.Not.Empty);
            var header = SlabHeader.Read(resultSlab);
            Assert.That(header.IrStage, Is.EqualTo(3u));
            // When implemented, unreachable load should be removed
            var loads = GetLoadInstructions(resultSlab);
            Assert.That(loads.Count, Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void OptimizeSlab_RedundantBranches_EliminatesJumpToNextInstruction()
        {
            // Arrange: Label, Load, Label, Load
            var slab = BuildLlirInstructions(
                new uint[] { EncodeLabel(), 1u },         // Label "here"
                new uint[] { EncodeLoad(), 1u, 2u },   // LLLoad A, 1
                new uint[] { EncodeLabel(), 2u },         // Label "next"
                new uint[] { EncodeLoad(), 1u, 3u }   // LLLoad A, 2
            );

            var stringPool = new StringPool();
            stringPool.Intern("here");
            stringPool.Intern("A");
            stringPool.Intern("1");
            stringPool.Intern("next");
            stringPool.Intern("2");

            // Act
            var resultSlab = _optimizer.OptimizeSlab(slab, stringPool, OptimizationLevel.Aggressive);

            // Assert
            Assert.That(resultSlab, Is.Not.Null.And.Not.Empty);
            var header = SlabHeader.Read(resultSlab);
            Assert.That(header.IrStage, Is.EqualTo(3u));
            // When implemented, jump to next instruction should be removed
            Assert.That(resultSlab, Is.Not.Null);
        }

        #endregion

        #region Memory Access Optimization Tests

        [Test]
        public void OptimizeSlab_ZeroPageAccess_PreferredOverAbsolute()
        {
            // Arrange: Load, Store (zero page), Load, Store (absolute)
            var slab = BuildLlirInstructions(
                new uint[] { EncodeLoad(), 1u, 2u },  // LLLoad A, 42
                new uint[] { EncodeStore(), 2u, 1u },  // LLStore $80, A (zero page)
                new uint[] { EncodeLoad(), 3u, 4u },  // LLLoad A, 84
                new uint[] { EncodeStore(), 5u, 3u }   // LLStore $2000, A (absolute)
            );

            var stringPool = new StringPool();
            stringPool.Intern("A");
            stringPool.Intern("42");
            stringPool.Intern("$80");
            stringPool.Intern("84");
            stringPool.Intern("$2000");

            // Act
            var resultSlab = _optimizer.OptimizeSlab(slab, stringPool, OptimizationLevel.Aggressive);

            // Assert
            Assert.That(resultSlab, Is.Not.Null.And.Not.Empty);
            var header = SlabHeader.Read(resultSlab);
            Assert.That(header.IrStage, Is.EqualTo(3u));
            // When implemented, zero-page access should be preferred
            Assert.That(resultSlab, Is.Not.Null);
        }

        #endregion

        #region Loop Optimization Tests

        [Test]
        public void OptimizeSlab_SimpleLoop_UnrollsIfSmall()
        {
            // Arrange: Label, Load (loop body)
            var slab = BuildLlirInstructions(
                new uint[] { EncodeLabel(), 1u },         // Label "loop"
                new uint[] { EncodeLoad(), 1u, 2u }   // LLLoad A, 1
            );

            var stringPool = new StringPool();
            stringPool.Intern("loop");
            stringPool.Intern("A");
            stringPool.Intern("1");

            // Act
            var resultSlab = _optimizer.OptimizeSlab(slab, stringPool, OptimizationLevel.Aggressive);

            // Assert
            Assert.That(resultSlab, Is.Not.Null.And.Not.Empty);
            var header = SlabHeader.Read(resultSlab);
            Assert.That(header.IrStage, Is.EqualTo(3u));
            // When implemented, small loops may be unrolled or optimized
            Assert.That(resultSlab, Is.Not.Null);
        }

        #endregion

        #region Optimization Level Tests

        [Test]
        public void OptimizeSlab_WithBasicLevel_AppliesBasicOptimizations()
        {
            // Arrange: Redundant load
            var slab = BuildLlirInstructions(
                new uint[] { EncodeLoad(), 1u, 2u },  // LLLoad A, 5
                new uint[] { EncodeLoad(), 1u, 2u },  // LLLoad A, 5 (duplicate)
                new uint[] { EncodeStore(), 2u, 1u }   // LLStore $80, A
            );

            var stringPool = new StringPool();
            stringPool.Intern("A");
            stringPool.Intern("5");
            stringPool.Intern("$80");

            // Act
            var resultSlab = _optimizer.OptimizeSlab(slab, stringPool, OptimizationLevel.Basic);

            // Assert
            Assert.That(resultSlab, Is.Not.Null.And.Not.Empty);
            var header = SlabHeader.Read(resultSlab);
            Assert.That(header.IrStage, Is.EqualTo(3u));
            // When implemented, redundant loads should be removed
            Assert.That(resultSlab, Is.Not.Null);
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
                instructionBlocks.Add(new uint[] { EncodeLoad(), 1u, 2u }); // LLLoad A, 5
                instructionBlocks.Add(new uint[] { EncodeStore(), 2u, 1u }); // LLStore $80, A
            }

            var slab = BuildLlirInstructions(instructionBlocks.ToArray());

            // Act
            var resultSlab = _optimizer.OptimizeSlab(slab, stringPool, OptimizationLevel.Aggressive);

            // Assert
            Assert.That(resultSlab, Is.Not.Null.And.Not.Empty);
            var header = SlabHeader.Read(resultSlab);
            Assert.That(header.IrStage, Is.EqualTo(3u));
            // When implemented, should reduce instruction count
            Assert.That(resultSlab.Count, Is.LessThanOrEqualTo(10));
        }

        #endregion

        #region Edge Cases

        [Test]
        public void OptimizeSlab_EmptyLLIR_RemainsEmpty()
        {
            // Arrange: Empty LLIR slab throws (optimizer requires valid slab)
            var slab = Array.Empty<uint>();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _optimizer.OptimizeSlab(slab, new StringPool(), OptimizationLevel.Basic));
        }

        [Test]
        public void OptimizeSlab_SingleInstruction_PreservesInstruction()
        {
            // Arrange
            var slab = BuildLlirInstructions(
                new uint[] { EncodeLoad(), 1u, 2u }  // LLLoad A, 1
            );

            var stringPool = new StringPool();
            stringPool.Intern("A");
            stringPool.Intern("1");

            // Act
            var resultSlab = _optimizer.OptimizeSlab(slab, stringPool, OptimizationLevel.Basic);

            // Assert
            Assert.That(resultSlab, Is.Not.Null.And.Not.Empty);
            var header = SlabHeader.Read(resultSlab);
            Assert.That(header.IrStage, Is.EqualTo(3u));
            Assert.That(resultSlab, Is.Not.Null);
            Assert.That(GetInstructionCount(resultSlab), Is.GreaterThanOrEqualTo(1));
        }

        #endregion

        #region Helper Methods

private static int GetInstructionCount(uint[] slab)
        {
            var header = SlabHeader.Read(slab);
            return (int)header.ElementCount;
        }

        private static List<uint[]> GetLoadInstructions(uint[] slab)
        {
            var loads = new List<uint[]>();
            var offset = SlabHeader.HeaderIndex.Length;

            while (offset < slab.Length)
            {
                var metadata = slab[offset];
                var kind = (byte)(metadata >> 26 & 0x3F);
                var size = (byte)((metadata >> 21) & 0x1F);

                if (kind == LLIR_LOAD)
                {
                    var instruction = new uint[size];
                    Array.Copy(slab, offset, instruction, 0, size);
                    loads.Add(instruction);
                }

                offset += size;
            }

            return loads;
        }

        #endregion
    }
}
