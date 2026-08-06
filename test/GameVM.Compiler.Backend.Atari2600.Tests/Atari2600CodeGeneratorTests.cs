using GameVM.Compiler.Core.IR;
using GameVM.Compiler.Core.IR.Interfaces;
using GameVM.Compiler.Core.IR.Slab;
using GameVM.Compiler.Core.IR.Buffers;
using GameVM.Compiler.Core.IR.Soa;

namespace GameVM.Compiler.Backend.Atari2600.Tests;

[TestFixture]
public class Atari2600CodeGeneratorTests
{
    private Atari2600CodeGenerator _codeGenerator;

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
        // Arrange
        var options = new CodeGenOptions();
        var llirSlab = CreateEmptyInstList();

        // Act
        var result = _codeGenerator.GenerateFromSlab(llirSlab, new StringPool(), options);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void GenerateFromSlab_WithValidSlab_ReturnsRomSize()
    {
        // Arrange
        var options = new CodeGenOptions();
        
        // Create a minimal LLIR slab with 2 instructions
        var llirSlab = new InstList(
            new byte[] { 
                0x47, 0x56, 0x4D, 0x56, // Magic: "GVMV"
                3,                       // Stage: LLIR (3)
                1,                       // Version
                2, 0                     // Element count: 2 instructions (ushort)
            },
            new ushort[] { 0x0000, 0x0000 }, // flags
            new ushort[] { 0x0000, 0x0000 }, // argCount=0
            new uint[] { 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000 }, // fixedOps (4 per instruction)
            new uint[] { }, // empty extra pool
            new uint[] { 0x00000000, 0x00000000 }, // extraOffsets
            new int[] { 0, 0 }, // blockIds
            2, // count
            0  // extraUsed
        );

        // Act
        var result = _codeGenerator.GenerateFromSlab(llirSlab, new StringPool(), options);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Length, Is.EqualTo(4096)); // Full ROM size
    }

    [Test]
    public void GenerateFromSlab_WithLoadStoreInstructions_GeneratesCorrectOpcodes()
    {
        // Arrange
        var options = new CodeGenOptions();
        
        // Build a simple LLIR slab with LOAD and STORE instructions
        // LLIR_LOAD = 0x22, LLIR_STORE = 0x24
        var llirSlab = new InstList(
            new byte[] 
            { 
                (byte)LlirInstructionKind.Load,    // 193
                (byte)LlirInstructionKind.Store    // 194
            },
            new ushort[] 
            { 
                0x0000, // flags for LOAD
                0x0000  // flags for STORE
            },
            new ushort[] 
            { 
                0x0002, // argCount=2 for LOAD (target + value)
                0x0003  // argCount=3 for STORE (target + address low + address high)
            },
            new uint[] 
            { 
                0x00000001, 0x00000042, 0x00000000, 0x00000000, // LOAD: target=1, value=0x42
                0x00000002, 0x00000080, 0x00000000, 0x00000000  // STORE: target=2, addr=0x80
            },
            new uint[] { }, // empty extra pool
            new uint[] { 0x00000000, 0x00000004 }, // extraOffsets
            new int[] { 0, 0 }, // blockIds
            2, // count
            0  // extraUsed
        );

        // Act
        var result = _codeGenerator.GenerateFromSlab(llirSlab, new StringPool(), options);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Length, Is.EqualTo(4096));
        
        // Check that the LOAD instruction generated LDA #0x42 at ROM offset 0 ($F000)
        Assert.That(result[0], Is.EqualTo(0xA9)); // LDA immediate
        Assert.That(result[1], Is.EqualTo(0x42)); // value 0x42
        
        // Check that the STORE instruction generated STA $0080 at ROM offset 2 ($F002)
        // STA zero-page (0x85) for address < 0x100
        Assert.That(result[2], Is.EqualTo(0x85)); // STA zero-page
        Assert.That(result[3], Is.EqualTo(0x80)); // address low byte
    }

    [Test]
    public void GenerateFromSlab_WithReturnInstruction_GeneratesRTS()
    {
        // Arrange
        var options = new CodeGenOptions();
        
        // LLIR_RETURN = 0x27
        var llirSlab = new InstList(
            new byte[] 
            { 
                (byte)LlirInstructionKind.Return    // 198
            },
            new ushort[] { 0x0000 }, // flags
            new ushort[] { 0x0001 }, // argCount=1 for RETURN
            new uint[] { 0x00000000, 0x00000000, 0x00000000, 0x00000000 }, // fixedOps
            new uint[] { }, // empty extra pool
            new uint[] { 0x00000000 }, // extraOffsets
            new int[] { 0 }, // blockIds
            1, // count
            0  // extraUsed
        );

        // Act
        var result = _codeGenerator.GenerateFromSlab(llirSlab, new StringPool(), options);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Length, Is.EqualTo(4096));
        
        // Check that RTS was generated at ROM offset 0 ($F000)
        Assert.That(result[0], Is.EqualTo(0x60)); // RTS
    }

    [Test]
    public void GenerateFromSlab_WithLargeCode_HandlesBoundsCorrectly()
    {
        // Arrange
        var options = new CodeGenOptions();
        
        // Build a slab with many instructions to test bounds
        var instructions = new List<byte>();
        var fixedOps = new List<uint>();
        var flags = new List<ushort>();
        var argCounts = new List<ushort>();
        var extraOffsets = new List<uint>();
        var blockIds = new List<int>();
        
        uint extraUsed = 0;
        for (int i = 0; i < 500; i++)
        {
            // LOAD instruction: metadata + 1 operand
            instructions.Add((byte)LlirInstructionKind.Load);
            flags.Add(0x0000);
            argCounts.Add(0x0002); // target + value
            fixedOps.Add((uint)i); // target register
            fixedOps.Add((uint)i); // immediate value
            fixedOps.Add(0x00000000);
            fixedOps.Add(0x00000000);
            extraOffsets.Add(extraUsed);
            blockIds.Add(0);
        }
        
        var llirSlab = new InstList(
            instructions.ToArray(),
            flags.ToArray(),
            argCounts.ToArray(),
            fixedOps.ToArray(),
            Array.Empty<uint>(), // empty extra pool
            extraOffsets.ToArray(),
            blockIds.ToArray(),
            500, // count
            0    // extraUsed
        );

        // Act
        var result = _codeGenerator.GenerateFromSlab(llirSlab, new StringPool(), options);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Length, Is.EqualTo(4096)); // Should always return full ROM size
        
        // Verify reset vectors are set correctly even with large code
        // Vectors are at the end of ROM (indices 4092-4095 correspond to $FFFC-$FFFF)
        const int VectorBaseOffset = 0x0FFC; // 4092
        Assert.That(result[VectorBaseOffset], Is.EqualTo(0x00)); // IRQ vector low
        Assert.That(result[VectorBaseOffset + 1], Is.EqualTo(0xF0)); // IRQ vector high  
        Assert.That(result[VectorBaseOffset + 2], Is.EqualTo(0x00)); // Reset vector low
        Assert.That(result[VectorBaseOffset + 3], Is.EqualTo(0xF0)); // Reset vector high
    }
}