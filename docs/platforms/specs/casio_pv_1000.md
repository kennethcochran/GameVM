# Casio PV-1000

## System Overview
- Profile: GV.Spec.L2
- CPU: Zilog Z80 (NEC D780C-1)
- Clock Speed: 3.579 MHz
- Release Year: 1983
- Generation: 3rd (Lower end)
- Region: Japan
- Predecessor: N/A
- Successor: Casio PV-2000 (Computer/Console hybrid)

## CPU Details
### Architecture Characteristics
- Instruction Set Family: Z80
- Word Size: 8-bit
- Endianness: Little
- Register Set:
  - General Purpose: A, B, C, D, E, H, L (and shadow set)
  - Special Purpose: IX, IY, SP, PC, I, R
  - Status Flags: S, Z, Y, H, X, P, N, C

### Memory Access
- Address Bus Width: 16-bit
- Data Bus Width: 8-bit
- Memory Page Size: N/A
- Zero Page: N/A
- Special Addressing Modes: Relative, Indexed
- DMA Capabilities: No

### Performance Considerations
- Instruction Timing: 4-23 cycles
- Pipeline Features: None
- Known Bottlenecks: Very small RAM (2 KB)
- Optimization Opportunities: 8x8 Tile-based graphics approach

## Memory Map
### RAM
- Total Size: 2 KB
- Layout: 0x0000-0x07FF
- Bank Switching: No
- Access Speed: 1 cycle
- Constraints: Shared with basic system tasks

### ROM
- Total Size: Cartridges (usually 8 KB - 32 KB)
- Bank Switching Capabilities: None standard
- Access Speed: 1 cycle
- Special Features: N/A

### Special Memory Regions
- VRAM: 1 KB (dedicated for tile index)
- Character Generator: 1 KB (dedicated for tile patterns)
- Audio Memory: N/A
- I/O Registers: Mapped via dedicated Z80 IN/OUT instructions
- System Vectors: 0x0000 (Reset), 0x0038 (IM 1)

## Video System
### Display Characteristics
- Resolution: 240x192 (Visible) / 256x192 (Full array)
- Color Depth: 3-bit (8 colors)
- Refresh Rate: 60 Hz (NTSC)
- Video RAM: 1 KB (Index) + 1 KB (Generator)

### Graphics Capabilities
- Sprite Support: No (Software sprites only)
- Background Layers: 1 (Tile-based)
  - Number of Layers: 1
  - Tile Size: 8x8 pixels
  - Colors per Tile: 8-color palette (1 color per tile or foreground/background)
- Special Effects: No hardware scrolling

### Timing
- VBLANK Duration: approx. 1 ms
- HBLANK Duration: Standard
- Access Windows: Unrestricted

## Audio System
### Audio Hardware
- Sound Channels: 3 (Custom NEC D65010G031 logic)
- Sample Rate: N/A
- Bit Depth: 6-bit Period control
- Audio Memory: N/A

### Channel Types
- Square Wave: 3 channels
- Noise: No
- Sample Playback: No

## System Timing
### Interrupts
- Types Available: IRQ
- Sources: VBLANK
- Timing: 60 Hz
- Priority: Standard

## GameVM Implementation Notes
### Compiler Considerations
- Preferred Code Generation Strategy: Native (Z80)
- Register Allocation Strategy: Minimal (due to 2KB RAM context)
- Memory Management Strategy: Static
- Optimization Opportunities: Pre-shifted software sprites for faster blitting into tile definitions

### Performance Targets
- Minimum Frame Rate: 30 fps
- Audio Update Frequency: 60 Hz
- Memory Budget: 2 KB RAM
- Known Limitations: Extremely limited colors and no hardware sprites

## References
- Wikipedia: Casio PV-1000
- NEC D65010G031 Technical Summary
- Casio PV-1000 Homebrew Wiki
