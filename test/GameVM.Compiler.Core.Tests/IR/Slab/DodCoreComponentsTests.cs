using GameVM.Compiler.Core.IR.Slab;
using GameVM.Compiler.Core.IR.Buffers;
namespace GameVM.Compiler.Core.Tests.IR.Slab;

public class LocalSlotIndexTests
{
    [Test]
    public void ForSlot_RoundTrips()
    {
        var slot = LocalSlotIndex.ForSlot(7);
        Assert.That(slot.SlotNumber, Is.EqualTo(7u));
        Assert.That(slot.IsParameter, Is.False);
        Assert.That(slot.IsSpilled, Is.False);
        Assert.That(slot.Encode(), Is.EqualTo(7u));
    }

    [Test]
    public void ForParameter_MarksParameterFlag()
    {
        var slot = LocalSlotIndex.ForParameter(3);
        Assert.That(slot.SlotNumber, Is.EqualTo(3u));
        Assert.That(slot.IsParameter, Is.True);
    }

    [Test]
    public void AsSpilled_SetsSpillFlag()
    {
        var slot = LocalSlotIndex.ForSlot(2).AsSpilled();
        Assert.That(slot.IsSpilled, Is.True);
        Assert.That(slot.SlotNumber, Is.EqualTo(2u));
    }

    [Test]
    public void Decode_RestoresAllFlags()
    {
        uint packed = LocalSlotIndex.ForParameter(5).AsSpilled().Encode();
        var decoded = LocalSlotIndex.Decode(packed);
        Assert.That(decoded.SlotNumber, Is.EqualTo(5u));
        Assert.That(decoded.IsParameter, Is.True);
        Assert.That(decoded.IsSpilled, Is.True);
    }

    [Test]
    public void None_IsSentinel()
    {
        Assert.That(LocalSlotIndex.None.IsNone, Is.True);
        Assert.That(LocalSlotIndex.None.SlotNumber, Is.EqualTo(0u));
    }

    [Test]
    public void ForSlot_OutOfRange_Throws()
    {
        Assert.Throws<System.ArgumentOutOfRangeException>(() => LocalSlotIndex.ForSlot(0x01000000));
    }
}

public class DiagnosticJournalTests
{
    [Test]
    public void Record_And_TryGet_RoundTrips()
    {
        var journal = new DiagnosticJournal();
        journal.Record(12, 100, 110, 42);

        Assert.That(journal.Count, Is.EqualTo(1));
        Assert.That(journal.Has(12), Is.True);
        Assert.That(journal.TryGet(12, out var entry), Is.True);
        Assert.That(entry.SlabOffset, Is.EqualTo(12u));
        Assert.That(entry.SourceStart, Is.EqualTo(100u));
        Assert.That(entry.SourceEnd, Is.EqualTo(110u));
        Assert.That(entry.DiagnosticCode, Is.EqualTo(42u));
    }

    [Test]
    public void Record_SameOffset_Replaces()
    {
        var journal = new DiagnosticJournal();
        journal.Record(12, 100, 110, 1);
        journal.Record(12, 200, 210, 2);

        Assert.That(journal.Count, Is.EqualTo(1));
        Assert.That(journal.TryGet(12, out var entry), Is.True);
        Assert.That(entry.DiagnosticCode, Is.EqualTo(2u));
    }

    [Test]
    public void TryGet_Missing_ReturnsFalse()
    {
        var journal = new DiagnosticJournal();
        Assert.That(journal.TryGet(99, out _), Is.False);
    }

    [Test]
    public void Remove_DeletesEntry()
    {
        var journal = new DiagnosticJournal();
        journal.Record(12, 100, 110, 1);
        Assert.That(journal.Remove(12), Is.True);
        Assert.That(journal.Count, Is.EqualTo(0));
        Assert.That(journal.Has(12), Is.False);
    }

    [Test]
    public void Record_BeyondCapacity_GrowsAndKeepsAll()
    {
        // Default capacity 16; record 20 distinct offsets to force growth.
        var journal = new DiagnosticJournal();
        for (uint i = 0; i < 20; i++)
            journal.Record(100 + i, i, i + 1, i);

        Assert.That(journal.Count, Is.EqualTo(20));
        for (uint i = 0; i < 20; i++)
        {
            Assert.That(journal.TryGet(100 + i, out var entry), Is.True);
            Assert.That(entry.DiagnosticCode, Is.EqualTo(i));
        }
    }

    [Test]
    public void Record_AfterSwapRemove_RetainsCorrectEntry()
    {
        var journal = new DiagnosticJournal();
        journal.Record(1, 0, 0, 10);
        journal.Record(2, 0, 0, 20);
        journal.Record(3, 0, 0, 30);
        journal.Remove(2); // swap-remove middle entry

        Assert.That(journal.Count, Is.EqualTo(2));
        Assert.That(journal.TryGet(1, out var e1), Is.True); Assert.That(e1.DiagnosticCode, Is.EqualTo(10u));
        Assert.That(journal.TryGet(3, out var e3), Is.True); Assert.That(e3.DiagnosticCode, Is.EqualTo(30u));
        Assert.That(journal.Has(2), Is.False);
    }
}

public class HashedSymbolTableTests
{
    [Test]
    public void Hash_IsStable()
    {
        uint foo = HashedSymbolTable.Hash("Foo".AsSpan());
        Assert.That(foo, Is.EqualTo(HashedSymbolTable.Hash("Foo".AsSpan())));
        Assert.That(foo, Is.Not.EqualTo(HashedSymbolTable.Hash("Bar".AsSpan())));
    }

    [Test]
    public void Add_And_GetOffset_RoundTrips()
    {
        var table = new HashedSymbolTable();
        table.Add("main", 64);
        Assert.That(table.Count, Is.EqualTo(1));
        Assert.That(table.TryGetOffset("main", out uint offset), Is.True);
        Assert.That(offset, Is.EqualTo(64u));
    }

    [Test]
    public void Contains_Missing_ReturnsFalse()
    {
        var table = new HashedSymbolTable();
        table.Add("main", 64);
        Assert.That(table.Contains(HashedSymbolTable.Hash("nope".AsSpan())), Is.False);
    }

    [Test]
    public void Add_Collision_PreservesBoth()
    {
        var table = new HashedSymbolTable(4);
        // Force enough entries to trigger probing/growth
        table.Add("a", 1);
        table.Add("b", 2);
        table.Add("c", 3);
        table.Add("d", 4);
        table.Add("e", 5);

        Assert.That(table.Count, Is.EqualTo(5));
        Assert.That(table.TryGetOffset("a", out uint oa), Is.True); Assert.That(oa, Is.EqualTo(1u));
        Assert.That(table.TryGetOffset("e", out uint oe), Is.True); Assert.That(oe, Is.EqualTo(5u));
    }

    [Test]
    public void Add_SameName_ReplacesOffset()
    {
        var table = new HashedSymbolTable();
        table.Add("sym", 10);
        table.Add("sym", 20);
        Assert.That(table.Count, Is.EqualTo(1));
        Assert.That(table.GetOffset(HashedSymbolTable.Hash("sym".AsSpan())), Is.EqualTo(20u));
    }
}

public class TlvSectionTests
{
    [Test]
    public void Write_Then_Read_RoundTrips()
    {
        var entry = new TlvEntry(7, new uint[] { 100, 200, 300 });
        var slab = new uint[entry.TotalWords + 4];
        entry.WriteTo(slab, 0);

        var read = TlvEntry.Read(slab, 0, out int next);
        Assert.That(read.Type, Is.EqualTo(7u));
        Assert.That(read.Length, Is.EqualTo(3u));
        Assert.That(read.Value.ToArray(), Is.EqualTo(new uint[] { 100, 200, 300 }));
        Assert.That(next, Is.EqualTo(entry.TotalWords));
    }

    [Test]
    public void Read_SkipUnknownChunk_AdvancesPastHeader()
    {
        var known = new TlvEntry(1, new uint[] { 9 });
        var slab = new uint[known.TotalWords];
        known.WriteTo(slab, 0);

        var read = TlvEntry.Read(slab, 0, out int next);
        Assert.That(read.Type, Is.EqualTo(1u));
        Assert.That(next, Is.EqualTo(known.TotalWords)); // caller can skip without decoding payload
    }

    [Test]
    public void Write_TooSmall_Throws()
    {
        var entry = new TlvEntry(1, new uint[] { 1, 2 });
        var slab = new uint[2];
        Assert.Throws<System.ArgumentException>(() => entry.WriteTo(slab, 0));
    }
}

public class SlabRelocatorTests
{
    [Test]
    public void Relocate_Unmapped_ReturnsOriginal()
    {
        var reloc = new SlabRelocator();
        Assert.That(reloc.Relocate(50), Is.EqualTo(50));
    }

    [Test]
    public void AddReloc_Then_Relocate_MapsOffset()
    {
        var reloc = new SlabRelocator();
        reloc.AddReloc(10, 40);
        Assert.That(reloc.Relocate(10), Is.EqualTo(40));
        Assert.That(reloc.HasReloc(10), Is.True);
        Assert.That(reloc.RelocCount, Is.EqualTo(1));
    }

    [Test]
    public void PatchSlab_RewritesRegisteredOperandOffsets()
    {
        // Build a slab: header(6) + block at offset 6 (size 2, operand at +1 = 7 initially 10)
        var slab = new uint[9];
        slab[0] = SlabHeader.Magic;
        slab[1] = 1; slab[2] = 0; slab[3] = 1; slab[4] = 1; slab[5] = 0;
        slab[6] = GameVM.Compiler.Core.Utilities.MetadataEncoder.Encode(1, 2, 1); // size 2
        slab[7] = 10; // operand = old offset 10
        slab[8] = 0;

        var reloc = new SlabRelocator();
        reloc.AddReloc(10, 40);

        reloc.PatchSlab(slab, new[] { 1 }); // operand at block-relative index 1

        Assert.That(slab[7], Is.EqualTo(40u));
    }

    [Test]
    public void AddReloc_BeyondCapacity_GrowsAndKeepsAll()
    {
        // Default capacity 16; register 20 relocations to force growth.
        var reloc = new SlabRelocator();
        for (int i = 0; i < 20; i++)
            reloc.AddReloc(i, i + 100);

        Assert.That(reloc.RelocCount, Is.EqualTo(20));
        for (int i = 0; i < 20; i++)
            Assert.That(reloc.Relocate(i), Is.EqualTo(i + 100));
    }

    [Test]
    public void AddReloc_DuplicateOldOffset_UpdatesNewOffset()
    {
        var reloc = new SlabRelocator();
        reloc.AddReloc(10, 40);
        reloc.AddReloc(10, 99);
        Assert.That(reloc.RelocCount, Is.EqualTo(1));
        Assert.That(reloc.Relocate(10), Is.EqualTo(99));
    }
}
