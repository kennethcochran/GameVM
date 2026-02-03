# Atari XEGS

## System Overview
- Profile: GV.Spec.L3
- CPU: MOS Technology 6502C
- Clock Speed: 1.79 MHz
- Release Year: 1987
- Generation: 3rd
- Region: Worldwide
- Predecessor: Atari 65XE (Computer roots back to 400/800)
- Successor: N/A (Atari Jaguar was the next console)

## CPU Details
### Architecture Characteristics
- Instruction Set Family: 6502
- Word Size: 8-bit
- Endianness: Little
- Register Set:
  - General Purpose: A, X, Y
  - Special Purpose: SP, PC, Status (P)
  - Status Flags: N, V, -, B, D, I, Z, C

### Memory Access
- Address Bus Width: 16-bit
- Data Bus Width: 8-bit
- Memory Page Size: 256 bytes
- Zero Page: 0x0000-0x00FF
- Special Addressing Modes: Absolute, Indexed, Indirect, Zero Page
- DMA Capabilities: Yes (ANTIC chip can pause CPU for memory fetching)

### Performance Considerations
- Instruction Timing: 2-7 cycles
- Pipeline Features: None
- Known Bottlenecks: CPU is paused during ANTIC DMA
- Optimization Opportunities: Use of Display Lists in ANTIC; Zero Page usage

## Memory Map
### RAM
- Total Size: 64 KB
- Layout: 0x0000-0xFFFF (shared with OS and I/O)
- Bank Switching: Hardware registers at 0xD301 allow swapping OS ROM for RAM
- Access Speed: 1 cycle
- Constraints: Shared with ANTIC graphics DMA

### ROM
- Total Size: 32 KB (Built-in OS + BASIC + Missile Command)
- Bank Switching Capabilities: Cartridges up to 128KB+ with mappers
- Access Speed: 1 cycle
- Special Features: Self-test ROM included

### Special Memory Regions
- VRAM: Shared main RAM
- Audio Memory: N/A
- I/O Registers: 0xD000-0xD7FF
  - GTIA: 0xD000
  - POKEY: 0xD200
  - PIA: 0xD300
  - ANTIC: 0xD400

## Video System
### Display Characteristics
- Resolution: 320x192 (Monochrome), 160x192 (Standard Color), 80x192 (High Color)
- Color Depth: 256 colors total (8 hues * 16 luminances / 16 hues * 8 luminances)
- Refresh Rate: 60 Hz (NTSC) / 50 Hz (PAL)
- Video RAM: Shared (defined by ANTIC Display List)

### Graphics Capabilities
- Sprite Support: Yes (Player-Missile Graphics)
  - Max Sprites: 4 Players + 4 Missiles (can be merged into 5th player)
  - Sprite Size: 8px or 4px wide, full screen height
  - Colors per Sprite: 1 color each
  - Limitations: Horizontal position registers only; vertical positioning via CPU
- Background Layers: 1 (Playfield)
  - Number of Layers: 1 (with up to 4 colors plus background)
  - Tile Size: 8x8 pixels (Character modes)
- Special Effects: Hardware scrolling (HSCROL/VSCROL in ANTIC), Palette changes per scanline via Display List Interrupts (DLI)

### Timing
- VBLANK Duration: approx. 1 ms
- HBLANK Duration: Standard
- Access Windows: Unrestricted (CPU and ANTIC both access RAM)

## Audio System
### Audio Hardware
- Sound Channels: 4 (via POKEY chip)
- Sample Rate: Variable (Digital-to-Analog conversion)
- Bit Depth: 8-bit or 16-bit (combined channels)
- Audio Memory: N/A

### Channel Types
- Square Wave: 4 channels with poly-counters (noise generators)
- Noise: Integrated into each channel via poly-counters
- Sample Playback: Possible via volume register manipulation (4-bit PCM)
- Special Features: Independent frequency and volume registers per channel

## System Timing
### Interrupts
- Types Available: IRQ, NMI (DLI and VBI)
- Sources: VBLANK, ANTIC DLI, Timer, Keyboard, SIO
- Timing: Triggered by system events
- Priority: NMI > IRQ

## GameVM Implementation Notes
### Compiler Considerations
- Preferred Code Generation Strategy: Native (6502)
- Register Allocation Strategy: Minimalist (A, X, Y)
- Memory Management Strategy: RAM/ROM mapping control via bits at 0xD301
- Optimization Opportunities: Use ANTIC Display Lists for background logic

### Performance Targets
- Minimum Frame Rate: 60 fps (NTSC)
- Audio Update Frequency: 60 Hz
- Memory Budget: 64 KB total
- Known Limitations: Limited sprites (PM Graphics require significant CPU intervention for vertical movement)

## References
- Atari 8-bit FAQ
- Mapping the Atari (Ian Chadwick)
- Atari ANTIC/GTIA Technical Documentation
