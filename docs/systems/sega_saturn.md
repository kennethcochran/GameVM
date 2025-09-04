# Sega Saturn

## System Overview
- CPU: Two Hitachi SH-2 (32-bit RISC)
- CPU Clock: 28.6 MHz (each)
- VDP: Two Video Display Processors
- Release Year: 1994/1995
- Generation: 5th
- Region: Worldwide
- Predecessor: Sega Genesis/Mega Drive
- Successor: Dreamcast
- Notable Feature: Dual CPU architecture, specialized for 2D and quadrilateral rendering

## CPU Details
### Dual SH-2 Processors
#### Architecture Characteristics
- Processor: Hitachi SH-2 RISC
- Word Size: 32-bit
- Endianness: Little-endian
- Register Set:
  - 16 General Purpose Registers (32-bit)
  - Program Counter (32-bit)
  - Status Register (32-bit)
  - Vector Base Register
- Features:
  - 5-stage pipeline
  - On-chip cache
  - Harvard architecture
  - DSP-like instructions

### SCU (System Control Unit)
- Purpose: DSP operations
- Features:
  - Matrix operations
  - Vector calculations
  - DMA control
  - Interrupt handling

## Memory Map
### System Memory
- Work RAM-H: 1MB (32-bit)
- Work RAM-L: 1MB (16-bit)
- VRAM: 1.54MB total
- CD Buffer: 512KB
- BIOS ROM: 512KB
- SRAM: 32KB

### Memory Layout
- $00000000-$000FFFFF: BIOS ROM
- $00100000-$0017FFFF: Work RAM-H
- $00180000-$001FFFFF: Work RAM-L
- $02000000-$023FFFFF: VDP1 VRAM
- $05A00000-$05AFFFFF: VDP2 VRAM
- $05C00000-$05C7FFFF: SCU RAM

## Video System
### VDP1 (Sprite/Polygon Processor)
- Features:
  - Sprite rendering
  - Polygon processing
  - Texture mapping
  - Gouraud shading
- Performance:
  - 200,000 textured polygons/sec
  - 500,000 flat polygons/sec
  - 60 sprites/line
  - 4096 displayable sprites

### VDP2 (Background Processor)
- Features:
  - Background layers
  - Rotation/scaling
  - Column/row scroll
  - 3D background planes
- Capabilities:
  - 5 simultaneous backgrounds
  - 2 rotation/scaling planes
  - Resolution up to 704×480
  - 16.7M colors (24-bit)

### Special Features
- Resolution Modes:
  - 320×224
  - 352×240
  - 640×240
  - 704×480
- Color Depths:
  - 16.7M colors (24-bit)
  - 32K colors (15-bit)
  - 256 colors (8-bit)
- Effects:
  - Alpha blending
  - Shadow processing
  - Transparency
  - Color calculation

## Audio System
### Sound Processor
- Type: Motorola 68EC000
- Clock: 11.3 MHz
- Features:
  - 32 PCM channels
  - 8 FM channels
  - DSP effects
  - CD-DA mixing

### Audio Capabilities
- Sample Rate: 44.1 kHz
- Channels:
  - 32 PCM channels
  - 8 FM synthesis channels
- Features:
  - CD audio
  - Real-time effects
  - Digital filtering
  - MIDI capabilities

## Input/Output System
### Controller Interface
- Two controller ports
- Multi-tap support (up to 12 players)
- Memory cartridge slot
- Communication port

### Storage Interface
- CD-ROM Drive:
  - Double speed
  - 680MB capacity
  - CD audio support
  - Enhanced CD support
- Memory Backup:
  - Internal battery backup
  - External memory cartridge
  - Save state support

### Video Output
- Composite video
- S-Video
- RGB SCART
- RF adapter (optional)

## System Integration Features
### Hardware Variants
- Model 1 (HST-3200)
- Model 2 (HST-3220)
- Hi-Saturn
- V-Saturn
- Development units

### Regional Differences
- TV standards (NTSC/PAL)
- Boot ROM versions
- Case design
- Game library

### Special Features
- NetLink modem support
- Action Replay cartridge
- Various peripherals
- Arcade ports

## Technical Legacy
### Hardware Innovations
- Dual CPU architecture
- Specialized video processors
- Advanced 2D capabilities
- CD-ROM integration

### Software Development
- Development Tools:
  - Sega Graphics Library
  - Saturn SDK
  - Development hardware
  - Debug utilities
- Programming Features:
  - Dual CPU coordination
  - VDP programming
  - DMA optimization
  - CD streaming

### Market Impact
- Production Run: 1994-2000
- Units Sold: 9.26 million
- Games Released: ~1,000
- Price Points:
  - Launch: $399
  - Final: $99
- Market Position:
  - Strong in Japan
  - 2D powerhouse
  - Arcade perfect ports
  - Complex architecture

## GameVM Implementation Notes
### Compiler Considerations
- Code Generation:
  - SH-2 instruction set
  - Dual CPU coordination
  - VDP command generation
  - DMA optimization
- Register Allocation:
  - Per-CPU registers
  - Shared memory access
  - VDP registers
- Memory Management:
  - Work RAM allocation
  - VRAM management
  - CD buffer handling
  - DMA queuing

### Performance Targets
- CPU: 28.6 MIPS (per CPU)
- VDP1: 200K textured polys/sec
- VDP2: 5 backgrounds
- Memory Budget:
  - Work RAM: 2MB total
  - VRAM: 1.54MB
  - Sound RAM: 512KB
  - CD buffer: 512KB

### Special Handling
- Dual CPU Synchronization
- VDP1/VDP2 Coordination
- SCU DSP Operations
- CD Streaming
- DMA Management

## References
- [Saturn Hardware Manual](http://segaretro.org/Saturn_Hardware_Manual)
- [SH-2 Programming Guide](http://renesas.com/sh2)
- [VDP1/VDP2 Programming Guide](http://segadev.org/saturn/docs)
- [Saturn Development Tools](http://segaxtreme.net/saturn)
