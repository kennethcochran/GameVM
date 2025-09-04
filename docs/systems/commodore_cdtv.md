# Commodore CDTV

## System Overview
- CPU: Motorola 68000
- CPU Clock: 7.14 MHz
- Release Year: 1991
- Generation: 4th
- Region: Worldwide
- Predecessor: Amiga 500
- Successor: Amiga CD32
- Notable Feature: First CD-ROM based Amiga system

## CPU Details
### Main CPU (68000)
#### Architecture Characteristics
- Processor: Motorola 68000
- Word Size: 16-bit (internally 32-bit)
- Endianness: Big-endian
- Register Set:
  - Data Registers (D0-D7): 32-bit
  - Address Registers (A0-A7): 32-bit
  - Program Counter (PC): 32-bit
  - Status Register (SR)
- Features:
  - 16/32-bit operations
  - 14 addressing modes
  - Supervisor/user modes
  - Interrupt handling

### Custom Chipset
#### Agnus
- Purpose: Memory and video control
- Features:
  - DMA channels
  - Blitter
  - Copper
  - Sprite control

#### Denise
- Purpose: Video generation
- Features:
  - Sprite engine
  - Color palette
  - Video modes
  - Bitplane control

#### Paula
- Purpose: Audio and I/O
- Features:
  - 4 DMA audio channels
  - Disk controller
  - Serial port
  - Interrupt control

## Memory Map
### System Memory
- Chip RAM: 512KB
- Fast RAM: 512KB
- ROM: 512KB (Kickstart)
- CD-ROM: Up to 650MB

### Memory Layout
- $000000-$07FFFF: Chip RAM
- $080000-$0FFFFF: Fast RAM
- $BFD000-$BFDFFF: Custom chips
- $F80000-$FFFFFF: Kickstart ROM

## Video System
### Graphics Processing
- Resolution: 320×256 to 640×512
- Colors: 4096 (12-bit)
- Sprites: 8 (16 pixels wide)
- Bitplanes: Up to 6

### Display Features
- Multiple Resolutions:
  - Low-Res: 320×256
  - High-Res: 640×256
  - Interlaced: Up to 640×512
- Color Depths:
  - 2 colors (1 bitplane)
  - 4 colors (2 bitplanes)
  - 16 colors (4 bitplanes)
  - 32 colors (5 bitplanes)
  - 64 colors (6 bitplanes)

### Special Features
- Hold-and-Modify (HAM)
- Extra-Half-Brite (EHB)
- Dual Playfield
- Hardware scrolling
- Copper effects

## Audio System
### Paula Audio
- Channels: 4
- Sample Resolution: 8-bit
- Features:
  - DMA driven
  - Volume control
  - Period control
  - Stereo output

### Audio Capabilities
- CD Audio:
  - 16-bit stereo
  - 44.1 kHz
  - Real-time mixing
- Features:
  - Sample playback
  - Volume control
  - Stereo panning
  - Filter control

## Input/Output System
### Controller Interface
- Two controller ports
- CD-i style remote
- Keyboard port
- Mouse port
- Serial port
- Parallel port

### Storage Interface
- CD-ROM Drive:
  - Single speed
  - CD-DA support
  - CD+G support
  - ISO 9660
- Floppy Drive (optional):
  - 880KB capacity
  - DD/HD support
  - Amiga format

### Video Output
- Composite video
- RF output
- RGB video
- S-Video (some models)

## System Integration Features
### Hardware Variants
- CDTV
- CDTV-CR (rack mount)
- CDTV II (prototype)

### Regional Differences
- TV standards (NTSC/PAL)
- Power supply
- Case design
- Software library

### Special Features
- CD-ROM based
- Remote control
- MIDI support
- Genlock capability

## Technical Legacy
### Hardware Innovations
- Early CD-ROM adoption
- Multimedia focus
- Amiga compatibility
- Consumer design

### Software Development
- Development Tools:
  - Amiga SDK
  - CD-ROM tools
  - RKRM manuals
  - AmigaOS
- Programming Features:
  - CD-ROM access
  - Custom chips
  - DMA control
  - Multimedia

### Market Impact
- Production Run: 1991-1993
- Units Sold: ~25,000
- Titles Released: ~200
- Price Points:
  - Launch: $999
  - Final: $599
- Market Position:
  - Multimedia platform
  - Home computer
  - CD-ROM pioneer
  - Limited success

## GameVM Implementation Notes
### Compiler Considerations
- Code Generation:
  - 68000 instructions
  - Custom chip access
  - DMA coordination
  - CD-ROM handling
- Register Allocation:
  - System resources
  - Chip RAM vs Fast RAM
  - DMA channels
- Memory Management:
  - Chip/Fast RAM
  - CD-ROM buffering
  - DMA control

### Performance Targets
- Frame Rate: 50/60 Hz
- Audio: CD quality
- Video: Multiple modes
- Memory Budget:
  - Chip RAM: 512KB
  - Fast RAM: 512KB
  - CD-ROM buffer

### Special Handling
- Custom Chipset
- CD-ROM Access
- DMA Control
- Audio Mixing
- Video Modes

## References
- [CDTV Hardware Manual](http://www.commodore.ca/cdtv)
- [Amiga Hardware Reference](http://amigadev.elowar.com)
- [CDTV Developer Documentation](http://www.cdtv.org/dev)
- [Kickstart ROM Guide](http://www.amigaos.net/kickstart)
