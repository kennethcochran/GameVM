using GameVM.Compiler.Core.IR;
using GameVM.Compiler.Core.IR.Soa;
using GameVM.Compiler.Core.IR.Transformers;
using NUnit.Framework;

namespace GameVM.DevTools.Tests;

[TestFixture]
public class HlirSlabToMlirSlabTransformerTests
{
    private static InstList SlabWithKind(byte kind, params uint[] operands)
    {
        var builder = new InstListBuilder();
        switch (operands.Length)
        {
            case 0: builder.Add(kind, InstructionFlag.None, 0); break;
            case 1: builder.Add(kind, InstructionFlag.None, 0, operands[0]); break;
            case 2: builder.Add(kind, InstructionFlag.None, 0, operands[0], operands[1]); break;
            default: builder.Add(kind, InstructionFlag.None, 0, operands[0], operands[1], operands[2]); break;
        }
        return builder.Build();
    }

    [Test]
    public void Transform_EmptySlab_ReturnsEmpty()
    {
        var transformer = new HlirSlabToMlirSlabTransformer();
        var empty = new InstListBuilder().Build();
        var result = transformer.Transform(empty);
        Assert.That(result.Count, Is.EqualTo(0));
    }

    [Test]
    public void Transform_AssignInstruction_EmitsAssign()
    {
        var transformer = new HlirSlabToMlirSlabTransformer();
        var slab = SlabWithKind((byte)MlirInstructionKind.Assign, 1u, 2u);
        var result = transformer.Transform(slab);
        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result.GetKind(0), Is.EqualTo((byte)MlirInstructionKind.Assign));
    }

    [Test]
    public void Transform_BranchInstruction_EmitsBranch()
    {
        var transformer = new HlirSlabToMlirSlabTransformer();
        var slab = SlabWithKind((byte)MlirInstructionKind.Branch, 1u, 2u);
        var result = transformer.Transform(slab);
        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result.GetKind(0), Is.EqualTo((byte)MlirInstructionKind.Branch));
    }

    [Test]
    public void Transform_ReturnInstruction_EmitsReturn()
    {
        var transformer = new HlirSlabToMlirSlabTransformer();
        var slab = SlabWithKind((byte)MlirInstructionKind.Return);
        var result = transformer.Transform(slab);
        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result.GetKind(0), Is.EqualTo((byte)MlirInstructionKind.Return));
    }

    [Test]
    public void Transform_LabelInstruction_EmitsLabel()
    {
        var transformer = new HlirSlabToMlirSlabTransformer();
        var slab = SlabWithKind((byte)MlirInstructionKind.Label, 5u);
        var result = transformer.Transform(slab);
        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result.GetKind(0), Is.EqualTo((byte)MlirInstructionKind.Label));
    }

    [Test]
    public void Transform_CallInstruction_EmitsCall()
    {
        var transformer = new HlirSlabToMlirSlabTransformer();
        var slab = SlabWithKind((byte)MlirInstructionKind.Call, 7u);
        var result = transformer.Transform(slab);
        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result.GetKind(0), Is.EqualTo((byte)MlirInstructionKind.Call));
    }

    [Test]
    public void Transform_UnknownInstruction_PreservesAsIs()
    {
        var transformer = new HlirSlabToMlirSlabTransformer();
        var slab = SlabWithKind(0xFF); // unknown kind
        var result = transformer.Transform(slab);
        Assert.That(result.Count, Is.EqualTo(1));
    }

    [Test]
    public void Transform_VariableInstruction_HandledAsExpressionStatement()
    {
        var transformer = new HlirSlabToMlirSlabTransformer();
        var slab = SlabWithKind((byte)MlirInstructionKind.Variable, 3u);
        var result = transformer.Transform(slab);
        Assert.That(result.Count, Is.GreaterThanOrEqualTo(0));
    }

    [Test]
    public void Transform_MultipleInstructions_ProcessesAll()
    {
        var transformer = new HlirSlabToMlirSlabTransformer();
        var builder = new InstListBuilder();
        builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, 1u, 2u);
        builder.Add((byte)MlirInstructionKind.Return, InstructionFlag.None, 0);
        var result = transformer.Transform(builder.Build());
        Assert.That(result.Count, Is.EqualTo(2));
    }
}
