# ADR-007: NUnitLite as the Default Test Runner

Date: 2026-10-08

## Status

Accepted

## Context

The project's test suite (476 tests across 10 NUnit assemblies, including
Reqnroll BDD scenarios) was executed via `dotnet test`, which shells out to
Microsoft's vstest platform. In the project's primary dev VM (and in Kenneth's
Termux environment historically), vstest's testhost process deadlocks during
startup: it loads the TestPlatform assemblies, parks all threads on futex
waits at zero CPU, and never connects back to the vstest listener. The hang
is deterministic and environmental — a minimal hello-world NUnit project
reproduces it — so no code change can fix it. `dotnet test` is hardcoded to
vstest by the .NET SDK; there is no configuration point to repoint it.

This blocked every commit: the pre-commit hook runs the full suite, and the
hook could never complete.

## Decision

We will run the test suite via a dedicated `test/GameVM.TestRunner` console
project built on NUnitLite (NUnit's self-contained runner). The runner loads
all 10 test assemblies in-process and executes them with no testhost, no
sockets, and no datacollector. The full suite runs in ~15 seconds.

- `dotnet run --project test/GameVM.TestRunner` is the documented default
  (see `test/AGENTS.md`).
- The pre-commit hook invokes the TestRunner instead of `dotnet test`.
- The GitHub Actions workflow invokes the TestRunner (wrapped in
  `dotnet-coverage collect` so SonarQube still receives coverage).
- `dotnet test` remains available for environments where vstest works, but
  it is no longer the default or the hook's mechanism.

## Consequences

- **Positive**: Commits are unblocked; the suite runs in seconds instead of
  hanging indefinitely. The runner is a plain console app with no
  environment-sensitive IPC.
- **Positive**: The CRAP gate was restored using `dotnet-coverage` (runner-
  agnostic, wraps any process) for coverage and a new Roslyn-based
  `tools/GameVM.CrapGate` tool for cyclomatic complexity. The gate computes
  CRAP = complexity² × (1 - coverage)³ + complexity per method and enforces
  the threshold of 30. This proves the user's point: CRAP needs coverage +
  complexity, and the collector is interchangeable.
- **Neutral**: Test projects retain their `Microsoft.NET.Test.Sdk` package
  references, so `dotnet test` still functions where vstest works. No test
  code was changed to accommodate the runner.

## Alternatives Considered

- **Fix vstest**: Root cause is below the observable layer in the dev VM (no
  strace, no managed stack dumps). Not actionable.
- **nunit3-console**: Standalone runner, but adds an external tool dependency;
  NUnitLite is a NuGet package in the repo's existing dependency graph.
- **Migrate to TUnit or xUnit**: Framework migration for a runner problem;
  rejected as disproportionate.
