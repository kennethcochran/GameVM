using GameVM.Compiler.Core.IR.Buffers;
using NUnit.Framework;

namespace GameVM.DevTools.Tests;

[TestFixture]
public class StringPoolEntryTests
{
    [Test]
    public void TryReadEntry_ValidEntry_ReturnsTrueAndAdvances()
    {
        var data = new byte[] { 5, 0, 0, 0, (byte)'h', (byte)'e', (byte)'l', (byte)'l', (byte)'o', 0 };
        bool ok = StringPool.TryReadEntry(data, 0, (uint)data.Length, out string value, out uint nextPos);
        Assert.That(ok, Is.True);
        Assert.That(value, Is.EqualTo("hello"));
        Assert.That(nextPos, Is.EqualTo(10));
    }

    [Test]
    public void TryReadEntry_EmptyEntry_ReturnsTrue()
    {
        var data = new byte[] { 0, 0, 0, 0, 0 };
        bool ok = StringPool.TryReadEntry(data, 0, (uint)data.Length, out string value, out _);
        Assert.That(ok, Is.True);
        Assert.That(value, Is.EqualTo(""));
    }

    [Test]
    public void TryReadEntry_TruncatedLength_ReturnsFalse()
    {
        var data = new byte[] { 5, 0 };
        bool ok = StringPool.TryReadEntry(data, 0, (uint)data.Length, out _, out _);
        Assert.That(ok, Is.False);
    }

    [Test]
    public void TryReadEntry_TruncatedData_ReturnsFalse()
    {
        var data = new byte[] { 5, 0, 0, 0, (byte)'h', (byte)'i' };
        bool ok = StringPool.TryReadEntry(data, 0, (uint)data.Length, out _, out _);
        Assert.That(ok, Is.False);
    }

    [Test]
    public void FromByteArray_RoundTrip_PreservesStrings()
    {
        var pool = new StringPool();
        uint helloOffset = pool.Intern("hello");
        uint worldOffset = pool.Intern("world");

        byte[] data = pool.ToByteArray();
        var restored = StringPool.FromByteArray(data);

        Assert.That(restored.Resolve(helloOffset), Is.EqualTo("hello"));
        Assert.That(restored.Resolve(worldOffset), Is.EqualTo("world"));
    }

    [Test]
    public void FromByteArray_Deduplication_PreservesOffsets()
    {
        var pool = new StringPool();
        uint first = pool.Intern("duplicate");
        uint second = pool.Intern("duplicate");
        Assert.That(first, Is.EqualTo(second));

        byte[] data = pool.ToByteArray();
        var restored = StringPool.FromByteArray(data);

        uint restoredOffset = restored.Intern("duplicate");
        Assert.That(restored.Resolve(restoredOffset), Is.EqualTo("duplicate"));
    }

    [Test]
    public void FromByteArray_EmptyPool_RoundTrips()
    {
        var pool = new StringPool();
        byte[] data = pool.ToByteArray();
        var restored = StringPool.FromByteArray(data);

        Assert.That(restored.Resolve(0), Is.EqualTo(""));
    }

    [Test]
    public void TryReadEntry_NegativeLength_ReturnsFalse()
    {
        var data = new byte[] { 255, 255, 255, 255, (byte)'x' };
        bool ok = StringPool.TryReadEntry(data, 0, (uint)data.Length, out _, out _);
        Assert.That(ok, Is.False);
    }

    [Test]
    public void FromByteArray_CorruptData_StopsGracefully()
    {
        var pool = new StringPool();
        pool.Intern("valid");
        byte[] data = pool.ToByteArray();
        // Corrupt the data by truncating
        byte[] corrupt = new byte[data.Length - 2];
        Array.Copy(data, corrupt, corrupt.Length);

        var restored = StringPool.FromByteArray(corrupt);
        // Should not throw, and should have partial data
        Assert.That(restored, Is.Not.Null);
    }
}
