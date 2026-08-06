using System;
using GameVM.Compiler.Core.IR;
using GameVM.Compiler.Core.IR.Soa;
using GameVM.Compiler.Core.IR.Buffers;

namespace GameVM.Compiler.Core.IR.Transformers
{
    public sealed class HlirSlabToMlirSlabTransformer
    {
        public InstList Transform(InstList hlirSlab)
        {
            if (hlirSlab.Count == 0)
            {
                return EmptyList();
            }

            var builder = new InstListBuilder();

            // Process all instructions in the HLIR slab sequentially
            for (int i = 0; i < hlirSlab.Count; i++)
            {
                ProcessInstruction(hlirSlab, i, builder);
            }

            return builder.Build();
        }

        private static InstList EmptyList()
        {
            return new InstList(
                Array.Empty<byte>(),
                Array.Empty<ushort>(),
                Array.Empty<ushort>(),
                Array.Empty<uint>(),
                Array.Empty<uint>(),
                Array.Empty<uint>(),
                Array.Empty<int>(),
                0,
                0);
        }

        private void ProcessInstruction(InstList hlirSlab, int instIdx, InstListBuilder builder)
        {
            var kind = (MlirInstructionKind)hlirSlab.GetKind(instIdx);
            
            switch (kind)
            {
                case MlirInstructionKind.Label:
                    ProcessLabel(hlirSlab, instIdx, builder);
                    break;
                case MlirInstructionKind.Assign:
                    ProcessAssignment(hlirSlab, instIdx, builder);
                    break;
                case MlirInstructionKind.Branch:
                    ProcessBranch(hlirSlab, instIdx, builder);
                    break;
                case MlirInstructionKind.Call:
                    ProcessCall(hlirSlab, instIdx, builder);
                    break;
                case MlirInstructionKind.Return:
                    ProcessReturn(hlirSlab, instIdx, builder);
                    break;
                case MlirInstructionKind.Variable:
                case MlirInstructionKind.ExpressionStatement:
                    // These are expressions, handled as expression statements
                    ProcessExpressionStatement(hlirSlab, instIdx, builder);
                    break;
                default:
                    // Unknown instruction - preserve as-is
                    CopyInstruction(hlirSlab, instIdx, builder);
                    break;
            }
        }

        private static void ProcessAssignment(InstList hlirSlab, int instIdx, InstListBuilder builder)
        {
            // HLIR_ASSIGN: [metadata, targetSlotId, valueSlotId]
            // SoA format: targetSlotId and valueSlotId are encoded as uint operands
            ReadOnlySpan<uint> operands = hlirSlab.GetOperands(instIdx);
            if (operands.Length >= 2)
            {
                var targetSlotId = operands[0];
                var valueSlotId = operands[1];
                
                // Emit assign instruction with slot identifiers
                builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 2, targetSlotId, valueSlotId);
            }
        }

        private void ProcessExpressionStatement(InstList hlirSlab, int instIdx, InstListBuilder builder)
        {
            // High-level ops like binary expressions are handled within parent statements
            // Direct codegen emits them as Assign instructions
        }

        private static void ProcessBranch(InstList hlirSlab, int instIdx, InstListBuilder builder)
        {
            // HLIR_BRANCH: [metadata, conditionOperandId, targetBlockId]
            ReadOnlySpan<uint> operands = hlirSlab.GetOperands(instIdx);
            if (operands.Length >= 2)
            {
                var conditionOperandId = operands[0];
                var targetBlockId = operands[1];

                builder.Add((byte)MlirInstructionKind.Branch, InstructionFlag.None, 2, conditionOperandId, targetBlockId);
            }
        }

        private static void ProcessCall(InstList hlirSlab, int instIdx, InstListBuilder builder)
        {
            // HLIR_CALL: [metadata, functionId, argCount, argIdentifiers...]
            ReadOnlySpan<uint> operands = hlirSlab.GetOperands(instIdx);
            if (operands.Length < 1) return;

            var functionId = operands[0];
            var argCount = (ushort)(hlirSlab.GetArgCount(instIdx) - 1);

            builder.Add((byte)MlirInstructionKind.Call, InstructionFlag.None, argCount, functionId);
        }

        private static void ProcessReturn(InstList hlirSlab, int instIdx, InstListBuilder builder)
        {
            // HLIR_RETURN: [metadata, valueSlotId?]
            ReadOnlySpan<uint> operands = hlirSlab.GetOperands(instIdx);
            if (operands.Length == 0)
            {
                builder.Add((byte)MlirInstructionKind.Return, InstructionFlag.None, 0);
                return;
            }

            if (operands.Length >= 1)
            {
                var returnValueId = operands[0];
                var targetHandle = (uint)"_return".GetHashCode();
                builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 2, targetHandle, returnValueId);
            }
        }

        private static void ProcessLabel(InstList hlirSlab, int instIdx, InstListBuilder builder)
        {
            ReadOnlySpan<uint> operands = hlirSlab.GetOperands(instIdx);
            if (operands.Length >= 1)
            {
                var functionNameHash = operands[0];
                builder.Add((byte)MlirInstructionKind.Label, InstructionFlag.None, 1, functionNameHash);
            }
        }

        private static void CopyInstruction(InstList hlirSlab, int instIdx, InstListBuilder builder)
        {
            byte kind = hlirSlab.GetKind(instIdx);
            ReadOnlySpan<uint> operands = hlirSlab.GetOperands(instIdx);
            builder.Add(kind, InstructionFlag.None, (ushort)operands.Length, operands);
        }
    }
}