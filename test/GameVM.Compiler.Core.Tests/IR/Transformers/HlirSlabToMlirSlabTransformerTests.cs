using GameVM.Compiler.Core.IR.Transformers;
using GameVM.Compiler.Core.IR.Slab;
using GameVM.Compiler.Core.IR.SlabProcessing;
using GameVM.Compiler.Core.IR.Buffers;
using GameVM.Compiler.Pascal;

namespace GameVM.Compiler.Core.Tests.IR.Transformers
{
    public class HlirSlabToMlirSlabTransformerTests
    {
        private readonly ArenaAllocator _arena = new ArenaAllocator();
        private readonly HlirSlabToMlirSlabTransformer _transformer;
        private readonly PascalFrontend _frontend = new PascalFrontend();

        public HlirSlabToMlirSlabTransformerTests()
        {
            _transformer = new HlirSlabToMlirSlabTransformer(_arena);
        }

        [Test]
        public void Transform_SimpleVariableAssignment_ProducesValidMlirSlab()
        {
            var sourceCode = "program Test;\nvar x: Integer;\nbegin\n  x := 42;\nend.";
            var astSlab = _frontend.ParseToSlab(sourceCode);
            var hlirSlab = _frontend.ConvertToHlirSlab(astSlab);
            Assert.That(hlirSlab, Is.Not.Null.And.Not.Empty);

            uint[] result = _transformer.Transform(hlirSlab, new StringPool());

            var resultHeader = SlabHeader.Read(result);
            Assert.That(resultHeader.IrStage, Is.EqualTo(2u), "Result should be MLIR (stage 2)");
            Assert.That(result.Length, Is.GreaterThan(SlabHeader.HeaderIndex.Length));
        }

        [Test]
        public void Transform_WithIfStatement_ProducesValidMlirSlab()
        {
            var sourceCode = "program Test;\nvar x: Integer;\nbegin\n  if x > 0 then x := 1;\nend.";
            var astSlab = _frontend.ParseToSlab(sourceCode);
            var hlirSlab = _frontend.ConvertToHlirSlab(astSlab);
            Assert.That(hlirSlab, Is.Not.Null.And.Not.Empty);

            uint[] result = _transformer.Transform(hlirSlab, new StringPool());

            var resultHeader = SlabHeader.Read(result);
            Assert.That(resultHeader.IrStage, Is.EqualTo(2u));
            Assert.That(result.Length, Is.GreaterThan(SlabHeader.HeaderIndex.Length));
        }

        [Test]
        public void Transform_WithWhileLoop_ProducesValidMlirSlab()
        {
            var sourceCode = "program Test;\nvar i: Integer;\nbegin\n  i := 0;\n  while i < 10 do\n    i := i + 1;\nend.";
            var astSlab = _frontend.ParseToSlab(sourceCode);
            var hlirSlab = _frontend.ConvertToHlirSlab(astSlab);
            Assert.That(hlirSlab, Is.Not.Null.And.Not.Empty);

            uint[] result = _transformer.Transform(hlirSlab, new StringPool());

            var resultHeader = SlabHeader.Read(result);
            Assert.That(resultHeader.IrStage, Is.EqualTo(2u));
            Assert.That(result.Length, Is.GreaterThan(SlabHeader.HeaderIndex.Length));
        }

        [Test]
        public void Transform_WithForLoop_ProducesValidMlirSlab()
        {
            var sourceCode = "program Test;\nvar i, sum: Integer;\nbegin\n  sum := 0;\n  for i := 1 to 10 do\n    sum := sum + i;\nend.";
            var astSlab = _frontend.ParseToSlab(sourceCode);
            var hlirSlab = _frontend.ConvertToHlirSlab(astSlab);
            Assert.That(hlirSlab, Is.Not.Null.And.Not.Empty);

            uint[] result = _transformer.Transform(hlirSlab, new StringPool());

            var resultHeader = SlabHeader.Read(result);
            Assert.That(resultHeader.IrStage, Is.EqualTo(2u));
            Assert.That(result.Length, Is.GreaterThan(SlabHeader.HeaderIndex.Length));
        }

        [Test]
        public void Transform_MultipleVariables_ProducesValidMlirSlab()
        {
            var sourceCode = "program Test;\nvar a, b, c: Integer;\nbegin\n  a := 1;\n  b := 2;\n  c := a + b;\nend.";
            var astSlab = _frontend.ParseToSlab(sourceCode);
            var hlirSlab = _frontend.ConvertToHlirSlab(astSlab);
            Assert.That(hlirSlab, Is.Not.Null.And.Not.Empty);

            uint[] result = _transformer.Transform(hlirSlab, new StringPool());

            var resultHeader = SlabHeader.Read(result);
            Assert.That(resultHeader.IrStage, Is.EqualTo(2u));
            Assert.That(result.Length, Is.GreaterThan(SlabHeader.HeaderIndex.Length));
        }

        [Test]
        public void Transform_EmptySlab_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _transformer.Transform(new uint[0], new StringPool()));
        }

        [Test]
        public void Transform_NullSlab_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _transformer.Transform(null!, new StringPool()));
        }

        [Test]
        public void Transform_InvalidMagic_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _transformer.Transform(new uint[] { 0x12345678, 1, 0, 0, 1, 0 }, new StringPool()));
        }
    }
}