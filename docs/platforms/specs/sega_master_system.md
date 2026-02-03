# Sega Master System

## System Overview
- Profile: GV.Spec.L3
- CPU: Zilog Z80A
- Clock Speed: 4 MHz
- Release Year: 1985 (JP), 1986 (NA), 1987 (EU)
- Generation: 3rd
- Region: Worldwide
- Predecessor: SG-1000
- Successor: Sega Genesis/Mega Drive

## CPU Details
### Architecture Characteristics
- Instruction Set: Zilog Z80A (enhanced 8080 instruction set)
- Word Size: 8-bit
- Endianness: Little-endian
- Register Set:
  - Main: A (accumulator), B, C, D, E, H, L
  - Alternate: A', B', C', D', E', H', L'
  - Special Purpose: IX, IY (index), SP (stack pointer), PC (program counter)
  - Flags: S (sign), Z (zero), H (half carry), P/V (parity/overflow), N (add/subtract), C (carry)

### Memory Access
- Address Bus Width: 16 bits
- Data Bus Width: 8 bits
- Memory Page Size: 16 KB
- Paging Capabilities: Mapper support for ROM banking
- DMA: VDP capable of memory-to-VRAM transfers
- Memory Access Timing: 4 cycles per instruction minimum

### Performance Considerations
- Instruction Timing: 4-23 T-states per instruction
- Interrupt Modes: IM 0, IM 1, IM 2 supported
- Known Bottlenecks:
  - VDP access timing constraints
  - Limited sprite count per scanline
  - Memory mapper overhead
- Optimization Opportunities:
  - Use of block instructions for fast memory transfers
  - Interrupt mode selection for specific timing needs
  - Strategic use of alternate register set

## Memory Map
### System Memory
- Total RAM: 8 KB main RAM
- Video RAM: 16 KB dedicated VRAM
- ROM Areas:
  - Boot ROM: 8-32 KB (region/model dependent)
  - Cartridge ROM: Up to 4 MB (with mapper)
  - Optional RAM: 2 KB (memory cards)

### Memory Layout
- $0000-$03FF: Page 0 (ROM)
- $0400-$3FFF: ROM
- $3E00-$3EFF: Sound registers
- $3F00-$3FFF: VDP registers
- $4000-$7FFF: ROM/Cartridge
- $8000-$BFFF: ROM/Cartridge
- $C000-$DFFF: System RAM
- $E000-$FFFF: System RAM mirror

## Video System
### VDP (Video Display Processor)
- Chip: Sega VDP (based on Texas Instruments TMS9918)
- Clock Speed: 10.738635 MHz (NTSC) / 10.6875 MHz (PAL)
- Resolution: 256×192 pixels (PAL/NTSC)
- Color Depth: 64 colors on screen from 256 color palette
- Color RAM: 32 bytes (16 entries × 2 bytes per color)

### Sprite Capabilities
- Total Sprites: 64
- Sprites per Scanline: 8
- Sprite Sizes: 8×8 or 8×16 pixels
- Sprite Colors: 16 colors per sprite
- Sprite Attributes:
  - X/Y position
  - Pattern number
  - Color
  - Priority
  - X-flip/Y-flip

### Display Modes
- Mode 0: Graphics I (Text mode)
  - Resolution: 40×24 characters
  - Character Size: 6×8 pixels
  - Colors: 2 colors per character
- Mode 1: Graphics II
  - Resolution: 256×192
  - Tile Size: 8×8
  - Colors: 16 colors per tile
- Mode 2: Multicolor
  - Resolution: 256×192
  - Block Size: 4×4 pixels
  - Colors: 16 colors per block
- Mode 4: Enhanced Resolution
  - Resolution: 256×192
  - Tile Size: 8×8
  - Colors: 16 colors per tile
  - Extended sprite capabilities

### Special Features
- Hardware Scrolling:
  - Horizontal: Pixel-perfect
  - Vertical: Pixel-perfect
- Line Interrupts: Programmable
- Screen Masking
- Background Priority
- Direct Memory Access (DMA)

## Audio System
### Primary Sound (SN76489)
- Chip: Texas Instruments SN76489
- Clock Speed: 3.579545 MHz
- Channels: 4 total
  - 3× Square Wave Generators:
    - Frequency Range: 100 Hz to 48 kHz
    - Volume: 16 levels (4-bit)
    - Duty Cycle: 50%
  - 1× Noise Generator:
    - White noise or periodic noise
    - 3 preset frequencies
    - Volume: 16 levels (4-bit)

### FM Sound (YM2413) - Japanese/Korean Models
- Chip: Yamaha YM2413
- Clock Speed: 3.579545 MHz
- Channels: 9 FM channels
- Instruments: 15 preset + 1 custom
- Features:
  - Vibrato
  - Tremolo
  - Sustain
  - Key Scale Level
  - Key Scale Rate
- Implementation: Optional, region dependent

## Input/Output System
### Controller Ports
- Type: DE-9 (9-pin)
- Number of Ports: 2
- Supported Inputs:
  - Standard Controller (2 buttons)
  - Light Phaser
  - 3D Glasses
  - Control Pad
  - Sports Pad

### Storage Interfaces
- Cartridge Slot:
  - Size: Up to 4 MB
  - Access Time: Direct
  - Mapper Support: Yes
- Card Slot:
  - Size: Up to 32 KB
  - Access Time: Direct
  - Format: Sega Card

### Video Output
- RF Output: Standard
- Composite Video: Yes (model dependent)
- RGB Output: Yes (European models)
- Video Format:
  - NTSC: 60 Hz
  - PAL: 50 Hz
  - Resolution: 256×192

### Expansion Capabilities
- Expansion Port: Yes
- External Power: 9V DC
- AV Multi-out (later models)

## System Integration Features
### Built-in Components
- BIOS: Region-specific
- Built-in Game: Varies by region/model
- Memory Card Support: Yes (optional)
- Power Supply: External AC adapter

### Region Implementation
- Hardware lockout: Yes
- Region differences:
  - FM Sound (JP/KR only)
  - Video system (NTSC/PAL)
  - Built-in games
  - Case design

### Special Hardware Features
- 3D Glasses Support:
  - LCD shutter technology
  - 60 Hz alternating frames
  - Dedicated port/adapter required
- Light Phaser Compatibility:
  - Precision: 256×192 resolution
  - Response time: 1 frame
  - Calibration: Self-calibrating
- Rapid Fire Unit Support
- Reset Button
- Pause Button (on console)

### Performance Optimizations
- VDP Wait States: Properly timed access required
- Sprite Flickering: Hardware limitation workarounds
- Sound mixing: Software implemented
- Memory banking: Mapper-dependent strategies
