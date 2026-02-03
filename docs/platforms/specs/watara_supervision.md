# Watara Supervision

## System Overview
- Profile: GV.Spec.L3
- CPU: WDC 65C02
- CPU Clock: 4 MHz
- Release Year: 1992
- Generation: 4th
- Region: Worldwide
- Predecessor: None
- Successor: None
- Notable Feature: Game Boy competitor with TV-out capability

## CPU Details
### WDC 65C02
#### Architecture Characteristics
- Processor: 8-bit CMOS
- Word Size: 8-bit
- Endianness: Little-endian
- Register Set:
  - Accumulator (A)
  - Index Registers (X, Y)
  - Stack Pointer
  - Program Counter
  - Status Register
- Features:
  - Enhanced 6502 instruction set
  - Low power CMOS
  - Additional addressing modes
  - Bug fixes from NMOS 6502

### System Architecture
- Custom video controller
- Sound generator
- Memory controller
- I/O controller

## Memory Map
### System Memory
- Work RAM: 8KB
- Video RAM: 8KB
- ROM: 2KB (boot)
- Cartridge ROM: Up to 64KB

### Memory Layout
- $0000-$1FFF: Work RAM
- $2000-$3FFF: Video RAM
- $4000-$7FFF: Reserved
- $8000-$FFFF: Cartridge ROM
- Zero Page: System variables

## Video System
### Graphics Processing
- Resolution: 160×160
- Colors: 4 shades of gray
- Sprites: No hardware sprites
- Tile-based graphics

### Display Features
- LCD Screen:
  - 160×160 pixels
  - 4 gray levels
  - No backlight
  - Adjustable contrast
- TV Output:
  - NTSC/PAL
  - Enhanced resolution
  - Color capabilities
  - External adapter

### Special Effects
- Tile manipulation
- Screen scrolling
- Gray scale control
- TV color modes

## Audio System
### Sound Generation
- Channels: 2
- Features:
  - Square waves
  - Volume control
  - Basic effects
  - Mono output

### Audio Capabilities
- Two sound channels
- Variable frequency
- Volume control
- Basic envelopes
- Internal speaker
- TV audio output

## Input/Output System
### Controller Interface
- Built-in D-pad
- A, B buttons
- Start, Select
- External controller port

### Storage Interface
- Game cartridges:
  - Up to 64KB ROM
  - No save capability
  - Simple connector
- External:
  - TV output
  - Power input
  - Link port

### Display Output
- Built-in LCD
- TV output port
- Contrast control
- External power

## System Integration Features
### Hardware Variants
- Supervision
- Quickshot Supervision
- Multiple clones
- TV adapter versions

### Regional Differences
- Universal hardware
- TV standard adapters
- Power requirements
- Case designs

### Special Features
- TV output capability
- Link cable support
- External controller
- Multiple variants

## Technical Legacy
### Hardware Innovations
- TV output handheld
- Low cost design
- Simple architecture
- Clone friendly

### Software Development
- Development Tools:
  - Assembly development
  - Basic graphics tools
  - Sound utilities
  - Debug hardware
- Programming Features:
  - Direct hardware access
  - Graphics control
  - Sound programming
  - I/O handling

### Market Impact
- Production Run: 1992-1996
- Units Sold: Unknown
- Games Released: ~70
- Price Points:
  - Launch: $49.99
  - Final: $19.99
- Market Position:
  - Budget handheld
  - Game Boy competitor
  - Limited success
  - Multiple markets

## GameVM Implementation Notes
### Compiler Considerations
- Code Generation:
  - 65C02 instructions
  - Video handling
  - Sound control
  - I/O management
- Register Allocation:
  - Limited registers
  - Zero page usage
  - Stack operations
- Memory Management:
  - Work RAM
  - Video RAM
  - ROM banking
  - Zero page

### Performance Targets
- CPU: 4 MHz
- Graphics: LCD refresh
- Audio: 2 channels
- Memory Budget:
  - Work RAM: 8KB
  - Video RAM: 8KB
  - Boot ROM: 2KB
  - Cart ROM: 64KB max

### Special Handling
- LCD Control
- TV Output
- Sound Generation
- Input Processing
- Power Management

## References
- [Supervision Hardware Manual](http://supervision-console.org)
- [65C02 Programming Reference](http://wdc65xx.com/docs)
- [Display System Documentation](http://supervision-dev.net)
- [Game Development Guide](http://supervision-games.org)
