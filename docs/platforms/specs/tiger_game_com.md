# Tiger Game.com

## System Overview
- Profile: GV.Spec.L3
- CPU: Sharp SM8521
- CPU Clock: 12 MHz
- Release Year: 1997
- Generation: 5th
- Region: Worldwide
- Predecessor: None
- Successor: Game.com Pocket Pro
- Notable Feature: First handheld with a touchscreen and Internet connectivity

## CPU Details
### Sharp SM8521
#### Architecture Characteristics
- Processor: 8-bit CPU
- Word Size: 8-bit
- Endianness: Little-endian
- Register Set:
  - Accumulator
  - Index Registers
  - Stack Pointer
  - Program Counter
  - Status Register
- Features:
  - Z80-like instruction set
  - Built-in LCD controller
  - Timer system
  - Serial interface

### System Architecture
- LCD Controller
- Sound Generator
- Timer/Counter
- I/O Ports
- Interrupt Controller

## Memory Map
### System Memory
- Main RAM: 32KB
- Video RAM: 16KB
- ROM: Up to 4MB (cartridge)
- System ROM: 1MB
- Serial EEPROM: 512 bytes

### Memory Layout
- $0000-$1FFF: System ROM
- $2000-$9FFF: Cartridge ROM
- $A000-$DFFF: RAM
- $E000-$FFFF: Video RAM

## Video System
### Graphics Processing
- Resolution: 200×160
- Display: 4 shades of gray
- Sprites: Hardware support
- Background: Tile-based

### Display Features
- LCD Screen:
  - Touch sensitive
  - 200×160 pixels
  - 4 gray levels
  - No backlight
- Features:
  - Sprite system
  - Tile mapping
  - Window support
  - Touch input

### Special Effects
- Hardware sprites
- Background scrolling
- Window overlay
- Touch detection
- Screen masking

## Audio System
### Sound Generation
- Type: Digital sound generator
- Channels: 2
- Features:
  - Square wave
  - Noise generation
  - Volume control
  - Mono output

### Audio Capabilities
- Two sound channels
- Variable frequency
- Volume control
- Simple effects
- Internal speaker
- Headphone support

## Input/Output System
### Controller Interface
- D-pad
- A, B, C, D buttons
- Menu, Sound, Pause
- Touchscreen
- Stylus input

### Storage Interface
- Game cartridges:
  - Up to 4MB ROM
  - Save capability
  - Multi-game carts
- External:
  - Serial port
  - Modem support
  - Link cable

### Communication
- 14.4K modem (optional)
- Serial link cable
- Internet connectivity
- Email capability

## System Integration Features
### Hardware Variants
- Original Game.com
- Game.com Pocket
- Game.com Pocket Pro
- Light variants

### Regional Differences
- Universal hardware
- No region lock
- Power requirements
- Modem standards

### Special Features
- Touchscreen input
- Internet access
- PDA functions
- Built-in applications

## Technical Legacy
### Hardware Innovations
- Touchscreen interface
- Internet connectivity
- PDA features
- Multiple buttons

### Software Development
- Development Tools:
  - Official SDK
  - Debug hardware
  - Programming tools
  - Graphics utilities
- Programming Features:
  - Assembly language
  - Graphics API
  - Touch input
  - Network stack

### Market Impact
- Production Run: 1997-2000
- Units Sold: ~300,000
- Games Released: ~20
- Price Points:
  - Launch: $69.99
  - Final: $29.99
- Market Position:
  - Gaming PDA
  - Internet device
  - Limited success
  - Innovative features

## GameVM Implementation Notes
### Compiler Considerations
- Code Generation:
  - SM8521 instructions
  - Touch handling
  - LCD control
  - Network code
- Register Allocation:
  - Limited registers
  - Memory mapping
  - I/O handling
- Memory Management:
  - Bank switching
  - Video memory
  - Network buffers
  - Touch data

### Performance Targets
- CPU: 12 MHz
- Graphics: LCD refresh
- Audio: 2 channels
- Memory Budget:
  - Main RAM: 32KB
  - Video RAM: 16KB
  - ROM: Up to 4MB
  - EEPROM: 512B

### Special Handling
- Touchscreen Input
- Network Stack
- Power Management
- LCD Control
- Sound Generation

## References
- [Game.com Technical Manual](http://game.com/docs/tech)
- [SM8521 Programming Guide](http://sharp.com/sm8521)
- [Game.com Development Kit](http://game.com/dev)
- [Tiger Electronics Documentation](http://tiger.com/gamecom)
