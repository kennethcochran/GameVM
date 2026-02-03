# Mattel Intellivision

## System Overview
- Profile: GV.Spec.L2
- CPU: General Instrument CP1610
- Clock Speed: 894.886 kHz (NTSC), 894.779 kHz (PAL)
- Release Year: 1979
- Generation: 2nd
- Region: Worldwide
- Predecessor: None (Mattel's first console)
- Successor: Intellivision II, Intellivision III (cancelled)

## CPU Details
### Architecture Characteristics
- Instruction Set: General Instrument CP1600 16-bit architecture
- Word Size: 16-bit
- Endianness: Big-endian
- Register Set:
  - R0: Program Counter
  - R1-R3: Reserved for interrupts
  - R4: Stack Pointer
  - R5: Return Address
  - R6: General Purpose
  - R7: General Purpose
  - Status Register: 16-bit
    - S (Sign)
    - Z (Zero)
    - O (Overflow)
    - C (Carry)
    - I (Interrupt Enable)
    - D (Double Byte)
- Notable Features:
  - 16-bit architecture (rare for era)
  - Hardware multiply/divide
  - Direct memory addressing
  - Indirect addressing through registers
  - Auto-increment/decrement
  - Interruptible instructions
  - Decimal arithmetic support

### Memory Access
- Address Bus Width: 16 bits
- Data Bus Width: 16 bits (10-bit external)
- Memory Page Size: N/A (flat memory model)
- Memory Access Timing:
  - Basic Instruction: 4-12 cycles
  - Memory Read: 4 cycles minimum
  - Memory Write: 4 cycles minimum
  - Interrupt Response: 24 cycles
- Special Addressing Modes:
  - Direct
  - Indirect
  - Immediate
  - Relative
- DMA Capabilities: Via STIC
- Shared memory access with STIC

### Performance Considerations
- Instruction Timing: 4-52 cycles per instruction
- Pipeline Features: None
- Clock Speed Limitations:
  - CPU halts during STIC active display
  - Memory contention with STIC
- Known Bottlenecks:
  - Limited RAM
  - STIC display synchronization
  - Slow clock speed
- Optimization Opportunities:
  - 16-bit operations
  - Hardware multiply/divide
  - Register-based operations
  - STIC idle period utilization
  - Interrupt-driven timing
  - Efficient register usage

## Memory Map
### RAM
- Total Size: 352 bytes main + 240 bytes STIC
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
- System ROM: 7168 bytes (Executive ROM)
- Cartridge Size: 4KB-32KB
- Bank Switching: Yes (various schemes)
- Access Speed: CPU-speed dependent
- Special Features:
  - Multiple bank switching schemes
  - 16-bit wide ROM access
  - Lockout during video access

### Memory Layout
- $0000-$03FF: Scratch RAM (352 bytes)
- $0400-$047F: STIC registers
- $0500-$0FFF: Unused
- $1000-$1FFF: Executive ROM
- $2000-$2FFF: Graphics ROM (GROM)
- $3000-$3FFF: Cartridge ROM (8K)
  - Expandable to $4000-$6FFF (16K)
- $7000-$7FFF: Unmapped
- $8000-$FFFF: Undefined (expansion bus)

### Special Memory Regions
- STIC Registers: $0000-$007F
- PSG Registers: $01F0-$01FF
- System ROM: $1000-$1FFF
- Cartridge ROM: $5000-$6FFF

## Video System
### Standard Television Interface Chip (STIC)
- Chip: General Instrument AY-3-8900-1
- Display Resolution: 159×96 pixels (20×12 cards)
- Color Depth: 16 colors
- Refresh Rate: 60 Hz (NTSC), 50 Hz (PAL)
- Video RAM: 240 bytes
- Timing:
  - VBLANK: Standard NTSC timing
  - HBLANK: Standard NTSC timing
  - Access Windows: Outside STIC lockout

### Graphics Capabilities
- Background Grid:
  - 20×12 cards (8×8 pixels each)
  - 2 colors per card
  - Foreground/Background color pairs
- Sprites (MOBs - Moving Objects):
  - 8 hardware sprites
  - Size: 8×8 or 8×16 pixels
  - Colors: 8 colors per sprite
  - Position: Pixel-precise
  - Collision detection
  - Auto-motion capability
  - Priority control
  
### Display Modes
- Colored Squares Mode:
  - 20×12 colored cards
  - 2 colors per card
- Foreground/Background Mode:
  - Separate foreground/background colors
  - Color stack for background
- GRAM Mode:
  - Programmable character definitions
  - 64 redefinable characters
  
### Special Features
- Color Stack:
  - 4-color rotating background
  - Automatic cycling
- Border Control:
  - Programmable border color
  - Border masking
- Collision Detection:
  - Sprite-to-sprite
  - Sprite-to-background
- Display Enable/Disable
- Direct Memory Access
- Sprite interactions
- Color cycling
- Card-based system

## Audio System
## Input/Output System
### Controller Interface
- Number of Ports: 2
- Controller Type: 16-direction disc + 12-button keypad
- Features per Controller:
  - 16-way directional disc
  - 12-button numeric keypad
  - 4 action buttons (2 per side)
  - Overlays for game-specific functions
  - 6-foot coiled cord
  - Non-detachable on original model

### Storage Interface
- Cartridge Slot:
  - ROM Size: 8K standard, 16K maximum
  - Edge connector: 44-pin
  - Gold-plated contacts
  - Write protection
- External Expansion:
  - System Changer (for Atari 2600 games)
  - Computer Adapter
  - Music Synthesizer

### Video Output
- RF Output: Standard
- Video Formats:
  - NTSC: 60 Hz
  - PAL: 50 Hz
- Resolution: 159×96 pixels
- Colors: 16 total

### Power System
- Input Voltage: 16.2V AC
- Power Supply: External transformer
- Power Consumption: ~7W
- Power LED indicator

## System Integration Features
### Hardware Variants
- Original Intellivision (1979)
- Intellivision II (1982)
  - Redesigned case
  - Detachable controllers
  - Modified video output
- Tandyvision One
  - Radio Shack branded version
- Sears Super Video Arcade
  - Sears branded version

### Regional Differences
- NTSC (North America)
  - 60 Hz refresh
  - 16.2V AC power
- PAL (Europe)
  - 50 Hz refresh
  - 220V AC power
- Different game release schedules
- Language variations in overlays

### Special Features
- Built-in Executive ROM
- Overlay system for contextual controls
- System reset button
- LED power indicator
- Expansion port
- Dual-controller design

## Technical Legacy
### Hardware Innovations
- 16-bit CPU architecture
- Programmable character generator (GRAM)
- 16-direction controller
- Overlay system for game controls
- Hardware sprite system

### Software Development
- Development System:
  - APh Technological Consulting
  - Assembler-based
  - EXEC ROM routines
- Programming Features:
  - EXEC ROM subroutines
  - Interrupt-driven timing
  - STIC synchronization
  - Sound queue system

### Market Impact
- Production Run: 1979-1984
- Units Sold: ~3 million
- Games Released: ~125 officially
- Price Points:
  - Launch: $299
  - Final: $69
- Competition:
  - Atari 2600
  - ColecoVision
  - Vectrex

### Programming Tools
- Development Language: Assembly
- EXEC ROM Support:
  - Standard routines
  - Sound drivers
  - Controller input
  - Display management
- Debug Capabilities:
  - Built-in ROM routines
  - Test points on board
  - Developer cartridges



## Audio System
### Programmable Sound Generator (PSG)
- Chip: General Instrument AY-3-8914
- Channels: 3 independent + noise
- Features per Channel:
  - Frequency: 12-bit control
  - Volume: 4-bit control (16 levels)
  - Envelope Control
  - Noise mixing

### Channel Types
- Three identical tone channels:
  - Frequency control
  - Volume control
  - Envelope control
- One noise channel:
  - White noise generator
  - Volume control
  - Frequency control

### Audio Characteristics
- Sample Rate: N/A (tone generation)
- Bit Depth: 4-bit volume per channel
- Audio Memory: Direct register control
- Sound Types:
  - Square waves
  - White noise
  - Enveloped sound
- Volume Range: 16 levels per channel
- Frequency Range: 30 Hz to 12 kHz

### Timing and Control
- Audio Update Rate: Any time
- DMA Features: None
- Interrupt Sources: None
- Control Method: Direct register access

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
