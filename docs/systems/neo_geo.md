# SNK Neo Geo AES/MVS

## System Overview
- CPU: Motorola 68000 (Main), Zilog Z80 (Sound)
- Main CPU Clock: 12 MHz
- Sound CPU Clock: 4 MHz
- Release Year: 1990 (MVS), 1991 (AES)
- Generation: 4th
- Region: Worldwide
- Predecessor: None (SNK's first console)
- Successor: Neo Geo CD
- Notable Feature: Arcade-perfect home gaming, identical hardware to MVS arcade system

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
  - Status Register (SR):
    - Trace Mode (T)
    - Supervisor State (S)
    - Interrupt Mask (I0-I2)
    - Extend (X)
    - Negative (N)
    - Zero (Z)
    - Overflow (V)
    - Carry (C)
- Special Features:
  - 32-bit internal architecture
  - 16 general-purpose registers
  - 14 addressing modes
  - Hardware multiply/divide
  - Supervisor/user modes

### Sound CPU (Z80)
#### Architecture Characteristics
- Processor: Zilog Z80A
- Clock Speed: 4 MHz
- Word Size: 8-bit
- Register Set:
  - Main/Alternate (A, B, C, D, E, H, L)
  - Index Registers (IX, IY)
  - Special Purpose (I, R, SP, PC)
  - Flags Register
- Purpose:
  - Sound chip control
  - Audio memory management
  - Sound driver execution

### Memory Access
#### Main System
- Address Bus Width: 24 bits
- Data Bus Width: 16 bits
- Memory Access Timing:
  - ROM: 140ns
  - Work RAM: 100ns
  - VRAM: 100ns
- DMA Features:
  - Sprite DMA
  - Fixed DMA
  - Auto-animation

#### Sound System
- Address Space: 64 KB
- Sound RAM: 2 KB
- ROM Access: Direct
- Sound Chip Access: Port-mapped

## Memory Map
### System Memory
- Work RAM: 64 KB
- Video RAM: 68 KB
- Sound RAM: 2 KB
- ROM: Up to 716 Mbit (cartridge)
- Backup RAM: 64 KB

### Memory Layout
#### 68000 Space
- $000000-$0FFFFF: System ROM
- $100000-$10FFFF: Work RAM
- $200000-$2FFFFF: VRAM
- $300000-$3FFFFF: Cartridge ROM
- $400000-$401FFF: Palette RAM
- $800000-$8FFFFF: Memory Card

#### Z80 Space
- $0000-$0FFF: Sound ROM
- $1000-$1FFF: Sound RAM
- $4000-$47FF: YM2610 Registers
- $8000-$BFFF: ROM Bank
- $C000-$DFFF: Work RAM
- $E000-$E3FF: Port Access
- $F000-$F7FF: ROM Bank

## Video System
### Graphics Processing
- Custom SNK graphics system
- Resolution: 320×224 pixels
- Colors: 65,536 (16-bit)
- Colors per Sprite: 16
- Colors on Screen: 4,096
- Refresh Rate: 60 Hz

### Sprite System
- Hardware Sprites:
  - Up to 380 sprites
  - Size: 16×16 to 512×512 pixels
  - Scaling and rotation
  - Hardware zoom (from 0% to 499%)
- Features:
  - Auto-animation
  - Chain effects
  - Priority control
  - Collision detection

### Background System
- Fixed Layer:
  - 40×32 tiles
  - 16 colors per tile
  - Hardware scrolling
- Additional Features:
  - Line scrolling
  - Multiple scroll speeds
  - Tile animation
  - Priority control

### Special Features
- Sprite Scaling:
  - Hardware zoom
  - Independent X/Y scaling
  - Real-time effects
- Auto-Animation:
  - Hardware sprite cycling
  - Multiple animation speeds
  - Programmable patterns
- Layering System:
  - Multiple priority levels
  - Sprite stacking
  - Background interaction

## Audio System
### YM2610 Sound Chip
- Channels: Multiple synthesis types
  - 4 FM channels
  - 3 SSG channels
  - 1 Noise channel
  - 7 ADPCM channels
- Features:
  - FM synthesis
  - PCM playback
  - Digital sound mixing
  - Multiple waveforms

### Audio Capabilities
- Sample Playback:
  - 16-bit PCM
  - ADPCM compression
  - Multiple rates
  - Stereo output
- FM Synthesis:
  - Multiple operators
  - Complex waveforms
  - Frequency modulation
  - Envelope control

### Sound Memory
- Sound RAM: 2 KB
- ROM Access: Banked
- ADPCM Storage: ROM-based
- Mixing: Hardware

## Input/Output System
### Controller Interface
- Two DE-15 ports
- Neo Geo Controller:
  - 8-way joystick
  - A, B, C, D buttons
  - Start button
  - Select button
- Memory Card Slot:
  - 68-pin connector
  - 2KB storage
  - Battery backup

### Storage Interface
- Game Cartridge:
  - ROM Size: Up to 716 Mbit
  - Custom connector
  - Security lockout
  - Multiple ROM boards
- Memory Card:
  - 2 KB storage
  - Save game support
  - Transferable between systems

### Video Output
- RGB Output (AES)
- Composite Video
- S-Video (some models)
- Resolution: 320×224
- Refresh: 60 Hz

## System Integration Features
### Hardware Variants
#### MVS (Multi Video System)
- Arcade cabinet system
- Multiple game slots
- Coin mechanisms
- Operator menus

#### AES (Advanced Entertainment System)
- Home console system
- Single cartridge slot
- Memory card support
- Consumer features

### Regional Differences
- Minimal regional lockout
- Universal cartridge format
- Power supply differences
- Cabinet/console variations

### Special Features
- Arcade-perfect conversion
- Memory card system
- Auto-fire capability
- System diagnostics

## Technical Legacy
### Hardware Innovations
- Arcade-identical home system
- Large ROM capacity
- Advanced sprite system
- Memory card saving

### Software Development
- Development Tools:
  - Official SNK kit
  - 68000 development
  - Sound tools
  - Graphics utilities
- Programming Features:
  - Sprite management
  - Sound driver development
  - Memory management
  - Auto-animation

### Market Impact
- Production Run: 1990-2004
- Units Sold: ~1 million (AES)
- Games Released: ~148
- Price Points:
  - Console: $649
  - Games: $200-300
- Market Position:
  - Premium system
  - Arcade-perfect games
  - Collector's market
  - Fighting game focus

## GameVM Implementation Notes
### Compiler Considerations
- Preferred Code Generation Strategy:
  - 68000 native code
  - Z80 sound code
  - DMA optimization
  - Sprite management
- Register Allocation Strategy:
  - Data/Address register balance
  - Sound CPU coordination
  - DMA awareness
- Memory Management:
  - ROM banking
  - Sprite system
  - Sound memory

### Performance Targets
- Frame Rate: 60 Hz
- Sprite Count: Up to 380
- Audio Quality: CD-quality
- Memory Budget:
  - Work RAM: 64 KB
  - VRAM: 68 KB
  - Sound RAM: 2 KB
  - ROM: Up to 716 Mbit

### Special Handling
- Dual CPU Coordination
- Sprite System Management
- Auto-Animation
- Memory Card Access
- Sound Processing

## References
- [Neo Geo Development Wiki](http://wiki.neogeodev.org)
- [YM2610 Documentation](http://www.ym2610.org/docs)
- [68000 Programming Manual](http://www.nxp.com/docs/en/reference-manual/MC68000UM.pdf)
- [Neo Geo Hardware Specifications](http://retrorgb.com/neogeospecifications.html)
