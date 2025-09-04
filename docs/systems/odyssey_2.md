# Magnavox Odyssey² / Philips Videopac G7000

## System Overview
- CPU: Intel 8048 @ 1.79 MHz
- Release Year: 1978
- Generation: 2nd
- Region: Worldwide (as Odyssey² and Videopac)
- Predecessor: Magnavox Odyssey
- Successor: Philips Videopac+ G7400
- Notable Feature: Integrated keyboard

## CPU Details
### Architecture Characteristics
- Processor: Intel 8048 microcontroller
- Clock Speed: 5.37 MHz ÷ 3 = 1.79 MHz
- Word Size: 8-bit
- Endianness: Little-endian
- Register Set:
  - Accumulator (A): 8-bit
  - Register Banks: 2×8 registers (R0-R7)
  - Program Counter (PC): 12-bit
  - Program Status Word (PSW)
    - C (Carry)
    - AC (Auxiliary Carry)
    - F0 (User Flag)
    - BS (Bank Select)
    - F1 (User Flag)
  - Stack Pointer: 8-bit (internal RAM)
  - Timer/Counter: 8-bit
- Special Features:
  - Built-in timer/counter
  - Internal RAM
  - Event counter input
  - Test inputs
  - Conditional branching

### Memory Access
- Program Memory: 12-bit address space
- Data Memory: 8-bit address space
- Internal RAM: 64 bytes (128 bytes in 8048H)
- Memory Access Timing:
  - Program Memory: 2-3 cycles
  - Internal RAM: 1 cycle
  - External RAM: 2 cycles
- No DMA capability
- Separate program and data spaces

### Performance Considerations
- Instruction Timing: 1-2 cycles (most instructions)
- Single-cycle internal RAM access
- Known Bottlenecks:
  - Limited RAM
  - No hardware sprites
  - Shared CPU/video timing
- Optimization Opportunities:
  - Internal RAM usage
  - Register bank switching
  - Efficient I/O handling

## Memory Map
### System Memory
- Internal RAM: 64 bytes (CPU)
- External RAM: 128 bytes
- ROM: 1 KB (built-in monitor)
- Cartridge ROM: Up to 8 KB
- Character ROM: 512 bytes

### Memory Layout
- Internal Memory:
  - $00-$07: Register Bank 0
  - $08-$0F: Register Bank 1
  - $10-$17: Stack area
  - $18-$3F: General purpose RAM
- External Memory:
  - $000-$3FF: System ROM
  - $400-$7FF: Character ROM
  - $800-$9FF: External RAM
  - $A00-$FFF: Cartridge ROM

## Video System
### Display Processor
- Custom Intel Graphics Generator
- Resolution: 160×200 pixels (NTSC)
- Character-based graphics
- Refresh Rate: 60 Hz (NTSC), 50 Hz (PAL)
- No hardware sprites

### Graphics Capabilities
- Character Grid:
  - 8×8 pixel characters
  - 20×24 character display
  - 64 predefined characters
  - 64 programmable characters
- Colors:
  - 8 colors + black
  - 4 luminance levels
  - Color limitations per line
- Background:
  - Single color per line
  - Programmable color
  - No scrolling capability

### Character Generation
- Built-in Character Set:
  - 64 fixed characters
  - ASCII-like encoding
  - Graphics symbols
  - Game elements
- Programmable Characters:
  - 64 user-definable patterns
  - 8×8 pixel resolution
  - Single color per character
  - Real-time modification possible

### Special Features
- Quad Movement:
  - 4 movable objects
  - Character-based movement
  - Collision detection
  - Limited animation
- Display Control:
  - Character blinking
  - Background color selection
  - Character color selection
  - Limited raster effects

## Audio System
## Input/Output System
### Built-in Keyboard
- Full 49-key layout
- Alpha-numeric input
- Function keys
- Game control keys
- Special purpose keys
- Membrane technology

### Controller Ports
- Number of Ports: 2
- Controller Type:
  - 8-way digital joystick
  - Single action button
  - Non-detachable (most models)
- Input Processing:
  - Direct CPU polling
  - No interrupt capability
  - 8-direction digital input

### Storage Interface
- Cartridge Slot:
  - ROM Size: Up to 8 KB
  - Edge connector
  - No bank switching
  - No save capability
- System ROM:
  - 1 KB built-in
  - Character ROM
  - BIOS functions

### Video Output
- RF Output: Standard
- Video Formats:
  - NTSC: 60 Hz
  - PAL: 50 Hz
- Resolution: 160×200 pixels
- Colors: 8 + black

## System Integration Features
### Hardware Variants
- Magnavox Odyssey² (North America)
- Philips Videopac G7000 (Europe)
- Philips Odyssey (Brazil)
- Enhanced Features in G7400:
  - Higher resolution
  - More colors
  - Improved graphics

### Regional Differences
- NTSC Models:
  - 60 Hz refresh
  - 120V power supply
  - Odyssey² branding
- PAL Models:
  - 50 Hz refresh
  - 220-240V power supply
  - Videopac branding
- Brazilian Models:
  - Modified case design
  - PAL-M system
  - Portuguese keyboard

### Special Features
- Built-in BASIC Programming
- Voice Enhancement Module
- The Voice add-on capability
- Chess Module support

## Technical Legacy
### Hardware Innovations
- Integrated keyboard
- Programmable characters
- Voice synthesis capability
- Educational focus

### Software Development
- Development Tools:
  - Assembly language
  - Custom development systems
  - Character editors
- Programming Features:
  - Character manipulation
  - Keyboard input
  - Sound generation
  - Quad object movement

### Market Impact
- Production Run: 1978-1984
- Units Sold: ~2 million
- Games Released: ~70 officially
- Price Points:
  - Launch: $179
  - Final: $99
- Competition:
  - Atari 2600
  - Intellivision
  - Channel F

### Programming Resources
- System ROM Functions:
  - Character display
  - Keyboard scanning
  - Sound generation
  - Controller reading
- Development Support:
  - Character definitions
  - Sound routines
  - Input handling
  - Display management







## Audio System
### Audio Hardware
- Sound Channels: 1
- Sample Rate: N/A (square wave)
- Bit Depth: 1-bit
- Audio Memory: Direct control

### Channel Types
- Single square wave:
  - Frequency control
  - On/off control
  - No volume control
  - No envelope

### Timing
- Audio Update Rate: Software controlled
- DMA Features: None
- Interrupt Sources: None

## System Timing
### Interrupts
- Types Available:
  - Timer interrupt
  - External interrupt
- Sources:
  - Internal timer
  - External pin
- Timing: Programmable
- Priority: Two levels

### DMA
- Transfer Rates: N/A
- Available Modes: N/A
- Timing Constraints: N/A

## GameVM Implementation Notes
### Compiler Considerations
- Preferred Code Generation Strategy:
  - Native 8048 code
  - Display synchronization
  - Memory-efficient code
- Register Allocation Strategy:
  - Internal RAM optimization
  - Register bank usage
  - Stack management
- Memory Management Strategy:
  - Split internal/external
  - Character RAM optimization
  - Static allocation
- Optimization Opportunities:
  - Internal RAM usage
  - Timer integration
  - Display list optimization

### Performance Targets
- Minimum Frame Rate: 60/50 FPS
- Audio Update Frequency: As needed
- Memory Budget:
  - Internal RAM: 256 bytes
  - External RAM: 128 bytes
  - ROM: Up to 8KB
- Known Limitations:
  - Limited RAM
  - Basic sprite system
  - Simple audio
  - Memory split

### Special Handling
- Graphics Implementation:
  - Sprite management
  - Character set optimization
  - Display timing
- Audio Implementation:
  - Software frequency control
  - Sound effect system
  - Music playback
- Memory Management:
  - Internal/external split
  - Character RAM allocation
  - Stack optimization

## References
- [Intel 8048 Datasheet](http://datasheets.chipdb.org/Intel/MCS48/INTEL-8048.pdf)
- [Videopac Technical Documentation](http://www.videopac.org/tech/)
- [8244/8245 Video Display Controller](http://www.videopac.org/technical/8244_8245.txt)
- [Programming Guide](http://www.videopac.org/programming/)
