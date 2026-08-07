# Semantic Analysis Specification

## Purpose
Defines a semantic analysis capability for the GameVM compiler to be implemented at the HLIR level, providing language-agnostic type checking, cross-module reference resolution, capability-profile validation, and performance constraint checking against retro game console targets. Aspirational — not yet implemented.

## Requirements

### Requirement: HLIR-Level Semantic Analysis
GameVM MUST implement semantic analysis at the HLIR level to maintain language agnosticism while supporting the compiler's performance and capability requirements.

#### Scenario: Analyze an HLIR graph
- **WHEN** a `HighLevelIR` graph is presented to the semantic analyzer
- **THEN** the analyzer performs basic semantic validation, capability profile validation, performance constraint analysis, and cross-module reference resolution

#### Scenario: Type validation
- **WHEN** types within an HLIR graph are validated
- **THEN** the analyzer validates types against the unified type system with width awareness

#### Scenario: Capability usage validation
- **WHEN** an HLIR graph is validated against a target capability profile
- **THEN** the analyzer validates that all feature usage stays within the target profile's hardware constraints

### Requirement: Multi-Phase Analysis Strategy
Semantic analysis MUST follow a multi-phase strategy: intramodule analysis, cross-module resolution with strict DAG enforcement, and whole-program optimization.

#### Scenario: Intramodule analysis
- **WHEN** analyzing within a single module
- **THEN** the analyzer performs width-aware type checking, local symbol resolution with scope tracking, and capability and performance constraint validation for the target profile

#### Scenario: Cross-module resolution
- **WHEN** analyzing across module boundaries
- **THEN** the analyzer traverses the dependency graph with strict DAG enforcement, resolves external symbols with module boundary validation, and checks interface compatibility across language boundaries

#### Scenario: Whole-program optimization
- **WHEN** analyzing the complete program
- **THEN** the analyzer performs global type consistency checks, ABI compliance verification, superinstruction identification and promotion analysis, memory layout optimization, and cycle budget analysis

### Requirement: Performance-Aware Deferred Analysis
The analyzer MUST validate cycle budgets for time-critical code against the target profile.

#### Scenario: Cycle budget validation
- **WHEN** a function's estimated cycle usage exceeds the profile's maximum cycles per frame
- **THEN** the analyzer reports that the function exceeds the cycle budget

#### Scenario: Cycle budget success
- **WHEN** all functions' estimated cycle usage is within the profile's maximum cycles per frame
- **THEN** the analyzer reports a successful cycle budget analysis

### Requirement: Superinstruction Promotion Analysis
The analyzer MUST identify functions eligible for superinstruction promotion and estimate cycle savings.

#### Scenario: Eligible function promotes to superinstruction
- **WHEN** a function meets the superinstruction criteria
- **THEN** the analyzer marks the function for promotion, generates the superinstruction, and estimates the cycle savings

#### Scenario: Ineligible function remains a function call
- **WHEN** a function does not meet the superinstruction criteria
- **THEN** the analyzer keeps the function as a normal function call

### Requirement: Memory Layout Optimization
The analyzer MUST optimize memory layout for the target profile, applying zero-page optimization for 8-bit targets and scratchpad optimization for 32-bit targets.

#### Scenario: Zero-page optimization on 8-bit target
- **WHEN** the target profile supports a zero-page
- **THEN** the analyzer optimizes zero-page usage for the HLIR graph

#### Scenario: Scratchpad optimization on 32-bit target
- **WHEN** the target profile supports a scratchpad
- **THEN** the analyzer optimizes scratchpad usage for the HLIR graph

### Requirement: Capability-Driven Semantic Analysis
The analyzer MUST validate the HLIR graph against the target hardware contract, checking feature support and memory usage limits.

#### Scenario: Unsupported feature validation
- **WHEN** a function requires a feature not supported in the target profile
- **THEN** the analyzer reports a capability validation error naming the feature and profile

#### Scenario: Memory usage validation
- **WHEN** the total memory usage of the HLIR graph exceeds the profile's maximum memory
- **THEN** the analyzer reports a memory validation error

### Requirement: Cross-Language Type Compatibility
The analyzer MUST check type compatibility across language boundaries, mapping source types to a unified type system and validating width and memory layout compatibility.

#### Scenario: Width mismatch across languages
- **WHEN** mapped source and target types have incompatible widths
- **THEN** the analyzer reports a type width mismatch

#### Scenario: Layout incompatibility across languages
- **WHEN** mapped source and target types have incompatible memory layouts
- **THEN** the analyzer reports that the memory layout is incompatible between the source and target languages

#### Scenario: Compatible cross-language types
- **WHEN** mapped source and target types are width- and layout-compatible
- **THEN** the analyzer reports the types as compatible

### Requirement: Width-Aware Type System Integration
The analyzer MUST perform type checking against the unified type system with width awareness and implicit/explicit conversion rules for the target profile.

#### Scenario: Implicit width conversion
- **WHEN** a source width can be implicitly converted to the target width for the target profile
- **THEN** the analyzer reports the conversion as compatible

#### Scenario: Explicit conversion required
- **WHEN** a source width requires an explicit cast to convert to the target width
- **THEN** the analyzer reports that an explicit cast is required

#### Scenario: Incompatible width conversion
- **WHEN** a source width cannot be converted to the target width on the target profile
- **THEN** the analyzer reports the conversion as incompatible

### Requirement: Static Memory Layout Validation
The analyzer MUST validate static allocation patterns, rejecting dynamic allocation on profiles that do not support it and validating alignment for target hardware.

#### Scenario: Dynamic allocation on unsupported profile
- **WHEN** an allocation is dynamic and the target profile does not support dynamic allocation
- **THEN** the analyzer reports a memory layout error

#### Scenario: Misaligned allocation
- **WHEN** an allocation is not aligned for the target hardware
- **THEN** the analyzer reports a misaligned allocation error

### Requirement: GameVM-Specific Error Handling
The analyzer MUST emit gamevm-specific semantic errors with stable error codes for performance, capability, and cross-language constraint violations.

#### Scenario: Performance constraint error code
- **WHEN** a function exceeds its cycle budget or a module exceeds a memory limit
- **THEN** the analyzer emits a performance error with code `PERF_CYCLE_BUDGET` or `PERF_MEMORY_LIMIT`

#### Scenario: Capability constraint error code
- **WHEN** a feature is unsupported or an operation is invalid for the target profile
- **THEN** the analyzer emits a capability error with code `CAP_UNSUPPORTED_FEATURE` or `CAP_INVALID_OPERATION`

#### Scenario: Cross-language error code
- **WHEN** a type is incompatible or an ABI mismatch occurs across languages
- **THEN** the analyzer emits a cross-language error with code `XLANG_TYPE_MISMATCH` or `XLANG_ABI_MISMATCH`

### Requirement: Source Location Infrastructure
The semantic analyzer MUST support source location tracking implemented against the DOD pipeline, recording location metadata in the AST-stage slab instruction blocks or as a side table keyed by `InstIndex` at the HLIR stage.

#### Scenario: Location metadata as side table
- **WHEN** source locations are tracked at the HLIR stage
- **THEN** a `LocationIndex[]` side table keyed by `InstIndex` records location metadata populated through the ANTLR listener in `PascalFrontend.cs`

#### Scenario: Parse error integration
- **WHEN** the parser encounters a parse error
- **THEN** the ANTLR error listener in `PascalFrontend.cs` reports the parse error at the correct source location

### Requirement: Language Agnosticism
Semantic analysis MUST work across all supported source languages (Pascal, C#, C++, etc.) via the unified HLIR and unified type system.

#### Scenario: Multi-language analysis
- **WHEN** an HLIR graph contains code originating from different source languages
- **THEN** the analyzer applies a single language-agnostic semantic analysis, mapping types through the unified type system