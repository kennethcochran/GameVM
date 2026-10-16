# GameVM Agent Instructions — test/

This file provides guidance for AI assistants and coding agents writing tests in the `test/`
folder. It supplements the [root AGENTS.md](../AGENTS.md).

## Testing Strategy

### Unit Tests (NUnit)
- Located in `test/GameVM.Compiler.*.Tests/`
- Test individual components in isolation
- Use descriptive test names following pattern: `Method_Scenario_ExpectedResult`

### BDD Tests (Reqnroll/Gherkin)
- Located in `test/GameVM.Compiler.Specs/`
- Scenario-based end-to-end tests covering language features
- Validate backend code generation
- Verify behavior correctness

### Running Tests

The default test runner is `test/GameVM.TestRunner`, a NUnitLite-based console
app that runs all test assemblies in-process. Use it instead of `dotnet test`:
vstest's testhost deadlocks on startup in some environments (notably the
project's dev VM), while the NUnitLite runner executes the same NUnit tests
directly with no testhost, no sockets, and no datacollector.

```bash
# Run all tests (default runner)
dotnet run --project test/GameVM.TestRunner

# Run with an NUnitLite filter (e.g. single fixture)
dotnet run --project test/GameVM.TestRunner -- --test=GameVM.Compiler.Core.Tests.MyFixture

# Legacy: dotnet test (vstest). Only use where vstest is known to work;
# it hangs on testhost startup in the dev VM.
dotnet test
```

### MAME Execution Scenarios

The BDD scenarios in `test/GameVM.Compiler.Specs/Features/MameExecution.feature` run
compiled ROMs in real MAME to verify behavioral correctness on the target hardware.
They require a MAME install (`dotnet run --project src/GameVM.DevTools -- mame install`);
on machines without MAME, or on headless hosts with no display, the scenarios report
**skipped**, not failed. Set `GAMEVM_SKIP_MAME=1` to force the skip (and silence the
skip message) when MAME is installed but unusable.

