# Nintendo Game Boy Color

## System Overview
- Profile: GV.Spec.L3
- Profile: GV.Spec.L3
- CPU: Sharp LR35902 (modified Z80)
- CPU Clock: 4.19/8.38 MHz (normal/double speed)
- Release Year: 1998
- Generation: 4th/5th (transitional)
- Region: Worldwide
- Predecessor: Game Boy
- Successor: Game Boy Advance
- Notable Feature: Full backward compatibility with Game Boy while adding color capabilities

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
  - DMA controller
  - Timer system

### Speed Modes
- Normal Speed: 4.19 MHz
- Double Speed: 8.38 MHz
- Dynamic switching
- Compatibility mode

## Memory Map
### System Memory
- Work RAM: 32KB
- Video RAM: 16KB
- ROM: Up to 8MB (cartridge)
- OAM: 160 bytes
- High RAM: 128 bytes

### Memory Layout
- $0000-$3FFF: ROM Bank 0
- $4000-$7FFF: ROM Bank n
- $8000-$9FFF: VRAM
- $A000-$BFFF: External RAM
- $C000-$DFFF: Work RAM
- $FE00-$FE9F: OAM
- $FF00-$FF7F: I/O Registers
- $FF80-$FFFE: High RAM
- $FFFF: Interrupt Enable

## Video System
### Graphics Processing
- Resolution: 160×144
- Colors: 32,768 (15-bit)
- Sprites: 40
- Display modes: 
  - Original GB
  - GBC enhanced

### Display Features
- Color Palettes:
  - 8 background palettes
  - 8 sprite palettes
  - 4 colors per palette
- Sprite Features:
  - 8×8 or 8×16 pixels
  - Priority control
  - Horizontal/vertical flip
  - Color palette selection

### Special Effects
- Color palette manipulation
- DMA transfers
- Window layer
- Background priorities
- Auto-colorization of GB games

## Audio System
### Sound Controller
- Channels: 4
  - 2 square wave
  - 1 programmable wave
  - 1 noise
- Features:
  - Volume envelope
  - Frequency sweep
  - Pattern memory
  - Stereo panning

### Audio Capabilities
- Square wave synthesis
- Wave pattern playback
- White noise generation
- Volume control
- Stereo output
- Envelope control

## Input/Output System
### Controller Interface
- D-pad
- A, B buttons
- Start, Select buttons
- Infrared port
- Link cable port

### Storage Interface
- Game cartridges:
  - Up to 8MB ROM
  - Battery backup
  - Real-time clock
  - Rumble
- External:
  - Link cable
  - Infrared communication
  - Printer support

### Display Output
- Built-in LCD screen
- 160×144 pixels
- 32,768 colors
- Game Boy compatibility

## System Integration Features
### Hardware Variants
- Game Boy Color
- Game Boy Color CGB-101
- Development units

### Regional Differences
- Minimal regional lockout
- Universal cartridge format
- Power requirements
- Safety certifications

### Special Features
- Game Boy compatibility
- Auto-colorization
- Infrared port
- Double speed mode

## Technical Legacy
### Hardware Innovations
- Color display
- Backward compatibility
- Enhanced processing
- IR communication

### Software Development
- Development Tools:
  - RGBDS
  - No$gmb
  - BGB
  - SDK libraries
- Programming Features:
  - Color management
  - Speed switching
  - Memory banking
  - DMA control

### Market Impact
- Production Run: 1998-2003
- Units Sold: 118.69 million
- Games Released: ~540
- Price Points:
  - Launch: $69.99
  - Final: $49.99
- Market Position:
  - Enhanced Game Boy
  - Color gaming
  - Backward compatible
  - Market leader

## GameVM Implementation Notes
### Compiler Considerations
- Code Generation:
  - Z80-like instructions
  - Memory banking
  - DMA optimization
  - Color management
- Register Allocation:
  - Limited registers
  - Accumulator focus
  - Stack usage
- Memory Management:
  - ROM banking
  - RAM banking
  - VRAM access
  - DMA control

### Performance Targets
- CPU: 4.19/8.38 MHz
- Graphics: 60 fps
- Audio: 4 channels
- Memory Budget:
  - Work RAM: 32KB
  - Video RAM: 16KB
  - ROM: Up to 8MB
  - External RAM: 32KB

### Special Handling
- Speed Switching
- Color Palettes
- GB Compatibility
- DMA Transfers
- IR Communication

## References
- [Game Boy Color Programming Manual](http://gbdev.gg8.se/files/docs/GBCProgramming.pdf)
- [Pan Docs](http://gbdev.io/pandocs)
- [Game Boy CPU Manual](http://marc.rawer.de/Gameboy/Docs/GBCPUman.pdf)
- [Game Boy Development Wiki](http://gbdev.gg8.se/wiki)
