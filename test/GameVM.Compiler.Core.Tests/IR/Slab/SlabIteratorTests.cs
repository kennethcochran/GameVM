using GameVM.Compiler.Core.IR.Slab;
using GameVM.Compiler.Core.Utilities;
using NUnit.Framework;

using GameVM.Compiler.Core.IR.SlabProcessing;
namespace GameVM.Compiler.Core.Tests.IR.Slab;

public class SlabIteratorTests
{
    private static uint[] BuildSlab(params uint[] instructions)
    {
        // 6-index header prefix + instruction blocks
        var slab = new uint[6 + instructions.Length];
        slab[0] = 0x4741564D; // magic
        slab[1] = 1;           // major version
        slab[2] = 0;           // minor version
        slab[3] = 1;           // IR stage
        slab[4] = (uint)instructions.Length; // element count
        slab[5] = 0;           // symbol table offset
        Array.Copy(instructions, 0, slab, 6, instructions.Length);
        return slab;
    }

    [Test]
    public void Constructor_WithValidSlab_SkipsHeader()
    {
        var slab = BuildSlab(MetadataEncoder.CreateNop());
        var iterator = new SlabIterator(slab);
        // First MoveNext should land past the header (index 6)
        Assert.That(iterator.MoveNext(), Is.True);
        Assert.That(iterator.CurrentInstruction.Kind, Is.EqualTo(0));
    }

    [Test]
    public void Constructor_WithNullSlab_ThrowsArgumentNullException()
    {
        Assert.Throws<System.ArgumentNullException>(() => new SlabIterator(null!));
    }

    [Test]
    public void Constructor_WithSlabBelowHeaderLength_ThrowsArgumentException()
    {
        Assert.Throws<System.ArgumentException>(() => new SlabIterator(new uint[3]));
    }

    [Test]
    public void MoveNext_EmptySlabAfterHeader_ReturnsFalse()
    {
        var slab = BuildSlab(); // header only, no instructions
        var iterator = new SlabIterator(slab);
        Assert.That(iterator.MoveNext(), Is.False);
    }

    [Test]
    public void MoveNext_SingleInstruction_IteratesExactlyOnce()
    {
        var slab = BuildSlab(MetadataEncoder.Encode(5, 1, 0));
        var iterator = new SlabIterator(slab);

        Assert.That(iterator.MoveNext(), Is.True);
        Assert.That(iterator.CurrentInstruction.Kind, Is.EqualTo(5));

        Assert.That(iterator.MoveNext(), Is.False);
    }

    [Test]
    public void MoveNext_MultipleInstructions_IteratesSequentially()
    {
        var slab = BuildSlab(
            MetadataEncoder.Encode(1, 1, 0),
            MetadataEncoder.Encode(2, 1, 0),
            MetadataEncoder.Encode(3, 1, 0));
        var iterator = new SlabIterator(slab);

        Assert.That(iterator.MoveNext(), Is.True);
        Assert.That(iterator.CurrentInstruction.Kind, Is.EqualTo(1));

        Assert.That(iterator.MoveNext(), Is.True);
        Assert.That(iterator.CurrentInstruction.Kind, Is.EqualTo(2));

        Assert.That(iterator.MoveNext(), Is.True);
        Assert.That(iterator.CurrentInstruction.Kind, Is.EqualTo(3));

        Assert.That(iterator.MoveNext(), Is.False);
    }

    [Test]
    public void MoveNext_VariableSizeBlocks_AdvancesByDecodedSize()
    {
        // Block of size 2 (metadata + 1 operand) followed by size-1 block
        var slab = BuildSlab(
            MetadataEncoder.Encode(7, 2, 1),
            0xABCD,
            MetadataEncoder.Encode(8, 1, 0));
        var iterator = new SlabIterator(slab);

        Assert.That(iterator.MoveNext(), Is.True);
        Assert.That(iterator.CurrentInstruction.Kind, Is.EqualTo(7));

        Assert.That(iterator.MoveNext(), Is.True);
        Assert.That(iterator.CurrentInstruction.Kind, Is.EqualTo(8));

        Assert.That(iterator.MoveNext(), Is.False);
    }

    [Test]
    public void MoveNext_TerminatorFlag_Detected()
    {
        var slab = BuildSlab(MetadataEncoder.Encode(9, 1, 0, isTerminator: true));
        var iterator = new SlabIterator(slab);

        Assert.That(iterator.MoveNext(), Is.True);
        Assert.That(iterator.CurrentInstruction.Flags.IsTerminator, Is.True);
    }

    [Test]
    public void MoveNext_NonTerminatorFlag_DetectedAsFalse()
    {
        var slab = BuildSlab(MetadataEncoder.Encode(9, 1, 0, isTerminator: false));
        var iterator = new SlabIterator(slab);

        Assert.That(iterator.MoveNext(), Is.True);
        Assert.That(iterator.CurrentInstruction.Flags.IsTerminator, Is.False);
    }

    [Test]
    public void MoveNext_DiagnosticFlag_Detected()
    {
        var slab = BuildSlab(MetadataEncoder.Encode(10, 1, 0, hasDiagnostic: true));
        var iterator = new SlabIterator(slab);

        Assert.That(iterator.MoveNext(), Is.True);
        Assert.That(iterator.CurrentInstruction.Flags.HasDiagnostic, Is.True);
    }

    [Test]
    public void MoveNext_DiagnosticFlag_AbsentDetectedAsFalse()
    {
        var slab = BuildSlab(MetadataEncoder.Encode(10, 1, 0, hasDiagnostic: false));
        var iterator = new SlabIterator(slab);

        Assert.That(iterator.MoveNext(), Is.True);
        Assert.That(iterator.CurrentInstruction.Flags.HasDiagnostic, Is.False);
    }

    [Test]
    public void MoveNext_BlockExceedingSlab_ThrowsInvalidOperationException()
    {
        // Header (6) + metadata claiming size 5, but only 1 uint follows -> block runs past slab end
        var slab = new uint[8];
        slab[0] = 0x4741564D;
        slab[1] = 1;
        slab[2] = 0;
        slab[3] = 1;
        slab[4] = 1;
        slab[5] = 0;
        slab[6] = MetadataEncoder.Encode(4, 5, 0); // claims size 5 starting at index 6

        var iterator = new SlabIterator(slab);
        Assert.Throws<System.InvalidOperationException>(() => iterator.MoveNext());
    }

    [Test]
    public void MoveNext_NopInstruction_IteratesAsValidInstruction()
    {
        var slab = BuildSlab(MetadataEncoder.CreateNop());
        var iterator = new SlabIterator(slab);

        Assert.That(iterator.MoveNext(), Is.True);
        Assert.That(iterator.CurrentInstruction.Kind, Is.EqualTo(0));
        Assert.That(MetadataDecoder.IsNop(MetadataEncoder.CreateNop()), Is.True);
    }
}
