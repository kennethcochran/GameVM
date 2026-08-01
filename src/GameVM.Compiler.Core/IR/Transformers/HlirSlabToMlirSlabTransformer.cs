using System;
using GameVM.Compiler.Core.IR.Slab;
using GameVM.Compiler.Core.IR.SlabProcessing;
using GameVM.Compiler.Core.IR.Buffers;
using static GameVM.Compiler.Core.IR.Slab.InstructionMetadataFlags;
using static GameVM.Compiler.Core.IR.Slab.InstructionMetadata;

namespace GameVM.Compiler.Core.IR.Transformers
{
    public sealed class HlirSlabToMlirSlabTransformer
    {
        private readonly ArenaAllocator _arena;
        private StringPool? _stringPool;

        public HlirSlabToMlirSlabTransformer(ArenaAllocator arena)
        {
            _arena = arena ?? throw new ArgumentNullException(nameof(arena));
        }

        public uint[] Transform(uint[] hlirSlab, StringPool stringPool)
        {
            _stringPool = stringPool ?? throw new ArgumentNullException(nameof(stringPool));
            
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
            var headerData = SlabHeader.ForStage(2, 0); // Stage 2 = MLIR
            var headerBytes = new uint[SlabHeader.HeaderIndex.Length];
            headerData.WriteTo(headerBytes);
            _arena.Write(headerOffset, headerBytes);

            int offset = SlabHeader.HeaderIndex.Length;
            int functionCount = 0;

            // Process all instructions in the HLIR slab sequentially
            while (offset < hlirSlab.Length)
            {
                var metadata = hlirSlab[offset];
                var size = InstructionMetadata.DecodeSize(metadata);
                var kind = InstructionMetadata.DecodeKind(metadata);

                if (size == 0 || offset + size > hlirSlab.Length)
                    break;

                ProcessStatement(hlirSlab, offset, kind);
                if (kind == HLIR_LABEL)
                {
                    functionCount++;
                }

                offset += size;
            }

            var finalHeader = SlabHeader.ForStage(2, (uint)functionCount);
            var finalHeaderData = new uint[SlabHeader.HeaderIndex.Length];
            finalHeader.WriteTo(finalHeaderData);
            _arena.Write(headerOffset, finalHeaderData);

            return _arena.ToContiguousArray();
        }

        private void ProcessLabel(uint[] hlirSlab, int funcOffset)
        {
            var funcNameHash = hlirSlab[funcOffset + 1];
            
            // Create MLIR_LABEL for function name: [metadata, nameHash]
            var labelOffset = _arena.Allocate(2);
            _arena.Write(labelOffset, Encode(MLIR_LABEL, (byte)2, (byte)1), funcNameHash);
        }

        private void ProcessStatement(uint[] hlirSlab, int offset, byte kind)
        {
            switch (kind)
            {
                case HLIR_ASSIGN:
                    ProcessAssignment(hlirSlab, offset);
                    break;
                case HLIR_BRANCH:
                    ProcessBranch(hlirSlab, offset);
                    break;
                case HLIR_CALL:
                    ProcessCall(hlirSlab, offset);
                    break;
                case HLIR_RETURN:
                    ProcessReturn(hlirSlab, offset);
                    break;
                case HLIR_LABEL:
                    ProcessLabel(hlirSlab, offset);
                    break;
                case HLIR_VARIABLE:
                case HLIR_LITERAL:
                    // These are expressions, handled as expression statements
                    ProcessExpressionStatement(hlirSlab, offset);
                    break;
                default:
                    // Unknown instruction - preserve as-is or tombstone
                    CopyInstruction(hlirSlab, offset);
                    break;
            }
        }

        private void ProcessAssignment(uint[] hlirSlab, int offset)
        {
            // HLIR_ASSIGN: [metadata, targetPoolOffset, valuePoolOffset]
            if (offset + 2 >= hlirSlab.Length) return;

            var targetPoolOffset = hlirSlab[offset + 1];
            var valuePoolOffset = hlirSlab[offset + 2];

            var instrSize = 3; // metadata + targetPoolOffset + valuePoolOffset
            var startOffset = _arena.Allocate(instrSize);

            // Pass through StringPool offsets directly
            _arena.Write(startOffset, Encode(MLIR_ASSIGN, (byte)instrSize, 2), targetPoolOffset, valuePoolOffset);
        }

        private void ProcessExpressionStatement(uint[] hlirSlab, int offset)
        {
            var exprOffset = hlirSlab[offset + 1];
            var exprStr = ResolveExpression(hlirSlab, exprOffset);

            var instrSize = 3; // metadata + targetPoolOffset + valuePoolOffset
            var startOffset = _arena.Allocate(instrSize);

            var targetPoolOffset = _stringPool!.Intern("_temp");
            var valuePoolOffset = _stringPool!.Intern(exprStr);

            _arena.Write(startOffset, Encode(MLIR_ASSIGN, (byte)instrSize, 2), targetPoolOffset, valuePoolOffset);
        }

        private void ProcessBranch(uint[] hlirSlab, int offset)
        {
            // HLIR_BRANCH: [metadata, conditionHash, targetLabelHash]
            if (offset + 2 >= hlirSlab.Length) return;

            var conditionHash = hlirSlab[offset + 1];
            var targetHash = hlirSlab[offset + 2];

            var conditionStr = $"cond_{conditionHash:X}";
            var targetStr = $"label_{targetHash:X}";

            var instrSize = 3; // metadata + conditionHash + targetHash
            var startOffset = _arena.Allocate(instrSize);

            var conditionHashMlir = (uint)conditionStr.GetHashCode();
            var targetHashMlir = (uint)targetStr.GetHashCode();

            _arena.Write(startOffset, Encode(MLIR_BRANCH, (byte)instrSize, 2), conditionHashMlir, targetHashMlir);
        }

        private void ProcessCall(uint[] hlirSlab, int offset)
        {
            // HLIR_CALL: [metadata, functionHash, argHashes...]
            if (offset + 1 >= hlirSlab.Length) return;

            var functionHash = hlirSlab[offset + 1];

            var funcName = $"func_{functionHash:X}";
            var funcHashMlir = (uint)funcName.GetHashCode();

            var argCount = 0;
            if (offset + 2 < hlirSlab.Length)
            {
                // Count remaining args
                argCount = InstructionMetadata.DecodeArgCount(hlirSlab[offset]) - 1;
            }

            var instrSize = (byte)(2 + argCount);
            var startOffset = _arena.Allocate(instrSize);

            var buffer = new uint[instrSize];
            buffer[0] = Encode(MLIR_CALL, instrSize, (byte)(1 + argCount));
            buffer[1] = funcHashMlir;
            for (int i = 0; i < argCount && offset + 2 + i < hlirSlab.Length; i++)
            {
                buffer[2 + i] = hlirSlab[offset + 2 + i];
            }
            _arena.Write(startOffset, buffer);
        }

        private void ProcessReturn(uint[] hlirSlab, int offset)
        {
            // HLIR_RETURN: [metadata, exprHash?]
            if (offset + 1 < hlirSlab.Length)
            {
                var exprHash = hlirSlab[offset + 1];
                var exprStr = ResolveExpression(hlirSlab, exprHash);
                
                // In MLIR, return values are handled via assignments to a special return variable
                var instrSize = 3;
                var startOffset = _arena.Allocate(instrSize);
                var targetHash = (uint)"_return".GetHashCode();
                var valueHash = (uint)exprStr.GetHashCode();
                _arena.Write(startOffset, Encode(MLIR_ASSIGN, (byte)instrSize, 2), targetHash, valueHash);
            }
        }

        private void CopyInstruction(uint[] sourceSlab, int offset)
        {
            if (offset >= sourceSlab.Length) return;

            var metadata = sourceSlab[offset];
            var size = DecodeSize(metadata);

            if (size == 0) return;

            var destOffset = _arena.Allocate(size);
            var buffer = new uint[size];
            Array.Copy(sourceSlab, offset, buffer, 0, size);
            _arena.Write(destOffset, buffer);
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
    }
}