using System;
using System.Collections.Generic;
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
        // MLIR for: Sub(x, 1); Assign(__tmp_0, 0); Assign(x, __tmp_0)
        // should lower to: Load(xAddr); Sub(1); Store(xAddr)
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

        // Should emit: Load(xAddr, 0); Sub(1); Store(xAddr)
        Assert.That(result.Count, Is.EqualTo(3),
            "Sub(x,1); Assign(tmp,0); Assign(x,tmp) should lower to Load; Sub; Store");

        // Check Load(xAddr, 0) - two operands for zp address
        Assert.That(result.GetKind(0), Is.EqualTo((byte)LlirInstructionKind.Load));
        var loadOps = result.GetOperands(0);
        Assert.That(loadOps.Length, Is.EqualTo(2), "Load should have [addrLow, addrHigh] operands");
        Assert.That(loadOps[0], Is.EqualTo(0x80u), "Load address low byte = xAddr");
        Assert.That(loadOps[1], Is.EqualTo(0x00u), "Load address high byte = 0 (zp)");

        // Check Sub(1) - single operand for immediate
        Assert.That(result.GetKind(1), Is.EqualTo((byte)LlirInstructionKind.Sub));
        var subOps = result.GetOperands(1);
        Assert.That(subOps.Length, Is.EqualTo(1), "Sub should have single immediate operand");
        Assert.That(subOps[0], Is.EqualTo(1u), "Sub immediate = 1");

        // Check Store(xAddr) - single operand for zp target
        Assert.That(result.GetKind(2), Is.EqualTo((byte)LlirInstructionKind.Store));
        var storeOps = result.GetOperands(2);
        Assert.That(storeOps.Length, Is.EqualTo(1), "Store should have single zp target");
        Assert.That(storeOps[0], Is.EqualTo(0x80u), "Store target = xAddr");
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
        public void Transform_UnaryMinusSequence_EmitsLoadZeroSubStore()
        {
            // MLIR for x := -y as produced by the HLIR UnaryOp lowering:
            //   Sub("0", y); Assign(__tmp_0, 0); Assign(x, __tmp_0)
            // Lowers to: Load #0 (LDA #0), Sub(yAddr), Store(xAddr).
            // y is allocated first ($80), x second ($81).
            uint zeroOffset = _stringPool.Intern("0");
            uint yOffset = _stringPool.Intern("y");
            uint xOffset = _stringPool.Intern("x");
            uint tmpOffset = _stringPool.Intern("__tmp_0");

            var mlir = BuildMlirSlab(
                ((byte)LlirInstructionKind.Sub, new uint[] { zeroOffset, yOffset }),
                ((byte)MlirInstructionKind.Assign, new uint[] { tmpOffset, 0 }),
                ((byte)MlirInstructionKind.Assign, new uint[] { xOffset, tmpOffset }));

            var result = _transformer.TransformSlab(mlir, _stringPool);

            Assert.That(result.Count, Is.EqualTo(3));

            Assert.That(result.GetKind(0), Is.EqualTo((byte)LlirInstructionKind.Load));
            var loadOps = result.GetOperands(0);
            Assert.That(loadOps.Length, Is.EqualTo(1));
            Assert.That(loadOps[0], Is.EqualTo(0u), "Load immediate 0");

            Assert.That(result.GetKind(1), Is.EqualTo((byte)LlirInstructionKind.Sub));
            var subOps = result.GetOperands(1);
            Assert.That(subOps.Length, Is.EqualTo(1));
            Assert.That(subOps[0], Is.EqualTo(0x80u), "Sub operand is y's zero-page address");

            Assert.That(result.GetKind(2), Is.EqualTo((byte)LlirInstructionKind.Store));
            var storeOps = result.GetOperands(2);
            Assert.That(storeOps.Length, Is.EqualTo(1));
            Assert.That(storeOps[0], Is.EqualTo(0x81u), "Store targets x's zero-page address");
        }
        #endregion
}