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
    public void MultiplicationLowersToMultiplyNotAdd()
    {
        // HLIR -> MLIR seam: x := y * z lowers to the temp sequence
        //   Multiply(y, z); Assign(__tmp_0, 0); Assign(x, __tmp_0)
        // Both operands are variables, not literals: only literal-vs-literal
        // multiplication folds correctly (in the constant folder), so this
        // is the shape the bug bites. Today '*' maps to
        // LlirInstructionKind.Add, so a * b computes a + b. The fix adds
        // LlirInstructionKind.Multiply — the next free value after
        // Assign (208) — and maps '*' to it; the backend then lowers it to
        // a real 8-bit multiply routine.
        uint xOffset = _pool.Intern("x");
        uint yOffset = _pool.Intern("y");
        uint zOffset = _pool.Intern("z");
        var builder = new HlirBuilder();
        int leftY = builder.Add((byte)HlirNodeKind.Identifier, 0, yOffset, HlirPayloadKind.PoolOffset);
        int rightZ = builder.Add((byte)HlirNodeKind.Identifier, 0, zOffset, HlirPayloadKind.PoolOffset);
        int binOp = builder.Add((byte)HlirNodeKind.BinaryOp, 0, (uint)'*', HlirPayloadKind.Immediate, leftY, rightZ);
        int target = builder.Add((byte)HlirNodeKind.Identifier, 0, xOffset, HlirPayloadKind.PoolOffset);
        builder.Add((byte)HlirNodeKind.Assign, 0, 0, HlirPayloadKind.None, target, binOp);
        var tree = builder.Build();

        var result = _transformer.Transform(tree);

        const byte multiplyKind = 210; // LlirInstructionKind.Multiply, added by this slice (Bcs took 209 on main via PR #18)
        uint tmpOffset = _pool.Intern("__tmp_0");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Count, Is.EqualTo(3),
                "x := y * z should lower to Multiply; Assign(tmp,0); Assign(x,tmp)");

            Assert.That(result.GetKind(0), Is.EqualTo(multiplyKind),
                "'*' must map to Multiply (byte 210), not Add — a * b is a product, not a sum");
            var mulOps = result.GetOperands(0);
            Assert.That(mulOps.Length, Is.EqualTo(2));
            Assert.That(mulOps[0], Is.EqualTo(yOffset), "Multiply left operand = y");
            Assert.That(mulOps[1], Is.EqualTo(zOffset), "Multiply right operand = z");

            Assert.That(result.GetKind(1), Is.EqualTo((byte)MlirInstructionKind.Assign));
            var tmpOps = result.GetOperands(1);
            Assert.That(tmpOps[0], Is.EqualTo(tmpOffset), "Temp target = __tmp_0");
            Assert.That(tmpOps[1], Is.Zero, "Temp declaration value = 0");

            Assert.That(result.GetKind(2), Is.EqualTo((byte)MlirInstructionKind.Assign));
            var storeOps = result.GetOperands(2);
            Assert.That(storeOps[0], Is.EqualTo(xOffset), "Final Assign target = x");
            Assert.That(storeOps[1], Is.EqualTo(tmpOffset), "Final Assign value = __tmp_0");
        }
    }

    [Test]
    public void Transform_AddExpression_EmitsAddWithTempSequence()
    {
        // Guard: re-pointing '*' at Multiply must not disturb '+', which
        // keeps lowering to Add. Same temp sequence as the Sub test:
        //   Add(y, z); Assign(__tmp_0, 0); Assign(x, __tmp_0)
        uint xOffset = _pool.Intern("x");
        uint yOffset = _pool.Intern("y");
        uint zOffset = _pool.Intern("z");
        var builder = new HlirBuilder();
        int leftY = builder.Add((byte)HlirNodeKind.Identifier, 0, yOffset, HlirPayloadKind.PoolOffset);
        int rightZ = builder.Add((byte)HlirNodeKind.Identifier, 0, zOffset, HlirPayloadKind.PoolOffset);
        int binOp = builder.Add((byte)HlirNodeKind.BinaryOp, 0, (uint)'+', HlirPayloadKind.Immediate, leftY, rightZ);
        int target = builder.Add((byte)HlirNodeKind.Identifier, 0, xOffset, HlirPayloadKind.PoolOffset);
        builder.Add((byte)HlirNodeKind.Assign, 0, 0, HlirPayloadKind.None, target, binOp);
        var tree = builder.Build();

        var result = _transformer.Transform(tree);

        uint tmpOffset = _pool.Intern("__tmp_0");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Count, Is.EqualTo(3),
                "x := y + z should lower to Add; Assign(tmp,0); Assign(x,tmp)");

            Assert.That(result.GetKind(0), Is.EqualTo((byte)LlirInstructionKind.Add),
                "'+' must keep mapping to Add");
            var addOps = result.GetOperands(0);
            Assert.That(addOps.Length, Is.EqualTo(2));
            Assert.That(addOps[0], Is.EqualTo(yOffset), "Add left operand = y");
            Assert.That(addOps[1], Is.EqualTo(zOffset), "Add right operand = z");

            Assert.That(result.GetKind(1), Is.EqualTo((byte)MlirInstructionKind.Assign));
            var tmpOps = result.GetOperands(1);
            Assert.That(tmpOps[0], Is.EqualTo(tmpOffset), "Temp target = __tmp_0");
            Assert.That(tmpOps[1], Is.Zero, "Temp declaration value = 0");

            Assert.That(result.GetKind(2), Is.EqualTo((byte)MlirInstructionKind.Assign));
            var storeOps = result.GetOperands(2);
            Assert.That(storeOps[0], Is.EqualTo(xOffset), "Final Assign target = x");
            Assert.That(storeOps[1], Is.EqualTo(tmpOffset), "Final Assign value = __tmp_0");
        }
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
        // Only '-' and '!' are produced by the frontend; any other op char
        // must fail loudly rather than silently lowering to nothing (or a
        // bogus Sub). ('!' used to be the unsupported char here; the
        // bitwise slice made it a supported unary op, so the test moved
        // to '~', a char no frontend production emits.)
        uint yOffset = _pool.Intern("y");
        var builder = new HlirBuilder();
        int operand = builder.Add((byte)HlirNodeKind.Identifier, 0, yOffset, HlirPayloadKind.PoolOffset);
        builder.Add((byte)HlirNodeKind.UnaryOp, 0, (uint)'~', HlirPayloadKind.Immediate, operand);
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

    [Test]
    public void AndOrNotLowerToBitwiseOperations()
    {
        // `and`/`or` in value position must lower to a two-operand bitwise
        // operation, and `not` to a complement — never to the comparison
        // path. Today ArithToMlirKind knows only +,-,*,/ so '&'/'|' fall
        // through to Cmp + Assign(tmp, 1): the value 1, whatever the bits
        // are. And UnaryOp '!' throws UnhandledInstruction.
        //
        // The new bitwise kind values are the implementer's:
        // compiler-assigned, never pinned as numbers here (the kind enum
        // has no And/Or/Not members on main yet, so naming them would not
        // compile). The test pins the stream shape — a two-operand compute
        // instruction that is none of the existing kinds, feeding the usual
        // temp sequence — and the ROM test pins the 6502 opcodes the new
        // kinds must become (AND $25, ORA $05, EOR #$FF $49).
        //
        // Note: this slice retires the premise of
        // Transform_UnaryMinusNonMinusOp_ThrowsFailLoud, which pins that
        // UnaryOp '!' throws. After the fix '!' is a supported unary op,
        // so that test must move to a genuinely unsupported op char (e.g.
        // '~') when this slice is implemented.
        using (Assert.EnterMultipleScope())
        {
            AssertBitwiseBinaryOp('&', "and");
            AssertBitwiseBinaryOp('|', "or");
            AssertNotLowers();
        }
    }

    [Test]
    public void EqualityInValuePosition_KeepsComparisonPath()
    {
        // Guard: wiring `and`/`or` to bitwise ops must not disturb the
        // comparison path — `x := y = z` still lowers to Cmp + Assign(tmp, 1)
        // + Assign(x, tmp), exactly as today.
        uint xOffset = _pool.Intern("x");
        uint yOffset = _pool.Intern("y");
        uint zOffset = _pool.Intern("z");
        uint tmpOffset = _pool.Intern("__tmp_0");
        var result = LowerAssignWithBinaryOp('=');

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Count, Is.EqualTo(3),
                "x := y = z should lower to Cmp; Assign(tmp,1); Assign(x,tmp)");

            Assert.That(result.GetKind(0), Is.EqualTo((byte)LlirInstructionKind.Cmp));
            var cmpOps = result.GetOperands(0);
            Assert.That(cmpOps.Length, Is.EqualTo(2));
            Assert.That(cmpOps[0], Is.EqualTo(yOffset), "Cmp left operand = y");
            Assert.That(cmpOps[1], Is.EqualTo(zOffset), "Cmp right operand = z");

            Assert.That(result.GetKind(1), Is.EqualTo((byte)MlirInstructionKind.Assign));
            var tmpOps = result.GetOperands(1);
            Assert.That(tmpOps[0], Is.EqualTo(tmpOffset), "Temp target = __tmp_0");
            Assert.That(tmpOps[1], Is.EqualTo(1u), "Temp declaration value = 1 (comparison result)");

            Assert.That(result.GetKind(2), Is.EqualTo((byte)MlirInstructionKind.Assign));
            var storeOps = result.GetOperands(2);
            Assert.That(storeOps[0], Is.EqualTo(xOffset), "Final Assign target = x");
            Assert.That(storeOps[1], Is.EqualTo(tmpOffset), "Final Assign value = __tmp_0");
        }
    }

    /// <summary>Instruction kinds a bitwise operation must not be: the
    /// comparison path (Cmp), the arithmetic ops (Add/Sub), and the
    /// control/structural kinds. This list names only what the op must
    /// not be, so no new kind value is pinned here — the implementer
    /// assigns the new bitwise kind values.</summary>
    private static readonly byte[] NonBitwiseKinds =
    {
        (byte)LlirInstructionKind.Cmp,
        (byte)LlirInstructionKind.Add,
        (byte)LlirInstructionKind.Sub,
        (byte)MlirInstructionKind.Assign,
        (byte)MlirInstructionKind.Branch,
        (byte)MlirInstructionKind.Label,
        (byte)MlirInstructionKind.Return,
        (byte)MlirInstructionKind.Call,
        (byte)MlirInstructionKind.Nop,
        (byte)LlirInstructionKind.Transition,
        (byte)LlirInstructionKind.Bcs,
    };

    /// <summary>Lowers `x := y op z` with a fresh transformer (so the temp
    /// is always __tmp_0) and returns the MLIR stream.</summary>
    private InstList LowerAssignWithBinaryOp(char op)
    {
        uint xOffset = _pool.Intern("x");
        uint yOffset = _pool.Intern("y");
        uint zOffset = _pool.Intern("z");
        var builder = new HlirBuilder();
        int leftY = builder.Add((byte)HlirNodeKind.Identifier, 0, yOffset, HlirPayloadKind.PoolOffset);
        int rightZ = builder.Add((byte)HlirNodeKind.Identifier, 0, zOffset, HlirPayloadKind.PoolOffset);
        int binOp = builder.Add((byte)HlirNodeKind.BinaryOp, 0, (uint)op, HlirPayloadKind.Immediate, leftY, rightZ);
        int target = builder.Add((byte)HlirNodeKind.Identifier, 0, xOffset, HlirPayloadKind.PoolOffset);
        builder.Add((byte)HlirNodeKind.Assign, 0, 0, HlirPayloadKind.None, target, binOp);
        var tree = builder.Build();

        return new HlirTreeToMlirTransformer(_pool).Transform(tree);
    }

    private void AssertBitwiseBinaryOp(char op, string name)
    {
        uint xOffset = _pool.Intern("x");
        uint yOffset = _pool.Intern("y");
        uint zOffset = _pool.Intern("z");
        uint tmpOffset = _pool.Intern("__tmp_0");
        var result = LowerAssignWithBinaryOp(op);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Count, Is.EqualTo(3),
                $"x := y {name} z should lower to <bitwise-op>; Assign(tmp,0); Assign(x,tmp)");

            byte opKind = result.GetKind(0);
            Assert.That(NonBitwiseKinds, Does.Not.Contain(opKind),
                $"'{op}' must lower to a bitwise operation, not the comparison path (Cmp), an arithmetic op (Add/Sub), or a control op");
            var opOperands = result.GetOperands(0);
            Assert.That(opOperands.Length, Is.EqualTo(2), "a bitwise op takes exactly two operands");
            Assert.That(opOperands[0], Is.EqualTo(yOffset), $"{name} left operand = y");
            Assert.That(opOperands[1], Is.EqualTo(zOffset), $"{name} right operand = z");

            Assert.That(result.GetKind(1), Is.EqualTo((byte)MlirInstructionKind.Assign));
            var tmpOps = result.GetOperands(1);
            Assert.That(tmpOps[0], Is.EqualTo(tmpOffset), "Temp target = __tmp_0");
            Assert.That(tmpOps[1], Is.Zero, "Temp declaration value = 0");

            Assert.That(result.GetKind(2), Is.EqualTo((byte)MlirInstructionKind.Assign));
            var storeOps = result.GetOperands(2);
            Assert.That(storeOps[0], Is.EqualTo(xOffset), "Final Assign target = x");
            Assert.That(storeOps[1], Is.EqualTo(tmpOffset), "Final Assign value = __tmp_0");
        }
    }

    private void AssertNotLowers()
    {
        uint xOffset = _pool.Intern("x");
        uint yOffset = _pool.Intern("y");
        uint tmpOffset = _pool.Intern("__tmp_0");
        var builder = new HlirBuilder();
        int operand = builder.Add((byte)HlirNodeKind.Identifier, 0, yOffset, HlirPayloadKind.PoolOffset);
        int unary = builder.Add((byte)HlirNodeKind.UnaryOp, 0, (uint)'!', HlirPayloadKind.Immediate, operand);
        int target = builder.Add((byte)HlirNodeKind.Identifier, 0, xOffset, HlirPayloadKind.PoolOffset);
        builder.Add((byte)HlirNodeKind.Assign, 0, 0, HlirPayloadKind.None, target, unary);
        var tree = builder.Build();

        InstList result = default;
        GameVmException? error = null;
        try
        {
            result = new HlirTreeToMlirTransformer(_pool).Transform(tree);
        }
        catch (GameVmException ex)
        {
            error = ex;
        }
        Assert.That(error, Is.Null,
            "x := not y must lower without throwing; today UnaryOp '!' throws UnhandledInstruction");
        if (error != null) return;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Count, Is.EqualTo(3),
                "x := not y should lower to <complement>; Assign(tmp,0); Assign(x,tmp)");

            byte opKind = result.GetKind(0);
            Assert.That(NonBitwiseKinds, Does.Not.Contain(opKind),
                "'!' must lower to a complement operation, not the comparison path (Cmp) or an arithmetic op");
            var opOperands = result.GetOperands(0);
            Assert.That(opOperands.ToArray(), Does.Contain(yOffset), "the complement must operate on y");

            Assert.That(result.GetKind(1), Is.EqualTo((byte)MlirInstructionKind.Assign));
            var tmpOps = result.GetOperands(1);
            Assert.That(tmpOps[0], Is.EqualTo(tmpOffset), "Temp target = __tmp_0");
            Assert.That(tmpOps[1], Is.Zero, "Temp declaration value = 0");

            Assert.That(result.GetKind(2), Is.EqualTo((byte)MlirInstructionKind.Assign));
            var storeOps = result.GetOperands(2);
            Assert.That(storeOps[0], Is.EqualTo(xOffset), "Final Assign target = x");
            Assert.That(storeOps[1], Is.EqualTo(tmpOffset), "Final Assign value = __tmp_0");
        }
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

    [Test]
    public void OrUsedAsConditionIsEvaluatedNotCompared()
    {
        // `if (a or b) then` must evaluate the `or` and branch on the
        // result's truth. Today EmitConditionalBranch treats every
        // two-child BinaryOp condition as a comparison and emits
        // Cmp(a, b): with a=1, b=0 the compare leaves Z clear and the
        // skip branch fires, so the then-arm is skipped although the
        // condition is true; with a=0, b=0 it runs although the
        // condition is false.
        AssertBitwiseConditionIsEvaluated("a or b", (byte)LlirInstructionKind.BitwiseOr, "or");
    }

    [Test]
    public void AndUsedAsConditionIsEvaluatedNotCompared()
    {
        // Same defect as the `or` condition: `if (a and b) then`
        // currently compares a against b instead of evaluating the
        // `and` and testing the result against zero.
        AssertBitwiseConditionIsEvaluated("a and b", (byte)LlirInstructionKind.BitwiseAnd, "and");
    }

    [Test]
    public void NotUsedAsConditionIsEvaluatedNotDropped()
    {
        // `if not a then` must evaluate the complement and branch on
        // its truth. Today a UnaryOp condition reaches the default
        // case of EmitConditionalBranch, which emits an unconditional
        // branch: the then-arm never runs, whatever a holds.
        var mlir = CompilePascal(ConditionProgram("if not a then x := 1;"), out var pool);
        uint aOffset = pool.Intern("a");
        uint xOffset = pool.Intern("x");
        uint oneOffset = pool.Intern("1");
        uint tmpOffset = pool.Intern("__tmp_0");

        int notIdx = FindKindIndex(mlir, (byte)LlirInstructionKind.BitwiseNot);
        int cmpIdx = FindCmpIndex(mlir, tmpOffset, 0);
        int branchIdx = FindKindIndex(mlir, (byte)MlirInstructionKind.Branch);
        int assignIdx = FindAssign(mlir, xOffset, oneOffset);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(notIdx, Is.GreaterThanOrEqualTo(0),
                "the not condition must be evaluated to a value (BitwiseNot), not dropped to an unconditional branch");
            if (notIdx >= 0)
                Assert.That(mlir.GetOperands(notIdx).ToArray(), Does.Contain(aOffset),
                    "the complement must operate on a");
            Assert.That(cmpIdx, Is.GreaterThan(notIdx),
                "the evaluated result must be compared against zero");
            Assert.That(branchIdx, Is.GreaterThan(cmpIdx),
                "the skip branch follows the zero test");
            if (branchIdx > 0)
                Assert.That(mlir.GetKind(branchIdx - 1), Is.EqualTo((byte)LlirInstructionKind.Transition),
                    "the skip branch must fire when the result is zero (condition false), so a Transition (BEQ) marker must precede it");
            Assert.That(assignIdx, Is.GreaterThan(branchIdx),
                "the then-arm assignment follows the skip branch");
        }
    }

    [Test]
    public void WhileOrConditionIsEvaluatedNotCompared()
    {
        // The while loop head shares EmitConditionalBranch with the
        // if statement, so `while (a or b) do` miscompiles the same
        // way: Cmp(a, b) decides whether the body runs and whether
        // the loop exits.
        var mlir = CompilePascal(ConditionProgram("while (a or b) do x := 0;"), out var pool);
        uint aOffset = pool.Intern("a");
        uint bOffset = pool.Intern("b");
        uint tmpOffset = pool.Intern("__tmp_0");

        int orIdx = FindKindIndex(mlir, (byte)LlirInstructionKind.BitwiseOr);
        int cmpIdx = FindCmpIndex(mlir, tmpOffset, 0);
        int branchIdx = FindKindIndex(mlir, (byte)MlirInstructionKind.Branch);

        using (Assert.EnterMultipleScope())
        {
            int loopLabelIdx = FindKindIndex(mlir, (byte)MlirInstructionKind.Label, 1);
            Assert.That(loopLabelIdx, Is.GreaterThanOrEqualTo(0),
                "the loop head starts with the loop label");
            Assert.That(loopLabelIdx, Is.LessThan(cmpIdx),
                "the loop label precedes the condition evaluation");
            Assert.That(orIdx, Is.GreaterThanOrEqualTo(0),
                "the or condition must be evaluated to a value (BitwiseOr), not compared");
            Assert.That(FindCmpIndex(mlir, aOffset, bOffset), Is.EqualTo(-1),
                "the condition's operands must never be compared against each other");
            Assert.That(cmpIdx, Is.GreaterThan(orIdx),
                "the evaluated result must be compared against zero");
            Assert.That(branchIdx, Is.GreaterThan(cmpIdx),
                "the exit branch follows the zero test");
            if (branchIdx > 0)
                Assert.That(mlir.GetKind(branchIdx - 1), Is.EqualTo((byte)LlirInstructionKind.Transition),
                    "the exit branch must fire when the result is zero (condition false), so a Transition (BEQ) marker must precede it");
        }
    }

    [Test]
    public void IfElseOrConditionIsEvaluatedNotCompared()
    {
        // The if/else form must also evaluate the condition and test
        // the result against zero. This test pins the evaluation only:
        // the branch polarity of the if/else form is the arm-swap
        // defect, owned by the branch-conditions redesign, and is
        // deliberately not asserted here.
        var mlir = CompilePascal(ConditionProgram("if (a or b) then x := 1 else x := 2;"), out var pool);
        uint aOffset = pool.Intern("a");
        uint bOffset = pool.Intern("b");
        uint xOffset = pool.Intern("x");
        uint oneOffset = pool.Intern("1");
        uint tmpOffset = pool.Intern("__tmp_0");

        int orIdx = FindKindIndex(mlir, (byte)LlirInstructionKind.BitwiseOr);
        int cmpIdx = FindCmpIndex(mlir, tmpOffset, 0);
        int thenIdx = FindAssign(mlir, xOffset, oneOffset);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(orIdx, Is.GreaterThanOrEqualTo(0),
                "the or condition must be evaluated to a value (BitwiseOr), not compared");
            Assert.That(FindCmpIndex(mlir, aOffset, bOffset), Is.EqualTo(-1),
                "the condition's operands must never be compared against each other");
            Assert.That(cmpIdx, Is.GreaterThan(orIdx),
                "the evaluated result must be compared against zero");
            Assert.That(thenIdx, Is.GreaterThan(cmpIdx),
                "the then-arm assignment follows the condition evaluation");
        }
    }

    [Test]
    public void NestedOrAndConditionIsEvaluatedNotCompared()
    {
        // A nested condition must be evaluated as a whole: the outer
        // `and` combines the evaluated `or` result with c, and the
        // branch tests that final result against zero. Today the
        // outer BinaryOp takes the comparison path, so the branch
        // compares the `or` result temp against c.
        var mlir = CompilePascal(ConditionProgram("if ((a or b) and c) then x := 1;"), out var pool);
        uint cOffset = pool.Intern("c");
        uint orTmpOffset = pool.Intern("__tmp_0");
        uint andTmpOffset = pool.Intern("__tmp_1");

        int orIdx = FindKindIndex(mlir, (byte)LlirInstructionKind.BitwiseOr);
        int andIdx = FindKindIndex(mlir, (byte)LlirInstructionKind.BitwiseAnd);
        int cmpIdx = FindCmpIndex(mlir, andTmpOffset, 0);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(orIdx, Is.GreaterThanOrEqualTo(0),
                "the inner or must be evaluated to a value (BitwiseOr)");
            Assert.That(andIdx, Is.GreaterThan(orIdx),
                "the outer and must be evaluated to a value (BitwiseAnd) after the or");
            if (andIdx >= 0)
            {
                var andOps = mlir.GetOperands(andIdx);
                Assert.That(andOps[0], Is.EqualTo(orTmpOffset), "and left operand = the evaluated or result");
                Assert.That(andOps[1], Is.EqualTo(cOffset), "and right operand = c");
            }
            Assert.That(FindCmpIndex(mlir, orTmpOffset, cOffset), Is.EqualTo(-1),
                "the or result must never be compared against c");
            Assert.That(cmpIdx, Is.GreaterThan(andIdx),
                "the evaluated and result must be compared against zero");
        }
    }

    [Test]
    public void IdentifierConditionSkipsThenArmOnlyWhenFalse()
    {
        // A bare identifier condition must skip the then-arm exactly
        // when the variable is zero. Today the Identifier case emits
        // Cmp(a, 0) with no marker, which the backend renders as BNE:
        // the branch fires when Z is clear, i.e. when a is nonzero
        // (condition true) — the then-arm is skipped exactly when it
        // should run, and run when it should be skipped. The stream
        // needs the Transition marker so the branch fires on Z set
        // (BEQ), the same encoding the '<>' condition already uses.
        var mlir = CompilePascal(ConditionProgram("if a then x := 1;"), out var pool);
        uint aOffset = pool.Intern("a");

        int cmpIdx = FindKindIndex(mlir, (byte)LlirInstructionKind.Cmp);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(cmpIdx, Is.GreaterThanOrEqualTo(0), "the condition must emit a compare");
            if (cmpIdx >= 0)
            {
                Assert.That(KindSlice(mlir, cmpIdx, 5), Is.EqualTo(new byte[]
                {
                    (byte)LlirInstructionKind.Cmp,
                    (byte)LlirInstructionKind.Transition,
                    (byte)MlirInstructionKind.Branch,
                    (byte)MlirInstructionKind.Assign,
                    (byte)MlirInstructionKind.Label,
                }), "an identifier condition must lower to Cmp + Transition + Branch + then-arm + end label");
                var cmpOps = mlir.GetOperands(cmpIdx);
                Assert.That(cmpOps[0], Is.EqualTo(aOffset), "Cmp left operand = a");
                Assert.That(cmpOps[1], Is.Zero, "Cmp right operand = 0");
            }
        }
    }

    [Test]
    public void RelationalConditionWithVariableOperandsKeepsComparisonEncoding()
    {
        // Guard: routing value conditions through evaluation must not
        // disturb genuine comparisons. `a = b` and `a < b` keep the
        // established encodings — Cmp(a, b) + Branch for '=', with the
        // Bcs marker for '<' — even though both operands are variables,
        // the exact shape the and/or defect mimics.
        var eqMlir = CompilePascal(ConditionProgram("if (a = b) then x := 1;"), out var eqPool);
        var ltMlir = CompilePascal(ConditionProgram("if (a < b) then x := 1;"), out var ltPool);

        int eqCmp = FindKindIndex(eqMlir, (byte)LlirInstructionKind.Cmp);
        int ltCmp = FindKindIndex(ltMlir, (byte)LlirInstructionKind.Cmp);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(eqCmp, Is.GreaterThanOrEqualTo(0), "'=' must emit a compare");
            if (eqCmp >= 0)
            {
                Assert.That(KindSlice(eqMlir, eqCmp, 4), Is.EqualTo(new byte[]
                {
                    (byte)LlirInstructionKind.Cmp,
                    (byte)MlirInstructionKind.Branch,
                    (byte)MlirInstructionKind.Assign,
                    (byte)MlirInstructionKind.Label,
                }), "'=' with variable operands keeps Cmp + Branch + then-arm + end label");
                var eqOps = eqMlir.GetOperands(eqCmp);
                Assert.That(eqOps[0], Is.EqualTo(eqPool.Intern("a")), "'=' Cmp left operand = a");
                Assert.That(eqOps[1], Is.EqualTo(eqPool.Intern("b")), "'=' Cmp right operand = b");
            }

            Assert.That(ltCmp, Is.GreaterThanOrEqualTo(0), "'<' must emit a compare");
            if (ltCmp >= 0)
            {
                Assert.That(KindSlice(ltMlir, ltCmp, 5), Is.EqualTo(new byte[]
                {
                    (byte)LlirInstructionKind.Cmp,
                    (byte)LlirInstructionKind.Bcs,
                    (byte)MlirInstructionKind.Branch,
                    (byte)MlirInstructionKind.Assign,
                    (byte)MlirInstructionKind.Label,
                }), "'<' with variable operands keeps Cmp + Bcs + Branch + then-arm + end label");
                var ltOps = ltMlir.GetOperands(ltCmp);
                Assert.That(ltOps[0], Is.EqualTo(ltPool.Intern("a")), "'<' Cmp left operand = a");
                Assert.That(ltOps[1], Is.EqualTo(ltPool.Intern("b")), "'<' Cmp right operand = b");
            }
        }
    }

    /// <summary>Builds a program whose only statement is the given one,
    /// over the variables a, b, c, x.</summary>
    private static string ConditionProgram(string statement) =>
        $"program Test;\nvar a, b, c, x: Integer;\nbegin\n  {statement}\nend.";

    /// <summary>Compiles Pascal source through the real frontend and the
    /// HLIR-to-MLIR transformer, returning the MLIR stream and the
    /// frontend's string pool (for interning the names the stream's
    /// operands reference).</summary>
    private static InstList CompilePascal(string source, out StringPool pool)
    {
        var frontend = new PascalFrontend();
        var parsed = frontend.ParseToHlir(source);
        Assert.That(parsed.Hlir.Count, Is.GreaterThan(0), "frontend must produce HLIR");
        pool = frontend.StringPool!;
        return new HlirTreeToMlirTransformer(pool).Transform(parsed.Hlir);
    }

    private static void AssertBitwiseConditionIsEvaluated(string condition, byte opKind, string opName)
    {
        var mlir = CompilePascal(ConditionProgram($"if ({condition}) then x := 1;"), out var pool);
        uint aOffset = pool.Intern("a");
        uint bOffset = pool.Intern("b");
        uint xOffset = pool.Intern("x");
        uint oneOffset = pool.Intern("1");
        uint tmpOffset = pool.Intern("__tmp_0");

        int opIdx = FindKindIndex(mlir, opKind);
        int cmpIdx = FindCmpIndex(mlir, tmpOffset, 0);
        int branchIdx = FindKindIndex(mlir, (byte)MlirInstructionKind.Branch);
        int assignIdx = FindAssign(mlir, xOffset, oneOffset);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(opIdx, Is.GreaterThanOrEqualTo(0),
                $"the {opName} condition must be evaluated to a value, not compared");
            if (opIdx >= 0)
            {
                var opOps = mlir.GetOperands(opIdx);
                Assert.That(opOps[0], Is.EqualTo(aOffset), $"{opName} left operand = a");
                Assert.That(opOps[1], Is.EqualTo(bOffset), $"{opName} right operand = b");
            }
            Assert.That(FindCmpIndex(mlir, aOffset, bOffset), Is.EqualTo(-1),
                "the condition's operands must never be compared against each other");
            Assert.That(cmpIdx, Is.GreaterThan(opIdx),
                "the evaluated result must be compared against zero");
            Assert.That(branchIdx, Is.GreaterThan(cmpIdx),
                "the skip branch follows the zero test");
            if (branchIdx > 0)
                Assert.That(mlir.GetKind(branchIdx - 1), Is.EqualTo((byte)LlirInstructionKind.Transition),
                    "the skip branch must fire when the result is zero (condition false), so a Transition (BEQ) marker must precede it");
            Assert.That(assignIdx, Is.GreaterThan(branchIdx),
                "the then-arm assignment follows the skip branch");
            if (branchIdx >= 0 && assignIdx >= 0)
            {
                int endLabel = FindLabelAfter(mlir, assignIdx);
                Assert.That(endLabel, Is.GreaterThanOrEqualTo(0), "the if must end in a label");
                if (endLabel >= 0)
                    Assert.That(mlir.GetOperands(branchIdx)[0], Is.EqualTo(mlir.GetOperands(endLabel)[0]),
                        "the skip branch must target the label placed after the then-arm");
            }
        }
    }

    private static int FindKindIndex(InstList mlir, byte kind) => FindKindIndex(mlir, kind, 0);

    private static int FindKindIndex(InstList mlir, byte kind, int startAt)
    {
        for (int i = startAt; i < mlir.Count; i++)
            if (mlir.GetKind(i) == kind) return i;
        return -1;
    }

    private static byte[] KindSlice(InstList mlir, int start, int count)
    {
        var kinds = new byte[count];
        for (int i = 0; i < count; i++)
            kinds[i] = mlir.GetKind(start + i);
        return kinds;
    }

    private static int FindCmpIndex(InstList mlir, uint left, uint right)
    {
        for (int i = 0; i < mlir.Count; i++)
        {
            if (mlir.GetKind(i) != (byte)LlirInstructionKind.Cmp) continue;
            var ops = mlir.GetOperands(i);
            if (ops.Length == 2 && ops[0] == left && ops[1] == right) return i;
        }
        return -1;
    }

}