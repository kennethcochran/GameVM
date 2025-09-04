# Hartung Game Master

## System Overview
- CPU: Z80A compatible
- CPU Clock: 4 MHz
- Release Year: 1990
- Generation: 4th
- Region: Europe
- Predecessor: None
- Successor: None
- Notable Feature: Game Boy-like hardware with unique color LCD technology

## CPU Details
### Z80A Compatible
#### Architecture Characteristics
- Processor: 8-bit
- Word Size: 8-bit
- Endianness: Little-endian
- Register Set:
  - Accumulator (A)
  - General Purpose (B, C, D, E, H, L)
  - Index Registers (IX, IY)
  - Stack Pointer
  - Program Counter
  - Flags Register
- Features:
  - Z80 instruction set
  - Interrupt modes
  - Block instructions
  - I/O ports

### System Architecture
- Custom video controller
- Sound generator
- Memory controller
- I/O controller

## Memory Map
### System Memory
- Work RAM: 8KB
- Video RAM: 8KB
- ROM: 4KB (boot)
- Cartridge ROM: Up to 128KB

### Memory Layout
- $0000-$3FFF: Boot ROM/Cart ROM
- $4000-$7FFF: Cart ROM Bank
- $8000-$9FFF: Video RAM
- $C000-$DFFF: Work RAM
- $F000-$FFFF: System RAM

## Video System
### Graphics Processing
- Resolution: 160×144
- Colors: 4 colors
- Sprites: 40 hardware sprites
- Background layer system

### Display Features
- LCD Screen:
  - 160×144 pixels
  - 4 unique colors
  - Custom LCD tech
  - No backlight
- Sprite Features:
  - 8×8 or 8×16 pixels
  - Multiple colors
  - Priority control
  - Collision detection

### Special Effects
- Hardware scrolling
- Color manipulation
- Sprite priorities
- Screen masking
- Background effects

## Audio System
### Sound Generation
- Channels: 3
  - 2 square wave
  - 1 noise
- Features:
  - Volume control
  - Frequency control
  - Basic envelopes
  - Mono output

### Audio Capabilities
- Square wave synthesis
- Noise generation
- Volume envelope
- Basic effects
- Internal speaker
- Headphone output

## Input/Output System
### Controller Interface
- D-pad
- A, B buttons
- Start, Select
- Volume control

### Storage Interface
- Game cartridges:
  - Up to 128KB ROM
  - No save capability
  - Custom format
  - Simple connector
- External:
  - Power input
  - Headphone jack
  - AC adapter port

### Display Output
- Built-in LCD
- Custom color system
- No TV output
- Contrast control

## System Integration Features
### Hardware Variants
- Standard model
- European variants
- Development units
- Prototype versions

### Regional Differences
- Europe focused
- Single video standard
- Power requirements
- Game library

### Special Features
- Custom color LCD
- Game Boy-like hardware
- Unique cartridge format
- Power efficiency

## Technical Legacy
### Hardware Innovations
- Color LCD technology
- Game Boy-like architecture
- Cost-effective design
- European market focus

### Software Development
- Development Tools:
  - Z80 assembly tools
  - Graphics utilities
  - Sound editor
  - Debug hardware
- Programming Features:
  - Direct hardware access
  - Color control
  - Sound programming
  - Sprite handling

### Market Impact
- Production Run: 1990-1992
- Units Sold: Unknown
- Games Released: ~20
- Price Points:
  - Launch: £49.99
  - Final: £29.99
- Market Position:
  - Budget handheld
  - Limited distribution
  - Game Boy competitor
  - European focus

## GameVM Implementation Notes
### Compiler Considerations
- Code Generation:
  - Z80 instructions
  - Video timing
  - Color handling
  - Sound control
- Register Allocation:
  - Z80 registers
  - Memory mapping
  - I/O ports
- Memory Management:
  - Bank switching
  - Video memory
  - Work RAM
  - Stack space

### Performance Targets
- CPU: 4 MHz
- Graphics: 60 fps
- Audio: 3 channels
- Memory Budget:
  - Work RAM: 8KB
  - Video RAM: 8KB
  - Boot ROM: 4KB
  - Cart ROM: 128KB max

### Special Handling
- Color System
- Sound Generation
- Input Processing
- Memory Banking
- Power Management

## References
- [Game Master Hardware Guide](http://hartung-systems.org)
- [Z80 Programming Manual](http://zilog.com/docs)
- [Display System Documentation](http://gamemaster-dev.net)
- [Development Tools Guide](http://hartung-dev.org)
