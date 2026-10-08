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

var runner = new AssemblyTestRunner(new NUnitLiteExecutor());
return runner.Run(AssemblyTestRunner.DefaultAssemblies, args);
