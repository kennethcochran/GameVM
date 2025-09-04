# Epoch Super Cassette Vision

## System Overview
- CPU: NEC µPD7801G
- CPU Clock: 4 MHz
- Release Year: 1984
- Generation: 3rd
- Region: Japan
- Predecessor: Cassette Vision
- Successor: None
- Notable Feature: First console with a 16-bit video chip

## CPU Details
### NEC µPD7801G
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
  - Enhanced instruction set
  - Built-in timer
  - I/O ports
  - Interrupt control

### Graphics Processor
- Custom 16-bit video chip
- Hardware sprites
- Background scrolling
- Color palette control

## Memory Map
### System Memory
- Work RAM: 2KB
- Video RAM: 4KB
- ROM: 8KB (system)
- Cartridge ROM: Up to 32KB

### Memory Layout
- $0000-$1FFF: System ROM
- $2000-$27FF: Work RAM
- $2800-$37FF: Video RAM
- $4000-$BFFF: Cartridge ROM
- I/O ports: Hardware registers

## Video System
### Graphics Processing
- Resolution: 309×246
- Colors: 16 from palette of 4096
- Sprites: 8 hardware sprites
- Background: Tile-based

### Display Features
- Sprite System:
  - 16×16 or 32×32 pixels
  - Individual colors
  - Priority control
  - Collision detection
- Background:
  - Tile-based
  - Scrolling
  - Multiple colors
  - Pattern tables

### Special Effects
- Hardware scrolling
- Sprite manipulation
- Color cycling
- Screen masking
- Priority control

## Audio System
### Sound Generation
- Type: Programmable Sound Generator
- Channels: 3
- Features:
  - Square waves
  - Noise channel
  - Volume control
  - Stereo output

### Audio Capabilities
- Three sound channels
- Variable frequency
- Envelope control
- Noise generation
- Volume control
- Stereo sound

## Input/Output System
### Controller Interface
- Two controller ports
- Digital joystick
- Action buttons
- Start button

### Storage Interface
- Game cartridges:
  - Up to 32KB ROM
  - Enhanced connector
  - No save capability
- External:
  - Power input
  - AV output

### Video Output
- Composite video
- RF output
- NTSC format
- Enhanced quality

## System Integration Features
### Hardware Variants
- Standard model
- European model (limited)
- Development units
- Prototype versions

### Regional Differences
- Primary Japanese release
- Limited European release
- NTSC/PAL versions
- Power standards

### Special Features
- 16-bit video processor
- Enhanced audio
- Improved controllers
- Larger game capacity

## Technical Legacy
### Hardware Innovations
- Early 16-bit graphics
- Enhanced sound
- Sprite system
- Large ROM support

### Software Development
- Development Tools:
  - Assembly tools
  - Graphics utilities
  - Sound editor
  - Debug hardware
- Programming Features:
  - Sprite control
  - Background handling
  - Sound programming
  - I/O management

### Market Impact
- Production Run: 1984-1987
- Units Sold: ~30,000
- Games Released: ~30
- Price Points:
  - Launch: ¥14,800
  - Final: ¥8,800
- Market Position:
  - Advanced hardware
  - Limited market
  - Japan focus
  - Technical pioneer

## GameVM Implementation Notes
### Compiler Considerations
- Code Generation:
  - µPD7801G instructions
  - Video chip commands
  - Memory management
  - I/O handling
- Register Allocation:
  - CPU registers
  - Video registers
  - Sound registers
- Memory Management:
  - Work RAM
  - Video RAM
  - ROM banking
  - Hardware registers

### Performance Targets
- CPU: 4 MHz
- Graphics: 60 Hz
- Audio: 3 channels
- Memory Budget:
  - Work RAM: 2KB
  - Video RAM: 4KB
  - System ROM: 8KB
  - Cart ROM: 32KB max

### Special Handling
- Sprite System
- Background Scrolling
- Sound Generation
- Controller Input
- Video Output

## References
- [Super Cassette Vision Hardware Guide](http://epoch.jp/scv/tech)
- [µPD7801G Programming Manual](http://nec.com/upd7801g)
- [Graphics Chip Documentation](http://epoch.jp/scv/graphics)
- [Development Tools Guide](http://scvdev.net)
