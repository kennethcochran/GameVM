# Epoch Cassette Vision

## System Overview
- Profile: GV.Spec.L1
- Profile: GV.Spec.L1
- CPU: NEC µPD7810
- CPU Clock: 4 MHz
- Release Year: 1981
- Generation: 2nd
- Region: Japan
- Predecessor: None
- Successor: Super Cassette Vision
- Notable Feature: First Japanese cartridge-based game console

## CPU Details
### NEC µPD7810
#### Architecture Characteristics
- Processor: 8-bit microcontroller
- Word Size: 8-bit
- Endianness: Little-endian
- Register Set:
  - Accumulator (A)
  - General Purpose (V, H, B, C, D, E, L)
  - Program Counter
  - Stack Pointer
  - Program Status Word
- Features:
  - Built-in timer
  - I/O ports
  - Interrupt control
  - Serial interface

### System Architecture
- Video Controller
- Sound Generator
- I/O Controller
- Memory Controller

## Memory Map
### System Memory
- Internal RAM: 128 bytes
- Work RAM: 1KB
- ROM: 1KB (system)
- Cartridge ROM: Up to 4KB

### Memory Layout
- $0000-$03FF: System ROM
- $0400-$07FF: Work RAM
- $0800-$1FFF: Cartridge ROM
- Internal RAM:
  - 128 bytes
  - Register space
  - I/O ports

## Video System
### Graphics Processing
- Resolution: 128×96
- Colors: 8
- Sprites: No hardware sprites
- Character-based graphics

### Display Features
- Character-based display
- Fixed color palette
- Basic animation
- Simple scrolling

### Special Effects
- Character manipulation
- Color changes
- Basic animation
- Screen masking

## Audio System
### Sound Generation
- Type: Simple tone generator
- Channels: 1
- Features:
  - Square wave
  - Volume control
  - Basic effects
  - Mono output

### Audio Capabilities
- Single channel
- Basic tones
- Volume control
- Sound effects
- Internal speaker

## Input/Output System
### Controller Interface
- Two controller ports
- Digital joystick
- Fire buttons
- Built-in controls

### Storage Interface
- Game cartridges:
  - Up to 4KB ROM
  - Simple connector
  - No save capability
- External:
  - Power input
  - TV output

### Video Output
- RF output
- NTSC video
- TV connection
- Mono audio

## System Integration Features
### Hardware Variants
- Standard model
- Development units
- Japanese market only
- No regional variants

### Regional Differences
- Japan exclusive
- NTSC video only
- Single power standard
- No export versions

### Special Features
- Built-in controllers
- Simple design
- Cartridge system
- Basic graphics

## Technical Legacy
### Hardware Innovations
- First Japanese cartridge console
- Microcontroller-based
- Simple architecture
- Home market focus

### Software Development
- Development Tools:
  - Assembly language
  - Basic utilities
  - Simple graphics
  - Sound tools
- Programming Features:
  - Direct hardware access
  - Character graphics
  - Sound control
  - Input handling

### Market Impact
- Production Run: 1981-1984
- Units Sold: Unknown
- Games Released: ~15
- Price Points:
  - Launch: ¥13,500
  - Final: ¥7,000
- Market Position:
  - Early Japanese console
  - Limited game library
  - Domestic market
  - Pioneer system

## GameVM Implementation Notes
### Compiler Considerations
- Code Generation:
  - µPD7810 instructions
  - Memory mapping
  - I/O handling
  - Graphics output
- Register Allocation:
  - Limited registers
  - Internal RAM
  - Work RAM
- Memory Management:
  - Small RAM size
  - Character data
  - Sound control
  - I/O mapping

### Performance Targets
- CPU: 4 MHz
- Graphics: TV refresh
- Audio: Single channel
- Memory Budget:
  - Internal RAM: 128 bytes
  - Work RAM: 1KB
  - System ROM: 1KB
  - Cart ROM: 4KB max

### Special Handling
- Character Graphics
- Sound Generation
- Controller Input
- TV Output
- Memory Management

## References
- [Cassette Vision Hardware Manual](http://epoch.jp/cv/manual)
- [µPD7810 Technical Reference](http://nec.com/upd7810)
- [Console Development Guide](http://cvdev.jp)
- [System Architecture Documentation](http://epoch-console.org)
