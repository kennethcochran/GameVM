using System;
using GameVM.Compiler.Backend.Atari2600;
using GameVM.Compiler.Core.IR;
using GameVM.Compiler.Core.IR.Soa;
using GameVM.Compiler.Core.IR.Buffers;
using GameVM.Compiler.Core.Enums;
using NUnit.Framework;

namespace GameVM.Compiler.Backend.Atari2600.Tests;

[TestFixture]
public class MidToLowLevelTransformerTests
{
    private MidToLowLevelTransformer _transformer = null!;
    private StringPool _stringPool = null!;

    [SetUp]
    public void Setup()
    {
        _transformer = new MidToLowLevelTransformer();
        _stringPool = new StringPool();
    }

    private static InstList BuildMlirSlab(params (byte kind, uint[] operands)[] instructions)
    {
        var builder = new InstListBuilder();
        foreach (var (kind, operands) in instructions)
        {
            builder.Add(kind, InstructionFlag.None, 0, operands);
        }
        return builder.Build();
    }

    private static InstList BuildMlirAssign(uint targetPoolOffset, uint valuePoolOffset)
    {
        var builder = new InstListBuilder();
        builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, targetPoolOffset, valuePoolOffset);
        return builder.Build();
    }

    private static InstList BuildMlirLabel(uint functionNameHash)
    {
        var builder = new InstListBuilder();
        builder.Add((byte)MlirInstructionKind.Label, InstructionFlag.None, 0, functionNameHash);
        return builder.Build();
    }

    private static InstList BuildMlirCall(uint functionId, uint argCount, uint[] argIds)
    {
        var builder = new InstListBuilder();
        var operands = new uint[1 + 1 + argCount];
        operands[0] = functionId;
        operands[1] = argCount;
        Array.Copy(argIds, 0, operands, 2, argIds.Length);
        builder.Add((byte)MlirInstructionKind.Call, InstructionFlag.None, 0, operands);
        return builder.Build();
    }

    private static InstList BuildMlirReturn(uint valuePoolOffset = 0)
    {
        var builder = new InstListBuilder();
        if (valuePoolOffset == 0)
        {
            builder.Add((byte)MlirInstructionKind.Return, InstructionFlag.None, 0);
        }
        else
        {
            builder.Add((byte)MlirInstructionKind.Return, InstructionFlag.None, 0, valuePoolOffset);
        }
        return builder.Build();
    }

    #region Assignment Transformation Tests

    [Test]
    public void Transform_SimpleAssignment_GeneratesLoadAndStore()
    {
        // Arrange: x := 42
        uint targetOffset = _stringPool.Intern("MyVar");
        uint valueOffset = _stringPool.Intern("42");
        var mlir = BuildMlirAssign(targetOffset, valueOffset);

        // Act
        var result = _transformer.TransformSlab(mlir, _stringPool);

        // Assert: Should emit LDA #42, STA $80
        Assert.That(result.Count, Is.EqualTo(2));
        
        // First instruction: Load (LDA #42)
        Assert.That(result.GetKind(0), Is.EqualTo((byte)LlirInstructionKind.Load));
        var loadOps = result.GetOperands(0);
        Assert.That(loadOps.Length, Is.GreaterThanOrEqualTo(2));
        Assert.That(loadOps[1], Is.EqualTo(42u), "Second operand should be immediate value 42");
        
        // Second instruction: Store (STA $80)
        Assert.That(result.GetKind(1), Is.EqualTo((byte)LlirInstructionKind.Store));
        var storeOps = result.GetOperands(1);
        Assert.That(storeOps.Length, Is.GreaterThanOrEqualTo(2));
        Assert.That(storeOps[1], Is.EqualTo(0x80u), "Second operand should be zero-page address $80");
    }

    [TestCase("COLUBK", 10, 0x09)]
    [TestCase("COLUPF", 255, 0x08)]
    [TestCase("COLUP0", 128, 0x06)]
    [TestCase("COLUP1", 64, 0x07)]
    public void Transform_TIARegisterAssignment_MapsToCorrectAddress(string register, int value, int expectedAddress)
    {
        // Arrange
        uint targetOffset = _stringPool.Intern(register);
        uint valueOffset = _stringPool.Intern(value.ToString());
        var mlir = BuildMlirAssign(targetOffset, valueOffset);

        // Act
        var result = _transformer.TransformSlab(mlir, _stringPool);

        // Assert
        Assert.That(result.Count, Is.EqualTo(2));
        
        var storeOps = result.GetOperands(1);
        Assert.That(storeOps.Length, Is.GreaterThanOrEqualTo(2));
        Assert.That(storeOps[1], Is.EqualTo((uint)expectedAddress), $"{register} should map to TIA register address ${expectedAddress:X2}");
    }

    [Test]
    public void Transform_UnknownVariable_MapsToDefaultAddress()
    {
        // Arrange
        uint targetOffset = _stringPool.Intern("UnknownVar");
        uint valueOffset = _stringPool.Intern("99");
        var mlir = BuildMlirAssign(targetOffset, valueOffset);

        // Act
        var result = _transformer.TransformSlab(mlir, _stringPool);

        // Assert: Unknown variables should map to $80 (first zero-page allocation)
        Assert.That(result.Count, Is.EqualTo(2));
        
        var storeOps = result.GetOperands(1);
        Assert.That(storeOps.Length, Is.GreaterThanOrEqualTo(2));
        Assert.That(storeOps[1], Is.EqualTo(0x80u), "Unknown variables should map to default zero-page address $80");
    }

    [Test]
    public void Transform_HexValue_ParsesCorrectly()
    {
        // Arrange: x := 0xFF
        uint targetOffset = _stringPool.Intern("MyVar");
        uint valueOffset = _stringPool.Intern("0xFF");
        var mlir = BuildMlirAssign(targetOffset, valueOffset);

        // Act
        var result = _transformer.TransformSlab(mlir, _stringPool);

        // Assert: 0xFF should be parsed as 255
        Assert.That(result.Count, Is.EqualTo(2));
        
        var loadOps = result.GetOperands(0);
        Assert.That(loadOps.Length, Is.GreaterThanOrEqualTo(2));
        Assert.That(loadOps[1], Is.EqualTo(0xFFu), "Hex value 0xFF should be parsed as 255");
    }

    [Test]
    public void Transform_VariableAssignment_LoadsFromSourceVariable()
    {
        // Arrange: y := x (copy from variable x)
        uint targetOffset = _stringPool.Intern("y");
        uint valueOffset = _stringPool.Intern("x");
        var mlir = BuildMlirAssign(targetOffset, valueOffset);

        // Act
        var result = _transformer.TransformSlab(mlir, _stringPool);

        // Assert: Should generate LDA from x's address, STA to y's address
        Assert.That(result.Count, Is.EqualTo(2));
        
        var loadOps = result.GetOperands(0);
        Assert.That(loadOps.Length, Is.GreaterThanOrEqualTo(2));
        Assert.That(loadOps[1], Is.EqualTo(0x81u), "Load should use x's address ($81, allocated after y)");
        
        var storeOps = result.GetOperands(1);
        Assert.That(storeOps.Length, Is.GreaterThanOrEqualTo(2));
        Assert.That(storeOps[1], Is.EqualTo(0x80u), "Store should use y's address ($80, first allocation)");
    }

    [Test]
    public void Transform_MultipleAssignments_SequentialAllocation()
    {
        // Arrange: var1 := 1; var2 := 2; var3 := 3
        var builder = new InstListBuilder();
        builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, 
            _stringPool.Intern("var1"), _stringPool.Intern("1"));
        builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, 
            _stringPool.Intern("var2"), _stringPool.Intern("2"));
        builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, 
            _stringPool.Intern("var3"), _stringPool.Intern("3"));
        var mlir = builder.Build();

        // Act
        var result = _transformer.TransformSlab(mlir, _stringPool);

        // Assert: 3 assignments * 2 instructions each = 6 instructions
        Assert.That(result.Count, Is.EqualTo(6));
        
        // Check store addresses are allocated sequentially: $80, $81, $82
        for (int i = 0; i < 3; i++)
        {
            int storeIdx = i * 2 + 1;
            var storeOps = result.GetOperands(storeIdx);
            Assert.That(storeOps.Length, Is.GreaterThanOrEqualTo(2));
            Assert.That(storeOps[1], Is.EqualTo((uint)(0x80 + i)), 
                $"Variable var{i + 1} should map to address ${0x80 + i:X2}");
        }
    }

    #endregion

    #region Function Label Tests

    [Test]
    public void Transform_LabelInstruction_GeneratesCorrectLabel()
    {
        // Arrange: function label "main"
        uint functionNameHash = (uint)"main".GetHashCode();
        var mlir = BuildMlirLabel(functionNameHash);

        // Act
        var result = _transformer.TransformSlab(mlir, _stringPool);

        // Assert
        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result.GetKind(0), Is.EqualTo((byte)LlirInstructionKind.Label));
        
        var labelOps = result.GetOperands(0);
        Assert.That(labelOps.Length, Is.EqualTo(1));
        Assert.That(labelOps[0], Is.EqualTo(functionNameHash), "Label should preserve function name hash");
    }

    [Test]
    public void Transform_LabelFollowedByBody_ProcessesFunctionBody()
    {
        // Arrange: label "main" followed by assignment
        var builder = new InstListBuilder();
        uint funcHash = (uint)"main".GetHashCode();
        builder.Add((byte)MlirInstructionKind.Label, InstructionFlag.None, 0, funcHash);
        builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, 
            _stringPool.Intern("x"), _stringPool.Intern("42"));
        var mlir = builder.Build();

        // Act
        var result = _transformer.TransformSlab(mlir, _stringPool);

        // Assert: Label + 2 instructions for assignment
        Assert.That(result.Count, Is.EqualTo(3));
        Assert.That(result.GetKind(0), Is.EqualTo((byte)LlirInstructionKind.Label));
        Assert.That(result.GetKind(1), Is.EqualTo((byte)LlirInstructionKind.Load));
        Assert.That(result.GetKind(2), Is.EqualTo((byte)LlirInstructionKind.Store));
    }

    #endregion

    #region Function Call Tests

    [Test]
    public void Transform_FunctionCall_GeneratesCallInstruction()
    {
        // Arrange
        uint functionId = (uint)"InitGame".GetHashCode();
        var mlir = BuildMlirCall(functionId, 0, Array.Empty<uint>());

        // Act
        var result = _transformer.TransformSlab(mlir, _stringPool);

        // Assert
        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result.GetKind(0), Is.EqualTo((byte)LlirInstructionKind.Call));
        
        var callOps = result.GetOperands(0);
        Assert.That(callOps.Length, Is.GreaterThanOrEqualTo(1));
    }

    [Test]
    public void Transform_FunctionCallWithArguments_PreservesCallStructure()
    {
        // Arrange: call Add(5, 3)
        uint functionId = (uint)"Add".GetHashCode();
        uint arg1Id = (uint)"5".GetHashCode(); // simplified
        uint arg2Id = (uint)"3".GetHashCode();
        var mlir = BuildMlirCall(functionId, 2, new[] { arg1Id, arg2Id });

        // Act
        var result = _transformer.TransformSlab(mlir, _stringPool);

        // Assert
        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result.GetKind(0), Is.EqualTo((byte)LlirInstructionKind.Call));
    }

    #endregion

    #region Return Tests

    [Test]
    public void Transform_ReturnInstruction_GeneratesReturn()
    {
        // Arrange
        var mlir = BuildMlirReturn();

        // Act
        var result = _transformer.TransformSlab(mlir, _stringPool);

        // Assert
        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result.GetKind(0), Is.EqualTo((byte)LlirInstructionKind.Return));
    }

    [Test]
    public void Transform_ReturnWithValue_CopiesOperandsAsReturn()
    {
        // Arrange: return x
        uint valueOffset = _stringPool.Intern("x");
        var mlir = BuildMlirReturn(valueOffset);

        // Act
        var result = _transformer.TransformSlab(mlir, _stringPool);

        // Assert: Return is mapped directly, preserving the value operand
        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result.GetKind(0), Is.EqualTo((byte)LlirInstructionKind.Return));
        var returnOps = result.GetOperands(0);
        Assert.That(returnOps.Length, Is.EqualTo(1), "Return with value should preserve the value operand");
        Assert.That(returnOps[0], Is.EqualTo(valueOffset));
    }

    #endregion

    #region Empty Input Tests

    [Test]
    public void Transform_EmptySlab_ReturnsEmptyResult()
    {
        // Arrange
        var mlir = BuildMlirSlab();

        // Act
        var result = _transformer.TransformSlab(mlir, _stringPool);

        // Assert
        Assert.That(result.Count, Is.EqualTo(0));
    }

    #endregion

    #region Complex Transformation Tests

    [Test]
    public void Transform_MixedInstructions_GeneratesCorrectSequence()
    {
        // Arrange: label "main", x := 10, call func, y := 20
        var builder = new InstListBuilder();
        uint funcHash = (uint)"main".GetHashCode();
        
        builder.Add((byte)MlirInstructionKind.Label, InstructionFlag.None, 0, funcHash);
        builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, 
            _stringPool.Intern("x"), _stringPool.Intern("10"));
        builder.Add((byte)MlirInstructionKind.Call, InstructionFlag.None, 0, 
            (uint)"myFunction".GetHashCode(), 0);
        builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, 
            _stringPool.Intern("y"), _stringPool.Intern("20"));
        var mlir = builder.Build();

        // Act
        var result = _transformer.TransformSlab(mlir, _stringPool);

        // Assert: label + (load+store) + call + (load+store) = 6 instructions
        Assert.That(result.Count, Is.EqualTo(6));
        
        Assert.That(result.GetKind(0), Is.EqualTo((byte)LlirInstructionKind.Label));
        Assert.That(result.GetKind(1), Is.EqualTo((byte)LlirInstructionKind.Load));
        Assert.That(result.GetKind(2), Is.EqualTo((byte)LlirInstructionKind.Store));
        Assert.That(result.GetKind(3), Is.EqualTo((byte)LlirInstructionKind.Call));
        Assert.That(result.GetKind(4), Is.EqualTo((byte)LlirInstructionKind.Load));
        Assert.That(result.GetKind(5), Is.EqualTo((byte)LlirInstructionKind.Store));
    }

    [Test]
    public void Transform_MultipleFunctions_TransformsAllFunctions()
    {
        // Arrange: func1 { x := 1 }, func2 { y := 2 }
        var builder = new InstListBuilder();
        
        // Function 1
        builder.Add((byte)MlirInstructionKind.Label, InstructionFlag.None, 0, (uint)"func1".GetHashCode());
        builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, 
            _stringPool.Intern("x"), _stringPool.Intern("1"));
        
        // Function 2
        builder.Add((byte)MlirInstructionKind.Label, InstructionFlag.None, 0, (uint)"func2".GetHashCode());
        builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, 
            _stringPool.Intern("y"), _stringPool.Intern("2"));
        
        var mlir = builder.Build();

        // Act
        var result = _transformer.TransformSlab(mlir, _stringPool);

        // Assert: 2 labels + 2*2 instructions = 6
        Assert.That(result.Count, Is.EqualTo(6));
        
        // Count labels
        int labelCount = 0;
        for (int i = 0; i < result.Count; i++)
        {
            if (result.GetKind(i) == (byte)LlirInstructionKind.Label)
                labelCount++;
        }
        Assert.That(labelCount, Is.EqualTo(2));
    }

    #endregion

    #region TIA Register Mapping Tests

    [Test]
    public void Transform_AllTIARegisters_MapCorrectly()
    {
        // Verify all TIA registers from InitializeAddressMap map correctly
        var tiaRegisters = new (string name, int addr)[]
        {
            ("COLUBK", 0x09), ("COLUPF", 0x08), ("COLUP0", 0x06), ("COLUP1", 0x07),
            ("PF0", 0x0D), ("PF1", 0x0E), ("PF2", 0x0F),
            ("RESP0", 0x01), ("RESP1", 0x02), ("RESM0", 0x03), ("RESM1", 0x04), ("RESBL", 0x05),
            ("AUDC0", 0x02), ("AUDC1", 0x06), ("AUDF0", 0x04), ("AUDF1", 0x08),
            ("AUDV0", 0x03), ("AUDV1", 0x07),
            ("WSYNC", 0x02), ("RSYNC", 0x04),
            ("NUSIZ0", 0x0B), ("NUSIZ1", 0x0C),
            ("RESF0", 0x07), ("RESF1", 0x08),
            ("HMP0", 0x00), ("HMP1", 0x01), ("HMM0", 0x02), ("HMM1", 0x03),
            ("HMPG", 0x04), ("HMBL", 0x05),
            ("VDELP0", 0x0B), ("VDELP1", 0x0C), ("VDELBL", 0x0D),
            ("RESET", 0xFF)
        };

        foreach (var (name, addr) in tiaRegisters)
        {
            uint targetOffset = _stringPool.Intern(name);
            uint valueOffset = _stringPool.Intern("42");
            var mlir = BuildMlirAssign(targetOffset, valueOffset);
            var result = _transformer.TransformSlab(mlir, _stringPool);
            
            Assert.That(result.Count, Is.EqualTo(2), $"Failed for {name}");
            var storeOps = result.GetOperands(1);
            Assert.That(storeOps[1], Is.EqualTo((uint)addr), $"{name} should map to ${addr:X2}");
        }
    }

    #endregion
}