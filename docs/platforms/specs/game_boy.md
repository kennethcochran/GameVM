# Nintendo Game Boy

## System Overview
- Profile: GV.Spec.L3
- CPU: Sharp LR35902 (modified Z80)
- Clock Speed: 4.194304 MHz
- Release Year: 1989
- Generation: 4th
- Region: Worldwide
- Predecessor: Nintendo Game & Watch series
- Successor: Game Boy Color
- Notable Feature: First successful handheld with interchangeable cartridges

## CPU Details
### Architecture Characteristics
- Instruction Set: Modified Z80/8080 hybrid
- Word Size: 8-bit
- Endianness: Little-endian
- Register Set:
  - Accumulator (A): 8-bit
  - Flags (F): 8-bit
    - Z (Zero)
    - N (Subtract)
    - H (Half Carry)
    - C (Carry)
  - General Purpose: B, C, D, E, H, L
  - Special Purpose:
    - SP (Stack Pointer): 16-bit
    - PC (Program Counter): 16-bit
  - Register Pairs:
    - AF (Accumulator & Flags)
    - BC (General Purpose)
    - DE (General Purpose)
    - HL (General Purpose/Indirect Addressing)
- Special Features:
  - Combined Z80/8080 instruction set
  - Optimized for portable operation
  - Built-in timer system
  - Hardware sprite engine

### Memory Access
- Address Bus Width: 16 bits
- Data Bus Width: 8 bits
- Memory Page Size: 16 KB
- Memory Access Timing:
  - Basic Memory Cycle: 4 cycles
  - Instruction Fetch: 4 cycles
  - Memory Read: 4 cycles
  - Memory Write: 4 cycles
- DMA: OAM DMA for sprite data
- Special Access:
  - High RAM (HRAM) accessible during DMA
  - Memory banking through MBC controllers

### Performance Considerations
- Instruction Timing: 4-16 cycles per instruction
- Interrupt Types:
  - VBlank (LCD vertical blank)
  - LCD STAT (LCD status triggers)
  - Timer Overflow
  - Serial Transfer
  - Joypad Press
- Known Bottlenecks:
  - LCD memory access restrictions
  - DMA blocks CPU access
  - Limited RAM
- Optimization Opportunities:
  - HRAM usage during DMA
  - VBlank period utilization
  - Efficient register usage

## Memory Map
### System Memory
- Internal RAM: 8 KB
- Video RAM: 8 KB
- OAM (Sprite Attributes): 160 bytes
- High RAM: 128 bytes
- Boot ROM: 256 bytes
- Cartridge Space: Up to 32 KB (expandable via banking)

### Memory Layout
- $0000-$3FFF: ROM Bank 0
- $4000-$7FFF: ROM Bank 1-N
- $8000-$9FFF: Video RAM
- $A000-$BFFF: External RAM
- $C000-$DFFF: Work RAM
- $E000-$FDFF: Echo RAM
- $FE00-$FE9F: Sprite Attribute Table (OAM)
- $FEA0-$FEFF: Not Usable
- $FF00-$FF7F: I/O Ports
- $FF80-$FFFE: High RAM (HRAM)
- $FFFF: Interrupt Enable Register

## Video System
### LCD Display
- Type: STN LCD (Super-Twisted Nematic)
- Resolution: 160×144 pixels
- Colors: 4 shades of gray-green
- Refresh Rate: ~59.73 Hz
- Frame Duration: 16.74ms
- Scanline Duration: 456 dots (114 µs)

### Graphics Capabilities
- Background Layer:
  - 256×256 pixel scrollable area
  - 32×32 tiles
  - Tile size: 8×8 pixels
  - Two colors per pixel
  - Independent X/Y scrolling
- Window Layer:
  - Fixed 160×144 overlay
  - Independent from background
  - No scrolling capability
  - Same tile properties as background
- Sprites (OBJ):
  - 40 sprites total
  - 10 sprites per scanline
  - Size: 8×8 or 8×16 pixels
  - Three colors per sprite (one transparent)
  - X/Y flipping
  - Priority vs background

### Graphics Memory
- Character Data:
  - Two banks of 256 tiles each
  - 16 bytes per tile
  - 2 bits per pixel
- Background Map:
  - Two 32×32 tile maps
  - One for background
  - One for window
- OAM (Object Attribute Memory):
  - 40 sprite entries
  - 4 bytes per sprite:
    - Y position
    - X position
    - Tile number
    - Attributes

### Special Features
- Hardware scrolling
- Window overlay
- Mid-frame timing effects
- Sprite priority system
- LCD interrupt system:
  - VBlank
  - HBlank
  - LY=LYC
  - OAM Search

## Audio System
### Sound Hardware (PSG)
- Four Sound Channels:
  1. Square Wave with Sweep
  2. Square Wave
  3. Programmable Wave
  4. Noise with Envelope
- Master Volume Control
- Left/Right Pan Control
- Mixing capabilities

### Channel Details
#### Channel 1 (Square with Sweep)
- Frequency Range: 64 Hz to 131 kHz
- Duty Cycles: 12.5%, 25%, 50%, 75%
- Volume Envelope
- Frequency Sweep
- Length Counter

#### Channel 2 (Square)
- Frequency Range: 64 Hz to 131 kHz
- Duty Cycles: 12.5%, 25%, 50%, 75%
- Volume Envelope
- Length Counter

#### Channel 3 (Wave)
- 32 4-bit custom samples
- Frequency Range: 32 Hz to 65.5 kHz
- Volume Reduction: 0%, 25%, 50%, 100%
- Length Counter

#### Channel 4 (Noise)
- Pseudo-random Generator
- Volume Envelope
- Length Counter
- Variable Frequency
- Counter Step Width Control

### Audio Control
- Stereo Output
- Channel Enable/Disable
- Master Volume
- Vin Mixing
- Status Flags

## Input/Output System
### Controller Interface
- D-Pad (4 directions)
- A Button
- B Button
- Start Button
- Select Button
- Input multiplexing system
- Interrupt on button press

### Communication
- Serial Port:
  - Link Cable Support
  - 8192 Hz transfer rate
  - Synchronous operation
  - Master/Slave configuration
- External Expansion:
  - Game Boy Printer
  - Four-player adapter
  - Super Game Boy interface

### Cartridge Interface
- ROM: Up to 32 KB direct, more with banking
- RAM: Up to 32 KB (battery-backed)
- Memory Bank Controllers:
  - MBC1: 2MB ROM/32KB RAM
  - MBC2: 256KB ROM/512×4 bits RAM
  - MBC3: 2MB ROM/32KB RAM + RTC
  - MBC5: 8MB ROM/128KB RAM
- Memory Management Unit
- Real-Time Clock (MBC3)

### Power System
- Power Source: 4 AA batteries
- Operating Voltage: 6V DC (4×1.5V)
- Power LED indicator
- Low battery detection
- Power consumption:
  - CPU: ~0.7mA
  - LCD: ~8.0mA
  - Sound: ~2.0mA per channel

## System Integration Features
### Power Management
- CPU Speed Control
- LCD Enable/Disable
- Sound Enable/Disable
- Serial Port Control
- Selective Interrupt Usage

### Hardware Variants
- Original DMG-01 (1989)
- Game Boy Pocket (1996)
- Game Boy Light (1998, Japan only)
- Play It Loud! Series (1995)

### Regional Differences
- Minimal regional variations
- Universal cartridge format
- Power supply differences
- Language ROMs

## GameVM Implementation Notes
### Compiler Considerations
- Preferred Code Generation Strategy:
  - Z80-like instruction set
  - Efficient register usage
  - HRAM optimization
  - DMA-aware code
- Register Allocation Strategy:
  - Maximize HL usage
  - Efficient 16-bit operations
  - Stack optimization
- Memory Management:
  - VRAM access timing
  - Bank switching overhead
  - DMA coordination

### Performance Targets
- Frame Rate: 59.73 Hz
- Audio Update: 256 Hz
- DMA Transfer: 160 microseconds
- Memory Budget:
  - RAM: 8 KB
  - VRAM: 8 KB
  - OAM: 160 bytes
  - HRAM: 128 bytes

### Special Handling
- LCD Timing Management
- Audio Channel Synchronization
- DMA Coordination
- Bank Switching
- Power Management

## References
- [Pan Docs](https://gbdev.io/pandocs/)
- [Game Boy CPU Manual](http://marc.rawer.de/Gameboy/Docs/GBCPUman.pdf)
- [Game Boy Programming Manual](https://archive.org/details/GameBoyProgManVer1.1)
- [The Cycle-Accurate Game Boy Docs](https://github.com/AntonioND/giibiiadvance/blob/master/docs/TCAGBD.pdf)
