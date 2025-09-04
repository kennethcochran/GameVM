# Atari Lynx

## System Overview
- CPU: WDC 65SC02 + "Mikey" and "Suzy" custom chips
- CPU Clock: 4 MHz (16 MHz system clock)
- Release Year: 1989
- Generation: 4th
- Region: Worldwide
- Predecessor: None (Atari's first handheld)
- Successor: None
- Notable Features: First color LCD handheld, hardware scaling/rotation

## CPU Details
### Architecture Characteristics
- Processor: WDC 65SC02
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
    - B (Break)
    - D (Decimal, disabled)
    - I (Interrupt)
    - Z (Zero)
    - C (Carry)
- Special Features:
  - No decimal mode
  - Improved instruction timing
  - Hardware multiply support
  - DMA support

### Memory Access
- Address Bus Width: 16 bits
- Data Bus Width: 8 bits
- Memory Page Size: 256 bytes
- Memory Access Timing:
  - Basic Memory Cycle: 250ns
  - DMA: Hardware-assisted
  - Vector pulling: 7 cycles
- Special Features:
  - Zero Page addressing
  - Hardware math acceleration
  - DMA controllers

### Performance Considerations
- Instruction Timing: 2-7 cycles
- Interrupt Types:
  - Video drawing
  - Audio buffer empty
  - Timer interrupts
  - Serial communication
- Known Bottlenecks:
  - Single accumulator
  - Limited registers
  - Memory access patterns
- Optimization Opportunities:
  - Zero page usage
  - DMA utilization
  - Math co-processor
  - Hardware sprite system

## Memory Map
### System Memory
- RAM: 64 KB
- ROM: 512 bytes boot ROM
- Cart ROM: Up to 2 MB with banking
- Video RAM: Shared with main RAM

### Memory Layout
- $0000-$00FF: Zero Page
- $0100-$01FF: Stack
- $0200-$0FFF: System RAM
- $1000-$1FFF: Hardware registers
- $2000-$FDFF: General RAM
- $FE00-$FFFF: Boot ROM
- Hardware Registers:
  - Suzy: $FC00-$FCFF
  - Mikey: $FD00-$FDFF

## Graphics System (Suzy)
### Display Hardware
- Screen: 3.5-inch LCD
- Resolution: 160×102 pixels
- Colors: 4096 (12-bit)
- Colors on Screen: 16 from 4096
- Refresh Rate: 60 Hz (NTSC), 50 Hz (PAL)
- Pixel Clock: 16 MHz

### Graphics Features
- Sprite Engine:
  - Hardware sprite scaling
  - Hardware sprite rotation
  - Collision detection
  - Priority ordering
  - Up to 32 sprites/scanline
- Sprite Properties:
  - Variable size
  - 16 colors per sprite
  - Independent scaling
  - 0-270° rotation
  - X/Y flipping
- Math Co-Processor:
  - Hardware multiply
  - Hardware divide
  - Matrix operations
  - Collision calculations

### Special Effects
- Hardware Scaling:
  - Independent X/Y scaling
  - 0.5x to 16x range
  - Real-time capability
- Sprite Rotation:
  - 0° to 270° in 90° steps
  - Per-sprite rotation
  - Combined with scaling
- Collision Detection:
  - Pixel-perfect
  - Multiple collision types
  - Hardware-assisted

### Display Modes
- Standard Mode:
  - 160×102 resolution
  - 16 colors on screen
  - 4096 color palette
- Varied Color Modes:
  - 4 bits per pixel standard
  - Variable color depth
  - Palette manipulation
- Special Modes:
  - LED Mode (power saving)
  - Variable screen size

## Audio System (Mikey)
### Audio Hardware
- Channels: 4 independent
- Sample Size: 8-bit
- Sample Rate: Up to 32 KHz
- Waveform Memory: Shared RAM
- Volume Control: 6-bit

### Channel Features
- Waveform Types:
  - PCM playback
  - Custom waveforms
  - Noise generation
- Volume Control:
  - 64 levels per channel
  - Independent channel control
  - Master volume
- Frequency Control:
  - Variable frequency
  - Timer-based timing
  - Sample rate control

### Special Features
- Timer Integration
- DMA Support
- Interrupt Generation
- Flexible Mixing

## Input/Output System
### Controller Interface
- D-Pad (8 directions)
- A and B buttons
- Option 1 and Option 2
- Pause button
- Flip screen button
- Left/right-handed operation

### Communication
- ComLynx Port:
  - Up to 8 players
  - 250 Kbit/s transfer
  - Network topology
  - Hot-swappable

### Storage Interface
- Cartridge Port:
  - ROM: Up to 2 MB
  - Battery backup
  - Bank switching
  - Security system

### Power System
- Battery: 6 AA batteries
- Power LED
- Battery life: 4-5 hours
- External power port
- Power management

## System Integration Features
### Hardware Components
- Mikey Chip:
  - Sound generation
  - Timer control
  - Serial communications
  - Display control
- Suzy Chip:
  - Sprite engine
  - Hardware math
  - Collision detection
  - Memory management

### Power Management
- Sleep Mode
- Variable Clock
- Power LED
- Battery Monitor

### Special Features
- Ambidextrous Design
- Screen Rotation
- Network Gaming
- Hardware Math
- DMA Control

## Technical Legacy
### Hardware Innovations
- First color handheld
- Hardware scaling/rotation
- Networking capability
- Math co-processor
- Ambidextrous design

### Software Development
- Development Environment:
  - 65SC02 assembly
  - C compiler support
  - Hardware debugging
  - Development system
- Programming Features:
  - Sprite control
  - Math co-processor
  - Audio generation
  - Network support

### Market Impact
- Production Run: 1989-1995
- Units Sold: ~2 million
- Games Released: ~70
- Price Points:
  - Launch: $179.95
  - Final: $99.99
- Competition:
  - Game Boy
  - Game Gear
  - TurboExpress

## GameVM Implementation Notes
### Compiler Considerations
- Preferred Code Generation Strategy:
  - 6502 native code
  - Hardware acceleration
  - DMA coordination
- Register Allocation Strategy:
  - Zero page optimization
  - Accumulator usage
  - Index register patterns
- Memory Management:
  - DMA coordination
  - Sprite data handling
  - Audio buffer management

### Performance Targets
- Frame Rate: 60/50 Hz
- Audio Rate: Up to 32 KHz
- Sprite Count: Up to 32/scanline
- Memory Budget:
  - RAM: 64 KB
  - ROM: Up to 2 MB
  - Stack: 256 bytes
  - Zero Page: 256 bytes

### Special Handling
- Sprite System Control
- Math Co-processor Usage
- Audio Buffer Management
- Network Communication
- Power Management

## References
- [Lynx Hardware Documentation](http://www.atarihq.com/danb/files/lynx_hw.txt)
- [6502 Programming Manual](http://www.6502.org/documents/datasheets/mos/mos_6500_programming_manual.pdf)
- [Epyx Lynx Documentation](http://www.atarihq.com/danb/files/lynx_prog.txt)
- [Mikey/Suzy Technical Reference](http://www.atarihq.com/danb/files/lynx_reg.txt)
