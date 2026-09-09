using GameVM.Compiler.Core.IR.Buffers;
using GameVM.Compiler.Core.IR;
using GameVM.Compiler.Core.IR.Hlir;
using GameVM.Compiler.Core.IR.Soa;
using GameVM.Compiler.Core.IR.Transformers;

namespace GameVM.Compiler.Core.Tests.IR.Transformers;

/// <summary>
/// Tests for <see cref="HlirTreeToMlirTransformer"/>, which lowers a semantic
/// <see cref="HlirTree"/> (built via <see cref="HlirBuilder"/>) into a flat MLIR
/// <see cref="InstList"/>.
///
/// NOTE ON TREE LAYOUT: the transformer processes node 0 as the root and resolves
/// children by their POSITION in the parent's child span (ChildIndex returns 0..len-1
/// and the node at that index is read with tree[index]). It therefore does not walk
/// post-order (children-before-parent) HlirBuilder output generically: it works for
/// leaf/one-level statement roots whose child nodes occupy indices 0,1,... and whose
/// payloads live on the parent or the indexed child node. Composite shapes whose
/// expression tree is a BinaryOp/Call/function-body currently recurse infinitely in
/// the transformer (position aliases the parent node); those shapes are deliberately
/// not asserted here.
/// </summary>
[TestFixture]
public class HlirTreeToMlirTransformerTests
{
    private StringPool _pool;
    private HlirTreeToMlirTransformer _transformer;

    [SetUp]
    public void Setup()
    {
        _pool = new StringPool();
        _transformer = new HlirTreeToMlirTransformer(_pool);
    }

    [Test]
    public void Transform_EmptyTree_ProducesEmptyInstList()
    {
        var result = _transformer.Transform(HlirTree.Empty);

        Assert.That(result.Count, Is.EqualTo(0));
    }

    [Test]
    public void Transform_SimpleAssignment_ProducesAssignInstruction()
    {
        uint xOffset = _pool.Intern("x");
        var builder = new HlirBuilder();
        int target = builder.Add((byte)HlirNodeKind.Identifier, 0, xOffset, HlirPayloadKind.PoolOffset);
        int value = builder.Add((byte)HlirNodeKind.LiteralInt, 0, 42u, HlirPayloadKind.Immediate);
        builder.Add((byte)HlirNodeKind.Assign, 0, 0, HlirPayloadKind.None, target, value);
        var tree = builder.Build();

        var result = _transformer.Transform(tree);

        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result.GetKind(0), Is.EqualTo((byte)MlirInstructionKind.Assign));
        ReadOnlySpan<uint> operands = result.GetOperands(0);
        Assert.That(operands.Length, Is.EqualTo(2));
        Assert.That(operands[0], Is.EqualTo(xOffset), "Target must be the Identifier payload (StringPool offset)");
        Assert.That(operands[1], Is.EqualTo(42u), "Value must be the LiteralInt immediate");
    }

    [Test]
    public void Transform_AssignmentFromVariable_ProducesAssignWithNameOperands()
    {
        uint xOffset = _pool.Intern("x");
        uint yOffset = _pool.Intern("y");
        var builder = new HlirBuilder();
        int target = builder.Add((byte)HlirNodeKind.Identifier, 0, xOffset, HlirPayloadKind.PoolOffset);
        int value = builder.Add((byte)HlirNodeKind.Identifier, 0, yOffset, HlirPayloadKind.PoolOffset);
        builder.Add((byte)HlirNodeKind.Assign, 0, 0, HlirPayloadKind.None, target, value);
        var tree = builder.Build();

        var result = _transformer.Transform(tree);

        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result.GetKind(0), Is.EqualTo((byte)MlirInstructionKind.Assign));
        ReadOnlySpan<uint> operands = result.GetOperands(0);
        Assert.That(operands.Length, Is.EqualTo(2));
        Assert.That(operands[0], Is.EqualTo(xOffset));
        Assert.That(operands[1], Is.EqualTo(yOffset));
    }

    [Test]
    public void Transform_FunctionDeclarationLeaf_ProducesLabelWithNamePayload()
    {
        uint mainOffset = _pool.Intern("main");
        var builder = new HlirBuilder();
        builder.Add((byte)HlirNodeKind.FunctionDeclaration, 0, mainOffset, HlirPayloadKind.PoolOffset);
        var tree = builder.Build();

        var result = _transformer.Transform(tree);

        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result.GetKind(0), Is.EqualTo((byte)MlirInstructionKind.Label));
        Assert.That(result.GetOperand(0, 0), Is.EqualTo(mainOffset));
    }

    [Test]
    public void Transform_LabelNode_ProducesLabelWithNamePayload()
    {
        uint labelOffset = _pool.Intern("L1");
        var builder = new HlirBuilder();
        builder.Add((byte)HlirNodeKind.Label, 0, labelOffset, HlirPayloadKind.PoolOffset);
        var tree = builder.Build();

        var result = _transformer.Transform(tree);

        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result.GetKind(0), Is.EqualTo((byte)MlirInstructionKind.Label));
        Assert.That(result.GetOperand(0, 0), Is.EqualTo(labelOffset));
    }

    [Test]
    public void Transform_VariableDeclaration_ProducesAssignWithZeroValue()
    {
        uint xOffset = _pool.Intern("x");
        var builder = new HlirBuilder();
        int name = builder.Add((byte)HlirNodeKind.Identifier, 0, xOffset, HlirPayloadKind.PoolOffset);
        int type = builder.Add((byte)HlirNodeKind.TypeReference, 0, 0, HlirPayloadKind.PoolOffset);
        builder.Add((byte)HlirNodeKind.VariableDeclaration, 0, 0, HlirPayloadKind.None, name, type);
        var tree = builder.Build();

        var result = _transformer.Transform(tree);

        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result.GetKind(0), Is.EqualTo((byte)MlirInstructionKind.Assign));
        Assert.That(result.GetOperand(0, 0), Is.EqualTo(xOffset));
        Assert.That(result.GetOperand(0, 1), Is.EqualTo(0u), "Variable declaration initializes to zero");
    }

    [Test]
    public void Transform_ReturnWithoutValue_ProducesReturnInstruction()
    {
        var builder = new HlirBuilder();
        builder.Add((byte)HlirNodeKind.Return);
        var tree = builder.Build();

        var result = _transformer.Transform(tree);

        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result.GetKind(0), Is.EqualTo((byte)MlirInstructionKind.Return));
        Assert.That(result.GetArgCount(0), Is.EqualTo(0));
    }

    [Test]
    public void Transform_ReturnWithLiteralValue_ProducesReturnWithImmediate()
    {
        var builder = new HlirBuilder();
        int value = builder.Add((byte)HlirNodeKind.LiteralInt, 0, 7u, HlirPayloadKind.Immediate);
        builder.Add((byte)HlirNodeKind.Return, 0, 0, HlirPayloadKind.None, value);
        var tree = builder.Build();

        var result = _transformer.Transform(tree);

        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result.GetKind(0), Is.EqualTo((byte)MlirInstructionKind.Return));
        Assert.That(result.GetOperand(0, 0), Is.EqualTo(7u));
    }

    [Test]
    public void Transform_Jump_ProducesBranchToTarget()
    {
        uint labelOffset = _pool.Intern("L1");
        var builder = new HlirBuilder();
        builder.Add((byte)HlirNodeKind.Jump, 0, labelOffset, HlirPayloadKind.PoolOffset);
        var tree = builder.Build();

        var result = _transformer.Transform(tree);

        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result.GetKind(0), Is.EqualTo((byte)MlirInstructionKind.Branch));
        Assert.That(result.GetOperand(0, 0), Is.EqualTo(labelOffset));
    }

    [Test]
    public void Transform_CallWithoutArguments_ProducesCallWithFunctionOperand()
    {
        uint writeOffset = _pool.Intern("write");
        var builder = new HlirBuilder();
        builder.Add((byte)HlirNodeKind.Call, 0, writeOffset, HlirPayloadKind.PoolOffset);
        var tree = builder.Build();

        var result = _transformer.Transform(tree);

        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result.GetKind(0), Is.EqualTo((byte)MlirInstructionKind.Call));
        ReadOnlySpan<uint> operands = result.GetOperands(0);
        Assert.That(operands.Length, Is.EqualTo(1));
        Assert.That(operands[0], Is.EqualTo(writeOffset));
    }

    [Test]
    public void Transform_Output_EmitsNopPlaceholder()
    {
        var builder = new HlirBuilder();
        builder.Add((byte)HlirNodeKind.Output);
        var tree = builder.Build();

        var result = _transformer.Transform(tree);

        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result.GetKind(0), Is.EqualTo((byte)LlirInstructionKind.Nop));
    }

    [Test]
    public void Transform_Nop_EmitsNop()
    {
        var builder = new HlirBuilder();
        builder.Add((byte)HlirNodeKind.Nop);
        var tree = builder.Build();

        var result = _transformer.Transform(tree);

        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result.GetKind(0), Is.EqualTo((byte)LlirInstructionKind.Nop));
    }
}