# ColecoVision

## System Overview
- CPU: Zilog Z80A
- Clock Speed: 3.58 MHz
- Release Year: 1982
- Generation: 2nd
- Region: Worldwide
- Predecessor: Coleco Telstar series
- Successor: None (planned ADAM computer integration)

## CPU Details
### Architecture Characteristics
- Instruction Set: Z80A (enhanced 8080 instruction set)
- Word Size: 8-bit
- Endianness: Little-endian
- Register Set:
  - Main: A (accumulator), B, C, D, E, H, L
  - Alternate: A', B', C', D', E', H', L'
  - Index Registers: IX, IY
  - Special Purpose: 
    - SP (stack pointer)
    - PC (program counter)
    - I (interrupt vector)
    - R (refresh)
  - Flags: S (sign), Z (zero), H (half carry), P/V (parity/overflow), N (add/subtract), C (carry)
- Special Features:
  - Block instructions
  - Index register operations
  - Extended instruction set
  - Two interrupt modes

### Memory Access
- Address Bus Width: 16 bits
- Data Bus Width: 8 bits
- Memory Page Size: 16 KB
- Memory Access Timing:
  - Basic Memory Cycle: 4 T-states
  - I/O Operations: 4 T-states
  - Instruction Fetch: 4-6 T-states
- DMA: Not available

### Performance Considerations
- Instruction Timing: 4-23 T-states
- Interrupt Modes: IM 0, IM 1, IM 2
- Known Bottlenecks:
  - VDP access timing
  - Limited RAM
  - Shared memory bus
- Optimization Opportunities:
  - Block instructions for memory transfers
  - Alternate register set usage
  - Interrupt mode selection

## Memory Map
### System Memory
- RAM: 1 KB work RAM
- Video RAM: 16 KB
- ROM: 8 KB BIOS
- Cartridge ROM: Up to 32 KB
- Expansion RAM: Up to 32 KB (with expansion module)

### Memory Layout
- $0000-$1FFF: BIOS ROM (8 KB)
- $2000-$3FFF: Expansion ROM
- $4000-$5FFF: Expansion RAM
- $6000-$7FFF: Work RAM (1 KB, mirrored)
- $8000-$FFFF: Cartridge ROM
- Ports:
  - $BE: VDP data read
  - $BF: VDP data write
  - $40-$7F: Controller 1
  - $80-$BF: Controller 2

## Video System
### Video Display Processor
- Chip: Texas Instruments TMS9928A
- Clock Speed: 10.738635 MHz
- VRAM: 16 KB dedicated
- Display Resolution: 256×192 pixels
- Refresh Rate: 60 Hz (NTSC)

### Graphics Capabilities
- Color Palette:
  - Total Colors: 16
  - Colors on Screen: 15 + transparent
  - Color Definition: RGB
- Sprites:
  - Total Sprites: 32
  - Size: 8×8 or 16×16 pixels
  - Colors: 1 color per sprite
  - Sprites per Scanline: 4 (hardware limit)
  - Early Clock: Allows 5th sprite
- Background:
  - Pattern Table: 256 patterns
  - Color Table: 32 pattern colors
  - Name Table: 768 bytes
  - 32×24 character display

### Display Modes
- Graphics I Mode:
  - Text: 40×24 characters
  - Character size: 6×8 pixels
  - Limited color options
- Graphics II Mode:
  - Resolution: 256×192
  - 32×24 tiles
  - Independent colors per 8×1 pixel row
- Multicolor Mode:
  - Resolution: 64×48
  - 4×4 pixel blocks
  - 15 colors + transparent
- Sprite Mode:
  - Magnification: 1× or 2×
  - Size selection: 8×8 or 16×16

### Special Features
- Hardware Sprite Management
- Sprite Collision Detection
- Sprite Coincidence Detection
- Interrupt Generation
- Status Register Reading
- Direct Memory Access

## Audio System
## Input/Output System
### Controller Interface
- Number of Ports: 2
- Controller Features:
  - 8-way digital joystick
  - Fire buttons (2)
  - Numeric keypad (12 buttons)
  - Expansion port
  - Overlays for game-specific functions
- Special Controllers:
  - Roller Controller (trackball)
  - Super Action Controllers
  - Driving Controller
  - Expansion Module #2 (steering wheel)

### Storage Interface
- Cartridge Slot:
  - ROM Size: Up to 32 KB
  - Edge connector
  - Gold-plated contacts
  - Region encoding
- Expansion Module Interface:
  - Module #1: Atari 2600 adapter
  - Module #2: Driving controller
  - Module #3: ADAM computer expansion

### Video Output
- RF Output: Standard
- Video Format: NTSC (60 Hz)
- Resolution: 256×192 pixels
- Colors: 16 total

### Expansion Capabilities
- ADAM Computer Expansion
- Memory Expansion
- Controller Expansions
- Game-Specific Peripherals

## System Integration Features
### Hardware Variants
- Original ColecoVision (1982)
- Telegames Personal Arcade
- Bentley Computer Systems Model 2000
- CBS ColecoVision

### Regional Differences
- NTSC (North America)
  - 60 Hz refresh
  - 120V power supply
- PAL (Europe)
  - 50 Hz refresh
  - 220V power supply
- Region coding in cartridges

### Special Features
- Built-in Donkey Kong
- Expansion module system
- ADAM computer compatibility
- High-quality arcade ports

## Technical Legacy
### Hardware Innovations
- Arcade-quality graphics
- Expandable system design
- Advanced controller design
- Multiple display modes

### Software Development
- Development Tools:
  - Assembly development
  - Sprite editor tools
  - Pattern editor tools
  - Sound development tools
- Programming Features:
  - BIOS routines
  - VDP interrupt handling
  - Sprite management
  - Controller scanning

### Market Impact
- Production Run: 1982-1985
- Units Sold: ~2 million
- Games Released: ~145 officially
- Price Points:
  - Launch: $199
  - Final: $99
- Competition:
  - Atari 2600
  - Intellivision
  - Vectrex

### Programming Resources
- BIOS Support:
  - Controller reading
  - Sound generation
  - VDP management
  - Interrupt handling
- Development Features:
  - Sprite system
  - Pattern tables
  - DMA transfers
  - Interrupt system







## Audio System (SN76489A)
### Audio Hardware
- Sound Channels: 3 + noise
- Sample Rate: N/A (tone generation)
- Bit Depth: 4-bit volume per channel
- Audio Memory: Direct register control

### Channel Types
- Three tone channels:
  - 10-bit frequency control
  - 4-bit volume control
  - Square wave output
- One noise channel:
  - White/periodic noise
  - 4-bit volume control
  - Frequency options

### Timing
- Audio Update Rate: Any time
- DMA Features: None
- Interrupt Sources: None

## System Timing
### Interrupts
- Types Available:
  - VDP interrupt
  - NMI possible
- Sources:
  - Video system
  - External input
- Timing: 60 Hz video sync
- Priority: Two levels (INT, NMI)

### DMA
- Transfer Rates: N/A
- Available Modes: N/A
- Timing Constraints: N/A

## GameVM Implementation Notes
### Compiler Considerations
- Preferred Code Generation Strategy:
  - Z80 native code
  - VDP-aware timing
  - Efficient register usage
- Register Allocation Strategy:
  - Use alternate register set
  - Index registers for arrays
  - Optimize accumulator usage
- Memory Management Strategy:
  - Static allocation
  - VRAM optimization
  - Bank switching support
- Optimization Opportunities:
  - Block instructions
  - Alternate registers
  - VDP timing optimization

### Performance Targets
- Minimum Frame Rate: 60 FPS
- Audio Update Frequency: Per frame
- Memory Budget:
  - RAM: 1KB main
  - VRAM: 16KB
  - ROM: Up to 32KB
- Known Limitations:
  - Limited main RAM
  - VDP sprite limitations
  - Memory access timing

### Special Handling
- Graphics Implementation:
  - VDP register setup
  - Sprite management
  - Pattern table optimization
- Audio Implementation:
  - PSG register updates
  - Sound effect system
  - Music playback
- Memory Management:
  - VRAM allocation
  - Pattern/color table management
  - Sprite attribute table

## References
- [ColecoVision Technical Information](http://www.atarihq.com/danb/files/CV-Tech.txt)
- [Z80 User Manual](http://www.zilog.com/docs/z80/um0080.pdf)
- [TMS9928A Manual](http://map.grauw.nl/resources/video/ti-vdp-programmers-guide.pdf)
- [SN76489 Documentation](http://www.smspower.org/Development/SN76489)
