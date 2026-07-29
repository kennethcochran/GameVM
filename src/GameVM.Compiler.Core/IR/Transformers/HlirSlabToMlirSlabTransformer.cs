using System;
using GameVM.Compiler.Core.IR.Slab;
using GameVM.Compiler.Core.IR.SlabProcessing;
using static GameVM.Compiler.Core.IR.Slab.InstructionMetadataFlags;
using static GameVM.Compiler.Core.IR.Slab.InstructionMetadata;

namespace GameVM.Compiler.Core.IR.Transformers
{
    public sealed class HlirSlabToMlirSlabTransformer
    {
        private readonly ArenaAllocator _arena;
        private int _labelCounter;

        public HlirSlabToMlirSlabTransformer(ArenaAllocator arena)
        {
            _arena = arena ?? throw new ArgumentNullException(nameof(arena));
            _labelCounter = 0;
        }

        public uint[] Transform(uint[] hlirSlab)
        {
            if (hlirSlab == null || hlirSlab.Length < SlabHeader.HeaderIndex.Length)
            {
                throw new ArgumentException("Invalid HLIR slab: too small or null", nameof(hlirSlab));
            }

            var header = SlabHeader.Read(hlirSlab);
            if (!header.HasValidMagic())
            {
                throw new ArgumentException("Invalid HLIR slab: invalid magic number");
            }

            var headerOffset = _arena.Allocate(SlabHeader.HeaderIndex.Length);
            var headerData = SlabHeader.ForStage(3, 0); // Stage 3 = MLIR
            var headerBytes = new uint[SlabHeader.HeaderIndex.Length];
            headerData.WriteTo(headerBytes);
            _arena.Write(headerOffset, headerBytes);

            int offset = SlabHeader.HeaderIndex.Length;
            int functionCount = 0;

            while (offset < hlirSlab.Length)
            {
                var metadata = hlirSlab[offset];
                var size = InstructionMetadata.DecodeSize(metadata);
                var kind = InstructionMetadata.DecodeKind(metadata);

                if (size == 0 || offset + size > hlirSlab.Length)
                    break;

                if (kind == METHOD_DECLARATION)
                {
                    ProcessFunction(hlirSlab, offset, size);
                    functionCount++;
                }

                offset += size;
            }

            var finalHeader = SlabHeader.ForStage(3, (uint)functionCount);
            var finalHeaderData = new uint[SlabHeader.HeaderIndex.Length];
            finalHeader.WriteTo(finalHeaderData);
            _arena.Write(headerOffset, finalHeaderData);

            return _arena.ToContiguousArray();
        }

        private void ProcessFunction(uint[] hlirSlab, int funcOffset, int funcSize)
        {
            var funcNameHash = hlirSlab[funcOffset + 1];
            var bodyOffset = funcOffset + 2;

            // Store function name
            var nameHashSlot = _arena.Allocate(1);
            _arena.Write(nameHashSlot, funcNameHash);

            int bodyEndOffset = funcOffset + funcSize;
            int currentOffset = bodyOffset;

            while (currentOffset < bodyEndOffset && currentOffset < hlirSlab.Length)
            {
                var stmtMeta = hlirSlab[currentOffset];
                var stmtSize = InstructionMetadata.DecodeSize(stmtMeta);
                var stmtKind = InstructionMetadata.DecodeKind(stmtMeta);

                if (stmtSize == 0 || currentOffset + stmtSize > hlirSlab.Length)
                    break;

                ProcessStatement(hlirSlab, currentOffset, stmtKind);
                currentOffset += stmtSize;
            }
        }

        private void ProcessStatement(uint[] hlirSlab, int offset, byte kind)
        {
            switch (kind)
            {
                case ASSIGNMENT:
                    ProcessAssignment(hlirSlab, offset);
                    break;
                case EXPRESSION_STATEMENT:
                    ProcessExpressionStatement(hlirSlab, offset);
                    break;
                case IF_STATEMENT:
                    ProcessIfStatement(hlirSlab, offset);
                    break;
                case WHILE_STATEMENT:
                    ProcessWhileStatement(hlirSlab, offset);
                    break;
                case RETURN_STATEMENT:
                    ProcessReturnStatement(hlirSlab, offset);
                    break;
                case BLOCK:
                    ProcessBlock(hlirSlab, offset);
                    break;
                case VARIABLE_DECLARATION:
                    ProcessVariableDeclaration(hlirSlab, offset);
                    break;
                default:
                    if (IsExpressionKind(kind))
                    {
                        ProcessExpressionStatement(hlirSlab, offset);
                    }
                    break;
            }
        }

        private static bool IsExpressionKind(byte kind)
        {
            return kind == LITERAL_INT || kind == LITERAL_STRING || 
                   kind == LITERAL_BOOL || kind == IDENTIFIER || kind == BINARY_OP;
        }

        private void ProcessAssignment(uint[] hlirSlab, int offset)
        {
            var targetOffset = hlirSlab[offset + 1];
            var valueOffset = hlirSlab[offset + 2];

            var targetStr = ResolveExpression(hlirSlab, targetOffset);
            var valueStr = ResolveExpression(hlirSlab, valueOffset);

            var instrSize = 3; // metadata + targetHash + valueHash
            var startOffset = _arena.Allocate(instrSize);

            var targetHash = (uint)targetStr.GetHashCode();
            var valueHash = (uint)valueStr.GetHashCode();

            _arena.Write(startOffset, Encode(MLIR_ASSIGN, (byte)instrSize, 2), targetHash, valueHash);
        }

        private void ProcessExpressionStatement(uint[] hlirSlab, int offset)
        {
            var exprOffset = hlirSlab[offset + 1];
            var exprStr = ResolveExpression(hlirSlab, exprOffset);

            var instrSize = 3; // metadata + targetHash + valueHash
            var startOffset = _arena.Allocate(instrSize);

            var targetHash = (uint)"_temp".GetHashCode();
            var valueHash = (uint)exprStr.GetHashCode();

            _arena.Write(startOffset, Encode(MLIR_ASSIGN, (byte)instrSize, 2), targetHash, valueHash);
        }

        private void ProcessIfStatement(uint[] hlirSlab, int offset)
        {
            var conditionOffset = hlirSlab[offset + 1];
            var elseOffset = offset + 4 < hlirSlab.Length ? hlirSlab[offset + 3] : 0;
            var hasElse = elseOffset != 0 && elseOffset < hlirSlab.Length;

            var conditionStr = ResolveExpression(hlirSlab, conditionOffset);

            var thenLabel = $"then_{_labelCounter++}";
            var endLabel = $"endif_{_labelCounter++}";
            var elseLabel = hasElse ? $"else_{_labelCounter++}" : endLabel;

            EmitLabel(thenLabel);
            EmitConditionalBranch(conditionStr, elseLabel);
            EmitLabel(endLabel);
        }

        private void ProcessWhileStatement(uint[] hlirSlab, int offset)
        {
            var conditionOffset = hlirSlab[offset + 1];

            var conditionStr = ResolveExpression(hlirSlab, conditionOffset);

            var loopLabel = $"loop_{_labelCounter++}";
            var endLabel = $"endloop_{_labelCounter++}";

            EmitLabel(loopLabel);
            EmitConditionalBranch(conditionStr, endLabel);
            EmitLabel(endLabel);
        }

        private void ProcessReturnStatement(uint[] hlirSlab, int offset)
        {
            if (offset + 1 < hlirSlab.Length)
            {
                var exprOffset = hlirSlab[offset + 1];
                if (exprOffset < hlirSlab.Length)
                {
                    var exprStr = ResolveExpression(hlirSlab, exprOffset);
                    // In MLIR, return values are handled via assignments to a special return variable
                    var instrSize = 3;
                    var startOffset = _arena.Allocate(instrSize);
                    var targetHash = (uint)"_return".GetHashCode();
                    var valueHash = (uint)exprStr.GetHashCode();
                    _arena.Write(startOffset, Encode(MLIR_ASSIGN, (byte)instrSize, 2), targetHash, valueHash);
                }
            }
        }

        private void ProcessBlock(uint[] hlirSlab, int offset)
        {
            // BLOCK: [metadata, statementOffset1, statementOffset2, ...]
            // Process each statement in the block
            int stmtIndex = offset + 1;
            while (true)
            {
                if (stmtIndex >= hlirSlab.Length) break;
                
                uint potentialOffset = hlirSlab[stmtIndex];
                if (potentialOffset >= hlirSlab.Length) break;

                var meta = hlirSlab[potentialOffset];
                var size = InstructionMetadata.DecodeSize(meta);
                if (size == 0 || (int)potentialOffset + size > hlirSlab.Length) break;

                var kind = InstructionMetadata.DecodeKind(meta);
                ProcessStatement(hlirSlab, (int)potentialOffset, kind);
                ++stmtIndex;
            }
        }

        private void ProcessVariableDeclaration(uint[] hlirSlab, int offset)
        {
            // VARIABLE_DECLARATION: [metadata, typeKind, varNameHash]
            // In MLIR, variable declarations become assignments with initial values
            if (hlirSlab.Length < offset + 3) return;

            _ = (byte)hlirSlab[offset + 1];
            uint varNameHash = hlirSlab[offset + 2];

            string varName = $"var_{varNameHash:X}";

            // Check for initializer in next instruction
            int nextOffset = offset + 3;
            if (nextOffset < hlirSlab.Length)
            {
                var nextMeta = hlirSlab[nextOffset];
                var nextSize = InstructionMetadata.DecodeSize(nextMeta);
                var nextKind = InstructionMetadata.DecodeKind(nextMeta);

                if (nextSize > 0 && IsExpressionKind(nextKind))
                {
                    var initStr = ResolveExpression(hlirSlab, (uint)nextOffset);
                    var instrSize = 3;
                    var startOffset = _arena.Allocate(instrSize);
                    var targetHash = (uint)varName.GetHashCode();
                    var valueHash = (uint)initStr.GetHashCode();
                    _arena.Write(startOffset, Encode(MLIR_ASSIGN, (byte)instrSize, 2), targetHash, valueHash);
                    return;
                }
            }

            // No initializer - just declare with zero
            var instrSize2 = 3;
            var startOffset2 = _arena.Allocate(instrSize2);
            var targetHash2 = (uint)varName.GetHashCode();
            var valueHash2 = (uint)"0".GetHashCode();
            _arena.Write(startOffset2, Encode(MLIR_ASSIGN, (byte)instrSize2, 2), targetHash2, valueHash2);
        }

        private string ResolveExpression(uint[] slab, uint exprOffset)
        {
            if (exprOffset >= slab.Length) return "0";

            var metadata = slab[exprOffset];
            var size = InstructionMetadata.DecodeSize(metadata);
            var kind = InstructionMetadata.DecodeKind(metadata);

            if (size == 0 || exprOffset + size > slab.Length) return "0";

            return kind switch
            {
                LITERAL_INT => ((int)slab[exprOffset + 1]).ToString(),
                LITERAL_STRING => $"<string_{slab[exprOffset + 1]:X}>",
                LITERAL_BOOL => slab[exprOffset + 1] != 0 ? "true" : "false",
                IDENTIFIER => $"id_{slab[exprOffset + 1]:X}",
                BINARY_OP => ResolveBinaryOp(slab, exprOffset),
                _ => "0"
            };
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

        private void EmitLabel(string name)
        {
            var instrSize = 2; // metadata + nameHash
            var startOffset = _arena.Allocate(instrSize);
            var nameHash = (uint)name.GetHashCode();
            _arena.Write(startOffset, Encode(MLIR_LABEL, (byte)instrSize, 1), nameHash);
        }

        private void EmitConditionalBranch(string condition, string target)
        {
            var instrSize = 3; // metadata + conditionHash + targetHash
            var startOffset = _arena.Allocate(instrSize);
            var conditionHash = (uint)condition.GetHashCode();
            var targetHash = (uint)target.GetHashCode();
            _arena.Write(startOffset, Encode(MLIR_BRANCH, (byte)instrSize, 2), conditionHash, targetHash);
        }
    }
}