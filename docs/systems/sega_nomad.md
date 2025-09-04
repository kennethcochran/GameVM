# Sega Nomad

## System Overview
- CPU: Motorola 68000
- CPU Clock: 7.67 MHz
- Sound CPU: Zilog Z80
- Release Year: 1995
- Generation: 4th
- Region: North America
- Predecessor: Game Gear
- Successor: None
- Notable Feature: Portable Sega Genesis/Mega Drive with full compatibility

## CPU Details
### Main CPU (68000)
#### Architecture Characteristics
- Processor: Motorola 68000
- Word Size: 16-bit (internally 32-bit)
- Endianness: Big-endian
- Register Set:
  - Data Registers (D0-D7): 32-bit
  - Address Registers (A0-A7): 32-bit
  - Program Counter (PC): 32-bit
  - Status Register (SR)
- Features:
  - 16/32-bit operations
  - 14 addressing modes
  - Hardware multiply/divide
  - Supervisor/user modes

### Sound CPU (Z80)
#### Architecture Characteristics
- Processor: Zilog Z80
- Clock Speed: 3.58 MHz
- Word Size: 8-bit
- Register Set:
  - Primary/alternate register sets
  - Index registers (IX, IY)
  - Stack pointer
  - Program counter
- Purpose: Sound control and FM synthesis

## Memory Map
### System Memory
- Main RAM: 64KB
- Video RAM: 64KB
- Sound RAM: 8KB
- ROM: Genesis cartridge

### Memory Layout
- $000000-$3FFFFF: Cartridge ROM
- $A00000-$A0FFFF: Z80 RAM
- $C00000-$C0FFFF: VDP registers
- $E00000-$E0FFFF: RAM
- $FF0000-$FFFFFF: System ROM

## Video System
### Graphics Processing
- Resolution: 320×224
- Colors: 512 (64 simultaneous)
- Sprites: 80 on screen
- Planes: 2 scrolling backgrounds

### Display Features
- LCD Screen:
  - 3.25" diagonal
  - Backlit display
  - 320×224 resolution
- TV Output:
  - NTSC video
  - Composite out
  - Full resolution

### Special Effects
- Hardware scrolling
- Multiple background layers
- Sprite priorities
- Window plane
- Shadow/highlight

## Audio System
### Sound Generation
- FM Synthesis:
  - Yamaha YM2612
  - 6 FM channels
  - Stereo output
- PSG:
  - Texas Instruments SN76489
  - 4 channels
  - 3 square waves
  - 1 noise channel

### Audio Capabilities
- FM synthesis
- PSG synthesis
- Digital playback (PCM)
- Stereo output
- Multiple channels
- Volume control

## Input/Output System
### Controller Interface
- Built-in 6-button controller
- Second controller port
- Video out
- Headphone jack

### Storage Interface
- Genesis cartridge slot
- Save battery backup
- External power port
- A/V out port

### Video Output
- Built-in LCD
- Composite video
- RF adapter support
- NTSC format

## System Integration Features
### Hardware Variants
- Single model (MK-6100)
- Development units
- TV tuner (unreleased)

### Regional Differences
- North America only
- NTSC video
- 120V power
- Genesis cartridges

### Special Features
- Genesis compatibility
- Portable design
- TV output
- Two-player support

## Technical Legacy
### Hardware Innovations
- Portable Genesis
- Full compatibility
- LCD technology
- Battery operation

### Software Development
- Development Tools:
  - Standard Genesis tools
  - LCD optimization
  - Power management
  - Display tools
- Programming Features:
  - Genesis compatible
  - LCD considerations
  - Power awareness
  - Display scaling

### Market Impact
- Production Run: 1995-1996
- Units Sold: ~1 million
- Compatible Games: ~1,000
- Price Points:
  - Launch: $179.99
  - Final: $79.99
- Market Position:
  - Portable Genesis
  - High power usage
  - Limited battery life
  - Niche market

## GameVM Implementation Notes
### Compiler Considerations
- Code Generation:
  - 68000 instructions
  - Z80 sound code
  - LCD handling
  - Power management
- Register Allocation:
  - Genesis compatible
  - Power optimized
  - Display aware
- Memory Management:
  - Standard Genesis
  - Battery optimization
  - LCD buffering

### Performance Targets
- CPU: 7.67 MHz
- Graphics: 60 fps
- Audio: Full Genesis
- Memory Budget:
  - Main RAM: 64KB
  - Video RAM: 64KB
  - Sound RAM: 8KB
  - Cart ROM: Variable

### Special Handling
- LCD Display
- Battery Management
- Heat Control
- TV Output
- Sound Mixing

## References
- [Nomad Hardware Specification](http://segaretro.org/Nomad)
- [Genesis Technical Manual](http://www.sega-16.com/Genesis_Software_Manual.pdf)
- [68000 Programming Reference](http://www.nxp.com/docs/en/reference-manual/MC68000UM.pdf)
- [Z80 User Manual](http://www.zilog.com/docs/z80/um0080.pdf)
