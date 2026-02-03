# Performance Guidelines: Retro & Constrained Hardware

## 1. Core Philosophy
On retro platforms, performance is a hard boundary defined by clock cycles and memory bandwidth. "Optimization" is not just about speed, but about fitting the logic into deterministic time windows (vblank, hblank).

## 2. Cycle-Budgeting & Timing

### 2.1 The Time Windows
- **Atari 2600**: "Racing the Beam" means you have exactly 76 color clocks per scanline. Complex logic must happen during VBlank (~37 scanlines).
- **NES/SNES/Genesis**: VBlank is the only time you can update VRAM. Logic must complete fast enough to leave room for DMA transfers.

### 2.2 Measurement
- **Host Profiling**: Use the LLIR simulator to count instructions.
- **MAME Debugger**: Use `trace` to see actual cycle counts on target hardware.
- **Visual Profiling (Raster Bar)**: Change the background color at the start and end of a routine to see its execution time on-screen.

## 3. Instruction & ISA Efficiency

### 3.1 Accumulator vs. Memory
- **Prefer Accumulator (`A`)**: On register-poor systems like the 6502, `LOAD A, [addr]` followed by `ADD [addr]` is significantly faster than moving data between virtual registers (`R0-R15`).
- **Intrinsic Promotion**: Use the `[Super]` attribute for logic that needs native speed. If a sequence of LLIR instructions can be collapsed into a single hardware opcode, the compiler will do so.

### 3.2 Integer, Fixed-Point, and Floating-Point
- **Prefer Fixed-Point (8.8/16.16)**: Recommended for 8-bit and 16-bit targets (NES, Genesis) to avoid the high cost of software-emulated floats. Use for physics, rotation, and high-precision scalars.
- **Floating Point (TYPE_FLOAT32/64)**: 
    - **Fully Supported**: Hardware with FPUs (N64, PlayStation) uses native instructions for maximum performance.
    - **Software Emulated**: On 8/16-bit targets, the compiler automatically links a software floating-point library. Useful for non-critical code but discouraged for hot loops on 6502/Z80/68k.

## 4. Memory Access Patterns

### 4.1 Zero-Page / Fast-RAM
- **6502 (Atari/NES)**: The first 256 bytes of RAM (Zero Page) are faster and allow smaller instructions. The compiler prioritizes this for hot variables (e.g., player position, camera coords).
- **MIPS Scratchpad (PS1)**: Use the 1KB Scratchpad for temporary stack-heavy operations.

### 4.2 Bank Switching
- **Minimize Cross-Bank Calls**: On systems with banking (NES, Genesis > 4MB), calls to different banks incur a significant cycle penalty for mapper switching. Group related logic (e.g., all Physics and Collision) within the same bank.

## 5. Dispatch Overhead Tuning
Choose the right dispatch method for the code's heat:
- **STC (Native Calls)**: Performance critical kernels.
- **DTC (Address Lists)**: Standard game logic.
- **TTC (Bytecode)**: Cold code where saving ROM is more important than speed.

## 7. Retro-Parallelism (Multiprocessor Coordination)
Fifth-generation hardware often utilized multiple CPUs or specialized coprocessors. Efficiency depends on non-blocking coordination.

### 7.1 Master/Slave Patterns
- **Sega Saturn (Dual SH-2)**: Avoid data contention on the bus. Split workloads into independent tasks (e.g., Physics on CPU1, Animation on CPU2).
- **Coordination**: Use shared RAM buffers with simple "Dirty Flag" or "FIFO" signaling rather than complex OS mutexes.

### 7.2 Coprocessor Offloading
- **PS1/N64 (GTE/RSP)**: Treat Geometry and Signal Processing as asynchronous tasks. The CPU should prepare the next data packet (DMA) while the coprocessor is still crunching the current one.
- **Double Buffering**: Always double-buffer vertex and command lists to ensure the CPU and GPU are never waiting on each other.

### 7.3 DMA Synchronization
- **Asynchronous Transfers**: Use DMA to move data (VRAM, RAM, CD-ROM) in the background. 
- **Wait Optimization**: Perform independent CPU work after initiating a DMA transfer and only query the "Transfer Complete" flag at the last possible moment.

## 8. Rendering Performance
- **DMA Transfers**: Always batch VRAM updates. Moving data byte-by-byte via CPU is an anti-pattern on 16/32-bit systems.
- **Collision Detection**: Use Bounding Box (AABB) checks in HLIR before falling back to complex per-pixel checks in native code.
- **VBlank Management**: Ensure all visual updates are queued and ready before the VBlank interrupt triggers.
