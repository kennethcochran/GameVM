using System;
using System.Collections.Generic;
using GameVM.Compiler.Core.IR.Slab;
using GameVM.Compiler.Core.IR.SlabProcessing;
using GameVM.Compiler.Core.IR.Buffers;
using static GameVM.Compiler.Core.IR.Slab.InstructionMetadataFlags;
using static GameVM.Compiler.Core.IR.Slab.InstructionMetadata;

namespace GameVM.Compiler.Core.IR.Transformers
{
    public sealed class AstSlabToHlirSlabTransformer
    {
        private readonly ArenaAllocator _arena;
        private readonly StringPool _stringPool;
        private readonly Dictionary<uint, string> _variableNames;
        private readonly Dictionary<uint, byte> _variableTypes;
        private readonly List<string> _errors;
#pragma warning disable S1450 // Field can be made readonly
        private bool _hasError;
#pragma warning restore S1450 // Field can be made readonly

        public AstSlabToHlirSlabTransformer(ArenaAllocator arena, StringPool stringPool)
        {
            _arena = arena ?? throw new ArgumentNullException(nameof(arena));
            _stringPool = stringPool ?? throw new ArgumentNullException(nameof(stringPool));
            _variableNames = new Dictionary<uint, string>();
            _variableTypes = new Dictionary<uint, byte>();
            _errors = new List<string>();
            _hasError = false;
        }

        public uint[] Transform(uint[] astSlab)
        {
            if (astSlab == null || astSlab.Length < SlabHeader.HeaderIndex.Length)
            {
                throw new ArgumentException("Invalid AST slab: too small or null", nameof(astSlab));
            }

            var header = SlabHeader.Read(astSlab);
            if (!header.HasValidMagic())
            {
                throw new ArgumentException("Invalid AST slab: invalid magic number");
            }

            _arena.Reset();
            _variableNames.Clear();
            _variableTypes.Clear();
            _errors.Clear();
            _hasError = false;

            // Write HLIR header (stage = 1)
            var headerOffset = _arena.Allocate(SlabHeader.HeaderIndex.Length);
            var headerData = SlabHeader.ForStage(1, 0); // Will update function count later
            var headerBytes = new uint[SlabHeader.HeaderIndex.Length];
            headerData.WriteTo(headerBytes);
            _arena.Write(headerOffset, headerBytes);

            int offset = SlabHeader.HeaderIndex.Length;
            int functionCount = 0;

            while (offset < astSlab.Length)
            {
                var metadata = astSlab[offset];
                var size = DecodeSize(metadata);
                var kind = DecodeKind(metadata);

                if (size == 0 || offset + size > astSlab.Length)
                    break;

                if (kind == METHOD_DECLARATION)
                {
                    ProcessFunction(astSlab, offset, size);
                    functionCount++;
                }

                offset += size;
            }

            // If we had an error, throw an exception with the first error message
            if (_hasError)
            {
                string errorMessage = _errors.Count > 0 ? _errors[0] : "Semantic analysis failed";
                throw new InvalidOperationException(errorMessage);
            }

            // Update header with actual function count
            var finalHeader = SlabHeader.ForStage(1, (uint)functionCount, 0);
            var finalHeaderData = new uint[SlabHeader.HeaderIndex.Length];
            finalHeader.WriteTo(finalHeaderData);
            _arena.Write(headerOffset, finalHeaderData);

            return _arena.ToContiguousArray();
        }

        private void ProcessFunction(uint[] astSlab, int funcOffset, int funcSize)
        {
            var _ = funcSize; // suppress unused warning
            var funcNameHash = astSlab[funcOffset + 1];
            var bodyOffset = (int)astSlab[funcOffset + 2];

            // Emit HLIR function declaration
            var funcInstrSize = 3; // metadata + nameHash + bodyStartOffset (placeholder)
            var funcStartOffset = _arena.Allocate(funcInstrSize);
            var funcNameHashHlir = funcNameHash; // reuse hash

            _arena.Write(funcStartOffset, Encode(HLIR_LABEL, (byte)funcInstrSize, 1), funcNameHashHlir);

            // Process function body as a block
            ProcessBlock(astSlab, bodyOffset);
        }

        private void ProcessBlock(uint[] astSlab, int blockOffset)
        {
            // AST BLOCK: [metadata, statementOffset1, statementOffset2, ...]
            // Use argCount to determine number of offsets
            var metadata = astSlab[blockOffset];
            var argCount = DecodeArgCount(metadata);

            for (int i = 1; i <= argCount; i++)
            {
                int stmtIndex = blockOffset + i;
                if (stmtIndex >= astSlab.Length) break;

                uint potentialOffset = astSlab[stmtIndex];
                if (potentialOffset >= astSlab.Length) break;

                var meta = astSlab[potentialOffset];
                var size = DecodeSize(meta);
                if (size == 0 || (int)potentialOffset + size > astSlab.Length) break;

                var kind = DecodeKind(meta);
                ProcessStatement(astSlab, (int)potentialOffset, kind);
            }
        }

        private void ProcessStatement(uint[] astSlab, int offset, byte kind)
        {
            switch (kind)
            {
                case ASSIGNMENT:
                    ProcessAssignment(astSlab, offset);
                    break;
                case EXPRESSION_STATEMENT:
                    ProcessExpressionStatement(astSlab, offset);
                    break;
                case IF_STATEMENT:
                    ProcessIfStatement(astSlab, offset);
                    break;
                case WHILE_STATEMENT:
                    ProcessWhileStatement(astSlab, offset);
                    break;
                case RETURN_STATEMENT:
                    ProcessReturnStatement(astSlab, offset);
                    break;
                case BLOCK:
                    ProcessBlock(astSlab, offset);
                    break;
                case VARIABLE_DECLARATION:
                    ProcessVariableDeclaration(astSlab, offset);
                    break;
                default:
                    if (IsExpressionKind(kind))
                    {
                        ProcessExpressionStatement(astSlab, offset);
                    }
                    break;
            }
        }

        private void ProcessAssignment(uint[] astSlab, int offset)
        {
            var targetOffset = astSlab[offset + 1];
            var valueOffset = astSlab[offset + 2];

            // Check if target variable exists
            if (!IsVariableDefined(astSlab, targetOffset))
            {
                _hasError = true;
                _errors.Add($"Undefined variable: {GetVariableName(astSlab, targetOffset)}");
                return;
            }

            var targetStr = ResolveExpression(astSlab, targetOffset);
            var valueStr = ResolveExpression(astSlab, valueOffset);

            // Check type compatibility
            byte targetType = GetVariableType(astSlab, targetOffset);
            byte valueType = GetExpressionType(astSlab, valueOffset);
            
            if (IsTypeMismatch(targetType, valueType))
            {
                _hasError = true;
                _errors.Add($"Type mismatch: cannot assign {GetTypeName(valueType)} to {GetTypeName(targetType)}");
                return;
            }

            var instrSize = 3; // metadata + targetPoolOffset + valuePoolOffset
            var startOffset = _arena.Allocate(instrSize);

            var targetPoolOffset = _stringPool.Intern(targetStr);
            var valuePoolOffset = _stringPool.Intern(valueStr);

            _arena.Write(startOffset, Encode(HLIR_ASSIGN, (byte)instrSize, 2), targetPoolOffset, valuePoolOffset);
        }

        private void ProcessExpressionStatement(uint[] astSlab, int offset)
        {
            var exprOffset = astSlab[offset + 1];
            var exprStr = ResolveExpression(astSlab, exprOffset);

            var instrSize = 3; // metadata + targetPoolOffset + valuePoolOffset (target is "_temp")
            var startOffset = _arena.Allocate(instrSize);

            var targetPoolOffset = _stringPool.Intern("_temp");
            var valuePoolOffset = _stringPool.Intern(exprStr);

            _arena.Write(startOffset, Encode(HLIR_ASSIGN, (byte)instrSize, 2), targetPoolOffset, valuePoolOffset);
        }

        private void ProcessIfStatement(uint[] astSlab, int offset)
        {
            var conditionOffset = astSlab[offset + 1];
            var thenOffset = astSlab[offset + 2];
            var elseOffset = offset + 3 < astSlab.Length ? astSlab[offset + 3] : 0;
            var hasElse = elseOffset != 0 && elseOffset < astSlab.Length;

            var conditionStr = ResolveExpression(astSlab, conditionOffset);

            var thenLabel = $"then_{Guid.NewGuid():N}";
            var endLabel = $"end_{Guid.NewGuid():N}";
            var elseLabel = hasElse ? $"else_{Guid.NewGuid():N}" : endLabel;

            EmitLabel(thenLabel);
            EmitConditionalBranch(conditionStr, elseLabel);
            EmitLabel(endLabel);

            if (hasElse)
            {
                // Process then block
                ProcessBlock(astSlab, (int)thenOffset);
                // Process else block
                ProcessBlock(astSlab, (int)elseOffset);
            }
            else
            {
                // Process then block
                ProcessBlock(astSlab, (int)thenOffset);
            }
        }

        private void ProcessWhileStatement(uint[] astSlab, int offset)
        {
            var conditionOffset = astSlab[offset + 1];
            var bodyOffset = astSlab[offset + 2];

            var conditionStr = ResolveExpression(astSlab, conditionOffset);

            var loopLabel = $"loop_{Guid.NewGuid():N}";
            var endLabel = $"end_{Guid.NewGuid():N}";

            EmitLabel(loopLabel);
            EmitConditionalBranch(conditionStr, endLabel);
            EmitLabel(endLabel);

            // Process body
            ProcessBlock(astSlab, (int)bodyOffset);
        }

        private void ProcessReturnStatement(uint[] astSlab, int offset)
        {
            if (offset + 1 < astSlab.Length)
            {
                var exprOffset = astSlab[offset + 1];
                if (exprOffset < astSlab.Length)
                {
                    var exprStr = ResolveExpression(astSlab, exprOffset);
                    // In HLIR, return values are handled via assignments to a special return variable
                    var instrSize = 3;
                    var startOffset = _arena.Allocate(instrSize);
                    var targetHash = (uint)"_return".GetHashCode();
                    var valueHash = (uint)exprStr.GetHashCode();
                    _arena.Write(startOffset, Encode(HLIR_RETURN, (byte)instrSize, 2), targetHash, valueHash);
                }
                else
                {
                    // Return with no value
                    var instrSize = 3;
                    var startOffset = _arena.Allocate(instrSize);
                    var targetHash = (uint)"_return".GetHashCode();
                    var valueHash = (uint)0u.GetHashCode(); // 0
                    _arena.Write(startOffset, Encode(HLIR_RETURN, (byte)instrSize, 2), targetHash, valueHash);
                }
            }
            else
            {
                // Return with no value
                var instrSize = 3;
                var startOffset = _arena.Allocate(instrSize);
                var targetHash = (uint)"_return".GetHashCode();
                var valueHash = (uint)0u.GetHashCode();
                _arena.Write(startOffset, Encode(HLIR_RETURN, (byte)instrSize, 2), targetHash, valueHash);
            }
        }

        private void ProcessVariableDeclaration(uint[] astSlab, int offset)
        {
            // VARIABLE_DECLARATION: [metadata, typeKind, varNameOffset]
            // In HLIR, variable declarations become assignments with initial values
            if (astSlab.Length < offset + 3) return;

            byte typeKind = (byte)astSlab[offset + 1];
            uint varNameOffset = astSlab[offset + 2];

            // Register symbol in our tracking dictionaries
            string varName = _stringPool.Resolve(varNameOffset);
            _variableNames[varNameOffset] = varName;
            _variableTypes[varNameOffset] = typeKind;

            // Check for initializer in next instruction
            int nextOffset = offset + 3;
            if (nextOffset < astSlab.Length)
            {
                var nextMeta = astSlab[nextOffset];
                var nextSize = DecodeSize(nextMeta);
                var nextKind = DecodeKind(nextMeta);

                if (nextSize > 0 && IsExpressionKind(nextKind))
                {
                    // Validate initializer type compatibility
                    byte initType = GetExpressionType(astSlab, (uint)nextOffset);
                    if (IsTypeMismatch(typeKind, initType))
                    {
                        _hasError = true;
                        _errors.Add($"Type mismatch: cannot assign {GetTypeName(initType)} to {GetTypeName(typeKind)}");
                        return;
                    }

                    var initStr = ResolveExpression(astSlab, (uint)nextOffset);
                    var instrSize = 3;
                    var startOffset = _arena.Allocate(instrSize);
                    var targetPoolOffset = _stringPool.Intern(varName);
                    var valuePoolOffset = _stringPool.Intern(initStr);
                    _arena.Write(startOffset, Encode(HLIR_ASSIGN, (byte)instrSize, 2), targetPoolOffset, valuePoolOffset);
                    return;
                }
            }

            // No initializer - just declare with zero
            var instrSize2 = 3;
            var startOffset2 = _arena.Allocate(instrSize2);
            var targetPoolOffset2 = _stringPool.Intern(varName);
            var valuePoolOffset2 = _stringPool.Intern("0");
            _arena.Write(startOffset2, Encode(HLIR_ASSIGN, (byte)instrSize2, 2), targetPoolOffset2, valuePoolOffset2);
        }

        private string ResolveExpression(uint[] slab, uint exprOffset)
        {
            if (exprOffset >= slab.Length) return "0";

            var metadata = slab[exprOffset];
            var size = DecodeSize(metadata);
            var kind = DecodeKind(metadata);

            if (size == 0 || exprOffset + size > slab.Length) return "0";

            return kind switch
            {
                LITERAL_INT => ((int)slab[exprOffset + 1]).ToString(),
                LITERAL_STRING => _stringPool.Resolve(slab[exprOffset + 1]),
                LITERAL_BOOL => slab[exprOffset + 1] != 0 ? "true" : "false",
                IDENTIFIER => GetVariableName(slab, exprOffset),
                BINARY_OP => ResolveBinaryOp(slab, exprOffset),
                _ => "0"
            };
        }

        private string GetVariableName(uint[] slab, uint exprOffset)
        {
            if (exprOffset >= slab.Length) return "<unknown>";
            
            var metadata = slab[exprOffset];
            var kind = DecodeKind(metadata);

            if (kind == IDENTIFIER)
            {
                if (exprOffset + 1 >= slab.Length) return "<invalid>";
                uint nameOffset = slab[exprOffset + 1];
                if (_variableNames.TryGetValue(nameOffset, out string? name) && name != null)
                    return name;
                return $"<unknown:{nameOffset:X}>";
            }
            
            return $"<expr:{exprOffset}>";
        }

        private string ResolveBinaryOp(uint[] slab, uint exprOffset)
        {
            // BINARY_OP: [metadata, leftOffset, rightOffset, operatorHash]
            if (slab.Length < exprOffset + 4) return "0";

            uint leftOffset = slab[exprOffset + 1];
            uint rightOffset = slab[exprOffset + 2];
            uint opHash = slab[exprOffset + 3];

            var left = ResolveExpression(slab, leftOffset);
            var right = ResolveExpression(slab, rightOffset);

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

        private static bool IsExpressionKind(byte kind)
        {
            return kind == LITERAL_INT || kind == LITERAL_STRING || 
                   kind == LITERAL_BOOL || kind == IDENTIFIER || kind == BINARY_OP;
        }

        private bool IsVariableDefined(uint[] astSlab, uint exprOffset)
        {
            if (exprOffset >= astSlab.Length) return false;

            var metadata = astSlab[exprOffset];
            var kind = DecodeKind(metadata);

            if (kind == IDENTIFIER)
            {
                if (exprOffset + 1 >= astSlab.Length) return false;
                uint nameOffset = astSlab[exprOffset + 1];
                return _variableNames.ContainsKey(nameOffset);
            }

            // Non-identifier expressions are always "defined" (literals, binary ops)
            return true;
        }

        private byte GetVariableType(uint[] astSlab, uint exprOffset)
        {
            if (exprOffset >= astSlab.Length) return 0;

            var metadata = astSlab[exprOffset];
            var kind = DecodeKind(metadata);

            if (kind == IDENTIFIER)
            {
                if (exprOffset + 1 >= astSlab.Length) return 0;
                uint nameOffset = astSlab[exprOffset + 1];
                if (_variableTypes.TryGetValue(nameOffset, out byte type))
                    return type;
                return 0; // Unknown type
            }

            // Non-identifier expressions: use expression type
            return GetExpressionType(astSlab, exprOffset);
        }

        private byte GetExpressionType(uint[] astSlab, uint exprOffset)
        {
            if (exprOffset >= astSlab.Length) return 0;

            var metadata = astSlab[exprOffset];
            var kind = DecodeKind(metadata);

            return kind switch
            {
                LITERAL_INT => 1,    // Integer
                LITERAL_STRING => 2, // String
                LITERAL_BOOL => 3,   // Boolean
                IDENTIFIER => GetVariableType(astSlab, exprOffset),
                BINARY_OP => 1,      // Arithmetic operations produce Integer (simplified)
                _ => 0               // Unknown
            };
        }

        private static bool IsTypeMismatch(byte targetType, byte valueType)
        {
            // Type 0 is unknown/other - allow assignment (can't validate)
            if (targetType == 0 || valueType == 0) return false;
            
            // Integer can be implicitly converted to Real (type 4)
            if (valueType == 1 && targetType == 4) return false;
            
            // Allow same-type assignments
            if (targetType == valueType) return false;
            
            return true;
        }

        private static string GetTypeName(byte typeKind)
        {
            return typeKind switch
            {
                1 => "Integer",
                2 => "String",
                3 => "Boolean",
                4 => "Real",
                _ => "Unknown"
            };
        }

        private void EmitLabel(string name)
        {
            var instrSize = 2; // metadata + nameHash
            var startOffset = _arena.Allocate(instrSize);
            var nameHash = (uint)name.GetHashCode();
            _arena.Write(startOffset, Encode(HLIR_LABEL, (byte)instrSize, 1), nameHash);
        }

        private void EmitConditionalBranch(string condition, string target)
        {
            var instrSize = 3; // metadata + conditionHash + targetHash
            var startOffset = _arena.Allocate(instrSize);
            var conditionHash = (uint)condition.GetHashCode();
            var targetHash = (uint)target.GetHashCode();
            _arena.Write(startOffset, Encode(HLIR_BRANCH, (byte)instrSize, 2), conditionHash, targetHash);
        }
    }
}