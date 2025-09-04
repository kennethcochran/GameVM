# NEC PC Engine/TurboGrafx-16

## System Overview
- CPU: Hudson HuC6280 (modified 65C02)
- Clock Speed: 7.16 MHz (1.79 MHz power-save mode)
- Release Year: 1987 (JP), 1989 (NA)
- Generation: 4th
- Region: Japan (PC Engine), North America (TurboGrafx-16)
- Predecessor: None (NEC's first console)
- Successor: PC-FX
- Notable Feature: First console with CD-ROM add-on

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
  - DMA controller

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
  - Single accumulator
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
- System Card RAM: 0-192 KB (add-on dependent)
- ROM: HuCard/CD-ROM based

### Memory Layout
- $0000-$1FFF: System RAM (8 KB)
- $2000-$3FFF: Bank switching
- $4000-$5FFF: Bank switching
- $6000-$7FFF: Bank switching
- $8000-$FFFF: Program ROM
- Memory-Mapped Hardware:
  - VDC: $0000-$0003
  - VCE: $0400-$0407
  - PSG: $0800-$0807
  - Timer: $0C00-$0C03
  - I/O: $1000-$1003

## Video System
### Video Display Processor
- Custom Hudson HuC6270 VDC
- Resolution: 256×239 to 512×242
- Colors: 512 (9-bit)
- Colors on Screen: 482 maximum
- Sprites: 64 hardware sprites
- Background Layers: 1 main + 1 optional

### Graphics Capabilities
- Sprites:
  - 64 hardware sprites
  - Size: 16×16 or 32×32 pixels
  - 16 colors per sprite
  - Up to 16 sprites per scanline
  - Hardware flipping
- Background:
  - 8×8 pixel tiles
  - 16 colors per tile
  - Hardware scrolling
  - Split screen effects

### Special Features
- Hardware Scrolling:
  - Horizontal/vertical
  - Per-scanline
  - Split screen
- Sprite Control:
  - Priority system
  - Size doubling
  - Collision detection
- Display Modes:
  - Text mode
  - Graphics mode
  - Mixed mode

### Display Memory
- VRAM: 64 KB
- SATB (Sprite Attribute Table Buffer)
- Pattern Generator Tables
- Name Tables
- Color Tables

## Audio System
### Programmable Sound Generator
- Type: Built-in 6-channel PSG
- Channels: 6 total
  - 5 Wavetable channels
  - 1 Noise channel
- Features:
  - 32-sample wavetables
  - Volume control
  - Frequency control
  - Channel balance
  - LFO

### CD-ROM Audio (with CD-ROM²)
- 16-bit stereo CD-DA
- ADPCM playback
- Hardware CD audio mixing
- Additional sound RAM

### Audio Control
- Volume: 16 levels per channel
- Pan: Left/right balance
- Waveform: 32 4-bit samples
- Noise: Configurable frequency

## Input/Output System
### Controller Interface
- One built-in port (expandable)
- Controller Features:
  - D-Pad
  - Run Button
  - Select Button
  - Two action buttons (base)
  - Six action buttons (6-button pad)
- Multi-tap Support:
  - Up to 5 players
  - TurboTap accessory required

### Storage Interface
- HuCard Slot:
  - ROM Size: Up to 2.5 MB
  - No battery backup (standard)
  - System Card support
- CD-ROM Interface (add-on):
  - CD-ROM²/Super CD-ROM²
  - 150 KB/s transfer rate
  - 64 KB buffer RAM
  - Additional sound capabilities

### Video Output
- RF Output: Standard
- Composite Video
- RGB (with adapter)
- Resolution Modes:
  - 256×239
  - 336×239
  - 512×242
  - Interlaced modes

## System Integration Features
### Add-on Systems
#### CD-ROM²
- CD-ROM drive
- Additional RAM
- ADPCM sound
- System Cards required

#### Super CD-ROM²
- Faster CD drive
- More RAM (256 KB)
- Improved audio
- Arcade Card support

#### Arcade Card
- 2MB additional RAM
- Enhanced DMA
- Data streaming
- Memory management

### Regional Differences
- PC Engine (Japan):
  - Compact design
  - RGB output
  - HuCard format
- TurboGrafx-16 (NA):
  - Larger case
  - Different card format
  - Region lockout

### System Cards
- System Card 1.0:
  - Basic CD-ROM support
- System Card 2.0:
  - Improved CD functions
- System Card 3.0:
  - Super CD-ROM² support
- Arcade Card:
  - Enhanced memory/DMA

## Technical Legacy
### Hardware Innovations
- First CD-ROM console add-on
- Compact design
- Expandable architecture
- Advanced sprite system

### Software Development
- Development Tools:
  - HuC assembler
  - MagicKit
  - CD-ROM tools
- Programming Features:
  - Sprite handling
  - CD streaming
  - Audio mixing
  - Memory management

### Market Impact
- Production Run: 1987-1994
- Units Sold: ~10 million
- Games Released: ~600 HuCards, ~400 CD-ROMs
- Price Points:
  - Launch: ¥24,800/US$199
  - CD-ROM²: ¥59,800/US$399

## GameVM Implementation Notes
### Compiler Considerations
- Preferred Code Generation Strategy:
  - 65C02-based native code
  - Bank switching awareness
  - CD-ROM optimization
- Register Allocation Strategy:
  - Zero page usage
  - Block transfer optimization
  - Stack management
- Memory Management:
  - Bank switching control
  - CD buffer management
  - DMA optimization

### Performance Targets
- Frame Rate: 60 Hz
- CD Access: 150 KB/s
- Audio Rate: 8-44.1 KHz
- Memory Budget:
  - RAM: 8 KB base
  - VRAM: 64 KB
  - CD Buffer: 64 KB
  - System Card: Up to 192 KB

### Special Handling
- CD-ROM Access
- Bank Switching
- Audio Mixing
- Sprite Management
- Add-on Detection

## References
- [PC Engine Development Wiki](http://www.pcengine.co.uk/wiki/)
- [HuC6280 Documentation](http://www.archaicpixels.com/HuC6280)
- [VDC Documentation](http://www.archaicpixels.com/PC_Engine_VDC)
- [CD-ROM² System Documentation](http://www.pcengine.co.uk/documents/)
