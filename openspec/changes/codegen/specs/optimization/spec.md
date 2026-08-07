# Optimization Specification

## Purpose
Describes the aspirational GameVM optimization capabilities: developer-suggested superinstructions, automatic superinstruction detection, and optional JIT compilation targeting 5th-generation consoles. Aspirational — not yet implemented.

## Requirements

### Requirement: Developer-Suggested Superinstructions
GameVM MUST allow developers to suggest methods as candidates for superinstruction creation, similar to function inlining hints in modern languages.

#### Scenario: Suggesting a method as a superinstruction candidate
- **WHEN** a developer marks a method as a suggested superinstruction candidate
- **THEN** the compiler considers the suggestion for superinstruction generation

### Requirement: Superinstruction Suggestion Criteria
The compiler MUST consider developer suggestions alongside criteria including method complexity and size, number of parameters and locals, usage frequency in the codebase, potential performance impact, and available instruction space.

#### Scenario: Evaluating method complexity and size
- **WHEN** a candidate method is evaluated
- **THEN** its complexity and size are considered

#### Scenario: Evaluating parameters and locals
- **WHEN** a candidate method is evaluated
- **THEN** the number of parameters and locals is considered

#### Scenario: Evaluating usage frequency
- **WHEN** a candidate method is evaluated
- **THEN** its usage frequency across the codebase is considered

#### Scenario: Evaluating performance impact and instruction space
- **WHEN** a candidate method is evaluated
- **THEN** potential performance impact and available instruction space are considered

### Requirement: Automatic Superinstruction Detection
GameVM MUST automatically identify and optimize frequently occurring instruction sequences.

#### Scenario: Analyzing bytecode for common sequences
- **WHEN** bytecode is compiled
- **THEN** pattern analysis identifies common instruction sequences

#### Scenario: Creating superinstructions for frequent patterns
- **WHEN** a pattern occurs frequently enough to meet the frequency threshold
- **THEN** a superinstruction is created for the pattern

#### Scenario: Cost-benefit analysis
- **WHEN** a candidate superinstruction is considered
- **THEN** a cost-benefit analysis evaluates the trade-off between code size and speed

#### Scenario: Cross-module pattern detection
- **WHEN** patterns span multiple source files
- **THEN** cross-module analysis detects patterns across the different source files

### Requirement: Superinstruction Configuration
Superinstruction generation MUST be configurable through settings controlling the minimum frequency, maximum sequence length, and maximum number of superinstructions to generate.

#### Scenario: Configuring detection thresholds
- **WHEN** a developer configures superinstruction generation
- **THEN** the minimum frequency, maximum instruction-sequence length, and maximum number of superinstructions are honored by the detector

### Requirement: JIT Compilation Platform Support
GameVM MUST provide optional JIT compilation for platforms with sufficient resources, primarily targeting 5th-generation consoles.

#### Scenario: Enabling JIT on a capable platform
- **WHEN** a target platform has sufficient resources and JIT is enabled
- **THEN** the JIT compiler generates native code for the platform

### Requirement: Nintendo 64 JIT Compilation
The Nintendo 64 (4MB-8MB RAM, MIPS R4300i @ 93.75 MHz) MUST support full method JIT compilation with advanced register allocation, loop unrolling, constant propagation, a code cache up to 512KB, and profile-guided optimization.

#### Scenario: Full method JIT on Nintendo 64
- **WHEN** a method is JIT-compiled on the Nintendo 64
- **THEN** full method JIT compilation with advanced register allocation is performed

#### Scenario: JIT optimizations on Nintendo 64
- **WHEN** the Nintendo 64 JIT optimizes code
- **THEN** loop unrolling and constant propagation are applied and profile-guided optimization is used

#### Scenario: Nintendo 64 code cache limits
- **WHEN** JIT code is cached on the Nintendo 64
- **THEN** the code cache is limited to 512KB

### Requirement: PlayStation JIT Compilation
The Sony PlayStation (2MB RAM, MIPS R3000 @ 33.8688 MHz) MUST support basic-block JIT for hot paths with simple register allocation, delay slot optimization, limited method inlining, and a code cache limited to 128-256KB.

#### Scenario: Basic-block JIT on PlayStation
- **WHEN** hot paths are JIT-compiled on the PlayStation
- **THEN** basic-block JIT with simple register allocation is used

#### Scenario: PlayStation-specific optimizations
- **WHEN** the PlayStation JIT optimizes code
- **THEN** delay slot optimization is applied and method inlining is limited

#### Scenario: PlayStation code cache limits
- **WHEN** JIT code is cached on the PlayStation
- **THEN** the code cache is limited to 128-256KB

### Requirement: Sega Saturn JIT Compilation
The Sega Saturn (2MB RAM, 2x Hitachi SH-2 @ 28.6 MHz) MUST support basic-block JIT for critical paths with dual-CPU-aware optimization, simple method inlining, and a code cache limited to 64-128KB per CPU.

#### Scenario: Basic-block JIT on Saturn
- **WHEN** critical paths are JIT-compiled on the Saturn
- **THEN** basic-block JIT is used with dual-CPU-aware optimization

#### Scenario: Saturn method inlining
- **WHEN** the Saturn JIT inlines methods
- **THEN** simple method inlining is applied

#### Scenario: Saturn code cache limits
- **WHEN** JIT code is cached on the Saturn
- **THEN** the code cache is limited to 64-128KB per CPU