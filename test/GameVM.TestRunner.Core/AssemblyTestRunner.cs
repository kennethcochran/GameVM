using System.Reflection;

namespace GameVM.TestRunner.Core;

/// <summary>
/// Runs NUnit test assemblies and aggregates results.
/// The default assembly list covers all GameVM test projects.
/// </summary>
public sealed class AssemblyTestRunner
{
    public static readonly string[] DefaultAssemblies =
    [
        "GameVM.Compile.Tests",
        "GameVM.Compiler.Application.Tests",
        "GameVM.Compiler.Backend.Atari2600.Tests",
        "GameVM.Compiler.Core.Tests",
        "GameVM.Compiler.Interaction.Tests",
        "GameVM.Compiler.Optimizers.LowLevel.Tests",
        "GameVM.Compiler.Optimizers.MidLevel.Tests",
        "GameVM.Compiler.Pascal.Tests",
        "GameVM.Compiler.Specs",
        "GameVM.DevTools.Tests",
        "GameVM.TestRunner.Tests",
    ];

    private readonly ITestExecutor _executor;
    private readonly Action<string> _log;

    public AssemblyTestRunner(ITestExecutor executor, Action<string>? log = null)
    {
        _executor = executor;
        _log = log ?? Console.WriteLine;
    }

    /// <summary>
    /// Runs each named assembly. Returns total failed test count.
    /// Unloadable assemblies count as 1 failure each.
    /// </summary>
    public int Run(IEnumerable<string> assemblyNames, string[] args)
    {
        var totalFailed = 0;
        foreach (var name in assemblyNames)
        {
            _log($"===== {name} =====");
            Assembly asm;
            try
            {
                asm = Assembly.Load(name);
            }
            catch (Exception ex)
            {
                _log($"ERROR: could not load test assembly '{name}': {ex.Message}");
                totalFailed++;
                continue;
            }
            totalFailed += _executor.Execute(asm, args);
            _log("");
        }
        _log($"TOTAL FAILED: {totalFailed}");
        return totalFailed;
    }
}
