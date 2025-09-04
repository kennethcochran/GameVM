# NEC PC Engine GT/TurboExpress

## System Overview
- CPU: Hudson HuC6280
- Clock Speed: 7.16 MHz (1.79 MHz power-save mode)
- Release Year: 1990
- Generation: 4th
- Region: Japan (PC Engine GT), North America (TurboExpress)
- Predecessor: None (NEC's first handheld)
- Successor: None
- Notable Feature: Perfect compatibility with PC Engine/TurboGrafx-16 home console games

## CPU Details
### Architecture Characteristics
- Processor: HuC6280 (modified 65C02)
- Word Size: 8-bit
- Endianness: Little-endian
- Register Set:
  - Accumulator (A): 8-bit
  - Index X (X): 8-bit
  - Index Y (Y): 8-bit
  - Stack Pointer (S): 8-bit
  - Program Counter (PC): 16-bit
  - Status Register (P):
    - N (Negative)
    - V (Overflow)
    - T (Memory Transfer)
    - B (Break)
    - D (Decimal)
    - I (Interrupt)
    - Z (Zero)
    - C (Carry)
- Special Features:
  - Memory bank switching
  - Block transfer instructions
  - Hardware multiply/divide
  - Timer system
  - Power management modes

### Memory Access
- Address Bus Width: 21 bits
- Data Bus Width: 8 bits
- Memory Page Size: 8 KB
- Memory Access Timing:
  - Basic Memory Cycle: 140ns
  - Block Transfers: Hardware accelerated
  - Bank Switching: Instant
- Special Features:
  - Memory mapping unit
  - DMA controller
  - Bank switching hardware
  - Dedicated VRAM port

### Performance Considerations
- Instruction Timing: 2-8 cycles
- Clock Speeds:
  - Normal: 7.16 MHz
  - Power Save: 1.79 MHz
- Known Bottlenecks:
  - Battery life
  - Memory bank switching overhead
  - VRAM access timing
- Optimization Opportunities:
  - Block transfer instructions
  - Hardware math operations
  - Bank switching optimization

## Memory Map
### System Memory
- Main RAM: 8 KB
- Video RAM: 64 KB
- Work RAM: 2 KB
- ROM: System Card dependent
- HuCard: Up to 2.5 MB

### Memory Layout
- $0000-$1FFF: System RAM
- $2000-$3FFF: Battery-backed RAM (optional)
- $0000-$1FFF: Memory-mapped hardware
- $2000-$7FFF: Bank-switched ROM
- $8000-$FFFF: Fixed ROM bank
- Hardware Registers:
  - VDC: $0000-$0003
  - VCE: $0400-$0407
  - PSG: $0800-$087F
  - Timer: $0C00-$0C03
  - I/O: $1000-$1003

## Video System
### Display Hardware
- Screen: 2.6-inch Active-Matrix LCD
- Resolution: 400×270 (scaled to LCD)
- Active Display: 336×224
- Colors: 512 (9-bit)
- Colors on Screen: 481 maximum
- Refresh Rate: 60 Hz

### Graphics Capabilities
- Sprites:
  - 64 hardware sprites
  - Size: 16×16 or 32×32 pixels
  - 16 colors per sprite
  - Up to 16 sprites per scanline
  - Hardware flipping
- Background:
  - Multiple background layers
  - 8×8 pixel tiles
  - 16 colors per tile
  - Hardware scrolling
  - Split screen effects

### Special Features
- Hardware Scaling
- Multiple Background Layers
- Sprite Priority System
- Line Interrupt System
- Color Palette Manipulation

### Display Modes
- Text Mode:
  - 40×25 characters
  - 256 characters
  - 16 colors
- Graphics Mode:
  - Resolution up to 336×224
  - 482 colors on screen
  - Multiple background layers
  - Hardware sprites

## Audio System
### Sound Hardware
- PSG Channels: 6
  - 5 Wavetable channels
  - 1 Noise channel
- Additional Features:
  - LFO
  - Volume control
  - Stereo output
  - DDA mode

### Channel Details
#### Wavetable Channels (5)
- 32-sample waveforms
- Frequency control
- Volume control
- Stereo panning
- DDA playback mode

#### Noise Channel
- White noise generator
- Frequency control
- Volume control
- Stereo panning

### Audio Control
- Master Volume
- Channel Enable/Disable
- Stereo Balance
- LFO Control
- DDA Mode Control

## Input/Output System
### Controller Interface
- D-Pad (8 directions)
- Run Button
- Select Button
- I Button
- II Button
- Built-in controls
- Multi-tap support (via adapter)

### Communication
- Multi-tap Port:
  - Up to 5 players
  - Link cable support
  - TV tuner support
- Headphone Jack:
  - Stereo output
  - Volume control

### Storage Interface
- HuCard Slot:
  - ROM: Up to 2.5 MB
  - Compatible with home console cards
  - No save capability (without System Card)
  - Hot-swappable

### Power System
- Battery: 6 AA batteries
- Battery Life: 3-4 hours
- External Power: 9V DC
- Power LED
- Low battery indicator

## System Integration Features
### Hardware Design
- Integrated Unit:
  - Active-matrix LCD
  - Built-in controls
  - Stereo speakers
  - HuCard slot
  - Multi-tap port
- TV Tuner Support:
  - Optional TV tuner
  - NTSC/PAL support
  - External antenna

### Special Features
- PC Engine Compatibility
- TV Output Support
- Active-Matrix Display
- Stereo Sound
- Multi-player Support

### Power Management
- Power LED
- Low Battery Warning
- External Power Support
- Power Save Mode
- Screen Brightness Control

## Technical Legacy
### Hardware Innovations
- First handheld with home console compatibility
- Active-matrix LCD screen
- High color count
- Advanced sound capabilities

### Software Development
- Development Environment:
  - HuC6280 assembly
  - C development possible
  - Shared with home console
- Programming Features:
  - Sprite handling
  - Background control
  - Sound generation
  - Memory management

### Market Impact
- Production Run: 1990-1994
- Units Sold: ~1.5 million
- Games Available: Full PC Engine library (~600)
- Price Points:
  - Launch: ¥44,800/US$249.99
  - Final: US$199.99
- Competition:
  - Game Boy
  - Game Gear
  - Atari Lynx

## GameVM Implementation Notes
### Compiler Considerations
- Preferred Code Generation Strategy:
  - 6502-based native code
  - Bank switching awareness
  - Power management
- Register Allocation Strategy:
  - Zero page optimization
  - Index register usage
  - Stack management
- Memory Management:
  - Bank switching control
  - VRAM access timing
  - DMA optimization

### Performance Targets
- Frame Rate: 60 Hz
- Audio Update: System dependent
- Battery Life: Power management
- Memory Budget:
  - RAM: 8 KB
  - VRAM: 64 KB
  - ROM: Up to 2.5 MB

### Special Handling
- Power Management
- Bank Switching
- VRAM Access
- Audio Generation
- Display Scaling

## References
- [PC Engine Programming Guide](http://www.archaicpixels.com/PCE_Programming_Guide)
- [HuC6280 Documentation](http://www.archaicpixels.com/HuC6280)
- [VDC Documentation](http://www.archaicpixels.com/PC_Engine_VDC)
- [PSG Documentation](http://www.archaicpixels.com/PC_Engine_PSG)
