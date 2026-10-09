using GameVM.Analyzers;
using NUnit.Framework;

namespace GameVM.DevTools.Tests;

[TestFixture]
public class VirtualDispatchEnforcementAnalyzerTests
{
    [Test]
    public void ParseNamespaces_CommaSeparated_TrimsAndFiltersEmpty()
    {
        var result = VirtualDispatchEnforcementAnalyzer.ParseNamespaces("A.B, C.D ,,E.F");
        Assert.That(result, Is.EqualTo(new[] { "A.B", "C.D", "E.F" }));
    }

    [Test]
    public void ParseNamespaces_EmptyString_ReturnsEmpty()
    {
        var result = VirtualDispatchEnforcementAnalyzer.ParseNamespaces("");
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void IsVirtualDispatch_Virtual_ReturnsTrue()
    {
        Assert.That(VirtualDispatchEnforcementAnalyzer.IsVirtualDispatch(true, false, false), Is.True);
    }

    [Test]
    public void IsVirtualDispatch_Abstract_ReturnsTrue()
    {
        Assert.That(VirtualDispatchEnforcementAnalyzer.IsVirtualDispatch(false, true, false), Is.True);
    }

    [Test]
    public void IsVirtualDispatch_Override_ReturnsTrue()
    {
        Assert.That(VirtualDispatchEnforcementAnalyzer.IsVirtualDispatch(false, false, true), Is.True);
    }

    [Test]
    public void IsVirtualDispatch_PlainMethod_ReturnsFalse()
    {
        Assert.That(VirtualDispatchEnforcementAnalyzer.IsVirtualDispatch(false, false, false), Is.False);
    }

    [Test]
    public void IsInTargetNamespace_ExactMatch_ReturnsTrue()
    {
        Assert.That(
            VirtualDispatchEnforcementAnalyzer.IsInTargetNamespace("A.B", new[] { "A.B" }),
            Is.True);
    }

    [Test]
    public void IsInTargetNamespace_NestedMatch_ReturnsTrue()
    {
        Assert.That(
            VirtualDispatchEnforcementAnalyzer.IsInTargetNamespace("A.B.C", new[] { "A.B" }),
            Is.True);
    }

    [Test]
    public void IsInTargetNamespace_PrefixWithoutDot_ReturnsFalse()
    {
        // "A.BC" is not under "A.B" — segment-aware matching
        Assert.That(
            VirtualDispatchEnforcementAnalyzer.IsInTargetNamespace("A.BC", new[] { "A.B" }),
            Is.False);
    }

    [Test]
    public void IsInTargetNamespace_NoMatch_ReturnsFalse()
    {
        Assert.That(
            VirtualDispatchEnforcementAnalyzer.IsInTargetNamespace("X.Y", new[] { "A.B" }),
            Is.False);
    }

    [Test]
    public void ShouldReportInvocation_NullConfig_ReturnsFalse()
    {
        Assert.That(
            VirtualDispatchEnforcementAnalyzer.ShouldReportInvocation(null, true, false, false, "A.B"),
            Is.False);
    }

    [Test]
    public void ShouldReportInvocation_EmptyTargets_ReturnsFalse()
    {
        Assert.That(
            VirtualDispatchEnforcementAnalyzer.ShouldReportInvocation(" , ", true, false, false, "A.B"),
            Is.False);
    }

    [Test]
    public void ShouldReportInvocation_NonVirtual_ReturnsFalse()
    {
        Assert.That(
            VirtualDispatchEnforcementAnalyzer.ShouldReportInvocation("A.B", false, false, false, "A.B"),
            Is.False);
    }

    [Test]
    public void ShouldReportInvocation_VirtualInTarget_ReturnsTrue()
    {
        Assert.That(
            VirtualDispatchEnforcementAnalyzer.ShouldReportInvocation("A.B", true, false, false, "A.B.C"),
            Is.True);
    }

    [Test]
    public void ShouldReportInvocation_VirtualOutsideTarget_ReturnsFalse()
    {
        Assert.That(
            VirtualDispatchEnforcementAnalyzer.ShouldReportInvocation("A.B", true, false, false, "X.Y"),
            Is.False);
    }
}
