using System;
using System.Collections.Generic;
using GameVM.Compiler.Backend.Atari2600;
using GameVM.Compiler.Core.IR;
using GameVM.Compiler.Core.IR.Soa;
using GameVM.Compiler.Core.IR.Buffers;
using GameVM.Compiler.Core.Enums;
using GameVM.Compiler.Core.Exceptions;
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

    // Builds an MLIR Call in the shape the production pipeline emits
    // (HlirTreeToMlirTransformer.ProcessCallStatement): the callee's pool
    // offset first, then one pool offset per argument — no arg count.
    private static InstList BuildMlirCall(uint functionId, uint[] argIds)
    {
        var builder = new InstListBuilder();
        var operands = new uint[1 + argIds.Length];
        operands[0] = functionId;
        Array.Copy(argIds, 0, operands, 1, argIds.Length);
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
    public void Transform_SimpleAssignment_EmitsFoldedAssign()
    {
        // Arrange: x := 42
        uint targetOffset = _stringPool.Intern("MyVar");
        uint valueOffset = _stringPool.Intern("42");
        var mlir = BuildMlirAssign(targetOffset, valueOffset);

        // Act
        var result = _transformer.TransformSlab(mlir, _stringPool);

        // Assert: single Assign = LDA #42 + STA $80 folded into one instruction
        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result.GetKind(0), Is.EqualTo((byte)LlirInstructionKind.Assign));

        var ops = result.GetOperands(0);
        Assert.That(ops.Length, Is.EqualTo(2));
        Assert.That(ops[0], Is.EqualTo(0x80u), "Target maps to zero-page address $80 (first allocation)");
        Assert.That(ops[1], Is.EqualTo(42u), "Value is the immediate 42");
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
        Assert.That(result.Count, Is.EqualTo(1));
        var ops = result.GetOperands(0);
        Assert.That(ops.Length, Is.EqualTo(2));
        Assert.That(ops[0], Is.EqualTo((uint)expectedAddress), $"{register} should map to TIA register address ${expectedAddress:X2}");
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
        Assert.That(result.Count, Is.EqualTo(1));
        var ops = result.GetOperands(0);
        Assert.That(ops.Length, Is.EqualTo(2));
        Assert.That(ops[0], Is.EqualTo(0x80u), "Unknown variables should map to default zero-page address $80");
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
        Assert.That(result.Count, Is.EqualTo(1));
        var ops = result.GetOperands(0);
        Assert.That(ops.Length, Is.EqualTo(2));
        Assert.That(ops[1], Is.EqualTo(0xFFu), "Hex value 0xFF should be parsed as 255");
    }

    [Test]
    public void Transform_VariableAssignment_LoadsFromSourceVariable()
    {
        // Arrange: y := x (copy from variable x)
        // The LLIR must read x from memory (Load zp) then Store to y; a folded
        // Assign would copy x's *address* ($81), not its value.
        uint targetOffset = _stringPool.Intern("y");
        uint valueOffset = _stringPool.Intern("x");
        var mlir = BuildMlirAssign(targetOffset, valueOffset);

        // Act
        var result = _transformer.TransformSlab(mlir, _stringPool);

        // Assert: Load(xAddr); Store(yAddr)
        Assert.That(result.Count, Is.EqualTo(2));
        Assert.That(result.GetKind(0), Is.EqualTo((byte)LlirInstructionKind.Load));
        var loadOps = result.GetOperands(0);
        Assert.That(loadOps.Length, Is.EqualTo(2), "Load should carry [addrLow, addrHigh]");
        Assert.That(loadOps[0], Is.EqualTo(0x81u), "Source x maps to $81 (allocated second)");
        Assert.That(loadOps[1], Is.EqualTo(0x00u), "zp high byte = 0");
        Assert.That(result.GetKind(1), Is.EqualTo((byte)LlirInstructionKind.Store));
        var storeOps = result.GetOperands(1);
        Assert.That(storeOps.Length, Is.EqualTo(1), "Store should carry the zp target");
        Assert.That(storeOps[0], Is.EqualTo(0x80u), "Target y maps to $80 (first allocation)");
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

        // Assert: 3 assignments = 3 folded Assign instructions
        Assert.That(result.Count, Is.EqualTo(3));

        // Check target addresses are allocated sequentially: $80, $81, $82
        for (int i = 0; i < 3; i++)
        {
            var ops = result.GetOperands(i);
            Assert.That(ops.Length, Is.EqualTo(2));
            Assert.That(ops[0], Is.EqualTo((uint)(0x80 + i)),
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

        // Assert: Label + 1 folded Assign
        Assert.That(result.Count, Is.EqualTo(2));
        Assert.That(result.GetKind(0), Is.EqualTo((byte)LlirInstructionKind.Label));
        Assert.That(result.GetKind(1), Is.EqualTo((byte)LlirInstructionKind.Assign));
    }

    #endregion

    #region Function Call Tests

    [Test]
    public void Transform_FunctionCall_GeneratesCallInstruction()
    {
        // Arrange
        uint functionId = (uint)"InitGame".GetHashCode();
        var mlir = BuildMlirCall(functionId, Array.Empty<uint>());

        // Act
        var result = _transformer.TransformSlab(mlir, _stringPool);

        // Assert
        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result.GetKind(0), Is.EqualTo((byte)LlirInstructionKind.Call));
        
        var callOps = result.GetOperands(0);
        Assert.That(callOps.Length, Is.GreaterThanOrEqualTo(1));
    }

    [Test]
    public void Transform_FunctionCallWithArguments_MaterializesArgumentsBeforeCall()
    {
        // Arrange: call Add(5, 3). Each argument value must be loaded
        // into the accumulator before the JSR — a bare Call carries only
        // the callee label, so an unloaded argument would vanish from
        // the ROM silently.
        uint functionId = (uint)"Add".GetHashCode();
        uint arg1 = _stringPool.Intern("5");
        uint arg2 = _stringPool.Intern("3");
        var mlir = BuildMlirCall(functionId, new[] { arg1, arg2 });

        // Act
        var result = _transformer.TransformSlab(mlir, _stringPool);

        // Assert: Load 5, Load 3, then the Call.
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Count, Is.EqualTo(3));
            Assert.That(result.GetKind(0), Is.EqualTo((byte)LlirInstructionKind.Load));
            Assert.That(result.GetArgCount(0), Is.EqualTo(1));
            Assert.That(result.GetOperand(0, 0), Is.EqualTo(5));
            Assert.That(result.GetKind(1), Is.EqualTo((byte)LlirInstructionKind.Load));
            Assert.That(result.GetArgCount(1), Is.EqualTo(1));
            Assert.That(result.GetOperand(1, 0), Is.EqualTo(3));
            Assert.That(result.GetKind(2), Is.EqualTo((byte)LlirInstructionKind.Call));
        }
    }

    [Test]
    public void Transform_FunctionCallWithComputedArgument_StoresTempBeforeArgumentLoad()
    {
        // MLIR for: F(x + 1), as the HLIR lowering emits it: the computed
        // argument lives in the accumulator after the Add, is assigned to
        // __tmp_0, and the Call reads it. The Assign to __tmp_0 must store
        // it to memory before the Call loads it — otherwise the load reads
        // an unwritten address and the callee receives garbage instead of
        // x + 1. x + 1 can reach 256, so the argument is a two-byte value
        // (fix round 15): the wide Add stores both bytes into __tmp_0's
        // two slots, and the Call loads the low byte.
        uint functionId = (uint)"F".GetHashCode();
        uint xOffset = _stringPool.Intern("x");
        uint oneOffset = _stringPool.Intern("1");
        uint tmpOffset = _stringPool.Intern("__tmp_0");

        var mlir = BuildMlirSlab(
            ((byte)LlirInstructionKind.Add, new uint[] { xOffset, oneOffset }),
            ((byte)MlirInstructionKind.Assign, new uint[] { tmpOffset, 0 }),
            ((byte)MlirInstructionKind.Call, new uint[] { functionId, tmpOffset }));
        var result = _transformer.TransformSlab(mlir, _stringPool);

        // Load x, Add 1, Store __tmp_0 low, Load 0, Add 0 with carry,
        // Store __tmp_0 high, Load __tmp_0, Call F.
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Count, Is.EqualTo(8));
            Assert.That(result.GetKind(0), Is.EqualTo((byte)LlirInstructionKind.Load));
            Assert.That(result.GetKind(1), Is.EqualTo((byte)LlirInstructionKind.Add));
            Assert.That(result.GetKind(2), Is.EqualTo((byte)LlirInstructionKind.Store));
            Assert.That(result.GetKind(3), Is.EqualTo((byte)LlirInstructionKind.Load));
            Assert.That(result.GetKind(4), Is.EqualTo((byte)LlirInstructionKind.Add));
            Assert.That((InstructionFlag)result.GetFlags(4), Is.EqualTo(InstructionFlag.CarryIn),
                "the argument's high byte is computed with the low pair's carry");
            Assert.That(result.GetKind(5), Is.EqualTo((byte)LlirInstructionKind.Store));
            Assert.That(result.GetKind(6), Is.EqualTo((byte)LlirInstructionKind.Load));
            Assert.That(result.GetKind(7), Is.EqualTo((byte)LlirInstructionKind.Call));

            uint tmpAddr = result.GetOperands(2)[0];
            Assert.That(tmpAddr, Is.GreaterThanOrEqualTo(0x80u), "__tmp_0 must have a zero-page address");
            Assert.That(result.GetOperands(5)[0], Is.EqualTo(tmpAddr + 1),
                "the argument's high byte stores in the temp's second slot");
            Assert.That(result.GetOperands(6)[0], Is.EqualTo(tmpAddr),
                "the Call must load the computed argument from the address the materializing store wrote");
        }
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
    public void Transform_CalleeReturnFlag_IsPreserved()
    {
        // The CalleeReturn flag set at the HLIR -> MLIR lowering must
        // survive to the LLIR so the code generator emits RTS (callee)
        // rather than the entry halt.
        var builder = new InstListBuilder();
        builder.Add((byte)MlirInstructionKind.Return, InstructionFlag.CalleeReturn, 0);
        var mlir = builder.Build();

        var result = _transformer.TransformSlab(mlir, _stringPool);

        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result.GetKind(0), Is.EqualTo((byte)LlirInstructionKind.Return));
        Assert.That((InstructionFlag)result.GetFlags(0), Is.EqualTo(InstructionFlag.CalleeReturn));
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
        // Argument-less call in the production shape: callee offset only.
        builder.Add((byte)MlirInstructionKind.Call, InstructionFlag.None, 0,
            (uint)"myFunction".GetHashCode());
        builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, 
            _stringPool.Intern("y"), _stringPool.Intern("20"));
        var mlir = builder.Build();

        // Act
        var result = _transformer.TransformSlab(mlir, _stringPool);


        // Assert: label + assign + call + assign = 4 instructions
        Assert.That(result.Count, Is.EqualTo(4));

        Assert.That(result.GetKind(0), Is.EqualTo((byte)LlirInstructionKind.Label));
        Assert.That(result.GetKind(1), Is.EqualTo((byte)LlirInstructionKind.Assign));
        Assert.That(result.GetKind(2), Is.EqualTo((byte)LlirInstructionKind.Call));
        Assert.That(result.GetKind(3), Is.EqualTo((byte)LlirInstructionKind.Assign));
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


        // Assert: 2 labels + 2 assignments = 4 instructions
        Assert.That(result.Count, Is.EqualTo(4));
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
        // TIA write-register map verified against the Stella Programmer's Guide
        // "TIA WRITE ADDRESS SUMMARY"
        // (https://alienbill.com/2600/101/docs/stella.html).
        var tiaRegisters = new (string name, int addr)[]
        {
            ("VSYNC", 0x00), ("VBLANK", 0x01), ("WSYNC", 0x02), ("RSYNC", 0x03),
            ("NUSIZ0", 0x04), ("NUSIZ1", 0x05),
            ("COLUP0", 0x06), ("COLUP1", 0x07), ("COLUPF", 0x08), ("COLUBK", 0x09),
            ("CTRLPF", 0x0A), ("REFP0", 0x0B), ("REFP1", 0x0C),
            ("PF0", 0x0D), ("PF1", 0x0E), ("PF2", 0x0F),
            ("RESP0", 0x10), ("RESP1", 0x11), ("RESM0", 0x12), ("RESM1", 0x13), ("RESBL", 0x14),
            ("AUDC0", 0x15), ("AUDC1", 0x16), ("AUDF0", 0x17), ("AUDF1", 0x18),
            ("AUDV0", 0x19), ("AUDV1", 0x1A),
            ("GRP0", 0x1B), ("GRP1", 0x1C),
            ("ENAM0", 0x1D), ("ENAM1", 0x1E), ("ENABL", 0x1F),
            ("HMP0", 0x20), ("HMP1", 0x21), ("HMM0", 0x22), ("HMM1", 0x23), ("HMBL", 0x24),
            ("VDELP0", 0x25), ("VDELP1", 0x26), ("VDELBL", 0x27),
            ("RESMP0", 0x28), ("RESMP1", 0x29),
            ("HMOVE", 0x2A), ("HMCLR", 0x2B), ("CXCLR", 0x2C)
        };

        foreach (var (name, addr) in tiaRegisters)
        {
            uint targetOffset = _stringPool.Intern(name);
            uint valueOffset = _stringPool.Intern("42");
            var mlir = BuildMlirAssign(targetOffset, valueOffset);
            var result = _transformer.TransformSlab(mlir, _stringPool);

            Assert.That(result.Count, Is.EqualTo(1), $"Failed for {name}");
            var ops = result.GetOperands(0);
            Assert.That(ops[0], Is.EqualTo((uint)addr), $"{name} should map to ${addr:X2}");
        }
    }

    [Test]
    public void Transform_TiaRegisterMap_HasNoAddressCollisions()
    {
        // Regression guard for B1: several registers used to alias the same
        // address ($02 was WSYNC, AUDC0 and HMM0 at once). Every TIA name must
        // resolve to a distinct hardware address.
        var tiaNames = new[]
        {
            "VSYNC", "VBLANK", "WSYNC", "RSYNC", "NUSIZ0", "NUSIZ1",
            "COLUP0", "COLUP1", "COLUPF", "COLUBK", "CTRLPF", "REFP0", "REFP1",
            "PF0", "PF1", "PF2", "RESP0", "RESP1", "RESM0", "RESM1", "RESBL",
            "AUDC0", "AUDC1", "AUDF0", "AUDF1", "AUDV0", "AUDV1",
            "GRP0", "GRP1", "ENAM0", "ENAM1", "ENABL",
            "HMP0", "HMP1", "HMM0", "HMM1", "HMBL",
            "VDELP0", "VDELP1", "VDELBL", "RESMP0", "RESMP1",
            "HMOVE", "HMCLR", "CXCLR"
        };

        var seen = new Dictionary<uint, string>();
        foreach (string name in tiaNames)
        {
            uint targetOffset = _stringPool.Intern(name);
            uint valueOffset = _stringPool.Intern("42");
            var mlir = BuildMlirAssign(targetOffset, valueOffset);
            var result = _transformer.TransformSlab(mlir, _stringPool);

            uint addr = result.GetOperands(0)[0];
            Assert.That(seen.TryGetValue(addr, out string? other), Is.False,
                $"{name} collides with {other} at ${addr:X2}");
            seen[addr] = name;
        }

        Assert.That(seen.Count, Is.EqualTo(tiaNames.Length));
    }

    [Test]
    public void Transform_RemovedInventedTiaNames_AllocateAsUserVariables()
    {
        // RESF0/RESF1, HMPG and RESET were invented names in the old map; they
        // are not TIA registers and must now allocate as ordinary zero-page
        // user variables ($80+) instead of shadowing hardware addresses.
        foreach (string name in new[] { "RESF0", "RESF1", "HMPG", "RESET" })
        {
            var freshTransformer = new MidToLowLevelTransformer();
            var freshPool = new StringPool();
            uint targetOffset = freshPool.Intern(name);
            uint valueOffset = freshPool.Intern("42");
            var mlir = BuildMlirAssign(targetOffset, valueOffset);
            var result = freshTransformer.TransformSlab(mlir, freshPool);

            uint addr = result.GetOperands(0)[0];
            Assert.That(addr, Is.GreaterThanOrEqualTo(0x80u),
                $"{name} should allocate as a user variable ($80+), not a TIA register");
        }
    }

    #endregion
    #region Arithmetic Sequencing Tests

    [Test]
    public void Transform_SubtractExpression_EmitsLoadSubStoreSequence()
    {
        // MLIR for x taking x - 1, in the shape the HLIR lowering
        // emits: a Sub on x and 1, the temp declaration, and the store
        // of the temp back into x. The Sub can borrow (x = 0 gives
        // -1), so the result cannot provably fit in one byte and x
        // takes two slots (fix round 15). The lowering is two byte
        // pairs: load, subtract and store for the low byte, then the
        // same for the high byte with the borrow carried in.
        var pool = new StringPool();
        uint xOffset = pool.Intern("x");
        uint oneOffset = pool.Intern("1");
        uint tmpOffset = pool.Intern("__tmp_0");


        var builder = new InstListBuilder();
        // Sub(x, 1) - kind 201 (Sub)
        builder.Add((byte)LlirInstructionKind.Sub, InstructionFlag.None, 0, xOffset, oneOffset);
        // Assign(__tmp_0, 0) - temp declaration (should be skipped)
        builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, tmpOffset, 0);
        // Assign(x, __tmp_0) - store accumulator result into x
        builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, xOffset, tmpOffset);
        var mlir = builder.Build();

        var transformer = new MidToLowLevelTransformer();
        var result = transformer.TransformSlab(mlir, pool);

        // Should emit the two byte pairs described above
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Count, Is.EqualTo(6),
                "Sub(x,1); Assign(tmp,0); Assign(x,tmp) should lower to Load; Sub; Store; Load; Sub; Store");

            // Check Load(xAddr, 0) - two operands for zp address
            Assert.That(result.GetKind(0), Is.EqualTo((byte)LlirInstructionKind.Load));
            var loadOps = result.GetOperands(0);
            Assert.That(loadOps.Length, Is.EqualTo(2), "Load should have [addrLow, addrHigh] operands");
            Assert.That(loadOps[0], Is.EqualTo(0x80u), "Load address low byte = xAddr");
            Assert.That(loadOps[1], Is.EqualTo(0x00u), "Load address high byte = 0 (zp)");

            // Check Sub(1) - single operand for immediate
            Assert.That(result.GetKind(1), Is.EqualTo((byte)LlirInstructionKind.Sub));
            Assert.That((InstructionFlag)result.GetFlags(1), Is.EqualTo(InstructionFlag.None),
                "the low sub sets the borrow; it does not consume one");
            var subOps = result.GetOperands(1);
            Assert.That(subOps.Length, Is.EqualTo(1), "Sub should have single immediate operand");
            Assert.That(subOps[0], Is.EqualTo(1u), "Sub immediate = 1");

            // Check Store(xAddr) - single operand for zp target
            Assert.That(result.GetKind(2), Is.EqualTo((byte)LlirInstructionKind.Store));
            var storeOps = result.GetOperands(2);
            Assert.That(storeOps.Length, Is.EqualTo(1), "Store should have single zp target");
            Assert.That(storeOps[0], Is.EqualTo(0x80u), "Store target = xAddr");

            // The high pair loads x's second slot, subtracts the literal's
            // high byte (0) with the borrow, and stores beside the low byte
            Assert.That(result.GetKind(3), Is.EqualTo((byte)LlirInstructionKind.Load));
            Assert.That(result.GetOperands(3)[0], Is.EqualTo(0x81u), "high pair loads x's high byte");
            Assert.That(result.GetKind(4), Is.EqualTo((byte)LlirInstructionKind.Sub));
            Assert.That((InstructionFlag)result.GetFlags(4), Is.EqualTo(InstructionFlag.CarryIn),
                "the high sub consumes the low pair's borrow");
            Assert.That(result.GetOperands(4)[0], Is.Zero, "the literal's high byte is 0");
            Assert.That(result.GetKind(5), Is.EqualTo((byte)LlirInstructionKind.Store));
            Assert.That(result.GetOperands(5)[0], Is.EqualTo(0x81u), "high difference stores to x's second slot");
        }
    }

    [Test]
    public void Transform_AddVariableToVariable_EmitsLoadAndMemoryOperandAdd()
    {
        // MLIR for: z := x + y  ->  Add(x, y); Assign(__tmp_0, 0); Assign(z, __tmp_0)
        // x + y can reach 510, so the result cannot provably fit in one
        // byte and the Add lowers wide (fix round 15). The low pair is
        // Load(xAddr); Add(yAddr); Store(zAddr), where Add carries the
        // [addrLow, addrHigh] memory-operand form — never the address as
        // an immediate (that would add y's address, not its value) — and
        // the high pair adds the two absent high bytes as immediate 0
        // with the low pair's carry. The wide result resolves to z
        // first, so z allocates $80/$81, x takes $82 and y $83.
        uint xOffset = _stringPool.Intern("x");
        uint yOffset = _stringPool.Intern("y");
        uint zOffset = _stringPool.Intern("z");
        uint tmpOffset = _stringPool.Intern("__tmp_0");

        var builder = new InstListBuilder();
        builder.Add((byte)LlirInstructionKind.Add, InstructionFlag.None, 0, xOffset, yOffset);
        builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, tmpOffset, 0);
        builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, zOffset, tmpOffset);
        var mlir = builder.Build();

        var result = _transformer.TransformSlab(mlir, _stringPool);

        // Low pair, then high pair
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Count, Is.EqualTo(6));

            Assert.That(result.GetKind(0), Is.EqualTo((byte)LlirInstructionKind.Load));
            var loadOps = result.GetOperands(0);
            Assert.That(loadOps.Length, Is.EqualTo(2));
            Assert.That(loadOps[0], Is.EqualTo(0x82u), "x allocated at $82");

            Assert.That(result.GetKind(1), Is.EqualTo((byte)LlirInstructionKind.Add));
            Assert.That((InstructionFlag)result.GetFlags(1), Is.EqualTo(InstructionFlag.None),
                "the low add sets the carry; it does not consume one");
            var addOps = result.GetOperands(1);
            Assert.That(addOps.Length, Is.EqualTo(2), "Add of a variable must use the [addrLow, addrHigh] memory-operand form");
            Assert.That(addOps[0], Is.EqualTo(0x83u), "y allocated at $83 — its VALUE address, not an immediate");
            Assert.That(addOps[1], Is.EqualTo(0x00u));

            Assert.That(result.GetKind(2), Is.EqualTo((byte)LlirInstructionKind.Store));
            var storeOps = result.GetOperands(2);
            Assert.That(storeOps[0], Is.EqualTo(0x80u), "z low allocated at $80");

            Assert.That(result.GetKind(3), Is.EqualTo((byte)LlirInstructionKind.Load));
            var loadHighOps = result.GetOperands(3);
            Assert.That(loadHighOps.Length, Is.EqualTo(1), "x has no high byte: its high load is an immediate");
            Assert.That(loadHighOps[0], Is.Zero);

            Assert.That(result.GetKind(4), Is.EqualTo((byte)LlirInstructionKind.Add));
            Assert.That((InstructionFlag)result.GetFlags(4), Is.EqualTo(InstructionFlag.CarryIn),
                "the high add consumes the low pair's carry");
            var addHighOps = result.GetOperands(4);
            Assert.That(addHighOps.Length, Is.EqualTo(1), "y has no high byte either: its high contribution is immediate 0");
            Assert.That(addHighOps[0], Is.Zero);

            Assert.That(result.GetKind(5), Is.EqualTo((byte)LlirInstructionKind.Store));
            Assert.That(result.GetOperands(5)[0], Is.EqualTo(0x81u), "z high allocated at $81");
        }
    }

    [Test]
    public void Transform_SubVariableMinusVariable_EmitsMemoryOperandSub()
    {
        // z := x - y. The difference can be as low as -255, so it cannot
        // provably fit in one byte and the Sub lowers wide (fix round
        // 15): the low pair is Load(x); Sub([yAddr, 0]); Store(z) in the
        // memory-operand form, and the high pair subtracts the absent
        // high bytes as immediate 0 with the low pair's borrow. The wide
        // result resolves to z first, so z allocates $80/$81, x takes
        // $82 and y $83.
        uint xOffset = _stringPool.Intern("x");
        uint yOffset = _stringPool.Intern("y");
        uint zOffset = _stringPool.Intern("z");
        uint tmpOffset = _stringPool.Intern("__tmp_0");

        var builder = new InstListBuilder();
        builder.Add((byte)LlirInstructionKind.Sub, InstructionFlag.None, 0, xOffset, yOffset);
        builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, tmpOffset, 0);
        builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, zOffset, tmpOffset);
        var mlir = builder.Build();

        var result = _transformer.TransformSlab(mlir, _stringPool);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Count, Is.EqualTo(6));
            Assert.That(result.GetKind(0), Is.EqualTo((byte)LlirInstructionKind.Load));
            Assert.That(result.GetOperands(0)[0], Is.EqualTo(0x82u), "x allocated at $82");
            Assert.That(result.GetKind(1), Is.EqualTo((byte)LlirInstructionKind.Sub));
            Assert.That((InstructionFlag)result.GetFlags(1), Is.EqualTo(InstructionFlag.None),
                "the low sub sets the borrow; it does not consume one");
            var subOps = result.GetOperands(1);
            Assert.That(subOps.Length, Is.EqualTo(2), "Sub of a variable must use the memory-operand form");
            Assert.That(subOps[0], Is.EqualTo(0x83u), "y's value address, not an immediate");
            Assert.That(subOps[1], Is.EqualTo(0x00u));
            Assert.That(result.GetKind(2), Is.EqualTo((byte)LlirInstructionKind.Store));
            Assert.That(result.GetOperands(2)[0], Is.EqualTo(0x80u), "z low allocated at $80");
            Assert.That(result.GetKind(3), Is.EqualTo((byte)LlirInstructionKind.Load));
            Assert.That(result.GetOperands(3).Length, Is.EqualTo(1), "x has no high byte: its high load is an immediate");
            Assert.That(result.GetKind(4), Is.EqualTo((byte)LlirInstructionKind.Sub));
            Assert.That((InstructionFlag)result.GetFlags(4), Is.EqualTo(InstructionFlag.CarryIn),
                "the high sub consumes the low pair's borrow");
            Assert.That(result.GetOperands(4)[0], Is.Zero, "y has no high byte either: its high contribution is immediate 0");
            Assert.That(result.GetKind(5), Is.EqualTo((byte)LlirInstructionKind.Store));
            Assert.That(result.GetOperands(5)[0], Is.EqualTo(0x81u), "z high allocated at $81");
        }
    }

    [Test]
    public void Transform_CmpVariableToVariable_EmitsMemoryOperandCmp()
    {
        // Cmp(x, y) must lower to Load(x); Cmp([yAddr, 0]) — comparing against
        // y's VALUE, not its address.
        uint xOffset = _stringPool.Intern("x");
        uint yOffset = _stringPool.Intern("y");

        var builder = new InstListBuilder();
        builder.Add((byte)LlirInstructionKind.Cmp, InstructionFlag.None, 0, xOffset, yOffset);
        var mlir = builder.Build();

        var result = _transformer.TransformSlab(mlir, _stringPool);

        Assert.That(result.Count, Is.EqualTo(2));
        Assert.That(result.GetKind(0), Is.EqualTo((byte)LlirInstructionKind.Load));
        Assert.That(result.GetKind(1), Is.EqualTo((byte)LlirInstructionKind.Cmp));
        var cmpOps = result.GetOperands(1);
        Assert.That(cmpOps.Length, Is.EqualTo(2), "Cmp of a variable must use the memory-operand form");
        Assert.That(cmpOps[0], Is.EqualTo(0x81u), "y's value address, not an immediate");
        Assert.That(cmpOps[1], Is.EqualTo(0x00u));
    }
        #endregion

        #region Sixteen-Bit Value Tests

        [Test]
        public void ValueWiderThanEightBitsAllocatesTwoSlotsAndCarriesCorrectly()
        {
            // x := 300; y := 500; z := x + y. 300 + 500 = 800, and the low
            // bytes (44 + 244 = 288) carry into the high byte, so the low
            // add must feed its carry into the high add. Each variable takes
            // TWO consecutive zero-page slots, low byte first: x $80/$81,
            // y $82/$83, z $84/$85. Values that fit in one byte keep the
            // single-slot lowering; only values above 255 take two slots.
            // Today the transformer masks every value with & 0xFF and
            // allocates one slot per variable, so 300 stores as 44 and y
            // lands on $81.
            uint xOffset = _stringPool.Intern("x");
            uint yOffset = _stringPool.Intern("y");
            uint zOffset = _stringPool.Intern("z");
            uint tmpOffset = _stringPool.Intern("__tmp_0");

            var builder = new InstListBuilder();
            builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, xOffset, _stringPool.Intern("300"));
            builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, yOffset, _stringPool.Intern("500"));
            builder.Add((byte)LlirInstructionKind.Add, InstructionFlag.None, 0, xOffset, yOffset);
            builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, tmpOffset, 0);
            builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, zOffset, tmpOffset);
            var mlir = builder.Build();

            var result = _transformer.TransformSlab(mlir, _stringPool);

            using (Assert.EnterMultipleScope())
            {
                // 300 stores as low 44 at $80 and high 1 at $81; 500 stores as
                // low 244 at $82 and high 1 at $83; then Load low, Add low,
                // Store low, Load high, Add high (carry from the low add),
                // Store high.
                Assert.That(result.Count, Is.EqualTo(10));

                Assert.That(result.GetKind(0), Is.EqualTo((byte)LlirInstructionKind.Assign));
                var assign0 = result.GetOperands(0);
                Assert.That(assign0.Length, Is.EqualTo(2));
                Assert.That(assign0[0], Is.EqualTo(0x80u), "x low byte at $80");
                Assert.That(assign0[1], Is.EqualTo(44u), "300 low byte is 44");

                Assert.That(result.GetKind(1), Is.EqualTo((byte)LlirInstructionKind.Assign));
                var assign1 = result.GetOperands(1);
                Assert.That(assign1.Length, Is.EqualTo(2));
                Assert.That(assign1[0], Is.EqualTo(0x81u), "x high byte at $81");
                Assert.That(assign1[1], Is.EqualTo(1u), "300 high byte is 1");

                Assert.That(result.GetKind(2), Is.EqualTo((byte)LlirInstructionKind.Assign));
                var assign2 = result.GetOperands(2);
                Assert.That(assign2.Length, Is.EqualTo(2));
                Assert.That(assign2[0], Is.EqualTo(0x82u), "y low byte at $82");
                Assert.That(assign2[1], Is.EqualTo(244u), "500 low byte is 244");

                Assert.That(result.GetKind(3), Is.EqualTo((byte)LlirInstructionKind.Assign));
                var assign3 = result.GetOperands(3);
                Assert.That(assign3.Length, Is.EqualTo(2));
                Assert.That(assign3[0], Is.EqualTo(0x83u), "y high byte at $83");
                Assert.That(assign3[1], Is.EqualTo(1u), "500 high byte is 1");

                Assert.That(result.GetKind(4), Is.EqualTo((byte)LlirInstructionKind.Load));
                var loadLow = result.GetOperands(4);
                Assert.That(loadLow.Length, Is.EqualTo(2));
                Assert.That(loadLow[0], Is.EqualTo(0x80u), "low add loads x low from $80");
                Assert.That(loadLow[1], Is.Zero);

                Assert.That(result.GetKind(5), Is.EqualTo((byte)LlirInstructionKind.Add));
                var addLow = result.GetOperands(5);
                Assert.That(addLow.Length, Is.EqualTo(2));
                Assert.That(addLow[0], Is.EqualTo(0x82u), "low add adds y low from $82");
                Assert.That(addLow[1], Is.Zero);

                Assert.That(result.GetKind(6), Is.EqualTo((byte)LlirInstructionKind.Store));
                var storeLow = result.GetOperands(6);
                Assert.That(storeLow.Length, Is.EqualTo(1));
                Assert.That(storeLow[0], Is.EqualTo(0x84u), "low sum stores to z low at $84");

                Assert.That(result.GetKind(7), Is.EqualTo((byte)LlirInstructionKind.Load));
                var loadHigh = result.GetOperands(7);
                Assert.That(loadHigh.Length, Is.EqualTo(2));
                Assert.That(loadHigh[0], Is.EqualTo(0x81u), "high add loads x high from $81");
                Assert.That(loadHigh[1], Is.Zero);

                // The high add is a separate step so the low add's carry feeds
                // it. No assertion is made here on instruction flags: the
                // mechanism that tells the code generator "add with incoming
                // carry, do not clear it first" (a flag, a new kind, or codegen
                // state) is the implementer's choice — the ROM test proves the
                // carry actually flows.
                Assert.That(result.GetKind(8), Is.EqualTo((byte)LlirInstructionKind.Add));
                var addHigh = result.GetOperands(8);
                Assert.That(addHigh.Length, Is.EqualTo(2));
                Assert.That(addHigh[0], Is.EqualTo(0x83u), "high add adds y high from $83");
                Assert.That(addHigh[1], Is.Zero);

                Assert.That(result.GetKind(9), Is.EqualTo((byte)LlirInstructionKind.Store));
                var storeHigh = result.GetOperands(9);
                Assert.That(storeHigh.Length, Is.EqualTo(1));
                Assert.That(storeHigh[0], Is.EqualTo(0x85u), "high sum stores to z high at $85");
            }
        }

        [Test]
        public void ValueWiderThanEightBitsSubtractsWithBorrow()
        {
            // x := 300; y := 500; w := x - y. The low bytes (44 - 244)
            // borrow from the high byte, so the high subtract must consume
            // the borrow — the mirror image of the carry test above.
            uint xOffset = _stringPool.Intern("x");
            uint yOffset = _stringPool.Intern("y");
            uint wOffset = _stringPool.Intern("w");
            uint tmpOffset = _stringPool.Intern("__tmp_0");

            var builder = new InstListBuilder();
            builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, xOffset, _stringPool.Intern("300"));
            builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, yOffset, _stringPool.Intern("500"));
            builder.Add((byte)LlirInstructionKind.Sub, InstructionFlag.None, 0, xOffset, yOffset);
            builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, tmpOffset, 0);
            builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, wOffset, tmpOffset);
            var mlir = builder.Build();

            var result = _transformer.TransformSlab(mlir, _stringPool);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Count, Is.EqualTo(10));

                Assert.That(result.GetKind(0), Is.EqualTo((byte)LlirInstructionKind.Assign));
                Assert.That(result.GetOperands(0)[0], Is.EqualTo(0x80u));
                Assert.That(result.GetOperands(0)[1], Is.EqualTo(44u));
                Assert.That(result.GetKind(1), Is.EqualTo((byte)LlirInstructionKind.Assign));
                Assert.That(result.GetOperands(1)[0], Is.EqualTo(0x81u));
                Assert.That(result.GetOperands(1)[1], Is.EqualTo(1u));
                Assert.That(result.GetKind(2), Is.EqualTo((byte)LlirInstructionKind.Assign));
                Assert.That(result.GetOperands(2)[0], Is.EqualTo(0x82u));
                Assert.That(result.GetOperands(2)[1], Is.EqualTo(244u));
                Assert.That(result.GetKind(3), Is.EqualTo((byte)LlirInstructionKind.Assign));
                Assert.That(result.GetOperands(3)[0], Is.EqualTo(0x83u));
                Assert.That(result.GetOperands(3)[1], Is.EqualTo(1u));

                Assert.That(result.GetKind(4), Is.EqualTo((byte)LlirInstructionKind.Load));
                Assert.That(result.GetOperands(4)[0], Is.EqualTo(0x80u), "low subtract loads x low from $80");
                Assert.That(result.GetKind(5), Is.EqualTo((byte)LlirInstructionKind.Sub));
                Assert.That(result.GetOperands(5)[0], Is.EqualTo(0x82u), "low subtract takes y low from $82");
                Assert.That(result.GetKind(6), Is.EqualTo((byte)LlirInstructionKind.Store));
                Assert.That(result.GetOperands(6)[0], Is.EqualTo(0x84u), "low difference stores to w low at $84");
                Assert.That(result.GetKind(7), Is.EqualTo((byte)LlirInstructionKind.Load));
                Assert.That(result.GetOperands(7)[0], Is.EqualTo(0x81u), "high subtract loads x high from $81");
                Assert.That(result.GetKind(8), Is.EqualTo((byte)LlirInstructionKind.Sub));
                Assert.That(result.GetOperands(8)[0], Is.EqualTo(0x83u), "high subtract takes y high from $83");
                Assert.That(result.GetKind(9), Is.EqualTo((byte)LlirInstructionKind.Store));
                Assert.That(result.GetOperands(9)[0], Is.EqualTo(0x85u), "high difference stores to w high at $85");
            }
        }

        [Test]
        public void ValueAtEightBitBoundary_AllocatesSecondSlotOnlyAbove255()
        {
            // 255 is the largest single-byte value: it keeps the established
            // single-slot, single-Assign lowering. 256 is the first value
            // that needs two slots (low 0, high 1), and the next variable
            // after it allocates past both.
            uint xOffset = _stringPool.Intern("x");
            uint yOffset = _stringPool.Intern("y");

            var builder = new InstListBuilder();
            builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, xOffset, _stringPool.Intern("255"));
            builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, yOffset, _stringPool.Intern("256"));
            var mlir = builder.Build();

            var result = _transformer.TransformSlab(mlir, _stringPool);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Count, Is.EqualTo(3));

                Assert.That(result.GetKind(0), Is.EqualTo((byte)LlirInstructionKind.Assign));
                var assign0 = result.GetOperands(0);
                Assert.That(assign0.Length, Is.EqualTo(2));
                Assert.That(assign0[0], Is.EqualTo(0x80u), "x := 255 still takes one slot at $80");
                Assert.That(assign0[1], Is.EqualTo(255u));

                Assert.That(result.GetKind(1), Is.EqualTo((byte)LlirInstructionKind.Assign));
                var assign1 = result.GetOperands(1);
                Assert.That(assign1[0], Is.EqualTo(0x81u), "y low byte at $81");
                Assert.That(assign1[1], Is.Zero, "256 low byte is 0");

                Assert.That(result.GetKind(2), Is.EqualTo((byte)LlirInstructionKind.Assign));
                var assign2 = result.GetOperands(2);
                Assert.That(assign2[0], Is.EqualTo(0x82u), "y high byte at $82");
                Assert.That(assign2[1], Is.EqualTo(1u), "256 high byte is 1");
            }
        }

        [Test]
        public void Transform_AssignConstant256_WritesZeroLowByteAndOneHighByte()
        {
            // x := 256. 256 is the first value needing two slots: the low byte
            // is 0 and the high byte is 1, so the transformer must emit TWO
            // Assigns — one to x's address carrying 0, one to x's address + 1
            // carrying 1. A narrowing mutant that emits a single Assign of
            // 256 & 0xFF == 0 loses the high byte.
            uint xOffset = _stringPool.Intern("x");

            var mlir = BuildMlirAssign(xOffset, _stringPool.Intern("256"));

            var result = _transformer.TransformSlab(mlir, _stringPool);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Count, Is.EqualTo(2), "256 needs a low and a high Assign");

                Assert.That(result.GetKind(0), Is.EqualTo((byte)LlirInstructionKind.Assign));
                var assignLow = result.GetOperands(0);
                Assert.That(assignLow.Length, Is.EqualTo(2));
                Assert.That(assignLow[0], Is.EqualTo(0x80u), "x low byte at $80");
                Assert.That(assignLow[1], Is.Zero, "256 low byte is 0");

                Assert.That(result.GetKind(1), Is.EqualTo((byte)LlirInstructionKind.Assign));
                var assignHigh = result.GetOperands(1);
                Assert.That(assignHigh.Length, Is.EqualTo(2));
                Assert.That(assignHigh[0], Is.EqualTo(0x81u), "x high byte at $80 + 1");
                Assert.That(assignHigh[1], Is.EqualTo(1u), "256 high byte is 1");
            }
        }

        [Test]
        public void Transform_AssignConstant255_WritesSingleAssignOnly()
        {
            // 255 is the width boundary: it stays a single-slot, single-Assign
            // lowering carrying 255 with no second instruction. A mutant using
            // >= 255 (or an always-true width condition) allocates a second
            // slot and emits a spurious high Assign of 0.
            uint xOffset = _stringPool.Intern("x");

            var mlir = BuildMlirAssign(xOffset, _stringPool.Intern("255"));

            var result = _transformer.TransformSlab(mlir, _stringPool);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Count, Is.EqualTo(1), "255 fits one slot: exactly one Assign");

                Assert.That(result.GetKind(0), Is.EqualTo((byte)LlirInstructionKind.Assign));
                var assign = result.GetOperands(0);
                Assert.That(assign.Length, Is.EqualTo(2));
                Assert.That(assign[0], Is.EqualTo(0x80u), "x at $80");
                Assert.That(assign[1], Is.EqualTo(255u), "255 stores unchanged");
            }
        }

        [Test]
        public void Transform_AssignConstant4660_WritesHighByteEighteen()
        {
            // 4660 == 0x1234: the low byte is 0x34 (52) and the high byte is
            // 0x12 (18). A mutant that shifts the high byte by the wrong amount
            // (or not at all) gets 4660 & 0xFF or a wrong high byte instead.
            uint xOffset = _stringPool.Intern("x");

            var mlir = BuildMlirAssign(xOffset, _stringPool.Intern("4660"));

            var result = _transformer.TransformSlab(mlir, _stringPool);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Count, Is.EqualTo(2), "4660 needs a low and a high Assign");

                Assert.That(result.GetKind(0), Is.EqualTo((byte)LlirInstructionKind.Assign));
                var assignLow = result.GetOperands(0);
                Assert.That(assignLow.Length, Is.EqualTo(2));
                Assert.That(assignLow[0], Is.EqualTo(0x80u), "x low byte at $80");
                Assert.That(assignLow[1], Is.EqualTo(52u), "4660 low byte is 0x34");

                Assert.That(result.GetKind(1), Is.EqualTo((byte)LlirInstructionKind.Assign));
                var assignHigh = result.GetOperands(1);
                Assert.That(assignHigh.Length, Is.EqualTo(2));
                Assert.That(assignHigh[0], Is.EqualTo(0x81u), "x high byte at $80 + 1");
                Assert.That(assignHigh[1], Is.EqualTo(18u), "4660 high byte is 0x12");
            }
        }

        [Test]
        public void Transform_RawZeroAssignToWideVariable_ClearsBothBytes()
        {
            // Regression: the declaration lowering emits the clearing
            // assign with raw slot 0 as the value operand, not an
            // interned "0". Slot 0 resolves to the empty string, so the
            // assign is neither a name copy nor a parseable constant,
            // and the fallback it used to land in wrote only the low
            // byte. A wide variable cleared that way kept its stale
            // high byte, so after x held 300 a later copy of the
            // cleared x read 256 instead of 0. The raw zero must clear
            // through the constant path, which writes both bytes of a
            // two-slot target.
            // MLIR: Assign(x, 300); Assign(x, raw 0).
            uint xOffset = _stringPool.Intern("x");

            var mlir = BuildMlirSlab(
                ((byte)MlirInstructionKind.Assign, new uint[] { xOffset, _stringPool.Intern("300") }),
                ((byte)MlirInstructionKind.Assign, new uint[] { xOffset, 0u }));

            var result = _transformer.TransformSlab(mlir, _stringPool);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Count, Is.EqualTo(4), "300 stores two bytes and the raw-zero clear writes two more");

                Assert.That(result.GetKind(0), Is.EqualTo((byte)LlirInstructionKind.Assign));
                Assert.That(result.GetOperands(0)[0], Is.EqualTo(0x80u), "x low byte at $80");
                Assert.That(result.GetOperands(0)[1], Is.EqualTo(44u), "300 low byte is 44");

                Assert.That(result.GetKind(1), Is.EqualTo((byte)LlirInstructionKind.Assign));
                Assert.That(result.GetOperands(1)[0], Is.EqualTo(0x81u), "x high byte at $80 + 1");
                Assert.That(result.GetOperands(1)[1], Is.EqualTo(1u), "300 high byte is 1");

                Assert.That(result.GetKind(2), Is.EqualTo((byte)LlirInstructionKind.Assign));
                Assert.That(result.GetOperands(2)[0], Is.EqualTo(0x80u), "the clear writes the low byte at $80");
                Assert.That(result.GetOperands(2)[1], Is.Zero, "the clear writes 0 to the low byte");

                Assert.That(result.GetKind(3), Is.EqualTo((byte)LlirInstructionKind.Assign));
                Assert.That(result.GetOperands(3)[0], Is.EqualTo(0x81u), "the clear writes the high byte at $80 + 1");
                Assert.That(result.GetOperands(3)[1], Is.Zero, "the clear writes 0 to the high byte, leaving nothing stale");
            }
        }

        [Test]
        public void Transform_RawZeroAssignToNarrowVariable_EmitsSingleAssign()
        {
            // The one-slot guard for the raw-zero clear: routing slot 0
            // through the constant path must not grow a second Assign
            // for a one-slot target, whose address + 1 is a different
            // variable or register.
            // MLIR: Assign(y, 5); Assign(y, raw 0).
            uint yOffset = _stringPool.Intern("y");

            var mlir = BuildMlirSlab(
                ((byte)MlirInstructionKind.Assign, new uint[] { yOffset, _stringPool.Intern("5") }),
                ((byte)MlirInstructionKind.Assign, new uint[] { yOffset, 0u }));

            var result = _transformer.TransformSlab(mlir, _stringPool);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Count, Is.EqualTo(2), "a one-slot target takes exactly one Assign per statement");

                Assert.That(result.GetKind(1), Is.EqualTo((byte)LlirInstructionKind.Assign));
                Assert.That(result.GetOperands(1)[0], Is.EqualTo(0x80u), "y at $80");
                Assert.That(result.GetOperands(1)[1], Is.Zero, "the raw zero clears y");
            }
        }

        [Test]
        public void WideAdd_LiteralRightOperand_AddsLowByteThenHighByteWithCarry()
        {
            // a := 300; z := a + 300. The right operand is a literal, so the
            // wide lowering must split it into its bytes: the low Add adds
            // immediate 44 (300 & 0xFF) and the high Add, fed by the low
            // pair's carry, adds immediate 1 ((300 >> 8) & 0xFF). A dispatch
            // mutant that treats the literal as a name (or as zero) adds the
            // wrong bytes; a byte-extraction mutant adds 300 whole or swaps
            // the bytes.
            uint aOffset = _stringPool.Intern("a");
            uint zOffset = _stringPool.Intern("z");
            uint tmpOffset = _stringPool.Intern("__tmp_0");
            uint literalOffset = _stringPool.Intern("300");

            var builder = new InstListBuilder();
            builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, aOffset, literalOffset);
            builder.Add((byte)LlirInstructionKind.Add, InstructionFlag.None, 0, aOffset, literalOffset);
            builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, tmpOffset, 0);
            builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, zOffset, tmpOffset);
            var mlir = builder.Build();

            var result = _transformer.TransformSlab(mlir, _stringPool);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Count, Is.EqualTo(8));

                Assert.That(result.GetKind(0), Is.EqualTo((byte)LlirInstructionKind.Assign));
                Assert.That(result.GetOperands(0)[0], Is.EqualTo(0x80u), "a low byte at $80");
                Assert.That(result.GetOperands(0)[1], Is.EqualTo(44u), "300 low byte is 44");
                Assert.That(result.GetKind(1), Is.EqualTo((byte)LlirInstructionKind.Assign));
                Assert.That(result.GetOperands(1)[0], Is.EqualTo(0x81u), "a high byte at $81");
                Assert.That(result.GetOperands(1)[1], Is.EqualTo(1u), "300 high byte is 1");

                Assert.That(result.GetKind(2), Is.EqualTo((byte)LlirInstructionKind.Load));
                var loadLow = result.GetOperands(2);
                Assert.That(loadLow.Length, Is.EqualTo(2));
                Assert.That(loadLow[0], Is.EqualTo(0x80u), "low pair loads a low from $80");
                Assert.That(loadLow[1], Is.Zero);

                Assert.That(result.GetKind(3), Is.EqualTo((byte)LlirInstructionKind.Add));
                Assert.That((InstructionFlag)result.GetFlags(3), Is.EqualTo(InstructionFlag.None),
                    "the low add sets the carry; it does not consume one");
                var addLow = result.GetOperands(3);
                Assert.That(addLow.Length, Is.EqualTo(1), "a literal right operand is a single immediate");
                Assert.That(addLow[0], Is.EqualTo(44u), "low add adds the literal's low byte, 44");

                Assert.That(result.GetKind(4), Is.EqualTo((byte)LlirInstructionKind.Store));
                Assert.That(result.GetOperands(4)[0], Is.EqualTo(0x82u), "low sum stores to z low at $82");

                Assert.That(result.GetKind(5), Is.EqualTo((byte)LlirInstructionKind.Load));
                var loadHigh = result.GetOperands(5);
                Assert.That(loadHigh.Length, Is.EqualTo(2));
                Assert.That(loadHigh[0], Is.EqualTo(0x81u), "high pair loads a high from $81");
                Assert.That(loadHigh[1], Is.Zero);

                Assert.That(result.GetKind(6), Is.EqualTo((byte)LlirInstructionKind.Add));
                Assert.That((InstructionFlag)result.GetFlags(6), Is.EqualTo(InstructionFlag.CarryIn),
                    "the high add consumes the low pair's carry");
                var addHigh = result.GetOperands(6);
                Assert.That(addHigh.Length, Is.EqualTo(1), "a literal right operand is a single immediate");
                Assert.That(addHigh[0], Is.EqualTo(1u), "high add adds the literal's high byte, 1");

                Assert.That(result.GetKind(7), Is.EqualTo((byte)LlirInstructionKind.Store));
                Assert.That(result.GetOperands(7)[0], Is.EqualTo(0x83u), "high sum stores to z high at $83");
            }
        }

        [Test]
        public void WideAdd_LiteralLeftOperand_LoadsLowByteThenHighByteWithCarry()
        {
            // a := 300; z := 300 + a. The left operand is a literal, so the
            // wide lowering must LOAD its bytes as immediates — low 44, then
            // high 1 — and add a's bytes from memory. A dispatch mutant that
            // routes the literal down the name path (or drops it to zero)
            // loads the wrong values.
            uint aOffset = _stringPool.Intern("a");
            uint zOffset = _stringPool.Intern("z");
            uint tmpOffset = _stringPool.Intern("__tmp_0");
            uint literalOffset = _stringPool.Intern("300");

            var builder = new InstListBuilder();
            builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, aOffset, literalOffset);
            builder.Add((byte)LlirInstructionKind.Add, InstructionFlag.None, 0, literalOffset, aOffset);
            builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, tmpOffset, 0);
            builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, zOffset, tmpOffset);
            var mlir = builder.Build();

            var result = _transformer.TransformSlab(mlir, _stringPool);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Count, Is.EqualTo(8));

                Assert.That(result.GetKind(0), Is.EqualTo((byte)LlirInstructionKind.Assign));
                Assert.That(result.GetOperands(0)[0], Is.EqualTo(0x80u), "a low byte at $80");
                Assert.That(result.GetOperands(0)[1], Is.EqualTo(44u), "300 low byte is 44");
                Assert.That(result.GetKind(1), Is.EqualTo((byte)LlirInstructionKind.Assign));
                Assert.That(result.GetOperands(1)[0], Is.EqualTo(0x81u), "a high byte at $81");
                Assert.That(result.GetOperands(1)[1], Is.EqualTo(1u), "300 high byte is 1");

                Assert.That(result.GetKind(2), Is.EqualTo((byte)LlirInstructionKind.Load));
                var loadLow = result.GetOperands(2);
                Assert.That(loadLow.Length, Is.EqualTo(1), "a literal left operand loads as a single immediate");
                Assert.That(loadLow[0], Is.EqualTo(44u), "low pair loads the literal's low byte, 44");

                Assert.That(result.GetKind(3), Is.EqualTo((byte)LlirInstructionKind.Add));
                Assert.That((InstructionFlag)result.GetFlags(3), Is.EqualTo(InstructionFlag.None),
                    "the low add sets the carry; it does not consume one");
                var addLow = result.GetOperands(3);
                Assert.That(addLow.Length, Is.EqualTo(2));
                Assert.That(addLow[0], Is.EqualTo(0x80u), "low add adds a low from $80");
                Assert.That(addLow[1], Is.Zero);

                Assert.That(result.GetKind(4), Is.EqualTo((byte)LlirInstructionKind.Store));
                Assert.That(result.GetOperands(4)[0], Is.EqualTo(0x82u), "low sum stores to z low at $82");

                Assert.That(result.GetKind(5), Is.EqualTo((byte)LlirInstructionKind.Load));
                var loadHigh = result.GetOperands(5);
                Assert.That(loadHigh.Length, Is.EqualTo(1), "a literal left operand loads as a single immediate");
                Assert.That(loadHigh[0], Is.EqualTo(1u), "high pair loads the literal's high byte, 1");

                Assert.That(result.GetKind(6), Is.EqualTo((byte)LlirInstructionKind.Add));
                Assert.That((InstructionFlag)result.GetFlags(6), Is.EqualTo(InstructionFlag.CarryIn),
                    "the high add consumes the low pair's carry");
                var addHigh = result.GetOperands(6);
                Assert.That(addHigh.Length, Is.EqualTo(2));
                Assert.That(addHigh[0], Is.EqualTo(0x81u), "high add adds a high from $81");
                Assert.That(addHigh[1], Is.Zero);

                Assert.That(result.GetKind(7), Is.EqualTo((byte)LlirInstructionKind.Store));
                Assert.That(result.GetOperands(7)[0], Is.EqualTo(0x83u), "high sum stores to z high at $83");
            }
        }

        [Test]
        public void Add_NarrowVariableAndWideLiteral_LowersWideOnLiteralWidth()
        {
            // a := 5; z := a + 300. a is one slot wide, so the ONLY wide
            // evidence is the literal: the lowering is wide because 300
            // needs two bytes. The high pair loads a's missing high byte as
            // immediate 0 and adds the literal's high byte, 1. A mutant that
            // reads the literal's width as 1 (boundary forced false, or the
            // numeric branch skipped) lowers narrow and drops the high byte.
            uint aOffset = _stringPool.Intern("a");
            uint zOffset = _stringPool.Intern("z");
            uint tmpOffset = _stringPool.Intern("__tmp_0");

            var builder = new InstListBuilder();
            builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, aOffset, _stringPool.Intern("5"));
            builder.Add((byte)LlirInstructionKind.Add, InstructionFlag.None, 0, aOffset, _stringPool.Intern("300"));
            builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, tmpOffset, 0);
            builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, zOffset, tmpOffset);
            var mlir = builder.Build();

            var result = _transformer.TransformSlab(mlir, _stringPool);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Count, Is.EqualTo(7));

                Assert.That(result.GetKind(0), Is.EqualTo((byte)LlirInstructionKind.Assign));
                Assert.That(result.GetOperands(0)[0], Is.EqualTo(0x80u), "a at $80");
                Assert.That(result.GetOperands(0)[1], Is.EqualTo(5u), "a := 5 fits one slot");

                Assert.That(result.GetKind(1), Is.EqualTo((byte)LlirInstructionKind.Load));
                var loadLow = result.GetOperands(1);
                Assert.That(loadLow.Length, Is.EqualTo(2));
                Assert.That(loadLow[0], Is.EqualTo(0x80u), "low pair loads a from $80");
                Assert.That(loadLow[1], Is.Zero);

                Assert.That(result.GetKind(2), Is.EqualTo((byte)LlirInstructionKind.Add));
                Assert.That((InstructionFlag)result.GetFlags(2), Is.EqualTo(InstructionFlag.None),
                    "the low add sets the carry; it does not consume one");
                var addLow = result.GetOperands(2);
                Assert.That(addLow.Length, Is.EqualTo(1), "a literal right operand is a single immediate");
                Assert.That(addLow[0], Is.EqualTo(44u), "low add adds the literal's low byte, 44");

                Assert.That(result.GetKind(3), Is.EqualTo((byte)LlirInstructionKind.Store));
                Assert.That(result.GetOperands(3)[0], Is.EqualTo(0x81u), "low sum stores to z low at $81");

                Assert.That(result.GetKind(4), Is.EqualTo((byte)LlirInstructionKind.Load));
                var loadHigh = result.GetOperands(4);
                Assert.That(loadHigh.Length, Is.EqualTo(1), "a one-slot variable's high byte loads as an immediate");
                Assert.That(loadHigh[0], Is.Zero, "a has no high byte: its high contribution is 0");

                Assert.That(result.GetKind(5), Is.EqualTo((byte)LlirInstructionKind.Add));
                Assert.That((InstructionFlag)result.GetFlags(5), Is.EqualTo(InstructionFlag.CarryIn),
                    "the high add consumes the low pair's carry");
                var addHigh = result.GetOperands(5);
                Assert.That(addHigh.Length, Is.EqualTo(1), "a literal right operand is a single immediate");
                Assert.That(addHigh[0], Is.EqualTo(1u), "high add adds the literal's high byte, 1");

                Assert.That(result.GetKind(6), Is.EqualTo((byte)LlirInstructionKind.Store));
                Assert.That(result.GetOperands(6)[0], Is.EqualTo(0x82u), "high sum stores to z high at $82");
            }
        }

        [Test]
        public void Add_NarrowVariableAndLiteral255_WidensToPreserveCarry()
        {
            // a := 5; z := a + 255. 255 is the largest one-byte literal:
            // it widens nothing by itself — a keeps its single slot, and
            // the low Add adds 255 whole, not split. But the sum can
            // reach 510, so the result cannot provably fit in one byte
            // and the Add lowers wide to preserve the carry: the low
            // pair computes 5 + 255 = 260, storing low byte 4 with the
            // carry set; the high pair adds 0 + 0 + carry and stores 1,
            // so z reads back 260, not 4. (Until fix round 15 this test
            // pinned the narrow lowering for this scenario; that pin
            // was the dropped-carry miscompile itself.)
            uint aOffset = _stringPool.Intern("a");
            uint zOffset = _stringPool.Intern("z");
            uint tmpOffset = _stringPool.Intern("__tmp_0");

            var builder = new InstListBuilder();
            builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, aOffset, _stringPool.Intern("5"));
            builder.Add((byte)LlirInstructionKind.Add, InstructionFlag.None, 0, aOffset, _stringPool.Intern("255"));
            builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, tmpOffset, 0);
            builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, zOffset, tmpOffset);
            var mlir = builder.Build();

            var result = _transformer.TransformSlab(mlir, _stringPool);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Count, Is.EqualTo(7), "a wide add is Assign, then the low and high byte pairs");

                Assert.That(result.GetKind(0), Is.EqualTo((byte)LlirInstructionKind.Assign));
                Assert.That(result.GetOperands(0)[0], Is.EqualTo(0x80u), "a at $80");
                Assert.That(result.GetOperands(0)[1], Is.EqualTo(5u), "a := 5 fits one slot");

                Assert.That(result.GetKind(1), Is.EqualTo((byte)LlirInstructionKind.Load));
                var loadLow = result.GetOperands(1);
                Assert.That(loadLow.Length, Is.EqualTo(2));
                Assert.That(loadLow[0], Is.EqualTo(0x80u), "low pair loads a from $80");
                Assert.That(loadLow[1], Is.Zero);

                Assert.That(result.GetKind(2), Is.EqualTo((byte)LlirInstructionKind.Add));
                Assert.That((InstructionFlag)result.GetFlags(2), Is.EqualTo(InstructionFlag.None),
                    "the low add sets the carry; it does not consume one");
                var addLow = result.GetOperands(2);
                Assert.That(addLow.Length, Is.EqualTo(1), "a literal right operand is a single immediate");
                Assert.That(addLow[0], Is.EqualTo(255u), "low add adds the literal's low byte, 255");

                Assert.That(result.GetKind(3), Is.EqualTo((byte)LlirInstructionKind.Store));
                Assert.That(result.GetOperands(3)[0], Is.EqualTo(0x81u), "low sum (4) stores to z low at $81");

                Assert.That(result.GetKind(4), Is.EqualTo((byte)LlirInstructionKind.Load));
                var loadHigh = result.GetOperands(4);
                Assert.That(loadHigh.Length, Is.EqualTo(1), "a one-slot variable's high byte loads as an immediate");
                Assert.That(loadHigh[0], Is.Zero, "a has no high byte: its high contribution is 0");

                Assert.That(result.GetKind(5), Is.EqualTo((byte)LlirInstructionKind.Add));
                Assert.That((InstructionFlag)result.GetFlags(5), Is.EqualTo(InstructionFlag.CarryIn),
                    "the high add consumes the low pair's carry");
                var addHigh = result.GetOperands(5);
                Assert.That(addHigh.Length, Is.EqualTo(1), "a literal right operand is a single immediate");
                Assert.That(addHigh[0], Is.Zero, "high add adds the literal's high byte, 0 — plus the carry");

                Assert.That(result.GetKind(6), Is.EqualTo((byte)LlirInstructionKind.Store));
                Assert.That(result.GetOperands(6)[0], Is.EqualTo(0x82u), "high sum (1) stores to z high at $82 — z reads back 260, not 4");
            }
        }

        [Test]
        public void Add_TwoNarrowVariables_CarriesIntoHighByte()
        {
            // a := 255; b := 1; x := a + b. Both operands fit one byte,
            // but the sum does not: 255 + 1 = 256. A narrow lowering
            // drops the carry out of the low add and x reads back 0 —
            // the miscompile this round fixes. The result takes two
            // slots and the Add lowers wide: the low pair computes
            // 255 + 1 = 0 and sets the carry; the high pair computes
            // 0 + 0 + carry = 1, so x holds low 0 at $82, high 1 at $83.
            uint aOffset = _stringPool.Intern("a");
            uint bOffset = _stringPool.Intern("b");
            uint xOffset = _stringPool.Intern("x");
            uint tmpOffset = _stringPool.Intern("__tmp_0");

            var builder = new InstListBuilder();
            builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, aOffset, _stringPool.Intern("255"));
            builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, bOffset, _stringPool.Intern("1"));
            builder.Add((byte)LlirInstructionKind.Add, InstructionFlag.None, 0, aOffset, bOffset);
            builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, tmpOffset, 0);
            builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, xOffset, tmpOffset);
            var mlir = builder.Build();

            var result = _transformer.TransformSlab(mlir, _stringPool);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Count, Is.EqualTo(8));

                Assert.That(result.GetKind(0), Is.EqualTo((byte)LlirInstructionKind.Assign));
                Assert.That(result.GetOperands(0)[0], Is.EqualTo(0x80u), "a at $80");
                Assert.That(result.GetOperands(0)[1], Is.EqualTo(255u), "a := 255 fits one slot");

                Assert.That(result.GetKind(1), Is.EqualTo((byte)LlirInstructionKind.Assign));
                Assert.That(result.GetOperands(1)[0], Is.EqualTo(0x81u), "b at $81");
                Assert.That(result.GetOperands(1)[1], Is.EqualTo(1u), "b := 1");

                Assert.That(result.GetKind(2), Is.EqualTo((byte)LlirInstructionKind.Load));
                var loadLow = result.GetOperands(2);
                Assert.That(loadLow.Length, Is.EqualTo(2));
                Assert.That(loadLow[0], Is.EqualTo(0x80u), "low pair loads a from $80");
                Assert.That(loadLow[1], Is.Zero);

                Assert.That(result.GetKind(3), Is.EqualTo((byte)LlirInstructionKind.Add));
                Assert.That((InstructionFlag)result.GetFlags(3), Is.EqualTo(InstructionFlag.None),
                    "the low add sets the carry; it does not consume one");
                var addLow = result.GetOperands(3);
                Assert.That(addLow.Length, Is.EqualTo(2), "a variable right operand uses the memory-operand form");
                Assert.That(addLow[0], Is.EqualTo(0x81u), "low add adds b's value from $81");
                Assert.That(addLow[1], Is.Zero);

                Assert.That(result.GetKind(4), Is.EqualTo((byte)LlirInstructionKind.Store));
                Assert.That(result.GetOperands(4)[0], Is.EqualTo(0x82u), "low sum (0) stores to x low at $82");

                Assert.That(result.GetKind(5), Is.EqualTo((byte)LlirInstructionKind.Load));
                var loadHigh = result.GetOperands(5);
                Assert.That(loadHigh.Length, Is.EqualTo(1), "a one-slot variable's high byte loads as an immediate");
                Assert.That(loadHigh[0], Is.Zero, "a has no high byte: its high contribution is 0");

                Assert.That(result.GetKind(6), Is.EqualTo((byte)LlirInstructionKind.Add));
                Assert.That((InstructionFlag)result.GetFlags(6), Is.EqualTo(InstructionFlag.CarryIn),
                    "the high add consumes the low pair's carry — that carry is the sum's high byte");
                var addHigh = result.GetOperands(6);
                Assert.That(addHigh.Length, Is.EqualTo(1));
                Assert.That(addHigh[0], Is.Zero, "b has no high byte either: the high add adds 0 plus the carry");

                Assert.That(result.GetKind(7), Is.EqualTo((byte)LlirInstructionKind.Store));
                Assert.That(result.GetOperands(7)[0], Is.EqualTo(0x83u), "high sum (1) stores to x high at $83 — x reads back 256, not 0");
            }
        }

        [Test]
        public void Sub_TwoNarrowVariables_BorrowsIntoHighByte()
        {
            // a := 0; b := 1; x := a - b. The difference is -1, which no
            // single byte holds: a narrow lowering wraps the low byte
            // to 255 and x reads back 255 instead of -1. The result
            // takes two slots and the Sub lowers wide: the low pair
            // computes 0 - 1 = 255 and borrows; the high pair computes
            // 0 - 0 - borrow = 255, so x holds the 16-bit -1 (low 255
            // at $82, high 255 at $83).
            uint aOffset = _stringPool.Intern("a");
            uint bOffset = _stringPool.Intern("b");
            uint xOffset = _stringPool.Intern("x");
            uint tmpOffset = _stringPool.Intern("__tmp_0");

            var builder = new InstListBuilder();
            builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, aOffset, _stringPool.Intern("0"));
            builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, bOffset, _stringPool.Intern("1"));
            builder.Add((byte)LlirInstructionKind.Sub, InstructionFlag.None, 0, aOffset, bOffset);
            builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, tmpOffset, 0);
            builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, xOffset, tmpOffset);
            var mlir = builder.Build();

            var result = _transformer.TransformSlab(mlir, _stringPool);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Count, Is.EqualTo(8));

                Assert.That(result.GetKind(0), Is.EqualTo((byte)LlirInstructionKind.Assign));
                Assert.That(result.GetOperands(0)[0], Is.EqualTo(0x80u), "a at $80");
                Assert.That(result.GetOperands(0)[1], Is.Zero, "a := 0");

                Assert.That(result.GetKind(1), Is.EqualTo((byte)LlirInstructionKind.Assign));
                Assert.That(result.GetOperands(1)[0], Is.EqualTo(0x81u), "b at $81");
                Assert.That(result.GetOperands(1)[1], Is.EqualTo(1u), "b := 1");

                Assert.That(result.GetKind(2), Is.EqualTo((byte)LlirInstructionKind.Load));
                var loadLow = result.GetOperands(2);
                Assert.That(loadLow.Length, Is.EqualTo(2));
                Assert.That(loadLow[0], Is.EqualTo(0x80u), "low pair loads a from $80");
                Assert.That(loadLow[1], Is.Zero);

                Assert.That(result.GetKind(3), Is.EqualTo((byte)LlirInstructionKind.Sub));
                Assert.That((InstructionFlag)result.GetFlags(3), Is.EqualTo(InstructionFlag.None),
                    "the low sub sets the borrow; it does not consume one");
                var subLow = result.GetOperands(3);
                Assert.That(subLow.Length, Is.EqualTo(2), "a variable right operand uses the memory-operand form");
                Assert.That(subLow[0], Is.EqualTo(0x81u), "low sub subtracts b's value from $81");
                Assert.That(subLow[1], Is.Zero);

                Assert.That(result.GetKind(4), Is.EqualTo((byte)LlirInstructionKind.Store));
                Assert.That(result.GetOperands(4)[0], Is.EqualTo(0x82u), "low difference (255) stores to x low at $82");

                Assert.That(result.GetKind(5), Is.EqualTo((byte)LlirInstructionKind.Load));
                var loadHigh = result.GetOperands(5);
                Assert.That(loadHigh.Length, Is.EqualTo(1), "a one-slot variable's high byte loads as an immediate");
                Assert.That(loadHigh[0], Is.Zero, "a has no high byte: its high contribution is 0");

                Assert.That(result.GetKind(6), Is.EqualTo((byte)LlirInstructionKind.Sub));
                Assert.That((InstructionFlag)result.GetFlags(6), Is.EqualTo(InstructionFlag.CarryIn),
                    "the high sub consumes the low pair's borrow");
                var subHigh = result.GetOperands(6);
                Assert.That(subHigh.Length, Is.EqualTo(1));
                Assert.That(subHigh[0], Is.Zero, "b has no high byte either: the high sub subtracts 0 plus the borrow");

                Assert.That(result.GetKind(7), Is.EqualTo((byte)LlirInstructionKind.Store));
                Assert.That(result.GetOperands(7)[0], Is.EqualTo(0x83u), "high difference (255) stores to x high at $83 — x reads back -1, not 255");
            }
        }

        [Test]
        public void Add_NarrowVariableAndLiteralZero_StaysNarrow()
        {
            // a := 5; z := a + 0. Adding zero can never carry: whatever
            // byte a holds, the sum is that same byte, so the bounds
            // rule proves the result byte-fitting and the Add keeps the
            // narrow lowering. This guards the fix against widening
            // every Add unconditionally — the widening must be earned
            // by the bounds, never applied blanket.
            uint aOffset = _stringPool.Intern("a");
            uint zOffset = _stringPool.Intern("z");
            uint tmpOffset = _stringPool.Intern("__tmp_0");

            var builder = new InstListBuilder();
            builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, aOffset, _stringPool.Intern("5"));
            builder.Add((byte)LlirInstructionKind.Add, InstructionFlag.None, 0, aOffset, _stringPool.Intern("0"));
            builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, tmpOffset, 0);
            builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, zOffset, tmpOffset);
            var mlir = builder.Build();

            var result = _transformer.TransformSlab(mlir, _stringPool);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Count, Is.EqualTo(4), "a narrow add is Assign, Load, Add, Store — no byte pairs");

                Assert.That(result.GetKind(0), Is.EqualTo((byte)LlirInstructionKind.Assign));
                Assert.That(result.GetOperands(0)[0], Is.EqualTo(0x80u), "a at $80");
                Assert.That(result.GetOperands(0)[1], Is.EqualTo(5u), "a := 5 fits one slot");

                Assert.That(result.GetKind(1), Is.EqualTo((byte)LlirInstructionKind.Load));
                var load = result.GetOperands(1);
                Assert.That(load.Length, Is.EqualTo(2));
                Assert.That(load[0], Is.EqualTo(0x80u), "the add loads a from $80");
                Assert.That(load[1], Is.Zero);

                Assert.That(result.GetKind(2), Is.EqualTo((byte)LlirInstructionKind.Add));
                Assert.That((InstructionFlag)result.GetFlags(2), Is.EqualTo(InstructionFlag.None),
                    "a narrow add has no carry chain");
                var add = result.GetOperands(2);
                Assert.That(add.Length, Is.EqualTo(1), "a literal right operand is a single immediate");
                Assert.That(add[0], Is.Zero, "the add adds 0");

                Assert.That(result.GetKind(3), Is.EqualTo((byte)LlirInstructionKind.Store));
                Assert.That(result.GetOperands(3)[0], Is.EqualTo(0x81u), "the sum stores to z at $81, its only slot");
            }
        }

        [Test]
        public void AddResultAssignedToWideVariable_StoresComputedHighByte()
        {
            // x := 300; a := 5; x := a + 1. The sum can reach 256, so it
            // cannot provably fit in one byte: the Add lowers wide and
            // stores BOTH computed bytes into x. The high pair computes
            // 0 + 0 + carry(0) = 0, so x's high byte becomes a genuinely
            // computed 0 — the stale 1 left by x := 300 is replaced by
            // the arithmetic itself, and x reads back 6, never 262.
            // (Until fix round 15 the Add stayed narrow here and a
            // separate zero-extension store cleared the high byte; that
            // shape was the dropped-carry lowering.)
            uint xOffset = _stringPool.Intern("x");
            uint aOffset = _stringPool.Intern("a");
            uint tmpOffset = _stringPool.Intern("__tmp_0");

            var builder = new InstListBuilder();
            builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, xOffset, _stringPool.Intern("300"));
            builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, aOffset, _stringPool.Intern("5"));
            builder.Add((byte)LlirInstructionKind.Add, InstructionFlag.None, 0, aOffset, _stringPool.Intern("1"));
            builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, tmpOffset, 0);
            builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, xOffset, tmpOffset);
            var mlir = builder.Build();

            var result = _transformer.TransformSlab(mlir, _stringPool);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Count, Is.EqualTo(9));

                Assert.That(result.GetKind(0), Is.EqualTo((byte)LlirInstructionKind.Assign));
                Assert.That(result.GetOperands(0)[0], Is.EqualTo(0x80u), "x low byte at $80");
                Assert.That(result.GetOperands(0)[1], Is.EqualTo(44u), "300 low byte is 44");
                Assert.That(result.GetKind(1), Is.EqualTo((byte)LlirInstructionKind.Assign));
                Assert.That(result.GetOperands(1)[0], Is.EqualTo(0x81u), "x high byte at $81");
                Assert.That(result.GetOperands(1)[1], Is.EqualTo(1u), "300 high byte is 1");

                Assert.That(result.GetKind(2), Is.EqualTo((byte)LlirInstructionKind.Assign));
                Assert.That(result.GetOperands(2)[0], Is.EqualTo(0x82u), "a at $82");
                Assert.That(result.GetOperands(2)[1], Is.EqualTo(5u));

                Assert.That(result.GetKind(3), Is.EqualTo((byte)LlirInstructionKind.Load));
                Assert.That(result.GetOperands(3)[0], Is.EqualTo(0x82u), "the low pair loads a from $82");
                Assert.That(result.GetKind(4), Is.EqualTo((byte)LlirInstructionKind.Add));
                Assert.That((InstructionFlag)result.GetFlags(4), Is.EqualTo(InstructionFlag.None),
                    "the low add sets the carry; it does not consume one");
                Assert.That(result.GetOperands(4)[0], Is.EqualTo(1u), "the low add adds 1");

                Assert.That(result.GetKind(5), Is.EqualTo((byte)LlirInstructionKind.Store));
                Assert.That(result.GetOperands(5)[0], Is.EqualTo(0x80u), "the low sum (6) stores to x low at $80");

                Assert.That(result.GetKind(6), Is.EqualTo((byte)LlirInstructionKind.Load));
                var loadHigh = result.GetOperands(6);
                Assert.That(loadHigh.Length, Is.EqualTo(1), "an immediate load, not a memory load");
                Assert.That(loadHigh[0], Is.Zero, "a has no high byte: the high pair starts from 0");

                Assert.That(result.GetKind(7), Is.EqualTo((byte)LlirInstructionKind.Add));
                Assert.That((InstructionFlag)result.GetFlags(7), Is.EqualTo(InstructionFlag.CarryIn),
                    "the high add consumes the low pair's carry");
                Assert.That(result.GetOperands(7)[0], Is.Zero, "the literal's high byte is 0");

                Assert.That(result.GetKind(8), Is.EqualTo((byte)LlirInstructionKind.Store));
                Assert.That(result.GetOperands(8)[0], Is.EqualTo(0x81u), "the computed high byte (0) stores to x high at $81 — x reads back 6, never 262");
            }
        }

        [Test]
        public void WideVariableAssignedToTiaRegister_StoresLowByteOnly()
        {
            // x := 300; COLUBK := x. COLUBK is a one-byte TIA register at
            // $09; the byte after it, $0A, is CTRLPF. Copying the wide
            // variable must store only its low byte — a high-byte store
            // gated on EITHER side being wide writes x's high byte (1)
            // into CTRLPF. The gate is the DESTINATION's width.
            uint xOffset = _stringPool.Intern("x");
            uint colubkOffset = _stringPool.Intern("COLUBK");

            var builder = new InstListBuilder();
            builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, xOffset, _stringPool.Intern("300"));
            builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, colubkOffset, xOffset);
            var mlir = builder.Build();

            var result = _transformer.TransformSlab(mlir, _stringPool);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Count, Is.EqualTo(4), "two Assigns for x := 300, then Load + Store for the copy — no high-byte store");

                Assert.That(result.GetKind(2), Is.EqualTo((byte)LlirInstructionKind.Load));
                Assert.That(result.GetOperands(2)[0], Is.EqualTo(0x80u), "the copy loads x low from $80");
                Assert.That(result.GetKind(3), Is.EqualTo((byte)LlirInstructionKind.Store));
                Assert.That(result.GetOperands(3)[0], Is.EqualTo(0x09u), "the copy stores to COLUBK at $09");

                for (int i = 0; i < result.Count; i++)
                {
                    if (result.GetKind(i) == (byte)LlirInstructionKind.Store)
                        Assert.That(result.GetOperands(i)[0], Is.Not.EqualTo(0x0Au),
                            $"instruction {i} must not store to CTRLPF at $0A");
                }
            }
        }

        [Test]
        public void WideAddAssignedToTiaRegister_StoresLowByteOnly()
        {
            // x := 300; y := 500; COLUBK := x + y. The wide Add stores
            // its result straight into the assignment target, so the
            // target here is COLUBK itself: the low byte pair stores to
            // $09, and the high byte pair must not run at all — its
            // store would land on $0A (CTRLPF).
            uint xOffset = _stringPool.Intern("x");
            uint yOffset = _stringPool.Intern("y");
            uint colubkOffset = _stringPool.Intern("COLUBK");
            uint tmpOffset = _stringPool.Intern("__tmp_0");

            var builder = new InstListBuilder();
            builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, xOffset, _stringPool.Intern("300"));
            builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, yOffset, _stringPool.Intern("500"));
            builder.Add((byte)LlirInstructionKind.Add, InstructionFlag.None, 0, xOffset, yOffset);
            builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, tmpOffset, 0);
            builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, colubkOffset, tmpOffset);
            var mlir = builder.Build();

            var result = _transformer.TransformSlab(mlir, _stringPool);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Count, Is.EqualTo(7), "four Assigns for the two wide constants, then only the low byte pair");

                Assert.That(result.GetKind(4), Is.EqualTo((byte)LlirInstructionKind.Load));
                Assert.That(result.GetOperands(4)[0], Is.EqualTo(0x80u), "the low add loads x low from $80");
                Assert.That(result.GetKind(5), Is.EqualTo((byte)LlirInstructionKind.Add));
                Assert.That(result.GetOperands(5)[0], Is.EqualTo(0x82u), "the low add adds y low from $82");
                Assert.That(result.GetKind(6), Is.EqualTo((byte)LlirInstructionKind.Store));
                Assert.That(result.GetOperands(6)[0], Is.EqualTo(0x09u), "the low sum stores to COLUBK at $09");

                for (int i = 0; i < result.Count; i++)
                {
                    if (result.GetKind(i) == (byte)LlirInstructionKind.Store)
                        Assert.That(result.GetOperands(i)[0], Is.Not.EqualTo(0x0Au),
                            $"instruction {i} must not store to CTRLPF at $0A");
                }
            }
        }

        [Test]
        public void WideTempAssignedToTiaRegister_StoresLowByteOnly()
        {
            // x takes 300, y takes 500, and their sum stays in a temp
            // because a later Add reads it (z takes the temp plus 1).
            // COLUBK then takes the temp, so the COLUBK assignment
            // copies the wide temp from memory (StoreAccumulatorValue's
            // wide path). Only the low byte may land on COLUBK: the
            // high-byte copy would store to $0A (CTRLPF).
            uint xOffset = _stringPool.Intern("x");
            uint yOffset = _stringPool.Intern("y");
            uint zOffset = _stringPool.Intern("z");
            uint colubkOffset = _stringPool.Intern("COLUBK");
            uint tmp0Offset = _stringPool.Intern("__tmp_0");
            uint tmp1Offset = _stringPool.Intern("__tmp_1");

            var builder = new InstListBuilder();
            builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, xOffset, _stringPool.Intern("300"));
            builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, yOffset, _stringPool.Intern("500"));
            builder.Add((byte)LlirInstructionKind.Add, InstructionFlag.None, 0, xOffset, yOffset);
            builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, tmp0Offset, 0);
            builder.Add((byte)LlirInstructionKind.Add, InstructionFlag.None, 0, tmp0Offset, _stringPool.Intern("1"));
            builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, tmp1Offset, 0);
            builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, zOffset, tmp1Offset);
            builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, colubkOffset, tmp0Offset);
            var mlir = builder.Build();

            var result = _transformer.TransformSlab(mlir, _stringPool);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Count, Is.EqualTo(18),
                    "four Assigns, two wide byte-pair Adds (6 each), then Load + Store for the temp copy");

                Assert.That(result.GetKind(6), Is.EqualTo((byte)LlirInstructionKind.Store));
                Assert.That(result.GetOperands(6)[0], Is.EqualTo(0x84u), "the first sum's low byte lives in its temp at $84");
                Assert.That(result.GetKind(9), Is.EqualTo((byte)LlirInstructionKind.Store));
                Assert.That(result.GetOperands(9)[0], Is.EqualTo(0x85u), "the first sum's high byte lives in its temp at $85");
                Assert.That(result.GetKind(12), Is.EqualTo((byte)LlirInstructionKind.Store));
                Assert.That(result.GetOperands(12)[0], Is.EqualTo(0x86u), "the second sum stores low to z at $86");
                Assert.That(result.GetKind(15), Is.EqualTo((byte)LlirInstructionKind.Store));
                Assert.That(result.GetOperands(15)[0], Is.EqualTo(0x87u), "the second sum stores high to z at $87");

                Assert.That(result.GetKind(16), Is.EqualTo((byte)LlirInstructionKind.Load));
                Assert.That(result.GetOperands(16)[0], Is.EqualTo(0x84u), "the COLUBK copy loads the temp's low byte from $84");
                Assert.That(result.GetKind(17), Is.EqualTo((byte)LlirInstructionKind.Store));
                Assert.That(result.GetOperands(17)[0], Is.EqualTo(0x09u), "the COLUBK copy stores to $09 — and stops there");

                for (int i = 0; i < result.Count; i++)
                {
                    if (result.GetKind(i) == (byte)LlirInstructionKind.Store)
                        Assert.That(result.GetOperands(i)[0], Is.Not.EqualTo(0x0Au),
                            $"instruction {i} must not store to CTRLPF at $0A");
                }
            }
        }

        [Test]
        public void CopyAssignedBeforeSourceWidens_TargetStillGetsTwoSlots()
        {
            // Declarations of x, y, and sentinel come first. Then
            // sentinel takes 7, x takes 5, y takes x, and x takes 300.
            // A single inference pass visits the y-from-x copy while x
            // is still recorded at one slot, so y keeps one slot and
            // sentinel allocates at $83 — the very address the copy
            // then writes x's high byte to, silently replacing
            // sentinel. Width inference must run to a fixed point:
            // y takes two slots ($82/$83) and sentinel lands past
            // them at $84.
            uint xOffset = _stringPool.Intern("x");
            uint yOffset = _stringPool.Intern("y");
            uint sentinelOffset = _stringPool.Intern("sentinel");

            var builder = new InstListBuilder();
            builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, xOffset, _stringPool.Intern("0"));
            builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, yOffset, _stringPool.Intern("0"));
            builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, sentinelOffset, _stringPool.Intern("0"));
            builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, sentinelOffset, _stringPool.Intern("7"));
            builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, xOffset, _stringPool.Intern("5"));
            builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, yOffset, xOffset);
            builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, xOffset, _stringPool.Intern("300"));
            var mlir = builder.Build();

            var result = _transformer.TransformSlab(mlir, _stringPool);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Count, Is.EqualTo(14));

                Assert.That(result.GetKind(4), Is.EqualTo((byte)LlirInstructionKind.Assign));
                Assert.That(result.GetOperands(4)[0], Is.EqualTo(0x84u), "sentinel allocates at $84, past y's two slots");
                Assert.That(result.GetKind(5), Is.EqualTo((byte)LlirInstructionKind.Assign));
                Assert.That(result.GetOperands(5)[0], Is.EqualTo(0x84u), "sentinel := 7 writes to $84");
                Assert.That(result.GetOperands(5)[1], Is.EqualTo(7u));

                Assert.That(result.GetKind(9), Is.EqualTo((byte)LlirInstructionKind.Store));
                Assert.That(result.GetOperands(9)[0], Is.EqualTo(0x82u), "the copy stores x low to y low at $82");
                Assert.That(result.GetKind(11), Is.EqualTo((byte)LlirInstructionKind.Store));
                Assert.That(result.GetOperands(11)[0], Is.EqualTo(0x83u), "the copy stores x high to y high at $83 — y's own slot, not sentinel's");
            }
        }

        [Test]
        public void BitwiseNotOfWideVariable_ResultTempStaysOneSlot()
        {
            // x := 300; y := not x. The complement the backend computes
            // is 8-bit: Load x's low byte, EOR #$FF, result in the
            // accumulator (211 for 300's low byte 44). If the result
            // temp inherits x's two-slot width, the final assignment
            // takes the wide-temp path and loads two bytes from the
            // temp's slots — which nothing ever wrote — instead of
            // storing the accumulator, so y becomes 0. Like Multiply
            // and BitwiseAnd/Or, the BitwiseNot result temp records
            // width 1, and the assignment stores the accumulator
            // straight to y's single slot.
            uint xOffset = _stringPool.Intern("x");
            uint yOffset = _stringPool.Intern("y");
            uint tmpOffset = _stringPool.Intern("__tmp_0");

            var builder = new InstListBuilder();
            builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, xOffset, _stringPool.Intern("300"));
            builder.Add((byte)LlirInstructionKind.BitwiseNot, InstructionFlag.None, 0, xOffset);
            builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, tmpOffset, 0);
            builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, yOffset, tmpOffset);
            var mlir = builder.Build();

            var result = _transformer.TransformSlab(mlir, _stringPool);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Count, Is.EqualTo(5),
                    "two Assigns for x := 300, Load + BitwiseNot, then a single accumulator Store");

                Assert.That(result.GetKind(2), Is.EqualTo((byte)LlirInstructionKind.Load));
                Assert.That(result.GetOperands(2)[0], Is.EqualTo(0x80u), "the complement loads x low from $80");
                Assert.That(result.GetKind(3), Is.EqualTo((byte)LlirInstructionKind.BitwiseNot));
                Assert.That(result.GetKind(4), Is.EqualTo((byte)LlirInstructionKind.Store));
                Assert.That(result.GetOperands(4)[0], Is.EqualTo(0x82u),
                    "the accumulator stores straight to y at $82 — no loads from an unwritten temp");
            }
        }

        [Test]
        public void TransformSlab_VariableWidenedInSecondCompilation_DoesNotClobberNeighbour()
        {
            // The transformer is a singleton: one instance transforms every
            // slab in the process, and each compilation is independent of
            // the ones before it. Compilation 1: a := 5; b := 7, both
            // narrow, so a takes $80 and b takes $81. Compilation 2 widens
            // a (a := 300) while b stays narrow: allocation restarts, so a
            // takes $80/$81 and b takes $82. If the first compilation's
            // addresses survive into the second, a keeps $80 but is now
            // two slots wide, so its high byte writes to $81, which is
            // still recorded as b's slot, and b's own assignment then
            // lands on the same address.
            uint aOffset = _stringPool.Intern("a");
            uint bOffset = _stringPool.Intern("b");

            var firstBuilder = new InstListBuilder();
            firstBuilder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, aOffset, _stringPool.Intern("5"));
            firstBuilder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, bOffset, _stringPool.Intern("7"));
            var first = _transformer.TransformSlab(firstBuilder.Build(), _stringPool);

            var secondBuilder = new InstListBuilder();
            secondBuilder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, aOffset, _stringPool.Intern("300"));
            secondBuilder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, bOffset, _stringPool.Intern("9"));
            var second = _transformer.TransformSlab(secondBuilder.Build(), _stringPool);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(first.Count, Is.EqualTo(2));
                Assert.That(first.GetOperands(0)[0], Is.EqualTo(0x80u), "first compilation: a at $80");
                Assert.That(first.GetOperands(1)[0], Is.EqualTo(0x81u), "first compilation: b at $81");

                Assert.That(second.Count, Is.EqualTo(3),
                    "a := 300 emits two Assigns (low and high byte), b := 9 emits one");
                Assert.That(second.GetKind(0), Is.EqualTo((byte)LlirInstructionKind.Assign));
                Assert.That(second.GetOperands(0)[0], Is.EqualTo(0x80u), "second compilation restarts allocation: a low at $80");
                Assert.That(second.GetOperands(0)[1], Is.EqualTo(44u), "300 low byte is 44");
                Assert.That(second.GetKind(1), Is.EqualTo((byte)LlirInstructionKind.Assign));
                Assert.That(second.GetOperands(1)[0], Is.EqualTo(0x81u), "a high byte at $81, a's own second slot");
                Assert.That(second.GetOperands(1)[1], Is.EqualTo(1u), "300 high byte is 1");
                Assert.That(second.GetKind(2), Is.EqualTo((byte)LlirInstructionKind.Assign));
                Assert.That(second.GetOperands(2)[0], Is.EqualTo(0x82u),
                    "b allocates past both of a's slots at $82, not on a's high byte at $81");
                Assert.That(second.GetOperands(2)[1], Is.EqualTo(9u));
            }
        }

        #endregion
        #region Branch Polarity Tests

        [Test]
        public void Transform_CmpFollowedByTransitionAndBranch_ThenArm()
        {
            // MLIR for an if-arm: compare left with zero, marker for branch
            // polarity, then a conditional branch. The transformer splits the
            // compare into a load followed by a one-operand compare, and keeps
            // the polarity marker so the backend emits BEQ.
            var pool = new StringPool();
            uint xOff = pool.Intern("x");
            uint zeroOff = pool.Intern("0");
            uint endOff = pool.Intern("L_end");

            var builder = new InstListBuilder();
            builder.Add((byte)LlirInstructionKind.Cmp, InstructionFlag.None, 0, xOff, zeroOff);
            builder.Add((byte)LlirInstructionKind.Transition, InstructionFlag.None, 0);
            builder.Add((byte)MlirInstructionKind.Branch, InstructionFlag.None, 0, endOff);
            var mlir = builder.Build();

            var result = _transformer.TransformSlab(mlir, pool);

            // Output order: load, compare, polarity marker, branch.
            Assert.That(result.Count, Is.EqualTo(4));
            Assert.That(result.GetKind(0), Is.EqualTo((byte)LlirInstructionKind.Load));
            Assert.That(result.GetKind(1), Is.EqualTo((byte)LlirInstructionKind.Cmp));
            Assert.That(result.GetKind(2), Is.EqualTo((byte)LlirInstructionKind.Transition));
            Assert.That(result.GetKind(3), Is.EqualTo((byte)LlirInstructionKind.Branch));
        }
        [Test]
        public void Transform_UnconditionalBranch_StaysJump()
        {
            // A branch with no compare or polarity predecessor is unconditional.
            var pool = new StringPool();
            uint endOff = pool.Intern("L_end");
            var builder = new InstListBuilder();
            builder.Add((byte)MlirInstructionKind.Branch, InstructionFlag.None, 0, endOff);
            var mlir = builder.Build();

            var result = _transformer.TransformSlab(mlir, pool);
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result.GetKind(0), Is.EqualTo((byte)LlirInstructionKind.Jump));
        }
        [Test]
        public void Transform_UnaryMinusSequenceWithoutWideResult_EmitsSixteenBitSub()
        {
            // MLIR for x := -y in its un-flagged form:
            //   Sub("0", y); Assign(__tmp_0, 0); Assign(x, __tmp_0)
            // Without the WideResult flag this is an ordinary binary
            // subtraction 0 - y, whose difference range is -255..0 — it
            // cannot provably fit in one byte, so the bounds rule (fix
            // round 15) takes the wide path and lands on the same shape
            // as the flagged unary minus below: the low pair stores the
            // low byte, the high pair subtracts y's (absent) high byte
            // as immediate 0 with the borrow and stores the high byte.
            // The wide result resolves to x first, so x allocates
            // $80/$81 and y takes $82.
            uint zeroOffset = _stringPool.Intern("0");
            uint yOffset = _stringPool.Intern("y");
            uint xOffset = _stringPool.Intern("x");
            uint tmpOffset = _stringPool.Intern("__tmp_0");

            var mlir = BuildMlirSlab(
                ((byte)LlirInstructionKind.Sub, new uint[] { zeroOffset, yOffset }),
                ((byte)MlirInstructionKind.Assign, new uint[] { tmpOffset, 0 }),
                ((byte)MlirInstructionKind.Assign, new uint[] { xOffset, tmpOffset }));

            var result = _transformer.TransformSlab(mlir, _stringPool);

            Assert.That(result.Count, Is.EqualTo(6));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.GetKind(0), Is.EqualTo((byte)LlirInstructionKind.Load));
                var loadOps = result.GetOperands(0);
                Assert.That(loadOps.Length, Is.EqualTo(1));
                Assert.That(loadOps[0], Is.EqualTo(0u), "Load immediate 0");

                Assert.That(result.GetKind(1), Is.EqualTo((byte)LlirInstructionKind.Sub));
                var subOps = result.GetOperands(1);
                Assert.That(subOps.Length, Is.EqualTo(2));
                Assert.That(subOps[0], Is.EqualTo(0x82u), "Low Sub subtracts y's zero-page address");
                Assert.That((InstructionFlag)result.GetFlags(1), Is.EqualTo(InstructionFlag.None),
                    "Low Sub sets the borrow itself (SEC)");

                Assert.That(result.GetKind(2), Is.EqualTo((byte)LlirInstructionKind.Store));
                var storeOps = result.GetOperands(2);
                Assert.That(storeOps.Length, Is.EqualTo(1));
                Assert.That(storeOps[0], Is.EqualTo(0x80u), "Store targets x's first slot");

                Assert.That(result.GetKind(3), Is.EqualTo((byte)LlirInstructionKind.Load));
                Assert.That(result.GetOperands(3)[0], Is.Zero, "High pair loads immediate 0");

                Assert.That(result.GetKind(4), Is.EqualTo((byte)LlirInstructionKind.Sub));
                var highSubOps = result.GetOperands(4);
                Assert.That(highSubOps.Length, Is.EqualTo(1));
                Assert.That(highSubOps[0], Is.Zero, "High Sub subtracts immediate 0 (narrow y has no high byte)");
                Assert.That((InstructionFlag)result.GetFlags(4), Is.EqualTo(InstructionFlag.CarryIn),
                    "High Sub consumes the low pair's borrow (no SEC)");

                Assert.That(result.GetKind(5), Is.EqualTo((byte)LlirInstructionKind.Store));
                Assert.That(result.GetOperands(5)[0], Is.EqualTo(0x81u), "High byte stores to x's second slot");
            }
        }

        [Test]
        public void Transform_UnaryMinusSequenceWithWideResult_EmitsSixteenBitSub()
        {
            // Same MLIR as Transform_UnaryMinusSequence_EmitsLoadZeroSubStore,
            // but the Sub carries WideResult (set by the unary-minus
            // lowering): negation is a 16-bit operation, so a narrow y
            // must still take the wide path — low pair stores the low
            // byte, high pair subtracts y's (absent) high byte as
            // immediate 0 with the borrow and stores the high byte.
            // The wide result resolves to x first, so x allocates
            // $80/$81 and y takes $82.
            uint zeroOffset = _stringPool.Intern("0");
            uint yOffset = _stringPool.Intern("y");
            uint xOffset = _stringPool.Intern("x");
            uint tmpOffset = _stringPool.Intern("__tmp_0");

            var builder = new InstListBuilder();
            builder.Add((byte)LlirInstructionKind.Sub, InstructionFlag.WideResult, 0, zeroOffset, yOffset);
            builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, tmpOffset, 0);
            builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, xOffset, tmpOffset);
            var mlir = builder.Build();

            var result = _transformer.TransformSlab(mlir, _stringPool);

            // Low pair, high pair; the two Assigns are consumed by the
            // wide result storing straight into x.
            Assert.That(result.Count, Is.EqualTo(6));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.GetKind(0), Is.EqualTo((byte)LlirInstructionKind.Load));
                Assert.That(result.GetOperands(0)[0], Is.Zero, "Low pair loads immediate 0");

                Assert.That(result.GetKind(1), Is.EqualTo((byte)LlirInstructionKind.Sub));
                var lowSubOps = result.GetOperands(1);
                Assert.That(lowSubOps.Length, Is.EqualTo(2));
                Assert.That(lowSubOps[0], Is.EqualTo(0x82u), "Low Sub subtracts y's zero-page address");
                Assert.That((InstructionFlag)result.GetFlags(1), Is.EqualTo(InstructionFlag.None),
                    "Low Sub sets the borrow itself (SEC)");

                Assert.That(result.GetKind(2), Is.EqualTo((byte)LlirInstructionKind.Store));
                Assert.That(result.GetOperands(2)[0], Is.EqualTo(0x80u), "Low byte stores to x's first slot");

                Assert.That(result.GetKind(3), Is.EqualTo((byte)LlirInstructionKind.Load));
                Assert.That(result.GetOperands(3)[0], Is.Zero, "High pair loads immediate 0");

                Assert.That(result.GetKind(4), Is.EqualTo((byte)LlirInstructionKind.Sub));
                var highSubOps = result.GetOperands(4);
                Assert.That(highSubOps.Length, Is.EqualTo(1));
                Assert.That(highSubOps[0], Is.Zero, "High Sub subtracts immediate 0 (narrow y has no high byte)");
                Assert.That((InstructionFlag)result.GetFlags(4), Is.EqualTo(InstructionFlag.CarryIn),
                    "High Sub consumes the low pair's borrow (no SEC)");

                Assert.That(result.GetKind(5), Is.EqualTo((byte)LlirInstructionKind.Store));
                Assert.That(result.GetOperands(5)[0], Is.EqualTo(0x81u), "High byte stores to x's second slot");
            }
        }
        #endregion

        #region Wide Comparison Tests (PR #22 fix round 13)

        [Test]
        public void Transform_WideEqualComparesBothBytes()
        {
            // Regression: x := 256; if x = 0 then y := 1 must NOT take
            // the then-arm — 256 and 0 share a low byte, so a single-byte
            // Cmp decides the branch on the wrong comparison. A wide
            // comparison emits a low pair and a high pair, each branching
            // to the skip label when its byte differs.
            // MLIR: Assign(x, 256); Cmp(x, 0); Branch(L_end); Assign(y, 1); Label(L_end).
            // x allocates $80/$81 (wide), y takes $82.
            uint xOffset = _stringPool.Intern("x");
            uint wideOffset = _stringPool.Intern("256");
            uint zeroOffset = _stringPool.Intern("0");
            uint yOffset = _stringPool.Intern("y");
            uint oneOffset = _stringPool.Intern("1");
            uint endOffset = _stringPool.Intern("L_end");

            var mlir = BuildMlirSlab(
                ((byte)MlirInstructionKind.Assign, new uint[] { xOffset, wideOffset }),
                ((byte)LlirInstructionKind.Cmp, new uint[] { xOffset, zeroOffset }),
                ((byte)MlirInstructionKind.Branch, new uint[] { endOffset }),
                ((byte)MlirInstructionKind.Assign, new uint[] { yOffset, oneOffset }),
                ((byte)MlirInstructionKind.Label, new uint[] { endOffset }));

            var result = _transformer.TransformSlab(mlir, _stringPool);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Count, Is.EqualTo(10));

                Assert.That(result.GetKind(0), Is.EqualTo((byte)LlirInstructionKind.Assign));
                Assert.That(result.GetOperands(0)[0], Is.EqualTo(0x80u), "x low byte at $80");
                Assert.That(result.GetOperands(0)[1], Is.Zero, "256 low byte is 0");
                Assert.That(result.GetKind(1), Is.EqualTo((byte)LlirInstructionKind.Assign));
                Assert.That(result.GetOperands(1)[0], Is.EqualTo(0x81u), "x high byte at $81");
                Assert.That(result.GetOperands(1)[1], Is.EqualTo(1u), "256 high byte is 1");

                Assert.That(result.GetKind(2), Is.EqualTo((byte)LlirInstructionKind.Load));
                Assert.That(result.GetOperands(2)[0], Is.EqualTo(0x80u), "Low pair loads x low byte");
                Assert.That(result.GetKind(3), Is.EqualTo((byte)LlirInstructionKind.Cmp));
                Assert.That(result.GetOperands(3)[0], Is.Zero, "Low pair compares against 0");
                Assert.That(result.GetKind(4), Is.EqualTo((byte)LlirInstructionKind.Branch));
                Assert.That(result.GetOperands(4)[0], Is.EqualTo(endOffset), "Low pair skips to L_end when the low byte differs");

                Assert.That(result.GetKind(5), Is.EqualTo((byte)LlirInstructionKind.Load));
                Assert.That(result.GetOperands(5)[0], Is.EqualTo(0x81u), "High pair loads x high byte");
                Assert.That(result.GetKind(6), Is.EqualTo((byte)LlirInstructionKind.Cmp));
                Assert.That(result.GetOperands(6)[0], Is.Zero, "High pair compares against 0");
                Assert.That(result.GetKind(7), Is.EqualTo((byte)LlirInstructionKind.Branch));
                Assert.That(result.GetOperands(7)[0], Is.EqualTo(endOffset), "High pair skips to L_end when the high byte differs");

                Assert.That(result.GetKind(8), Is.EqualTo((byte)LlirInstructionKind.Assign));
                Assert.That(result.GetOperands(8)[0], Is.EqualTo(0x82u), "Then-arm stores y at $82");
                Assert.That(result.GetKind(9), Is.EqualTo((byte)LlirInstructionKind.Label));
                Assert.That(result.GetOperands(9)[0], Is.EqualTo(endOffset), "End label closes the if");
            }
        }

        [Test]
        public void Transform_WideCaseLabelComparesBothBytes()
        {
            // Regression: a case selector of 251 must NOT match a -5 label
            // (65531) — they share the low byte 251, so a single-byte Cmp
            // dispatches to the wrong arm. The label is a wide literal, so
            // the comparison takes the wide path even though the selector
            // itself is one slot; its absent high byte compares as 0.
            // MLIR: Assign(x, 251); Cmp(x, 65531); Branch(L_skip); Assign(y, 1); Label(L_skip).
            uint xOffset = _stringPool.Intern("x");
            uint selectorOffset = _stringPool.Intern("251");
            uint labelOffset = _stringPool.Intern("65531");
            uint yOffset = _stringPool.Intern("y");
            uint oneOffset = _stringPool.Intern("1");
            uint skipOffset = _stringPool.Intern("L_skip");

            var mlir = BuildMlirSlab(
                ((byte)MlirInstructionKind.Assign, new uint[] { xOffset, selectorOffset }),
                ((byte)LlirInstructionKind.Cmp, new uint[] { xOffset, labelOffset }),
                ((byte)MlirInstructionKind.Branch, new uint[] { skipOffset }),
                ((byte)MlirInstructionKind.Assign, new uint[] { yOffset, oneOffset }),
                ((byte)MlirInstructionKind.Label, new uint[] { skipOffset }));

            var result = _transformer.TransformSlab(mlir, _stringPool);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Count, Is.EqualTo(9));

                Assert.That(result.GetKind(0), Is.EqualTo((byte)LlirInstructionKind.Assign));
                Assert.That(result.GetOperands(0)[0], Is.EqualTo(0x80u), "x at $80");
                Assert.That(result.GetOperands(0)[1], Is.EqualTo(251u));

                Assert.That(result.GetKind(1), Is.EqualTo((byte)LlirInstructionKind.Load));
                Assert.That(result.GetOperands(1)[0], Is.EqualTo(0x80u), "Low pair loads the selector");
                Assert.That(result.GetKind(2), Is.EqualTo((byte)LlirInstructionKind.Cmp));
                Assert.That(result.GetOperands(2)[0], Is.EqualTo(251u), "Low pair compares against the label low byte 251");
                Assert.That(result.GetKind(3), Is.EqualTo((byte)LlirInstructionKind.Branch));
                Assert.That(result.GetOperands(3)[0], Is.EqualTo(skipOffset), "Low pair skips when the low byte differs");

                Assert.That(result.GetKind(4), Is.EqualTo((byte)LlirInstructionKind.Load));
                Assert.That(result.GetOperands(4)[0], Is.Zero, "A one-slot selector has no high byte: immediate 0");
                Assert.That(result.GetKind(5), Is.EqualTo((byte)LlirInstructionKind.Cmp));
                Assert.That(result.GetOperands(5)[0], Is.EqualTo(255u), "High pair compares against the label high byte 255");
                Assert.That(result.GetKind(6), Is.EqualTo((byte)LlirInstructionKind.Branch));
                Assert.That(result.GetOperands(6)[0], Is.EqualTo(skipOffset), "High pair skips when the high byte differs");

                Assert.That(result.GetKind(7), Is.EqualTo((byte)LlirInstructionKind.Assign));
                Assert.That(result.GetOperands(7)[0], Is.EqualTo(0x81u), "Arm stores y at $81");
                Assert.That(result.GetKind(8), Is.EqualTo((byte)LlirInstructionKind.Label));
                Assert.That(result.GetOperands(8)[0], Is.EqualTo(skipOffset), "Skip label closes the arm");
            }
        }

        [Test]
        public void Transform_WideLessThanComparesHighByteFirst()
        {
            // Regression: if x < 1 with x := 256 must NOT take the then-arm.
            // Unsigned less-than decides on the carry after the compare, so
            // the high bytes compare first: when they differ the carry from
            // the high compare decides, and the low bytes compare only when
            // the high bytes are equal. The intermediate branch jumps over
            // the low compare to the carry decision.
            // MLIR: Assign(x, 256); Cmp(x, 1); Bcs; Branch(L_end); Assign(y, 1); Label(L_end).
            uint xOffset = _stringPool.Intern("x");
            uint wideOffset = _stringPool.Intern("256");
            uint oneOffset = _stringPool.Intern("1");
            uint yOffset = _stringPool.Intern("y");
            uint endOffset = _stringPool.Intern("L_end");

            var mlir = BuildMlirSlab(
                ((byte)MlirInstructionKind.Assign, new uint[] { xOffset, wideOffset }),
                ((byte)LlirInstructionKind.Cmp, new uint[] { xOffset, oneOffset }),
                ((byte)LlirInstructionKind.Bcs, Array.Empty<uint>()),
                ((byte)MlirInstructionKind.Branch, new uint[] { endOffset }),
                ((byte)MlirInstructionKind.Assign, new uint[] { yOffset, oneOffset }),
                ((byte)MlirInstructionKind.Label, new uint[] { endOffset }));

            var result = _transformer.TransformSlab(mlir, _stringPool);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Count, Is.EqualTo(14));

                Assert.That(result.GetKind(0), Is.EqualTo((byte)LlirInstructionKind.Assign));
                Assert.That(result.GetKind(1), Is.EqualTo((byte)LlirInstructionKind.Assign));

                Assert.That(result.GetKind(2), Is.EqualTo((byte)LlirInstructionKind.Load));
                Assert.That(result.GetOperands(2)[0], Is.EqualTo(0x81u), "High bytes compare first: load x high byte");
                Assert.That(result.GetKind(3), Is.EqualTo((byte)LlirInstructionKind.Cmp));
                Assert.That(result.GetOperands(3)[0], Is.Zero, "High pair compares against 1 >> 8 = 0");
                Assert.That(result.GetKind(4), Is.EqualTo((byte)LlirInstructionKind.Branch));
                uint freshOffset = result.GetOperands(4)[0];
                Assert.That(_stringPool.Resolve(freshOffset), Does.StartWith("L_widecmp_"),
                    "The intermediate branch jumps over the low compare to a fresh label");

                Assert.That(result.GetKind(5), Is.EqualTo((byte)LlirInstructionKind.Load));
                Assert.That(result.GetOperands(5)[0], Is.EqualTo(0x80u), "Low pair loads x low byte when the high bytes are equal");
                Assert.That(result.GetKind(6), Is.EqualTo((byte)LlirInstructionKind.Cmp));
                Assert.That(result.GetOperands(6)[0], Is.EqualTo(1u), "Low pair compares against 1");
                Assert.That(result.GetKind(7), Is.EqualTo((byte)LlirInstructionKind.Bcs));
                Assert.That(result.GetKind(8), Is.EqualTo((byte)LlirInstructionKind.Branch));
                Assert.That(result.GetOperands(8)[0], Is.EqualTo(endOffset), "Carry-set branch skips when the low compare says left >= right");

                Assert.That(result.GetKind(9), Is.EqualTo((byte)LlirInstructionKind.Label));
                Assert.That(result.GetOperands(9)[0], Is.EqualTo(freshOffset), "The fresh label lands after the first carry branch");
                Assert.That(result.GetKind(10), Is.EqualTo((byte)LlirInstructionKind.Bcs));
                Assert.That(result.GetKind(11), Is.EqualTo((byte)LlirInstructionKind.Branch));
                Assert.That(result.GetOperands(11)[0], Is.EqualTo(endOffset), "Second carry branch decides on the high compare's carry");

                Assert.That(result.GetKind(12), Is.EqualTo((byte)LlirInstructionKind.Assign));
                Assert.That(result.GetOperands(12)[0], Is.EqualTo(0x82u), "Then-arm stores y at $82");
                Assert.That(result.GetKind(13), Is.EqualTo((byte)LlirInstructionKind.Label));
            }
        }

        [Test]
        public void Transform_WideNotEqualComparesBothBytes()
        {
            // Regression: if x <> 0 with x := 256 must take the then-arm —
            // the values differ, so the skip branch (BEQ, fire when equal)
            // must not fire. Equality needs both bytes: when the low bytes
            // differ the branch is skipped outright, and the high compare
            // decides only when the low bytes are equal.
            // MLIR: Assign(x, 256); Cmp(x, 0); Transition; Branch(L_end); Assign(y, 1); Label(L_end).
            uint xOffset = _stringPool.Intern("x");
            uint wideOffset = _stringPool.Intern("256");
            uint zeroOffset = _stringPool.Intern("0");
            uint yOffset = _stringPool.Intern("y");
            uint oneOffset = _stringPool.Intern("1");
            uint endOffset = _stringPool.Intern("L_end");

            var mlir = BuildMlirSlab(
                ((byte)MlirInstructionKind.Assign, new uint[] { xOffset, wideOffset }),
                ((byte)LlirInstructionKind.Cmp, new uint[] { xOffset, zeroOffset }),
                ((byte)LlirInstructionKind.Transition, Array.Empty<uint>()),
                ((byte)MlirInstructionKind.Branch, new uint[] { endOffset }),
                ((byte)MlirInstructionKind.Assign, new uint[] { yOffset, oneOffset }),
                ((byte)MlirInstructionKind.Label, new uint[] { endOffset }));

            var result = _transformer.TransformSlab(mlir, _stringPool);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Count, Is.EqualTo(12));

                Assert.That(result.GetKind(0), Is.EqualTo((byte)LlirInstructionKind.Assign));
                Assert.That(result.GetKind(1), Is.EqualTo((byte)LlirInstructionKind.Assign));

                Assert.That(result.GetKind(2), Is.EqualTo((byte)LlirInstructionKind.Load));
                Assert.That(result.GetOperands(2)[0], Is.EqualTo(0x80u), "Low pair loads x low byte");
                Assert.That(result.GetKind(3), Is.EqualTo((byte)LlirInstructionKind.Cmp));
                Assert.That(result.GetOperands(3)[0], Is.Zero, "Low pair compares against 0");
                Assert.That(result.GetKind(4), Is.EqualTo((byte)LlirInstructionKind.Branch));
                uint freshOffset = result.GetOperands(4)[0];
                Assert.That(_stringPool.Resolve(freshOffset), Does.StartWith("L_widecmp_"),
                    "The intermediate branch jumps over the equal-branch when the low bytes differ");

                Assert.That(result.GetKind(5), Is.EqualTo((byte)LlirInstructionKind.Load));
                Assert.That(result.GetOperands(5)[0], Is.EqualTo(0x81u), "High pair loads x high byte");
                Assert.That(result.GetKind(6), Is.EqualTo((byte)LlirInstructionKind.Cmp));
                Assert.That(result.GetOperands(6)[0], Is.Zero, "High pair compares against 0");
                Assert.That(result.GetKind(7), Is.EqualTo((byte)LlirInstructionKind.Transition));
                Assert.That(result.GetKind(8), Is.EqualTo((byte)LlirInstructionKind.Branch));
                Assert.That(result.GetOperands(8)[0], Is.EqualTo(endOffset), "Equal-branch skips when both bytes are equal");

                Assert.That(result.GetKind(9), Is.EqualTo((byte)LlirInstructionKind.Label));
                Assert.That(result.GetOperands(9)[0], Is.EqualTo(freshOffset), "The fresh label lands after the equal-branch");
                Assert.That(result.GetKind(10), Is.EqualTo((byte)LlirInstructionKind.Assign));
                Assert.That(result.GetOperands(10)[0], Is.EqualTo(0x82u), "Then-arm stores y at $82");
                Assert.That(result.GetKind(11), Is.EqualTo((byte)LlirInstructionKind.Label));
            }
        }

        [Test]
        public void Transform_WideNotEqual_InputLabelCollidesWithGeneratedName_GeneratedLabelIsDistinct()
        {
            // Regression: the wide <> lowering invents a fall-through
            // label whose first candidate name is L_widecmp_0 (a fresh
            // transformer starts its counter at 0). When the input slab
            // already defines a label with that exact name, interning the
            // generated name returns the input label's pool offset, so
            // the generated Label would share the input label's identity
            // and the code generator's address table would keep only one
            // of the two targets. The generated name must skip every name
            // the input slab already uses.
            // MLIR: Assign(x, 256); Cmp(x, 0); Transition; Branch(L_end); Assign(y, 1); Label(L_end); Label(L_widecmp_0).
            uint xOffset = _stringPool.Intern("x");
            uint wideOffset = _stringPool.Intern("256");
            uint zeroOffset = _stringPool.Intern("0");
            uint yOffset = _stringPool.Intern("y");
            uint oneOffset = _stringPool.Intern("1");
            uint endOffset = _stringPool.Intern("L_end");
            uint inputLabelOffset = _stringPool.Intern("L_widecmp_0");

            var mlir = BuildMlirSlab(
                ((byte)MlirInstructionKind.Assign, new uint[] { xOffset, wideOffset }),
                ((byte)LlirInstructionKind.Cmp, new uint[] { xOffset, zeroOffset }),
                ((byte)LlirInstructionKind.Transition, Array.Empty<uint>()),
                ((byte)MlirInstructionKind.Branch, new uint[] { endOffset }),
                ((byte)MlirInstructionKind.Assign, new uint[] { yOffset, oneOffset }),
                ((byte)MlirInstructionKind.Label, new uint[] { endOffset }),
                ((byte)MlirInstructionKind.Label, new uint[] { inputLabelOffset }));

            var result = _transformer.TransformSlab(mlir, _stringPool);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Count, Is.EqualTo(13));

                Assert.That(result.GetKind(4), Is.EqualTo((byte)LlirInstructionKind.Branch));
                uint freshOffset = result.GetOperands(4)[0];
                Assert.That(freshOffset, Is.Not.EqualTo(inputLabelOffset),
                    "The generated fall-through label must not reuse the input label's offset");
                Assert.That(_stringPool.Resolve(freshOffset), Does.StartWith("L_widecmp_"),
                    "The generated label keeps the L_widecmp_ prefix");
                Assert.That(_stringPool.Resolve(freshOffset), Is.Not.EqualTo("L_widecmp_0"),
                    "The generated label must not reuse the input label's name");

                Assert.That(result.GetKind(9), Is.EqualTo((byte)LlirInstructionKind.Label));
                Assert.That(result.GetOperands(9)[0], Is.EqualTo(freshOffset), "The fresh label lands after the equal-branch");

                Assert.That(result.GetKind(12), Is.EqualTo((byte)LlirInstructionKind.Label));
                Assert.That(result.GetOperands(12)[0], Is.EqualTo(inputLabelOffset),
                    "The input label is preserved with its own offset");
            }
        }

        #endregion

        #region Fail-Loud Tests (Slice 1, L1)

        [Test]
        public void Transform_MlirOutput_ThrowsGameVmException()
        {
            // Console output has no lowering on this target and arrives as a
            // dedicated Output instruction; the transformer must fail loudly,
            // never silently drop it.
            var mlir = BuildMlirSlab(((byte)MlirInstructionKind.Output, new uint[] { 1 }));

            var ex = Assert.Throws<GameVmException>(() => _transformer.TransformSlab(mlir, _stringPool));
            Assert.That(ex!.ErrorCode, Is.EqualTo(ErrorCode.UnhandledInstruction));
            Assert.That(ex.Message, Does.Contain("'Output'"));
            Assert.That(ex.Message, Does.Contain("not supported on the Atari 2600 target"));
            Assert.That(ex.Context["SlabIndex"], Is.EqualTo(0));
        }

        [Test]
        public void Transform_MlirNop_IsElided()
        {
            // A benign no-op (empty statement/block) lowers to nothing:
            // zero LLIR instructions, zero ROM bytes.
            var mlir = BuildMlirSlab(((byte)MlirInstructionKind.Nop, Array.Empty<uint>()));

            var result = _transformer.TransformSlab(mlir, _stringPool);

            Assert.That(result.Count, Is.EqualTo(0));
        }

        [Test]
        public void Transform_UnmappedKind_ThrowsGameVmException()
        {
            var mlir = BuildMlirSlab(((byte)MlirInstructionKind.Variable, new uint[] { 1, 2 }));

            var ex = Assert.Throws<GameVmException>(() => _transformer.TransformSlab(mlir, _stringPool));
            Assert.That(ex!.ErrorCode, Is.EqualTo(ErrorCode.UnhandledInstruction));
            Assert.That(ex.Message, Does.Contain("0x08"));
            Assert.That(ex.Context["SlabIndex"], Is.EqualTo(0));
        }

        #endregion

        #region Fail-Loud Tests (Slice 1, L3)

        [Test]
        public void Transform_127thVariable_ThrowsZeroPageExhausted()
        {
            // The 2600 has 128 bytes of RAM ($80-$FF) shared between
            // variables and the 6502 stack; $FE-$FF are reserved for the
            // stack (the outermost JSR return address lives there), so
            // $80-$FD hold variables and the 127th variable must fail
            // loudly instead of wrapping into stack space.
            var builder = new InstListBuilder();
            uint value = _stringPool.Intern("1");
            for (int v = 0; v < 129; v++)
            {
                uint target = _stringPool.Intern("var" + v);
                builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, target, value);
            }
            var mlir = builder.Build();

            var ex = Assert.Throws<GameVmException>(() => _transformer.TransformSlab(mlir, _stringPool));
            Assert.That(ex!.ErrorCode, Is.EqualTo(ErrorCode.ZeroPageExhausted));
            Assert.That(ex.Message, Does.Contain("var126"));
        }

        #endregion

        #region Fail-Loud Tests (CodeRabbit review, PR #19)

        [Test]
        public void Transform_MultiplyWithFewerThanTwoOperands_ThrowsGameVmException()
        {
            // A Multiply with a single operand is malformed: the fallback
            // below would emit an underspecified LLIR Multiply that code
            // generation cannot emit, silently truncating the ROM at that
            // instruction.
            uint xOffset = _stringPool.Intern("x");

            var mlir = BuildMlirSlab(((byte)MlirInstructionKind.Multiply, new uint[] { xOffset }));

            var ex = Assert.Throws<GameVmException>(() => _transformer.TransformSlab(mlir, _stringPool));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(ex!.ErrorCode, Is.EqualTo(ErrorCode.UnhandledInstruction));
                Assert.That(ex.Message, Does.Contain("Multiply"));
                Assert.That(ex.Context["SlabIndex"], Is.Zero);
            }
        }

        [Test]
        public void Transform_NestedMultiply_MaterializesInnerProductBeforeOuterMultiply()
        {
            // MLIR for x := (a * b) * c, as the HLIR lowering emits it: the
            // inner product is assigned to __tmp_0, then the outer multiply
            // reads it. The inner product lives in the accumulator after the
            // first Multiply, so the Assign to __tmp_0 must store it to
            // memory before the outer multiply loads it — otherwise the load
            // reads an unwritten address (zero after the reset clear) and
            // the product comes out 0 instead of 42.
            uint aOffset = _stringPool.Intern("a");
            uint bOffset = _stringPool.Intern("b");
            uint cOffset = _stringPool.Intern("c");
            uint xOffset = _stringPool.Intern("x");
            uint tmp0Offset = _stringPool.Intern("__tmp_0");
            uint tmp1Offset = _stringPool.Intern("__tmp_1");

            var mlir = BuildMlirSlab(
                ((byte)MlirInstructionKind.Multiply, new uint[] { aOffset, bOffset }),
                ((byte)MlirInstructionKind.Assign, new uint[] { tmp0Offset, 0 }),
                ((byte)MlirInstructionKind.Multiply, new uint[] { tmp0Offset, cOffset }),
                ((byte)MlirInstructionKind.Assign, new uint[] { tmp1Offset, 0 }),
                ((byte)MlirInstructionKind.Assign, new uint[] { xOffset, tmp1Offset }));
            var result = _transformer.TransformSlab(mlir, _stringPool);

            // Load a, Multiply b, Store __tmp_0, Load __tmp_0, Multiply c,
            // Store x. The second temp is consumed directly from the
            // accumulator by the final Store, so it needs no materializing
            // store of its own.
            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Count, Is.EqualTo(6));
                Assert.That(result.GetKind(0), Is.EqualTo((byte)LlirInstructionKind.Load));
                Assert.That(result.GetKind(1), Is.EqualTo((byte)LlirInstructionKind.Multiply));
                Assert.That(result.GetKind(2), Is.EqualTo((byte)LlirInstructionKind.Store));
                Assert.That(result.GetKind(3), Is.EqualTo((byte)LlirInstructionKind.Load));
                Assert.That(result.GetKind(4), Is.EqualTo((byte)LlirInstructionKind.Multiply));
                Assert.That(result.GetKind(5), Is.EqualTo((byte)LlirInstructionKind.Store));

                uint tmp0Addr = result.GetOperands(2)[0];
                Assert.That(tmp0Addr, Is.GreaterThanOrEqualTo(0x80u), "__tmp_0 must have a zero-page address");
                Assert.That(result.GetOperands(3)[0], Is.EqualTo(tmp0Addr),
                    "the outer multiply must load the inner product from the address the materializing store wrote");
            }
        }

        [Test]
        public void Transform_MultiplyScratchDoesNotCollideWithUserVariableNamedMulM()
        {
            // A user variable literally named "__mul_m" must not share its
            // address with the multiply scratch: the shift-and-add routine
            // writes the left operand into the scratch, so sharing it
            // would overwrite the user's variable and square the product
            // (the right-operand load would read the left operand back).
            uint mulMOffset = _stringPool.Intern("__mul_m");
            uint aOffset = _stringPool.Intern("a");
            uint xOffset = _stringPool.Intern("x");
            uint tmp0Offset = _stringPool.Intern("__tmp_0");
            uint sevenOffset = _stringPool.Intern("7");
            uint threeOffset = _stringPool.Intern("3");

            var mlir = BuildMlirSlab(
                ((byte)MlirInstructionKind.Assign, new uint[] { mulMOffset, sevenOffset }),
                ((byte)MlirInstructionKind.Assign, new uint[] { aOffset, threeOffset }),
                ((byte)MlirInstructionKind.Multiply, new uint[] { aOffset, mulMOffset }),
                ((byte)MlirInstructionKind.Assign, new uint[] { xOffset, tmp0Offset }));
            var result = _transformer.TransformSlab(mlir, _stringPool);

            // Assign __mul_m, Assign a, Load a, Multiply, Store x.
            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Count, Is.EqualTo(5));
                Assert.That(result.GetKind(3), Is.EqualTo((byte)LlirInstructionKind.Multiply));
                uint userMulMAddr = result.GetOperands(0)[0];
                ReadOnlySpan<uint> multiplyOps = result.GetOperands(3);
                Assert.That(multiplyOps.Length, Is.EqualTo(4));
                Assert.That(multiplyOps[0], Is.EqualTo(userMulMAddr),
                    "the multiply must read the user's __mul_m variable");
                Assert.That(multiplyOps[2], Is.Not.EqualTo(userMulMAddr),
                    "scratch __mul_m must not share the user's __mul_m address");
                Assert.That(multiplyOps[3], Is.Not.EqualTo(userMulMAddr),
                    "scratch __mul_n must not share the user's __mul_m address");
            }
        }

        [Test]
        public void Transform_MultiplyAfter125Variables_ThrowsZeroPageExhausted()
        {
            // 125 variables fill $80-$FC; the multiply scratch would need
            // $FD and $FE, but $FE-$FF are reserved for the 6502 stack
            // (the outermost JSR return address lives there — zero page
            // and stack share the 2600's 128 bytes of RAM). Fail loudly
            // instead of letting the shift-and-add routine overwrite the
            // return address.
            var builder = new InstListBuilder();
            uint value = _stringPool.Intern("1");
            for (int v = 0; v < 125; v++)
            {
                uint target = _stringPool.Intern("var" + v);
                builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, target, value);
            }
            uint aOffset = _stringPool.Intern("var0");
            uint bOffset = _stringPool.Intern("var1");
            builder.Add((byte)MlirInstructionKind.Multiply, InstructionFlag.None, 0, aOffset, bOffset);
            var mlir = builder.Build();

            var ex = Assert.Throws<GameVmException>(() => _transformer.TransformSlab(mlir, _stringPool));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(ex!.ErrorCode, Is.EqualTo(ErrorCode.ZeroPageExhausted));
                Assert.That(ex.Message, Does.Contain("__mul_n"),
                    "125 variables leave $FD for __mul_m; __mul_n is the allocation that hits the reserved $FE");
            }
        }

        [Test]
        public void Transform_MultiplyAfter124Variables_SucceedsWithScratchBelowStack()
        {
            // Boundary: 124 variables fill $80-$FB, so the scratch lands
            // at $FC/$FD — the highest addresses below the reserved
            // $FE-$FF stack bytes. This must still compile.
            var builder = new InstListBuilder();
            uint value = _stringPool.Intern("1");
            for (int v = 0; v < 124; v++)
            {
                uint target = _stringPool.Intern("var" + v);
                builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 0, target, value);
            }
            uint aOffset = _stringPool.Intern("var0");
            uint bOffset = _stringPool.Intern("var1");
            builder.Add((byte)MlirInstructionKind.Multiply, InstructionFlag.None, 0, aOffset, bOffset);
            var mlir = builder.Build();

            var result = _transformer.TransformSlab(mlir, _stringPool);

            // 124 Assigns, then Load var0, then Multiply.
            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Count, Is.EqualTo(126));
                Assert.That(result.GetKind(125), Is.EqualTo((byte)LlirInstructionKind.Multiply));
                ReadOnlySpan<uint> multiplyOps = result.GetOperands(125);
                Assert.That(multiplyOps[2], Is.EqualTo(0xFCu), "scratch __mul_m must land at $FC");
                Assert.That(multiplyOps[3], Is.EqualTo(0xFDu), "scratch __mul_n must land at $FD");
            }
        }

        #endregion
}