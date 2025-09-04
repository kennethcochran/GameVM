# Sony PlayStation

## System Overview
- CPU: MIPS R3000A
- CPU Clock: 33.8688 MHz
- GPU: Custom Sony GPU
- Release Year: 1994/1995
- Generation: 5th
- Region: Worldwide
- Predecessor: None (Sony's first console)
- Successor: PlayStation 2
- Notable Feature: First successful CD-ROM based gaming console

## CPU Details
### MIPS R3000A
#### Architecture Characteristics
- Processor: LSI LR33300 (R3000A core)
- Word Size: 32-bit
- Endianness: Little-endian
- Register Set:
  - 32 General Purpose Registers (32-bit)
  - HI/LO Registers (64-bit)
  - Program Counter (32-bit)
  - Status Register
- Features:
  - 5-stage pipeline
  - Branch delay slot
  - Load delay slot
  - MIPS I instruction set

### Geometry Transformation Engine (GTE)
- Purpose: 3D geometry calculations
- Features:
  - Matrix multiplication
  - Vector operations
  - Perspective transforms
  - Lighting calculations
- Performance:
  - 66M coordinates/second
  - 4.5M polygons/second
  - Fixed-point arithmetic
  - Parallel operation

## Memory Map
### System Memory
- Main RAM: 2MB
- Video RAM: 1MB
- Sound RAM: 512KB
- BIOS ROM: 512KB
- CD Buffer: 32KB

### Memory Layout
- $00000000-$001FFFFF: Main RAM
- $1F000000-$1F7FFFFF: Expansion
- $1F800000-$1F8003FF: Scratchpad
- $1FC00000-$1FC7FFFF: BIOS ROM
- $1FF00000-$1FF7FFFF: I/O Ports

## Video System
### Graphics Processing Unit
- Resolution: 256×224 to 640×480
- Colors: 16.7M (24-bit)
- Sprites: 4000 8×8
- Textures: 256×256 maximum

### Display Features
- Resolutions:
  - 256×224
  - 320×240
  - 512×240
  - 640×480
- Color Depths:
  - 24-bit (16.7M colors)
  - 15-bit (32K colors)
  - 8-bit (256 colors)
- Features:
  - 2D sprites
  - Texture mapping
  - Gouraud shading
  - Semi-transparency

### Special Effects
- Texture mapping
- Gouraud shading
- Sprites/polygons
- Semi-transparency
- Dithering
- Frame buffer effects

## Audio System
### SPU (Sound Processing Unit)
- Channels: 24
- Sample Rate: 44.1 kHz
- Features:
  - ADPCM compression
  - Hardware reverb
  - Digital filtering
  - CD audio

### Audio Capabilities
- CD-quality audio
- Real-time effects
- MIDI support
- Digital mixing
- Hardware compression
- Reverb processing

## Input/Output System
### Controller Interface
- Two controller ports
- Memory card slots
- Serial I/O port
- Parallel I/O port

### Storage Interface
- CD-ROM Drive:
  - Double speed
  - 660MB capacity
  - XA format support
  - CD audio support
- Memory Cards:
  - 128KB capacity
  - 15 save blocks
  - Flash memory

### Video Output
- Composite video
- S-Video
- RGB SCART
- RFU adaptor

## System Integration Features
### Hardware Variants
- SCPH-1000 series
- SCPH-5000 series
- SCPH-7000 series
- SCPH-9000 series

### Regional Differences
- TV standards (NTSC/PAL)
- Boot ROM versions
- Case design
- Power supply

### Special Features
- Link cable support
- Multitap support
- Net Yaroze
- Debug units

## Technical Legacy
### Hardware Innovations
- CD-ROM based
- 3D acceleration
- Sound processing
- Memory card storage

### Software Development
- Development Tools:
  - PsyQ SDK
  - DTL development hardware
  - LibGS graphics
  - CD emulator
- Programming Features:
  - Low-level access
  - High-level libraries
  - Debug features
  - Performance tools

### Market Impact
- Production Run: 1994-2006
- Units Sold: 102.49 million
- Games Released: 7,918
- Price Points:
  - Launch: $299
  - Final: $99
- Market Position:
  - Market leader
  - CD-ROM gaming
  - 3D graphics pioneer
  - Major success

## GameVM Implementation Notes
### Compiler Considerations
- Code Generation:
  - MIPS R3000A instructions
  - GTE optimization
  - DMA coordination
  - Cache management
- Register Allocation:
  - General purpose registers
  - GTE registers
  - Coprocessor registers
- Memory Management:
  - Main RAM
  - VRAM
  - Sound RAM
  - CD buffer

### Performance Targets
- CPU: 30 MIPS
- GTE: 66M coordinates/sec
- GPU: 360K polygons/sec
- Memory Budget:
  - Main RAM: 2MB
  - VRAM: 1MB
  - Sound RAM: 512KB
  - CD buffer: 32KB

### Special Handling
- GTE Operations
- GPU Commands
- SPU Management
- CD-ROM Access
- DMA Control

## References
- [PlayStation Hardware Documentation](http://problemkaputt.de/psx-spx.htm)
- [MIPS R3000A Manual](http://www.mips.com/r3000a)
- [PlayStation Development Guide](http://www.psxdev.net/docs)
- [GPU Programming Guide](http://www.psxdev.net/gpu)
