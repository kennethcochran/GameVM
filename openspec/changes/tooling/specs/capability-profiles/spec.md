# Capability Profiles Specification

## Purpose
Defines the GameVM "Hardware Contract" system: a hierarchy of System Specs (Profiles) L1 through L7 that form a guaranteed hardware baseline across retro gaming consoles. Developers develop against a Spec, and the compiler prevents use of features outside the chosen spec, catching porting problems at design time rather than build time. Aspirational — not yet implemented.

## Requirements

### Requirement: Hardware Contract Philosophy
GameVM MUST target System Specs rather than individual consoles, where a Spec is a guaranteed hardware baseline ensuring that a game developed against it runs on any console that fulfills the spec.

#### Scenario: Design-time portability enforcement
- **WHEN** a developer uses a feature not present in the chosen spec
- **THEN** the compiler rejects the feature usage so the problem is caught at design time rather than build time

### Requirement: Hardware Tiers L1-L7
GameVM MUST provide seven cumulative hardware tier Specs (L1 through L7), each extending the previous tier with additional capabilities and signatures.

#### Scenario: Choosing a System Spec
- **WHEN** a developer selects a System Spec at the start of a project
- **THEN** the chosen profile acts as a boundary and the compiler prevents use of features outside that spec

#### Scenario: Cumulative tier extension
- **WHEN** a project targets a higher tier
- **THEN** it relies on a stable set of signatures from all lower tiers while gaining the extended capabilities

### Requirement: Spec L1 - Bare-Metal Baseline
Spec L1 MUST provide the core execution contract where the CPU is the video card, covering timing and input as framework drivers and graphics as a developer-implemented display kernel.

#### Scenario: L1 display kernel
- **WHEN** a developer targets L1 with a racing-the-beam or DMA-feeding graphics architecture
- **THEN** the developer implements the display kernel with CPU-driven synchronization for every scanline

#### Scenario: L1 GVM-provided signatures
- **WHEN** an L1 project runs
- **THEN** GVM provides `GameVM_System_AcknowledgeInterrupt` and `GameVM_Input_PollPrimaryControllerState` signatures

#### Scenario: L1 developer-implemented signatures
- **WHEN** an L1 project runs
- **THEN** the developer implements `GameVM_Graphics_UpdateScanlineKernel` and `GameVM_Graphics_AwaitVerticalBlank`

### Requirement: Spec L2 Fixed Display and Multi-Channel IO
Spec L2 MUST extend L1 with object-based graphics, multi-channel audio synthesis, and extended input.

#### Scenario: L2 sprite and sound channel
- **WHEN** an L2 project uses graphics or audio
- **THEN** it has access to static tiles, hardware sprites, and multi-channel Pulse/Triangle/Noise synthesis

#### Scenario: L2 extended input
- **WHEN** an L2 project polls input
- **THEN** it supports an 8-way D-Pad and multiple action buttons

### Requirement: Spec L3 - Scrolling and Dynamic Viewports
Spec L3 MUST extend L2 with smooth scrolling and dynamic viewports.

#### Scenario: L3 sub-pixel scrolling
- **WHEN** an L3 project renders the background
- **THEN** it supports sub-pixel hardware scrolling and large virtual tilemaps

### Requirement: Spec L4 - Multi-Layer and FM Synthesis
Spec L4 MUST extend L3 with multiple independent background planes, higher color depth, and FM synthesis operators.

#### Scenario: L4 parallax planes
- **WHEN** an L4 project renders depth
- **THEN** it selects among multiple independent background planes and higher color depth

#### Scenario: L4 FM synthesis
- **WHEN** an L4 project synthesizes audio
- **THEN** it uses FM Synthesis operators

### Requirement: Spec L5 - Per-Sprite Transform and PCM Audio
Spec L5 MUST extend L4 with affine transformations and digital PCM audio.

#### Scenario: L5 affine transformation
- **WHEN** an L5 project transforms graphics
- **THEN** it applies affine transformations (scale, rotate, shear) to backgrounds and sprites

#### Scenario: L5 PCM audio
- **WHEN** an L5 project plays audio
- **THEN** it reproduces digital PCM (sample-based) audio

### Requirement: Spec L6 - Geometric Pipeline and Media Streaming
Spec L6 MUST extend L5 with 3D geometric primitives, high-capacity media storage, and modern analog input.

#### Scenario: L6 triangle mesh submission
- **WHEN** an L6 project renders 3D
- **THEN** it submits rasterized polygon triangle meshes with transform matrices

#### Scenario: L6 media streaming
- **WHEN** an L6 project reads data
- **THEN** it supports high-capacity CD-media data and audio streaming

#### Scenario: L6 analog input
- **WHEN** an L6 project polls input
- **THEN** it reads high-precision analog axes and triggers

### Requirement: Spec L7 - Filtered Pipeline and Vector Precision
Spec L7 MUST extend L6 with pipeline state controls including depth management, anti-aliasing/filtering, and dedicated vector/matrix arithmetic.

#### Scenario: L7 depth management
- **WHEN** an L7 project renders 3D
- **THEN** it configures a depth buffer range for Z-buffering

#### Scenario: L7 filtering
- **WHEN** an L7 project renders textured surfaces
- **THEN** it sets the texture filtering mode

#### Scenario: L7 vector precision
- **WHEN** an L7 project performs math
- **THEN** it executes dedicated high-precision vector and matrix operations

### Requirement: System Compatibility Matrix
Each supported console MUST map to its highest guaranteed hardware spec.

#### Scenario: Console-to-spec mapping
- **WHEN** a game targets a specific console
- **THEN** the compatibility matrix defines the highest spec guaranteed to run correctly, failing to lower and disabling higher-tier features

#### Scenario: Guaranteed baseline
- **WHEN** a game is developed against a spec
- **THEN** it is guaranteed to run on any console that fulfills at least that spec

### Requirement: System Extensions and Hardware Injections
GameVM MUST support optional hardware extensions, often provided by custom cartridge hardware or co-processors, that allow a project to access signatures from higher tiers without changing the base machine requirements.

#### Scenario: The DPC injection
- **WHEN** a project on an L1 console registers the `Ext.Snd.Polyphonic` injection via a custom DPC chip
- **THEN** the project remains an L1 game while the compiler allows use of higher-tier (e.g., L5-grade PCM) or multi-voice audio signatures

### Requirement: Software Fallback Resolution
When a feature is not natively supported or injected, the compiler MUST attempt to provide a software fallback, following a defined priority: Native, Injected, Emulated, Impossible.

#### Scenario: Native capability
- **WHEN** the target hardware supports a capability directly
- **THEN** the compiler allows it without emulation

#### Scenario: Injected capability
- **WHEN** a registered hardware extension provides a capability
- **THEN** the compiler allows it

#### Scenario: Emulated capability
- **WHEN** the compiler injects a software polyfill for a capability
- **THEN** it issues a performance warning

#### Scenario: Impossible capability
- **WHEN** a feature exceeds target resources
- **THEN** the compiler issues an error

### Requirement: Advisory Enforcement Mode
The profile configured in `gamevm.yaml` MUST default to a strict contract, but the compiler MUST allow an advisory mode for experimentation.

#### Scenario: Strict enforcement
- **WHEN** enforcement is strict and a capability is only possible via fallback
- **THEN** the compiler reports an error (fallback and impossible are both errors)

#### Scenario: Advisory enforcement
- **WHEN** enforcement is advisory and a capability only possible via fallback
- **THEN** the compiler issues a warning and injects the fallback, while impossible features remain a hard-stop error