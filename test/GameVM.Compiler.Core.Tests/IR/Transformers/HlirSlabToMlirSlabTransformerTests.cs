using GameVM.Compiler.Core.IR.Transformers;
using GameVM.Compiler.Core.IR.Slab;
using GameVM.Compiler.Core.IR.Buffers;
using GameVM.Compiler.Pascal;
using GameVM.Compiler.Core.IR.Soa;

namespace GameVM.Compiler.Core.Tests.IR.Transformers
{
    public class HlirSlabToMlirSlabTransformerTests
    {
        private readonly HlirSlabToMlirSlabTransformer _transformer;
        private readonly PascalFrontend _frontend = new PascalFrontend();

        public HlirSlabToMlirSlabTransformerTests()
        {
            _transformer = new HlirSlabToMlirSlabTransformer();
        }

        [Test]
        public void Transform_SimpleVariableAssignment_ProducesValidMlirSlab()
        {
            var sourceCode = "program Test;\nvar x: Integer;\nbegin\n  x := 42;\nend.";
            var astSlab = _frontend.ParseToSlab(sourceCode);
            var hlirSlab = _frontend.ConvertToHlirSlab(astSlab);
            Assert.That(hlirSlab.Count, Is.GreaterThan(0));

            InstList result = _transformer.Transform(hlirSlab);

            Assert.That(result.Count, Is.GreaterThan(0), "Result should not be empty");
            
            // Check that we have MLIR instructions (kind >= 128)
            bool foundMlir = false;
            for (int i = 0; i < result.Count; i++)
            {
                if (result.GetKind(i) >= 128)
                {
                    foundMlir = true;
                    break;
                }
            }
            Assert.That(foundMlir, Is.True, "Should find MLIR instructions");
        }

        [Test]
        public void Transform_WithIfStatement_ProducesValidMlirSlab()
        {
            var sourceCode = "program Test;\nvar x: Integer;\nbegin\n  if x > 0 then x := 1;\nend.";
            var astSlab = _frontend.ParseToSlab(sourceCode);
            var hlirSlab = _frontend.ConvertToHlirSlab(astSlab);
            Assert.That(hlirSlab.Count, Is.GreaterThan(0));

            InstList result = _transformer.Transform(hlirSlab);

            Assert.That(result.Count, Is.GreaterThan(0));
        }

        [Test]
        public void Transform_WithWhileLoop_ProducesValidMlirSlab()
        {
            var sourceCode = "program Test;\nvar i: Integer;\nbegin\n  i := 0;\n  while i < 10 do\n    i := i + 1;\nend.";
            var astSlab = _frontend.ParseToSlab(sourceCode);
            var hlirSlab = _frontend.ConvertToHlirSlab(astSlab);
            Assert.That(hlirSlab.Count, Is.GreaterThan(0));

            InstList result = _transformer.Transform(hlirSlab);

            Assert.That(result.Count, Is.GreaterThan(0));
        }

        [Test]
        public void Transform_WithForLoop_ProducesValidMlirSlab()
        {
            var sourceCode = "program Test;\nvar i, sum: Integer;\nbegin\n  sum := 0;\n  for i := 1 to 10 do\n    sum := sum + i;\nend.";
            var astSlab = _frontend.ParseToSlab(sourceCode);
            var hlirSlab = _frontend.ConvertToHlirSlab(astSlab);
            Assert.That(hlirSlab.Count, Is.GreaterThan(0));

            InstList result = _transformer.Transform(hlirSlab);

            Assert.That(result.Count, Is.GreaterThan(0));
        }

        [Test]
        public void Transform_MultipleVariables_ProducesValidMlirSlab()
        {
            var sourceCode = "program Test;\nvar a, b, c: Integer;\nbegin\n  a := 1;\n  b := 2;\n  c := a + b;\nend.";
            var astSlab = _frontend.ParseToSlab(sourceCode);
            var hlirSlab = _frontend.ConvertToHlirSlab(astSlab);
            Assert.That(hlirSlab.Count, Is.GreaterThan(0));

            InstList result = _transformer.Transform(hlirSlab);

            Assert.That(result.Count, Is.GreaterThan(0));
        }

        [Test]
        public void Transform_EmptySlab_ProducesEmptySlab()
        {
            var emptySlab = new InstList(
                    Array.Empty<byte>(),
                    Array.Empty<ushort>(),
                    Array.Empty<ushort>(),
                    Array.Empty<uint>(),
                    Array.Empty<uint>(),
                    Array.Empty<uint>(),
                    Array.Empty<int>(),
                    0,
                    0);
            var result = _transformer.Transform(emptySlab);
            Assert.That(result.Count, Is.EqualTo(0));
        }

        [Test]
        public void Transform_WithLabel_ProducesValidMlirSlab()
        {
            var sourceCode = "program Test;\nlabel 1;\nvar x: Integer;\nbegin\n  if x > 0 then\n    begin\n      x := 1;\n      goto 1;\n    end;\n1:\n  x := 2;\nend.";
            var astSlab = _frontend.ParseToSlab(sourceCode);
            var hlirSlab = _frontend.ConvertToHlirSlab(astSlab);
            Assert.That(hlirSlab.Count, Is.GreaterThan(0));

            InstList result = _transformer.Transform(hlirSlab);

            Assert.That(result.Count, Is.GreaterThan(0));
        }
    }
}