using System;
using System.Collections.Generic;
using GameVM.Compiler.Core.IR.Buffers;
using GameVM.Compiler.Core.IR.Hlir;
using GameVM.Compiler.Core.IR.Soa;
using GameVM.Compiler.Core.IR.Ast;

namespace GameVM.Compiler.Pascal.Transformers
{
    /// <summary>
    /// Transforms a Pascal AstTree to an AoS HLIR semantic tree (HlirTree) via a
    /// structural tree-to-tree morph.
    ///
    /// Content contract (visitor is authority, transformer aligns):
    ///  - VisitProgram emits Program -> MethodDeclaration(name="main", child=body Block).
    ///  - VariableDeclaration has an Identifier + TypeDefinition child (payload = pool
    ///    offset of the type-name text).
    ///  - The transformer finds the Program root, lowers its MethodDeclaration body
    ///    recursively (blocks nested in blocks included), and processes function
    ///    MethodDeclaration nodes (payload = name pool offset, child = body Block).
    ///  - Errors surface directly (no rewrap): "Undefined variable 'name'".
    /// </summary>
    public sealed class PascalAstToHlirTransformer
    {
        private readonly StringPool _pool = null!;
        private readonly HlirBuilder _builder = new();
        private readonly HashSet<uint> _declaredVariables = new();
        private readonly Dictionary<uint, string> _declaredTypes = new();
        private readonly Dictionary<uint, uint> _constantValues = new();
        private readonly List<string> _errors = new();
        public PascalAstToHlirTransformer(StringPool stringPool)
        {
            _pool = stringPool ?? throw new ArgumentNullException(nameof(stringPool));
        }

        /// <summary>Transforms a Pascal AstTree to an AoS HLIR semantic tree.</summary>
        public HlirTree Transform(AstTree astTree)
        {
            if (astTree.Count == 0)
                return HlirTree.Empty;

            _declaredVariables.Clear();
            _declaredTypes.Clear();
            _constantValues.Clear();
            int programIdx = FindProgramRoot(astTree);
            if (programIdx < 0)
            {
                throw new InvalidOperationException("Failed to convert AST tree to HLIR tree: no program root");
            }

            ProcessFunction(astTree, programIdx);

            if (_errors.Count > 0)
            {
                throw new InvalidOperationException(string.Join(Environment.NewLine, _errors));
            }

            return _builder.Build();
        }

        private static int FindProgramRoot(AstTree astTree)
        {
            for (int i = 0; i < astTree.Count; i++)
            {
                if ((PascalAstNodeKind)astTree.GetKind(i) == PascalAstNodeKind.Program)
                    return i;
            }
            return -1;
        }

        private void ProcessFunction(AstTree astTree, int fnIdx)
        {
            var fnChildren = astTree.Children(fnIdx);
            if (fnChildren.Length == 0) return;

            int bodyIdx = fnChildren[fnChildren.Length - 1]; // last child = body block

            // Pre-register constants and variable declarations declared in the
            // method header so body expressions can reference them.
            for (int i = 0; i < fnChildren.Length - 1; i++)
            {
                int childIdx = fnChildren[i];
                PascalAstNodeKind kind = (PascalAstNodeKind)astTree.GetKind(childIdx);
                if (kind == PascalAstNodeKind.ConstantDefinition)
                    RegisterConstant(astTree, childIdx);
                else if (kind == PascalAstNodeKind.VariableDeclaration)
                {
                    var varChildren = astTree.Children(childIdx);
                    if (varChildren.Length > 0)
                        _declaredVariables.Add(astTree[varChildren[0]].Payload);
                }
            }
            int bodyHlir = BuildBlock(astTree, bodyIdx);

            uint nameOffset = astTree[fnIdx].Payload;
            _builder.Add((byte)HlirNodeKind.FunctionDeclaration, 0, nameOffset, HlirPayloadKind.PoolOffset, bodyHlir);
        }

        /// <summary>
        /// Builds a Block HLIR node from an AST Block by lowering each child statement
        /// recursively. Returns a Nop leaf for an empty block.
        /// </summary>
        private int BuildBlock(AstTree astTree, int blockIdx)
        {
            var children = astTree.Children(blockIdx);
            var lowered = new List<int>(children.Length);

            foreach (int childIdx in children)
            {
                int hlirChild = ProcessStatement(astTree, childIdx);
                if (hlirChild >= 0)
                    lowered.Add(hlirChild);
            }

            if (lowered.Count == 0)
                return _builder.Add((byte)HlirNodeKind.Nop, 0, 0, HlirPayloadKind.None);

            return _builder.Add((byte)HlirNodeKind.Block, 0, 0, HlirPayloadKind.None, lowered.ToArray());
        }

        private int ProcessStatement(AstTree astTree, int stmtIdx)
        {
            if (stmtIdx < 0 || stmtIdx >= astTree.Count) return -1;

            PascalAstNodeKind kind = (PascalAstNodeKind)astTree.GetKind(stmtIdx);

            switch (kind)
            {
                case PascalAstNodeKind.VariableDeclaration:
                    return ProcessVariableDeclaration(astTree, stmtIdx);
                case PascalAstNodeKind.ConstantDefinition:
                    // Constant definitions are hoisted into the body block by the
                    // visitor, so register the name and value before use.
                    RegisterConstant(astTree, stmtIdx);
                    return _builder.Add((byte)HlirNodeKind.Nop, 0, 0, HlirPayloadKind.None);
                case PascalAstNodeKind.Assignment:
                    return ProcessAssignment(astTree, stmtIdx);
                case PascalAstNodeKind.IfStatement:
                    return ProcessIfStatement(astTree, stmtIdx);
                case PascalAstNodeKind.WhileStatement:
                    return ProcessWhileStatement(astTree, stmtIdx);
                case PascalAstNodeKind.ForStatement:
                    return ProcessForStatement(astTree, stmtIdx);
                case PascalAstNodeKind.Block:
                    return BuildBlock(astTree, stmtIdx);
                case PascalAstNodeKind.ReturnStatement:
                    return ProcessReturnStatement(astTree, stmtIdx);
                case PascalAstNodeKind.MethodCall:
                    return ProcessMethodCall(astTree, stmtIdx);
                case PascalAstNodeKind.Nop:
                    return _builder.Add((byte)HlirNodeKind.Nop, 0, 0, HlirPayloadKind.None);
                default:
                    return ProcessExpressionStatement(astTree, stmtIdx);
            }
        }

        private int ProcessVariableDeclaration(AstTree astTree, int stmtIdx)
        {
            var children = astTree.Children(stmtIdx);
            if (children.Length < 1) return -1;

            int nameIdx = children[0];
            int typeIdx = children.Length > 1 ? children[1] : -1;

            var nameNode = astTree[nameIdx];
            uint nameOffset = nameNode.Payload;
            _declaredVariables.Add(nameOffset);

            uint typeOffset = 0;
            if (typeIdx >= 0)
            {
                typeOffset = astTree[typeIdx].Payload;
                string typeName = _pool.Resolve(typeOffset);
                if (!string.IsNullOrEmpty(typeName))
                    _declaredTypes[nameOffset] = typeName;
            }

            int idHlir = _builder.Add((byte)HlirNodeKind.Identifier, 0, nameOffset, HlirPayloadKind.PoolOffset);
            int typeHlir = _builder.Add((byte)HlirNodeKind.TypeReference, 0, typeOffset, HlirPayloadKind.PoolOffset);

            return _builder.Add((byte)HlirNodeKind.VariableDeclaration, 0, 0, HlirPayloadKind.None, idHlir, typeHlir);
        }


        /// <summary>
        /// Registers a named constant: constantDefinition: identifier EQUAL constant.
        /// The value child is a literal whose payload is the interned literal text.
        /// </summary>
        private void RegisterConstant(AstTree astTree, int constIdx)
        {
            var children = astTree.Children(constIdx);
            if (children.Length < 2) return;

            int nameIdx = children[0];
            int valueIdx = children[1];
            var nameNode = astTree[nameIdx];
            if (nameNode.Kind != (byte)PascalAstNodeKind.Identifier) return;

            uint nameOffset = nameNode.Payload;
            var valueNode = astTree[valueIdx];
            if ((PascalAstNodeKind)valueNode.Kind == PascalAstNodeKind.LiteralInt)
            {
                if (uint.TryParse(_pool.Resolve(valueNode.Payload), out uint intVal))
                    _constantValues[nameOffset] = intVal;
            }
            else if ((PascalAstNodeKind)valueNode.Kind == PascalAstNodeKind.LiteralBool)
            {
                _constantValues[nameOffset] = valueNode.Payload;
            }
        }

        private int ProcessAssignment(AstTree astTree, int stmtIdx)
        {
            var children = astTree.Children(stmtIdx);
            if (children.Length < 2) return -1;

            int targetIdx = children[0];
            int valueIdx = children[1];

            var targetNode = astTree[targetIdx];
            uint targetOffset = targetNode.Payload;

            if (targetNode.Kind == (byte)PascalAstNodeKind.Identifier &&
                !_declaredVariables.Contains(targetOffset))
            {
                _errors.Add($"Undefined variable '{_pool.Resolve(targetOffset)}'");
                return -1;
            }

            // Type checking: string/boolean literals cannot be assigned to an
            // Integer variable; report the mismatch (compile-time error).
            if (targetNode.Kind == (byte)PascalAstNodeKind.Identifier &&
                _declaredTypes.TryGetValue(targetOffset, out string? targetType) &&
                string.Equals(targetType, "integer", StringComparison.OrdinalIgnoreCase))
            {
                var valueNode = astTree[valueIdx];
                PascalAstNodeKind valueKind = (PascalAstNodeKind)valueNode.Kind;
                if (valueKind == PascalAstNodeKind.LiteralString ||
                    valueKind == PascalAstNodeKind.LiteralBool)
                {
                    _errors.Add($"Type mismatch: cannot assign '{_pool.Resolve(valueNode.Payload)}' to Integer variable '{_pool.Resolve(targetOffset)}'");
                    return -1;
                }
            }

            int targetHlir = BuildExpression(astTree, targetIdx);
            int valueHlir = BuildExpression(astTree, valueIdx);
            if (targetHlir < 0 || valueHlir < 0) return -1;
            return _builder.Add((byte)HlirNodeKind.Assign, 0, 0, HlirPayloadKind.None, targetHlir, valueHlir);
        }
        private int ProcessExpressionStatement(AstTree astTree, int stmtIdx)
        {
            var children = astTree.Children(stmtIdx);
            if (children.Length < 1) return -1;

            int exprHlir = BuildExpression(astTree, children[0]);
            if (exprHlir < 0) return -1;

            return _builder.Add((byte)HlirNodeKind.ExpressionStatement, 0, 0, HlirPayloadKind.None, exprHlir);
        }

        private int ProcessIfStatement(AstTree astTree, int stmtIdx)
        {
            var children = astTree.Children(stmtIdx);
            if (children.Length < 2) return -1;

            int condHlir = BuildExpression(astTree, children[0]);
            int thenHlir = ProcessStatement(astTree, children[1]);
            if (condHlir < 0 || thenHlir < 0) return -1;

            if (children.Length > 2)
            {
                int elseHlir = ProcessStatement(astTree, children[2]);
                return _builder.Add((byte)HlirNodeKind.If, 0, 0, HlirPayloadKind.None, condHlir, thenHlir, elseHlir);
            }

            return _builder.Add((byte)HlirNodeKind.If, 0, 0, HlirPayloadKind.None, condHlir, thenHlir);
        }

        private int ProcessWhileStatement(AstTree astTree, int stmtIdx)
        {
            var children = astTree.Children(stmtIdx);
            if (children.Length < 2) return -1;

            int condHlir = BuildExpression(astTree, children[0]);
            int bodyHlir = ProcessStatement(astTree, children[1]);
            if (condHlir < 0 || bodyHlir < 0) return -1;

            return _builder.Add((byte)HlirNodeKind.While, 0, 0, HlirPayloadKind.None, condHlir, bodyHlir);
        }

        private int ProcessForStatement(AstTree astTree, int stmtIdx)
        {
            var children = astTree.Children(stmtIdx);
            if (children.Length < 4) return -1;

            uint direction = astTree[stmtIdx].Payload; // 0 = to, 1 = downto

            int varHlir = BuildExpression(astTree, children[0]);
            int initHlir = BuildExpression(astTree, children[1]);
            int finalHlir = BuildExpression(astTree, children[2]);
            int bodyHlir = ProcessStatement(astTree, children[3]);
            if (varHlir < 0 || initHlir < 0 || finalHlir < 0 || bodyHlir < 0) return -1;

            return _builder.Add((byte)HlirNodeKind.For, 0, direction, HlirPayloadKind.Immediate, varHlir, initHlir, finalHlir, bodyHlir);
        }

        private int ProcessReturnStatement(AstTree astTree, int stmtIdx)
        {
            var children = astTree.Children(stmtIdx);
            if (children.Length == 0)
                return _builder.Add((byte)HlirNodeKind.Return, 0, 0, HlirPayloadKind.None);

            int valueHlir = BuildExpression(astTree, children[0]);
            if (valueHlir < 0) return -1;

            return _builder.Add((byte)HlirNodeKind.Return, 0, 0, HlirPayloadKind.None, valueHlir);
        }

        private int ProcessMethodCall(AstTree astTree, int stmtIdx)
        {
            var children = astTree.Children(stmtIdx);
            if (children.Length < 1) return -1;

            uint nameOffset = astTree[stmtIdx].Payload;

            var argIndices = new List<int>();
            for (int i = 0; i < children.Length; i++)
            {
                int argHlir = BuildExpression(astTree, children[i]);
                if (argHlir >= 0)
                    argIndices.Add(argHlir);
            }

            if (argIndices.Count == 0)
                return _builder.Add((byte)HlirNodeKind.Call, 0, nameOffset, HlirPayloadKind.PoolOffset);

            return _builder.Add((byte)HlirNodeKind.Call, 0, nameOffset, HlirPayloadKind.PoolOffset, argIndices.ToArray());
        }

        private int BuildExpression(AstTree astTree, int exprIdx)
        {
            if (exprIdx < 0 || exprIdx >= astTree.Count) return -1;

            var node = astTree[exprIdx];
            PascalAstNodeKind kind = (PascalAstNodeKind)node.Kind;

            switch (kind)
            {
                case PascalAstNodeKind.LiteralInt:
                    return _builder.Add((byte)HlirNodeKind.LiteralInt, 0, node.Payload, HlirPayloadKind.Immediate);
                case PascalAstNodeKind.LiteralString:
                    return _builder.Add((byte)HlirNodeKind.LiteralString, 0, node.Payload, HlirPayloadKind.PoolOffset);
                case PascalAstNodeKind.LiteralBool:
                    return _builder.Add((byte)HlirNodeKind.LiteralBool, 0, node.Payload, HlirPayloadKind.Immediate);
                case PascalAstNodeKind.Identifier:
                    if (_constantValues.TryGetValue(node.Payload, out uint constVal))
                    {
                        // Named constant: fold to its literal value.
                        return _builder.Add((byte)HlirNodeKind.LiteralInt, 0, constVal, HlirPayloadKind.Immediate);
                    }
                    if (!_declaredVariables.Contains(node.Payload))
                    {
                        _errors.Add($"Undefined variable '{_pool.Resolve(node.Payload)}'");
                        return -1;
                    }
                    return _builder.Add((byte)HlirNodeKind.Identifier, 0, node.Payload, HlirPayloadKind.PoolOffset);
                case PascalAstNodeKind.BinaryOp:
                    return BuildBinaryOp(astTree, exprIdx);
                case PascalAstNodeKind.MethodCall:
                    return ProcessMethodCall(astTree, exprIdx);
                default:
                    return -1;
            }
        }
        private int BuildBinaryOp(AstTree astTree, int exprIdx)
        {
            var children = astTree.Children(exprIdx);
            if (children.Length < 2) return -1;

            char op = (char)astTree[exprIdx].Payload;

            // Fold constant integer expressions (e.g. 1 + 2 -> 3) so the target
            // emits a single immediate load.
            var leftNode = astTree[children[0]];
            var rightNode = astTree[children[1]];
            if ((PascalAstNodeKind)leftNode.Kind == PascalAstNodeKind.LiteralInt &&
                (PascalAstNodeKind)rightNode.Kind == PascalAstNodeKind.LiteralInt &&
                uint.TryParse(_pool.Resolve(leftNode.Payload), out uint leftVal) &&
                uint.TryParse(_pool.Resolve(rightNode.Payload), out uint rightVal))
            {
                uint folded = op switch
                {
                    '+' => leftVal + rightVal,
                    '-' => leftVal - rightVal,
                    '*' => leftVal * rightVal,
                    '/' => rightVal != 0 ? leftVal / rightVal : 0,
                    _ => 0
                };
                // Intern the folded value so the MLIR/backend layers treat it as a
                // literal string (numeric) rather than a raw value or pool offset.
                uint foldedOffset = _pool.Intern(folded.ToString());
                return _builder.Add((byte)HlirNodeKind.LiteralInt, 0, foldedOffset, HlirPayloadKind.Immediate);
            }

            int leftHlir = BuildExpression(astTree, children[0]);
            int rightHlir = BuildExpression(astTree, children[1]);
            if (leftHlir < 0 || rightHlir < 0) return -1;

            return _builder.Add((byte)HlirNodeKind.BinaryOp, 0, (uint)op, HlirPayloadKind.Immediate, leftHlir, rightHlir);
        }
    }
}
