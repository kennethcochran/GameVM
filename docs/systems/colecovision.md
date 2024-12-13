# ColecoVision

## System Overview
- CPU: Zilog Z80A
- Clock Speed: 3.58 MHz
- Video Processor: Texas Instruments TMS9928A
- Sound Processor: Texas Instruments SN76489A
- Release Year: 1982
- Generation: 2nd
- Region: Worldwide
- Predecessor: None (Coleco's first console)
- Successor: None

## CPU Details
### Architecture Characteristics
- Instruction Set Family: Zilog Z80
- Word Size: 8-bit
- Endianness: Little-endian
- Register Set:
  - Main: A, F, B, C, D, E, H, L
  - Alternate: A', F', B', C', D', E', H', L'
  - Index: IX, IY
  - Special: I (Interrupt), R (Refresh)
  - PC (Program Counter)
  - SP (Stack Pointer)
- Notable Features:
  - Extended instruction set
  - Block instructions
  - Indexed addressing
  - Multiple register sets

### Memory Access
- Address Bus Width: 16 bits
- Data Bus Width: 8 bits
- Memory Page Size: 256 bytes
- Special Addressing Modes:
  - Indexed (IX+d, IY+d)
  - Relative
  - Block transfer
  - Bit addressing
- DMA Capabilities: Via VDP

### Performance Considerations
- Instruction Timing: 4-23 T-states
- Pipeline Features: None
- Known Bottlenecks:
  - VDP access timing
  - Single accumulator
  - Memory access speed
- Optimization Opportunities:
  - Block instructions
  - Alternate register set
  - Index register usage

## Memory Map
### RAM
- Total Size: 1KB main + 16KB video
- Layout:
  - System RAM: 1KB ($7000-$73FF)
  - Video RAM: 16KB (in VDP)
  - Stack: Configurable in main RAM
- Bank Switching: None for RAM
- Access Speed: CPU-speed dependent
- Constraints:
  - Limited main RAM
  - VDP memory timing
  - Shared video access

### ROM
- BIOS Size: 8KB
- Cartridge Size: Up to 32KB
- Bank Switching: Some cartridges
- Access Speed: CPU-speed dependent
- Special Features:
  - Multiple ROM pages possible
  - BIOS routines available
  - Some cartridges include RAM

### Special Memory Regions
- BIOS ROM: $0000-$1FFF
- RAM: $7000-$73FF
- VDP Ports: $BE, $BF
- Sound Port: $FF
- Controller Ports: $FC-$FF

## Video System (TMS9928A)
### Display Characteristics
- Resolution: 256×192
- Color Depth: 16 colors
- Refresh Rate: 60 Hz (NTSC)
- Video RAM: 16KB

### Graphics Capabilities
- Sprite Support:
  - 32 hardware sprites
  - 16×16 or 8×8 size
  - 4 sprites per line
  - Collision detection
- Background:
  - 32×24 tile grid
  - 256 unique 8×8 patterns
  - 32 simultaneous colors
  - Two planes available
- Special Effects:
  - Sprite magnification
  - External video input
  - Screen blanking
  - Multiple modes

### Graphics Modes
- Text Mode: 40×24 characters
- Graphics I: 32×24 tiles, limited color
- Graphics II: 32×24 tiles, full color
- Multicolor: 64×48 blocks
- Sprite Mode: Overlaid on any mode

### Timing
- VBLANK: 60 Hz
- HBLANK: Standard NTSC
- Access Windows: Any time via ports

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
