using GameVM.TestRunner.Core;

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
//
// NOTE: explicit Main (not top-level statements). The Microsoft Testing
// Platform MSBuild target generates a competing entry point
// (MicrosoftTestingPlatformEntryPoint) despite
// <UseMicrosoftTestingPlatformRunner>false</UseMicrosoftTestingPlatformRunner>;
// with top-level statements the compiler emits CS7022 for the ambiguity.
// An explicit Main removes the ambiguity at the source.

internal static class Program
{
    internal static int Main(string[] args)
    {
        var runner = new AssemblyTestRunner(new NUnitLiteExecutor());
        return runner.Run(AssemblyTestRunner.DefaultAssemblies, args);
    }
}
