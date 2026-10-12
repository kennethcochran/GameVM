using GameVM.Analyzers;
using NUnit.Framework;

namespace GameVM.DevTools.Tests;

[TestFixture]
public class ValueTypeEnforcementAnalyzerTests
{
    [Test]
    public void ParseNamespaces_CommaSeparated_TrimsAndFiltersEmpty()
    {
        var result = ValueTypeEnforcementAnalyzer.ParseNamespaces("A.B, C.D ,,E.F");
        Assert.That(result, Is.EqualTo(new[] { "A.B", "C.D", "E.F" }));
    }

    [Test]
    public void IsOrUnderNamespace_ExactMatch_ReturnsTrue()
    {
        Assert.That(ValueTypeEnforcementAnalyzer.IsOrUnderNamespace("A.B", "A.B"), Is.True);
    }

    [Test]
    public void IsOrUnderNamespace_Nested_ReturnsTrue()
    {
        Assert.That(ValueTypeEnforcementAnalyzer.IsOrUnderNamespace("A.B.C", "A.B"), Is.True);
    }

    [Test]
    public void IsOrUnderNamespace_PrefixWithoutDot_ReturnsFalse()
    {
        Assert.That(ValueTypeEnforcementAnalyzer.IsOrUnderNamespace("A.BC", "A.B"), Is.False);
    }

    [Test]
    public void IsNamespaceTargeted_NullConfig_ReturnsFalse()
    {
        Assert.That(ValueTypeEnforcementAnalyzer.IsNamespaceTargeted("A.B", null), Is.False);
    }

    [Test]
    public void IsNamespaceTargeted_MatchingTarget_ReturnsTrue()
    {
        Assert.That(ValueTypeEnforcementAnalyzer.IsNamespaceTargeted("A.B.C", "X.Y,A.B"), Is.True);
    }

    [Test]
    public void IsNamespaceTargeted_NoMatch_ReturnsFalse()
    {
        Assert.That(ValueTypeEnforcementAnalyzer.IsNamespaceTargeted("X.Y", "A.B"), Is.False);
    }
}
