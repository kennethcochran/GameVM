using GameVM.Compiler.Core.IR;
using GameVM.Compiler.Core.IR.Interfaces;
using GameVM.Compiler.Core.IR.Slab;
using GameVM.Compiler.Core.IR.Buffers;
using GameVM.Compiler.Core.IR.Soa;
using GameVM.Compiler.Core.Enums;
using GameVM.Compiler.Core.Exceptions;

using NUnit.Framework;

namespace GameVM.Compiler.Backend.Atari2600.Tests;

[TestFixture]
public class Atari2600CodeGeneratorTests
{
    private Atari2600CodeGenerator _codeGenerator;

    // Length of the reset prologue emitted at the start of every ROM.
    // Pinned by GenerateFromSlab_EmitsResetPrologue below; if the prologue
    // changes, update this constant and the shifted assertions with it.
    private const int PrologueSize = 13;

    [SetUp]
    public void Setup()
    {
        _codeGenerator = new Atari2600CodeGenerator();
    }

    private static InstList CreateEmptyInstList()
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

    [Test]
    public void GenerateFromSlab_WithNullSlab_ReturnsEmptyArray()
    {
        // Arrange
        var options = new CodeGenOptions();

        // Act
        var result = _codeGenerator.GenerateFromSlab(CreateEmptyInstList(), new StringPool(), options);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void GenerateFromSlab_WithEmptySlab_ReturnsEmptyArray()
    {
        var slab = CreateEmptyInstList();
        var stringPool = new StringPool();
        var rom = _codeGenerator.GenerateFromSlab(slab, stringPool, new CodeGenOptions());
        Assert.That(rom, Is.Empty);
    }

    [Test]
    public void GenerateFromSlab_WithValidSlab_ReturnsRomSize()
    {
        var builder = new InstListBuilder();
        builder.Add((byte)LlirInstructionKind.Load, InstructionFlag.None, 0, 0x42); // LDA #$42
        var slab = builder.Build();
        var stringPool = new StringPool();
        
        var rom = _codeGenerator.GenerateFromSlab(slab, stringPool, new CodeGenOptions());
        
        Assert.That(rom.Length, Is.EqualTo(4096)); // 4K ROM
    }

    [Test]
    public void GenerateFromSlab_WithLoadStoreInstructions_GeneratesCorrectOpcodes()
    {
        var builder = new InstListBuilder();
        builder.Add((byte)LlirInstructionKind.Load, InstructionFlag.None, 0, 0x05); // LDA #$05
        builder.Add((byte)LlirInstructionKind.Store, InstructionFlag.None, 0, 0x10); // STA $10 (1-operand [addr], the transformer's shape)
        var slab = builder.Build();
        var stringPool = new StringPool();
        
        var rom = _codeGenerator.GenerateFromSlab(slab, stringPool, new CodeGenOptions());
        
        Assert.That(rom[PrologueSize + 0], Is.EqualTo(0xA9)); // LDA #immediate
        Assert.That(rom[PrologueSize + 1], Is.EqualTo(0x05)); // immediate value 5
        Assert.That(rom[PrologueSize + 2], Is.EqualTo(0x85)); // STA zero-page
        Assert.That(rom[PrologueSize + 3], Is.EqualTo(0x10)); // address $10
    }

    [Test]
    public void GenerateFromSlab_EmitsResetPrologue()
    {
        // Every ROM starts with the reset prologue at $F000:
        // SEI; CLD; LDX #$FF; TXS; LDX #$00; TXA; STA $80,X; INX; BNE -5
        var builder = new InstListBuilder();
        builder.Add((byte)LlirInstructionKind.Load, InstructionFlag.None, 0, 0x42); // LDA #$42
        var slab = builder.Build();
        var stringPool = new StringPool();

        var rom = _codeGenerator.GenerateFromSlab(slab, stringPool, new CodeGenOptions());

        var expected = new byte[]
        {
            0x78,        // SEI
            0xD8,        // CLD
            0xA2, 0xFF,  // LDX #$FF
            0x9A,        // TXS
            0xA2, 0x00,  // LDX #$00
            0x8A,        // TXA
            0x95, 0x80,  // STA $80,X
            0xE8,        // INX
            0xD0, 0xFB   // BNE -5 (back to STA $80,X)
        };
        Assert.That(expected.Length, Is.EqualTo(PrologueSize));
        for (int i = 0; i < expected.Length; i++)
            Assert.That(rom[i], Is.EqualTo(expected[i]), $"Prologue byte {i}");
    }

    [Test]
    public void GenerateFromSlab_WithReturnInstruction_JumpsToHaltLoop()
    {
        // B4: the program entry never returns — a top-level Return must jump
        // to the halt (self-loop), never emit RTS into an empty stack.
        var builder = new InstListBuilder();
        builder.Add((byte)LlirInstructionKind.Return, InstructionFlag.None, 0);
        var slab = builder.Build();
        var stringPool = new StringPool();

        var rom = _codeGenerator.GenerateFromSlab(slab, stringPool, new CodeGenOptions());

        // Prologue (13) + JMP <halt> (3); halt loop sits right after at 16.
        Assert.That(rom[PrologueSize], Is.EqualTo(0x4C)); // JMP, not RTS
        int target = rom[PrologueSize + 1] | (rom[PrologueSize + 2] << 8);
        int haltAddr = 0xF000 + PrologueSize + 3;
        Assert.That(target, Is.EqualTo(haltAddr));
        // The halt loop itself follows immediately.
        Assert.That(rom[PrologueSize + 3], Is.EqualTo(0x4C)); // self-loop JMP
        int loopTarget = rom[PrologueSize + 4] | (rom[PrologueSize + 5] << 8);
        Assert.That(loopTarget, Is.EqualTo(haltAddr), "halt loop points to itself");
        // No RTS anywhere in the ROM.
        Assert.That(rom, Has.None.EqualTo((byte)0x60), "no RTS on the main path");
    }

    [Test]
    public void GenerateFromSlab_WithCalleeReturn_EmitsRTS()
    {
        // A Return flagged CalleeReturn (lowering inside a function body)
        // emits RTS so a JSR caller resumes at the next instruction.
        var builder = new InstListBuilder();
        builder.Add((byte)LlirInstructionKind.Return, InstructionFlag.CalleeReturn, 0);
        var slab = builder.Build();
        var stringPool = new StringPool();

        var rom = _codeGenerator.GenerateFromSlab(slab, stringPool, new CodeGenOptions());

        Assert.That(rom[PrologueSize], Is.EqualTo(0x60)); // RTS (1 byte)
        // The halt self-loop follows immediately after the 1-byte RTS.
        Assert.That(rom[PrologueSize + 1], Is.EqualTo(0x4C)); // self-loop JMP
    }

    [Test]
    public void GenerateFromSlab_CalleeReturnLayout_JumpTargetAccountsForRtsSize()
    {
        // Layout: [Label fn][Return (callee, 1 byte)][Jump after][Label after][LDA #$42]
        // If EmitSize still sized a callee Return as 3 (the entry JMP), the
        // 'after' label would be computed 2 bytes late and the Jump would
        // land past the LDA.
        var builder = new InstListBuilder();
        builder.Add((byte)LlirInstructionKind.Label, InstructionFlag.None, 0, 1000); // fn:
        builder.Add((byte)LlirInstructionKind.Return, InstructionFlag.CalleeReturn, 0); // RTS
        builder.Add((byte)LlirInstructionKind.Jump, InstructionFlag.None, 0, 2000); // JMP after
        builder.Add((byte)LlirInstructionKind.Label, InstructionFlag.None, 0, 2000); // after:
        builder.Add((byte)LlirInstructionKind.Load, InstructionFlag.None, 0, 0x42); // LDA #$42
        var slab = builder.Build();
        var stringPool = new StringPool();

        var rom = _codeGenerator.GenerateFromSlab(slab, stringPool, new CodeGenOptions());

        Assert.That(rom[PrologueSize + 0], Is.EqualTo(0x60)); // RTS at 13
        Assert.That(rom[PrologueSize + 1], Is.EqualTo(0x4C)); // JMP at 14
        int jumpTarget = rom[PrologueSize + 2] | (rom[PrologueSize + 3] << 8);
        Assert.That(jumpTarget, Is.EqualTo(0xF000 + PrologueSize + 4), "jump lands on the LDA after the 1-byte RTS + 3-byte JMP");
        Assert.That(rom[PrologueSize + 4], Is.EqualTo(0xA9)); // LDA #$42
        Assert.That(rom[PrologueSize + 5], Is.EqualTo(0x42));
    }

    [Test]
    public void GenerateFromSlab_TwoOperandStoreAbsolute_JumpTargetAccountsForSize()
    {
        // Regression: EmitSize sized a two-operand Store by its high byte
        // alone (op[1] < 0x100 -> 2 bytes), while EmitStore emits a 3-byte
        // absolute STA for [addrLow, addrHigh] = $1234. A label after such
        // a Store was computed 1 byte early, corrupting jump targets.
        var builder = new InstListBuilder();
        builder.Add((byte)LlirInstructionKind.Jump, InstructionFlag.None, 0, 3000); // JMP target
        builder.Add((byte)LlirInstructionKind.Store, InstructionFlag.None, 0, 0x34, 0x12); // STA $1234 (3 bytes)
        builder.Add((byte)LlirInstructionKind.Label, InstructionFlag.None, 0, 3000); // target:
        builder.Add((byte)LlirInstructionKind.Load, InstructionFlag.None, 0, 0x07); // LDA #$07
        var slab = builder.Build();
        var stringPool = new StringPool();

        var rom = _codeGenerator.GenerateFromSlab(slab, stringPool, new CodeGenOptions());

        Assert.That(rom[PrologueSize + 0], Is.EqualTo(0x4C)); // JMP at 13
        int jumpTarget = rom[PrologueSize + 1] | (rom[PrologueSize + 2] << 8);
        Assert.That(jumpTarget, Is.EqualTo(0xF000 + PrologueSize + 6), "label sits after JMP (3) + STA abs (3)");
        Assert.That(rom[PrologueSize + 3], Is.EqualTo(0x8D)); // STA abs
        Assert.That(rom[PrologueSize + 4], Is.EqualTo(0x34));
        Assert.That(rom[PrologueSize + 5], Is.EqualTo(0x12));
        Assert.That(rom[PrologueSize + 6], Is.EqualTo(0xA9)); // LDA #$07
        Assert.That(rom[PrologueSize + 7], Is.EqualTo(0x07));
    }

    [Test]
    public void GenerateFromSlab_WithCodeExceedingRom_ThrowsRomSizeExceeded()
    {
        // A program whose code plus the halt loop would run past the
        // interrupt vectors must fail loudly instead of returning a
        // truncated ROM with its tail silently omitted.
        var builder = new InstListBuilder();
        // Fill almost entire ROM with NOPs
        for (int i = 0; i < 4090; i++)
        {
            builder.Add((byte)LlirInstructionKind.Load, InstructionFlag.None, 0, 0xEA); // LDA #$EA
        }
        var slab = builder.Build();

        var ex = Assert.Throws<GameVmException>(() =>
            _codeGenerator.GenerateFromSlab(slab, new StringPool(), new CodeGenOptions()));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(ex!.ErrorCode, Is.EqualTo(ErrorCode.RomSizeExceeded));
            Assert.That(ex.Message, Does.Contain("4092"));
        }
    }

    [Test]
    public void GenerateFromSlab_MultiplyWithFewerThanThreeOperands_ThrowsGameVmException()
    {
        // A direct LLIR producer can hand the code generator a Multiply
        // with one or two operands (a well-formed LLIR Multiply carries
        // three for a literal right operand, four for a variable one).
        // The old code returned 0 bytes for it and the emit loop broke,
        // returning a ROM that omitted that instruction and every
        // following one. Fail loudly instead.
        foreach (uint[] operands in new[] { new uint[] { 0x81 }, new uint[] { 0x81, 0x00 } })
        {
            var builder = new InstListBuilder();
            builder.Add((byte)LlirInstructionKind.Load, InstructionFlag.None, 0, 0x06); // LDA #6
            builder.Add((byte)LlirInstructionKind.Multiply, InstructionFlag.None, 0, operands);
            builder.Add((byte)LlirInstructionKind.Store, InstructionFlag.None, 0, 0x80); // STA $80
            var slab = builder.Build();

            var ex = Assert.Throws<GameVmException>(() =>
                _codeGenerator.GenerateFromSlab(slab, new StringPool(), new CodeGenOptions()));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(ex!.ErrorCode, Is.EqualTo(ErrorCode.UnhandledLlirInstruction));
                Assert.That(ex.Message, Does.Contain("Multiply"));
            }
        }
    }

    [Test]
    public void GenerateFromSlab_MultiplyEmitsShiftAndAddRoutine()
    {
        // Literal right (3 operands) and variable right (4 operands)
        // both emit the same 22-byte shift-and-add routine; only the
        // multiplier load differs (LDA #imm vs LDA zp).
        var literalBuilder = new InstListBuilder();
        literalBuilder.Add((byte)LlirInstructionKind.Load, InstructionFlag.None, 0, 0x03); // LDA #3
        literalBuilder.Add((byte)LlirInstructionKind.Multiply, InstructionFlag.None, 0, 0x07, 0x85, 0x86);
        literalBuilder.Add((byte)LlirInstructionKind.Store, InstructionFlag.None, 0, 0x80); // STA $80
        var literalRom = _codeGenerator.GenerateFromSlab(literalBuilder.Build(), new StringPool(), new CodeGenOptions());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(literalRom[PrologueSize + 0], Is.EqualTo(0xA9), "LDA #3");
            Assert.That(literalRom[PrologueSize + 2], Is.EqualTo(0x85), "STA scratchM");
            Assert.That(literalRom[PrologueSize + 3], Is.EqualTo(0x85), "scratchM $85");
            Assert.That(literalRom[PrologueSize + 4], Is.EqualTo(0xA9), "LDA #rightVal");
            Assert.That(literalRom[PrologueSize + 5], Is.EqualTo(0x07), "rightVal 7");
            Assert.That(literalRom[PrologueSize + 24], Is.EqualTo(0x85), "STA $80 follows the 22-byte routine");
            Assert.That(literalRom[PrologueSize + 25], Is.EqualTo(0x80));
        }

        var variableBuilder = new InstListBuilder();
        variableBuilder.Add((byte)LlirInstructionKind.Load, InstructionFlag.None, 0, 0x03); // LDA #3
        variableBuilder.Add((byte)LlirInstructionKind.Multiply, InstructionFlag.None, 0, 0x81, 0x00, 0x85, 0x86);
        variableBuilder.Add((byte)LlirInstructionKind.Store, InstructionFlag.None, 0, 0x80); // STA $80
        var variableRom = _codeGenerator.GenerateFromSlab(variableBuilder.Build(), new StringPool(), new CodeGenOptions());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(variableRom[PrologueSize + 2], Is.EqualTo(0x85), "STA scratchM");
            Assert.That(variableRom[PrologueSize + 3], Is.EqualTo(0x85), "scratchM $85");
            Assert.That(variableRom[PrologueSize + 4], Is.EqualTo(0xA5), "LDA rightAddr");
            Assert.That(variableRom[PrologueSize + 5], Is.EqualTo(0x81), "rightAddr $81");
            Assert.That(variableRom[PrologueSize + 24], Is.EqualTo(0x85), "STA $80 follows the 22-byte routine");
            Assert.That(variableRom[PrologueSize + 25], Is.EqualTo(0x80));
        }
    }

    [Test]
    public void GenerateFromSlab_WithCallInstruction_GeneratesJSR()
    {
        var builder = new InstListBuilder();
        builder.Add((byte)LlirInstructionKind.Call, InstructionFlag.None, 0, 0x00, 0xF0); // JSR $F000
        var slab = builder.Build();
        var stringPool = new StringPool();
        
        var rom = _codeGenerator.GenerateFromSlab(slab, stringPool, new CodeGenOptions());
        
        Assert.That(rom, Is.Not.Null);
        Assert.That(rom[PrologueSize + 0], Is.EqualTo(0x20)); // JSR
        Assert.That(rom[PrologueSize + 1], Is.EqualTo(0x00)); // low byte
        Assert.That(rom[PrologueSize + 2], Is.EqualTo(0xF0)); // high byte
    }

    [Test]
    public void GenerateFromSlab_WithLabelInstruction_SkipsCodeGeneration()
    {
        var builder = new InstListBuilder();
        builder.Add((byte)LlirInstructionKind.Label, InstructionFlag.None, 0); // label at address 0
        var slab = builder.Build();
        var stringPool = new StringPool();
        
        var rom = _codeGenerator.GenerateFromSlab(slab, stringPool, new CodeGenOptions());
        
        Assert.That(rom, Is.Not.Null);
        // Label should not generate any code, so first byte should be 0 (self-loop JMP will overwrite)
    }

    [Test]
    public void GenerateFromSlab_WithJumpInstruction_GeneratesJMP()
    {
        var builder = new InstListBuilder();
        builder.Add((byte)LlirInstructionKind.Jump, InstructionFlag.None, 0, 0x00, 0xF0); // JMP $F000
        var slab = builder.Build();
        var stringPool = new StringPool();
        
        var rom = _codeGenerator.GenerateFromSlab(slab, stringPool, new CodeGenOptions());
        
        Assert.That(rom, Is.Not.Null);
        Assert.That(rom[PrologueSize + 0], Is.EqualTo(0x4C)); // JMP
        Assert.That(rom[PrologueSize + 1], Is.EqualTo(0x00)); // low byte
        Assert.That(rom[PrologueSize + 2], Is.EqualTo(0xF0)); // high byte
    }

    [Test]
    public void GenerateFromSlab_WithBranchInstruction_EmitsBneWithOffset()
    {
        // The branch target must be defined: an unresolved label now
        // fails loudly. Five bytes of code sit between the Branch and
        // its Label so the computed displacement is still 5.
        var builder = new InstListBuilder();
        builder.Add((byte)LlirInstructionKind.Branch, InstructionFlag.None, 0, 5); // BNE offset 5
        builder.Add((byte)LlirInstructionKind.Load, InstructionFlag.None, 0, 0x42); // LDA #$42 (2 bytes)
        builder.Add((byte)LlirInstructionKind.Add, InstructionFlag.None, 0, 1); // CLC; ADC #1 (3 bytes)
        builder.Add((byte)LlirInstructionKind.Label, InstructionFlag.None, 0, 5);
        var slab = builder.Build();
        var stringPool = new StringPool();

        var rom = _codeGenerator.GenerateFromSlab(slab, stringPool, new CodeGenOptions());

        Assert.That(rom, Is.Not.Null);
        Assert.That(rom[PrologueSize + 0], Is.EqualTo(0xD0)); // BNE (branch when not equal)
        Assert.That(rom[PrologueSize + 1], Is.EqualTo(5)); // offset
    }

    [Test]
    public void GenerateFromSlab_WithSyscallInstruction_GeneratesJSR()
    {
        var builder = new InstListBuilder();
        builder.Add((byte)LlirInstructionKind.Syscall, InstructionFlag.None, 0, 0x34, 0x12); // Syscall $1234
        var slab = builder.Build();
        var stringPool = new StringPool();
        
        var rom = _codeGenerator.GenerateFromSlab(slab, stringPool, new CodeGenOptions());
        
        Assert.That(rom, Is.Not.Null);
        Assert.That(rom[PrologueSize + 0], Is.EqualTo(0x20)); // JSR
        Assert.That(rom[PrologueSize + 1], Is.EqualTo(0x34)); // low byte
        Assert.That(rom[PrologueSize + 2], Is.EqualTo(0x12)); // high byte
    }

    [Test]
    public void GenerateFromSlab_WithAssignInstruction_EmitsLdaImmediateThenStaZeroPage()
    {
        // LLIR -> ROM seam: Assign(targetAddr=$80, value=5) is the folded form the
        // constant store lowers to. It must emit LDA #5; STA $80.
        var builder = new InstListBuilder();
        builder.Add((byte)LlirInstructionKind.Assign, InstructionFlag.None, 0, 0x80, 5);
        var slab = builder.Build();
        var stringPool = new StringPool();

        var rom = _codeGenerator.GenerateFromSlab(slab, stringPool, new CodeGenOptions());

        Assert.That(rom, Is.Not.Null);
        Assert.That(rom[PrologueSize + 0], Is.EqualTo(0xA9), "First byte should be LDA #immediate");
        Assert.That(rom[PrologueSize + 1], Is.EqualTo(0x05), "Immediate should be the value 5");
        Assert.That(rom[PrologueSize + 2], Is.EqualTo(0x85), "Third byte should be STA zero-page");
        Assert.That(rom[PrologueSize + 3], Is.EqualTo(0x80), "Target address should be $80");
    }

    [Test]
    public void GenerateFromSlab_WithUnknownInstruction_ThrowsGameVmException()
    {
        var builder = new InstListBuilder();
        builder.Add(255, InstructionFlag.None, 0); // Unknown kind
        var slab = builder.Build();
        var stringPool = new StringPool();
        var ex = Assert.Throws<GameVmException>(() =>
            _codeGenerator.GenerateFromSlab(slab, stringPool, new CodeGenOptions()));

        Assert.That(ex!.ErrorCode, Is.EqualTo(ErrorCode.UnhandledLlirInstruction));
        Assert.That(ex.Message, Does.Contain("0xFF"));
    }



    [Test]
    public void GenerateFromSlab_WithUnemittedBranchKinds_ThrowsNamingTheKind()
    {
        foreach (byte kind in new[]
                 {
                     (byte)LlirInstructionKind.Bne,
                     (byte)LlirInstructionKind.Beq,
                     (byte)LlirInstructionKind.Bpl,
                     (byte)LlirInstructionKind.Bmi,
                 })
        {
            var builder = new InstListBuilder();
            builder.Add(kind, InstructionFlag.None, 0, 0x10);
            var slab = builder.Build();

            var ex = Assert.Throws<GameVmException>(() =>
                _codeGenerator.GenerateFromSlab(slab, stringPool: new StringPool(), new CodeGenOptions()));

            Assert.That(ex!.ErrorCode, Is.EqualTo(ErrorCode.UnhandledLlirInstruction));
            Assert.That(ex.Message, Does.Contain(((LlirInstructionKind)kind).ToString()));
        }
    }

    [Test]
    public void GenerateFromSlab_EmitsSelfLoopJMPAtEnd()
    {
        var builder = new InstListBuilder();
        builder.Add((byte)LlirInstructionKind.Load, InstructionFlag.None, 0, 0x42); // LDA #$42
        var slab = builder.Build();
        var stringPool = new StringPool();
        
        var rom = _codeGenerator.GenerateFromSlab(slab, stringPool, new CodeGenOptions());
        
        Assert.That(rom, Is.Not.Null);
        // The self-loop JMP should be at the end of generated code
        // prologue (13) + LDA #$42 = 2 bytes, then JMP * = 3 bytes
        Assert.That(rom[PrologueSize + 2], Is.EqualTo(0x4C)); // JMP opcode
    }

    [Test]
    public void GenerateFromSlab_SetsInterruptVectors()
    {
        var builder = new InstListBuilder();
        builder.Add((byte)LlirInstructionKind.Load, InstructionFlag.None, 0, 0x42);
        var slab = builder.Build();
        var stringPool = new StringPool();
        
        var rom = _codeGenerator.GenerateFromSlab(slab, stringPool, new CodeGenOptions());
        
        Assert.That(rom, Is.Not.Null);
        Assert.That(rom.Length, Is.EqualTo(4096));
        // Vectors at offset 0x0FFC (4092)
        Assert.That(rom[4092], Is.EqualTo(0x00)); // IRQ low
        Assert.That(rom[4093], Is.EqualTo(0xF0)); // IRQ high ($F000)
        Assert.That(rom[4094], Is.EqualTo(0x00)); // Reset low
        Assert.That(rom[4095], Is.EqualTo(0xF0)); // Reset high ($F000)
    }

    [Test]
    public void GenerateFromSlab_WithMultipleInstructions_GeneratesSequentialCode()
    {
        var builder = new InstListBuilder();
        builder.Add((byte)LlirInstructionKind.Load, InstructionFlag.None, 0, 0x42); // LDA #$42
        builder.Add((byte)LlirInstructionKind.Load, InstructionFlag.None, 0, 0x24); // LDA #$24
        builder.Add((byte)LlirInstructionKind.Store, InstructionFlag.None, 0, 0x09); // STA $09 (zero page, 1-operand [addr])
        var slab = builder.Build();
        var stringPool = new StringPool();
        
        var rom = _codeGenerator.GenerateFromSlab(slab, stringPool, new CodeGenOptions());
        
        Assert.That(rom, Is.Not.Null);
        // prologue (13) + LDA #$42 (2 bytes) + LDA #$24 (2 bytes) + STA $09 (2 bytes) + self-loop JMP (3 bytes)
        Assert.That(rom[PrologueSize + 0], Is.EqualTo(0xA9)); Assert.That(rom[PrologueSize + 1], Is.EqualTo(0x42)); // LDA #$42
        Assert.That(rom[PrologueSize + 2], Is.EqualTo(0xA9)); Assert.That(rom[PrologueSize + 3], Is.EqualTo(0x24)); // LDA #$24
        Assert.That(rom[PrologueSize + 4], Is.EqualTo(0x85)); Assert.That(rom[PrologueSize + 5], Is.EqualTo(0x09)); // STA $09
        Assert.That(rom[PrologueSize + 6], Is.EqualTo(0x4C)); // JMP self-loop
    }

    [Test]
    public void GenerateFromSlab_WithAbsoluteStore_Generates3ByteInstruction()
    {
        var builder = new InstListBuilder();
        // STA with 3 operands: target_reg, addr_low, addr_high
        builder.Add((byte)LlirInstructionKind.Store, InstructionFlag.None, 0, 0, 0x34, 0x12); // STA $1234
        var slab = builder.Build();
        var stringPool = new StringPool();
        
        var rom = _codeGenerator.GenerateFromSlab(slab, stringPool, new CodeGenOptions());
        
        Assert.That(rom, Is.Not.Null);
        Assert.That(rom[PrologueSize + 0], Is.EqualTo(0x8D)); // STA absolute
        Assert.That(rom[PrologueSize + 1], Is.EqualTo(0x34)); // low byte
        Assert.That(rom[PrologueSize + 2], Is.EqualTo(0x12)); // high byte
    }
    [Test]
    public void GenerateFromSlab_WithStoreInstruction_ZeroPageAndAbsolute()
    {
        // Test zero-page store (address < 0x100)
        // Address 0x000F: addr_low=0x0F, addr_high=0x00 => address = 0x0F
        var builder = new InstListBuilder();
        builder.Add((byte)LlirInstructionKind.Store, InstructionFlag.None, 0, 0, 0x0F, 0x00); // target=0, addr_low=0x0F, addr_high=0x00
        var slab = builder.Build();
        var stringPool = new StringPool();
        
        var rom = _codeGenerator.GenerateFromSlab(slab, stringPool, new CodeGenOptions());
        
        Assert.That(rom, Is.Not.Null);
        Assert.That(rom[PrologueSize + 0], Is.EqualTo(0x85)); // STA zero-page
        Assert.That(rom[PrologueSize + 1], Is.EqualTo(0x0F)); // address $0F
        
        // Test absolute store (address >= 0x100)
        // Address 0x1234: addr_low=0x34, addr_high=0x12
        var builder2 = new InstListBuilder();
        builder2.Add((byte)LlirInstructionKind.Store, InstructionFlag.None, 0, 0, 0x34, 0x12); // target=0, addr_low=0x34, addr_high=0x12
        var slab2 = builder2.Build();
        var stringPool2 = new StringPool();
        
        var rom2 = _codeGenerator.GenerateFromSlab(slab2, stringPool2, new CodeGenOptions());
        
        Assert.That(rom2, Is.Not.Null);
        Assert.That(rom2[PrologueSize + 0], Is.EqualTo(0x8D)); // STA absolute
        Assert.That(rom2[PrologueSize + 1], Is.EqualTo(0x34)); // low byte
        Assert.That(rom2[PrologueSize + 2], Is.EqualTo(0x12)); // high byte
    }

    [Test]
    public void GenerateFromSlab_WithStoreInstruction_OneOperandFallback()
    {
        // Operands length == 1: treats operands[0] as address
        var builder = new InstListBuilder();
        builder.Add((byte)LlirInstructionKind.Store, InstructionFlag.None, 0, 0x20); // operands: [0x20]
        var slab = builder.Build();
        var stringPool = new StringPool();
        
        var rom = _codeGenerator.GenerateFromSlab(slab, stringPool, new CodeGenOptions());
        
        Assert.That(rom, Is.Not.Null);
        Assert.That(rom[PrologueSize + 0], Is.EqualTo(0x85)); // STA zero-page
        Assert.That(rom[PrologueSize + 1], Is.EqualTo(0x20)); // address $20
    }

    [Test]
    public void GenerateFromSlab_WithStoreInstruction_ZeroOperands_GeneratesSelfLoop()
    {
        // Zero operands: the Store case doesn't fire (no address available),
        // so bytesWritten stays 0, loop breaks, and only self-loop JMP is emitted
        var builder = new InstListBuilder();
        builder.Add((byte)LlirInstructionKind.Store, InstructionFlag.None, 0x60); // blockId = 0x60, no operands
        var slab = builder.Build();
        var stringPool = new StringPool();
        
        var rom = _codeGenerator.GenerateFromSlab(slab, stringPool, new CodeGenOptions());
        
        Assert.That(rom, Is.Not.Null);
        Assert.That(rom.Length, Is.EqualTo(4096));
        // With zero operands, nothing is written before the self-loop JMP
        // The self-loop JMP appears right after the prologue
        Assert.That(rom[PrologueSize], Is.EqualTo(0x4C)); // JMP opcode (self-loop)
    }

    [Test]
    public void GenerateFromSlab_WithStoreInstruction_ThreeOperandAbsolute()
    {
        var builder = new InstListBuilder();
        // Address 0xABCD: addr_low=0xCD, addr_high=0xAB
        builder.Add((byte)LlirInstructionKind.Store, InstructionFlag.None, 0, 0, 0xCD, 0xAB); // target=0, addr_low=0xCD, addr_high=0xAB
        var slab = builder.Build();
        var stringPool = new StringPool();
        
        var rom = _codeGenerator.GenerateFromSlab(slab, stringPool, new CodeGenOptions());
        
        Assert.That(rom, Is.Not.Null);
        Assert.That(rom[PrologueSize + 0], Is.EqualTo(0x8D)); // STA absolute
        Assert.That(rom[PrologueSize + 1], Is.EqualTo(0xCD)); // low byte
        Assert.That(rom[PrologueSize + 2], Is.EqualTo(0xAB)); // high byte
    }



    [Test]
    public void GenerateFromSlab_WithCodeExactlyFillingRom_GeneratesRomWithVectorsIntact()
    {
        // 2038 two-byte Loads end the code at offset 4089, and the 3-byte
        // halt loop fills exactly to 4092, the vectors' base. This is the
        // largest program that fits and must still generate.
        var builder = new InstListBuilder();
        for (int i = 0; i < 2038; i++)
        {
            builder.Add((byte)LlirInstructionKind.Load, InstructionFlag.None, 0, 0xEA);
        }
        var slab = builder.Build();

        var rom = _codeGenerator.GenerateFromSlab(slab, new StringPool(), new CodeGenOptions());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(rom.Length, Is.EqualTo(4096));
            Assert.That(rom[4089], Is.EqualTo(0x4C), "halt loop JMP must sit at 4089");
            int loopTarget = rom[4090] | (rom[4091] << 8);
            Assert.That(loopTarget, Is.EqualTo(0xF000 + 4089), "halt loop points to itself");
            Assert.That(rom[4092], Is.Zero); // IRQ low
            Assert.That(rom[4093], Is.EqualTo(0xF0)); // IRQ high
            Assert.That(rom[4094], Is.Zero); // Reset low
            Assert.That(rom[4095], Is.EqualTo(0xF0)); // Reset high
        }
    }

    [Test]
    public void GenerateFromSlab_SelfLoopJump_PointsToItself()
    {
        var builder = new InstListBuilder();
        builder.Add((byte)LlirInstructionKind.Load, InstructionFlag.None, 0, 0x42); // LDA #$42
        var slab = builder.Build();
        var stringPool = new StringPool();
        
        var rom = _codeGenerator.GenerateFromSlab(slab, stringPool, new CodeGenOptions());
        
        Assert.That(rom, Is.Not.Null);
        int codeEndIndex = PrologueSize + 2; // prologue + LDA #$42 = 2 bytes
        Assert.That(rom[codeEndIndex], Is.EqualTo(0x4C)); // JMP opcode
        int loopAddr = rom[codeEndIndex + 1] | (rom[codeEndIndex + 2] << 8);
        int expectedAddr = 0xF000 + codeEndIndex;
        Assert.That(loopAddr, Is.EqualTo(expectedAddr));
    }

    [Test]
    public void GenerateFromSlab_InterruptVectors_RemainAtEndWithMoreCode()
    {
        var builder = new InstListBuilder();
        for (int i = 0; i < 100; i++)
        {
            builder.Add((byte)LlirInstructionKind.Load, InstructionFlag.None, 0, (byte)i);
        }
        var slab = builder.Build();
        var stringPool = new StringPool();
        
        var rom = _codeGenerator.GenerateFromSlab(slab, stringPool, new CodeGenOptions());
        
        Assert.That(rom.Length, Is.EqualTo(4096));
        Assert.That(rom[4092], Is.EqualTo(0x00)); // IRQ low
        Assert.That(rom[4093], Is.EqualTo(0xF0)); // IRQ high
        Assert.That(rom[4094], Is.EqualTo(0x00)); // Reset low
        Assert.That(rom[4095], Is.EqualTo(0xF0)); // Reset high
    }

    [Test]
    public void GenerateFromSlab_SequentialMixedInstructions_LayoutIsCorrect()
    {
        var builder = new InstListBuilder();
        builder.Add((byte)LlirInstructionKind.Load, InstructionFlag.None, 0, 0x01);     // LDA #$01  (2 bytes)
        builder.Add((byte)LlirInstructionKind.Store, InstructionFlag.None, 0, 0, 0xFF, 0x00); // STA $00FF zp (2 bytes)
        builder.Add((byte)LlirInstructionKind.Call, InstructionFlag.None, 0, 0x34, 0x12); // JSR $1234 (3 bytes)
        builder.Add((byte)LlirInstructionKind.Branch, InstructionFlag.None, 0, 0x7F);  // BNE to label 0x7F (2 bytes)
        builder.Add((byte)LlirInstructionKind.Return, InstructionFlag.None, 0);           // JMP <halt-loop> (3 bytes, B4)
        builder.Add((byte)LlirInstructionKind.Label, InstructionFlag.None, 0, 0x7F); // defines the branch target (0 bytes)
        var slab = builder.Build();
        var stringPool = new StringPool();

        var rom = _codeGenerator.GenerateFromSlab(slab, stringPool, new CodeGenOptions());

        Assert.That(rom, Is.Not.Null);
        Assert.That(rom[PrologueSize + 0], Is.EqualTo(0xA9)); // LDA #$01
        Assert.That(rom[PrologueSize + 1], Is.EqualTo(0x01));
        Assert.That(rom[PrologueSize + 2], Is.EqualTo(0x85)); // STA zp $00FF
        Assert.That(rom[PrologueSize + 3], Is.EqualTo(0xFF));
        Assert.That(rom[PrologueSize + 4], Is.EqualTo(0x20)); // JSR $1234
        Assert.That(rom[PrologueSize + 5], Is.EqualTo(0x34));
        Assert.That(rom[PrologueSize + 6], Is.EqualTo(0x12));
        Assert.That(rom[PrologueSize + 7], Is.EqualTo(0xD0)); // BNE to label at +12: displacement 3
        Assert.That(rom[PrologueSize + 8], Is.EqualTo(0x03));
        // B4: Return on the main path jumps to the halt loop, never RTS.
        Assert.That(rom[PrologueSize + 9], Is.EqualTo(0x4C)); // JMP
        int haltAddr = 0xF000 + PrologueSize + 12;
        int returnTarget = rom[PrologueSize + 10] | (rom[PrologueSize + 11] << 8);
        Assert.That(returnTarget, Is.EqualTo(haltAddr));
        Assert.That(rom[PrologueSize + 12], Is.EqualTo(0x4C)); // self-loop JMP
    }

    [Test]
    public void GenerateFromSlab_WithLoadSubStore_EmitsLdaZpSecSbcStaZp()
    {
        // LLIR -> ROM seam: Load(xAddr); Sub(1); Store(xAddr)
        // Load(xAddr) = LDA $80 (0xA5 0x80)
        // Sub(1) = SEC; SBC #1 (0x38 0xE9 0x01)
        // Store($80) = STA $80 (0x85 0x80)
        var builder = new InstListBuilder();
        builder.Add((byte)LlirInstructionKind.Load, InstructionFlag.None, 0, 0x80, 0); // Load(xAddr, 0) -> LDA zp
        builder.Add((byte)LlirInstructionKind.Sub, InstructionFlag.None, 0, 1);       // Sub(1) -> SEC; SBC #1
        builder.Add((byte)LlirInstructionKind.Store, InstructionFlag.None, 0, 0x80); // Store($80) -> STA zp
        var slab = builder.Build();
        var stringPool = new StringPool();

        var rom = _codeGenerator.GenerateFromSlab(slab, stringPool, new CodeGenOptions());

        Assert.That(rom, Is.Not.Null);
        // Load(xAddr,0) -> LDA zp $80 = A5 80
        Assert.That(rom[PrologueSize + 0], Is.EqualTo(0xA5), "Load zp should emit LDA zp (0xA5)");
        Assert.That(rom[PrologueSize + 1], Is.EqualTo(0x80), "Load zp address should be $80");
        // Sub(1) -> SEC; SBC #1 = 38 E9 01
        Assert.That(rom[PrologueSize + 2], Is.EqualTo(0x38), "Sub should emit SEC (0x38)");
        Assert.That(rom[PrologueSize + 3], Is.EqualTo(0xE9), "Sub should emit SBC #imm (0xE9)");
        Assert.That(rom[PrologueSize + 4], Is.EqualTo(0x01), "Sub immediate should be 1");
        // Store($80) -> STA zp $80 = 85 80
        Assert.That(rom[PrologueSize + 5], Is.EqualTo(0x85), "Store should emit STA zp (0x85)");
        Assert.That(rom[PrologueSize + 6], Is.EqualTo(0x80), "Store target should be $80");
        // self-loop JMP * follows
        Assert.That(rom[PrologueSize + 7], Is.EqualTo(0x4C), "Self-loop JMP should follow");
    }

    [Test]
    public void GenerateFromSlab_WithLoadZpSingleOperand_EmitsLdaImmediate()
    {
        // Single-operand Load = immediate (0xA9)
        var builder = new InstListBuilder();
        builder.Add((byte)LlirInstructionKind.Load, InstructionFlag.None, 0, 0x42); // LDA #$42
        var slab = builder.Build();
        var stringPool = new StringPool();

        var rom = _codeGenerator.GenerateFromSlab(slab, stringPool, new CodeGenOptions());

        Assert.That(rom, Is.Not.Null);
        Assert.That(rom[PrologueSize + 0], Is.EqualTo(0xA9), "Single-operand Load = LDA #imm");
        Assert.That(rom[PrologueSize + 1], Is.EqualTo(0x42), "Immediate value $42");
    }

    [Test]
    public void GenerateFromSlab_WithBranchBeyondRange_ThrowsBranchOutOfRange()
    {
        // An if-body of five variable multiplications is 130 bytes (five
        // times 26). The skip branch over it needs displacement +130,
        // outside the 6502's -128..+127 relative range. The old code
        // wrapped it to 0x82 and jumped backward into the body, so the
        // generator must reject it.
        var pool = new StringPool();
        uint endOff = pool.Intern("L_end");

        var builder = new InstListBuilder();
        builder.Add((byte)LlirInstructionKind.Cmp, InstructionFlag.None, 0, 0x80, 0); // CMP $80 (2 bytes)
        builder.Add((byte)LlirInstructionKind.Transition, InstructionFlag.None, 0); // polarity marker (0 bytes)
        builder.Add((byte)LlirInstructionKind.Branch, InstructionFlag.None, 0, endOff); // skip branch (2 bytes)
        for (int i = 0; i < 65; i++)
            builder.Add((byte)LlirInstructionKind.Load, InstructionFlag.None, 0, 0xEA); // 130 bytes of body
        builder.Add((byte)LlirInstructionKind.Label, InstructionFlag.None, 0, endOff);
        builder.Add((byte)LlirInstructionKind.Load, InstructionFlag.None, 0, 0x42);
        var slab = builder.Build();

        var ex = Assert.Throws<GameVmException>(() =>
            _codeGenerator.GenerateFromSlab(slab, pool, new CodeGenOptions()));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(ex!.ErrorCode, Is.EqualTo(ErrorCode.BranchOutOfRange));
            Assert.That(ex.Message, Does.Contain("130"));
        }
    }

    [Test]
    public void GenerateFromSlab_WithCmpTransitionBranch_EmitsBne()
    {
        // Cmp + Branch (no Transition) → BNE (0xD0)
        // Simulates: if x <> 0 then y := 1;  arm execution
        var pool = new StringPool();
        uint xOff = pool.Intern("x");
        uint endOff = pool.Intern("L_end");

        var builder = new InstListBuilder();
        builder.Add((byte)LlirInstructionKind.Cmp, InstructionFlag.None, 0, xOff, 0);
        builder.Add((byte)LlirInstructionKind.Branch, InstructionFlag.None, 0, endOff);
        builder.Add((byte)LlirInstructionKind.Label, InstructionFlag.None, 0, endOff);
        var slab = builder.Build();

        var rom = _codeGenerator.GenerateFromSlab(slab, pool, new CodeGenOptions());

        Assert.That(rom, Is.Not.Null);
        Assert.That(rom[PrologueSize + 0], Is.EqualTo(0xC5), "CMP zp opcode (2-operand Cmp uses zero-page)");
        Assert.That(rom[PrologueSize + 2], Is.EqualTo(0xD0), "BNE opcode");
    }

    [Test]
    public void GenerateFromSlab_WithCmpTransitionBranch_EmitsBeq()
    {
        var pool = new StringPool();
        uint xOff = pool.Intern("x");
        uint endOff = pool.Intern("L_end");

        var builder = new InstListBuilder();
        builder.Add((byte)LlirInstructionKind.Cmp, InstructionFlag.None, 0, xOff, 0);
        builder.Add((byte)LlirInstructionKind.Transition, InstructionFlag.None, 0);
        builder.Add((byte)LlirInstructionKind.Branch, InstructionFlag.None, 0, endOff);
        builder.Add((byte)LlirInstructionKind.Label, InstructionFlag.None, 0, endOff);
        var slab = builder.Build();

        var rom = _codeGenerator.GenerateFromSlab(slab, pool, new CodeGenOptions());

        Assert.That(rom, Is.Not.Null);
        Assert.That(rom[PrologueSize + 0], Is.EqualTo(0xC5), "CMP zp opcode (2-operand Cmp uses zero-page)");
        Assert.That(rom[PrologueSize + 2], Is.EqualTo(0xF0), "BEQ opcode");
    }

    // Unresolved-label slice: a Jump, Call, or Branch to a label with no
    // matching Label instruction must fail loudly. Today Jump/Call silently
    // emit a transfer to $F000 (the reset prologue) and Branch uses the
    // label's pool-offset byte as its displacement — both produce a ROM
    // that jumps somewhere wrong. Labels that DO resolve still emit
    // correctly (the guards below).

    [Test]
    public void UnresolvedLabelInJumpCallOrBranchThrowsInsteadOfSilentFallback()
    {
        // One assertion per transfer kind, collected so a partial fix
        // still shows exactly which kinds fall back silently.
        using (Assert.EnterMultipleScope())
        {
            Assert.Throws<GameVmException>(() =>
            {
                var builder = new InstListBuilder();
                builder.Add((byte)LlirInstructionKind.Jump, InstructionFlag.None, 0, 9999);
                _codeGenerator.GenerateFromSlab(builder.Build(), new StringPool(), new CodeGenOptions());
            }, "Jump to an unresolved label must throw, not JMP $F000");
            Assert.Throws<GameVmException>(() =>
            {
                var builder = new InstListBuilder();
                builder.Add((byte)LlirInstructionKind.Call, InstructionFlag.None, 0, 9999);
                _codeGenerator.GenerateFromSlab(builder.Build(), new StringPool(), new CodeGenOptions());
            }, "Call to an unresolved label must throw, not JSR $F000");
            Assert.Throws<GameVmException>(() =>
            {
                var builder = new InstListBuilder();
                builder.Add((byte)LlirInstructionKind.Cmp, InstructionFlag.None, 0, 0x80, 0);
                builder.Add((byte)LlirInstructionKind.Branch, InstructionFlag.None, 0, 9999);
                _codeGenerator.GenerateFromSlab(builder.Build(), new StringPool(), new CodeGenOptions());
            }, "Branch to an unresolved label must throw, not branch by a garbage offset");
        }
    }

    [Test]
    public void GenerateFromSlab_WithJumpToUnresolvedLabel_ThrowsInsteadOfSilentFallback()
    {
        // Today the generator silently emits JMP $F000 for a Jump whose
        // label has no matching Label instruction.
        var builder = new InstListBuilder();
        builder.Add((byte)LlirInstructionKind.Jump, InstructionFlag.None, 0, 9999);
        var slab = builder.Build();

        Assert.Throws<GameVmException>(() =>
            _codeGenerator.GenerateFromSlab(slab, new StringPool(), new CodeGenOptions()));
    }

    [Test]
    public void GenerateFromSlab_WithCallToUnresolvedLabel_ThrowsInsteadOfSilentFallback()
    {
        // Today the generator silently emits JSR $F000 for a Call whose
        // label has no matching Label instruction.
        var builder = new InstListBuilder();
        builder.Add((byte)LlirInstructionKind.Call, InstructionFlag.None, 0, 9999);
        var slab = builder.Build();

        Assert.Throws<GameVmException>(() =>
            _codeGenerator.GenerateFromSlab(slab, new StringPool(), new CodeGenOptions()));
    }

    [Test]
    public void GenerateFromSlab_WithBranchToUnresolvedLabel_ThrowsInsteadOfSilentFallback()
    {
        // Today the generator uses the label's pool-offset byte
        // (9999 & 0xFF = 0x0F) as the branch displacement and emits a BNE
        // that lands somewhere wrong.
        var builder = new InstListBuilder();
        builder.Add((byte)LlirInstructionKind.Cmp, InstructionFlag.None, 0, 0x80, 0);
        builder.Add((byte)LlirInstructionKind.Branch, InstructionFlag.None, 0, 9999);
        var slab = builder.Build();

        Assert.Throws<GameVmException>(() =>
            _codeGenerator.GenerateFromSlab(slab, new StringPool(), new CodeGenOptions()));
    }

    [Test]
    public void GenerateFromSlab_WithJumpToResolvedLabel_EmitsJumpToLabelAddress()
    {
        // Guard: a Jump to a label that IS defined still emits JMP to the
        // label's address, not an error.
        var builder = new InstListBuilder();
        builder.Add((byte)LlirInstructionKind.Jump, InstructionFlag.None, 0, 1000);
        builder.Add((byte)LlirInstructionKind.Label, InstructionFlag.None, 0, 1000);
        builder.Add((byte)LlirInstructionKind.Load, InstructionFlag.None, 0, 0x42);
        var slab = builder.Build();
        var stringPool = new StringPool();

        var rom = _codeGenerator.GenerateFromSlab(slab, stringPool, new CodeGenOptions());

        Assert.That(rom[PrologueSize + 0], Is.EqualTo(0x4C)); // JMP
        int jumpTarget = rom[PrologueSize + 1] | (rom[PrologueSize + 2] << 8);
        Assert.That(jumpTarget, Is.EqualTo(0xF000 + PrologueSize + 3), "jump lands on the LDA after the label");
        Assert.That(rom[PrologueSize + 3], Is.EqualTo(0xA9)); // LDA #$42
        Assert.That(rom[PrologueSize + 4], Is.EqualTo(0x42));
    }

    [Test]
    public void GenerateFromSlab_WithCallToResolvedLabel_EmitsCallToLabelAddress()
    {
        // Guard: a Call to a label that IS defined still emits JSR to the
        // label's address, not an error.
        var builder = new InstListBuilder();
        builder.Add((byte)LlirInstructionKind.Call, InstructionFlag.None, 0, 1000);
        builder.Add((byte)LlirInstructionKind.Label, InstructionFlag.None, 0, 1000);
        builder.Add((byte)LlirInstructionKind.Load, InstructionFlag.None, 0, 0x42);
        var slab = builder.Build();
        var stringPool = new StringPool();

        var rom = _codeGenerator.GenerateFromSlab(slab, stringPool, new CodeGenOptions());

        Assert.That(rom[PrologueSize + 0], Is.EqualTo(0x20)); // JSR
        int callTarget = rom[PrologueSize + 1] | (rom[PrologueSize + 2] << 8);
        Assert.That(callTarget, Is.EqualTo(0xF000 + PrologueSize + 3), "call lands on the LDA after the label");
        Assert.That(rom[PrologueSize + 3], Is.EqualTo(0xA9)); // LDA #$42
        Assert.That(rom[PrologueSize + 4], Is.EqualTo(0x42));
    }

    [Test]
    public void GenerateFromSlab_WithBranchToResolvedLabel_EmitsBranchWithCorrectDisplacement()
    {
        // Guard: a Branch to a label that IS defined still emits a branch
        // with the displacement to the label, not an error.
        var builder = new InstListBuilder();
        builder.Add((byte)LlirInstructionKind.Cmp, InstructionFlag.None, 0, 0x80, 0); // CMP $80
        builder.Add((byte)LlirInstructionKind.Branch, InstructionFlag.None, 0, 1000);
        builder.Add((byte)LlirInstructionKind.Label, InstructionFlag.None, 0, 1000);
        builder.Add((byte)LlirInstructionKind.Load, InstructionFlag.None, 0, 0x42);
        var slab = builder.Build();
        var stringPool = new StringPool();

        var rom = _codeGenerator.GenerateFromSlab(slab, stringPool, new CodeGenOptions());

        Assert.That(rom[PrologueSize + 0], Is.EqualTo(0xC5)); // CMP zp
        Assert.That(rom[PrologueSize + 1], Is.EqualTo(0x80));
        Assert.That(rom[PrologueSize + 2], Is.EqualTo(0xD0)); // BNE
        Assert.That(rom[PrologueSize + 3], Is.EqualTo(0x00), "displacement is 0: the label sits right after the branch");
        Assert.That(rom[PrologueSize + 4], Is.EqualTo(0xA9)); // LDA #$42
        Assert.That(rom[PrologueSize + 5], Is.EqualTo(0x42));
    }

    // Mutation pins (PR #23 fix round 1): the 21 surviving mutants in
    // this slice's loud-failure paths. Each test names the mutants it
    // kills; where a mutant is provably equivalent the test pins the
    // reachable boundary instead and the equivalence is recorded in
    // FIX-NOTES.md.

    [Test]
    public void MultiplyWithFewerThanThreeOperands_ThrowsWithFullMessageAndContext()
    {
        // Kills line 353 (Object initializer: the context dictionary
        // must not be empty) and line 356 (String: the "OperandCount"
        // key must not be emptied). The message assertions also kill
        // the pre-existing line 350/351 String mutants on the same
        // throw (outside this slice's hunks).
        foreach (uint[] operands in new[] { new uint[] { 0x81 }, new uint[] { 0x81, 0x00 } })
        {
            var builder = new InstListBuilder();
            builder.Add((byte)LlirInstructionKind.Load, InstructionFlag.None, 0, 0x06); // LDA #6
            builder.Add((byte)LlirInstructionKind.Multiply, InstructionFlag.None, 0, operands);
            builder.Add((byte)LlirInstructionKind.Store, InstructionFlag.None, 0, 0x80); // STA $80
            var slab = builder.Build();

            var ex = Assert.Throws<GameVmException>(() =>
                _codeGenerator.GenerateFromSlab(slab, new StringPool(), new CodeGenOptions()));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(ex!.ErrorCode, Is.EqualTo(ErrorCode.UnhandledLlirInstruction));
                Assert.That(ex.Message, Does.Contain("cannot emit LLIR Multiply with"));
                Assert.That(ex.Message, Does.Contain($"{operands.Length} operand(s)"));
                Assert.That(ex.Message, Does.Contain("three operands (literal right) or four (variable right)"));
                Assert.That(ex.Context["LlirKind"], Is.EqualTo("Multiply"));
                Assert.That(ex.Context["OperandCount"], Is.EqualTo(operands.Length));
            }
        }
    }

    [Test]
    public void JumpAndCallToUnresolvedLabel_ThrowWithFullMessageAndContext()
    {
        // Kills lines 499/500 (String: both message parts must survive),
        // 502 (Object initializer: the context dictionary must not be
        // empty) and 504 (String: the "LabelPoolOffset" key must not be
        // emptied). Jump and Call share EmitTransfer, so one test pins
        // both kinds' context values.
        foreach (LlirInstructionKind kind in new[] { LlirInstructionKind.Jump, LlirInstructionKind.Call })
        {
            var builder = new InstListBuilder();
            builder.Add((byte)kind, InstructionFlag.None, 0, 9999);
            var slab = builder.Build();

            var ex = Assert.Throws<GameVmException>(() =>
                _codeGenerator.GenerateFromSlab(slab, new StringPool(), new CodeGenOptions()));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(ex!.ErrorCode, Is.EqualTo(ErrorCode.UnresolvedLabel));
                Assert.That(ex.Message, Does.Contain($"cannot emit {kind} to label with pool offset 9999"));
                Assert.That(ex.Message, Does.Contain("no matching Label instruction in the slab"));
                Assert.That(ex.Context["LabelPoolOffset"], Is.EqualTo(9999u));
                Assert.That(ex.Context["LlirKind"], Is.EqualTo(kind.ToString()));
            }
        }
    }

    [Test]
    public void BranchToUnresolvedLabel_ThrowsWithFullMessageAndContext()
    {
        // Kills lines 532/533 (String: both message parts must survive),
        // 535 (Object initializer: the context dictionary must not be
        // empty), 537 (String: the "LabelPoolOffset" key) and 539
        // (String: the "InstructionAddress" key). The branch sits at
        // ROM offset 15 (13-byte prologue + 2-byte CMP).
        var builder = new InstListBuilder();
        builder.Add((byte)LlirInstructionKind.Cmp, InstructionFlag.None, 0, 0x80, 0); // CMP $80
        builder.Add((byte)LlirInstructionKind.Branch, InstructionFlag.None, 0, 9999);
        var slab = builder.Build();

        var ex = Assert.Throws<GameVmException>(() =>
            _codeGenerator.GenerateFromSlab(slab, new StringPool(), new CodeGenOptions()));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(ex!.ErrorCode, Is.EqualTo(ErrorCode.UnresolvedLabel));
            Assert.That(ex.Message, Does.Contain("cannot emit Branch to label with pool offset 9999"));
            Assert.That(ex.Message, Does.Contain("at ROM offset 15"));
            Assert.That(ex.Message, Does.Contain("no matching Label instruction in the slab"));
            Assert.That(ex.Context["LabelPoolOffset"], Is.EqualTo(9999u));
            Assert.That(ex.Context["LlirKind"], Is.EqualTo("Branch"));
            Assert.That(ex.Context["InstructionAddress"], Is.EqualTo(15));
        }
    }

    [Test]
    public void UnknownInstruction_ThrowsWithFullMessageAndContext()
    {
        // Kills line 700 (Object initializer: the context dictionary
        // must not be empty) and 703 (String: the "LlirKindByte" key
        // must not be emptied). The message assertion also kills the
        // pre-existing line 698 String mutant on the same throw
        // (outside this slice's hunks).
        var builder = new InstListBuilder();
        builder.Add(255, InstructionFlag.None, 0); // Unknown kind
        var slab = builder.Build();

        var ex = Assert.Throws<GameVmException>(() =>
            _codeGenerator.GenerateFromSlab(slab, new StringPool(), new CodeGenOptions()));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(ex!.ErrorCode, Is.EqualTo(ErrorCode.UnhandledLlirInstruction));
            Assert.That(ex.Message, Does.Contain("cannot size LLIR instruction '0xFF'"));
            Assert.That(ex.Message, Does.Contain("(kind 0xFF)"));
            Assert.That(ex.Context["LlirKind"], Is.EqualTo("0xFF"));
            Assert.That(ex.Context["LlirKindByte"], Is.EqualTo((byte)255));
        }
    }

    [Test]
    public void ZeroOperandJumpAndCall_AreSkippedWithoutThrowing()
    {
        // Kills line 493 (Logical): the second EmitTransfer guard is
        // `operands.Length >= 1 && currentAddress + 3 <= RomSize`. The
        // `||` mutant enters the body with zero operands and indexes
        // operands[0], throwing IndexOutOfRangeException; the real code
        // skips the transfer, the emit loop breaks, and the halt loop is
        // emitted right after the prologue.
        foreach (LlirInstructionKind kind in new[] { LlirInstructionKind.Jump, LlirInstructionKind.Call })
        {
            var builder = new InstListBuilder();
            builder.Add((byte)kind, InstructionFlag.None, 0);
            var slab = builder.Build();

            var rom = _codeGenerator.GenerateFromSlab(slab, new StringPool(), new CodeGenOptions());
            using (Assert.EnterMultipleScope())
            {
                Assert.That(rom.Length, Is.EqualTo(4096));
                Assert.That(rom[PrologueSize], Is.EqualTo(0x4C), $"{kind} with no operands must not emit; the halt loop follows the prologue");
                int loopTarget = rom[PrologueSize + 1] | (rom[PrologueSize + 2] << 8);
                Assert.That(loopTarget, Is.EqualTo(0xF000 + PrologueSize), "halt loop points to itself");
            }
        }
    }

    [Test]
    public void TwoOperandJumpFittingExactlyAtRomEnd_Emits()
    {
        // Reachable-boundary pin for line 486: a two-operand Jump whose
        // three bytes end exactly at offset 4089 — the last address the
        // RomSizeExceeded pre-check admits (codeEnd + 3 must stay
        // <= 4092) — must still emit. The guard's own boundary
        // (currentAddress + 3 == RomSize, start 4093) is unreachable:
        // no program can place an instruction there without tripping
        // the pre-check, so the Arithmetic and Equality mutants on this
        // guard are equivalent (see FIX-NOTES.md); this pins the
        // reachable edge instead.
        var builder = new InstListBuilder();
        for (int i = 0; i < 2035; i++)
            builder.Add((byte)LlirInstructionKind.Load, InstructionFlag.None, 0, 0xEA); // 4070 bytes
        builder.Add((byte)LlirInstructionKind.Add, InstructionFlag.None, 0, 1); // CLC; ADC #1 (3 bytes)
        builder.Add((byte)LlirInstructionKind.Jump, InstructionFlag.None, 0, 0xF9, 0xFF); // JMP $FFF9 (3 bytes, ends 4089)
        var slab = builder.Build();

        var rom = _codeGenerator.GenerateFromSlab(slab, new StringPool(), new CodeGenOptions());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(rom.Length, Is.EqualTo(4096));
            Assert.That(rom[4086], Is.EqualTo(0x4C), "JMP must be emitted at 4086");
            Assert.That(rom[4087], Is.EqualTo(0xF9));
            Assert.That(rom[4088], Is.EqualTo(0xFF));
            Assert.That(rom[4089], Is.EqualTo(0x4C), "halt loop JMP must sit at 4089");
            int loopTarget = rom[4090] | (rom[4091] << 8);
            Assert.That(loopTarget, Is.EqualTo(0xF000 + 4089), "halt loop points to itself");
            Assert.That(rom[4092], Is.Zero); // IRQ low
            Assert.That(rom[4093], Is.EqualTo(0xF0)); // IRQ high
            Assert.That(rom[4094], Is.Zero); // Reset low
            Assert.That(rom[4095], Is.EqualTo(0xF0)); // Reset high
        }
    }

    [Test]
    public void JumpToLabelFittingExactlyAtRomEnd_Emits()
    {
        // Reachable-boundary pin for line 493: a Jump to a resolved
        // label whose three bytes end exactly at offset 4089 must still
        // emit through the single-operand guard. Same unreachable
        // boundary as the two-operand guard (start 4093); the Arithmetic
        // and Equality mutants on this guard are equivalent
        // (see FIX-NOTES.md).
        var builder = new InstListBuilder();
        for (int i = 0; i < 2035; i++)
            builder.Add((byte)LlirInstructionKind.Load, InstructionFlag.None, 0, 0xEA); // 4070 bytes
        builder.Add((byte)LlirInstructionKind.Add, InstructionFlag.None, 0, 1); // CLC; ADC #1 (3 bytes)
        builder.Add((byte)LlirInstructionKind.Jump, InstructionFlag.None, 0, 1000); // JMP label (3 bytes, ends 4089)
        builder.Add((byte)LlirInstructionKind.Label, InstructionFlag.None, 0, 1000); // label at 4089 (0 bytes)
        var slab = builder.Build();

        var rom = _codeGenerator.GenerateFromSlab(slab, new StringPool(), new CodeGenOptions());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(rom.Length, Is.EqualTo(4096));
            Assert.That(rom[4086], Is.EqualTo(0x4C), "JMP must be emitted at 4086");
            Assert.That(rom[4087], Is.EqualTo(0xF9), "label at 4089 -> $FFF9 low byte");
            Assert.That(rom[4088], Is.EqualTo(0xFF), "label at 4089 -> $FFF9 high byte");
            Assert.That(rom[4089], Is.EqualTo(0x4C), "halt loop JMP must sit at 4089");
            int loopTarget = rom[4090] | (rom[4091] << 8);
            Assert.That(loopTarget, Is.EqualTo(0xF000 + 4089), "halt loop points to itself");
        }
    }

    [Test]
    public void BranchFittingExactlyAtRomEnd_Emits()
    {
        // Reachable-boundary pin for line 545: a Branch whose two bytes
        // end exactly at offset 4089 must still emit. The guard's own
        // boundary (currentAddress + 2 == RomSize, start 4094) is
        // unreachable for the same pre-check reason, so the Arithmetic
        // and Equality mutants on this guard are equivalent
        // (see FIX-NOTES.md).
        var builder = new InstListBuilder();
        for (int i = 0; i < 2036; i++)
            builder.Add((byte)LlirInstructionKind.Load, InstructionFlag.None, 0, 0xEA); // 4072 bytes
        builder.Add((byte)LlirInstructionKind.Cmp, InstructionFlag.None, 0, 0x80, 0); // CMP $80 (2 bytes)
        builder.Add((byte)LlirInstructionKind.Branch, InstructionFlag.None, 0, 1000); // BNE (2 bytes, ends 4089)
        builder.Add((byte)LlirInstructionKind.Label, InstructionFlag.None, 0, 1000); // label at 4089 (0 bytes)
        var slab = builder.Build();

        var rom = _codeGenerator.GenerateFromSlab(slab, new StringPool(), new CodeGenOptions());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(rom.Length, Is.EqualTo(4096));
            Assert.That(rom[4087], Is.EqualTo(0xD0), "BNE (no Transition marker)");
            Assert.That(rom[4088], Is.Zero, "displacement is 0: the label sits right after the branch");
            Assert.That(rom[4089], Is.EqualTo(0x4C), "halt loop JMP must sit at 4089");
            int loopTarget = rom[4090] | (rom[4091] << 8);
            Assert.That(loopTarget, Is.EqualTo(0xF000 + 4089), "halt loop points to itself");
        }
    }
}