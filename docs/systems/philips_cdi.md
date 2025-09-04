# Philips CD-i

## System Overview
- CPU: 68070 VLSI (68000 derivative)
- CPU Clock: 15.5 MHz
- Release Year: 1991
- Generation: 4th/5th (transitional)
- Region: Worldwide
- Predecessor: None (Philips' first console)
- Successor: None
- Notable Feature: Multimedia platform with CD-ROM support

## CPU Details
### Main CPU (68070)
#### Architecture Characteristics
- Processor: Philips 68070 (68000 derivative)
- Word Size: 16/32-bit
- Endianness: Big-endian
- Register Set:
  - Data Registers (D0-D7): 32-bit
  - Address Registers (A0-A7): 32-bit
  - Program Counter (PC): 32-bit
  - Status Register (SR)
- Integrated Features:
  - UART
  - I2C interface
  - Memory management unit
  - DMA controller
  - Timer

### Memory Management Unit
- Address Space: 16MB
- Page Size: 2KB
- Protection Levels: Supervisor/User
- Features:
  - Virtual memory support
  - Memory protection
  - Address translation

## Memory Map
### System Memory
- Main RAM: 1MB
- Video RAM: 768KB
- ROM: 1MB (system)
- CD-ROM: Up to 650MB

### Memory Layout
- $000000-$0FFFFF: System ROM
- $100000-$1FFFFF: RAM
- $200000-$2BFFFF: Video RAM
- $2C0000-$2FFFFF: Reserved
- $300000-$3FFFFF: I/O Space

## Video System
### Graphics Processing
- Resolution: 384×280 to 768×560
- Colors: Up to 32,768 (15-bit)
- Planes: 2 background, 1 sprite/cursor
- Features:
  - Hardware scaling
  - CLUT support
  - Real-time video mixing
  - MPEG-1 decoding (with cartridge)

### Display Features
- Multiple Resolutions:
  - Normal: 384×280
  - Double: 768×560
  - Interlaced modes
- Color Depths:
  - DYUV (16-bit)
  - RGB555 (15-bit)
  - CLUT8 (8-bit)
  - CLUT7 (7-bit)

### Video Playback
- MPEG-1 Support (with cartridge)
- Digital Video (CD-i format)
- Motion JPEG
- Real-time scaling
- Video overlay

## Audio System
### MCD Audio Processor
- Sample Rate: 44.1 kHz
- Channels: 2 (stereo)
- Features:
  - CD-quality audio
  - ADPCM decoding
  - Real-time mixing
  - Level control

### Audio Capabilities
- CD Audio Playback
- ADPCM Encoding:
  - Level A (8-bit)
  - Level B (4-bit)
  - Level C (4-bit)
- Features:
  - Real-time mixing
  - Volume control
  - Audio effects
  - Multiple streams

## Input/Output System
### Controller Interface
- Two controller ports
- CD-i Controller:
  - D-pad or Thumbstick
  - Two buttons
  - Optional pointing device
- Special Controllers:
  - Touchpad
  - Mouse
  - Light gun
  - Trackball

### Storage Interface
- CD-ROM Drive:
  - Single speed
  - CD-i format
  - Audio CD support
  - Photo CD support
- Memory Cards (optional)

### Video Output
- Composite video
- S-Video
- RF output
- RGB SCART (European models)

## System Integration Features
### Hardware Variants
- CD-i 200 series
- CD-i 400 series
- CD-i 600 series
- Professional models

### Regional Differences
- TV standards (NTSC/PAL)
- Power supply
- Case design
- Software library

### Special Features
- Digital video cartridge (optional)
- Memory expansion
- Network capabilities
- Professional features

## Technical Legacy
### Hardware Innovations
- Integrated multimedia
- CD-ROM based
- Digital video support
- Professional applications

### Software Development
- Development Tools:
  - OS-9 based
  - CD-i specific tools
  - Multimedia authoring
  - Video encoding
- Programming Features:
  - Real-time multimedia
  - Interactive features
  - Video synchronization
  - Audio mixing

### Market Impact
- Production Run: 1991-1998
- Units Sold: ~1 million
- Titles Released: ~200
- Price Points:
  - Launch: $799
  - Final: $399
- Market Position:
  - Multimedia platform
  - Educational focus
  - Home entertainment
  - Professional use

## GameVM Implementation Notes
### Compiler Considerations
- Code Generation:
  - 68070 instructions
  - OS-9 compatibility
  - Real-time constraints
  - Multimedia sync
- Register Allocation:
  - System resources
  - DMA coordination
  - I/O handling
- Memory Management:
  - MMU utilization
  - CD-ROM buffering
  - Video memory

### Performance Targets
- Frame Rate: 30/60 Hz
- Audio: CD quality
- Video: Full screen
- Memory Budget:
  - Main RAM: 1MB
  - Video RAM: 768KB
  - System ROM: 1MB
  - CD-ROM buffer

### Special Handling
- CD-ROM Access
- Video Playback
- Audio Synchronization
- Input Device Support
- Real-time Constraints

## References
- [CD-i Hardware Documentation](http://www.icdia.co.uk/hardware)
- [68070 Technical Reference](http://www.nxp.com/docs/68070.pdf)
- [CD-i Developer Guide](http://www.cdidev.org/docs)
- [Multimedia Kernel Documentation](http://www.os9.tech/cdi)
