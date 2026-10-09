using System.Runtime.InteropServices;
using GameVM.Analyzers;
using NUnit.Framework;

namespace GameVM.DevTools.Tests;

[TestFixture]
public class StructLayoutEnforcementAnalyzerTests
{
    [Test]
    public void ParseNamespaces_CommaSeparated_TrimsAndFiltersEmpty()
    {
        var result = StructLayoutEnforcementAnalyzer.ParseNamespaces("A.B, C.D ,,E.F");
        Assert.That(result, Is.EqualTo(new[] { "A.B", "C.D", "E.F" }));
    }

    [Test]
    public void IsOrUnderNamespace_ExactMatch_ReturnsTrue()
    {
        Assert.That(StructLayoutEnforcementAnalyzer.IsOrUnderNamespace("A.B", "A.B"), Is.True);
    }

    [Test]
    public void IsOrUnderNamespace_Nested_ReturnsTrue()
    {
        Assert.That(StructLayoutEnforcementAnalyzer.IsOrUnderNamespace("A.B.C", "A.B"), Is.True);
    }

    [Test]
    public void IsOrUnderNamespace_PrefixWithoutDot_ReturnsFalse()
    {
        Assert.That(StructLayoutEnforcementAnalyzer.IsOrUnderNamespace("A.BC", "A.B"), Is.False);
    }

    [Test]
    public void IsOrUnderNamespace_ShorterThanTarget_ReturnsFalse()
    {
        Assert.That(StructLayoutEnforcementAnalyzer.IsOrUnderNamespace("A", "A.B"), Is.False);
    }

    [Test]
    public void IsNamespaceTargeted_NullConfig_ReturnsFalse()
    {
        Assert.That(StructLayoutEnforcementAnalyzer.IsNamespaceTargeted("A.B", null), Is.False);
    }

    [Test]
    public void IsNamespaceTargeted_EmptyConfig_ReturnsFalse()
    {
        Assert.That(StructLayoutEnforcementAnalyzer.IsNamespaceTargeted("A.B", "  "), Is.False);
    }

    [Test]
    public void IsNamespaceTargeted_MatchingTarget_ReturnsTrue()
    {
        Assert.That(StructLayoutEnforcementAnalyzer.IsNamespaceTargeted("A.B.C", "X.Y,A.B"), Is.True);
    }

    [Test]
    public void IsNamespaceTargeted_NoMatch_ReturnsFalse()
    {
        Assert.That(StructLayoutEnforcementAnalyzer.IsNamespaceTargeted("X.Y", "A.B"), Is.False);
    }

    [Test]
    public void IsValidLayoutKind_Sequential_ReturnsTrue()
    {
        Assert.That(StructLayoutEnforcementAnalyzer.IsValidLayoutKind(LayoutKind.Sequential), Is.True);
    }

    [Test]
    public void IsValidLayoutKind_Explicit_ReturnsTrue()
    {
        Assert.That(StructLayoutEnforcementAnalyzer.IsValidLayoutKind(LayoutKind.Explicit), Is.True);
    }

    [Test]
    public void IsValidLayoutKind_Auto_ReturnsFalse()
    {
        Assert.That(StructLayoutEnforcementAnalyzer.IsValidLayoutKind(LayoutKind.Auto), Is.False);
    }

    [Test]
    public void IsValidLayoutKind_Null_ReturnsFalse()
    {
        Assert.That(StructLayoutEnforcementAnalyzer.IsValidLayoutKind(null), Is.False);
    }

    [Test]
    public void IsValidLayoutKind_NonLayoutKind_ReturnsFalse()
    {
        Assert.That(StructLayoutEnforcementAnalyzer.IsValidLayoutKind("Sequential"), Is.False);
    }
}
