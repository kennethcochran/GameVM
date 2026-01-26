using System.Collections.Generic;
using System.Linq;
using GameVM.Compiler.Application.Services;
using GameVM.Compiler.Core.IR;
using GameVM.Compiler.Core.Enums;
using NUnit.Framework;

namespace GameVM.Compiler.Optimizers.MidLevel.Tests
{
    [TestFixture]
    public class MidLevelOptimizerTests
    {
        private DefaultMidLevelOptimizer _optimizer;

        [SetUp]
        public void SetUp()
        {
            _optimizer = new DefaultMidLevelOptimizer();
        }

        [Test]
        public void Optimize_WithNoOptimization_ShouldCopyInstructions()
        {
            var ir = CreateTestIR(
                new MidLevelIR.MLAssign { Target = "x", Source = "5" },
                new MidLevelIR.MLAssign { Target = "y", Source = "10" }
            );

            var result = _optimizer.Optimize(ir, OptimizationLevel.None);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Modules.Count, Is.EqualTo(1));
            Assert.That(result.Modules[0].Functions.Count, Is.EqualTo(1));
            Assert.That(result.Modules[0].Functions[0].Instructions.Count, Is.EqualTo(2));
        }

        [Test]
        public void Optimize_WithBasicOptimization_ShouldPerformConstantFolding()
        {
            var ir = CreateTestIR(
                new MidLevelIR.MLAssign { Target = "x", Source = "(5 + 3)" }
            );

            var result = _optimizer.Optimize(ir, OptimizationLevel.Basic);

            Assert.That(result, Is.Not.Null);
            var instructions = result.Modules[0].Functions[0].Instructions;
            Assert.That(instructions.Count, Is.EqualTo(1));
            
            var assign = (MidLevelIR.MLAssign)instructions[0];
            Assert.That(assign.Source, Is.EqualTo("8")); // 5 + 3 = 8
        }

        [Test]
        public void Optimize_WithBasicOptimization_ShouldRemoveDuplicateAssignments()
        {
            var ir = CreateTestIR(
                new MidLevelIR.MLAssign { Target = "x", Source = "5" },
                new MidLevelIR.MLAssign { Target = "x", Source = "10" },
                new MidLevelIR.MLAssign { Target = "y", Source = "15" }
            );

            var result = _optimizer.Optimize(ir, OptimizationLevel.Basic);

            Assert.That(result, Is.Not.Null);
            var instructions = result.Modules[0].Functions[0].Instructions;
            Assert.That(instructions.Count, Is.EqualTo(2));
            
            // Should keep only the last assignment to x and the assignment to y
            Assert.That(((MidLevelIR.MLAssign)instructions[0]).Target, Is.EqualTo("x"));
            Assert.That(((MidLevelIR.MLAssign)instructions[0]).Source, Is.EqualTo("10"));
            Assert.That(((MidLevelIR.MLAssign)instructions[1]).Target, Is.EqualTo("y"));
        }

        [Test]
        public void Optimize_WithAggressiveOptimization_ShouldRemoveUnreachableCode()
        {
            var ir = CreateTestIR(
                new MidLevelIR.MLLabel { Name = "start" },
                new MidLevelIR.MLBranch { Target = "end", Condition = null },
                new MidLevelIR.MLAssign { Target = "x", Source = "5" }, // Unreachable
                new MidLevelIR.MLAssign { Target = "y", Source = "10" }, // Unreachable
                new MidLevelIR.MLLabel { Name = "end" }
            );

            var result = _optimizer.Optimize(ir, OptimizationLevel.Aggressive);

            Assert.That(result, Is.Not.Null);
            var instructions = result.Modules[0].Functions[0].Instructions;
            
            // Should keep: start label, branch, end label
            Assert.That(instructions.Count, Is.EqualTo(3));
            Assert.That(instructions[0], Is.InstanceOf<MidLevelIR.MLLabel>());
            Assert.That(instructions[1], Is.InstanceOf<MidLevelIR.MLBranch>());
            Assert.That(instructions[2], Is.InstanceOf<MidLevelIR.MLLabel>());
        }

        [Test]
        public void Optimize_WithAggressiveOptimization_ShouldPerformConstantFolding()
        {
            var ir = CreateTestIR(
                new MidLevelIR.MLAssign { Target = "x", Source = "(5 + 3)" }
            );

            var result = _optimizer.Optimize(ir, OptimizationLevel.Aggressive);

            Assert.That(result, Is.Not.Null);
            var instructions = result.Modules[0].Functions[0].Instructions;
            Assert.That(instructions.Count, Is.EqualTo(1));
            
            var assign = (MidLevelIR.MLAssign)instructions[0];
            Assert.That(assign.Source, Is.EqualTo("8")); // 5 + 3 = 8
        }

        [Test]
        public void Optimize_WithAggressiveOptimization_ShouldRemoveDuplicateAssignments()
        {
            var ir = CreateTestIR(
                new MidLevelIR.MLAssign { Target = "x", Source = "5" },
                new MidLevelIR.MLAssign { Target = "x", Source = "10" },
                new MidLevelIR.MLAssign { Target = "y", Source = "15" }
            );

            var result = _optimizer.Optimize(ir, OptimizationLevel.Aggressive);

            Assert.That(result, Is.Not.Null);
            var instructions = result.Modules[0].Functions[0].Instructions;
            Assert.That(instructions.Count, Is.EqualTo(2));
            
            // Should keep only the last assignment to x and the assignment to y
            Assert.That(((MidLevelIR.MLAssign)instructions[0]).Target, Is.EqualTo("x"));
            Assert.That(((MidLevelIR.MLAssign)instructions[0]).Source, Is.EqualTo("10"));
            Assert.That(((MidLevelIR.MLAssign)instructions[1]).Target, Is.EqualTo("y"));
        }

        [Test]
        public void Optimize_WithMixedInstructions_ShouldHandleNonAssignmentInstructions()
        {
            var ir = CreateTestIR(
                new MidLevelIR.MLAssign { Target = "x", Source = "5" },
                new MidLevelIR.MLLabel { Name = "label1" },
                new MidLevelIR.MLAssign { Target = "x", Source = "10" },
                new MidLevelIR.MLBranch { Target = "label2", Condition = "x > 0" },
                new MidLevelIR.MLAssign { Target = "x", Source = "15" }
            );

            var result = _optimizer.Optimize(ir, OptimizationLevel.Basic);

            Assert.That(result, Is.Not.Null);
            var instructions = result.Modules[0].Functions[0].Instructions;
            
            // Should keep: first x assignment (cleared by branch), label, branch, last x assignment
            Assert.That(instructions.Count, Is.EqualTo(5));
        }

        [Test]
        public void Optimize_WithEmptyIR_ShouldReturnEmpty()
        {
            var ir = new MidLevelIR
            {
                SourceFile = "test.mlir",
                Modules = new List<MidLevelIR.MLModule>()
            };

            var result = _optimizer.Optimize(ir, OptimizationLevel.Basic);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Modules.Count, Is.EqualTo(0));
        }

        [Test]
        public void Optimize_WithMultipleFunctions_ShouldOptimizeEach()
        {
            var ir = new MidLevelIR
            {
                SourceFile = "test.mlir",
                Modules = new List<MidLevelIR.MLModule>
                {
                    new MidLevelIR.MLModule
                    {
                        Name = "module1",
                        Functions = new List<MidLevelIR.MLFunction>
                        {
                            new MidLevelIR.MLFunction
                            {
                                Name = "func1",
                                Instructions = new List<MidLevelIR.MLInstruction>
                                {
                                    new MidLevelIR.MLAssign { Target = "x", Source = "(5 + 3)" }
                                }
                            },
                            new MidLevelIR.MLFunction
                            {
                                Name = "func2",
                                Instructions = new List<MidLevelIR.MLInstruction>
                                {
                                    new MidLevelIR.MLAssign { Target = "y", Source = "(10 + 20)" }
                                }
                            }
                        }
                    }
                }
            };

            var result = _optimizer.Optimize(ir, OptimizationLevel.Basic);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Modules.Count, Is.EqualTo(1));
            Assert.That(result.Modules[0].Functions.Count, Is.EqualTo(2));
            
            // Both functions should have optimized assignments
            var func1Instrs = result.Modules[0].Functions[0].Instructions;
            var func2Instrs = result.Modules[0].Functions[1].Instructions;
            
            Assert.That(func1Instrs.Count, Is.EqualTo(1));
            Assert.That(((MidLevelIR.MLAssign)func1Instrs[0]).Source, Is.EqualTo("8"));
            
            Assert.That(func2Instrs.Count, Is.EqualTo(1));
            Assert.That(((MidLevelIR.MLAssign)func2Instrs[0]).Source, Is.EqualTo("30"));
        }

        [Test]
        public void Optimize_WithLargeFunction_ShouldNotHang()
        {
            var instructions = new List<MidLevelIR.MLInstruction>();
            
            // Create a large function with many assignments
            for (int i = 0; i < 1000; i++)
            {
                instructions.Add(new MidLevelIR.MLAssign { Target = $"var{i}", Source = $"({i} + 1)" });
            }

            var ir = CreateTestIR(instructions.ToArray());

            // This should complete quickly without hanging
            var result = _optimizer.Optimize(ir, OptimizationLevel.Basic);

            Assert.That(result, Is.Not.Null);
            var resultInstructions = result.Modules[0].Functions[0].Instructions;
            Assert.That(resultInstructions.Count, Is.EqualTo(1000));
            
            // Verify constant folding was applied
            for (int i = 0; i < 1000; i++)
            {
                var assign = (MidLevelIR.MLAssign)resultInstructions[i];
                Assert.That(assign.Source, Is.EqualTo($"{i + 1}"));
            }
        }

        private static MidLevelIR CreateTestIR(params MidLevelIR.MLInstruction[] instructions)
        {
            return new MidLevelIR
            {
                SourceFile = "test.mlir",
                Modules = new List<MidLevelIR.MLModule>
                {
                    new MidLevelIR.MLModule
                    {
                        Name = "default",
                        Functions = new List<MidLevelIR.MLFunction>
                        {
                            new MidLevelIR.MLFunction
                            {
                                Name = "test_function",
                                Instructions = new List<MidLevelIR.MLInstruction>(instructions)
                            }
                        }
                    }
                }
            };
        }
    }
}
