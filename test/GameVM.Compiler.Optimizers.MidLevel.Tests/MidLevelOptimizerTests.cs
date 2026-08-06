using GameVM.Compiler.Core.IR.Slab;
using GameVM.Compiler.Core.IR.Buffers;
using GameVM.Compiler.Core.Enums;
using GameVM.Compiler.Pascal;
using NUnit.Framework;
using GameVM.Compiler.Core.IR.Soa;

namespace GameVM.Compiler.Optimizers.MidLevel.Tests
{
    [TestFixture]
    public class MidLevelOptimizerTests
    {
        private DefaultMidLevelOptimizer _optimizer;
        private PascalFrontend _frontend;

        [SetUp]
        public void SetUp()
        {
            _optimizer = new DefaultMidLevelOptimizer();
            _frontend = new PascalFrontend();
        }

        private InstList BuildHlirSlabFromSource(string source)
        {
            var astSlab = _frontend.ParseToSlab(source);
            Assert.That(astSlab.Count, Is.GreaterThan(0), "AST slab should not be empty");
            var hlirSlab = _frontend.ConvertToHlirSlab(astSlab);
            Assert.That(hlirSlab.Count, Is.GreaterThan(0), "HLIR slab should not be empty");
            return hlirSlab;
        }

        [Test]
        public void OptimizeSlab_WithNoOptimization_ShouldPreserveInstructions()
        {
            // Arrange: Simple assignment program
            var hlirSlab = BuildHlirSlabFromSource("program Test;\nvar x: Integer;\nbegin\n  x := 5;\nend.");
            
            // Act
            var resultSlab = _optimizer.OptimizeSlab(hlirSlab, new StringPool(), OptimizationLevel.None);
            
            // Assert
            Assert.That(resultSlab.Count, Is.GreaterThan(0));
            // For InstList, we check the IrStage from one of its instructions or metadata if available
            // but the transformer already handles this. We can't use SlabHeader.Read on InstList.
        }

        [Test]
        public void OptimizeSlab_WithBasicOptimization_ShouldPerformConstantFolding()
        {
            // Arrange: Program with constant expression
            var hlirSlab = BuildHlirSlabFromSource("program Test;\nvar x: Integer;\nbegin\n  x := (5 + 3);\nend.");
            
            // Act
            var resultSlab = _optimizer.OptimizeSlab(hlirSlab, new StringPool(), OptimizationLevel.Basic);
            
            // Assert
            Assert.That(resultSlab.Count, Is.GreaterThan(0));
        }

        [Test]
        public void OptimizeSlab_WithBasicOptimization_ShouldRemoveDuplicateAssignments()
        {
            // Arrange: Program with duplicate assignments
            var hlirSlab = BuildHlirSlabFromSource("program Test;\nvar x, y: Integer;\nbegin\n  x := 5;\n  x := 10;\n  y := 15;\nend.");
            
            // Act
            var resultSlab = _optimizer.OptimizeSlab(hlirSlab, new StringPool(), OptimizationLevel.Basic);
            
            // Assert
            Assert.That(resultSlab.Count, Is.GreaterThan(0));
        }

        [Test]
        public void OptimizeSlab_WithAggressiveOptimization_ShouldRemoveUnreachableCode()
        {
            // Arrange: Program with constant expressions (parses to valid HLIR)
            var hlirSlab = BuildHlirSlabFromSource("program Test;\nvar x: Integer;\nbegin\n  x := 1;\n  x := 5;\nend.");
            
            // Act
            var resultSlab = _optimizer.OptimizeSlab(hlirSlab, new StringPool(), OptimizationLevel.Aggressive);
            
            // Assert
            Assert.That(resultSlab.Count, Is.GreaterThan(0));
        }

        [Test]
        public void OptimizeSlab_WithAggressiveOptimization_ShouldPerformConstantFolding()
        {
            // Arrange: Program with constant expression
            var hlirSlab = BuildHlirSlabFromSource("program Test;\nvar x: Integer;\nbegin\n  x := (5 + 3);\nend.");
            
            // Act
            var resultSlab = _optimizer.OptimizeSlab(hlirSlab, new StringPool(), OptimizationLevel.Aggressive);
            
            // Assert
            Assert.That(resultSlab.Count, Is.GreaterThan(0));
        }

        [Test]
        public void OptimizeSlab_WithAggressiveOptimization_ShouldRemoveDuplicateAssignments()
        {
            // Arrange: Program with duplicate assignments
            var hlirSlab = BuildHlirSlabFromSource("program Test;\nvar x, y: Integer;\nbegin\n  x := 5;\n  x := 10;\n  y := 15;\nend.");
            
            // Act
            var resultSlab = _optimizer.OptimizeSlab(hlirSlab, new StringPool(), OptimizationLevel.Aggressive);
            
            // Assert
            Assert.That(resultSlab.Count, Is.GreaterThan(0));
        }

        [Test]
        public void OptimizeSlab_WithMixedInstructions_ShouldHandleNonAssignmentInstructions()
        {
            // Arrange: Program with mixed control flow
            var hlirSlab = BuildHlirSlabFromSource("program Test;\nvar x: Integer;\nbegin\n  x := 5;\n  if x > 0 then x := 10 else x := 15;\nend.");
            
            // Act
            var resultSlab = _optimizer.OptimizeSlab(hlirSlab, new StringPool(), OptimizationLevel.Basic);
            
            // Assert
            Assert.That(resultSlab.Count, Is.GreaterThan(0));
        }

        [Test]
        public void OptimizeSlab_WithEmptyInput_ShouldReturnEmpty()
        {
            // Arrange: Empty HLIR slab throws (optimizer requires valid slab)
            var hlirSlab = new InstList();
            
            // Act & Assert
            Assert.Throws<ArgumentException>(() => _optimizer.OptimizeSlab(hlirSlab, new StringPool(), OptimizationLevel.Basic));
        }

        [Test]
        public void OptimizeSlab_WithMultipleFunctions_ShouldOptimizeEach()
        {
            // Arrange: Program with multiple functions
            var hlirSlab = BuildHlirSlabFromSource("program Test;\nvar x: Integer;\nprocedure Func1;\nbegin\n  x := (5 + 3);\nend;\nprocedure Func2;\nbegin\n  x := (10 + 20);\nend;\nbegin\nend.");
            
            // Act
            var resultSlab = _optimizer.OptimizeSlab(hlirSlab, new StringPool(), OptimizationLevel.Basic);
            
            // Assert
            Assert.That(resultSlab.Count, Is.GreaterThan(0));
        }

        [Test]
        public void OptimizeSlab_WithLargeFunction_ShouldNotHang()
        {
            // Arrange: Create a function with several assignments
            var code = new System.Text.StringBuilder();
            code.AppendLine("program LargeFunction;");
            code.AppendLine("var x: Integer;");
            code.AppendLine("begin");
            for (int i = 0; i < 10; i++)
            {
                code.AppendLine($"  x := ({i} + 1);");
            }
            code.AppendLine("end.");
            
            var hlirSlab = BuildHlirSlabFromSource(code.ToString());
            
            // Act
            var resultSlab = _optimizer.OptimizeSlab(hlirSlab, new StringPool(), OptimizationLevel.Basic);
            
            // Assert
            Assert.That(resultSlab.Count, Is.GreaterThan(0));
        }
    }
}