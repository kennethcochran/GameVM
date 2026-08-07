# Architecture Specification

## Purpose
Describes the aspirational multi-level GameVM architecture for efficient game development across diverse retro gaming platforms: a compiler pipeline, a Hardware Abstraction Layer (HAL), and a runtime system. Aspirational — not yet implemented.

## Requirements

### Requirement: Compiler Pipeline
The compiler pipeline SHALL transform high-level source code into optimized, platform-specific code suitable for diverse gaming hardware.

#### Scenario: Compiling high-level code to platform code
- **WHEN** a developer compiles high-level source for a target platform
- **THEN** the compiler pipeline produces optimized, platform-specific code for that target

### Requirement: Hardware Abstraction Layer
GameVM MUST provide a Hardware Abstraction Layer (HAL) designed specifically for retro gaming hardware that balances abstraction with performance.

#### Scenario: Unified access to gaming hardware
- **WHEN** game code needs to address underlying console hardware
- **THEN** the HAL provides unified access without requiring platform-specific knowledge in the game

### Requirement: Tiered Abstraction Levels
The HAL MUST expose tiered abstraction levels ranging from portable high-level APIs to direct hardware access.

#### Scenario: High-level portable development
- **WHEN** a developer targets multiple platforms with a single codebase
- **THEN** high-level portable APIs are used for cross-platform development

#### Scenario: Accessing platform-specific optimizations
- **WHEN** a developer needs target-specific performance
- **THEN** mid-level APIs exposing platform-specific optimizations are available

#### Scenario: Direct hardware access
- **WHEN** a game requires precise control over hardware
- **THEN** low-level direct hardware access is available

### Requirement: Video Subsystem
The HAL MUST abstract various video chips with common primitives including sprite management, tile-based backgrounds, hardware scrolling, and color palette management.

#### Scenario: Managing sprites across video chips
- **WHEN** game code manipulates sprites (e.g. on a TIA, PPU, or VDP)
- **THEN** the video subsystem provides common sprite primitives that abstract the underlying video chip

#### Scenario: Rendering tile-based backgrounds
- **WHEN** game code renders a background using tiles
- **THEN** the video subsystem supports tile-based backgrounds with common primitives

#### Scenario: Hardware scrolling
- **WHEN** a game scrolls its display
- **THEN** the video subsystem exposes hardware scrolling primitives

#### Scenario: Palette management
- **WHEN** a game manages its color palette
- **THEN** the video subsystem provides color palette management across video hardware

### Requirement: Audio Subsystem
The system MUST provide a unified interface for different sound hardware including pulse/square wave generation, frequency modulation, and sample playback where supported.

#### Scenario: Pulse and square wave generation
- **WHEN** a game emits pulse or square wave audio
- **THEN** the audio subsystem generates the waveform through a unified sound hardware interface

#### Scenario: Frequency modulation
- **WHEN** a game uses frequency modulation for audio effects
- **THEN** the audio subsystem supports frequency modulation

#### Scenario: Sample playback
- **WHEN** a target's sound hardware supports sample playback
- **THEN** the audio subsystem supports sample playback through the unified interface

### Requirement: Input Subsystem
The input subsystem MUST normalize different controller types, including digital pad mapping, analog input scaling, and multi-player support.

#### Scenario: Normalizing controller types
- **WHEN** a player uses a controller with a non-standard type
- **THEN** the input subsystem normalizes it to a common controller abstraction

#### Scenario: Digital pad mapping
- **WHEN** a digital pad is used
- **THEN** the input subsystem maps digital pad inputs into the unified input model

#### Scenario: Analog input scaling
- **WHEN** an analog controller input is read
- **THEN** the input subsystem scales it to the expected range

#### Scenario: Multi-player input
- **WHEN** multiple players are connected
- **THEN** the input subsystem supports multi-player input handling

### Requirement: Memory Subsystem
The system MUST provide smart memory management including bank switching support, memory-mapped I/O handling, and zero-page optimization for 6502.

#### Scenario: Smart memory management
- **WHEN** game code allocates or accesses memory
- **THEN** the memory subsystem manages memory smartly for the target hardware

#### Scenario: Bank switching
- **WHEN** a game uses more memory than is directly addressable
- **THEN** the memory subsystem supports bank switching

#### Scenario: Memory-mapped I/O handling
- **WHEN** game code accesses hardware through memory-mapped I/O
- **THEN** the memory subsystem handles memory-mapped I/O correctly

#### Scenario: Zero-page optimization for 6502
- **WHEN** optimizing memory access on 6502 targets
- **THEN** the memory subsystem applies zero-page optimization

### Requirement: Zero-Cost Bytecode-Native Bridging
Interoperability between bytecode and native machine code MUST be seamless and near zero-cost: direct function calls, no marshalling overhead for primitive types, efficient object representation sharing, and register-aware calling conventions.

#### Scenario: Direct bytecode-to-native function call
- **WHEN** bytecode calls a native machine code function (or vice versa)
- **THEN** the call is made directly without costly indirection

#### Scenario: Passing primitive types across the boundary
- **WHEN** primitive-typed values cross the bytecode/native boundary
- **THEN** no marshalling overhead is incurred

#### Scenario: Sharing object representations
- **WHEN** objects are shared between bytecode and native code
- **THEN** object representation is shared efficiently between both domains

#### Scenario: Register-aware calls
- **WHEN** a call crosses the bytecode/native boundary
- **THEN** a register-aware calling convention is used

### Requirement: Runtime Memory Management
The runtime MUST provide a unified memory model across bytecode and native code with automatic stack frame alignment, shared heap management, and zero-copy data access where possible.

#### Scenario: Unified memory model
- **WHEN** bytecode and native code operate on the same data
- **THEN** a unified memory model applies across both domains

#### Scenario: Automatic stack frame alignment
- **WHEN** stack frames are created
- **THEN** stack frame alignment is handled automatically

#### Scenario: Shared heap management
- **WHEN** bytecode and native code allocate memory
- **THEN** they share a common heap management scheme

#### Scenario: Zero-copy data access
- **WHEN** data is accessed across the bytecode/native boundary
- **THEN** zero-copy data access is used where possible