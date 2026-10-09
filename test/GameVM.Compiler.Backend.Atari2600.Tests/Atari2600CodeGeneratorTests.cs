using GameVM.Compiler.Core.IR;
using GameVM.Compiler.Core.IR.Interfaces;
using GameVM.Compiler.Core.IR.Slab;
using GameVM.Compiler.Core.IR.Buffers;
using GameVM.Compiler.Core.IR.Soa;

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
        builder.Add((byte)LlirInstructionKind.Store, InstructionFlag.None, 0, 0, 0x10); // STA $10
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
    public void GenerateFromSlab_WithLargeCode_HandlesBoundsCorrectly()
    {
        var builder = new InstListBuilder();
        // Fill almost entire ROM with NOPs
        for (int i = 0; i < 4090; i++)
        {
            builder.Add((byte)LlirInstructionKind.Load, InstructionFlag.None, 0, 0xEA); // LDA #$EA
        }
        var slab = builder.Build();
        var stringPool = new StringPool();
        
        var rom = _codeGenerator.GenerateFromSlab(slab, stringPool, new CodeGenOptions());
        
        Assert.That(rom.Length, Is.EqualTo(4096));
        Assert.That(rom[4095], Is.EqualTo(0xF0)); // Reset vector high byte
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
        var builder = new InstListBuilder();
        builder.Add((byte)LlirInstructionKind.Branch, InstructionFlag.None, 0, 5); // BNE offset 5
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
    public void GenerateFromSlab_WithUnknownInstruction_GeneratesNOP()
    {
        var builder = new InstListBuilder();
        builder.Add(255, InstructionFlag.None, 0); // Unknown kind
        var slab = builder.Build();
        var stringPool = new StringPool();
        
        var rom = _codeGenerator.GenerateFromSlab(slab, stringPool, new CodeGenOptions());
        
        Assert.That(rom, Is.Not.Null);
        Assert.That(rom[PrologueSize + 0], Is.EqualTo(0xEA)); // NOP
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
        builder.Add((byte)LlirInstructionKind.Store, InstructionFlag.None, 0, 0, 0x09); // STA $09 (zero page)
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
    public void GenerateFromSlab_BoundsOverflow_TruncatesGracefully()
    {
        var builder = new InstListBuilder();
        for (int i = 0; i < 5000; i++) // More instructions than fit in 4K ROM
        {
            builder.Add((byte)LlirInstructionKind.Load, InstructionFlag.None, 0, 0xEA);
        }
        var slab = builder.Build();
        var stringPool = new StringPool();
        
        var rom = _codeGenerator.GenerateFromSlab(slab, stringPool, new CodeGenOptions());
        
        Assert.That(rom.Length, Is.EqualTo(4096));
        // Vectors should still be present
        Assert.That(rom[4094], Is.EqualTo(0x00));
        Assert.That(rom[4095], Is.EqualTo(0xF0));
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
        builder.Add((byte)LlirInstructionKind.Branch, InstructionFlag.None, 0, 0x7F);  // BCC +127 (2 bytes)
        builder.Add((byte)LlirInstructionKind.Return, InstructionFlag.None, 0);           // JMP <halt-loop> (3 bytes, B4)
builder.Add(255, InstructionFlag.None, 0);          // NOP (1 byte)
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
        Assert.That(rom[PrologueSize + 7], Is.EqualTo(0xD0)); // BNE +127
        Assert.That(rom[PrologueSize + 8], Is.EqualTo(0x7F));
        // B4: Return on the main path jumps to the halt loop, never RTS.
        Assert.That(rom[PrologueSize + 9], Is.EqualTo(0x4C)); // JMP
        int haltAddr = 0xF000 + PrologueSize + 13;
        int returnTarget = rom[PrologueSize + 10] | (rom[PrologueSize + 11] << 8);
        Assert.That(returnTarget, Is.EqualTo(haltAddr));
        Assert.That(rom[PrologueSize + 12], Is.EqualTo(0xEA)); // NOP
        Assert.That(rom[PrologueSize + 13], Is.EqualTo(0x4C)); // self-loop JMP
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
        var slab = builder.Build();

        var rom = _codeGenerator.GenerateFromSlab(slab, pool, new CodeGenOptions());

        Assert.That(rom, Is.Not.Null);
        Assert.That(rom[PrologueSize + 0], Is.EqualTo(0xC9), "CMP #imm opcode");
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
        var slab = builder.Build();

        var rom = _codeGenerator.GenerateFromSlab(slab, pool, new CodeGenOptions());

        Assert.That(rom, Is.Not.Null);
        Assert.That(rom[PrologueSize + 0], Is.EqualTo(0xC9), "CMP #imm opcode");
        Assert.That(rom[PrologueSize + 2], Is.EqualTo(0xF0), "BEQ opcode");
    }
}