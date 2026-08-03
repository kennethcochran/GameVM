using GameVM.Compiler.Core.IR;
using GameVM.Compiler.Core.IR.Interfaces;
using GameVM.Compiler.Core.IR.Slab;
using GameVM.Compiler.Core.IR.Buffers;

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

    [Test]
    public void GenerateFromSlab_WithNullSlab_ReturnsEmptyArray()
    {
        // Arrange
        var options = new CodeGenOptions();

        // Act
        var result = _codeGenerator.GenerateFromSlab(null!, new StringPool(), options);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void GenerateFromSlab_WithEmptySlab_ReturnsEmptyArray()
    {
        // Arrange
        var options = new CodeGenOptions();
        var llirSlab = Array.Empty<uint>();

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
        var llirSlab = new uint[] 
        { 
            0x47564D56, // Magic: "GVMV"
            3,          // Stage: LLIR (3)
            1,          // Version
            2,          // Element count: 2 instructions
            0, 0, 0, 0  // Reserved
        };

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
        var header = new uint[] { 0x47564D56, 3, 1, 2, 0, 0, 0, 0 }; // 2 elements
        var loadInstr = new uint[] 
        { 
            InstructionMetadata.Encode((byte)LlirInstructionKind.Load, 2, 1), // metadata
            0x42  // immediate value 0x42
        };
        var storeInstr = new uint[]
        {
            InstructionMetadata.Encode((byte)LlirInstructionKind.Store, 3, 2), // metadata
            0x80, // address low byte
            0x00  // address high byte
        };
        
        var llirSlab = new uint[header.Length + loadInstr.Length + storeInstr.Length];
        Array.Copy(header, 0, llirSlab, 0, header.Length);
        Array.Copy(loadInstr, 0, llirSlab, header.Length, loadInstr.Length);
        Array.Copy(storeInstr, 0, llirSlab, header.Length + loadInstr.Length, storeInstr.Length);

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
        var header = new uint[] { 0x47564D56, 3, 1, 1, 0, 0, 0, 0 }; // 1 element
        var returnInstr = new uint[] 
        { 
            InstructionMetadata.Encode((byte)LlirInstructionKind.Return, 1, 0) // metadata
        };
        
        var llirSlab = new uint[header.Length + returnInstr.Length];
        Array.Copy(header, 0, llirSlab, 0, header.Length);
        Array.Copy(returnInstr, 0, llirSlab, header.Length, returnInstr.Length);

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
        var instructions = new List<uint>();
        for (int i = 0; i < 500; i++)
        {
            // LOAD instruction: metadata + 1 operand
            instructions.Add(InstructionMetadata.Encode((byte)LlirInstructionKind.Load, 2, 1));
            instructions.Add((uint)i);
        }
        
        var header = new uint[] { 0x47564D56, 3, 1, (uint)instructions.Count / 2, 0, 0, 0, 0 };
        var llirSlab = new uint[header.Length + instructions.Count];
        Array.Copy(header, 0, llirSlab, 0, header.Length);
        Array.Copy(instructions.ToArray(), 0, llirSlab, header.Length, instructions.Count);

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
