using GameVM.Analyzers;
using NUnit.Framework;

namespace GameVM.DevTools.Tests;

[TestFixture]
public class ExhaustiveSwitchAnalyzerTests
{
    [Test]
    public void HasTooManyMissingKinds_AllMissing_ReturnsTrue()
    {
        var handled = new HashSet<string>();
        Assert.That(ExhaustiveSwitchAnalyzer.HasTooManyMissingKinds(handled), Is.True);
    }

    [Test]
    public void HasTooManyMissingKinds_NoneMissing_ReturnsFalse()
    {
        var handled = new HashSet<string>
        {
            "NOP", "LITERAL_INT", "LITERAL_STRING", "LITERAL_BOOL",
            "IDENTIFIER", "BINARY_OP", "UNARY_OP", "ASSIGNMENT",
            "VARIABLE_DECLARATION", "METHOD_CALL", "IF_STATEMENT",
            "WHILE_STATEMENT", "RETURN_STATEMENT", "BLOCK"
        };
        Assert.That(ExhaustiveSwitchAnalyzer.HasTooManyMissingKinds(handled), Is.False);
    }

    [Test]
    public void HasTooManyMissingKinds_HalfMissing_ReturnsFalse()
    {
        // 7 of 14 missing = exactly half, not "more than half"
        var handled = new HashSet<string>
        {
            "NOP", "LITERAL_INT", "LITERAL_STRING", "LITERAL_BOOL",
            "IDENTIFIER", "BINARY_OP", "UNARY_OP"
        };
        Assert.That(ExhaustiveSwitchAnalyzer.HasTooManyMissingKinds(handled), Is.False);
    }

    [Test]
    public void HasTooManyMissingKinds_MoreThanHalfMissing_ReturnsTrue()
    {
        // 8 of 14 missing = more than half
        var handled = new HashSet<string>
        {
            "NOP", "LITERAL_INT", "LITERAL_STRING", "LITERAL_BOOL",
            "IDENTIFIER", "BINARY_OP"
        };
        Assert.That(ExhaustiveSwitchAnalyzer.HasTooManyMissingKinds(handled), Is.True);
    }
}
