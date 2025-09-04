# FM Towns Marty

## System Overview
- CPU: AMD 386SX
- CPU Clock: 16 MHz
- GPU: Custom sprite/tilemap processor
- Release Year: 1993
- Generation: 4th/5th (transitional)
- Region: Japan only
- Predecessor: FM Towns (computer)
- Successor: FM Towns Marty II
- Notable Feature: Console version of FM Towns computer with CD-ROM and sprite hardware

## CPU Details
### AMD 386SX
#### Architecture Characteristics
- Processor: 32-bit x86
- Word Size: 32-bit internal, 16-bit external
- Endianness: Little-endian
- Register Set:
  - General Purpose Registers (32-bit)
  - Segment Registers
  - Flags Register
  - Extended registers
- Features:
  - Protected mode
  - Virtual 8086 mode
  - Paging support
  - x87 FPU support

### Graphics Processing
- Custom sprite processor
- Tilemap engine
- PCM sound processor
- CD-ROM controller

## Memory Map
### System Memory
- Main RAM: 2MB
- Video RAM: 640KB
- Sprite RAM: 512KB
- ROM: 512KB
- CD Buffer: 64KB

### Memory Layout
- Real Mode:
  - 0x00000-0x9FFFF: Base RAM
  - 0xA0000-0xBFFFF: Video RAM
  - 0xC0000-0xDFFFF: ROM
  - 0xE0000-0xFFFFF: System ROM
- Protected Mode:
  - Full 32-bit address space
  - Memory management
  - Paging support

## Video System
### Graphics Processing
- Resolution: 352×232 to 640×480
- Colors: 16.7M (24-bit)
- Sprites: Hardware-assisted
- Planes: Multiple layers

### Display Features
- Resolutions:
  - 352×232
  - 640×400
  - 640×480
- Color Depths:
  - 24-bit (16.7M colors)
  - 15-bit (32K colors)
  - 8-bit (256 colors)
- Features:
  - Hardware sprites
  - Tilemap engine
  - Multiple planes
  - Overlay support

### Special Effects
- Hardware sprite scaling
- Priority control
- Color mixing
- CD video overlay
- Multiple layers
- Transparency

## Audio System
### Sound Processor
- Channels: 6 FM + 8 PCM
- Sample Rate: 44.1 kHz
- Features:
  - CD-quality audio
  - FM synthesis
  - PCM playback
  - Digital mixing

### Audio Capabilities
- CD audio playback
- FM synthesis
- PCM samples
- Digital effects
- Mixing capabilities
- MIDI support

## Input/Output System
### Controller Interface
- Two controller ports
- Mouse port
- Keyboard port
- MIDI interface

### Storage Interface
- CD-ROM Drive:
  - Double speed
  - 680MB capacity
  - CD audio support
  - Photo CD support
- Floppy Drive:
  - 3.5" 1.2MB
  - MS-DOS format
  - Backup storage

### Video Output
- Composite video
- RGB (15 kHz)
- RGB (31 kHz)
- S-Video

## System Integration Features
### Hardware Variants
- FM Towns Marty
- FM Towns Marty II
- Development systems

### Regional Differences
- Japan-only release
- Single video standard
- Single power standard
- No region lock

### Special Features
- PC compatibility
- MIDI support
- Mouse support
- Keyboard support

## Technical Legacy
### Hardware Innovations
- PC architecture
- CD-ROM integration
- Sprite hardware
- Audio capabilities

### Software Development
- Development Tools:
  - MS-DOS compatible
  - Towns OS tools
  - Graphics libraries
  - Sound libraries
- Programming Features:
  - x86 assembly
  - C/C++ support
  - Hardware access
  - OS services

### Market Impact
- Production Run: 1993-1995
- Units Sold: ~45,000
- Games Released: ~175
- Price Points:
  - Launch: ¥98,000
  - Final: ¥66,000
- Market Position:
  - PC/Console hybrid
  - High-end system
  - Limited market
  - Japan exclusive

## GameVM Implementation Notes
### Compiler Considerations
- Code Generation:
  - x86 instructions
  - Protected mode
  - Hardware access
  - DMA control
- Register Allocation:
  - x86 registers
  - Segment registers
  - System resources
- Memory Management:
  - Segmentation
  - Paging
  - DMA
  - Cache control

### Performance Targets
- CPU: 16 MHz 386SX
- Video: Multiple modes
- Audio: CD quality
- Memory Budget:
  - Main RAM: 2MB
  - Video RAM: 640KB
  - Sprite RAM: 512KB
  - CD Buffer: 64KB

### Special Handling
- x86 Architecture
- Protected Mode
- Sprite Hardware
- CD-ROM Access
- Audio System

## References
- [FM Towns Hardware Manual](http://www.fmtowns.org/docs/hardware)
- [386SX Programming Guide](http://www.intel.com/support/386sx)
- [Towns OS Programming](http://www.townsdev.net)
- [FM Towns Development](http://www.fmtowns.org/development)
