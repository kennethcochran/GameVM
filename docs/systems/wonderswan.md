# Bandai WonderSwan

## System Overview
- CPU: NEC V30 MZ
- CPU Clock: 3.072 MHz
- Release Year: 1999
- Generation: 5th/6th (transitional)
- Region: Japan, Asia
- Predecessor: None
- Successor: WonderSwan Color
- Notable Feature: Designed by Game Boy creator Gunpei Yokoi, vertical/horizontal screen orientation

## CPU Details
### NEC V30 MZ
#### Architecture Characteristics
- Processor: 16-bit (80186 compatible)
- Word Size: 16-bit
- Endianness: Little-endian
- Register Set:
  - AX, BX, CX, DX (General Purpose)
  - SI, DI (Index)
  - BP, SP (Pointers)
  - CS, DS, ES, SS (Segments)
  - IP (Instruction Pointer)
  - FLAGS
- Features:
  - x86 instruction set
  - Hardware multiply/divide
  - DMA support
  - Power management

### System Architecture
- DMA Controller
- Timer/Counter
- Interrupt Controller
- Power Management Unit

## Memory Map
### System Memory
- Main RAM: 16KB
- Video RAM: 4KB
- ROM: Up to 128MB (cartridge)
- Display RAM: 512 bytes
- Sprite Table: 32×128 bytes

### Memory Layout
- $00000-$0FFFF: System ROM
- $10000-$13FFF: Work RAM
- $14000-$14FFF: Display RAM
- $15000-$15FFF: Sprite Table
- $16000-$17FFF: Tile Data

## Video System
### Graphics Processing
- Resolution: 224×144
- Colors: 8 shades of gray
- Sprites: 128 hardware sprites
- Planes: 2 background layers

### Display Features
- Screen Modes:
  - Vertical orientation
  - Horizontal orientation
- Display:
  - 2.49" LCD
  - Reflective screen
  - No backlight
  - High contrast

### Special Effects
- Hardware sprite scaling
- Screen rotation
- Window feature
- Line scroll
- Priority control

## Audio System
### Sound Generation
- Channels: 4
- Wave Types:
  - Square waves
  - Noise
  - PCM
- Features:
  - Variable duty cycle
  - Volume control
  - Frequency control
  - Stereo output

### Audio Capabilities
- 4 simultaneous channels
- Programmable wave memory
- Volume envelope
- Sweep functions
- Internal speaker
- Headphone output

## Input/Output System
### Controller Interface
- D-pad (×2)
- A, B buttons
- Start, Select buttons
- X1-X4 buttons
- Y1-Y4 buttons

### Storage Interface
- Game cartridges:
  - Up to 128MB ROM
  - Battery backup
  - SRAM/EEPROM
- External:
  - Link cable port
  - Power input

### Display Output
- Built-in LCD screen
- Link cable for multiplayer
- No TV output

## System Integration Features
### Hardware Variants
- WonderSwan (original)
- WonderSwan Color
- SwanCrystal
- Development units

### Regional Differences
- Japan/Asia exclusive
- No region lock
- Universal cartridge format
- Japanese text only

### Special Features
- Vertical/horizontal play
- Low power consumption
- Multiple control layouts
- Unique button arrangement

## Technical Legacy
### Hardware Innovations
- Dual screen orientation
- Extended button layout
- Power efficiency
- x86 architecture

### Software Development
- Development Tools:
  - Official SDK
  - GNU tools
  - Debugging hardware
  - Emulators
- Programming Features:
  - x86 assembly
  - Screen rotation
  - Power management
  - Sound synthesis

### Market Impact
- Production Run: 1999-2003
- Units Sold: ~3.5 million
- Games Released: ~108
- Price Points:
  - Launch: ¥4,800
  - Final: ¥2,980
- Market Position:
  - Game Boy competitor
  - Japan exclusive
  - Energy efficient
  - Innovative design

## GameVM Implementation Notes
### Compiler Considerations
- Code Generation:
  - x86 instruction set
  - Screen orientation
  - Power optimization
  - DMA usage
- Register Allocation:
  - x86 registers
  - Segment handling
  - Stack management
- Memory Management:
  - Limited RAM
  - DMA control
  - Display buffer
  - Sprite tables

### Performance Targets
- CPU: 3.072 MHz
- Graphics: 75 fps
- Audio: 4 channels
- Memory Budget:
  - Main RAM: 16KB
  - Video RAM: 4KB
  - Display RAM: 512B
  - Sprite RAM: 4KB

### Special Handling
- Screen Rotation
- Power Management
- Button Mapping
- Sound Generation
- DMA Operations

## References
- [WonderSwan Hardware Manual](http://daifukkat.su/docs/wsman)
- [NEC V30MZ Documentation](http://www.nec.com/v30)
- [WonderSwan Development Guide](http://wonderswan.org/dev)
- [x86 Programming Reference](http://www.intel.com/design/literature/manuals)
