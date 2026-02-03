# Atari 5200 SuperSystem

## System Overview
- Profile: GV.Spec.L3
- CPU: Custom MOS Technology 6502C
- Clock Speed: 1.79 MHz
- Release Year: 1982
- Generation: 3rd
- Region: North America
- Predecessor: Atari 2600
- Successor: Atari 7800

## CPU Details
### Architecture Characteristics
- Instruction Set Family: 6502
- Word Size: 8-bit
- Endianness: Little
- Register Set:
  - General Purpose: A, X, Y
  - Special Purpose: SP, PC, Status (P)
  - Status Flags: N, V, -, B, D (disabled in 5200 BIOS), I, Z, C

### Memory Access
- Address Bus Width: 16-bit
- Data Bus Width: 8-bit
- Memory Page Size: 256 bytes
- Zero Page: 0x0000-0x00FF
- Special Addressing Modes: Absolute, Indexed, Indirect, Zero Page
- DMA Capabilities: Yes (ANTIC co-processor features DMA for graphics fetching)

### Performance Considerations
- Instruction Timing: 2-7 cycles
- Pipeline Features: None
- Known Bottlenecks: Memory access contention with ANTIC DMA
- Optimization Opportunities: Zero Page usage; ANTIC Display List interrupts (DLI)

## Memory Map
### RAM
- Total Size: 16 KB
- Layout: 0x0000-0x3FFF
- Bank Switching: No
- Access Speed: 1 cycle
- Constraints: Shared with video display logic

### ROM
- Total Size: 2 KB (BIOS) + Cartridges up to 32 KB
- Bank Switching Capabilities: Cartridge dependent
- Access Speed: 1 cycle
- Special Features: Bio-OS handles self-test and basic I/O

### Special Memory Regions
- VRAM: Shared main RAM
- Audio Memory: N/A
- I/O Registers: 0xC000-0xDFFF (ANTIC, GTIA, POKEY)
- System Vectors: 0xFFF0-0xFFFF

## Video System
### Display Characteristics
- Resolution: up to 320x192 (2 color), 160x192 (4 color), 80x192 (16 color)
- Color Depth: 128 or 256 colors total palette
- Refresh Rate: 60 Hz (NTSC)
- Video RAM: Shared (defined by ANTIC)

### Graphics Capabilities
- Sprite Support: Yes (Player-Missile Graphics)
  - Max Sprites: 4 Players (8-bit) + 4 Missiles (2-bit)
  - Sprite Size: Variable height (128 or 256 lines)
  - Colors per Sprite: 1 color each
  - Limitations: Horizontal position only in registers
- Background Layers: 1 (Playfield)
  - Number of Layers: 1
  - Tile Size: 8x8 or 8x10 pixels (Character modes)
- Special Effects: Fine and coarse scrolling (H/V), Display List interrupts for mid-frame color/mode changes

### Timing
- VBLANK Duration: approx. 1 ms
- HBLANK Duration: Standard
- Access Windows: Unrestricted (Shared memory)

## Audio System
### Audio Hardware
- Sound Channels: 4 (via POKEY chip)
- Sample Rate: Variable (Digital-to-Analog conversion)
- Bit Depth: 8-bit
- Audio Memory: N/A

### Channel Types
- Square Wave: 4 channels
- Noise: Integrated via poly-counters
- Sample Playback: Possible via volume manipulation
- Special Features: Frequency and distortion control per channel

## System Timing
### Interrupts
- Types Available: IRQ, NMI (DLI and VBI)
- Sources: VBLANK, ANTIC DLI, Timers
- Timing: Triggered by hardware
- Priority: NMI > IRQ

## GameVM Implementation Notes
### Compiler Considerations
- **Tier Assignment**: **GV.Spec.L3**. 
- **Rationale**: The **ANTIC** chip provides dedicated hardware for smooth fine-scrolling (HSCROL/VSCROL), and the **POKEY** chip provides 4-channel, high-accuracy audio. This fulfills the L3 "Scrolling Bridge" and "Advanced Audio" contracts natively.
- Preferred Code Generation Strategy: Native (6502)
- Register Allocation Strategy: Minimal (A, X, Y)
- Memory Management Strategy: Static 16 KB
- Optimization Opportunities: Use of ANTIC Display Lists to offload logic

### Performance Targets
- Minimum Frame Rate: 60 fps
- Audio Update Frequency: 60 Hz
- Memory Budget: 16 KB RAM

## References
- Atari 5200 Technical Reference Manual
- ANTIC/GTIA Chip Specifications
- Mapping the Atari (for 8-bit computer comparison)
