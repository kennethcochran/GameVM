using System.Collections.Generic;
using System.Linq;
using GameVM.Compiler.Application.Services;
using GameVM.Compiler.Core.IR;
using GameVM.Compiler.Core.Enums;
using NUnit.Framework;

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

        [Test]
        public void Optimize_WithNoOptimization_ShouldCopyInstructions()
        {
            var ir = CreateTestIR(
                new LowLevelIR.LLLoad { Register = "A", Value = "$80" },
                new LowLevelIR.LLStore { Address = "$81", Register = "A" }
            );

            var result = _optimizer.Optimize(ir, OptimizationLevel.None);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Instructions.Count, Is.EqualTo(2));
            Assert.That(result.Instructions[0], Is.InstanceOf<LowLevelIR.LLLoad>());
            Assert.That(result.Instructions[1], Is.InstanceOf<LowLevelIR.LLStore>());
        }

        [Test]
        public void Optimize_WithBasicOptimization_ShouldRemoveRedundantLoadStores()
        {
            var ir = CreateTestIR(
                new LowLevelIR.LLLoad { Register = "A", Value = "$80" },
                new LowLevelIR.LLStore { Address = "$80", Register = "A" },
                new LowLevelIR.LLLoad { Register = "X", Value = "$81" },
                new LowLevelIR.LLStore { Address = "$81", Register = "X" }
            );

            var result = _optimizer.Optimize(ir, OptimizationLevel.Basic);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Instructions.Count, Is.EqualTo(2));
            
            // Should keep only the stores
            Assert.That(result.Instructions[0], Is.InstanceOf<LowLevelIR.LLStore>());
            Assert.That(result.Instructions[1], Is.InstanceOf<LowLevelIR.LLStore>());
            
            var store1 = (LowLevelIR.LLStore)result.Instructions[0];
            var store2 = (LowLevelIR.LLStore)result.Instructions[1];
            Assert.That(store1.Address, Is.EqualTo("$80"));
            Assert.That(store1.Register, Is.EqualTo("A"));
            Assert.That(store2.Address, Is.EqualTo("$81"));
            Assert.That(store2.Register, Is.EqualTo("X"));
        }

        [Test]
        public void Optimize_WithAggressiveOptimization_ShouldRemoveRedundantLoadStores()
        {
            var ir = CreateTestIR(
                new LowLevelIR.LLLoad { Register = "A", Value = "$80" },
                new LowLevelIR.LLStore { Address = "$80", Register = "A" },
                new LowLevelIR.LLLoad { Register = "A", Value = "$81" },
                new LowLevelIR.LLStore { Address = "$82", Register = "A" }
            );

            var result = _optimizer.Optimize(ir, OptimizationLevel.Aggressive);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Instructions.Count, Is.EqualTo(3));
            
            // First redundant pair should be optimized to just store
            Assert.That(result.Instructions[0], Is.InstanceOf<LowLevelIR.LLStore>());
            
            // Second pair is not redundant (different addresses), should remain as Load+Store
            Assert.That(result.Instructions[1], Is.InstanceOf<LowLevelIR.LLLoad>());
            Assert.That(result.Instructions[2], Is.InstanceOf<LowLevelIR.LLStore>());
        }

        [Test]
        public void Optimize_WithNonRedundantLoadStore_ShouldKeepBoth()
        {
            var ir = CreateTestIR(
                new LowLevelIR.LLLoad { Register = "A", Value = "$80" },
                new LowLevelIR.LLStore { Address = "$81", Register = "A" }
            );

            var result = _optimizer.Optimize(ir, OptimizationLevel.Basic);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Instructions.Count, Is.EqualTo(2));
            Assert.That(result.Instructions[0], Is.InstanceOf<LowLevelIR.LLLoad>());
            Assert.That(result.Instructions[1], Is.InstanceOf<LowLevelIR.LLStore>());
        }

        [Test]
        public void Optimize_WithDifferentRegisters_ShouldKeepBoth()
        {
            var ir = CreateTestIR(
                new LowLevelIR.LLLoad { Register = "A", Value = "$80" },
                new LowLevelIR.LLStore { Address = "$80", Register = "X" }
            );

            var result = _optimizer.Optimize(ir, OptimizationLevel.Basic);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Instructions.Count, Is.EqualTo(2));
            Assert.That(result.Instructions[0], Is.InstanceOf<LowLevelIR.LLLoad>());
            Assert.That(result.Instructions[1], Is.InstanceOf<LowLevelIR.LLStore>());
        }

        [Test]
        public void Optimize_WithMixedInstructions_ShouldOnlyOptimizeLoadStorePairs()
        {
            var ir = CreateTestIR(
                new LowLevelIR.LLLabel { Name = "start" },
                new LowLevelIR.LLLoad { Register = "A", Value = "$80" },
                new LowLevelIR.LLStore { Address = "$80", Register = "A" },
                new LowLevelIR.LLCall { Label = "subroutine" },
                new LowLevelIR.LLLoad { Register = "X", Value = "$81" },
                new LowLevelIR.LLStore { Address = "$81", Register = "X" },
                new LowLevelIR.LLJump { Target = "end" }
            );

            var result = _optimizer.Optimize(ir, OptimizationLevel.Basic);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Instructions.Count, Is.EqualTo(5)); // 2 redundant pairs optimized
            
            // Verify instruction types
            Assert.That(result.Instructions[0], Is.InstanceOf<LowLevelIR.LLLabel>());
            Assert.That(result.Instructions[1], Is.InstanceOf<LowLevelIR.LLStore>()); // Optimized from Load+Store
            Assert.That(result.Instructions[2], Is.InstanceOf<LowLevelIR.LLCall>());
            Assert.That(result.Instructions[3], Is.InstanceOf<LowLevelIR.LLStore>()); // Optimized from Load+Store
            Assert.That(result.Instructions[4], Is.InstanceOf<LowLevelIR.LLJump>());
        }

        [Test]
        public void Optimize_WithEmptyInstructions_ShouldReturnEmpty()
        {
            var ir = CreateTestIR();

            var result = _optimizer.Optimize(ir, OptimizationLevel.Basic);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Instructions.Count, Is.EqualTo(0));
        }

        [Test]
        public void Optimize_WithSingleInstruction_ShouldReturnSame()
        {
            var ir = CreateTestIR(new LowLevelIR.LLLoad { Register = "A", Value = "$80" });

            var result = _optimizer.Optimize(ir, OptimizationLevel.Basic);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Instructions.Count, Is.EqualTo(1));
            Assert.That(result.Instructions[0], Is.InstanceOf<LowLevelIR.LLLoad>());
        }

        [Test]
        public void Optimize_WithConsecutiveRedundantPairs_ShouldOptimizeAll()
        {
            var ir = CreateTestIR(
                new LowLevelIR.LLLoad { Register = "A", Value = "$80" },
                new LowLevelIR.LLStore { Address = "$80", Register = "A" },
                new LowLevelIR.LLLoad { Register = "A", Value = "$81" },
                new LowLevelIR.LLStore { Address = "$81", Register = "A" },
                new LowLevelIR.LLLoad { Register = "A", Value = "$82" },
                new LowLevelIR.LLStore { Address = "$82", Register = "A" }
            );

            var result = _optimizer.Optimize(ir, OptimizationLevel.Basic);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Instructions.Count, Is.EqualTo(3));
            
            // All should be stores
            foreach (var instruction in result.Instructions)
            {
                Assert.That(instruction, Is.InstanceOf<LowLevelIR.LLStore>());
            }
        }

        [Test]
        public void Optimize_WithLargeInstructionList_ShouldNotHang()
        {
            var instructions = new List<LowLevelIR.LLInstruction>();
            
            // Create a large but manageable list with some redundant pairs
            for (int i = 0; i < 1000; i++)
            {
                instructions.Add(new LowLevelIR.LLLoad { Register = "A", Value = $"${i & 0xFF:X2}" });
                instructions.Add(new LowLevelIR.LLStore { Address = $"${i & 0xFF:X2}", Register = "A" });
            }

            var ir = CreateTestIR(instructions.ToArray());

            // This should complete quickly and not hang due to the infinite loop fix
            var result = _optimizer.Optimize(ir, OptimizationLevel.Basic);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Instructions.Count, Is.EqualTo(1000)); // All optimized to stores
            
            // Verify all instructions are stores
            Assert.That(result.Instructions.All(i => i is LowLevelIR.LLStore), Is.True);
        }

        private static LowLevelIR CreateTestIR(params LowLevelIR.LLInstruction[] instructions)
        {
            return new LowLevelIR
            {
                SourceFile = "test.ll",
                Modules = new List<LowLevelIR.LLModule>(),
                Instructions = new List<LowLevelIR.LLInstruction>(instructions)
            };
        }
    }
}
