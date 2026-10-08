using System.Reflection;
using NUnitLite;

// GameVM's test runner. vstest's testhost deadlocks on startup in some
// environments (notably this project's dev VM); NUnitLite runs the same
// NUnit test assemblies in-process as a plain console app, no testhost,
// no sockets, no datacollector.
//
// Usage:
//   dotnet run --project test/GameVM.TestRunner
//   dotnet run --project test/GameVM.TestRunner -- --test=Full.Name.Filter
//
// Any extra args are forwarded to NUnitLite (e.g. --test, --where).
// Exit code is the total number of failed tests across all assemblies.

string[] testAssemblies =
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
];

int totalFailed = 0;
foreach (var name in testAssemblies)
{
    Console.WriteLine($"===== {name} =====");
    Assembly asm;
    try
    {
        asm = Assembly.Load(name);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"ERROR: could not load test assembly '{name}': {ex.Message}");
        totalFailed++;
        continue;
    }

    totalFailed += new AutoRun(asm).Execute(args);
    Console.WriteLine();
}

Console.WriteLine($"TOTAL FAILED: {totalFailed}");
return totalFailed;
