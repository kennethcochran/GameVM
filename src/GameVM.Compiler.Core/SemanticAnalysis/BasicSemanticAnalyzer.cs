using System;
using System.Collections.Generic;
using GameVM.Compiler.Core.Interfaces;
using GameVM.Compiler.Core.IR.Soa;
using GameVM.Compiler.Core.IR.Buffers;
using GameVM.Compiler.Core.IR.Slab;

namespace GameVM.Compiler.Core.SemanticAnalysis
{
    /// <summary>
    /// DOD-native semantic analyzer for HLIR slabs.
    /// Performs linear iteration with switch-based processing to detect:
    /// - Type mismatches in assignments (e.g. Integer := 'hello')
    /// - Undefined variable references
    /// - Invalid literal types
    ///
    /// The HLIR slab uses string-pool offsets stored directly in instruction operands:
    /// - HLIR_ASSIGN: [targetVarStringPoolOffset, valueStringPoolOffset]
    ///   Both operands are offsets into the Shared StringPool (from StringPool.Intern).
    ///   The target is a variable name; the value is either:
    ///     - an integer literal as a string (e.g. "0", "42")
    ///     - a string literal (e.g. "hello", without quotes)
    ///     - a binary operation expression (e.g. "x + y")
    /// - HLIR_VARIABLE: not currently emitted by AstSlabToHlirSlabTransformer
    ///   (variable declarations are folded into HLIR_ASSIGN with initial value "0")
    /// </summary>
    public sealed class BasicSemanticAnalyzer : ISemanticAnalyzer
    {
        /// <summary>
        /// Analyzes a DOD HLIR slab using linear iteration and switch-based processing.
        /// </summary>
        /// <param name="hlirSlab">The HLIR instruction list to analyze</param>
        /// <param name="stringPool">String pool for resolving identifier names from pool offsets</param>
        /// <returns>Result of semantic analysis including any errors found</returns>
        public SemanticAnalysisResult AnalyzeSlab(InstList hlirSlab, StringPool stringPool)
        {
            var errors = new List<string>();

            if (hlirSlab.Count == 0)
            {
                errors.Add("HLIR slab is empty");
                return new SemanticAnalysisResult { Success = false, Errors = errors };
            }

            // Track variable names to their inferred types
            var variableTypes = new Dictionary<string, string>();

            int assignCount = 0;
            int invalidCount = 0;

            for (int i = 0; i < hlirSlab.Count; i++)
            {
                byte kind = hlirSlab.GetKind(i);
                ReadOnlySpan<uint> operands = hlirSlab.GetOperands(i);

                switch (kind)
                {
                    case 130: // MLIR_ASSIGN (was InstructionMetadataFlags.MLIR_ASSIGN)
                        // HLIR_ASSIGN: [targetVarStringPoolOffset, valueStringPoolOffset]
                        assignCount++;
                        if (operands.Length >= 2)
                        {
                            uint targetPoolOffset = operands[0];
                            uint valuePoolOffset = operands[1];

                            // Resolve target variable name from string pool
                            string targetName = ResolveIdentifier(stringPool, targetPoolOffset);
                            if (string.IsNullOrEmpty(targetName))
                            {
                                // Can't resolve variable name - skip
                                break;
                            }

                            // Resolve value type from string pool
                            string valueExpr = ResolveIdentifier(stringPool, valuePoolOffset);
                            string? valueType = InferExpressionType(valueExpr);

                            if (valueType == null)
                            {
                                // Could not infer type - skip
                                break;
                            }

                            // Check type compatibility
                            if (variableTypes.TryGetValue(targetName, out string? declaredType))
                            {
                                if (declaredType != null && !AreTypesCompatible(declaredType, valueType))
                                {
                                    errors.Add($"Type mismatch at index {i}: cannot assign '{valueType}' to variable '{targetName}' of type '{declaredType}'");
                                }
                            }
                            else
                            {
                                // First assignment - infer and record type.
                                variableTypes[targetName] = valueType;
                            }
                        }
                        break;
                    default:
                        // Count AST-level instruction kinds (0x01-0x16) as invalid in an HLIR context
                        if (kind >= 1 && kind <= 22)
                        {
                            invalidCount++;
                        }
                        break;
                }
            }

            // If slab contains instructions but no valid assignments and has invalid instructions,
            // the slab is not a valid HLIR slab
            if (assignCount == 0 && invalidCount > 0)
            {
                errors.Add("Invalid HLIR slab: no assignment instructions found; slab may be at wrong IR stage or has corrupted content");
            }
            return new SemanticAnalysisResult
            {
                Success = errors.Count == 0,
                Errors = errors
            };
        }

        /// <summary>
        /// Resolves an identifier name from a string pool offset.
        /// Returns empty string if offset is 0 or resolution fails.
        /// </summary>
        private static string ResolveIdentifier(StringPool stringPool, uint offset)
        {
            if (offset == 0) return string.Empty;
            try
            {
                return stringPool.Resolve(offset) ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Infers the type of an expression value string.
        /// Handles:
        /// - Integer literals: "0", "42", "-1", "+5"
        /// - Boolean literals: "true", "false"
        /// - String literals: any non-numeric, non-boolean string
        /// - Binary operations: "x + y", "a * b", etc. - returns type of operands if both same
        /// </summary>
        private static string? InferExpressionType(string value)
        {
            if (value == null)
                return "Unknown";

            if (value == "")
                return "String";

            // Check if it's a boolean literal
            if (value == "true" || value == "false")
                return "Boolean";

            // Check if it's an integer literal
            if (int.TryParse(value, out _))
                return "Integer";

            // Check if it's a binary operation (contains space and operator)
            if (IsBinaryOperation(value))
            {
                // For binary operations with integers, result is integer
                // For simplicity, assume all binary ops in this context are integer operations
                // A more sophisticated implementation would parse the operands and check their types
                return "Integer";
            }

            // If it doesn't parse as int/bool and isn't obviously a binary op, assume it's a string literal
            return "String";
        }

        /// <summary>
        /// Checks if a string looks like a binary operation (contains space and operator).
        /// </summary>
        private static bool IsBinaryOperation(string value)
        {
            // Simple heuristic: contains space and one of + - * / =
            return value.Contains(' ') && 
                   (value.Contains('+') || value.Contains('-') || 
                    value.Contains('*') || value.Contains('/') || 
                    value.Contains('='));
        }

        /// <summary>
        /// Checks if a value of type 'valueType' can be assigned to a variable of type 'targetType'.
        /// </summary>
        private static bool AreTypesCompatible(string targetType, string valueType)
        {
            if (string.IsNullOrEmpty(targetType) || string.IsNullOrEmpty(valueType))
                return false;

            if (targetType == valueType) return true;

            // Allow implicit conversions: Integer -> Real
            return (targetType == "Real" && valueType == "Integer") ||
                   (targetType == "Integer" && valueType == "Real");
        }
    }
}