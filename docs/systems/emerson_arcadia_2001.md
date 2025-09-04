# Emerson Arcadia 2001

## System Overview
- CPU: Signetics 2650A
- CPU Clock: 3.58 MHz
- Release Year: 1982
- Generation: 2nd
- Region: Worldwide
- Predecessor: None
- Successor: None
- Notable Feature: Multiple regional variants and clones

## CPU Details
### Signetics 2650A
#### Architecture Characteristics
- Processor: 8-bit
- Word Size: 8-bit
- Endianness: Little-endian
- Register Set:
  - R0-R7 (6 general purpose)
  - Program Status Word
  - Program Counter
- Features:
  - 75 instructions
  - Hardware arithmetic
  - Indexed addressing
  - Interrupt support

### System Architecture
- Video Display Processor
- Sound Generator
- I/O Controller
- Memory Controller

## Memory Map
### System Memory
- Main RAM: 1KB
- Video RAM: 2KB
- ROM: 2KB (system)
- Cartridge ROM: Up to 8KB

### Memory Layout
- $0000-$07FF: System ROM
- $0800-$0BFF: RAM
- $0C00-$13FF: Video RAM
- $1400-$3FFF: Cartridge ROM

## Video System
### Graphics Processing
- Resolution: 128×208
- Colors: 8
- Sprites: No hardware sprites
- Display: Character-based

### Display Features
- Character-based graphics
- Fixed color palette
- No sprite hardware
- Simple scrolling

### Special Effects
- Basic animation
- Character manipulation
- Color changes
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
- Frequency control
- Volume control
- Basic sound effects
- Internal speaker

## Input/Output System
### Controller Interface
- Two controller ports
- Numeric keypad
- Action buttons
- Side buttons

### Storage Interface
- Game cartridges:
  - Up to 8KB ROM
  - No save capability
  - Simple connection
- External:
  - Controller ports
  - Power input

### Video Output
- RF output
- TV connection
- NTSC/PAL variants

## System Integration Features
### Hardware Variants
- Emerson Arcadia 2001
- Bandai Super Vision 8000
- MPT-03
- Numerous clones

### Regional Differences
- TV standards
- Case design
- Controller layout
- Game library

### Special Features
- Built-in games
- Numeric keypad
- Multiple controllers
- Regional variants

## Technical Legacy
### Hardware Innovations
- Unique processor choice
- Multiple variants
- International release
- Controller design

### Software Development
- Development Tools:
  - Assembly language
  - Basic tools
  - Limited documentation
  - Simple graphics
- Programming Features:
  - Direct hardware access
  - Character graphics
  - Sound control
  - Input handling

### Market Impact
- Production Run: 1982-1984
- Units Sold: Unknown
- Games Released: ~35
- Price Points:
  - Launch: $99
  - Final: $49
- Market Position:
  - Budget console
  - Multiple markets
  - Limited success
  - Many variants

## GameVM Implementation Notes
### Compiler Considerations
- Code Generation:
  - 2650A instructions
  - Memory mapping
  - I/O handling
  - Graphics output
- Register Allocation:
  - Limited registers
  - Memory access
  - Stack usage
- Memory Management:
  - Small RAM
  - Video memory
  - ROM access
  - I/O mapping

### Performance Targets
- CPU: 3.58 MHz
- Graphics: TV refresh rate
- Audio: Single channel
- Memory Budget:
  - Main RAM: 1KB
  - Video RAM: 2KB
  - System ROM: 2KB
  - Cart ROM: 8KB max

### Special Handling
- Character Graphics
- Sound Generation
- Controller Input
- TV Output
- Memory Mapping

## References
- [Arcadia 2001 Technical Guide](http://arcadia.console.info)
- [Signetics 2650A Manual](http://datasheets.chipdb.org/Signetics/2650A)
- [Arcadia Development Notes](http://arcadiadev.com)
- [Console Variants Documentation](http://consolevariants.com/arcadia)
