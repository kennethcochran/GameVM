# Bitcorp Gamate

## System Overview
- Profile: GV.Spec.L1
- CPU: 8-bit MOS 6502 (UMC UA6588F or NCR 81489)
- Clock Speed: 2.22 MHz
- Release Year: 1990
- Generation: 4th (Handheld competitor to Game Boy)
- Region: Worldwide
- Predecessor: N/A
- Successor: N/A

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
- DMA Capabilities: No

### Performance Considerations
- Instruction Timing: 2-7 cycles
- Pipeline Features: None
- Known Bottlenecks: Slow clock speed for a bitmap-based system
- Optimization Opportunities: Zero Page usage for fast variables

## Memory Map
### RAM
- Total Size: 16 KB (Main) + 1 KB (CPU Internal)
- Layout: 0x0000-0x3FFF (16 KB)
- Bank Switching: None standard for RAM
- Access Speed: 1 cycle
- Constraints: 16 KB total for game logic and software-rendered buffers

### ROM
- Total Size: 4 KB (Internal BIOS) + Cartridge
- Bank Switching Capabilities: Cartridge mappers up to 512 KB
- Access Speed: 1 cycle
- Special Features: External cartridges require specific header signature

### Special Memory Regions
- VRAM: 8 KB (Flat framebuffer space)
- Audio Memory: N/A
- I/O Registers: 0x4000-0x5FFF (Hardware control)
- System Vectors: 0xFFF0-0xFFFF

## Video System
### Display Characteristics
- Resolution: 160x152 (Visible) / 256x256 (Bitmap space)
- Color Depth: 2-bit (4 levels of grayscale)
- Refresh Rate: 60 Hz
- Video RAM: 8 KB

### Graphics Capabilities
- Sprite Support: No (Software-rendered into bitmap)
- Background Layers: 1 (Bitmap framebuffer)
  - Number of Layers: 1
  - Tile Size: N/A (Bitmap-based)
  - Colors per Tile: N/A
- Special Effects: No hardware scrolling (must be done in software by shifting bitmap memory)

### Timing
- VBLANK Duration: approx. 1 ms
- HBLANK Duration: Standard
- Access Windows: Unrestricted

## Audio System
### Audio Hardware
- Sound Channels: 3 (Integrated AY-3-8910 compatible logic)
- Sample Rate: N/A
- Bit Depth: N/A
- Audio Memory: N/A

### Channel Types
- Square Wave: 3 channels
- Noise: 1 channel
- Sample Playback: No
- Special Features: Stereo output via headphone jack

## System Timing
### Interrupts
- Types Available: IRQ, NMI
- Sources: VBLANK, Timer
- Timing: 60 Hz
- Priority: Standard

## GameVM Implementation Notes
### Compiler Considerations
- Preferred Code Generation Strategy: Native (6502)
- Register Allocation Strategy: Minimal (A, X, Y)
- Memory Management Strategy: Static 16 KB allocation
- Optimization Opportunities: Optimize bitmap clearing and pixel manipulation for the 6502

### Performance Targets
- Minimum Frame Rate: 15-20 fps (due to software rendering overhead)
- Audio Update Frequency: 60 Hz
- Memory Budget: 16 KB RAM
- Known Limitations: High ghosting on original LCD; heavy CPU load for any scrolling effect

## References
- Bitcorp Gamate Technical Analysis (grauw.nl)
- 12bit.club: Gamate Platform Guide
- UA6588F Datasheet
