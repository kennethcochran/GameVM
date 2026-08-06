using GameVM.Compiler.Core.IR.Slab;
using GameVM.Compiler.Core.IR.Soa;
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
        var index = InstIndex.FromInt(12);
        journal.Record(index, 100, 110, 42);

        Assert.That(journal.Count, Is.EqualTo(1));
        Assert.That(journal.Has(index), Is.True);
        Assert.That(journal.TryGet(index, out var entry), Is.True);
        Assert.That(entry.InstIndex, Is.EqualTo(index));
        Assert.That(entry.SourceStart, Is.EqualTo(100u));
        Assert.That(entry.SourceEnd, Is.EqualTo(110u));
        Assert.That(entry.DiagnosticCode, Is.EqualTo(42u));
    }

    [Test]
    public void Record_SameOffset_Replaces()
    {
        var journal = new DiagnosticJournal();
        var index = InstIndex.FromInt(12);
        journal.Record(index, 100, 110, 1);
        journal.Record(index, 200, 210, 2);

        Assert.That(journal.Count, Is.EqualTo(1));
        Assert.That(journal.TryGet(index, out var entry), Is.True);
        Assert.That(entry.DiagnosticCode, Is.EqualTo(2u));
    }

    [Test]
    public void TryGet_Missing_ReturnsFalse()
    {
        var journal = new DiagnosticJournal();
        Assert.That(journal.TryGet(InstIndex.FromInt(99), out _), Is.False);
    }

    [Test]
    public void Remove_DeletesEntry()
    {
        var journal = new DiagnosticJournal();
        var index = InstIndex.FromInt(12);
        journal.Record(index, 100, 110, 1);
        Assert.That(journal.Remove(index), Is.True);
        Assert.That(journal.Count, Is.EqualTo(0));
        Assert.That(journal.Has(index), Is.False);
    }

    [Test]
    public void Record_BeyondCapacity_GrowsAndKeepsAll()
    {
        // Default capacity 16; record 20 distinct offsets to force growth.
        var journal = new DiagnosticJournal();
        for (int i = 0; i < 20; i++)
            journal.Record(InstIndex.FromInt(100 + i), (uint)i, (uint)(i + 1), (uint)i);

        Assert.That(journal.Count, Is.EqualTo(20));
        for (int i = 0; i < 20; i++)
        {
            var index = InstIndex.FromInt(100 + i);
            Assert.That(journal.TryGet(index, out var entry), Is.True);
            Assert.That(entry.DiagnosticCode, Is.EqualTo((uint)i));
        }
    }

    [Test]
    public void Record_AfterSwapRemove_RetainsCorrectEntry()
    {
        var journal = new DiagnosticJournal();
        journal.Record(InstIndex.FromInt(1), 0, 0, 10);
        journal.Record(InstIndex.FromInt(2), 0, 0, 20);
        journal.Record(InstIndex.FromInt(3), 0, 0, 30);
        journal.Remove(InstIndex.FromInt(2)); // swap-remove middle entry

        Assert.That(journal.Count, Is.EqualTo(2));
        Assert.That(journal.TryGet(InstIndex.FromInt(1), out var e1), Is.True); Assert.That(e1.DiagnosticCode, Is.EqualTo(10u));
        Assert.That(journal.TryGet(InstIndex.FromInt(3), out var e3), Is.True); Assert.That(e3.DiagnosticCode, Is.EqualTo(30u));
        Assert.That(journal.Has(InstIndex.FromInt(2)), Is.False);
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

