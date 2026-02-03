# Amstrad GX4000

## System Overview
- Profile: GV.Spec.L4
- CPU: Zilog Z80A
- Clock Speed: 4 MHz (Effective 3.3 MHz due to video memory contention)
- Release Year: 1990
- Generation: 3.5th (Marketed against 8-bit systems but with 16-bit color features)
- Region: Europe
- Predecessor: Amstrad CPC 6128 Plus (Console version of computer)
- Successor: N/A

## CPU Details
### Architecture Characteristics
- Instruction Set Family: Z80
- Word Size: 8-bit
- Endianness: Little
- Register Set:
  - General Purpose: A, B, C, D, E, H, L (and shadow set A', B', C', D', E', H', L')
  - Special Purpose: IX, IY, SP, PC, I, R
  - Status Flags: S, Z, Y, H, X, P, N, C

### Memory Access
- Address Bus Width: 16-bit
- Data Bus Width: 8-bit
- Memory Page Size: N/A
- Zero Page: N/A
- Special Addressing Modes: Relative, Indexed, Bit Addressing
- DMA Capabilities: Yes (ASIC provides 3-channel DMA for sound)

### Performance Considerations
- Instruction Timing: 4-23 cycles
- Pipeline Features: None
- Known Bottlenecks: Memory access wait states due to ASIC/Video contention
- Optimization Opportunities: Use of shadow registers; block move instructions (LDIR, etc.)

## Memory Map
### RAM
- Total Size: 64 KB
- Layout: 0x0000-0xFFFF (mapped by ASIC)
- Bank Switching: Yes (ASIC can map different 16KB pages)
- Access Speed: 1 cycle (subject to wait states)
- Constraints: Contention with video generation

### ROM
- Total Size: 32 KB (Internal System ROM / Cartridge)
- Bank Switching Capabilities: ASIC manages cartridge mapping
- Access Speed: 1 cycle
- Special Features: N/A

### Special Memory Regions
- VRAM: 16 KB (Part of the 64KB RAM, addressable by ASIC)
- Audio Memory: N/A
- I/O Registers: 0x4000-0x7FFF (ASIC configuration mapping)
- System Vectors: 0x0000, 0x0038 (IM 1), 0x0066 (NMI)

## Video System
### Display Characteristics
- Resolution: 160x200 (16 colors), 320x200 (4 colors), 640x200 (2 colors)
- Color Depth: 12-bit RGB (4,096 colors)
- Refresh Rate: 50 Hz (PAL)
- Video RAM: 16 KB

### Graphics Capabilities
- Sprite Support: Yes (Hardware Sprites)
  - Max Sprites: 16
  - Sprite Size: 16x16 pixels (can be doubled/quadrupled)
  - Colors per Sprite: 15 colors + transparency
  - Limitations: Max 16 sprites per scanline (using magnified ones reduces this)
- Background Layers: 1
  - Number of Layers: 1
  - Tile Size: Soft-defined (Character or pixel graphics)
  - Colors per Tile: Based on mode (up to 16)
- Special Effects: Hardware smooth scrolling (Pixel accuracy), Hardware split-screen support

### Timing
- VBLANK Duration: approx. 1 ms
- HBLANK Duration: approx. 12 us
- Access Windows: CPU access is slowed during active scan to prevent "snow"

## Audio System
### Audio Hardware
- Sound Channels: 3 (via General Instrument AY-3-8912)
- Sample Rate: N/A (Programmable Sound Generator)
- Bit Depth: N/A
- Audio Memory: N/A

### Channel Types
- Square Wave: 3 channels
- Noise: 1 channel (assignable to any/all square channels)
- Sample Playback: Possible via software volume manipulation or ASIC DMA
- Special Features: 3-channel DMA Sound supported by ASIC

## System Timing
### Interrupts
- Types Available: IRQ (standard), NMI
- Sources: VBLANK, ASIC scanline counters
- Timing: Programmable via ASIC raster interrupts
- Priority: NMI > IRQ

## GameVM Implementation Notes
### Compiler Considerations
- Preferred Code Generation Strategy: Native (Z80)
- Register Allocation Strategy: Register-heavy (utilizing HL, BC, DE pairs)
- Memory Management Strategy: ASIC bank switching
- Optimization Opportunities: Exploit 12-bit palette for high-end 8-bit effects

### Performance Targets
- Minimum Frame Rate: 50 fps (PAL)
- Audio Update Frequency: 50 Hz
- Memory Budget: 64 KB total
- Known Limitations: Effective CPU speed is slower than 4MHz due to wait states

## References
- Amstrad CPC Plus ASIC Specifications
- GX4000 Technical Archive (gx4000.co.uk)
- Z80 Architecture Reference Manual
