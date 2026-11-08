using GameVM.Compiler.Core.Enums;
using GameVM.Compiler.Core.Exceptions;
using GameVM.Compiler.Core.IR.Buffers;
using GameVM.Compiler.Core.IR;
using GameVM.Compiler.Core.IR.Hlir;
using GameVM.Compiler.Core.IR.Soa;
using GameVM.Compiler.Core.IR.Transformers;
using GameVM.Compiler.Pascal;

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
    public void Transform_Output_EmitsOutputInstruction()
    {
        // Output lowers to the dedicated MLIR Output kind — never a Nop —
        // so backends can distinguish an unlowered construct (must fail
        // loudly on console-less targets) from a benign no-op.
        var builder = new HlirBuilder();
        builder.Add((byte)HlirNodeKind.Output);
        var tree = builder.Build();

        var result = _transformer.Transform(tree);

        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result.GetKind(0), Is.EqualTo((byte)MlirInstructionKind.Output));
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

    [Test]
    public void CaseSelectorMatchesAgainstEachCaseValue()
    {
        // `case x of 1: ...; 2: ...; 3: ...; else ... end` lowers to a chain
        // of selector tests in source order: for each label, Cmp(selector,
        // label) + Branch skipping the arm when the selector differs (the
        // '=' relation's skip branch is BNE, so no Transition marker — the
        // same machinery `if x = n` uses with invert=true), then the arm,
        // then an unconditional Branch to the shared end label. The else
        // arm sits in the fall-through position after the last test, then
        // the end label. Arms stay in source order in the stream.
        //
        // HlirNodeKind.Case does not exist yet: the implementer adds it
        // with the value pinned here (24 — the next free value after
        // Block = 22 and Repeat = 23, claimed by the repeat-until slice in
        // draft PR #24), with children [selector, label, arm, label, arm,
        // ..., (else)] in source order: labels are LiteralInt immediates,
        // arms are Assign statements, the optional else arm is the last
        // child. Replace this constant with (byte)HlirNodeKind.Case once
        // the kind exists.
        const byte caseKind = 24;
        var pool = new StringPool();
        var builder = new HlirBuilder();
        uint xOffset = pool.Intern("x");

        int selector = builder.Add((byte)HlirNodeKind.Identifier, 0, xOffset, HlirPayloadKind.PoolOffset);

        int label1 = builder.Add((byte)HlirNodeKind.LiteralInt, 0, 1u, HlirPayloadKind.Immediate);
        int target1 = builder.Add((byte)HlirNodeKind.Identifier, 0, xOffset, HlirPayloadKind.PoolOffset);
        int value1 = builder.Add((byte)HlirNodeKind.LiteralInt, 0, 10u, HlirPayloadKind.Immediate);
        int arm1 = builder.Add((byte)HlirNodeKind.Assign, 0, 0, HlirPayloadKind.None, target1, value1);

        int label2 = builder.Add((byte)HlirNodeKind.LiteralInt, 0, 2u, HlirPayloadKind.Immediate);
        int target2 = builder.Add((byte)HlirNodeKind.Identifier, 0, xOffset, HlirPayloadKind.PoolOffset);
        int value2 = builder.Add((byte)HlirNodeKind.LiteralInt, 0, 20u, HlirPayloadKind.Immediate);
        int arm2 = builder.Add((byte)HlirNodeKind.Assign, 0, 0, HlirPayloadKind.None, target2, value2);

        int label3 = builder.Add((byte)HlirNodeKind.LiteralInt, 0, 3u, HlirPayloadKind.Immediate);
        int target3 = builder.Add((byte)HlirNodeKind.Identifier, 0, xOffset, HlirPayloadKind.PoolOffset);
        int value3 = builder.Add((byte)HlirNodeKind.LiteralInt, 0, 30u, HlirPayloadKind.Immediate);
        int arm3 = builder.Add((byte)HlirNodeKind.Assign, 0, 0, HlirPayloadKind.None, target3, value3);

        int elseTarget = builder.Add((byte)HlirNodeKind.Identifier, 0, xOffset, HlirPayloadKind.PoolOffset);
        int elseValue = builder.Add((byte)HlirNodeKind.LiteralInt, 0, 40u, HlirPayloadKind.Immediate);
        int elseArm = builder.Add((byte)HlirNodeKind.Assign, 0, 0, HlirPayloadKind.None, elseTarget, elseValue);

        // Case node: 8 children — selector, then label/arm pairs in source
        // order, then the else arm.
        builder.Add(caseKind, 0, 0, HlirPayloadKind.None,
            selector, label1, arm1, label2, arm2, label3, arm3, elseArm);

        var tree = builder.Build();
        var transformer = new HlirTreeToMlirTransformer(pool);
        var result = transformer.Transform(tree);

        Assert.That(result.Count, Is.EqualTo(17),
            "Case lowers to: per label (Cmp, Branch, Assign, Branch), a label before each later test, else Assign, end Label");
        Assert.That(result.GetKind(0), Is.EqualTo((byte)LlirInstructionKind.Cmp), "Index 0: compare selector against label 1");
        Assert.That(result.GetKind(1), Is.EqualTo((byte)MlirInstructionKind.Branch), "Index 1: skip arm 1 when the selector differs");
        Assert.That(result.GetKind(2), Is.EqualTo((byte)MlirInstructionKind.Assign), "Index 2: arm 1 assignment");
        Assert.That(result.GetKind(3), Is.EqualTo((byte)MlirInstructionKind.Branch), "Index 3: arm 1 jumps to the shared end");
        Assert.That(result.GetKind(4), Is.EqualTo((byte)MlirInstructionKind.Label), "Index 4: next-test label");
        Assert.That(result.GetKind(5), Is.EqualTo((byte)LlirInstructionKind.Cmp), "Index 5: compare selector against label 2");
        Assert.That(result.GetKind(6), Is.EqualTo((byte)MlirInstructionKind.Branch), "Index 6: skip arm 2 when the selector differs");
        Assert.That(result.GetKind(7), Is.EqualTo((byte)MlirInstructionKind.Assign), "Index 7: arm 2 assignment");
        Assert.That(result.GetKind(8), Is.EqualTo((byte)MlirInstructionKind.Branch), "Index 8: arm 2 jumps to the shared end");
        Assert.That(result.GetKind(9), Is.EqualTo((byte)MlirInstructionKind.Label), "Index 9: next-test label");
        Assert.That(result.GetKind(10), Is.EqualTo((byte)LlirInstructionKind.Cmp), "Index 10: compare selector against label 3");
        Assert.That(result.GetKind(11), Is.EqualTo((byte)MlirInstructionKind.Branch), "Index 11: skip arm 3 when the selector differs");
        Assert.That(result.GetKind(12), Is.EqualTo((byte)MlirInstructionKind.Assign), "Index 12: arm 3 assignment");
        Assert.That(result.GetKind(13), Is.EqualTo((byte)MlirInstructionKind.Branch), "Index 13: arm 3 jumps to the shared end");
        Assert.That(result.GetKind(14), Is.EqualTo((byte)MlirInstructionKind.Label), "Index 14: else label");
        Assert.That(result.GetKind(15), Is.EqualTo((byte)MlirInstructionKind.Assign), "Index 15: else assignment");
        Assert.That(result.GetKind(16), Is.EqualTo((byte)MlirInstructionKind.Label), "Index 16: end label");

        // Each compare tests the selector against its label, in source order.
        Assert.That(result.GetOperands(0).ToArray(), Is.EqualTo(new uint[] { xOffset, 1u }),
            "First compare must test the selector against label 1");
        Assert.That(result.GetOperands(5).ToArray(), Is.EqualTo(new uint[] { xOffset, 2u }),
            "Second compare must test the selector against label 2");
        Assert.That(result.GetOperands(10).ToArray(), Is.EqualTo(new uint[] { xOffset, 3u }),
            "Third compare must test the selector against label 3");

        // Each arm stores its own constant to x.
        Assert.That(result.GetOperands(2).ToArray(), Is.EqualTo(new uint[] { xOffset, 10u }),
            "Arm 1 must assign 10 to x");
        Assert.That(result.GetOperands(7).ToArray(), Is.EqualTo(new uint[] { xOffset, 20u }),
            "Arm 2 must assign 20 to x");
        Assert.That(result.GetOperands(12).ToArray(), Is.EqualTo(new uint[] { xOffset, 30u }),
            "Arm 3 must assign 30 to x");
        Assert.That(result.GetOperands(15).ToArray(), Is.EqualTo(new uint[] { xOffset, 40u }),
            "Else arm must assign 40 to x");

        // Branch wiring: each skip branch lands on the label starting the
        // next test (or the else arm for the last label); every arm's jump
        // lands on the shared end label; all four labels differ.
        uint next1Label = result.GetOperands(4)[0];
        uint next2Label = result.GetOperands(9)[0];
        uint elseLabel = result.GetOperands(14)[0];
        uint endLabel = result.GetOperands(16)[0];
        Assert.That(next1Label, Is.Not.EqualTo(next2Label));
        Assert.That(next1Label, Is.Not.EqualTo(elseLabel));
        Assert.That(next1Label, Is.Not.EqualTo(endLabel));
        Assert.That(next2Label, Is.Not.EqualTo(elseLabel));
        Assert.That(next2Label, Is.Not.EqualTo(endLabel));
        Assert.That(elseLabel, Is.Not.EqualTo(endLabel));
        Assert.That(result.GetOperands(1)[0], Is.EqualTo(next1Label),
            "skipping arm 1 must land on the label starting test 2");
        Assert.That(result.GetOperands(6)[0], Is.EqualTo(next2Label),
            "skipping arm 2 must land on the label starting test 3");
        Assert.That(result.GetOperands(11)[0], Is.EqualTo(elseLabel),
            "skipping arm 3 must land on the else arm");
        Assert.That(result.GetOperands(3)[0], Is.EqualTo(endLabel),
            "arm 1 must jump to the shared end label");
        Assert.That(result.GetOperands(8)[0], Is.EqualTo(endLabel),
            "arm 2 must jump to the shared end label");
        Assert.That(result.GetOperands(13)[0], Is.EqualTo(endLabel),
            "arm 3 must jump to the shared end label");
    }

    [Test]
    public void Transform_UnaryMinusVariable_LowersToSubFromZero()
    {
        // HLIR -> MLIR seam: x := -y lowers to the temp sequence
        //   Sub(0, y); Assign(__tmp_0, 0); Assign(x, __tmp_0)
        // i.e. -y == 0 - y, reusing the existing Sub lowering.
        uint xOffset = _pool.Intern("x");
        uint yOffset = _pool.Intern("y");
        uint zeroOffset = _pool.Intern("0");
        var builder = new HlirBuilder();
        int operand = builder.Add((byte)HlirNodeKind.Identifier, 0, yOffset, HlirPayloadKind.PoolOffset);
        int unary = builder.Add((byte)HlirNodeKind.UnaryOp, 0, (uint)'-', HlirPayloadKind.Immediate, operand);
        int target = builder.Add((byte)HlirNodeKind.Identifier, 0, xOffset, HlirPayloadKind.PoolOffset);
        builder.Add((byte)HlirNodeKind.Assign, 0, 0, HlirPayloadKind.None, target, unary);
        var tree = builder.Build();

        var result = _transformer.Transform(tree);

        uint tmpOffset = _pool.Intern("__tmp_0");
        Assert.That(result.Count, Is.EqualTo(3),
            "x := -y should lower to Sub; Assign(tmp,0); Assign(x,tmp)");

        Assert.That(result.GetKind(0), Is.EqualTo((byte)LlirInstructionKind.Sub));
        var subOps = result.GetOperands(0);
        Assert.That(subOps.Length, Is.EqualTo(2));
        Assert.That(subOps[0], Is.EqualTo(zeroOffset), "Sub left operand is literal 0");
        Assert.That(subOps[1], Is.EqualTo(yOffset), "Sub right operand is y");

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
    public void Transform_UnaryMinusNonMinusOp_ThrowsFailLoud()
    {
        // Only '-' is produced by the frontend; any other op char must fail
        // loudly rather than silently lowering to nothing (or a bogus Sub).
        uint yOffset = _pool.Intern("y");
        var builder = new HlirBuilder();
        int operand = builder.Add((byte)HlirNodeKind.Identifier, 0, yOffset, HlirPayloadKind.PoolOffset);
        builder.Add((byte)HlirNodeKind.UnaryOp, 0, (uint)'!', HlirPayloadKind.Immediate, operand);
        var tree = builder.Build();

        var ex = Assert.Throws<GameVmException>(() => _transformer.Transform(tree));
        Assert.That(ex!.ErrorCode, Is.EqualTo(ErrorCode.UnhandledInstruction));
    }

    [Test]
    public void Transform_BareBinaryOpStatement_EvaluatesExpression()
    {
        // A bare expression in statement position (no ExpressionStatement
        // wrapper, no Block) is evaluated and its result discarded — the
        // semantics of an expression statement.
        uint xOffset = _pool.Intern("x");
        uint oneOffset = _pool.Intern("1");
        var builder = new HlirBuilder();
        int leftX = builder.Add((byte)HlirNodeKind.Identifier, 0, xOffset, HlirPayloadKind.PoolOffset);
        int rightOne = builder.Add((byte)HlirNodeKind.LiteralInt, 0, oneOffset, HlirPayloadKind.Immediate);
        builder.Add((byte)HlirNodeKind.BinaryOp, 0, (uint)'-', HlirPayloadKind.Immediate, leftX, rightOne);
        var tree = builder.Build();

        var result = _transformer.Transform(tree);

        Assert.That(result.Count, Is.EqualTo(2), "x - 1 as a statement lowers to Sub; Assign(tmp,0)");
        Assert.That(result.GetKind(0), Is.EqualTo((byte)LlirInstructionKind.Sub));
    }

    [Test]
    public void Transform_UnhandledNodeKind_ThrowsFailLoud()
    {
        // A statement-position node with no lowering (here a TypeReference,
        // which is only meaningful inside a declaration) must fail loudly
        // at the HLIR -> MLIR boundary, never vanish silently.
        uint intOffset = _pool.Intern("Integer");
        var builder = new HlirBuilder();
        builder.Add((byte)HlirNodeKind.TypeReference, 0, intOffset, HlirPayloadKind.PoolOffset);
        var tree = builder.Build();

        var ex = Assert.Throws<GameVmException>(() => _transformer.Transform(tree));
        Assert.That(ex!.ErrorCode, Is.EqualTo(ErrorCode.UnhandledInstruction));
        Assert.That(ex.Message, Does.Contain("TypeReference"));
    }

    [Test]
    public void Transform_ReturnInsideNestedFunction_IsFlaggedCalleeReturn()
    {
        // The outermost FunctionDeclaration is the program entry; a Return
        // inside a nested function body must carry CalleeReturn so the
        // backend emits RTS instead of the entry halt.
        uint progOffset = _pool.Intern("Prog");
        uint fnOffset = _pool.Intern("Fn");
        var builder = new HlirBuilder();
        int ret = builder.Add((byte)HlirNodeKind.Return);
        int fnBlock = builder.Add((byte)HlirNodeKind.Block, 0, 0, HlirPayloadKind.None, ret);
        int fn = builder.Add((byte)HlirNodeKind.FunctionDeclaration, 0, fnOffset, HlirPayloadKind.PoolOffset, fnBlock);
        int progBlock = builder.Add((byte)HlirNodeKind.Block, 0, 0, HlirPayloadKind.None, fn);
        builder.Add((byte)HlirNodeKind.FunctionDeclaration, 0, progOffset, HlirPayloadKind.PoolOffset, progBlock);
        var tree = builder.Build();

        var result = _transformer.Transform(tree);

        int returnIdx = -1;
        for (int i = 0; i < result.Count; i++)
            if (result.GetKind(i) == (byte)MlirInstructionKind.Return) { returnIdx = i; break; }
        Assert.That(returnIdx, Is.GreaterThanOrEqualTo(0));
        Assert.That((InstructionFlag)result.GetFlags(returnIdx), Is.EqualTo(InstructionFlag.CalleeReturn));
    }

    [Test]
    public void Transform_NestedFunctionDeclaration_IsBranchedOverOnEntryPath()
    {
        // A nested callable declared before the program's own statements
        // must not execute on the entry path: labels emit no code, so
        // without a skip the entry would fall through into the function
        // body and hit its CalleeReturn (RTS) with an empty stack. The
        // lowering must emit an unconditional Branch over the body,
        // targeting a label placed immediately after it.
        uint progOffset = _pool.Intern("Prog");
        uint fnOffset = _pool.Intern("Fn");
        var builder = new HlirBuilder();
        int fnRet = builder.Add((byte)HlirNodeKind.Return);
        int fnBlock = builder.Add((byte)HlirNodeKind.Block, 0, 0, HlirPayloadKind.None, fnRet);
        int fn = builder.Add((byte)HlirNodeKind.FunctionDeclaration, 0, fnOffset, HlirPayloadKind.PoolOffset, fnBlock);
        int progRet = builder.Add((byte)HlirNodeKind.Return);
        int progBlock = builder.Add((byte)HlirNodeKind.Block, 0, 0, HlirPayloadKind.None, fn, progRet);
        builder.Add((byte)HlirNodeKind.FunctionDeclaration, 0, progOffset, HlirPayloadKind.PoolOffset, progBlock);
        var tree = builder.Build();

        var result = _transformer.Transform(tree);

        // Expected stream: Label(Prog), Branch(after), Label(Fn),
        // Return(CalleeReturn), Label(after), Return(entry).
        Assert.That(result.Count, Is.EqualTo(6));
        Assert.That(result.GetKind(0), Is.EqualTo((byte)MlirInstructionKind.Label));
        Assert.That(result.GetKind(1), Is.EqualTo((byte)MlirInstructionKind.Branch));
        Assert.That(result.GetKind(2), Is.EqualTo((byte)MlirInstructionKind.Label));
        Assert.That(result.GetOperands(2)[0], Is.EqualTo(fnOffset));
        Assert.That(result.GetKind(3), Is.EqualTo((byte)MlirInstructionKind.Return));
        Assert.That((InstructionFlag)result.GetFlags(3), Is.EqualTo(InstructionFlag.CalleeReturn));
        Assert.That(result.GetKind(4), Is.EqualTo((byte)MlirInstructionKind.Label));
        Assert.That(result.GetOperands(1)[0], Is.EqualTo(result.GetOperands(4)[0]),
            "The skip Branch must target the label placed after the function body");
        Assert.That(result.GetKind(5), Is.EqualTo((byte)MlirInstructionKind.Return));
        Assert.That((InstructionFlag)result.GetFlags(5), Is.EqualTo(InstructionFlag.None));
    }

    [Test]
    public void LiteralBoolConditionBranchesOnlyWhenTrue()
    {
        // A boolean-literal condition must branch (skip the then-arm) only
        // when it is false. The HLIR is built by the real frontend, so the
        // condition is whatever the frontend produces. Today the literal is
        // dropped to a Nop by the visitor, and the AST-to-HLIR transformer
        // has no lowering for a Nop condition (BuildExpression returns -1),
        // so the whole if statement is silently dropped and the then-arm
        // never reaches the MLIR at all. After the fix the payload is 1
        // for true and 0 for false, and the LiteralBool case branches only
        // when false.
        const string falseProgram =
            "program Test;\nvar x: Integer;\nbegin\n  if false then x := 1;\nend.";
        const string trueProgram =
            "program Test;\nvar x: Integer;\nbegin\n  if true then x := 1;\nend.";

        var falseMlir = ToMlir(falseProgram, out uint falseX, out uint falseOne);
        int falseAssign = FindAssign(falseMlir, falseX, falseOne);
        int falseBranch = FindBranchBefore(falseMlir, falseAssign);
        int falseEndLabel = FindLabelAfter(falseMlir, falseAssign);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(falseAssign, Is.GreaterThanOrEqualTo(0), "the then-arm assignment must be emitted");
            Assert.That(falseBranch, Is.GreaterThanOrEqualTo(0),
                "if false must emit a branch skipping the then-arm");
            Assert.That(falseEndLabel, Is.GreaterThanOrEqualTo(0), "the if must end in a label");
            Assert.That(falseMlir.GetOperands(falseBranch)[0], Is.EqualTo(falseMlir.GetOperands(falseEndLabel)[0]),
                "the skip branch must target the label placed after the then-arm");
        }

        var trueMlir = ToMlir(trueProgram, out uint trueX, out uint trueOne);
        int trueAssign = FindAssign(trueMlir, trueX, trueOne);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(trueAssign, Is.GreaterThanOrEqualTo(0), "the then-arm assignment must be emitted");
            Assert.That(FindBranchBefore(trueMlir, trueAssign), Is.EqualTo(-1),
                "if true must fall straight through into the then-arm (no skip branch)");
        }
    }

    [Test]
    public void LiteralBoolConditionInIfElseBranchesToElseArmOnlyWhenFalse()
    {
        // if/else with a literal condition: the branch must fire exactly
        // when the literal is false, sending control to the else-arm. The
        // branch lowers to an unconditional jump (no Cmp precedes it), so
        // no polarity applies — unlike a comparison, there is no BEQ/BNE
        // to flip.
        const string falseProgram =
            "program Test;\nvar x: Integer;\nbegin\n  if false then x := 1 else x := 2;\nend.";
        const string trueProgram =
            "program Test;\nvar x: Integer;\nbegin\n  if true then x := 1 else x := 2;\nend.";

        var falseMlir = ToMlir(falseProgram, out uint falseX, out uint falseOne);
        int falseThen = FindAssign(falseMlir, falseX, falseOne);
        int falseBranch = FindBranchBefore(falseMlir, falseThen);
        int falseElseLabel = FindLabelAfter(falseMlir, falseThen);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(falseThen, Is.GreaterThanOrEqualTo(0), "the then-arm assignment must be emitted");
            Assert.That(falseBranch, Is.GreaterThanOrEqualTo(0),
                "if false must emit a branch to the else-arm before the then-arm");
            Assert.That(falseElseLabel, Is.GreaterThanOrEqualTo(0), "the else-arm must be labelled");
            Assert.That(falseMlir.GetOperands(falseBranch)[0], Is.EqualTo(falseMlir.GetOperands(falseElseLabel)[0]),
                "the branch must target the label placed after the then-arm");
        }

        var trueMlir = ToMlir(trueProgram, out uint trueX, out uint trueOne);
        int trueThen = FindAssign(trueMlir, trueX, trueOne);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(trueThen, Is.GreaterThanOrEqualTo(0), "the then-arm assignment must be emitted");
            Assert.That(FindBranchBefore(trueMlir, trueThen), Is.EqualTo(-1),
                "if true must fall straight through into the then-arm (no branch to the else-arm)");
        }
    }

    private static InstList ToMlir(string source, out uint xOffset, out uint oneOffset)
    {
        var frontend = new PascalFrontend();
        var parsed = frontend.ParseToHlir(source);
        Assert.That(parsed.Hlir.Count, Is.GreaterThan(0), "frontend must produce HLIR");

        xOffset = frontend.StringPool!.Intern("x");
        oneOffset = frontend.StringPool.Intern("1");
        return new HlirTreeToMlirTransformer(frontend.StringPool).Transform(parsed.Hlir);
    }

    private static int FindAssign(InstList mlir, uint target, uint value)
    {
        for (int i = 0; i < mlir.Count; i++)
        {
            if ((MlirInstructionKind)mlir.GetKind(i) != MlirInstructionKind.Assign) continue;
            var ops = mlir.GetOperands(i);
            if (ops.Length == 2 && ops[0] == target && ops[1] == value) return i;
        }
        return -1;
    }

    private static int FindBranchBefore(InstList mlir, int beforeIdx)
    {
        for (int i = 0; i < beforeIdx; i++)
            if ((MlirInstructionKind)mlir.GetKind(i) == MlirInstructionKind.Branch) return i;
        return -1;
    }

    private static int FindLabelAfter(InstList mlir, int afterIdx)
    {
        for (int i = afterIdx + 1; i < mlir.Count; i++)
            if ((MlirInstructionKind)mlir.GetKind(i) == MlirInstructionKind.Label) return i;
        return -1;
    }

    [Test]
    public void Transform_ReturnAtProgramEntry_IsNotFlaggedCalleeReturn()
    {
        uint progOffset = _pool.Intern("Prog");
        var builder = new HlirBuilder();
        int ret = builder.Add((byte)HlirNodeKind.Return);
        int progBlock = builder.Add((byte)HlirNodeKind.Block, 0, 0, HlirPayloadKind.None, ret);
        builder.Add((byte)HlirNodeKind.FunctionDeclaration, 0, progOffset, HlirPayloadKind.PoolOffset, progBlock);
        var tree = builder.Build();

        var result = _transformer.Transform(tree);

        int returnIdx = -1;
        for (int i = 0; i < result.Count; i++)
            if (result.GetKind(i) == (byte)MlirInstructionKind.Return) { returnIdx = i; break; }
        Assert.That(returnIdx, Is.GreaterThanOrEqualTo(0));
        Assert.That((InstructionFlag)result.GetFlags(returnIdx), Is.EqualTo(InstructionFlag.None));
    }

    [Test]
    public void LessThanEmitsCorrectBranch()
    {
        // An if-skip on '<' must lower to a conditional branch distinct
        // from the '=' and '<>' encodings. Today '<' emits exactly the '='
        // stream (Cmp + Branch), so `if x < 5` behaves as `if x = 5`. The
        // exact new encoding (a carry-based branch kind, a flag, or an
        // operand) is the implementer's; the ROM test pins the 6502 opcode
        // it must become. This slice covers '<' only; '>', '<=', '>=' are
        // separate slices.
        byte[] equalStream = IfKindStream('=');
        byte[] notEqualStream = IfKindStream('!');

        Assert.That(equalStream, Is.EqualTo(new byte[]
        {
            (byte)LlirInstructionKind.Cmp,
            (byte)MlirInstructionKind.Branch,
            (byte)MlirInstructionKind.Assign,
            (byte)MlirInstructionKind.Label,
        }), "the '=' baseline this test compares against must be Cmp + Branch + Assign + Label");

        byte[] stream = IfKindStream('<');
        Assert.That(stream.Length, Is.GreaterThanOrEqualTo(4),
            "'<' must lower to at least Cmp + Branch + Assign + Label");
        Assert.That(stream[0], Is.EqualTo((byte)LlirInstructionKind.Cmp),
            "'<' must start with a compare");
        Assert.That(stream, Does.Contain((byte)MlirInstructionKind.Branch),
            "'<' must emit a conditional Branch");
        Assert.That(stream, Does.Contain((byte)MlirInstructionKind.Assign),
            "'<' must keep the then-arm assignment after the branch");
        Assert.That(stream, Is.Not.EqualTo(equalStream),
            "'<' must not lower like '='");
        Assert.That(stream, Is.Not.EqualTo(notEqualStream),
            "'<' must not lower like '<>'");
    }

    [Test]
    public void IfCondition_EqualityComparison_KeepsExistingBranchEncoding()
    {
        // Guard: '=' in an if-skip keeps its established encoding, Cmp +
        // Branch with no Transition; adding ordering comparisons must not
        // disturb it.
        byte[] stream = IfKindStream('=');
        Assert.That(stream, Is.EqualTo(new byte[]
        {
            (byte)LlirInstructionKind.Cmp,
            (byte)MlirInstructionKind.Branch,
            (byte)MlirInstructionKind.Assign,
            (byte)MlirInstructionKind.Label,
        }));
    }

    /// <summary>Lowers `if x op 0 then x := 0` (no else) and returns the
    /// emitted instruction kinds in order.</summary>
    private byte[] IfKindStream(char op)
    {
        uint xOffset = _pool.Intern("x");
        uint zeroOffset = _pool.Intern("0");
        var builder = new HlirBuilder();
        int leftX = builder.Add((byte)HlirNodeKind.Identifier, 0, xOffset, HlirPayloadKind.PoolOffset);
        int rightZero = builder.Add((byte)HlirNodeKind.LiteralInt, 0, zeroOffset, HlirPayloadKind.Immediate);
        int cond = builder.Add((byte)HlirNodeKind.BinaryOp, 0, (uint)op, HlirPayloadKind.Immediate, leftX, rightZero);
        int target = builder.Add((byte)HlirNodeKind.Identifier, 0, xOffset, HlirPayloadKind.PoolOffset);
        int value = builder.Add((byte)HlirNodeKind.LiteralInt, 0, zeroOffset, HlirPayloadKind.Immediate);
        int assign = builder.Add((byte)HlirNodeKind.Assign, 0, 0, HlirPayloadKind.None, target, value);
        builder.Add((byte)HlirNodeKind.If, 0, 0, HlirPayloadKind.None, cond, assign);
        var tree = builder.Build();

        var result = _transformer.Transform(tree);
        var kinds = new byte[result.Count];
        for (int i = 0; i < result.Count; i++)
            kinds[i] = result.GetKind(i);
        return kinds;
    }
}