using GameVM.Compiler.Core.IR;
using GameVM.Compiler.Core.IR.Transformers;
using GameVM.Compiler.Core.IR.Slab;
using GameVM.Compiler.Core.IR.Buffers;
using GameVM.Compiler.Pascal;
using GameVM.Compiler.Core.IR.Soa;

namespace GameVM.Compiler.Core.Tests.IR.Transformers
{
    public class HlirSlabToMlirSlabTransformerTests
    {
        private readonly HlirSlabToMlirSlabTransformer _transformer;
        private readonly PascalFrontend _frontend = new PascalFrontend();

        public HlirSlabToMlirSlabTransformerTests()
        {
            _transformer = new HlirSlabToMlirSlabTransformer();
        }

        [Test]
        public void Transform_SimpleVariableAssignment_ProducesValidMlirSlab()
        {
            var sourceCode = "program Test;\nvar x: Integer;\nbegin\n  x := 42;\nend.";
            var astSlab = _frontend.ParseToSlab(sourceCode);
            var hlirSlab = _frontend.ConvertToHlirSlab(astSlab);
            Assert.That(hlirSlab.Count, Is.GreaterThan(0));

            InstList result = _transformer.Transform(hlirSlab);

            Assert.That(result.Count, Is.GreaterThan(0), "Result should not be empty");
            
            // Check that we have MLIR instructions (kind >= 128)
            bool foundMlir = false;
            for (int i = 0; i < result.Count; i++)
            {
                if (result.GetKind(i) >= 128)
                {
                    foundMlir = true;
                    break;
                }
            }
            Assert.That(foundMlir, Is.True, "Should find MLIR instructions");
        }

        [Test]
        public void Transform_WithIfStatement_ProducesValidMlirSlab()
        {
            var sourceCode = "program Test;\nvar x: Integer;\nbegin\n  if x > 0 then x := 1;\nend.";
            var astSlab = _frontend.ParseToSlab(sourceCode);
            var hlirSlab = _frontend.ConvertToHlirSlab(astSlab);
            Assert.That(hlirSlab.Count, Is.GreaterThan(0));

            InstList result = _transformer.Transform(hlirSlab);

            Assert.That(result.Count, Is.GreaterThan(0));
        }

        [Test]
        public void Transform_WithWhileLoop_ProducesValidMlirSlab()
        {
            var sourceCode = "program Test;\nvar i: Integer;\nbegin\n  i := 0;\n  while i < 10 do\n    i := i + 1;\nend.";
            var astSlab = _frontend.ParseToSlab(sourceCode);
            var hlirSlab = _frontend.ConvertToHlirSlab(astSlab);
            Assert.That(hlirSlab.Count, Is.GreaterThan(0));

            InstList result = _transformer.Transform(hlirSlab);

            Assert.That(result.Count, Is.GreaterThan(0));
        }

        [Test]
        public void Transform_WithForLoop_ProducesValidMlirSlab()
        {
            var sourceCode = "program Test;\nvar i, sum: Integer;\nbegin\n  sum := 0;\n  for i := 1 to 10 do\n    sum := sum + i;\nend.";
            var astSlab = _frontend.ParseToSlab(sourceCode);
            var hlirSlab = _frontend.ConvertToHlirSlab(astSlab);
            Assert.That(hlirSlab.Count, Is.GreaterThan(0));

            InstList result = _transformer.Transform(hlirSlab);

            Assert.That(result.Count, Is.GreaterThan(0));
        }

        [Test]
        public void Transform_MultipleVariables_ProducesValidMlirSlab()
        {
            var sourceCode = "program Test;\nvar a, b, c: Integer;\nbegin\n  a := 1;\n  b := 2;\n  c := a + b;\nend.";
            var astSlab = _frontend.ParseToSlab(sourceCode);
            var hlirSlab = _frontend.ConvertToHlirSlab(astSlab);
            Assert.That(hlirSlab.Count, Is.GreaterThan(0));

            InstList result = _transformer.Transform(hlirSlab);

            Assert.That(result.Count, Is.GreaterThan(0));
        }

        [Test]
        public void Transform_EmptySlab_ProducesEmptySlab()
        {
            var emptySlab = new InstList(
                    Array.Empty<byte>(),
                    Array.Empty<ushort>(),
                    Array.Empty<ushort>(),
                    Array.Empty<uint>(),
                    Array.Empty<uint>(),
                    Array.Empty<uint>(),
                    Array.Empty<int>(),
                    0,
                    0);
            var result = _transformer.Transform(emptySlab);
            Assert.That(result.Count, Is.EqualTo(0));
        }

        [Test]
        public void Transform_WithLabel_ProducesValidMlirSlab()
        {
            var sourceCode = "program Test;\nlabel 1;\nvar x: Integer;\nbegin\n  if x > 0 then\n    begin\n      x := 1;\n      goto 1;\n    end;\n1:\n  x := 2;\nend.";
            var astSlab = _frontend.ParseToSlab(sourceCode);
            var hlirSlab = _frontend.ConvertToHlirSlab(astSlab);
            Assert.That(hlirSlab.Count, Is.GreaterThan(0));

            InstList result = _transformer.Transform(hlirSlab);

            Assert.That(result.Count, Is.GreaterThan(0));
        }

        [Test]
        public void Transform_Call_SufficientOperands_ProducesCallInstruction()
        {
            // Build an HLIR slab with a call instruction with sufficient operands
            // The HLIR_CALL format: [metadata, functionId, argCount, argIdentifiers...]
            // We need to set up the HLIR slab to have a call instruction.
            // We want the HLIR slab to have an argument count of 3 (for the call instruction: functionId + 2 args).
            var builder = new InstListBuilder();
            builder.Add((byte)MlirInstructionKind.Call, InstructionFlag.None, 3, 123u, 456u, 789u);
            var hlirSlab = builder.Build();

            var result = _transformer.Transform(hlirSlab);

            // We expect one instruction in the result
            Assert.That(result.Count, Is.EqualTo(1));
            // We expect the instruction to be a call
            Assert.That(result.GetKind(0), Is.EqualTo((byte)MlirInstructionKind.Call));
            // We expect the argCount of the result to be 1
            Assert.That(result.GetArgCount(0), Is.EqualTo(1));
            // We expect the operands to be [123]
            ReadOnlySpan<uint> operands = result.GetOperands(0);
            Assert.That(operands.Length, Is.EqualTo(1));
            Assert.That(operands[0], Is.EqualTo(123u));
        }

        [Test]
        public void Transform_Branch_SufficientOperands_ProducesBranchInstruction()
        {
            // Build an HLIR slab with a branch instruction with sufficient operands
            var builder = new InstListBuilder();
            // The HLIR_BRANCH format: [metadata, conditionOperandId, targetBlockId]
            // We'll set:
            //   conditionOperandId = 123
            //   targetBlockId = 456
            builder.Add((byte)MlirInstructionKind.Branch, InstructionFlag.None, 2, 123u, 456u);
            var hlirSlab = builder.Build();

            var result = _transformer.Transform(hlirSlab);

            // We expect one instruction in the result
            Assert.That(result.Count, Is.EqualTo(1));
            // We expect the instruction to be a branch
            Assert.That(result.GetKind(0), Is.EqualTo((byte)MlirInstructionKind.Branch));
            // We expect the operands to be [123, 456]
            ReadOnlySpan<uint> operands = result.GetOperands(0);
            Assert.That(operands.Length, Is.EqualTo(2));
            Assert.That(operands[0], Is.EqualTo(123u));
            Assert.That(operands[1], Is.EqualTo(456u));
        }

        [Test]
        public void Transform_VariableAndExpressionStatement_DoNothing()
        {
            // Build an HLIR slab with a Variable and an ExpressionStatement instruction
            var builder = new InstListBuilder();
            // Variable instruction (kind = Variable) with 0 operands
            builder.Add((byte)MlirInstructionKind.Variable, InstructionFlag.None, 0);
            // ExpressionStatement instruction (kind = ExpressionStatement) with 0 operands
            builder.Add((byte)MlirInstructionKind.ExpressionStatement, InstructionFlag.None, 0);
            var hlirSlab = builder.Build();

            var result = _transformer.Transform(hlirSlab);

            // These instructions should produce no output
            Assert.That(result.Count, Is.EqualTo(0));
        }

        [Test]
        public void Transform_UnknownInstruction_CopiedAsIs()
        {
            // Build an HLIR slab with an unknown kind (e.g., 255) and some operands
            var builder = new InstListBuilder();
            // Unknown instruction kind (255) with 2 operands: 123 and 456
            builder.Add((byte)255, InstructionFlag.None, 2, 123u, 456u);
            var hlirSlab = builder.Build();

            var result = _transformer.Transform(hlirSlab);

            // The unknown instruction should be copied as-is
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result.GetKind(0), Is.EqualTo((byte)255));
            ReadOnlySpan<uint> operands = result.GetOperands(0);
            Assert.That(operands.Length, Is.EqualTo(2));
            Assert.That(operands[0], Is.EqualTo(123u));
            Assert.That(operands[1], Is.EqualTo(456u));
        }

        [Test]
        public void Transform_ReturnWithZeroOperands_ProducesReturnWithZeroOperands()
        {
            // Build an HLIR slab with a return instruction with 0 operands
            var builder = new InstListBuilder();
            // Return instruction (kind = Return) with 0 operands
            builder.Add((byte)MlirInstructionKind.Return, InstructionFlag.None, 0);
            var hlirSlab = builder.Build();

            var result = _transformer.Transform(hlirSlab);

            // Should produce a return instruction with 0 operands
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result.GetKind(0), Is.EqualTo((byte)MlirInstructionKind.Return));
            ReadOnlySpan<uint> operands = result.GetOperands(0);
            Assert.That(operands.Length, Is.EqualTo(0));
        }

        [Test]
        public void Transform_ReturnWithOneOperand_ProducesAssignToReturnHandle()
        {
            // Build an HLIR slab with a return instruction with 1 operand (value slot id = 42)
            var builder = new InstListBuilder();
            // Return instruction (kind = Return) with 1 operand: 42
            builder.Add((byte)MlirInstructionKind.Return, InstructionFlag.None, 1, 42u);
            var hlirSlab = builder.Build();

            var result = _transformer.Transform(hlirSlab);

            // Should produce an assign instruction (to the return handle) with 2 operands
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result.GetKind(0), Is.EqualTo((byte)MlirInstructionKind.Assign));
            ReadOnlySpan<uint> operands = result.GetOperands(0);
            Assert.That(operands.Length, Is.EqualTo(2));
            // The first operand should be the return handle hash of "_return"
            var returnHandle = (uint)"_return".GetHashCode();
            Assert.That(operands[0], Is.EqualTo(returnHandle));
            // The second operand should be the value slot id (42)
            Assert.That(operands[1], Is.EqualTo(42u));
        }

        [Test]
        public void Transform_Label_InsufficientOperands_DoesNothing()
        {
            // Build an HLIR slab with a label instruction with 0 operands (requires at least 1)
            var builder = new InstListBuilder();
            builder.Add((byte)MlirInstructionKind.Label, InstructionFlag.None, 0);
            var hlirSlab = builder.Build();

            var result = _transformer.Transform(hlirSlab);

            // Should produce no output
            Assert.That(result.Count, Is.EqualTo(0));
        }

        [Test]
        public void Transform_Assign_InsufficientOperands_DoesNothing()
        {
            // Build an HLIR slab with an assign instruction with 0 operands (requires at least 2)
            var builder = new InstListBuilder();
            builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0);
            var hlirSlab = builder.Build();

            var result = _transformer.Transform(hlirSlab);

            // Should produce no output
            Assert.That(result.Count, Is.EqualTo(0));
        }

        [Test]
        public void Transform_Branch_InsufficientOperands_DoesNothing()
        {
            // Build an HLIR slab with a branch instruction with 0 operands (requires at least 2)
            var builder = new InstListBuilder();
            builder.Add((byte)MlirInstructionKind.Branch, InstructionFlag.None, 0);
            var hlirSlab = builder.Build();

            var result = _transformer.Transform(hlirSlab);

            // Should produce no output
            Assert.That(result.Count, Is.EqualTo(0));
        }

        [Test]
        public void Transform_Call_InsufficientOperands_DoesNothing()
        {
            // Build an HLIR slab with a call instruction with 0 operands (requires at least 1)
            var builder = new InstListBuilder();
            builder.Add((byte)MlirInstructionKind.Call, InstructionFlag.None, 0);
            var hlirSlab = builder.Build();

            var result = _transformer.Transform(hlirSlab);

            // Should produce no output
            Assert.That(result.Count, Is.EqualTo(0));
        }

        [Test]
        public void Transform_Assign_SufficientOperands_ProducesAssignInstruction()
        {
            // Build an HLIR slab with an assign instruction with sufficient operands
            var builder = new InstListBuilder();
            // HLIR_ASSIGN: [metadata, targetSlotId, valueSlotId]
            builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 2, 100u, 200u);
            var hlirSlab = builder.Build();

            var result = _transformer.Transform(hlirSlab);

            // We expect one instruction in the result
            Assert.That(result.Count, Is.EqualTo(1));
            // We expect the instruction to be an assign
            Assert.That(result.GetKind(0), Is.EqualTo((byte)MlirInstructionKind.Assign));
            // We expect the operands to be [100, 200]
            ReadOnlySpan<uint> operands = result.GetOperands(0);
            Assert.That(operands.Length, Is.EqualTo(2));
            Assert.That(operands[0], Is.EqualTo(100u));
            Assert.That(operands[1], Is.EqualTo(200u));
        }

        [Test]
        public void Transform_Label_SufficientOperands_ProducesLabelInstruction()
        {
            // Build an HLIR slab with a label instruction with sufficient operands
            var builder = new InstListBuilder();
            // HLIR_LABEL: [metadata, functionNameHash]
            builder.Add((byte)MlirInstructionKind.Label, InstructionFlag.None, 1, 12345u);
            var hlirSlab = builder.Build();

            var result = _transformer.Transform(hlirSlab);

            // We expect one instruction in the result
            Assert.That(result.Count, Is.EqualTo(1));
            // We expect the instruction to be a label
            Assert.That(result.GetKind(0), Is.EqualTo((byte)MlirInstructionKind.Label));
            // We expect the operands to be [12345]
            ReadOnlySpan<uint> operands = result.GetOperands(0);
            Assert.That(operands.Length, Is.EqualTo(1));
            Assert.That(operands[0], Is.EqualTo(12345u));
        }
    }
}