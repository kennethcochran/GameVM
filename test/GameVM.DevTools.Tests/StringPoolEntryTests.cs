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
}
