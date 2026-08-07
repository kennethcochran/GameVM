# Test Specification

## Purpose

Defines test cases and validation criteria for the GameVM cross-compiler, covering unit, behavioral, and performance testing on hardware-accurate emulators.
Aspirational — not yet implemented.

## Requirements

### Requirement: Unit Tests
Unit tests MUST verify the execution model and compiler emission in isolation.

#### Scenario: Instruction verification
- **WHEN** the LLIR execution model is verified
- **THEN** every instruction MUST be checked against a reference simulator, including edge cases such as accumulator overflow where `A=0xFF` plus `0x01` yields `A=0x00` and sets the Carry flag

#### Scenario: Dispatch table generation
- **WHEN** a dispatch technique (DTC, ITC, TTC) is chosen
- **THEN** tests MUST ensure the compiler emits a valid jump table for that dispatch technique

### Requirement: Behavioral Tests (MAME)
Emitted binaries MUST behave correctly on hardware-accurate emulators.

#### Scenario: Visual regression (NTSC golden frames)
- **WHEN** a ROM is executed against an emulator frame expectations
- **THEN** each expected frame MUST be compared against a golden screenshot with a specified tolerance, and any mismatch MUST fail the test

#### Scenario: Memory snapshot validation
- **WHEN** a ROM is executed for a given elapsed time
- **THEN** the memory at a specified address MUST equal the expected data bytes, else the test fails

### Requirement: Performance and Cycle Targets
Promoted intrinsics and superinstructions MUST meet their timing budgets.

#### Scenario: Superinstruction cycle counts
- **WHEN** a promoted intrinsic is executed under a given dispatch profile
- **THEN** its executed cycles MUST be at or below the defined maximum cycle budget

### Requirement: Test Environment
The test environment MUST meet defined requirements.

#### Scenario: Environment requirements
- **WHEN** tests are executed
- **THEN** MAME 0.250 or later MUST be available for CLI-based headless execution, the host toolchain MUST be C# (.NET 8.0) or C++17 depending on backend, and target profiles MUST provide hardware-specific ROM headers and memory maps

### Requirement: Test Execution
Tests MUST be executable via a CLI.

#### Scenario: CLI verification
- **WHEN** a ROM is verified via CLI
- **THEN** the `gamevm-test` command MUST execute the ROM for the given target and verify the expected register or memory state, failing if it does not match

### Requirement: Test Maintenance
Tests dependent on exact hardware timing MUST be handled deterministically.

#### Scenario: Flaky hardware timing
- **WHEN** a test depends on cycle-exact hardware behavior such as "Racing the Beam"
- **THEN** it MUST be flagged with `@timing_sensitive`, and emulators MUST be set to deterministic mode, disabling the dynamic recompiler when necessary