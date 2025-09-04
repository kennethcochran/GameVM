# Bally Professional Arcade / Astrocade

## System Overview
- CPU: Zilog Z80 @ 1.789 MHz
- Release Year: 1977 (as Bally Home Library Computer)
- Generation: 2nd
- Region: North America
- Predecessor: None
- Successor: None
- Notable Features: High-resolution graphics, BASIC programming

## CPU Details
### Architecture Characteristics
- Processor: Zilog Z80
- Clock Speed: 1.789 MHz (half NTSC colorburst)
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
  - Bit manipulation
  - Index register operations
  - Two register sets

### Memory Access
- Address Bus Width: 16 bits
- Data Bus Width: 8 bits
- Memory Page Size: 4 KB
- Memory Access Timing:
  - Basic Memory Cycle: 4 T-states
  - I/O Operations: 4 T-states
  - Instruction Fetch: 4-6 T-states
- DMA: Not available
- Memory Refresh: Automatic (R register)

### Performance Considerations
- Instruction Timing: 4-23 T-states
- Interrupt Modes: IM 0, IM 1, IM 2
- Known Bottlenecks:
  - Video memory access
  - Cartridge ROM speed
  - Limited RAM
- Optimization Opportunities:
  - Block instructions
  - Register alternates
  - Screen buffer management

## Memory Map
### System Memory
- RAM: 4 KB (expandable to 64 KB)
- ROM: 8 KB built-in
- Cartridge ROM: Up to 32 KB
- Video RAM: Shared with main RAM

### Memory Layout
- $0000-$1FFF: System ROM (8 KB)
  - $0000-$07FF: BIOS
  - $0800-$1FFF: Built-in BASIC interpreter
- $2000-$3FFF: Graphics RAM (8 KB)
- $4000-$7FFF: Cartridge ROM space
- $8000-$FFFF: Expansion RAM (optional)
- I/O Ports:
  - $10-$18: Sound registers
  - $19: Graphics mode control
  - $20-$3F: Custom chip registers

## Video System
### Display Processor
- Custom Magic chip
- Resolution: 160×102 to 320×204 pixels
- Colors: 8 colors per scan line from 256 color palette
- Refresh Rate: 60 Hz (NTSC)

### Graphics Capabilities
- Display Modes:
  - 160×102 (2 bytes per 8 pixels)
  - 320×204 (4 bytes per 8 pixels)
  - 160×102 with 4 planes
- Color Features:
  - 256 color palette
  - 8 colors per scan line
  - Color rotation capabilities
  - Independent foreground/background
- Pattern Generator:
  - 8×8 pixel characters
  - Programmable patterns
  - Hardware pattern expansion
  - Pattern rotation

### Advanced Features
- Hardware screen rotation
- On-the-fly palette changes
- Pixel-by-pixel color control
- Hardware-assisted drawing:
  - Line drawing
  - Pattern fill
  - Screen clear
  - Color cycling
- Multiple screen pages
- Smooth scrolling support

### Screen Memory
- Direct memory mapping
- Bit-mapped display
- Interleaved memory access
- Double buffering capable
- Memory-mapped color control

## Audio System
## Input/Output System
### Controller Interface
- Number of Ports: 4
- Controller Features:
  - Analog joystick (knob)
  - Trigger button
  - Second action button
  - Numeric keypad (0-9)
  - Additional function buttons
- Special Controls:
  - Built-in controllers
  - Light pen support
  - ZGRASS keyboard (optional)
  - Expansion interface

### Storage Interface
- Cartridge Slot:
  - ROM Size: Up to 32 KB
  - Edge connector
  - Software protection
  - Expansion capability
- Cassette Interface (optional):
  - 300 baud
  - Program storage
  - Data storage
  - Custom format

### Video Output
- RF Output: Standard
- Video Format: NTSC
- Resolution Modes:
  - 160×102
  - 320×204
- Colors: 256 palette

### Expansion Capabilities
- ZGRASS-32 upgrade
- Memory expansion
- Cassette interface
- Printer interface
- Additional controllers

## System Integration Features
### Hardware Variants
- Bally Home Library Computer (1977)
- Bally Professional Arcade (1978)
- Astrocade (1981)
- Third-party expansions

### Programming Features
- Built-in BASIC:
  - Graphical commands
  - Sound commands
  - Game development
  - Educational software
- ZGRASS Programming:
  - High-level language
  - Graphics primitives
  - Advanced math functions
  - Real-time capabilities

### Special Features
- Built-in Games:
  - Calculator
  - Checkbook balancing
  - Scribbling
  - Gunfight
- Light Pen Support:
  - Drawing applications
  - Menu selection
  - Educational software
- Music Features:
  - Sound editor
  - Music composition
  - Waveform creation

## Technical Legacy
### Hardware Innovations
- High-resolution graphics
- Advanced sound capabilities
- Built-in BASIC
- Expandable architecture
- Multiple controller ports

### Software Development
- Development Tools:
  - Built-in BASIC
  - ZGRASS system
  - Assembly development
  - Graphics tools
- Programming Features:
  - Direct video access
  - Sound programming
  - Controller handling
  - Interrupt system

### Market Impact
- Production Run: 1977-1983
- Units Sold: ~100,000
- Games Released: ~40 officially
- Price Points:
  - Launch: $299
  - Final: $199
- Competition:
  - Atari 2600
  - Intellivision
  - Channel F

### Programming Resources
- System ROM Functions:
  - Graphics primitives
  - Sound generation
  - Controller reading
  - BASIC interpreter
- Development Support:
  - Character patterns
  - Sprite handling
  - Sound effects
  - Game logic

## System Overview
- CPU: Zilog Z80 (custom variant)
- Clock Speed: 1.789 MHz
- Release Year: 1978
- Generation: 2nd
- Region: North America
- Predecessor: None (Bally's first console)
- Successor: None

## CPU Details
### Architecture Characteristics
- Instruction Set Family: Z80
- Word Size: 8-bit
- Endianness: Little-endian
- Register Set:
  - Main: A, F, B, C, D, E, H, L
  - Alternate: A', F', B', C', D', E', H', L'
  - Index: IX, IY
  - Special: I (Interrupt), R (Refresh)
  - PC (Program Counter)
  - SP (Stack Pointer)

### Memory Access
- Address Bus Width: 16 bits
- Data Bus Width: 8 bits
- Memory Page Size: 256 bytes
- Special Addressing Modes:
  - Indexed (IX+d, IY+d)
  - Block transfer instructions
  - Bit manipulation
- DMA Capabilities: None

### Performance Considerations
- Instruction Timing: 4-23 T-states
- Pipeline Features: None
- Known Bottlenecks:
  - Single accumulator
  - Memory access patterns
  - Video timing constraints
- Optimization Opportunities:
  - Block instructions
  - Alternate register set
  - Index register usage

## Memory Map
### RAM
- Total Size: 4KB (expandable to 32KB)
- Layout:
  - System RAM: 4KB
  - Screen RAM: Shared with system RAM
  - Stack: Configurable in main RAM
- Bank Switching: None for RAM
- Access Speed: CPU-speed dependent
- Constraints:
  - Limited base RAM
  - Shared video memory
  - No dedicated VRAM

### ROM
- Built-in ROM: 8KB
- Cartridge Size: Up to 32KB
- Bank Switching: Some cartridges
- Access Speed: CPU-speed dependent
- Special Features:
  - BASIC in ROM
  - Calculator mode
  - Music composition tools

### Special Memory Regions
- I/O Ports: $00-$FF
- Video Registers: Custom memory-mapped
- Sound Registers: Custom memory-mapped
- Controller Ports: Memory-mapped

## Video System
### Display Characteristics
- Resolution: 160×102 to 320×204
- Color Depth: 8 colors per scan line (from palette of 256)
- Refresh Rate: 60 Hz (NTSC)
- Video RAM: Shared with system RAM

### Graphics Capabilities
- Sprite Support: None (software-based)
- Background:
  - Custom display lists
  - Hardware scaling
  - Multiple resolution modes
  - Color per scan line
- Special Effects:
  - Line drawing hardware
  - Pattern fill
  - Hardware zoom
  - Palette manipulation

### Timing
- VBLANK Duration: Standard NTSC
- HBLANK Duration: Standard NTSC
- Access Windows: During VBLANK/HBLANK

## Audio System
### Audio Hardware
- Sound Channels: 3 + noise
- Sample Rate: N/A (tone generation)
- Bit Depth: Variable frequency control
- Audio Memory: Direct register control

### Channel Types
- Three tone generators:
  - Frequency control
  - Volume control
  - Waveform selection
- One noise channel:
  - White noise
  - Volume control
  - Frequency control

### Timing
- Audio Update Rate: Any time
- DMA Features: None
- Interrupt Sources: None

## System Timing
### Interrupts
- Types Available:
  - Vertical blank
  - External interrupts
- Sources:
  - Display system
  - External input
- Timing: 60 Hz video sync
- Priority: Standard Z80 modes

### DMA
- Transfer Rates: N/A
- Available Modes: N/A
- Timing Constraints: N/A

## GameVM Implementation Notes
### Compiler Considerations
- Preferred Code Generation Strategy:
  - Z80 native code
  - Display list optimization
  - Audio timing awareness
- Register Allocation Strategy:
  - Use alternate register set
  - Index registers for arrays
  - Stack optimization
- Memory Management Strategy:
  - Static allocation
  - Display list management
  - Audio buffer management
- Optimization Opportunities:
  - Block instructions
  - Display list compilation
  - Audio timing

### Performance Targets
- Minimum Frame Rate: 60 FPS
- Audio Update Frequency: Per frame
- Memory Budget:
  - RAM: 4KB baseline
  - Extended RAM: Up to 32KB
  - ROM: Up to 32KB
- Known Limitations:
  - Limited base RAM
  - No hardware sprites
  - Shared video memory

### Special Handling
- Graphics Implementation:
  - Custom display list generator
  - Scan line color management
  - Pattern fill optimization
- Audio Implementation:
  - Direct tone control
  - Noise channel management
  - Music sequencing
- Memory Management:
  - RAM optimization
  - Display list allocation
  - Stack management

## References
- [Bally Astrocade Technical Reference](http://www.ballyalley.com/ml/ML_reference.pdf)
- [Z80 User Manual](http://www.zilog.com/docs/z80/um0080.pdf)
- [Hardware Documentation](http://www.ballyalley.com/documentation/documentation.html)
- [Programming Guide](http://www.ballyalley.com/basic/Bally_Basic_Reference_Manual.pdf)
