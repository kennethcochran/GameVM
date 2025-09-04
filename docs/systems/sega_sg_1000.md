# Sega SG-1000

## System Overview
- CPU: NEC 780C (Z80A clone)
- Clock Speed: 3.58 MHz
- Release Year: 1983
- Generation: 3rd (Early)
- Region: Japan, Australia, New Zealand, some European markets
- Predecessor: None (Sega's first home console)
- Successor: Sega Mark III/Master System

## CPU Details
### Architecture Characteristics
- Instruction Set: Z80A compatible
- Word Size: 8-bit
- Endianness: Little-endian
- Register Set:
  - Main: A (accumulator), B, C, D, E, H, L
  - Alternate: A', B', C', D', E', H', L'
  - Special Purpose: IX, IY (index), SP (stack pointer), PC (program counter)
  - Flags: S (sign), Z (zero), H (half carry), P/V (parity/overflow), N (add/subtract), C (carry)
- Special Features:
  - Block instructions
  - Bit manipulation instructions
  - Index register instructions

### Memory Access
- Address Bus Width: 16 bits
- Data Bus Width: 8 bits
- Memory Page Size: 16 KB
- No Memory Management Unit
- No bank switching support
- Fixed memory map
- Memory Access Timing:
  - Basic instruction: 4 T-states
  - Memory access: 3 T-states
  - I/O access: 4 T-states

### Performance Considerations
- Instruction Timing: 4-23 T-states per instruction
- Interrupt Modes: IM 0, IM 1, IM 2 supported
- Known Bottlenecks:
  - Limited RAM
  - No bank switching
  - VDP access timing
- Optimization Opportunities:
  - Block instructions for memory transfers
  - Efficient register usage
  - Interrupt mode selection

## Memory Map
### System Memory
- Main RAM: 1 KB
- Video RAM: 16 KB
- Game ROM: Up to 48 KB
- No built-in BIOS

### Memory Layout
- $0000-$BFFF: Cartridge ROM
- $C000-$C3FF: System RAM
- $8000-$BFFF: Additional ROM space
- Port $BE: VDP data port
- Port $BF: VDP control port
- Port $7E: PSG register select
- Port $7F: PSG data write

## Video System
### TMS9928A VDP
- Chip: Texas Instruments TMS9928A
- Clock Speed: 10.738635 MHz
- VRAM: 16 KB dedicated
- Display Resolution: 256×192 pixels
- Refresh Rate: 60 Hz (NTSC)

### Graphics Capabilities
- Color Palette:
  - Total Colors: 16
  - Colors on Screen: 16
  - Color Definition: RGB111 (1-bit per channel)
- Pattern Generator:
  - Pattern Size: 8×8 pixels
  - Pattern Table: 256 patterns
  - Pattern Colors: 2 colors per pattern
- Sprite System:
  - Total Sprites: 32
  - Sprite Size: 8×8 or 16×16 pixels
  - Colors: 1 color per sprite (plus transparent)
  - Sprites per Scanline: 4
  - Early Clock Bit: Yes

### Display Modes
- Graphics I Mode:
  - Resolution: 256×192
  - Character-based
  - 32×24 characters
  - 2 colors per character
- Graphics II Mode:
  - Resolution: 256×192
  - Character-based with color table
  - 32×24 characters
  - 8 colors per line of character
- Multicolor Mode:
  - Resolution: 64×48
  - 4×4 pixel blocks
  - 16 colors
- Text Mode:
  - Resolution: 40×24 characters
  - Single color
  - 6×8 character matrix

### Special Features
- Background Collision Detection
- Sprite Collision Detection
- External Video Input
- Interrupt Generation
- Status Register Reading

## Audio System
### SN76489 PSG
- Chip: Texas Instruments SN76489
- Clock Speed: 3.579545 MHz
- Channels: 4 total
  - Tone Generators: 3
    - Frequency Range: 110 Hz to 55.9 kHz
    - 10-bit frequency control
    - 4-bit volume control
    - Square wave output
  - Noise Generator: 1
    - White noise or periodic noise
    - 3 preset frequencies
    - 4-bit volume control

### Audio Features
- Output: Mono
- Volume Levels: 16 per channel
- Mixing: Hardware mixing of all channels
- Control: Direct port access
- Sample Rate: ~32 kHz effective
- Output Resolution: 4-bit

## Input/Output System
### Controller Interface
- Ports: Two DE-9 connectors
- Original Model:
  - Hardwired controllers
  - Non-detachable design
  - 8-way directional pad
  - 2 action buttons
- SG-1000 II Model:
  - Detachable controllers
  - Compatible with SC-3000 keyboards
  - Standard Sega gamepad support
- Controller Features:
  - Digital inputs only
  - No auto-fire
  - No special accessories

### Storage Interface
- Cartridge System:
  - ROM Size: Up to 48 KB
  - No bank switching
  - Access Time: Immediate
  - Edge connector design
  - No save capability
- Card Slot: None
- Expansion Port: None

### Video Output
- RF Output: Standard
- Video Format: NTSC-J
- Resolution: 256×192
- Refresh Rate: 60 Hz
- No direct video output

### Power System
- Input Voltage: 9V DC
- Power Supply: External AC adapter
- Power Consumption: ~5W
- Power LED indicator

## System Integration
### Hardware Architecture
- Common Platform Elements:
  - Similar to ColecoVision
  - MSX computer compatibility
  - SC-3000 computer shared components
- System Design:
  - Compact form factor
  - Cartridge loading mechanism
  - Ventilation system
  - RF shielding

### Regional Characteristics
- No Region Lock
- TV Standards:
  - Primary: NTSC-J
  - Limited PAL support
- Market Presence:
  - Primary: Japan
  - Limited: Australia, New Zealand, Europe

### System Reliability
- Known Issues:
  - Controller durability
  - RF interference
  - Cartridge contact wear
  - Power supply stability
- Design Improvements in SG-1000 II:
  - Detachable controllers
  - Better RF shielding
  - Improved cartridge slot
  - Enhanced reliability

## Historical Significance
### Market Impact
- Release Timing:
  - Launch: July 15, 1983
  - End of Life: 1985
  - Market Duration: ~2.5 years
- Competition:
  - Direct: Nintendo Famicom
  - Indirect: MSX computers
- Sales Performance:
  - Total Units: ~160,000
  - Market Share: Limited

### Technical Legacy
- Development Experience:
  - Hardware design learning
  - Manufacturing processes
  - Market research data
- Influence on Future Systems:
  - Master System architecture
  - Controller design evolution
  - Game development practices

### Software Library
- Official Releases: ~75 titles
- Genre Distribution:
  - Action: 40%
  - Arcade Ports: 30%
  - Sports: 15%
  - Other: 15%
- Development Support:
  - First-party titles
  - Third-party partnerships
  - Cross-platform ports
