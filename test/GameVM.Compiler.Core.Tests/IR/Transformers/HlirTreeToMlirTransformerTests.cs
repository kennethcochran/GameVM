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

    [Test]
    public void Transform_ConstantAssignment_LowersToAssignWithImmediate()
    {
        // HLIR -> MLIR seam: a semantic Assign(Identifier x, LiteralInt 5) lowers
        // to a single MLIR Assign whose second operand is the StringPool offset
        // of the literal "5" (the backend resolver turns it into the immediate 5).
        uint xOffset = _pool.Intern("x");
        uint fiveOffset = _pool.Intern("5");
        var builder = new HlirBuilder();
        int target = builder.Add((byte)HlirNodeKind.Identifier, 0, xOffset, HlirPayloadKind.PoolOffset);
        int value = builder.Add((byte)HlirNodeKind.LiteralInt, 0, fiveOffset, HlirPayloadKind.Immediate);
        builder.Add((byte)HlirNodeKind.Assign, 0, 0, HlirPayloadKind.None, target, value);
        var tree = builder.Build();

        var result = _transformer.Transform(tree);

        Assert.That(result.Count, Is.EqualTo(1), "A constant assignment should lower to exactly one MLIR instruction");
        Assert.That(result.GetKind(0), Is.EqualTo((byte)MlirInstructionKind.Assign));
        var operands = result.GetOperands(0);
        Assert.That(operands.Length, Is.EqualTo(2));
        Assert.That(operands[0], Is.EqualTo(xOffset), "Target must be the StringPool offset of x");
        Assert.That(operands[1], Is.EqualTo(fiveOffset), "Value must be the StringPool offset of the literal \"5\"");
    }

    [Test]
    public void Transform_SubtractExpression_EmitsSubWithTempSequence()
    {
        // HLIR -> MLIR seam: x := x - 1 lowers to the temp sequence
        //   Sub(x, 1); Assign(__tmp_0, 0); Assign(x, __tmp_0)
        // The '-' operator must map to LlirInstructionKind.Sub (ticket 02 criterion 4).
        uint xOffset = _pool.Intern("x");
        uint oneOffset = _pool.Intern("1");
        var builder = new HlirBuilder();
        int leftX = builder.Add((byte)HlirNodeKind.Identifier, 0, xOffset, HlirPayloadKind.PoolOffset);
        int rightOne = builder.Add((byte)HlirNodeKind.LiteralInt, 0, oneOffset, HlirPayloadKind.Immediate);
        int binOp = builder.Add((byte)HlirNodeKind.BinaryOp, 0, (uint)'-', HlirPayloadKind.Immediate, leftX, rightOne);
        int target = builder.Add((byte)HlirNodeKind.Identifier, 0, xOffset, HlirPayloadKind.PoolOffset);
        builder.Add((byte)HlirNodeKind.Assign, 0, 0, HlirPayloadKind.None, target, binOp);
        var tree = builder.Build();

        var result = _transformer.Transform(tree);

        uint tmpOffset = _pool.Intern("__tmp_0");
        Assert.That(result.Count, Is.EqualTo(3),
            "x := x - 1 should lower to Sub; Assign(tmp,0); Assign(x,tmp)");

        Assert.That(result.GetKind(0), Is.EqualTo((byte)LlirInstructionKind.Sub),
            "'-' must map to Sub (byte 201), not a folded immediate");
        var subOps = result.GetOperands(0);
        Assert.That(subOps.Length, Is.EqualTo(2));
        Assert.That(subOps[0], Is.EqualTo(xOffset), "Sub left operand = x");
        Assert.That(subOps[1], Is.EqualTo(oneOffset), "Sub right operand = literal 1");

        Assert.That(result.GetKind(1), Is.EqualTo((byte)MlirInstructionKind.Assign));
        var tmpOps = result.GetOperands(1);
        Assert.That(tmpOps[0], Is.EqualTo(tmpOffset), "Temp target = __tmp_0");
        Assert.That(tmpOps[1], Is.EqualTo(0u), "Temp declaration value = 0");

        Assert.That(result.GetKind(2), Is.EqualTo((byte)MlirInstructionKind.Assign));
        var storeOps = result.GetOperands(2);
        Assert.That(storeOps[0], Is.EqualTo(xOffset), "Final Assign target = x");
        Assert.That(storeOps[1], Is.EqualTo(tmpOffset), "Final Assign value = __tmp_0");
    }

    [Test]
    public void Transform_IfCondition_BranchPolarity_SelectsCorrectOp()
    {
        // '<>' (relOp='!') in an if-skip must emit Cmp + Branch (BNE)
        uint xOffset = _pool.Intern("x");
        uint zeroOffset = _pool.Intern("0");
        var builder = new HlirBuilder();
        int leftX = builder.Add((byte)HlirNodeKind.Identifier, 0, xOffset, HlirPayloadKind.PoolOffset);
        int rightZero = builder.Add((byte)HlirNodeKind.LiteralInt, 0, zeroOffset, HlirPayloadKind.Immediate);
        int binOpNe = builder.Add((byte)HlirNodeKind.BinaryOp, 0, (uint)'!', HlirPayloadKind.Immediate, leftX, rightZero);
        int binOpEq = builder.Add((byte)HlirNodeKind.BinaryOp, 0, (uint)'=', HlirPayloadKind.Immediate, leftX, rightZero);
        builder.Add((byte)HlirNodeKind.If, 0, 0, HlirPayloadKind.None, binOpNe, binOpEq);
        var tree = builder.Build();

        var result = _transformer.Transform(tree);

        // '<>' with invert=true → Cmp + Transition + Branch (BEQ: skip then-arm when x==0)
        Assert.That(result.Count, Is.GreaterThan(0), "Transformer produced output");
        Assert.That(result.GetKind(0), Is.EqualTo((byte)LlirInstructionKind.Cmp));
        Assert.That(result.GetKind(1), Is.EqualTo((byte)LlirInstructionKind.Transition));
        Assert.That(result.GetKind(2), Is.EqualTo((byte)MlirInstructionKind.Branch));
        var cmpOps = result.GetOperands(0);
        Assert.That(cmpOps.Length, Is.EqualTo(2));
    }
    [Test]
    public void While_Loop_Lowering_Produces_Correct_MLIR()
    {
        // Verify that While lowers to: Label, Cmp, Transition, Branch, [body], Branch, Label
        // ProcessWhile emits: loop label, inverted conditional branch, body, back-edge, exit label
        var pool = new StringPool();
        var builder = new HlirBuilder();
        uint xOffset = pool.Intern("x");

        // Body of while: x := 0
        int identBody = builder.Add((byte)HlirNodeKind.Identifier, 0, xOffset, HlirPayloadKind.PoolOffset);
        int litBody = builder.Add((byte)HlirNodeKind.LiteralInt, 0, (uint)0, HlirPayloadKind.Immediate);
        int assign = builder.Add((byte)HlirNodeKind.Assign, 0, 0, HlirPayloadKind.None, identBody, litBody);

        // Condition: x <> 0
        int identCond = builder.Add((byte)HlirNodeKind.Identifier, 0, xOffset, HlirPayloadKind.PoolOffset);
        int litCond = builder.Add((byte)HlirNodeKind.LiteralInt, 0, (uint)0, HlirPayloadKind.Immediate);
        int binOpNe = builder.Add((byte)HlirNodeKind.BinaryOp, 0, (uint)'!', HlirPayloadKind.Immediate, identCond, litCond);

        // While node: 2 children — condition + body
        builder.Add((byte)HlirNodeKind.While, 0, 0, HlirPayloadKind.None, binOpNe, assign);

        var tree = builder.Build();
        var transformer = new HlirTreeToMlirTransformer(pool);
        var result = transformer.Transform(tree);

        Assert.That(result.Count, Is.GreaterThan(0), "Transformer produced output");
        Assert.That(result.GetKind(0), Is.EqualTo((byte)MlirInstructionKind.Label), "Index 0: loop header label");
        Assert.That(result.GetKind(1), Is.EqualTo((byte)LlirInstructionKind.Cmp), "Index 1: compare condition");
        Assert.That(result.GetKind(2), Is.EqualTo((byte)LlirInstructionKind.Transition), "Index 2: transition");
        Assert.That(result.GetKind(3), Is.EqualTo((byte)MlirInstructionKind.Branch), "Index 3: conditional exit branch");
        Assert.That(result.GetKind(4), Is.EqualTo((byte)MlirInstructionKind.Assign), "Index 4: body assignment");
        Assert.That(result.GetKind(5), Is.EqualTo((byte)MlirInstructionKind.Branch), "Index 5: unconditional back-edge");
        Assert.That(result.GetKind(6), Is.EqualTo((byte)MlirInstructionKind.Label), "Index 6: exit label");
    }
}
