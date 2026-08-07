# HAL Interfaces Specification

## Purpose
Defines the GameVM Hardware Abstraction Layer (HAL) interface that provides a unified interface for accessing hardware-specific functionality across different retro gaming consoles while abstracting the differences between target platforms. Aspirational — not yet implemented.

## Requirements

### Requirement: Design Philosophy
The HAL SHALL adhere to five core principles: (1) Hardware-Agnostic Interface — common operations work across all targets; (2) Platform-Specific Extensions — access to unique hardware features when needed; (3) Performance-Optimized — superinstructions for timing-critical operations; (4) Developer-Friendly — intuitive APIs that map to hardware concepts; (5) Extensible — easy to add support for new platforms.

#### Scenario: Hardware-agnostic common operations
- **WHEN** a program uses a HAL operation
- **THEN** common operations SHALL work across all supported targets.

#### Scenario: Platform-specific extensions
- **WHEN** a program needs a unique hardware feature of a specific target
- **THEN** the HAL SHALL provide access through platform-specific extensions.

### Requirement: Abstraction Levels
The HAL SHALL provide three abstraction levels: a Level 1 cross-platform HAL (display output, input handling, audio generation, memory management, timing functions available on all platforms), a Level 2 platform-specific HAL (target-specific extensions and optimizations such as Atari 2600 TIA registers, Genesis VDP features, N64 RDP capabilities, PlayStation GPU commands), and a Level 3 hardware-level access allowing direct hardware access through inline assembly (memory-mapped I/O, hardware registers, timing-critical operations, custom protocols).

#### Scenario: Cross-platform HAL operations
- **WHEN** a program performs display, input, audio, memory, or timing operations
- **THEN** it SHALL do so through the Level 1 cross-platform interface available on all platforms.

#### Scenario: Platform-specific features
- **WHEN** a program requires target-specific features
- **THEN** it SHALL access them through the Level 2 platform-specific HAL with extensions and optimizations for that platform.

#### Scenario: Direct hardware access
- **WHEN** a program needs direct access to hardware registers, memory-mapped I/O, or custom protocols
- **THEN** it SHALL use Level 3 inline assembly access.

### Requirement: Cross-Platform Display HAL
The HAL SHALL provide a cross-platform display interface with ClearScreen, SetPixel, DrawLine, DrawRect, DrawText, and a FlipBuffer double-buffering operation. Platform-specific implementations SHALL be able to extend or override this interface (e.g., an Atari2600DisplayHAL providing WriteTIARegister, SetPlayfieldColor, SetPlayerSprite, SetBackgroundColor).

#### Scenario: Cross-platform display operations
- **WHEN** a program draws to the display
- **THEN** the cross-platform Display HAL SHALL provide ClearScreen, SetPixel, DrawLine, DrawRect, DrawText, and double-buffered FlipBuffer operations.

#### Scenario: Platform-specific display extensions
- **WHEN** a target provides unique display hardware (e.g., Atari 2600 TIA)
- **THEN** the platform's Display HAL SHALL provide the corresponding TIA register, playfield color, player sprite, and background color operations.

### Requirement: Cross-Platform Input HAL
The HAL SHALL provide a cross-platform input interface with get controller state, button-pressed query, axis value query, and rumble. Platform-specific implementations SHALL extend this (e.g., a GenesisInputHAL providing six-button state and controller type selection).

#### Scenario: Cross-platform input operations
- **WHEN** a program reads controller input
- **THEN** the cross-platform Input HAL SHALL provide GetControllerState, IsButtonPressed, GetAxisValue, and RumbleController.

#### Scenario: Platform-specific input extensions
- **WHEN** a platform has unique input capabilities
- **THEN** the platform-specific Input HAL SHALL provide extensions such as six-button state and controller type selection.

### Requirement: Cross-Platform Audio HAL
The HAL SHALL provide a cross-platform audio interface with PlaySound, PlayMusic, StopSound, StopMusic, SetVolume, and SetPan operations. Platform-specific implementations SHALL extend this (e.g., an N64AudioHAL adding sample loading, frequency setting, and reverb enabling).

#### Scenario: Cross-platform audio operations
- **WHEN** a program plays or stops audio
- **THEN** the cross-platform Audio HAL SHALL provide PlaySound, PlayMusic, StopSound, StopMusic, SetVolume, and SetPan.

#### Scenario: Platform-specific audio extensions
- **WHEN** a target has unique audio capabilities
- **THEN** the platform-specific Audio HAL SHALL provide extensions such as sample loading, frequency setting, and reverb enabling.

### Requirement: Platform-Specific HAL Implementation
The HAL SHALL provide platform-specific HAL types for Atari 2600, Genesis, N64, and PlayStation, each exposing the hardware operations of that platform. Selected kernel and hardware operations SHOULD be provided as superinstructions.

#### Scenario: Atari 2600 HAL
- **WHEN** a program targets the Atari 2600
- **THEN** the Atari 2600 HAL SHALL provide display kernels (DisplayKernel, VBlankKernel, OverscanKernel), TIA register read/write, timing (WaitForHBlank, WaitForVBlank, DelayCycles), and sprite positioning operations.

#### Scenario: Genesis HAL
- **WHEN** a program targets the Genesis
- **THEN** the Genesis HAL SHALL provide VDP register read/write, VRAM address/data access, DMA transfer, and VBlank/HBlank interrupt enable/disable operations.

#### Scenario: N64 HAL
- **WHEN** a program targets the N64
- **THEN** the N64 HAL SHALL provide RDP register, RDP command buffer and flush, RSP microcode loading and execution, and RDRAM read/write and controller-pak read/write operations.

#### Scenario: PlayStation HAL
- **WHEN** a program targets the PlayStation
- **THEN** the PlayStation HAL SHALL provide GPU command/register/packet operations, SPU register, data loading, volume, and CD-ROM sector/track/status operations.

### Requirement: HAL Superinstructions
The HAL SHALL provide optimized superinstructions for common hardware operations, grouped into display-kernel superinstructions (e.g., display kernels for Atari 2600, Genesis VBlank/HBlank interrupts, N64 display/command processors, PlayStation GPU packet processors), hardware access superinstructions (memory-mapped I/O, timing, and interrupt operations), and DMA superinstructions.

#### Scenario: Display kernel superinstructions
- **WHEN** a program performs target display-line scheduling logic (kernels) or display pipeline operations
- **THEN** the HAL SHALL provide the corresponding display-kernel superinstructions for the target platform.

#### Scenario: Memory-mapped I/O and timing superinstructions
- **WHEN** a program performs memory-mapped I/O, timing waits, or delays
- **THEN** the HAL SHALL provide WriteMemoryMappedIO, ReadMemoryMappedIO, and (word) variants, WaitForVerticalBlank, WaitForHorizontalBlank, DelayMicroseconds, and DelayCycles superinstructions.

#### Scenario: Interrupt superinstructions
- **WHEN** a program manages hardware interrupts
- **THEN** the HAL SHALL provide EnableInterrupt, DisableInterrupt, SetInterruptVector, and AcknowledgeInterrupt.

#### Scenario: DMA superinstructions
- **WHEN** a program performs DMA transfers
- **THEN** the HAL SHALL provide StartDMATransfer, StartVRAMToVRAMDMA, StartRAMToVRAMDMA, StartVRAMToRAMDMA, IsDMAComplete, and WaitForDMAComplete superinstructions.

### Requirement: Platform Selection and Initialization
The HAL SHALL provide platform selection at initialization, instantiating the HAL implementation matching the target platform (Atari2600, Genesis, N64, PlayStation), and SHALL expose a runtime function returning the current platform type to enable platform-specific behavior.

#### Scenario: HAL platform selection
- **WHEN** a program begins using the HAL
- **THEN** the HAL SHALL instantiate the implementation matching the target platform (Atari2600, Genesis, N64, or PlayStation).

#### Scenario: Runtime platform detection
- **WHEN** a program needs to branch on the runtime platform
- **THEN** the HAL SHALL provide a GetCurrentPlatform/GetPlatformType function and allow downcasting to the platform-specific HAL for platform-only features.

### Requirement: HAL Development Guidelines
The HAL SHALL follow performance, memory, and timing guidelines: use built-in superinstructions for timing-critical operations, minimize HAL calls by caching frequently accessed values, batch similar operations together, use platform-specific optimization, use predefined memory regions, use DMA transfers for large data movements, consider cache behavior and alignment, use cycle-accurate superinstructions, minimize interrupt latency, use VBlank time for background processing, and maintain consistent frame rates.

#### Scenario: Performance-optimized HAL use
- **WHEN** a developer writes HAL code
- **THEN** it SHOULD use superinstructions, cache frequently accessed values, and batch similar operations, and SHALL be able to rely on platform-specific optimizations.

### Requirement: HAL Testing and Validation
The HAL SHALL be testable through unit tests, integration tests, and hardware tests. The HAL SHALL verify display, input, display-kernel, and DMA operations through tests that mock or simulate platform behavior.

#### Scenario: Unit and integration tests
- **WHEN** the HAL is validated
- **THEN** tests SHALL verify display (ClearScreen, pixel write/read), input (button presses), display kernels (frame completion), and DMA transfer completion behavior.