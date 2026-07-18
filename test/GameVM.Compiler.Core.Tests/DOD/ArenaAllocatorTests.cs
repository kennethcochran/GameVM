using GameVM.Compiler.Core.DOD;
using NUnit.Framework;

namespace GameVM.Compiler.Core.Tests.DOD;

public class ArenaAllocatorTests
{
    [Test]
    public void Constructor_WithValidChunkSize_CreatesAllocator()
    {
        var allocator = new ArenaAllocator(1024);
        Assert.That(allocator.TotalAllocated, Is.EqualTo(0));
        Assert.That(allocator.ChunkCount, Is.EqualTo(1));
    }

    [Test]
    public void Constructor_WithInvalidChunkSize_ThrowsException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ArenaAllocator(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ArenaAllocator(-1));
    }

    [Test]
    public void Allocate_SingleValue_ReturnsCorrectOffset()
    {
        var allocator = new ArenaAllocator(1024);
        uint offset = allocator.Allocate(1);
        Assert.That(offset, Is.EqualTo(0u));
        Assert.That(allocator.TotalAllocated, Is.EqualTo(1));
    }

    [Test]
    public void Allocate_MultipleValues_ReturnsSequentialOffsets()
    {
        var allocator = new ArenaAllocator(1024);
        uint offset1 = allocator.Allocate(5);
        uint offset2 = allocator.Allocate(3);
        
        Assert.That(offset1, Is.EqualTo(0u));
        Assert.That(offset2, Is.EqualTo(5u));
        Assert.That(allocator.TotalAllocated, Is.EqualTo(8));
    }

    [Test]
    public void Allocate_ExceedsChunkSize_CreatesNewChunk()
    {
        var allocator = new ArenaAllocator(10);
        allocator.Allocate(5);
        allocator.Allocate(10); // This should trigger a new chunk
        
        Assert.That(allocator.ChunkCount, Is.EqualTo(2));
        Assert.That(allocator.TotalAllocated, Is.EqualTo(15));
    }

    [Test]
    public void Allocate_WithZeroCount_ThrowsException()
    {
        var allocator = new ArenaAllocator(1024);
        Assert.Throws<ArgumentOutOfRangeException>(() => allocator.Allocate(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => allocator.Allocate(-1));
    }

    [Test]
    public void WriteAndRead_SingleValue_WorksCorrectly()
    {
        var allocator = new ArenaAllocator(1024);
        uint offset = allocator.Allocate(1);
        
        allocator.Write(offset, 42u);
        uint[] read = allocator.Read(offset, 1);
        
        Assert.That(read, Has.Length.EqualTo(1));
        Assert.That(read[0], Is.EqualTo(42u));
    }

    [Test]
    public void WriteAndRead_MultipleValues_WorksCorrectly()
    {
        var allocator = new ArenaAllocator(1024);
        uint offset = allocator.Allocate(5);
        
        allocator.Write(offset, 1u, 2u, 3u, 4u, 5u);
        uint[] read = allocator.Read(offset, 5);
        
        Assert.That(read, Has.Length.EqualTo(5));
        Assert.That(read[0], Is.EqualTo(1u));
        Assert.That(read[1], Is.EqualTo(2u));
        Assert.That(read[2], Is.EqualTo(3u));
        Assert.That(read[3], Is.EqualTo(4u));
        Assert.That(read[4], Is.EqualTo(5u));
    }

    [Test]
    public void Write_WithNullValues_ThrowsException()
    {
        var allocator = new ArenaAllocator(1024);
        uint offset = allocator.Allocate(1);
        
        Assert.Throws<ArgumentNullException>(() => allocator.Write(offset, null!));
    }

    [Test]
    public void Write_WithEmptyValues_DoesNothing()
    {
        var allocator = new ArenaAllocator(1024);
        uint offset = allocator.Allocate(1);
        
        Assert.DoesNotThrow(() => allocator.Write(offset)); // Should not throw
    }

    [Test]
    public void Read_WithInvalidCount_ThrowsException()
    {
        var allocator = new ArenaAllocator(1024);
        uint offset = allocator.Allocate(1);
        
        Assert.Throws<ArgumentOutOfRangeException>(() => allocator.Read(offset, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => allocator.Read(offset, -1));
    }

    [Test]
    public void WriteAndRead_AcrossChunkBoundaries_WorksCorrectly()
    {
        var allocator = new ArenaAllocator(10);
        uint offset1 = allocator.Allocate(8);
        uint offset2 = allocator.Allocate(5); // This will be in a new chunk
        
        allocator.Write(offset1, 1u, 2u, 3u, 4u, 5u, 6u, 7u, 8u);
        allocator.Write(offset2, 9u, 10u, 11u, 12u, 13u);
        
        uint[] read1 = allocator.Read(offset1, 8);
        uint[] read2 = allocator.Read(offset2, 5);
        
        Assert.That(read1, Has.Length.EqualTo(8));
        Assert.That(read2, Has.Length.EqualTo(5));
        Assert.That(read1[7], Is.EqualTo(8u));
        Assert.That(read2[4], Is.EqualTo(13u));
    }

    [Test]
    public void Reset_ClearsAllData()
    {
        var allocator = new ArenaAllocator(1024);
        allocator.Allocate(10);
        allocator.Write(0, 1u, 2u, 3u);
        
        allocator.Reset();
        
        Assert.That(allocator.TotalAllocated, Is.EqualTo(0));
        Assert.That(allocator.ChunkCount, Is.EqualTo(1));
        
        uint[] read = allocator.Read(0, 3);
        Assert.That(read[0], Is.EqualTo(0u));
        Assert.That(read[1], Is.EqualTo(0u));
        Assert.That(read[2], Is.EqualTo(0u));
    }

    [Test]
    public void ToContiguousArray_CreatesSingleArray()
    {
        var allocator = new ArenaAllocator(10);
        allocator.Allocate(5);
        allocator.Write(0, 1u, 2u, 3u, 4u, 5u);
        
        uint[] result = allocator.ToContiguousArray();
        
        Assert.That(result, Has.Length.EqualTo(5));
        Assert.That(result[0], Is.EqualTo(1u));
        Assert.That(result[4], Is.EqualTo(5u));
    }

    [Test]
    public void ToContiguousArray_WithMultipleChunks_MergesCorrectly()
    {
        var allocator = new ArenaAllocator(10);
        allocator.Allocate(8);
        allocator.Write(0, 1u, 2u, 3u, 4u, 5u, 6u, 7u, 8u);
        allocator.Allocate(5);
        allocator.Write(8, 9u, 10u, 11u, 12u, 13u);
        
        uint[] result = allocator.ToContiguousArray();
        
        Assert.That(result, Has.Length.EqualTo(13));
        Assert.That(result[0], Is.EqualTo(1u));
        Assert.That(result[7], Is.EqualTo(8u));
        Assert.That(result[8], Is.EqualTo(9u));
        Assert.That(result[12], Is.EqualTo(13u));
    }
}
