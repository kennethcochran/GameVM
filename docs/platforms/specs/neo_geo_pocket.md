# SNK Neo Geo Pocket

## System Overview
- Profile: GV.Spec.L4
- CPU: 16-bit Toshiba TLCS900H (Main), Zilog Z80 (Sound)
- Clock Speed: 6.144 MHz (Main), 3.072 MHz (Sound)
- Release Year: 1998
- Generation: 5th (Handheld)
- Region: Japan
- Predecessor: N/A
- Successor: Neo Geo Pocket Color

## CPU Details
### Architecture Characteristics
- Instruction Set Family: TLCS-900 (Z80-based 16-bit CISC)
- Word Size: 16-bit
- Endianness: Little
- Register Set:
  - 8 x 32-bit registers (can be used as 16 x 16-bit or 32 x 8-bit)
- Features:
  - High code density
  - Z80 compatibility mode (not used for main logic)

### Memory Access
- Address Bus Width: 24-bit
- Data Bus Width: 16-bit
- DMA Capabilities: Yes

### Performance Considerations
- Instruction Timing: 1-12 cycles
- Known Bottlenecks: Limited main RAM (12 KB)
- Optimization Opportunities: Efficient 16-bit arithmetic; secondary Z80 for offloading audio

## Memory Map
### RAM
- Total Size: 12 KB (WRAM) + 4 KB (Z80 RAM)
- Bank Switching: No
- Access Speed: 1 cycle

### ROM
- Total Size: Cartridges up to 4 MB (standard)
- Bank Switching Capabilities: Hardware mapping support
- Access Speed: 1 cycle

### Special Memory Regions
- VRAM: 12 KB (Shared indices/attributes)
- Character RAM: 8 KB
- Tilemap RAM: 4 KB
- I/O Registers: Memory-mapped

## Video System
### Display Characteristics
- Resolution: 160x152 (Visible) / 256x256 (Virtual plane)
- Color Depth: Monochrome (8 shades of gray)
- Refresh Rate: 60 Hz
- Video RAM: 12 KB

### Graphics Capabilities
- Sprite Support: Yes
  - Max Sprites: 64
  - Sprite Size: 8x8 pixels
  - Colors: 8 shades
  - Limitations: Hardware tile-based sprite engine
- Background Layers: 2
  - Number of Layers: 2 (Scrolling planes)
  - Tile Size: 8x8 pixels
- Special Effects: Hardware scrolling (2 planes)

## Audio System
### Audio Hardware
- Sound Channels: 6 simultaneous tones + 2 DAC channels
- Audio CPU: Zilog Z80 (3.072 MHz)

### Channel Types
- Square Wave: 3 channels (SN76489 compatible)
- Noise: 1 channel
- Sample Playback: Yes (Dual 8-bit DACs)

## System Timing
### Interrupts
- Types Available: Multiple maskable and non-maskable
- Sources: VBLANK, Timer, Serial, etc.

## GameVM Implementation Notes
### Compiler Considerations
- Preferred Code Generation Strategy: Native (TLCS900H)
- Register Allocation Strategy: Utilize large register file for local variable caching
- Memory Management Strategy: Static 12 KB allocation

### Performance Targets
- Minimum Frame Rate: 60 fps
- Audio Update Frequency: 60 Hz
- Memory Budget: 12 KB Main RAM

## References
- Neo Geo Pocket Technical Manual
- Toshiba TLCS-900 Series User Manual
