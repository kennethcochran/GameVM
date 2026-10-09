using GameVM.Analyzers;
using NUnit.Framework;

namespace GameVM.DevTools.Tests;

[TestFixture]
public class LinqUsageAnalyzerTests
{
    [Test]
    public void IsSystemLinqNamespace_ExactMatch_ReturnsTrue()
    {
        Assert.That(LinqUsageAnalyzer.IsSystemLinqNamespace("System.Linq"), Is.True);
    }

    [Test]
    public void IsSystemLinqNamespace_Nested_ReturnsTrue()
    {
        Assert.That(LinqUsageAnalyzer.IsSystemLinqNamespace("System.Linq.Expressions"), Is.True);
    }

    [Test]
    public void IsSystemLinqNamespace_OtherNamespace_ReturnsFalse()
    {
        Assert.That(LinqUsageAnalyzer.IsSystemLinqNamespace("System.Collections"), Is.False);
    }

    [Test]
    public void IsSystemLinqNamespace_Null_ReturnsFalse()
    {
        Assert.That(LinqUsageAnalyzer.IsSystemLinqNamespace(null), Is.False);
    }

    [Test]
    public void IsSystemLinqNamespace_PrefixWithoutDot_ReturnsFalse()
    {
        Assert.That(LinqUsageAnalyzer.IsSystemLinqNamespace("System.LinqX"), Is.False);
    }

    [Test]
    public void IsLinqExtensionMethod_NotExtension_ReturnsFalse()
    {
        Assert.That(
            LinqUsageAnalyzer.IsLinqExtensionMethod("Where", false, "System.Collections.Generic.IEnumerable<T>", null),
            Is.False);
    }

    [Test]
    public void IsLinqExtensionMethod_NonLinqName_ReturnsFalse()
    {
        Assert.That(
            LinqUsageAnalyzer.IsLinqExtensionMethod("MyMethod", true, "System.Collections.Generic.IEnumerable<T>", null),
            Is.False);
    }

    [Test]
    public void IsLinqExtensionMethod_WhereOnIEnumerable_ReturnsTrue()
    {
        Assert.That(
            LinqUsageAnalyzer.IsLinqExtensionMethod("Where", true, "System.Collections.Generic.IEnumerable<T>", null),
            Is.True);
    }

    [Test]
    public void IsLinqExtensionMethod_SelectOnIEnumerableOriginalDef_ReturnsTrue()
    {
        Assert.That(
            LinqUsageAnalyzer.IsLinqExtensionMethod("Select", true, "System.Collections.Generic.List<T>", "System.Collections.Generic.IEnumerable<T>"),
            Is.True);
    }

    [Test]
    public void IsLinqExtensionMethod_WhereOnNonEnumerable_ReturnsFalse()
    {
        Assert.That(
            LinqUsageAnalyzer.IsLinqExtensionMethod("Where", true, "System.String", "System.String"),
            Is.False);
    }

    [Test]
    public void LinqMethodNames_ContainsCommonMethods()
    {
        Assert.That(LinqUsageAnalyzer.LinqMethodNames, Has.Member("Where"));
        Assert.That(LinqUsageAnalyzer.LinqMethodNames, Has.Member("Select"));
        Assert.That(LinqUsageAnalyzer.LinqMethodNames, Has.Member("ToList"));
    }
}
