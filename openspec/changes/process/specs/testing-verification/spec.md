# Testing-Verification Specification

## Purpose
Describes the aspirational long-term testing strategy for the GameVM compiler beyond existing unit, BDD, and Lit tests: execution verification in MAME, property-based testing and fuzzing, and differential testing. Aspirational — not yet implemented.

## Requirements

### Requirement: Execution Verification via MAME Integration
The behavior of generated code MUST be verified by running it in a cycle-accurate emulator and checking hardware state after execution.
#### Scenario: Verifying hardware state after execution
- **WHEN** a test program is compiled and executed in a cycle-accurate MAME emulator
- **THEN** the hardware state (RAM, TIA registers) after execution is verified against expected results

#### Scenario: Headless automated execution workflow
- **WHEN** verifying generated code behavior
- **THEN** the Pascal program is compiled to `.bin`, MAME is launched in a headless/automated state, executed for a given number of cycles, and memory/registers are dumped for comparison

### Requirement: Property-Based Testing and Fuzzing
The compiler MUST find edge cases and internal crashes by generating large volumes of varied input.

#### Scenario: Finding internal compiler crashes via fuzzing
- **WHEN** a grammar-based fuzzer generates valid Pascal programs
- **THEN** the compiler processes them without internal errors (ICEs) or logic errors in complex transformations

### Requirement: Differential Testing
Optimizations MUST NOT change observable program behavior, verified by running the same program through the pipeline at different optimization levels and comparing end-state in the emulator.

#### Scenario: Comparing optimization levels in the emulator
- **WHEN** the same program is run through the pipeline with different optimization levels (e.g. `-O0` vs `-O3`)
- **THEN** the end-state of the program in the emulator is identical across levels, validating optimizer correctness