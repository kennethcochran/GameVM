using System;
using System.Collections.Generic;
using GameVM.Compiler.Core.IR;
using GameVM.Compiler.Core.IR.Soa;
using GameVM.Compiler.Core.IR.Buffers;

namespace GameVM.Compiler.Core.IR.Transformers
{
    /// <summary>
    /// Transforms an AST InstList (from Pascal frontend) to an HLIR InstList.
    /// </summary>
    public sealed class AstSlabToHlirSlabTransformer
    {
        private readonly StringPool _stringPool;
        private readonly Dictionary<uint, string> _variableNames;
        private readonly Dictionary<uint, byte> _variableTypes;
        private readonly List<string> _errors;
        // Counter for deterministic label generation
        private int _labelCounter;

        public AstSlabToHlirSlabTransformer(StringPool stringPool)
        {
            _stringPool = stringPool ?? throw new ArgumentNullException(nameof(stringPool));
            _variableNames = new Dictionary<uint, string>();
            _variableTypes = new Dictionary<uint, byte>();
            _errors = new List<string>();
        }

        /// <summary>
        /// Transforms an AST InstList to an HLIR InstList.
        /// </summary>
        /// <param name="astSlab">The AST instruction list to transform.</param>
        /// <returns>The resulting HLIR instruction list.</returns>
        public InstList Transform(InstList astSlab)
        {
            // Reset state
            _variableNames.Clear();
            _variableTypes.Clear();
            _errors.Clear();
            _labelCounter = 0;

            var builder = new InstListBuilder();

            // Process the AST as a sequence of top-level declarations
            // In Pascal AST: PROGRAM -> METHOD_DECLARATION -> BLOCK -> statements
            for (int i = 0; i < astSlab.Count; i++)
            {
                AstNodeKind kind = (AstNodeKind)astSlab.GetKind(i);
                if (kind == AstNodeKind.MethodDeclaration)
                {
                    // METHOD_DECLARATION: [nameOffset, bodyIdx]
                    ReadOnlySpan<uint> operands = astSlab.GetOperands(i);
                    int bodyIdx = (int)operands[1]; // InstIndex as uint

                    ProcessFunction(astSlab, bodyIdx, builder);
                }
                // Skip other top-level constructs for now (like PROGRAM node)
                // The PROGRAM node would be at index 0 with operands pointing to the method
            }

            if (_errors.Count > 0)
            {
                string errorMessage = _errors[0];
                throw new InvalidOperationException(errorMessage);
            }

            return builder.Build();
        }

        private void ProcessFunction(InstList astSlab, int bodyIdx, InstListBuilder builder)
        {
            // In HLIR, we represent a function as:
            // HLIR_LABEL: [functionNameHash]  (acts as function entry point)
            // Use deterministic label generation based on label counter
            uint functionNameHash = _stringPool.Intern($"_func_{_labelCounter:00000000}");
            _labelCounter++;
            builder.Add((byte)MlirInstructionKind.Label, InstructionFlag.None, 0, functionNameHash);
            // Process the function body (the BLOCK instruction)
            ProcessBlock(astSlab, bodyIdx, builder);
        }

        private void ProcessBlock(InstList astSlab, int blockIdx, InstListBuilder builder)
        {
            // BLOCK: [statementIdx1, statementIdx2, ...]
            ReadOnlySpan<uint> operands = astSlab.GetOperands(blockIdx);
            
            foreach (uint operand in operands)
            {
                int stmtIdx = (int)operand; // InstIndex as uint
                if (stmtIdx < 0 || stmtIdx >= astSlab.Count) continue;

                AstNodeKind stmtKind = (AstNodeKind)astSlab.GetKind(stmtIdx);
                ProcessStatement(astSlab, stmtIdx, stmtKind, builder);
            }
        }

        private void ProcessStatement(InstList astSlab, int stmtIdx, AstNodeKind stmtKind, InstListBuilder builder)
        {
            switch (stmtKind)
            {
                case AstNodeKind.Assignment:
                    ProcessAssignment(astSlab, stmtIdx, builder);
                    break;
                case AstNodeKind.ExpressionStatement:
                    ProcessExpressionStatement(astSlab, stmtIdx, builder);
                    break;
                case AstNodeKind.IfStatement:
                    ProcessIfStatement(astSlab, stmtIdx, builder);
                    break;
                case AstNodeKind.WhileStatement:
                    ProcessWhileStatement(astSlab, stmtIdx, builder);
                    break;
                case AstNodeKind.ReturnStatement:
                    ProcessReturnStatement(astSlab, stmtIdx, builder);
                    break;
                case AstNodeKind.Block:
                    ProcessBlock(astSlab, stmtIdx, builder);
                    break;
                case AstNodeKind.VariableDeclaration:
                    ProcessVariableDeclaration(astSlab, stmtIdx, builder);
                    break;
                default:
                    if (IsExpressionKind(stmtKind))
                    {
                        // Treat expression as expression statement (assign to temp)
                        ProcessExpressionStatement(astSlab, stmtIdx, builder);
                    }
                    break;
            }
        }

        private void ProcessAssignment(InstList astSlab, int stmtIdx, InstListBuilder builder)
        {
            // ASSIGNMENT: [targetIdx, valueIdx]
            ReadOnlySpan<uint> operands = astSlab.GetOperands(stmtIdx);
            if (operands.Length < 2) return;

            int targetIdx = (int)operands[0]; // InstIndex
            int valueIdx = (int)operands[1];  // InstIndex

            if (targetIdx < 0 || targetIdx >= astSlab.Count ||
                valueIdx < 0 || valueIdx >= astSlab.Count)
            {
                return;
            }

            // Check if target is an identifier and if so, validate it's declared
            AstNodeKind targetKind = (AstNodeKind)astSlab.GetKind(targetIdx);
            if (targetKind == AstNodeKind.Identifier)
            {
                ReadOnlySpan<uint> targetOperands = astSlab.GetOperands(targetIdx);
                if (targetOperands.Length >= 1)
                {
                    uint nameOffset = targetOperands[0];
                    if (!_variableNames.ContainsKey(nameOffset))
                    {
                        string varName = _stringPool.Resolve(nameOffset);
                        _errors.Add($"Undefined variable '{varName}'");
                        return;
                    }
                }
            }

            // Resolve target and value expressions to strings
            string targetStr = ResolveExpression(astSlab, targetIdx);
            string valueStr = ResolveExpression(astSlab, valueIdx);

            // For now, we'll skip type checking to match the original behavior
            // In a full implementation, we would check type compatibility here

            var targetPoolOffset = _stringPool.Intern(targetStr);
            var valuePoolOffset = _stringPool.Intern(valueStr);

            // HLIR_ASSIGN: [targetPoolOffset, valuePoolOffset]
            builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 2, targetPoolOffset, valuePoolOffset);
        }

        private void ProcessExpressionStatement(InstList astSlab, int stmtIdx, InstListBuilder builder)
        {
            // EXPRESSION_STATEMENT: [exprIdx]
            ReadOnlySpan<uint> operands = astSlab.GetOperands(stmtIdx);
            if (operands.Length < 1) return;

            int exprIdx = (int)operands[0]; // InstIndex
            if (exprIdx < 0 || exprIdx >= astSlab.Count) return;

            string exprStr = ResolveExpression(astSlab, exprIdx);

            // In HLIR, expression statements assign to a temporary variable "_temp"
            var targetPoolOffset = _stringPool.Intern("_temp");
            var valuePoolOffset = _stringPool.Intern(exprStr);

            // HLIR_ASSIGN: [targetPoolOffset, valuePoolOffset]
            builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 2, targetPoolOffset, valuePoolOffset);
        }

        private void ProcessIfStatement(InstList astSlab, int stmtIdx, InstListBuilder builder)
        {
            // IF_STATEMENT: [conditionIdx, thenIdx, elseIdx?]
            ReadOnlySpan<uint> operands = astSlab.GetOperands(stmtIdx);
            if (operands.Length < 2) return;

            int conditionIdx = (int)operands[0]; // InstIndex
            int thenIdx = (int)operands[1];    // InstIndex
            int? elseIdx = operands.Length >= 3 ? (int)operands[2] : (int?)null;

            if (conditionIdx < 0 || conditionIdx >= astSlab.Count ||
                thenIdx < 0 || thenIdx >= astSlab.Count ||
                (elseIdx.HasValue && (elseIdx.Value < 0 || elseIdx.Value >= astSlab.Count)))
            {
                return;
            }

            string conditionStr = ResolveExpression(astSlab, conditionIdx);

            int labelBase = _labelCounter++;
            var thenLabel = $"L_if_then_{labelBase}";
            var endLabel = $"L_if_end_{labelBase}";
            var elseLabel = elseIdx.HasValue ? $"L_if_else_{labelBase}" : endLabel;
            builder.Add((byte)MlirInstructionKind.Label, InstructionFlag.None, 0, _stringPool.Intern(thenLabel));
            builder.Add((byte)MlirInstructionKind.Branch, InstructionFlag.None, 2, 
                        _stringPool.Intern(conditionStr), _stringPool.Intern(elseLabel));
            
            // Process then branch - operand is a statement index, not necessarily a block
            AstNodeKind thenKind = (AstNodeKind)astSlab.GetKind(thenIdx);
            ProcessStatement(astSlab, thenIdx, thenKind, builder);
            
            // Emit end label
            builder.Add((byte)MlirInstructionKind.Label, InstructionFlag.None, 0, _stringPool.Intern(endLabel));
            
            if (elseIdx.HasValue)
            {
                // Process else branch - operand is a statement index, not necessarily a block
                AstNodeKind elseKind = (AstNodeKind)astSlab.GetKind(elseIdx.Value);
                ProcessStatement(astSlab, elseIdx.Value, elseKind, builder);
                // Fall through to end label (already emitted above)
            }
            // If no else, we fall through to end label
        }

        private void ProcessWhileStatement(InstList astSlab, int stmtIdx, InstListBuilder builder)
        {
            // WHILE_STATEMENT: [conditionIdx, bodyIdx]
            ReadOnlySpan<uint> operands = astSlab.GetOperands(stmtIdx);
            if (operands.Length < 2) return;

            int conditionIdx = (int)operands[0]; // InstIndex
            int bodyIdx = (int)operands[1];      // InstIndex

            if (conditionIdx < 0 || conditionIdx >= astSlab.Count ||
                bodyIdx < 0 || bodyIdx >= astSlab.Count)
            {
                return;
            }

            // Generate deterministic labels
            string loopLabel = $"L_loop_{_variableNames.Count}";
            string endLabel = $"L_end_{_variableNames.Count}";

            // Emit loop label (loop top / condition check)
            builder.Add((byte)MlirInstructionKind.Label, InstructionFlag.None, 1, _stringPool.Intern(loopLabel));

            // Emit condition branch: branch to endLabel if condition is false (0)
            string conditionStr = ResolveExpression(astSlab, conditionIdx);
            builder.Add((byte)MlirInstructionKind.Branch, InstructionFlag.None, 2,
                        _stringPool.Intern(endLabel), _stringPool.Intern(conditionStr));

            // Process body
            AstNodeKind bodyKind = (AstNodeKind)astSlab.GetKind(bodyIdx);
            ProcessStatement(astSlab, bodyIdx, bodyKind, builder);

            // Emit unconditional branch back to loop label
            builder.Add((byte)MlirInstructionKind.Branch, InstructionFlag.None, 1, _stringPool.Intern(loopLabel));

            // Emit end label
            builder.Add((byte)MlirInstructionKind.Label, InstructionFlag.None, 1, _stringPool.Intern(endLabel));
        }

        private void ProcessReturnStatement(InstList astSlab, int stmtIdx, InstListBuilder builder)
        {
            // RETURN_STATEMENT: [exprIdx?] - optional expression
            ReadOnlySpan<uint> operands = astSlab.GetOperands(stmtIdx);
            if (operands.Length == 0)
            {
                // RETURN with no value
                builder.Add((byte)MlirInstructionKind.Return, InstructionFlag.None, 0);
                return;
            }

            if (operands.Length >= 1)
            {
                int exprIdx = (int)operands[0]; // InstIndex (may be 0 if no expr, but we check length)
                if (exprIdx >= 0 && exprIdx < astSlab.Count)
                {
                    // RETURN with value - assign to return variable
                    string exprStr = ResolveExpression(astSlab, exprIdx);
                    var valuePoolOffset = _stringPool.Intern(exprStr);
                    var targetPoolOffset = _stringPool.Intern("_return");

                    // HLIR_ASSIGN: [targetPoolOffset, valuePoolOffset]
                    builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 2, targetPoolOffset, valuePoolOffset);
                }
            }
        }

        private void ProcessVariableDeclaration(InstList astSlab, int stmtIdx, InstListBuilder builder)
        {
            // VARIABLE_DECLARATION: [typeKind, nameOffset]
            ReadOnlySpan<uint> operands = astSlab.GetOperands(stmtIdx);
            if (operands.Length < 2) return;

            byte typeKind = (byte)operands[0]; // byte
            uint nameOffset = operands[1];    // uint (string pool offset)

            string varName = _stringPool.Resolve(nameOffset);
            if (string.IsNullOrEmpty(varName)) return;

            // Register symbol
            _variableNames[nameOffset] = varName;
            _variableTypes[nameOffset] = typeKind;

            // In HLIR, variable declarations become assignments with initial values
            // Initialize to default value based on type kind
            string initStr = typeKind switch
            {
                1 => "0",        // LITERAL_INT
                2 => "",         // LITERAL_STRING
                3 => "false",    // LITERAL_BOOL
                _ => "0"         // Unknown/Integer fallback
            };

            var targetPoolOffset = _stringPool.Intern(varName);
            var valuePoolOffset = _stringPool.Intern(initStr);

            // HLIR_ASSIGN: [targetPoolOffset, valuePoolOffset]
            builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 2, targetPoolOffset, valuePoolOffset);
        }

        private string ResolveExpression(InstList astSlab, int exprIdx)
        {
            if (exprIdx < 0 || exprIdx >= astSlab.Count) return "0";

            AstNodeKind kind = (AstNodeKind)astSlab.GetKind(exprIdx);
            ReadOnlySpan<uint> operands = astSlab.GetOperands(exprIdx);

            return kind switch
            {
                AstNodeKind.LiteralInt => operands.Length >= 1 ? operands[0].ToString() : "0",
                AstNodeKind.LiteralString => operands.Length >= 1 ? _stringPool.Resolve(operands[0]) : "",
                AstNodeKind.LiteralBool => operands.Length >= 1 && operands[0] != 0 ? "true" : "false",
                AstNodeKind.Identifier =>
                    operands.Length >= 1 ? _stringPool.Resolve(operands[0]) : "<unknown>",
                AstNodeKind.BinaryOp => ResolveBinaryOp(astSlab, exprIdx),
                _ => "0"
            };
        }

        private string ResolveBinaryOp(InstList astSlab, int exprIdx)
        {
            // BINARY_OP: [leftIdx, rightIdx, operatorHash]
            ReadOnlySpan<uint> operands = astSlab.GetOperands(exprIdx);
            if (operands.Length < 3) return "0";

            int leftIdx = (int)operands[0];
            int rightIdx = (int)operands[1];
            uint opHash = operands[2];

            string left = ResolveExpression(astSlab, leftIdx);
            string right = ResolveExpression(astSlab, rightIdx);

            // Try to decode operator from hash (simplified)
            string op = opHash switch
            {
                43 => "+",    // '+'
                45 => "-",    // '-'
                42 => "*",    // '*'
                47 => "/",    // '/'
                _ => "?"
            };

            if (int.TryParse(left, out int lVal) && int.TryParse(right, out int rVal))
            {
                return op switch
                {
                    "+" => (lVal + rVal).ToString(),
                    "-" => (lVal - rVal).ToString(),
                    "*" => (lVal * rVal).ToString(),
                    "/" => rVal != 0 ? (lVal / rVal).ToString() : "0",
                    _ => $"({left} {op} {right})"
                };
            }

            return $"({left} {op} {right})";
        }

        private static bool IsExpressionKind(AstNodeKind kind)
        {
            return kind == AstNodeKind.LiteralInt || 
                   kind == AstNodeKind.LiteralString || 
                   kind == AstNodeKind.LiteralBool || 
                   kind == AstNodeKind.Identifier || 
                   kind == AstNodeKind.BinaryOp;
        }
    }
}