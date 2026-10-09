using System.Reflection;
using NUnitLite;

namespace GameVM.TestRunner.Core;

/// <summary>
/// Executes NUnit tests in an assembly via NUnitLite.
/// </summary>
public interface ITestExecutor
{
    /// <summary>Runs all tests in the assembly. Returns the number of failed tests.</summary>
    int Execute(Assembly assembly, string[] args);
}

/// <summary>Production executor using NUnitLite's AutoRun.</summary>
public sealed class NUnitLiteExecutor : ITestExecutor
{
    public int Execute(Assembly assembly, string[] args) =>
        new AutoRun(assembly).Execute(args);
}
