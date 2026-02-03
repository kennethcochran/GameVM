# Sega Game Gear

## System Overview
- Profile: GV.Spec.L3
- CPU: Zilog Z80A
- Clock Speed: 3.58 MHz
- Release Year: 1990 (JP), 1991 (NA/EU)
- Generation: 4th
- Region: Worldwide
- Predecessor: None (Sega's first handheld)
- Successor: None
- Notable Feature: Full color backlit screen

## CPU Details
### Architecture Characteristics
- Instruction Set: Z80A
- Word Size: 8-bit
- Endianness: Little-endian
- Register Set:
  - Main: A (accumulator), B, C, D, E, H, L
  - Alternate: A', B', C', D', E', H', L'
  - Index Registers: IX, IY
  - Special Purpose:
    - SP (Stack Pointer)
    - PC (Program Counter)
    - I (Interrupt Vector)
    - R (Memory Refresh)
  - Flags: S, Z, H, P/V, N, C
- Special Features:
  - Block instructions
  - Bit manipulation
  - Two register banks
  - Index register operations

### Memory Access
- Address Bus Width: 16 bits
- Data Bus Width: 8 bits
- Memory Page Size: 16 KB
- Memory Access Timing:
  - Basic Memory Cycle: 4 T-states
  - I/O Operations: 4 T-states
  - Instruction Fetch: 4-6 T-states
- DMA: VDP capable of memory-to-VRAM transfers
- Mapper Support: Bank switching for ROM/RAM

### Performance Considerations
- Instruction Timing: 4-23 T-states
- Interrupt Modes: IM 0, IM 1, IM 2
- Known Bottlenecks:
  - VDP access timing
  - Limited RAM
  - Memory bank switching overhead
- Optimization Opportunities:
  - Block instructions
  - Alternate register set
  - Efficient bank switching

## Memory Map
### System Memory
- Main RAM: 8 KB
- Video RAM: 16 KB
- ROM: Up to 1 MB with banking
- Display RAM: Managed by VDP

### Memory Layout
- $0000-$03FF: ROM (fixed)
- $0400-$3FFF: ROM (switchable)
- $4000-$7FFF: ROM (switchable)
- $8000-$9FFF: ROM (switchable)
- $C000-$DFFF: System RAM
- $E000-$FFFF: System RAM (mirror)

## Video System
### Display Hardware
- Screen: 3.2-inch backlit LCD
- Resolution: 160×144 pixels
- Colors: 4096 (12-bit)
- Colors on Screen: 32 colors (2 palettes of 16)
- Refresh Rate: 59.922 Hz (NTSC)

### Video Display Processor
- Type: Modified Sega Master System VDP
- VRAM: 16 KB
- Sprite Attributes: 64 bytes
- Color RAM: 64 bytes
- Display Modes: Compatible with SMS modes

### Graphics Capabilities
- Background Layer:
  - 32×28 tiles
  - Scrollable
  - 16 colors per tile
  - Hardware scrolling
- Sprites:
  - 64 hardware sprites
  - 8×8 or 8×16 pixels
  - 16 colors per sprite
  - 8 sprites per scanline
- Display Modes:
  - Mode 4: 256×192 (cropped to 160×144)
  - Mode 0-3: SMS compatibility modes

### Special Features
- Hardware scrolling (X and Y)
- Sprite zooming
- Column-based scrolling
- Background priority
- Collision detection

## Audio System
### Sound Hardware
- Primary: Texas Instruments SN76489
- Channels: 4 total
  - 3× Square Wave:
    - Frequency: 100 Hz to 48 kHz
    - Volume: 16 levels
    - Duty cycle: Fixed 50%
  - 1× Noise:
    - White noise or periodic
    - 3 preset frequencies
    - Volume: 16 levels

### Audio Features
- Stereo Output
- Independent Channel Control
- Direct Frequency Control
- Volume Envelopes (software)
- Noise Generator Modes

## Input/Output System
### Controller Interface
- D-Pad (8 directions)
- Button 1 (primary)
- Button 2 (secondary)
- Start Button
- Built-in controls
- Pause button (on top)

### Communication
- Link Cable Port:
  - Multi-player support
  - Game sharing (some titles)
  - SMS converter support
- TV Tuner Port (region specific)

### Storage Interface
- Cartridge Slot:
  - ROM: Up to 1 MB
  - Save RAM: Optional
  - Mapper support
  - SMS compatibility
- External Power: 9V DC

### Power System
- Battery: 6 AA batteries
- Battery Life: 3-5 hours
- External Power: 9V DC 300mA
- Power LED indicator
- Low battery indicator

## System Integration Features
### Hardware Design
- Integrated Unit:
  - Backlit color LCD
  - Built-in controls
  - Stereo speakers
  - Cartridge slot
  - Link port
- Regional Variants:
  - NTSC/PAL timing
  - TV Tuner support
  - Power supply

### Special Features
- Master System Compatibility
- TV Tuner Support
- Built-in Stereo Sound
- Backlit Display
- Link Cable Support

### Power Management
- Power LED
- Low Battery LED
- External Power Support
- Screen Brightness Control

## Technical Legacy
### Hardware Innovations
- First backlit color handheld
- SMS compatibility
- Stereo sound
- TV tuner capability

### Software Development
- Development Environment:
  - Z80 assembly
  - C compilers available
  - SMS tools compatible
- Programming Features:
  - VDP routines
  - Sound generation
  - Sprite handling
  - Bank switching

### Market Impact
- Production Run: 1990-1997
- Units Sold: ~10.62 million
- Games Released: ~300
- Price Points:
  - Launch: ¥19,800/US$149.99
  - Final: US$99.99
- Competition:
  - Game Boy
  - Atari Lynx
  - NEC PC Engine GT

## GameVM Implementation Notes
### Compiler Considerations
- Preferred Code Generation Strategy:
  - Z80 native code
  - Bank-aware code
  - VDP timing awareness
- Register Allocation Strategy:
  - Maximize register usage
  - Efficient bank switching
  - VDP access optimization
- Memory Management:
  - Bank switching coordination
  - VDP memory management
  - RAM optimization

### Performance Targets
- Frame Rate: 59.922 Hz
- Audio Update: 60 Hz
- VDP Access: During VBLANK
- Memory Budget:
  - RAM: 8 KB
  - VRAM: 16 KB
  - ROM: Up to 1 MB

### Special Handling
- VDP Access Timing
- Bank Switching
- Audio Mixing
- Power Management
- Screen Scaling

## References
- [Game Gear Hardware Manual](https://www.smspower.org/uploads/Development/GameGearDevelopmentManual.pdf)
- [Z80 CPU Manual](http://www.zilog.com/docs/z80/um0080.pdf)
- [VDP Documentation](http://www.smspower.org/Development/VDP)
- [SN76489 Documentation](http://www.smspower.org/Development/SN76489)
