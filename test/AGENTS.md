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
```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test test/GameVM.Compiler.Core.Tests/

# Run with coverage
dotnet test /p:CollectCoverage=true
```

