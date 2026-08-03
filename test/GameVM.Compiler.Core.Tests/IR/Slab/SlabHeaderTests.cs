using GameVM.Compiler.Core.IR.Slab;

namespace GameVM.Compiler.Core.Tests.IR.Slab;

public class SlabHeaderTests
{
    [Test]
    public void ForStage_BuildsHeaderWithMagicAndStage()
    {
        var header = SlabHeader.ForStage(1, 10);
        Assert.That(header.MagicNumber, Is.EqualTo(SlabHeader.Magic));
        Assert.That(header.IrStage, Is.EqualTo(1u));
        Assert.That(header.ElementCount, Is.EqualTo(10u));
        Assert.That(header.Major, Is.EqualTo(SlabHeader.CurrentMajorVersion));
    }

    [Test]
    public void WriteTo_Then_Read_RoundTrips()
    {
        var header = SlabHeader.ForStage(2, 5, symbolTableOffset: 40);
        var slab = new uint[6];
        header.WriteTo(slab);

        var read = SlabHeader.Read(slab);
        Assert.That(read.MagicNumber, Is.EqualTo(SlabHeader.Magic));
        Assert.That(read.IrStage, Is.EqualTo(2u));
        Assert.That(read.ElementCount, Is.EqualTo(5u));
        Assert.That(read.SymbolTableOffset, Is.EqualTo(40u));
    }

    [Test]
    public void HeaderIndex_Length_IsSix()
    {
        Assert.That(SlabHeader.HeaderIndex.Length, Is.EqualTo(6));
    }

    [Test]
    public void Validate_WithWrongMagic_Throws()
    {
        var slab = new uint[6];
        slab[0] = 0xDEADBEEF; // wrong magic
        slab[1] = 1;
        slab[2] = 0;
        slab[3] = 1;
        slab[4] = 0;
        slab[5] = 0;

        var header = SlabHeader.Read(slab);
        Assert.Throws<System.InvalidOperationException>(() => header.Validate(6));
    }

    [Test]
    public void Validate_WithElementCountExceedingSlab_Throws()
    {
        var slab = new uint[8];
        slab[0] = SlabHeader.Magic;
        slab[1] = 1;
        slab[2] = 0;
        slab[3] = 1;
        slab[4] = 100; // claims 100 elements but slab only 8 long
        slab[5] = 0;

        var header = SlabHeader.Read(slab);
        Assert.Throws<System.InvalidOperationException>(() => header.Validate(8));
    }

    [Test]
    public void Validate_WithValidHeader_Passes()
    {
        var slab = new uint[10];
        slab[0] = SlabHeader.Magic;
        slab[1] = 1;
        slab[2] = 0;
        slab[3] = 1;
        slab[4] = 2; // 6 header + 2 elements = 8 <= 10
        slab[5] = 0;

        var header = SlabHeader.Read(slab);
        Assert.DoesNotThrow(() => header.Validate(10));
    }

    [Test]
    public void Read_WithTooShortSlab_Throws()
    {
        Assert.Throws<System.ArgumentException>(() => SlabHeader.Read(new uint[3]));
    }

    [Test]
    public void WriteTo_WithTooShortSlab_Throws()
    {
        var header = SlabHeader.ForStage(0, 0);
        Assert.Throws<System.ArgumentException>(() => header.WriteTo(new uint[3]));
    }
}
