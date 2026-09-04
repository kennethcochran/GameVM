using System;
using System.Collections.Generic;
using GameVM.Compiler.Core.IR;
using GameVM.Compiler.Core.IR.Soa;
using GameVM.Compiler.Core.IR.Buffers;
using GameVM.Compiler.Core.IR.Ast;

namespace GameVM.Compiler.Pascal.Transformers
{
    /// <summary>
    /// Transforms a Pascal AstTree to an HLIR InstList.
    /// Language-specific lowering from Pascal's AstTree (with PascalAstNodeKind) to shared HLIR.
    /// </summary>
    public sealed class PascalAstToHlirTransformer
    {
        private readonly StringPool _stringPool;
        private readonly Dictionary<uint, string> _variableNames = new();
        private readonly Dictionary<uint, byte> _variableTypes = new();
        private readonly List<string> _errors = new();
        private int _labelCounter;

        public PascalAstToHlirTransformer(StringPool stringPool)
        {
            _stringPool = stringPool ?? throw new ArgumentNullException(nameof(stringPool));
        }

        /// <summary>
        /// Transforms a Pascal AstTree to an HLIR InstList.
        /// </summary>
        public InstList Transform(AstTree astTree)
        {
            _variableNames.Clear();
            _variableTypes.Clear();
            _errors.Clear();
            _labelCounter = 0;

            var builder = new InstListBuilder();

            // Process the AST as a sequence of top-level declarations
            // In Pascal AST: PROGRAM -> METHOD_DECLARATION -> BLOCK -> statements
            for (int i = 0; i < astTree.Count; i++)
            {
                PascalAstNodeKind kind = (PascalAstNodeKind)astTree.GetKind(i);
                if (kind == PascalAstNodeKind.MethodDeclaration)
                {
                    var node = astTree[i];
                    // METHOD_DECLARATION: payload = functionNameHash, children = [bodyBlock]
                    if (node.ChildCount > 0 && node.FirstChild >= 0)
                    {
                        int bodyIdx = ChildIndex(astTree, i, 0);
                        if (bodyIdx >= 0)
                        {
                            ProcessFunction(astTree, bodyIdx, builder);
                        }
                    }
                }
            }

            if (_errors.Count > 0)
            {
                throw new InvalidOperationException(_errors[0]);
            }

            return builder.Build();
        }

        private static int ChildIndex(AstTree astTree, int parentIdx, int childOffset)
        {
            var children = astTree.Children(parentIdx);
            if (childOffset < children.Length)
            {
                // Find the index in the flat array
                var parent = astTree[parentIdx];
                return parent.FirstChild + childOffset;
            }
            return -1;
        }

        private void ProcessFunction(AstTree astTree, int bodyIdx, InstListBuilder builder)
        {
            // In HLIR, we represent a function as:
            // HLIR_LABEL: [functionNameHash]  (acts as function entry point)
            uint functionNameHash = _stringPool.Intern($"_func_{_labelCounter:00000000}");
            _labelCounter++;
            builder.Add((byte)MlirInstructionKind.Label, 0, 0, functionNameHash);

            // Process the function body (the BLOCK instruction)
            var bodyNode = astTree[bodyIdx];
            if (bodyNode.Kind == (byte)PascalAstNodeKind.Block)
            {
                var children = astTree.Children(bodyIdx);
                for (int childOffset = 0; childOffset < children.Length; childOffset++)
                {
                    int stmtIdx = ChildIndex(astTree, bodyIdx, childOffset);
                    if (stmtIdx >= 0)
                    {
                        PascalAstNodeKind stmtKind = (PascalAstNodeKind)astTree.GetKind(stmtIdx);
                        ProcessStatement(astTree, stmtIdx, stmtKind, builder);
                    }
                }
            }
        }

        private void ProcessBlock(AstTree astTree, int blockIdx, InstListBuilder builder)
        {
            var children = astTree.Children(blockIdx);
            for (int childOffset = 0; childOffset < children.Length; childOffset++)
            {
                int stmtIdx = ChildIndex(astTree, blockIdx, childOffset);
                if (stmtIdx < 0 || stmtIdx >= astTree.Count) continue;

                PascalAstNodeKind stmtKind = (PascalAstNodeKind)astTree.GetKind(stmtIdx);
                ProcessStatement(astTree, stmtIdx, stmtKind, builder);
            }
        }

        private void ProcessStatement(AstTree astTree, int stmtIdx, PascalAstNodeKind stmtKind, InstListBuilder builder)
        {
            switch (stmtKind)
            {
                case PascalAstNodeKind.Assignment:
                    ProcessAssignment(astTree, stmtIdx, builder);
                    break;
                case PascalAstNodeKind.ExpressionStatement:
                    ProcessExpressionStatement(astTree, stmtIdx, builder);
                    break;
                case PascalAstNodeKind.IfStatement:
                    ProcessIfStatement(astTree, stmtIdx, builder);
                    break;
                case PascalAstNodeKind.WhileStatement:
                    ProcessWhileStatement(astTree, stmtIdx, builder);
                    break;
                case PascalAstNodeKind.ReturnStatement:
                    ProcessReturnStatement(astTree, stmtIdx, builder);
                    break;
                case PascalAstNodeKind.Block:
                    ProcessBlock(astTree, stmtIdx, builder);
                    break;
                case PascalAstNodeKind.VariableDeclaration:
                    ProcessVariableDeclaration(astTree, stmtIdx, builder);
                    break;
                case PascalAstNodeKind.ForStatement:
                    ProcessForStatement(astTree, stmtIdx, builder);
                    break;
                default:
                    if (IsExpressionKind(stmtKind))
                    {
                        ProcessExpressionStatement(astTree, stmtIdx, builder);
                    }
                    break;
            }
        }

        private void ProcessAssignment(AstTree astTree, int stmtIdx, InstListBuilder builder)
        {
            var children = astTree.Children(stmtIdx);
            if (children.Length < 2) return;

            int targetIdx = ChildIndex(astTree, stmtIdx, 0);
            int valueIdx = ChildIndex(astTree, stmtIdx, 1);

            if (targetIdx < 0 || valueIdx < 0 || targetIdx >= astTree.Count || valueIdx >= astTree.Count)
                return;

            PascalAstNodeKind targetKind = (PascalAstNodeKind)astTree.GetKind(targetIdx);
            if (targetKind == PascalAstNodeKind.Identifier)
            {
                var targetNode = astTree[targetIdx];
                uint nameOffset = targetNode.Payload;
                if (!_variableNames.ContainsKey(nameOffset))
                {
                    string varName = _stringPool.Resolve(nameOffset);
                    _errors.Add($"Undefined variable '{varName}'");
                    return;
                }
            }

            string targetStr = ResolveExpression(astTree, targetIdx);
            string valueStr = ResolveExpression(astTree, valueIdx);

            var targetPoolOffset = _stringPool.Intern(targetStr);
            var valuePoolOffset = _stringPool.Intern(valueStr);

            builder.Add((byte)MlirInstructionKind.Assign, 0, 0, targetPoolOffset, valuePoolOffset);
        }

        private void ProcessExpressionStatement(AstTree astTree, int stmtIdx, InstListBuilder builder)
        {
            var children = astTree.Children(stmtIdx);
            if (children.Length < 1) return;

            int exprIdx = ChildIndex(astTree, stmtIdx, 0);
            if (exprIdx < 0 || exprIdx >= astTree.Count) return;

            string exprStr = ResolveExpression(astTree, exprIdx);

            var targetPoolOffset = _stringPool.Intern("_temp");
            var valuePoolOffset = _stringPool.Intern(exprStr);

            builder.Add((byte)MlirInstructionKind.Assign, 0, 0, targetPoolOffset, valuePoolOffset);
        }

        private void ProcessIfStatement(AstTree astTree, int stmtIdx, InstListBuilder builder)
        {
            var children = astTree.Children(stmtIdx);
            if (children.Length < 2) return;

            int conditionIdx = ChildIndex(astTree, stmtIdx, 0);
            int thenIdx = ChildIndex(astTree, stmtIdx, 1);
            int? elseIdx = children.Length >= 3 ? ChildIndex(astTree, stmtIdx, 2) : null;

            if (conditionIdx < 0 || thenIdx < 0 ||
                (elseIdx.HasValue && (elseIdx.Value < 0 || elseIdx.Value >= astTree.Count)))
                return;

            string conditionStr = ResolveExpression(astTree, conditionIdx);

            int labelBase = _labelCounter++;
            var thenLabel = $"L_if_then_{labelBase}";
            var endLabel = $"L_if_end_{labelBase}";
            var elseLabel = elseIdx.HasValue ? $"L_if_else_{labelBase}" : endLabel;

            builder.Add((byte)MlirInstructionKind.Label, 0, 0, _stringPool.Intern(thenLabel));
            builder.Add((byte)MlirInstructionKind.Branch, 0, 0, _stringPool.Intern(conditionStr), _stringPool.Intern(elseLabel));

            PascalAstNodeKind thenKind = (PascalAstNodeKind)astTree.GetKind(thenIdx);
            ProcessStatement(astTree, thenIdx, thenKind, builder);

            builder.Add((byte)MlirInstructionKind.Label, 0, 0, _stringPool.Intern(endLabel));

            if (elseIdx.HasValue)
            {
                PascalAstNodeKind elseKind = (PascalAstNodeKind)astTree.GetKind(elseIdx.Value);
                ProcessStatement(astTree, elseIdx.Value, elseKind, builder);
            }
        }

        private void ProcessWhileStatement(AstTree astTree, int stmtIdx, InstListBuilder builder)
        {
            var children = astTree.Children(stmtIdx);
            if (children.Length < 2) return;

            int conditionIdx = ChildIndex(astTree, stmtIdx, 0);
            int bodyIdx = ChildIndex(astTree, stmtIdx, 1);

            if (conditionIdx < 0 || conditionIdx >= astTree.Count ||
                bodyIdx < 0 || bodyIdx >= astTree.Count)
                return;

            string loopLabel = $"L_loop_{_variableNames.Count}";
            string endLabel = $"L_end_{_variableNames.Count}";

            builder.Add((byte)MlirInstructionKind.Label, 0, 0, _stringPool.Intern(loopLabel));

            string conditionStr = ResolveExpression(astTree, conditionIdx);
            builder.Add((byte)MlirInstructionKind.Branch, 0, 0, _stringPool.Intern(endLabel), _stringPool.Intern(conditionStr));

            PascalAstNodeKind bodyKind = (PascalAstNodeKind)astTree.GetKind(bodyIdx);
            ProcessStatement(astTree, bodyIdx, bodyKind, builder);

            builder.Add((byte)MlirInstructionKind.Branch, 0, 0, _stringPool.Intern(loopLabel));
            builder.Add((byte)MlirInstructionKind.Label, 0, 0, _stringPool.Intern(endLabel));
        }

        private void ProcessReturnStatement(AstTree astTree, int stmtIdx, InstListBuilder builder)
        {
            var children = astTree.Children(stmtIdx);
            if (children.Length == 0)
            {
                builder.Add((byte)MlirInstructionKind.Return, 0, 0);
                return;
            }

            if (children.Length >= 1)
            {
                int exprIdx = ChildIndex(astTree, stmtIdx, 0);
                if (exprIdx >= 0 && exprIdx < astTree.Count)
                {
                    string exprStr = ResolveExpression(astTree, exprIdx);
                    var valuePoolOffset = _stringPool.Intern(exprStr);
                    var targetPoolOffset = _stringPool.Intern("_return");

                    builder.Add((byte)MlirInstructionKind.Assign, 0, 0, targetPoolOffset, valuePoolOffset);
                }
            }
        }

        private void ProcessVariableDeclaration(AstTree astTree, int stmtIdx, InstListBuilder builder)
        {
            var node = astTree[stmtIdx];
            // VARIABLE_DECLARATION: payload = typeKind, child = Identifier
            byte typeKind = (byte)node.Payload;
            var children = astTree.Children(stmtIdx);
            if (children.Length < 1) return;

            int nameIdx = ChildIndex(astTree, stmtIdx, 0);
            if (nameIdx < 0 || nameIdx >= astTree.Count) return;

            var nameNode = astTree[nameIdx];
            uint nameOffset = nameNode.Payload;

            string varName = _stringPool.Resolve(nameOffset);
            if (string.IsNullOrEmpty(varName)) return;

            _variableNames[nameOffset] = varName;
            _variableTypes[nameOffset] = typeKind;

            string initStr = typeKind switch
            {
                1 => "0",
                2 => "",
                3 => "false",
                _ => "0"
            };

            var targetPoolOffset = _stringPool.Intern(varName);
            var valuePoolOffset = _stringPool.Intern(initStr);

            builder.Add((byte)MlirInstructionKind.Assign, 0, 0, targetPoolOffset, valuePoolOffset);
        }

        private void ProcessForStatement(AstTree astTree, int stmtIdx, InstListBuilder builder)
        {
            // FOR_STATEMENT: payload = direction, children = [varName, initial, final, body]
            var children = astTree.Children(stmtIdx);
            if (children.Length < 4) return;

            int varNameIdx = ChildIndex(astTree, stmtIdx, 0);
            int initialIdx = ChildIndex(astTree, stmtIdx, 1);
            int finalIdx = ChildIndex(astTree, stmtIdx, 2);
            int bodyIdx = ChildIndex(astTree, stmtIdx, 3);

            if (varNameIdx < 0 || initialIdx < 0 || finalIdx < 0 || bodyIdx < 0) return;

            var varNameNode = astTree[varNameIdx];
            uint varNameOffset = varNameNode.Payload;

            string initialStr = ResolveExpression(astTree, initialIdx);
            string finalStr = ResolveExpression(astTree, finalIdx);

            string loopLabel = $"L_for_{_labelCounter++}";
            string endLabel = $"L_end_for_{_labelCounter}";

            // Initialize loop variable
            var initValuePoolOffset = _stringPool.Intern(initialStr);
            var varPoolOffset = _stringPool.Intern(_stringPool.Resolve(varNameOffset));
            builder.Add((byte)MlirInstructionKind.Assign, 0, 0, varPoolOffset, initValuePoolOffset);

            // Loop condition
            builder.Add((byte)MlirInstructionKind.Label, 0, 0, _stringPool.Intern(loopLabel));

            // Compare loop variable to final value (direction: 0=TO uses <=, 1=DOWNTO uses >=)
            var stmtNode = astTree[stmtIdx];
            bool isDownto = stmtNode.Payload == 1;
            string conditionStr = isDownto
                ? $"({_stringPool.Resolve(varNameOffset)} >= {finalStr})"
                : $"({_stringPool.Resolve(varNameOffset)} <= {finalStr})";
            builder.Add((byte)MlirInstructionKind.Branch, 0, 0, _stringPool.Intern(endLabel), _stringPool.Intern(conditionStr));

            PascalAstNodeKind bodyKind = (PascalAstNodeKind)astTree.GetKind(bodyIdx);
            ProcessStatement(astTree, bodyIdx, bodyKind, builder);

            // Increment loop variable
            builder.Add((byte)MlirInstructionKind.Branch, 0, 0, _stringPool.Intern(loopLabel));
            builder.Add((byte)MlirInstructionKind.Label, 0, 0, _stringPool.Intern(endLabel));
        }

        private string ResolveExpression(AstTree astTree, int exprIdx)
        {
            if (exprIdx < 0 || exprIdx >= astTree.Count) return "0";

            PascalAstNodeKind kind = (PascalAstNodeKind)astTree.GetKind(exprIdx);
            var node = astTree[exprIdx];

            return kind switch
            {
                PascalAstNodeKind.LiteralInt => node.Payload.ToString(),
                PascalAstNodeKind.LiteralString => node.Payload != 0 ? _stringPool.Resolve(node.Payload) : "",
                PascalAstNodeKind.LiteralBool => node.Payload != 0 ? "true" : "false",
                PascalAstNodeKind.Identifier =>
                    node.Payload != 0 ? _stringPool.Resolve(node.Payload) : "<unknown>",
                PascalAstNodeKind.BinaryOp => ResolveBinaryOp(astTree, exprIdx),
                _ => "0"
            };
        }

        private string ResolveBinaryOp(AstTree astTree, int exprIdx)
        {
            var children = astTree.Children(exprIdx);
            if (children.Length < 2) return "0";

            int leftIdx = ChildIndex(astTree, exprIdx, 0);
            int rightIdx = ChildIndex(astTree, exprIdx, 1);
            var opNode = astTree[exprIdx];
            char opChar = (char)opNode.Payload;

            if (leftIdx < 0 || rightIdx < 0 || leftIdx >= astTree.Count || rightIdx >= astTree.Count)
                return "0";

            string left = ResolveExpression(astTree, leftIdx);
            string right = ResolveExpression(astTree, rightIdx);

            if (int.TryParse(left, out int lVal) && int.TryParse(right, out int rVal))
            {
                return opChar switch
                {
                    '+' => (lVal + rVal).ToString(),
                    '-' => (lVal - rVal).ToString(),
                    '*' => (lVal * rVal).ToString(),
                    '/' => rVal != 0 ? (lVal / rVal).ToString() : "0",
                    _ => $"({left} {opChar} {right})"
                };
            }

            return $"({left} {opChar} {right})";
        }

        private static bool IsExpressionKind(PascalAstNodeKind kind)
        {
            return kind == PascalAstNodeKind.LiteralInt ||
                   kind == PascalAstNodeKind.LiteralString ||
                   kind == PascalAstNodeKind.LiteralBool ||
                   kind == PascalAstNodeKind.Identifier ||
                   kind == PascalAstNodeKind.BinaryOp;
        }
    }
}