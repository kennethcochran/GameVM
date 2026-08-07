# Internal Assembly API Specification

## Purpose
Define the internal C# DSL (Domain Specific Language) that lets the compiler generate target-native machine code patterns directly from C# at compile-time, eliminating the need for external cross-assemblers or separate assembly files for each backend. Inspired by the HotSpot Template Interpreter (JVM) approach, it emits the interpreter's inner loop and instruction handlers as optimized machine-code blocks tailored to each game target.
Aspirational — not yet implemented.

## Requirements

### Requirement: Target-Native Emission from C#
The compiler MUST generate target-native machine instructions directly from C#-based architecture-specific API modules, eliminating the need to maintain or bundle target-specific cross-assemblers or separate assembly files per backend.

#### Scenario: Direct opcode emission
- **WHEN** the compiler needs to emit an instruction for a target architecture
- **THEN** it MUST emit the machine code directly through the corresponding C# API method (e.g., `Assembly6502.LDX_Indirect` emits `0xA1 ...`, `JMP_Indirect` emits `0x6C ...`)

### Requirement: Architecture-Specific API Modules
Each target architecture MUST expose a static API class whose methods correspond directly to that architecture's physical opcodes, and MUST provide higher-level template methods composed from those opcodes.

#### Scenario: Architecture module per backend
- **WHEN** targeting a supported architecture (6502, Z80, MIPS, SH-2)
- **THEN** the architecture MUST be implemented as its own static API class providing a method per physical opcode plus higher-level template compositions

#### Scenario: Template composition
- **WHEN** a template such as the standardized `NEXT` loop is emitted for 6502 (`EmitNextLoop`)
- **THEN** it MUST compose the opcode methods in sequence (LDA from the PC pointer, increment PC, store the dispatch pointer, then jump indirect through the dispatch pointer)

### Requirement: Template Interpreter Design
The run-time loop's inner dispatch and per-instruction handlers MUST be emitted as direct, non-switched machine code adapted to the target game, avoiding slow, C-switch-based dispatch loops, and MUST be written in C# so the emission logic is version-controlled, tightly integrated with the compiler's IR mapping, and micro-optimizable per target.

#### Scenario: Direct non-switched loops
- **WHEN** the interpreter's inner loop and instruction handlers are emitted
- **THEN** they MUST be produced as direct, non-switched machine-code blocks with no reliance on a C-switch-based dispatch loop

#### Scenario: Micro-optimized addressing modes
- **WHEN** the compiler emits an instruction handler
- **THEN** it MUST choose the most efficient addressing mode for that target (e.g., Zero-Page addressing on the 6502) based on whole-program analysis

### Requirement: No External Assemblers
GameVM MUST NOT require maintaining or bundling target-specific cross-assemblers for any supported backend.

#### Scenario: Self-contained emission
- **WHEN** a build targets a supported platform
- **THEN** the build MUST complete without an external cross-assembler, because the C# pipeline emits the hardware-native code directly

### Requirement: Consistent Toolchain
The same C# toolchain that compiles the source-language frontends (e.g., Python or Pascal) MUST also generate the final hardware-native entry point.

#### Scenario: Unified emission path
- **WHEN** any supported frontend source is compiled to a target
- **THEN** the same C# toolchain generates both the parsed IR and the final hardware-native entry point, keeping the emission logic version-controlled and consistent with the compiler's IR mapping

### Requirement: Testable Instruction Mapping
The API-based emission MUST be unit-testable in isolation so the bytecode-to-native mapping for every instruction can be verified.

#### Scenario: Isolated verification
- **WHEN** an instruction's native mapping is added or changed
- **THEN** the bytecode-to-native mapping for that instruction MUST be verifiable in isolation through C# unit tests

### Requirement: Cross-Architecture Interpreter Abstraction (Future)
Following the architecture-specific API phase, GameVM MUST provide an IR-neutral "Interpreter Specification" (e.g., a High-Level Interpreter Definition) that the compiler lowers automatically to each architecture's templates, providing a generic interpreter definition that lowers to 6502, Z80, or MIPS templates.

#### Scenario: IR-neutral interpreter definitions
- **WHEN** a developer defines VM logic in an IR-neutral way (e.g., "add the value at R0 to A") through the interpreter specification
- **THEN** the compiler MUST lower that definition automatically to the corresponding architecture-specific Assembly APIs / templates (e.g., 6502, Z80, or MIPS)