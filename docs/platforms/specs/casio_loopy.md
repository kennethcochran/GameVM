# Casio Loopy

## System Overview
- Profile: GV.Spec.L5
- CPU: NEC V30MZ
- CPU Clock: 20 MHz
- GPU: Custom sprite processor
- Release Year: 1995
- Generation: 4th/5th (transitional)
- Region: Japan only
- Predecessor: None (Casio's first console)
- Successor: None
- Notable Feature: Built-in thermal printer and "My Seal" creation system

## CPU Details
### NEC V30MZ
#### Architecture Characteristics
- Processor: 16-bit (8086 compatible)
- Word Size: 16-bit
- Endianness: Little-endian
- Register Set:
  - General Purpose Registers (16-bit)
  - Segment Registers
  - Flags Register
  - Instruction Pointer
- Features:
  - 8086 instruction set
  - Enhanced instructions
  - DMA support
  - Interrupt control

### Graphics Processor
- Custom sprite engine
- Tilemap processor
- Printer controller
- DMA controller

## Memory Map
### System Memory
- Main RAM: 512KB
- Video RAM: 256KB
- ROM: 1MB (system)
- Backup RAM: 8KB

### Memory Layout
- 0x00000-0x7FFFF: Main RAM
- 0x80000-0xBFFFF: Video RAM
- 0xC0000-0xFFFFF: System ROM
- I/O mapped devices

## Video System
### Graphics Processing
- Resolution: 320×240
- Colors: 256 from 32,768
- Sprites: Hardware support
- Layers: Multiple background planes

### Display Features
- Resolution: 320×240
- Color Depth: 8-bit
- Palette: 32,768 colors
- Features:
  - Hardware sprites
  - Background layers
  - Priority control
  - Color effects

### Special Effects
- Sprite scaling
- Rotation
- Color manipulation
- Priority control
- Layer blending
- Printer effects

## Audio System
### Sound Processing
- Type: PCM
- Channels: 8
- Sample Rate: 32 kHz
- Features:
  - Digital audio
  - Volume control
  - Pan control
  - Effects

### Audio Capabilities
- PCM playback
- Multiple channels
- Digital effects
- Volume control
- Stereo output
- Sound effects

## Input/Output System
### Controller Interface
- One controller port
- Printer interface
- Expansion port
- Mouse support

### Storage Interface
- ROM Cartridges:
  - Up to 16MB
  - Battery backup
  - Save data
- Thermal Printer:
  - Built-in
  - Sticker printing
  - Image capture
  - Custom designs

### Video Output
- Composite video
- RF output
- Printer output

## System Integration Features
### Hardware Variants
- Standard Loopy
- Development units
- Prototype versions

### Regional Differences
- Japan exclusive
- Single video standard
- Single power standard
- No region lock

### Special Features
- Thermal printer
- Seal creation
- Animation tools
- Image capture

## Technical Legacy
### Hardware Innovations
- Built-in printer
- Seal creation system
- Female market focus
- Creative tools

### Software Development
- Development Tools:
  - Casio SDK
  - Graphics tools
  - Printer utilities
  - Animation tools
- Programming Features:
  - Sprite control
  - Printer access
  - Sound control
  - DMA handling

### Market Impact
- Production Run: 1995-1996
- Units Sold: ~10,000
- Games Released: ~11
- Price Points:
  - Launch: ¥25,000
  - Final: ¥15,000
- Market Position:
  - Female demographic
  - Creative focus
  - Limited release
  - Niche market

## GameVM Implementation Notes
### Compiler Considerations
- Code Generation:
  - V30MZ instructions
  - 8086 compatibility
  - DMA control
  - Printer handling
- Register Allocation:
  - 16-bit registers
  - Segment registers
  - System resources
- Memory Management:
  - Segmentation
  - DMA control
  - Printer buffer
  - Video memory

### Performance Targets
- CPU: 20 MHz V30MZ
- Graphics: 320×240
- Audio: 32 kHz
- Memory Budget:
  - Main RAM: 512KB
  - Video RAM: 256KB
  - ROM: 1MB
  - Backup: 8KB

### Special Handling
- Printer Integration
- Sprite System
- Audio Processing
- Input Management
- Creative Tools

## References
- [Loopy Hardware Manual](http://www.casio.co.jp/loopy)
- [V30MZ Technical Reference](http://www.nec.com/v30mz)
- [Loopy Development Guide](http://www.loopydev.net)
- [Printer Programming Guide](http://www.casio.co.jp/loopy/printer)
