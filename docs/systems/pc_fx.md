# NEC PC-FX

## System Overview
- CPU: NEC V810 RISC
- CPU Clock: 21.5 MHz
- GPU: 16-bit video processor
- Release Year: 1994
- Generation: 5th
- Region: Japan only
- Predecessor: PC Engine/TurboGrafx-16
- Successor: None
- Notable Feature: Specialized for 2D sprite manipulation and FMV playback

## CPU Details
### NEC V810
#### Architecture Characteristics
- Processor: 32-bit RISC
- Word Size: 32-bit
- Endianness: Little-endian
- Register Set:
  - 32 General Purpose Registers (32-bit)
  - Program Counter (32-bit)
  - System Registers
- Features:
  - 5-stage pipeline
  - Single-cycle execution
  - Integrated cache
  - Hardware multiply/divide

### Graphics Coprocessors
- Six background processors
- Two video output processors
- Motion decoder
- Rainbow palette processor

## Memory Map
### System Memory
- Main RAM: 2MB
- Video RAM: 1MB (2× 512KB)
- CD Buffer: 32KB
- ROM: 1MB (BIOS)
- Backup RAM: 32KB

### Memory Layout
- $00000000-$000FFFFF: BIOS ROM
- $00100000-$002FFFFF: Main RAM
- $00300000-$003FFFFF: Video RAM
- $00400000-$00FFFFFF: I/O Space
- $01000000-$01FFFFFF: CD-ROM Space

## Video System
### Graphics Processing
- Resolution: 256×240 to 480×240
- Colors: 16.7M (24-bit)
- Sprites: Hardware-assisted
- Features:
  - Hardware scaling
  - Video overlay
  - FMV acceleration
  - Multiple layers

### Display Features
- Resolutions:
  - 256×240
  - 320×240
  - 480×240
- Color Depths:
  - 24-bit (16.7M colors)
  - 16-bit (65,536 colors)
  - 256 colors/palette
- Features:
  - Hardware sprites
  - Background layers
  - Priority control
  - Rainbow palette effects

### Special Effects
- Hardware scaling
- Alpha blending
- Color mixing
- Chroma key
- FMV overlay
- Scan line effects

## Audio System
### Sound Processor
- Channels: 16
- Sample Rate: 44.1 kHz
- Features:
  - CD-quality audio
  - ADPCM support
  - Digital mixing
  - Real-time effects

### Audio Capabilities
- CD audio playback
- PCM sample playback
- Digital effects
- Multiple channels
- Volume control
- Pan control

## Input/Output System
### Controller Interface
- Two controller ports
- Mouse support
- Multitap support
- Memory card slot

### Storage Interface
- CD-ROM Drive:
  - Double speed
  - 680MB capacity
  - CD audio support
  - Photo CD support
- Backup Memory:
  - Internal battery backup
  - Memory card support

### Video Output
- Composite video
- S-Video
- RGB (21-pin)
- RF output (optional)

## System Integration Features
### Hardware Variants
- Standard PC-FX
- PC-FX GA (add-on graphics card for NEC PC-98)
- Development systems

### Regional Differences
- Japan-only release
- NTSC video only
- Single power standard
- No region lock

### Special Features
- Built-in font ROM
- JPEG decompression
- Motion JPEG support
- Backup memory

## Technical Legacy
### Hardware Innovations
- RISC processor
- FMV capabilities
- Multiple video processors
- Advanced 2D features

### Software Development
- Development Tools:
  - NEC SDK
  - PC-FX development system
  - Graphics tools
  - Sound tools
- Programming Features:
  - Direct hardware access
  - Video programming
  - Audio programming
  - CD streaming

### Market Impact
- Production Run: 1994-1998
- Units Sold: ~100,000
- Games Released: ~62
- Price Points:
  - Launch: ¥49,800
  - Final: ¥14,800
- Market Position:
  - Japan exclusive
  - FMV focus
  - Limited success
  - Niche market

## GameVM Implementation Notes
### Compiler Considerations
- Code Generation:
  - V810 instruction set
  - Graphics commands
  - DMA operations
  - CD access
- Register Allocation:
  - 32 general registers
  - System registers
  - Video registers
- Memory Management:
  - Main RAM
  - Video RAM
  - CD buffer
  - DMA control

### Performance Targets
- CPU: 21.5 MIPS
- Video: Multiple resolutions
- Audio: CD quality
- Memory Budget:
  - Main RAM: 2MB
  - Video RAM: 1MB
  - CD Buffer: 32KB
  - Backup RAM: 32KB

### Special Handling
- Graphics Pipeline
- CD-ROM Access
- FMV Playback
- Audio Processing
- Input Management

## References
- [PC-FX Technical Manual](http://www.pcfx.com/tech)
- [V810 Programming Guide](http://www.nec.com/v810)
- [PC-FX Development Documentation](http://www.pcfxdev.net)
- [NEC Graphics Programming](http://www.necdev.com/pcfx)
