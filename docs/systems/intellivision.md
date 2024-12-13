# Mattel Intellivision

## System Overview
- CPU: GI CP1610 (based on CP1600)
- Clock Speed: 894.886 kHz (NTSC)
- Video Processor: GI AY-3-8900-1 (STIC)
- Sound Processor: GI AY-3-8914 (PSG)
- Release Year: 1979
- Generation: 2nd
- Region: Worldwide
- Predecessor: None (Mattel's first console)
- Successor: Intellivision II

## CPU Details
### Architecture Characteristics
- Instruction Set Family: General Instrument CP1600
- Word Size: 16-bit
- Endianness: Big-endian
- Register Set:
  - R0: Program Counter
  - R1-R3: Reserved for interrupts
  - R4: Stack Pointer
  - R5: Return Address
  - R6: General Purpose
  - R7: General Purpose
- Notable Features:
  - 16-bit architecture (rare for era)
  - Hardware multiply/divide
  - Interruptible instructions
  - Decimal arithmetic support

### Memory Access
- Address Bus Width: 16 bits
- Data Bus Width: 16 bits
- Memory Page Size: N/A (flat memory model)
- Special Addressing Modes:
  - Direct
  - Indirect
  - Immediate
  - Relative
- DMA Capabilities: Via STIC

### Performance Considerations
- Instruction Timing: 4-12 cycles per instruction
- Pipeline Features: None
- Known Bottlenecks:
  - Slow CPU clock speed
  - STIC video lockout periods
  - Limited RAM
- Optimization Opportunities:
  - 16-bit operations
  - Hardware multiply/divide
  - Efficient register usage

## Memory Map
### RAM
- Total Size: 352 bytes
- Layout:
  - System RAM: 240 bytes
  - Graphics RAM: 112 bytes (STIC)
  - Scratchpad: 240 bytes
- Bank Switching: None for RAM
- Access Speed: CPU-speed dependent
- Constraints:
  - Limited RAM size
  - STIC lockout periods
  - Split memory spaces

### ROM
- Cartridge Size: 4KB-32KB
- Bank Switching: Yes (various schemes)
- Access Speed: CPU-speed dependent
- Special Features:
  - Multiple bank switching schemes
  - 16-bit wide ROM access
  - Lockout during video access

### Special Memory Regions
- STIC Registers: $0000-$007F
- PSG Registers: $01F0-$01FF
- System ROM: $1000-$1FFF
- Cartridge ROM: $5000-$6FFF

## Video System (STIC)
### Display Characteristics
- Resolution: 159×96 (20×12 tiles)
- Color Depth: 16 colors
- Refresh Rate: 60 Hz (NTSC)
- Video RAM: 112 bytes

### Graphics Capabilities
- Sprite Support:
  - 8 hardware sprites (MOBs)
  - 8×8 or 8×16 size
  - Collision detection
  - Priority control
- Background:
  - 20×12 tile grid
  - 8×8 pixel tiles
  - 2 colors per tile
  - Card-based system
- Special Effects:
  - Sprite interactions
  - Color cycling
  - Border control
  - Foreground/background modes

### Timing
- VBLANK: Standard NTSC timing
- HBLANK: Standard NTSC timing
- Access Windows: Outside STIC lockout

## Audio System (PSG)
### Audio Hardware
- Sound Channels: 3 + noise
- Sample Rate: N/A (tone generation)
- Bit Depth: 4-bit volume per channel
- Audio Memory: Direct register control

### Channel Types
- Three identical tone channels:
  - Frequency control
  - Volume control
  - Envelope control
- One noise channel:
  - White noise generator
  - Volume control
  - Frequency control

### Timing
- Audio Update Rate: Any time
- DMA Features: None
- Interrupt Sources: None

## System Timing
### Interrupts
- Types Available:
  - STIC video interrupt
  - External interrupts
- Sources:
  - Video system
  - Controller input
  - Timer
- Timing: 60 Hz video sync
- Priority: Multiple levels

### DMA
- Transfer Rates: During VBLANK
- Available Modes: STIC to GRAM
- Timing Constraints: Video sync locked

## GameVM Implementation Notes
### Compiler Considerations
- Preferred Code Generation Strategy:
  - 16-bit optimized code
  - Efficient register usage
  - STIC-aware timing
- Register Allocation Strategy:
  - Maximize use of R6-R7
  - Careful interrupt handling
  - Stack optimization
- Memory Management Strategy:
  - Static allocation
  - GRAM optimization
  - Bank switching support
- Optimization Opportunities:
  - 16-bit operations
  - Hardware multiply/divide
  - STIC timing optimization

### Performance Targets
- Minimum Frame Rate: 60 FPS
- Audio Update Frequency: Per frame
- Memory Budget:
  - RAM: 352 bytes total
  - GRAM: 112 bytes
  - ROM: Up to 32KB
- Known Limitations:
  - STIC lockout periods
  - Limited RAM
  - Slow CPU clock

### Special Handling
- Graphics Implementation:
  - STIC synchronization
  - MOB (sprite) management
  - Background tile optimization
- Audio Implementation:
  - PSG register updates
  - Volume envelope control
  - Sound effect system
- Memory Management:
  - Bank switching control
  - GRAM allocation
  - RAM optimization

## References
- [Intellivision Technical Reference](http://sdk-1600.spatula-city.org/)
- [CP1610 Instruction Set](http://spatula-city.org/~im14u2c/intv/tech/cp1600/)
- [STIC Documentation](http://spatula-city.org/~im14u2c/intv/tech/stic.html)
- [PSG Documentation](http://spatula-city.org/~im14u2c/intv/tech/psg.html)
- [IntyBASIC Compiler Documentation](http://nanochess.org/intybasic.html)
