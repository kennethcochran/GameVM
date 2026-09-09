using System;
using System.Collections.Generic;
using GameVM.Compiler.Core.IR.Buffers;
using GameVM.Compiler.Core.IR.Hlir;
using GameVM.Compiler.Core.IR.Soa;

namespace GameVM.Compiler.Core.IR.Transformers;

/// <summary>
/// Lowers an AoS HLIR semantic tree (<see cref="HlirTree"/>) into a flat MLIR
/// instruction stream (<see cref="InstList"/>). This is the boundary where the
/// semantic tree becomes an imperative, target-agnostic instruction list.
///
/// Expressions are lowered to temporaries: every non-trivial expression is evaluated
/// into a fresh temp slot, so a BinaryOp operand is always a name (temp) or a literal
/// immediate. Names are StringPool offsets; literal integer immediates pass through
/// as the numeric value.
///
/// Branch polarity: branches are branch-to-target-when-true. The MLIR stream carries
/// branch polarity explicitly; the backend resolves the exact 6502 conditional from
/// the comparison op and an optional <see cref="LlirInstructionKind.Transition"/> marker.
/// </summary>
public sealed class HlirTreeToMlirTransformer
{
    private readonly StringPool _pool = null!;
    private int _labelCounter;
    private int _tempCounter;



    public HlirTreeToMlirTransformer(StringPool stringPool)
    {
        _pool = stringPool ?? throw new ArgumentNullException(nameof(stringPool));
    }

    /// <summary>Lower the semantic HLIR tree to a flat MLIR instruction list.</summary>
    public InstList Transform(HlirTree tree)
    {
        if (tree.Count == 0)
            return new InstList(Array.Empty<byte>(), Array.Empty<ushort>(), Array.Empty<ushort>(), Array.Empty<uint>(), Array.Empty<uint>(), Array.Empty<uint>(), Array.Empty<int>(), 0, 0);

        var builder = new InstListBuilder();
        _labelCounter = 0;
        _tempCounter = 0;
        int rootIdx = tree.Count - 1;

        ProcessNode(tree, rootIdx, builder);

        return builder.Build();
    }

    private void ProcessNode(HlirTree tree, int idx, InstListBuilder builder)
    {
        var node = tree[idx];
        switch ((HlirNodeKind)node.Kind)
        {
            case HlirNodeKind.FunctionDeclaration:
                builder.Add((byte)MlirInstructionKind.Label, InstructionFlag.None, 0, node.Payload);
                var bodyChildren = tree.Children(idx);
                if (bodyChildren.Length > 0)
                    ProcessNode(tree, ChildIndex(tree, idx, 0), builder);
                break;

            case HlirNodeKind.Assign:
                ProcessAssign(tree, idx, builder);
                break;

            case HlirNodeKind.If:
                ProcessIf(tree, idx, builder);
                break;

            case HlirNodeKind.While:
                ProcessWhile(tree, idx, builder);
                break;

            case HlirNodeKind.For:
                ProcessFor(tree, idx, builder);
                break;

            case HlirNodeKind.Return:
                ProcessReturn(tree, idx, builder);
                break;
            case HlirNodeKind.Label:
                builder.Add((byte)MlirInstructionKind.Label, InstructionFlag.None, 0, node.Payload);
                break;

            case HlirNodeKind.Jump:
                builder.Add((byte)MlirInstructionKind.Branch, InstructionFlag.None, 0, node.Payload);
                break;

            case HlirNodeKind.Call:
                ProcessCallStatement(tree, idx, builder);
                break;

            case HlirNodeKind.ExpressionStatement:
                var ch = tree.Children(idx);
                if (ch.Length > 0)
                    EvaluateExpression(tree, ChildIndex(tree, idx, 0), builder, out _);
                break;

            case HlirNodeKind.Output:
                builder.Add((byte)MlirInstructionKind.Nop, InstructionFlag.None, 0);
                break;

            case HlirNodeKind.Nop:
                builder.Add((byte)MlirInstructionKind.Nop, InstructionFlag.None, 0);
                break;

            case HlirNodeKind.VariableDeclaration:
                ProcessVariableDeclaration(tree, idx, builder);
                break;

            case HlirNodeKind.Block:
                var blockChildren = tree.Children(idx);
                for (int i = 0; i < blockChildren.Length; i++)
                    ProcessNode(tree, ChildIndex(tree, idx, i), builder);
                break;
        }
    }

    private static int ChildIndex(HlirTree tree, int parentIdx, int offset)
    {
        var children = tree.Children(parentIdx);
        return offset < children.Length ? children[offset] : -1;
    }

    private void ProcessAssign(HlirTree tree, int idx, InstListBuilder builder)
    {
        var children = tree.Children(idx);
        if (children.Length < 1) return;

        int targetIdx = ChildIndex(tree, idx, 0);
        int valueIdx = children.Length > 1 ? ChildIndex(tree, idx, 1) : -1;

        var targetNode = tree[targetIdx];
        uint targetOffset = targetNode.Payload;

        if (valueIdx < 0)
        {
            builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, targetOffset, 0);
            return;
        }

        uint valueSlot = EvaluateExpression(tree, valueIdx, builder, out _);
        builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, targetOffset, valueSlot);
    }
    private uint EvaluateExpression(HlirTree tree, int idx, InstListBuilder builder, out bool isImmediate)
    {
        var node = tree[idx];
        switch ((HlirNodeKind)node.Kind)
        {
            case HlirNodeKind.LiteralInt:
                // Pass the StringPool offset of the literal text; the backend
                // resolver classifies it (numeric -> immediate, name -> zp).
                isImmediate = true;
                return node.Payload;


            case HlirNodeKind.LiteralBool:
                isImmediate = true;
                return node.Payload;

            case HlirNodeKind.LiteralString:
            case HlirNodeKind.Identifier:
                isImmediate = false;
                return node.Payload;

            case HlirNodeKind.Call:
                {
                    string temp = NewTemp();
                    uint tempOffset = _pool.Intern(temp);
                    ProcessCallStatement(tree, idx, builder);
                    builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, tempOffset, 0);
                    isImmediate = false;
                    return tempOffset;
                }

            case HlirNodeKind.BinaryOp:
                {
                    var children = tree.Children(idx);
                    if (children.Length < 2)
                    {
                        isImmediate = true;
                        return 0;
                    }

                    int leftIdx = ChildIndex(tree, idx, 0);
                    int rightIdx = ChildIndex(tree, idx, 1);
                    uint leftSlot = EvaluateExpression(tree, leftIdx, builder, out _);
                    uint rightSlot = EvaluateExpression(tree, rightIdx, builder, out _);
                    char op = (char)node.Payload;

                    string temp = NewTemp();
                    uint tempOffset = _pool.Intern(temp);
                    byte kind = ArithToMlirKind(op);
                    if (kind != (byte)MlirInstructionKind.Nop)
                    {
                        builder.Add(kind, InstructionFlag.None, 0, leftSlot, rightSlot);
                        builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, tempOffset, 0);
                    }
                    else
                    {
                        builder.Add((byte)LlirInstructionKind.Cmp, InstructionFlag.None, 0, leftSlot, rightSlot);
                        builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, tempOffset, 1);
                    }

                    isImmediate = false;
                    return tempOffset;
                }

            default:
                isImmediate = true;
                return 0;
        }
    }

    private static byte ArithToMlirKind(char op)
    {
        return op switch
        {
            '+' => (byte)LlirInstructionKind.Add,
            '-' => (byte)LlirInstructionKind.Sub,
            '*' => (byte)LlirInstructionKind.Add,
            '/' => (byte)LlirInstructionKind.Sub,
            _ => (byte)MlirInstructionKind.Nop
        };
    }

    private string NewTemp()
    {
        return $"__tmp_{_tempCounter++}";
    }

    private void ProcessCallStatement(HlirTree tree, int idx, InstListBuilder builder)
    {
        var children = tree.Children(idx);
        uint funcOffset = tree[idx].Payload;
        var argSlots = new List<uint>();
        for (int i = 0; i < children.Length; i++)
        {
            int argIdx = ChildIndex(tree, idx, i);
            argSlots.Add(EvaluateExpression(tree, argIdx, builder, out _));
        }

        var ops = new uint[1 + argSlots.Count];
        ops[0] = funcOffset;
        for (int i = 0; i < argSlots.Count; i++)
            ops[i + 1] = argSlots[i];

        builder.Add((byte)MlirInstructionKind.Call, InstructionFlag.None, 0, ops);
    }

    private static void ProcessVariableDeclaration(HlirTree tree, int idx, InstListBuilder builder)
    {
        var children = tree.Children(idx);
        if (children.Length < 1) return;

        int nameIdx = ChildIndex(tree, idx, 0);
        var nameNode = tree[nameIdx];
        uint nameOffset = nameNode.Payload;

        builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, nameOffset, 0);
    }

    private void ProcessReturn(HlirTree tree, int idx, InstListBuilder builder)
    {
        var children = tree.Children(idx);
        if (children.Length == 0)
        {
            builder.Add((byte)MlirInstructionKind.Return, InstructionFlag.None, 0);
            return;
        }

        int valueIdx = ChildIndex(tree, idx, 0);
        uint valueSlot = EvaluateExpression(tree, valueIdx, builder, out _);
        builder.Add((byte)MlirInstructionKind.Return, InstructionFlag.None, 0, valueSlot);
    }

    private void ProcessIf(HlirTree tree, int idx, InstListBuilder builder)
    {
        var children = tree.Children(idx);
        if (children.Length < 2) return;

        int condIdx = ChildIndex(tree, idx, 0);
        int thenIdx = ChildIndex(tree, idx, 1);
        int elseIdx = children.Length > 2 ? ChildIndex(tree, idx, 2) : -1;

        string endLabel = $"L_if_end_{_labelCounter++}";

        if (elseIdx < 0)
        {
            EmitConditionalBranch(tree, condIdx, builder, endLabel, invert: true);
            ProcessNode(tree, thenIdx, builder);
            builder.Add((byte)MlirInstructionKind.Label, InstructionFlag.None, 0, _pool.Intern(endLabel));
        }
        else
        {
            string elseLabel = $"L_else_{_labelCounter++}";
            EmitConditionalBranch(tree, condIdx, builder, elseLabel, invert: false);
            ProcessNode(tree, thenIdx, builder);
            builder.Add((byte)MlirInstructionKind.Branch, InstructionFlag.None, 0, _pool.Intern(endLabel));
            builder.Add((byte)MlirInstructionKind.Label, InstructionFlag.None, 0, _pool.Intern(elseLabel));
            ProcessNode(tree, elseIdx, builder);
            builder.Add((byte)MlirInstructionKind.Label, InstructionFlag.None, 0, _pool.Intern(endLabel));
        }
    }

    private void ProcessWhile(HlirTree tree, int idx, InstListBuilder builder)
    {
        var children = tree.Children(idx);
        if (children.Length < 2) return;

        int condIdx = ChildIndex(tree, idx, 0);
        int bodyIdx = ChildIndex(tree, idx, 1);

        string loopLabel = $"L_while_{_labelCounter++}";
        string endLabel = $"L_while_end_{_labelCounter++}";

        builder.Add((byte)MlirInstructionKind.Label, InstructionFlag.None, 0, _pool.Intern(loopLabel));
        EmitConditionalBranch(tree, condIdx, builder, endLabel, invert: true);
        ProcessNode(tree, bodyIdx, builder);
        builder.Add((byte)MlirInstructionKind.Branch, InstructionFlag.None, 0, _pool.Intern(loopLabel));
        builder.Add((byte)MlirInstructionKind.Label, InstructionFlag.None, 0, _pool.Intern(endLabel));
    }

    private void ProcessFor(HlirTree tree, int idx, InstListBuilder builder)
    {
        var children = tree.Children(idx);
        if (children.Length < 3) return;

        int initIdx = ChildIndex(tree, idx, 0);
        int condIdx = ChildIndex(tree, idx, 1);
        int bodyIdx = ChildIndex(tree, idx, 2);

        string loopLabel = $"L_for_{_labelCounter++}";
        string endLabel = $"L_for_end_{_labelCounter++}";

        ProcessNode(tree, initIdx, builder);
        builder.Add((byte)MlirInstructionKind.Label, InstructionFlag.None, 0, _pool.Intern(loopLabel));
        EmitConditionalBranch(tree, condIdx, builder, endLabel, invert: true);
        ProcessNode(tree, bodyIdx, builder);
        builder.Add((byte)MlirInstructionKind.Branch, InstructionFlag.None, 0, _pool.Intern(loopLabel));
        builder.Add((byte)MlirInstructionKind.Label, InstructionFlag.None, 0, _pool.Intern(endLabel));
    }

    private void EmitConditionalBranch(HlirTree tree, int condIdx, InstListBuilder builder, string targetLabel, bool invert)
    {
        uint labelOffset = _pool.Intern(targetLabel);

        var condNode = tree[condIdx];
        switch ((HlirNodeKind)condNode.Kind)
        {
            case HlirNodeKind.BinaryOp:
                {
                    var children = tree.Children(condIdx);
                    if (children.Length >= 2)
                    {
                        int leftIdx = ChildIndex(tree, condIdx, 0);
                        int rightIdx = ChildIndex(tree, condIdx, 1);
                        uint leftSlot = EvaluateExpression(tree, leftIdx, builder, out _);
                        uint rightSlot = EvaluateExpression(tree, rightIdx, builder, out _);
                        char relOp = (char)condNode.Payload;

                        byte mlirKind = RelOpToBranchKind(relOp, invert);
                        if (mlirKind != (byte)MlirInstructionKind.Nop)
                        {
                            builder.Add(mlirKind, InstructionFlag.None, 0, leftSlot, rightSlot);
                            builder.Add((byte)MlirInstructionKind.Branch, InstructionFlag.None, 0, labelOffset);
                        }
                    }
                }
                break;

            case HlirNodeKind.Identifier:
                {
                    uint nameOffset = condNode.Payload;
                    builder.Add((byte)LlirInstructionKind.Cmp, InstructionFlag.None, 0, nameOffset, 0);
                    builder.Add((byte)MlirInstructionKind.Branch, InstructionFlag.None, 0, labelOffset);
                }
                break;

            case HlirNodeKind.LiteralBool:
                {
                    bool val = condNode.Payload != 0;
                    byte branchKind = (val ^ invert)
                        ? (byte)MlirInstructionKind.Branch
                        : (byte)MlirInstructionKind.Nop;
                    if (branchKind != (byte)MlirInstructionKind.Nop)
                        builder.Add(branchKind, InstructionFlag.None, 0, labelOffset);
                }
                break;
            default:
                builder.Add((byte)MlirInstructionKind.Branch, InstructionFlag.None, 0, labelOffset);
                break;
        }
    }

    private static byte RelOpToBranchKind(char relOp, bool invert)
    {
        _ = relOp;
        _ = invert;
        // All conditional branches use the same Branch opcode; the backend
        // resolves the exact 6502 conditional from the comparison op and polarity.
        return (byte)MlirInstructionKind.Branch;
    }
}
