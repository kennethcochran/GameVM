namespace GameVM.CrapGate.Core;

/// <summary>
/// Computes CRAP (Change Risk Anti-Patterns) scores.
/// CRAP(m) = complexity(m)² × (1 - coverage(m))³ + complexity(m)
/// A high score means code that is BOTH complex and untested.
/// </summary>
public static class CrapCalculator
{
    public static double Compute(int complexity, double coverage)
    {
        if (complexity < 1)
            throw new ArgumentOutOfRangeException(nameof(complexity), "Complexity must be at least 1");
        if (coverage < 0 || coverage > 1)
            throw new ArgumentOutOfRangeException(nameof(coverage), "Coverage must be between 0 and 1");

        return complexity * complexity * Math.Pow(1 - coverage, 3) + complexity;
    }

    public static bool ExceedsThreshold(int complexity, double coverage, double threshold) =>
        Compute(complexity, coverage) > threshold;
}
