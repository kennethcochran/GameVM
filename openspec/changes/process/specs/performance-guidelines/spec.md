# Performance Guidelines Specification

## Purpose

Defines performance guidelines for retro and constrained hardware, where performance is a hard boundary defined by clock cycles and memory bandwidth.
Aspirational — not yet implemented.

## Requirements

### Requirement: Cycle Budgeting and Timing
Code on retro platforms MUST fit logic into deterministic time windows.

#### Scenario: Use of time windows
- **WHEN** logic is scheduled on retro hardware
- **THEN** on systems like the Atari 2600 where a scanline is exactly 76 color clocks, complex logic must complete during VBlank (~37 scanlines), and on NES/SNES/Genesis where VBlank is the only time VRAM can be updated, logic must finish fast enough to leave room for DMA transfers

#### Scenario: Timing measurement
- **WHEN** timing is measured
- **THEN** the LLIR simulator MUST be used to count instructions via host profiling, the MAME debugger MUST be used to observe actual cycle counts on target hardware, and visual profiling via a raster bar (changing background color at routine start and end) MUST be used to see execution time on-screen

### Requirement: Instruction and ISA Efficiency
Generated code MUST prefer efficient instruction patterns for the target ISA.

#### Scenario: Accumulator vs memory
- **WHEN** code targets register-poor systems such as the 6502
- **THEN** accumulator-relative patterns such as `LOAD A, [addr]` followed by `ADD [addr]` MUST be preferred over moving data between virtual registers (`R0-R15`)

#### Scenario: Intrinsic promotion
- **WHEN** a sequence of LLIR instructions can be collapsed into a single hardware opcode
- **THEN** the compiler MUST promote the sequence to a single hardware opcode when the `[Super]` attribute marks the logic as needing native speed

### Requirement: Integer, Fixed-Point, and Floating-Point
Numeric representation MUST be chosen appropriately for the target.

#### Scenario: Fixed-point preference
- **WHEN** code uses high-precision scalars on 8-bit or 16-bit targets (NES, Genesis)
- **THEN** fixed-point (8.8/16.16) MUST be preferred to avoid the cost of software-emulated floats, and used for physics, rotation, and high-precision scalars

#### Scenario: Floating-point support
- **WHEN** floating point (`TYPE_FLOAT32/64`) is used on hardware with an FPU (N64, PlayStation)
- **THEN** native instructions MUST be used for maximum performance

#### Scenario: Software-emulated floats
- **WHEN** floating point is used on 8/16-bit targets
- **THEN** the compiler MUST automatically link a software floating-point library, which is acceptable for non-critical code but discouraged for hot loops on 6502/Z80/68k

### Requirement: Memory Access Patterns
Hot variables and calls MUST exploit fast memory and avoid bank-switching penalties.

#### Scenario: Zero-page and fast-RAM allocation
- **WHEN** hot variables (e.g. player position, camera coordinates) are allocated on 6502 targets (Atari/NES)
- **THEN** the compiler MUST prioritize the faster, smaller-instruction Zero Page (first 256 bytes of RAM), and on MIPS Scratchpad targets (PS1) the 1KB Scratchpad MUST be used for temporary stack-heavy operations

#### Scenario: Bank-switch call minimization
- **WHEN** code targets systems with banking (NES, Genesis >4MB)
- **THEN** cross-bank calls MUST be minimized because they incur a significant cycle penalty for mapper switching, and related logic (e.g., all Physics and Collision) MUST be grouped within the same bank

### Requirement: Dispatch Overhead Tuning
The dispatch method MUST be chosen according to code heat.

#### Scenario: Dispatch selection
- **WHEN** dispatch is chosen for a region of code
- **THEN** STC (Native Calls) MUST be used for performance-critical kernels, DTC (Address Lists) for standard game logic, and TTC (Bytecode) for cold code where saving ROM matters more than speed

### Requirement: Retro-Parallelism
Multiprocessor coordination MUST be non-blocking.

#### Scenario: Master/slave patterns
- **WHEN** coordinating multiple CPUs such as the Sega Saturn's dual SH-2
- **THEN** bus data contention MUST be avoided by splitting workloads into independent tasks (e.g., Physics on CPU1, Animation on CPU2), and coordination MUST use shared RAM buffers with simple "Dirty Flag" or "FIFO" signaling rather than complex OS mutexes

#### Scenario: Coprocessor offloading
- **WHEN** Geometry and signal processing on PS1/N64 are offloaded to coprocessors (GTE/RSP)
- **THEN** they MUST be treated as asynchronous tasks, the CPU MUST prepare the next data packet (DMA) while the coprocessor is still crunching the current one, and vertex and command lists MUST always be double-buffered so the CPU and GPU never wait on each other

#### Scenario: DMA synchronization
- **WHEN** data is moved by DMA (VRAM, RAM, CD-ROM)
- **THEN** transfers MUST be asynchronous in the background, independent CPU work MUST be performed after initiating a transfer, and the "Transfer Complete" flag MUST be queried only at the last possible moment

### Requirement: Rendering Performance
Rendering MUST avoid byte-by-byte CPU transfers and MUST batch updates.

#### Scenario: DMA batching
- **WHEN** VRAM is updated
- **THEN** DMA transfers MUST be batched, and moving data byte-by-byte via CPU MUST be avoided as an anti-pattern on 16/32-bit systems

#### Scenario: Collision detection
- **WHEN** collision detection is performed
- **THEN** bounding box (AABB) checks MUST be done in HLIR before falling back to complex per-pixel checks in native code

#### Scenario: VBlank readiness
- **WHEN** the VBlank interrupt is about to trigger
- **THEN** all visual updates MUST be queued and ready before the VBlank interrupt fires