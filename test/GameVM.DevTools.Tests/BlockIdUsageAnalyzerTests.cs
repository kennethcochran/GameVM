using GameVM.Analyzers;
using NUnit.Framework;

namespace GameVM.DevTools.Tests;

[TestFixture]
public class BlockIdUsageAnalyzerTests
{
    [Test]
    public void IsCfgTableNamespace_ContainsCfgTable_ReturnsTrue()
    {
        Assert.That(BlockIdUsageAnalyzer.IsCfgTableNamespace("GameVM.Compiler.CfgTable"), Is.True);
    }

    [Test]
    public void IsCfgTableNamespace_NoCfgTable_ReturnsFalse()
    {
        Assert.That(BlockIdUsageAnalyzer.IsCfgTableNamespace("GameVM.Compiler.Core"), Is.False);
    }

    [Test]
    public void IsCfgTableNamespace_Empty_ReturnsFalse()
    {
        Assert.That(BlockIdUsageAnalyzer.IsCfgTableNamespace(""), Is.False);
    }

    [Test]
    public void IsCfgTableNamespace_Null_ReturnsFalse()
    {
        Assert.That(BlockIdUsageAnalyzer.IsCfgTableNamespace(null), Is.False);
    }

    [Test]
    public void IsSystemInt32_Int32InSystem_ReturnsTrue()
    {
        Assert.That(BlockIdUsageAnalyzer.IsSystemInt32("Int32", "System"), Is.True);
    }

    [Test]
    public void IsSystemInt32_Int32NotInSystem_ReturnsFalse()
    {
        Assert.That(BlockIdUsageAnalyzer.IsSystemInt32("Int32", "MyNamespace"), Is.False);
    }

    [Test]
    public void IsSystemInt32_NonInt32InSystem_ReturnsFalse()
    {
        Assert.That(BlockIdUsageAnalyzer.IsSystemInt32("String", "System"), Is.False);
    }

    [Test]
    public void IsSystemInt32_NullNamespace_ReturnsFalse()
    {
        Assert.That(BlockIdUsageAnalyzer.IsSystemInt32("Int32", null), Is.False);
    }
}
