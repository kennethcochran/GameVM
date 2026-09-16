using System;
using System.Collections.Generic;
using GameVM.Compiler.Core.Interfaces;
using GameVM.Compiler.Core.IR.Buffers;
using GameVM.Compiler.Pascal.Ast;

namespace GameVM.Compiler.Pascal.Semantics
{
    /// <summary>
    /// Dedicated semantic-analysis pass over the Pascal <see cref="AstTree"/>.
    ///
    /// Runs after the AST is built (by <c>PascalToAstVisitor</c>) and before the
    /// AST -&gt; HLIR lowering. It enforces Pascal semantic rules and records every
    /// violation in a single pass as a <see cref="SemanticError"/> with the source
    /// position of the offending node (line/column from the per-node source-position
    /// side table populated by the visitor). The pass never mutates the tree.
    ///
    /// The two rules mirror exactly the checks that previously ran inside
    /// <c>PascalAstToHlirTransformer</c> (scope guard: move, do not add):
    ///  - an assignment target / expression identifier that is neither a declared
    ///    variable nor a known constant is "undefined";
    ///  - a string or boolean literal cannot be assigned to an Integer variable.
    /// Message text is unchanged so existing grep-based tests keep passing.
    /// </summary>
    public sealed class PascalSemanticAnalyzer
    {
        private readonly StringPool _pool;
        private readonly HashSet<uint> _declaredVariables = new();
        private readonly Dictionary<uint, string> _declaredTypes = new();
        private readonly HashSet<uint> _constants = new();
        private readonly List<SemanticError> _errors = new();
        private IReadOnlyList<(int Line, int Column)> _positions = null!;
        private const string ErrorCodeUndefinedVariable = "PAS0001";
        private const string ErrorCodeTypeMismatch = "PAS0002";

        public PascalSemanticAnalyzer(StringPool stringPool)
        {
            _pool = stringPool ?? throw new ArgumentNullException(nameof(stringPool));
        }

        /// <summary>
        /// Analyzes an <see cref="AstTree"/>, returning every semantic violation found.
        /// <paramref name="positions"/> is the visitor's per-node source-position side
        /// table (node index -&gt; line/column), used to attach line/column to errors.
        /// </summary>
        public IReadOnlyList<SemanticError> Analyze(AstTree astTree, IReadOnlyList<(int Line, int Column)> positions)
        {
            _declaredVariables.Clear();
            _declaredTypes.Clear();
            _constants.Clear();
            _errors.Clear();
            _positions = positions ?? throw new ArgumentNullException(nameof(positions));

            if (astTree.Count == 0)
                return _errors;

            int programIdx = FindProgramRoot(astTree);
            if (programIdx < 0)
                return _errors;

            var programChildren = astTree.Children(programIdx);
            if (programChildren.Length == 0)
                return _errors;

            // Mirror the transformer's ProcessFunction: the last Program child is the
            // body block. Declarations are registered inline by the statement walk,
            // matching the transformer's traversal order.
            int bodyIdx = programChildren[programChildren.Length - 1];
            ProcessStatement(astTree, bodyIdx);

            return _errors;
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

        private void ProcessStatement(AstTree astTree, int stmtIdx)
        {
            if (stmtIdx < 0 || stmtIdx >= astTree.Count)
                return;

            var kind = (PascalAstNodeKind)astTree.GetKind(stmtIdx);
            switch (kind)
            {
                case PascalAstNodeKind.VariableDeclaration:
                    RegisterDeclaration(astTree, stmtIdx);
                    break;
                case PascalAstNodeKind.ConstantDefinition:
                    RegisterConstant(astTree, stmtIdx);
                    break;
                case PascalAstNodeKind.Assignment:
                    ProcessAssignment(astTree, stmtIdx);
                    break;
                case PascalAstNodeKind.IfStatement:
                    ProcessIfStatement(astTree, stmtIdx);
                    break;
                case PascalAstNodeKind.WhileStatement:
                    ProcessWhileStatement(astTree, stmtIdx);
                    break;
                case PascalAstNodeKind.ForStatement:
                    ProcessForStatement(astTree, stmtIdx);
                    break;
                case PascalAstNodeKind.MethodDeclaration:
                    {
                        var mChildren = astTree.Children(stmtIdx);
                        for (int i = 0; i < mChildren.Length - 1; i++)
                        {
                            int childIdx = mChildren[i];
                            PascalAstNodeKind childKind = (PascalAstNodeKind)astTree.GetKind(childIdx);
                            if (childKind == PascalAstNodeKind.VariableDeclaration)
                                RegisterDeclaration(astTree, childIdx);
                            else if (childKind == PascalAstNodeKind.ConstantDefinition)
                                RegisterConstant(astTree, childIdx);
                        }
                        if (mChildren.Length > 0)
                            ProcessStatement(astTree, mChildren[mChildren.Length - 1]);
                    }
                    break;
                case PascalAstNodeKind.ReturnStatement:
                    ProcessReturnStatement(astTree, stmtIdx);
                    break;
                case PascalAstNodeKind.Block:
                    ProcessBlock(astTree, stmtIdx);
                    break;
                default:
                    // Structural artifacts (Program/Block children) — not user statements.
                    break;
            }
        }

        private void ProcessBlock(AstTree astTree, int blockIdx)
        {
            foreach (int childIdx in astTree.Children(blockIdx))
                ProcessStatement(astTree, childIdx);
        }

        /// <summary>Registers a variable declaration's name and type, mirroring the
        /// transformer's ProcessVariableDeclaration.</summary>
        private void RegisterDeclaration(AstTree astTree, int declIdx)
        {
            var children = astTree.Children(declIdx);
            if (children.Length < 1)
                return;

            uint nameOffset = astTree[children[0]].Payload;
            _declaredVariables.Add(nameOffset);

            if (children.Length > 1)
            {
                uint typeOffset = astTree[children[1]].Payload;
                string typeName = _pool.Resolve(typeOffset);
                if (!string.IsNullOrEmpty(typeName))
                    _declaredTypes[nameOffset] = typeName;
            }
        }

        /// <summary>Registers a named constant as a known (non-undefined) identifier,
        /// mirroring the transformer's RegisterConstant (int/bool literals only).</summary>
        private void RegisterConstant(AstTree astTree, int constIdx)
        {
            var children = astTree.Children(constIdx);
            if (children.Length < 2)
                return;

            int nameIdx = children[0];
            var nameNode = astTree[nameIdx];
            if (nameNode.Kind != (byte)PascalAstNodeKind.Identifier)
                return;

            int valueIdx = children[1];
            var valueKind = (PascalAstNodeKind)astTree[valueIdx].Kind;
            if (valueKind == PascalAstNodeKind.LiteralInt || valueKind == PascalAstNodeKind.LiteralBool || valueKind == PascalAstNodeKind.LiteralString)
                _constants.Add(nameNode.Payload);
        }

        private void ProcessAssignment(AstTree astTree, int stmtIdx)
        {
            var children = astTree.Children(stmtIdx);
            if (children.Length < 2)
                return;

            int targetIdx = children[0];
            int valueIdx = children[1];
            var targetNode = astTree[targetIdx];

            if (targetNode.Kind == (byte)PascalAstNodeKind.Identifier)
            {
                uint targetOffset = targetNode.Payload;
                if (!_declaredVariables.Contains(targetOffset))
                {
                    Report(targetIdx, $"Undefined variable '{_pool.Resolve(targetOffset)}'", ErrorCodeUndefinedVariable);
                }
                else if (_declaredTypes.TryGetValue(targetOffset, out string? targetType) &&
                         string.Equals(targetType, "integer", StringComparison.OrdinalIgnoreCase))
                {
                    var valueKind = (PascalAstNodeKind)astTree[valueIdx].Kind;
                    if (valueKind == PascalAstNodeKind.LiteralString || valueKind == PascalAstNodeKind.LiteralBool)
                    {
                        string valueText = _pool.Resolve(astTree[valueIdx].Payload);
                        Report(valueIdx, $"Type mismatch: cannot assign '{valueText}' to Integer variable '{_pool.Resolve(targetOffset)}'", ErrorCodeTypeMismatch);
                    }
                }
            }

            // Never abort early — walk both children (skip target only when
            // it was an undefined Identifier, to avoid double-reporting).
            bool targetWasUndefined = targetNode.Kind == (byte)PascalAstNodeKind.Identifier &&
                                      !_declaredVariables.Contains(targetNode.Payload);
            if (!targetWasUndefined)
                ProcessExpression(astTree, targetIdx);
            ProcessExpression(astTree, valueIdx);
        }


        private void ProcessIfStatement(AstTree astTree, int stmtIdx)
        {
            var children = astTree.Children(stmtIdx);
            if (children.Length < 2)
                return;

            ProcessExpression(astTree, children[0]);
            ProcessStatement(astTree, children[1]);
            if (children.Length > 2)
                ProcessStatement(astTree, children[2]);
        }

        private void ProcessWhileStatement(AstTree astTree, int stmtIdx)
        {
            var children = astTree.Children(stmtIdx);
            if (children.Length < 2)
                return;

            ProcessExpression(astTree, children[0]);
            ProcessStatement(astTree, children[1]);
        }

        private void ProcessForStatement(AstTree astTree, int stmtIdx)
        {
            var children = astTree.Children(stmtIdx);
            if (children.Length < 4)
                return;

            ProcessExpression(astTree, children[0]);
            ProcessExpression(astTree, children[1]);
            ProcessExpression(astTree, children[2]);
            ProcessStatement(astTree, children[3]);
        }

        private void ProcessReturnStatement(AstTree astTree, int stmtIdx)
        {
            var children = astTree.Children(stmtIdx);
            if (children.Length == 0)
                return;
            ProcessExpression(astTree, children[0]);
        }
        // ProcessExpressionStatement removed: default case no longer routes to it.
        // Expression statements reach here only via ProcessExpression.
        private void ProcessExpression(AstTree astTree, int exprIdx)
        {
            if (exprIdx < 0 || exprIdx >= astTree.Count)
                return;

            var node = astTree[exprIdx];
            var kind = (PascalAstNodeKind)node.Kind;

            switch (kind)
            {
                case PascalAstNodeKind.Identifier:
                    uint offset = node.Payload;
                    if (!_declaredVariables.Contains(offset) && !_constants.Contains(offset))
                        Report(exprIdx, $"Undefined variable '{_pool.Resolve(offset)}'", ErrorCodeUndefinedVariable);
                    return;
                case PascalAstNodeKind.BinaryOp:
                    foreach (int childIdx in astTree.Children(exprIdx))
                        ProcessExpression(astTree, childIdx);
                    return;
                case PascalAstNodeKind.MethodCall:
                    // Method calls are structural — no semantic rules apply.
                    return;
                default:
                    return;
            }
        }

        private void Report(int nodeIdx, string message, string errorCode)
        {
            _errors.Add(new SemanticError(message, errorCode, _positions[nodeIdx].Line, _positions[nodeIdx].Column));
        }
    }
}