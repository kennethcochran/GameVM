using GameVM.Compiler.Core.IR;
using GameVM.Compiler.Core.IR.Soa;
using GameVM.Compiler.Core.IR.Transformers;

namespace GameVM.Compiler.Core.Tests.IR.Transformers
{
    [TestFixture]
    public class HlirSlabToMlirSlabTransformerTests
    {
        private HlirSlabToMlirSlabTransformer _transformer;

        [SetUp]
        public void Setup()
        {
            _transformer = new HlirSlabToMlirSlabTransformer();
        }

        [Test]
        public void Transform_EmptySlab_ReturnsEmpty()
        {
            var empty = new InstList();
            var result = _transformer.Transform(empty);
            Assert.That(result.Count, Is.EqualTo(0));
        }

        [Test]
        public void Transform_LabelInstruction_DoesNotThrow()
        {
            var builder = new InstListBuilder();
            builder.Add((byte)MlirInstructionKind.Label, InstructionFlag.None, 0, 1u);
            var slab = builder.Build();

            Assert.DoesNotThrow(() => _transformer.Transform(slab));
        }

        [Test]
        public void Transform_AssignInstruction_DoesNotThrow()
        {
            var builder = new InstListBuilder();
            builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, 1u, 2u);
            var slab = builder.Build();

            Assert.DoesNotThrow(() => _transformer.Transform(slab));
        }

        [Test]
        public void Transform_BranchInstruction_DoesNotThrow()
        {
            var builder = new InstListBuilder();
            builder.Add((byte)MlirInstructionKind.Branch, InstructionFlag.None, 0, 1u);
            var slab = builder.Build();

            Assert.DoesNotThrow(() => _transformer.Transform(slab));
        }

        [Test]
        public void Transform_CallInstruction_DoesNotThrow()
        {
            var builder = new InstListBuilder();
            builder.Add((byte)MlirInstructionKind.Call, InstructionFlag.None, 0, 1u);
            var slab = builder.Build();

            Assert.DoesNotThrow(() => _transformer.Transform(slab));
        }

        [Test]
        public void Transform_ReturnInstruction_DoesNotThrow()
        {
            var builder = new InstListBuilder();
            builder.Add((byte)MlirInstructionKind.Return, InstructionFlag.None, 0);
            var slab = builder.Build();

            Assert.DoesNotThrow(() => _transformer.Transform(slab));
        }

        [Test]
        public void Transform_VariableInstruction_DoesNotThrow()
        {
            var builder = new InstListBuilder();
            builder.Add((byte)MlirInstructionKind.Variable, InstructionFlag.None, 0);
            var slab = builder.Build();

            Assert.DoesNotThrow(() => _transformer.Transform(slab));
        }

        [Test]
        public void Transform_ExpressionStatement_DoesNotThrow()
        {
            var builder = new InstListBuilder();
            builder.Add((byte)MlirInstructionKind.ExpressionStatement, InstructionFlag.None, 0);
            var slab = builder.Build();

            Assert.DoesNotThrow(() => _transformer.Transform(slab));
        }

        [Test]
        public void Transform_UnknownInstruction_PreservesAsIs()
        {
            var builder = new InstListBuilder();
            builder.Add(255, InstructionFlag.None, 0);
            var slab = builder.Build();

            var result = _transformer.Transform(slab);
            Assert.That(result.Count, Is.EqualTo(1));
        }
    }
}
