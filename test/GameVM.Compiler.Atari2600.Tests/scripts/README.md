# GameVM Compiler Test Runner

This directory contains the test runner for the GameVM compiler using LLVM's lit testing framework.

## Prerequisites

1. **Python 3.6+** - Required to run lit
2. **pip** - Python package manager
3. **.NET 8.0 SDK** - Required to build the compiler

## Setup

1. Install lit:
   ```bash
   pip install lit
   ```

2. Make the script executable (Linux/macOS):
   ```bash
   chmod +x run-lit-tests.ps1
   ```

## Running Tests

### Windows (PowerShell)
```powershell
.\scripts\run-lit-tests.ps1
```

### Linux/macOS (PowerShell Core)
```bash
pwsh ./scripts/run-lit-tests.ps1
```

## Test Structure

- `lit-tests/` - Root test directory
  - `features/` - Tests for specific compiler features
  - `regression/` - Regression tests
  - `unit/` - Unit tests for specific components

## Writing Tests

1. Create a new test file with a `.pas` extension in the appropriate directory
2. Add test commands using `RUN:` and expected output with `CHECK:`

Example test:
```pascal
// RUN: %compile %s -o %t
// RUN: %t | FileCheck %s
// CHECK: Hello, Atari 2600!

program HelloWorld;
begin
    WriteLn('Hello, Atari 2600!');
end.
```

## Customizing Test Execution

You can pass additional arguments to lit by appending them after the script:

```powershell
.\scripts\run-lit-tests.ps1 -v  # Verbose output
.\scripts\run-lit-tests.ps1 --filter=features  # Run only tests in features directory
```

## Debugging

To debug test failures:
1. Run with `-v` for verbose output
2. Check the generated files in `lit-tests/test_output/`
3. Look for `.log` files with detailed error information
