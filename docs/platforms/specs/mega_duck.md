# Mega Duck/Cougar Boy

## System Overview
- Profile: GV.Spec.L3
- Profile: GV.Spec.L3
- CPU: Sharp LR35902 (modified Z80)
- CPU Clock: 4 MHz
- Release Year: 1993
- Generation: 4th
- Region: Europe, Asia
- Predecessor: None
- Successor: None
- Notable Feature: Game Boy-compatible architecture with unique cartridge format

## CPU Details
### Sharp LR35902
#### Architecture Characteristics
- Processor: 8-bit (modified Z80)
- Word Size: 8-bit
- Endianness: Little-endian
- Register Set:
  - A (Accumulator)
  - B, C, D, E, H, L (General Purpose)
  - SP (Stack Pointer)
  - PC (Program Counter)
  - Flags: Zero, Subtract, Half Carry, Carry
- Features:
  - Z80-like instruction set
  - Hardware sprite support
  - Timer system
  - Interrupt control

### Graphics Processor
- Integrated in CPU
- Tile-based engine
- Sprite system
- Background layer

## Memory Map
### System Memory
- Work RAM: 8KB
- Video RAM: 8KB
- ROM: 2KB (boot)
- Cartridge ROM: Up to 1MB

### Memory Layout
- $0000-$3FFF: ROM Bank 0
- $4000-$7FFF: ROM Bank n
- $8000-$9FFF: Video RAM
- $A000-$BFFF: External RAM
- $C000-$DFFF: Work RAM
- $FE00-$FE9F: OAM
- $FF00-$FF7F: I/O Registers
- $FF80-$FFFE: High RAM

## Video System
### Graphics Processing
- Resolution: 160×144
- Colors: 4 shades of gray
- Sprites: 40 hardware sprites
- Layers: Background, Window, Sprites

### Display Features
- LCD Screen:
  - 160×144 pixels
  - 4 gray levels
  - No backlight
  - Adjustable contrast
- Sprite Features:
  - 8×8 or 8×16 pixels
  - Up to 10 per line
  - Priority control
  - Flip attributes

### Special Effects
- Hardware scrolling
- Window overlay
- Sprite priorities
- LCD control
- Screen blanking

## Audio System
### Sound Generation
- Channels: 4
  - 2 square wave
  - 1 programmable wave
  - 1 noise
- Features:
  - Volume envelope
  - Frequency sweep
  - Wave patterns
  - Stereo panning

### Audio Capabilities
- Square wave synthesis
- Noise generation
- Wave table playback
- Volume control
- Stereo output
- Channel mixing

## Input/Output System
### Controller Interface
- D-pad
- A, B buttons
- Start, Select
- Link port

### Storage Interface
- Game cartridges:
  - Up to 1MB ROM
  - External RAM
  - Unique format
  - No MBC support
- External:
  - Link cable
  - Power input
  - Headphone jack

### Display Output
- Built-in LCD
- Contrast control
- No TV output
- Link capabilities

## System Integration Features
### Hardware Variants
- Mega Duck (Europe)
- Cougar Boy (Asia)
- Development units
- Clone versions

### Regional Differences
- Power standards
- Case design
- Game library
- Marketing

### Special Features
- Game Boy-like architecture
- Unique cartridge format
- Link cable support
- Battery efficiency

## Technical Legacy
### Hardware Innovations
- Game Boy architecture clone
- Cost-effective design
- Compatible instruction set
- Unique storage format

### Software Development
- Development Tools:
  - Modified GB tools
  - Graphics utilities
  - Sound editor
  - Debugging hardware
- Programming Features:
  - Z80 assembly
  - Graphics control
  - Sound programming
  - I/O handling

### Market Impact
- Production Run: 1993-1995
- Units Sold: Unknown
- Games Released: ~30
- Price Points:
  - Launch: $49.99
  - Final: $29.99
- Market Position:
  - Budget handheld
  - Limited markets
  - Game Boy alternative
  - Short lifespan

## GameVM Implementation Notes
### Compiler Considerations
- Code Generation:
  - LR35902 instructions
  - Memory management
  - Video timing
  - Sound control
- Register Allocation:
  - Limited registers
  - Memory mapping
  - Stack usage
- Memory Management:
  - Bank switching
  - VRAM access
  - OAM DMA
  - Work RAM

### Performance Targets
- CPU: 4 MHz
- Graphics: 60 fps
- Audio: 4 channels
- Memory Budget:
  - Work RAM: 8KB
  - Video RAM: 8KB
  - Boot ROM: 2KB
  - Cart ROM: Up to 1MB

### Special Handling
- LCD Control
- Sound Generation
- Input Processing
- Memory Banking
- Power Management

## References
- [Mega Duck Hardware Manual](http://megaduck.org/docs)
- [LR35902 Technical Reference](http://sharp.com/cpu)
- [Game Development Guide](http://megaduck-dev.net)
- [System Architecture Documentation](http://cougarboy.org/tech)
