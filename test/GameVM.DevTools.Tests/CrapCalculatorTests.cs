using GameVM.CrapGate.Core;
using NUnit.Framework;

namespace GameVM.DevTools.Tests;

[TestFixture]
public class CrapCalculatorTests
{
    [Test]
    public void Compute_SimpleMethodFullCoverage_ReturnsComplexity()
    {
        // CRAP = 1² × (1-1)³ + 1 = 1
        Assert.That(CrapCalculator.Compute(1, 1.0), Is.EqualTo(1.0));
    }

    [Test]
    public void Compute_SimpleMethodNoCoverage_ReturnsTwo()
    {
        // CRAP = 1² × (1-0)³ + 1 = 2
        Assert.That(CrapCalculator.Compute(1, 0.0), Is.EqualTo(2.0));
    }

    [Test]
    public void Compute_ComplexMethodNoCoverage_ReturnsHighScore()
    {
        // CRAP = 10² × (1-0)³ + 10 = 110
        Assert.That(CrapCalculator.Compute(10, 0.0), Is.EqualTo(110.0));
    }

    [Test]
    public void Compute_ComplexMethodFullCoverage_ReturnsComplexity()
    {
        // CRAP = 10² × (1-1)³ + 10 = 10
        Assert.That(CrapCalculator.Compute(10, 1.0), Is.EqualTo(10.0));
    }

    [Test]
    public void Compute_PartialCoverage_ReturnsIntermediateScore()
    {
        // CRAP = 5² × (1-0.5)³ + 5 = 25 × 0.125 + 5 = 8.125
        Assert.That(CrapCalculator.Compute(5, 0.5), Is.EqualTo(8.125).Within(0.001));
    }

    [Test]
    public void Compute_InvalidComplexity_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CrapCalculator.Compute(0, 0.5));
    }

    [Test]
    public void Compute_InvalidCoverage_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CrapCalculator.Compute(5, -0.1));
        Assert.Throws<ArgumentOutOfRangeException>(() => CrapCalculator.Compute(5, 1.1));
    }

    [Test]
    public void ExceedsThreshold_AboveThreshold_ReturnsTrue()
    {
        Assert.That(CrapCalculator.ExceedsThreshold(10, 0.0, 30), Is.True);
    }

    [Test]
    public void ExceedsThreshold_BelowThreshold_ReturnsFalse()
    {
        Assert.That(CrapCalculator.ExceedsThreshold(5, 1.0, 30), Is.False);
    }

    [Test]
    public void ExceedsThreshold_AtThreshold_ReturnsFalse()
    {
        // CRAP = 5, threshold 5 → not exceeding
        Assert.That(CrapCalculator.ExceedsThreshold(5, 1.0, 5), Is.False);
    }
}
