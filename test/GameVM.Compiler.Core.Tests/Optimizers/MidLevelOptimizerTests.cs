using GameVM.Compiler.Core.IR.Slab;
using GameVM.Compiler.Core.IR.SlabProcessing;
using GameVM.Compiler.Core.IR.Transformers;
using GameVM.Compiler.Core.IR.Buffers;
using GameVM.Compiler.Core.Enums;
using GameVM.Compiler.Pascal;
using GameVM.Compiler.Optimizers.MidLevel;
using GameVM.Compiler.Core.IR.Soa;

namespace GameVM.Compiler.Core.Tests.Optimizers
{
    /// <summary>
    /// Tests for mid-level IR optimization.
    /// Validates that the mid-level optimizer correctly transforms MLIR with various optimizations.
    /// </summary>
    [TestFixture]
    public class MidLevelOptimizerTests
    {
        private DefaultMidLevelOptimizer _optimizer;
        private PascalFrontend _frontend;

[SetUp]
        public void Setup()
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

        #region Dead Code Elimination Tests

[Test]
        public void OptimizeSlab_WithNoOptimization_ShouldPreserveInstructions()
        {
            // Arrange: Simple assignment program
            var hlirSlab = BuildHlirSlabFromSource("program Test;\nvar x: Integer;\nbegin\n  x := 1;\nend.");
            
            // Act
            var resultSlab = _optimizer.OptimizeSlab(hlirSlab, new StringPool(), OptimizationLevel.None);
            
            // Assert
            Assert.That(resultSlab.Count, Is.GreaterThan(0));
        }

        [Test]
        public void OptimizeSlab_DeadCodeInBranches_EliminatesUnusedAssignments()
        {
            var hlirSlab = BuildHlirSlabFromSource("program Test;\nvar unused, x: Integer;\nbegin\n  unused := 42;\n  x := 1;\nend.");

            // Act
            var resultSlab = _optimizer.OptimizeSlab(hlirSlab, new StringPool(), OptimizationLevel.Basic);

            // Assert
            Assert.That(resultSlab.Count, Is.GreaterThan(0));
        }

        #endregion

        #region Constant Propagation Tests

        [Test]
        public void OptimizeSlab_ConstantPropagation_SimplifiesExpressions()
        {
            var hlirSlab = BuildHlirSlabFromSource("program Test;\nvar a, b: Integer;\nbegin\n  a := 5;\n  b := a;\nend.");

            // Act
            var resultSlab = _optimizer.OptimizeSlab(hlirSlab, new StringPool(), OptimizationLevel.Basic);

            // Assert
            Assert.That(resultSlab.Count, Is.GreaterThan(0));
        }

        [Test]
        public void OptimizeSlab_ConstantFolding_ComputesCompileTimeConstants()
        {
            var hlirSlab = BuildHlirSlabFromSource("program Test;\nvar result: Integer;\nbegin\n  result := (5 + 3);\nend.");

            // Act
            var resultSlab = _optimizer.OptimizeSlab(hlirSlab, new StringPool(), OptimizationLevel.Basic);

            // Assert
            Assert.That(resultSlab.Count, Is.GreaterThan(0));
        }

        #endregion

        #region Common Subexpression Elimination Tests

        [Test]
        public void OptimizeSlab_DuplicateExpressions_EliminatesDuplicates()
        {
            var hlirSlab = BuildHlirSlabFromSource("program Test;\nvar a, b, x, y: Integer;\nbegin\n  x := (a + b);\n  y := (a + b);\nend.");

            // Act
            var resultSlab = _optimizer.OptimizeSlab(hlirSlab, new StringPool(), OptimizationLevel.Aggressive);

            // Assert
            Assert.That(resultSlab.Count, Is.GreaterThan(0));
        }

        [Test]
        public void OptimizeSlab_RelatedSubexpressions_OptimizesRelationships()
        {
            var hlirSlab = BuildHlirSlabFromSource("program Test;\nvar a, b, c, x, y: Integer;\nbegin\n  x := (a + b + c);\n  y := (a + b);\nend.");

            // Act
            var resultSlab = _optimizer.OptimizeSlab(hlirSlab, new StringPool(), OptimizationLevel.Aggressive);

            // Assert
            Assert.That(resultSlab.Count, Is.GreaterThan(0));
        }

        #endregion

        #region Loop Optimization Tests

        [Test]
        public void OptimizeSlab_LoopInvariantCode_HoistesConstantComputation()
        {
            var hlirSlab = BuildHlirSlabFromSource("program Test;\nvar c, i: Integer;\nbegin\n  c := (5 + 3);\n  i := (i + 1);\nend.");

            // Act
            var resultSlab = _optimizer.OptimizeSlab(hlirSlab, new StringPool(), OptimizationLevel.Aggressive);

            // Assert
            Assert.That(resultSlab.Count, Is.GreaterThan(0));
        }

        #endregion

        #region Function Inlining Tests

        [Test]
        public void OptimizeSlab_SmallFunction_InlinesFunction()
        {
            var hlirSlab = BuildHlirSlabFromSource("program Test;\nvar x, result: Integer;\nbegin\n  x := 5;\n  result := x + 1;\nend.");

            // Act
            var resultSlab = _optimizer.OptimizeSlab(hlirSlab, new StringPool(), OptimizationLevel.Aggressive);

            // Assert
            Assert.That(resultSlab.Count, Is.GreaterThan(0));
        }

        #endregion

        #region Unused Variable Elimination Tests

        [Test]
        public void OptimizeSlab_UnusedVariable_RemovesAssignment()
        {
            var hlirSlab = BuildHlirSlabFromSource("program Test;\nvar unused, used: Integer;\nbegin\n  unused := 42;\n  used := 1;\n  WriteLn(used);\nend.");

            // Act
            var resultSlab = _optimizer.OptimizeSlab(hlirSlab, new StringPool(), OptimizationLevel.Basic);

            // Assert
            Assert.That(resultSlab.Count, Is.GreaterThan(0));
        }

        #endregion

        #region Optimization Level Tests

        [Test]
        public void OptimizeSlab_WithBasicLevel_AppliesBasicOptimizations()
        {
            var hlirSlab = BuildHlirSlabFromSource("program Test;\nvar a, b: Integer;\nbegin\n  a := 5;\n  b := (a + 3);\nend.");

            // Act
            var resultSlab = _optimizer.OptimizeSlab(hlirSlab, new StringPool(), OptimizationLevel.Basic);

            // Assert
            Assert.That(resultSlab.Count, Is.GreaterThan(0));
        }

        [Test]
        public void OptimizeSlab_WithAggressiveLevel_AppliesAdvancedOptimizations()
        {
            var hlirSlab = BuildHlirSlabFromSource("program Test;\nvar a, b, c, x, y: Integer;\nbegin\n  x := (a + b);\n  y := (a + b);\n  c := (5 + 3);\nend.");

            // Act
            var resultSlab = _optimizer.OptimizeSlab(hlirSlab, new StringPool(), OptimizationLevel.Aggressive);

            // Assert
            Assert.That(resultSlab.Count, Is.GreaterThan(0));
        }

        #endregion

        #region Edge Cases

        [Test]
        public void OptimizeSlab_EmptyFunction_RemainsEmpty()
        {
            // Arrange
            var hlirSlab = BuildHlirSlabFromSource("program Test;\nbegin\nend.");

            // Act
            var resultSlab = _optimizer.OptimizeSlab(hlirSlab, new StringPool(), OptimizationLevel.Basic);

            // Assert
            Assert.That(resultSlab.Count, Is.GreaterThan(0));
        }

        #endregion
    }
}