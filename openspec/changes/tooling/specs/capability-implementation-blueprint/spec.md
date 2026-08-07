# Capability Implementation Blueprint Specification

## Purpose
Defines the technical implementation requirements for enforcing Capability Profiles across the GameVM toolchain, ensuring that the compiler, Standard Library, and HAL share a unified capability awareness during the compilation pipeline. Aspirational — not yet implemented.

## Requirements

### Requirement: Capability Meta-Tagging
All IR nodes and library functions MUST be tagged with their required extension code so the compiler can enforce capability compatibility.

#### Scenario: MLInstruction extension metadata
- **WHEN** an `MLInstruction` is defined in the MidLevelIR
- **THEN** it carries a `RequiredExtension` property declaring the extension code it requires

#### Scenario: Worked-extension attribute
- **WHEN** a compiler processes Standard Library implementations tagged with C# attributes
- **THEN** it maps the attribute-declared requirements into the corresponding IR extension metadata

### Requirement: Capability Verifier Integration
The compiler MUST integrate an `ICapabilityVerifier` service into the `CompileUseCase.Execute` pipeline.

#### Scenario: Profile selection from build options
- **WHEN** the `CompileUseCase` pipeline reads the desired profile/extension string from the upgraded `CompilationOptions`
- **THEN** that profile is used as the allowed capability set for the build

#### Scenario: Verification between HLIR and LLIR conversion
- **WHEN** code is converted to MidLevelIR but not yet transformed to LowLevelIR
- **THEN** the verifier walks the MidLevelIR graph and matches each `MLInstruction.RequiredExtension` against the allowed project set

#### Scenario: Fallback on mismatch
- **WHEN** an instruction's required extension is not in the allowed project set
- **THEN** the verifier queries the `IEmulationLibrary` for a fallback implementation

#### Scenario: Hard capability error
- **WHEN** a required extension is not in the allowed set and no fallback exists
- **THEN** the compiler throws a `HardwareCapabilityException`

### Requirement: Vendor Extension Handling
The compiler MUST allow restricted intrinsic instructions when the vendor (`Z`) extension is detected for a target.

#### Scenario: Vendor extensions enable intrinsic opcodes
- **WHEN** the `Z` extension (e.g., `Zatari`) is detected for the target
- **THEN** the compiler permits otherwise-restricted intrinsic instructions, and the target code generator emits optimized native opcodes (e.g., specific TIA strobes) valid only for that target

### Requirement: Backend Capability Self-Reporting
Each backend code generator MUST report its own capabilities and supported extensions through the code generator interface, providing accurate dynamic capability information.

#### Scenario: Backend reports capability profile
- **WHEN** a backend code generator is queried for its capability profile
- **THEN** it reports its base capability level and supported extensions (e.g., Atari2600 reports `BaseLevel = L1` with an optional DPC extension)

#### Scenario: Backend reports supported extensions
- **WHEN** a backend code generator is queried for its supported extensions
- **THEN** it returns the list of extension codes it supports (e.g., Genesis reports `BaseLevel = L4`, SNES reports `BaseLevel = L5` with Mode 7 capabilities)

### Requirement: Interface-Based HAL Abstraction
The HAL MUST be refactored from a flat structure into a set of capability interfaces extending a base hardware extension interface.

#### Scenario: Capability interface hierarchy
- **WHEN** a hardware capability such as tiling is declared
- **THEN** it is expressed as a capability interface deriving from `IHardwareExtension`, such as `ITileExtension` with `SetTile`

### Requirement: Modular Standard Library Assembly
The Standard Library source code MUST be organized by extension, and the language frontend MUST selectively include units based on the active capability string.

#### Scenario: Extension-selective unit inclusion
- **WHEN** the Standard Library is compiled for a given capability string
- **THEN** the language frontend includes only standard library units whose extensions are in the active capability string

### Requirement: Emulation Library Fallbacks
The compiler MUST provide an emulation library containing generic MidLevelIR implementations of premium extensions as software fallbacks.

#### Scenario: Software math fallback
- **WHEN** the `SoftwareMath` (M) extension is not natively supported
- **THEN** emulation provides long division and multiplication routines for 8-bit targets

#### Scenario: Software wavetable fallback
- **WHEN** the `SoftwareWavetable` (W) extension is not natively supported
- **THEN** emulation provides software-mixing logic for targets with only digital (DAC) output

### Requirement: Deployment Pipeline Capability Enforcement
The toolchain MUST enforce capability usage end-to-end, from frontend scanning through analysis, backend transition, and code generation.

#### Scenario: Frontend extension scanning
- **WHEN** the frontend scans source
- **THEN** it detects extension-specific calls (e.g., `Draw3D`) and inline LLIR blocks (`asm { ... }`)

#### Scenario: Pipeline validation
- **WHEN** the `CompileUseCase` analyzes the scanned usage
- **THEN** it validates the usage against the project's capability profile

#### Scenario: Backend lowering of inline LLIR
- **WHEN** the backend transitions MidLevelIR to LowLevelIR
- **THEN** the mid-to-low level transformer lowers both compiler-generated LLIR and developer-written inline LLIR

#### Scenario: Final code generation
- **WHEN** machine code is emitted
- **THEN** the target emitter (e.g., `M6502Emitter`) produces the final machine code