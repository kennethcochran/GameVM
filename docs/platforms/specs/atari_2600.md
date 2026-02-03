# Atari Video Computer System (VCS/2600)

## System Overview
- Profile: GV.Spec.L1
- CPU: MOS Technology 6507
- Clock Speed: 1.19 MHz (NTSC), 1.182097 MHz (PAL)
- Release Year: 1977
- Generation: 2nd
- Region: Worldwide
- Predecessor: Atari Pong consoles
- Successor: Atari 5200

## CPU Details
### Architecture Characteristics
- Instruction Set: MOS 6502 subset
- Word Size: 8-bit
- Address Bus: 13-bit (compared to 6502's 16-bit)
- Endianness: Little-endian
- Register Set:
  - Accumulator (A): 8-bit
  - Index Registers (X, Y): 8-bit each
  - Stack Pointer (S): 8-bit
  - Program Counter (PC): 13-bit
  - Status Register (P): 8-bit flags
    - N (Negative)
    - V (Overflow)
    - B (Break)
    - D (Decimal Mode)
    - I (Interrupt Disable)
    - Z (Zero)
    - C (Carry)

### Memory Access
- Address Space: 8,192 bytes (8K)
- Page Size: 256 bytes
- Zero Page: Available (first 256 bytes)
- Stack: Fixed at $0100-$01FF
- Memory Access Timing:
  - Read: 3 cycles
  - Write: 3 cycles
  - Read-Modify-Write: 5 cycles
- No DMA capability

### Performance Considerations
- Instruction Timing: 2-7 cycles
- Clock Speed Limitations:
  - NTSC: 1.19 MHz
  - PAL: 1.182097 MHz
- Known Bottlenecks:
  - Limited RAM
  - No interrupts (requires precise timing)
  - TIA synchronization required
- Optimization Opportunities:
  - Zero page usage
  - Cycle-counted loops
  - Racing the beam techniques

## Memory Map
### System Memory
- RAM: 128 bytes
- ROM: None built-in (cartridge only)
- Cartridge ROM: Up to 4K standard (up to 32K with bank switching)
- TIA Registers: 32 bytes

### Memory Layout
- $0000-$007F: TIA registers (write)
- $0080-$00FF: RAM (128 bytes)
- $0280-$029F: PIA RAM I/O, timer
- $1000-$1FFF: Cartridge ROM (4K)
  - Mirrored through remaining space

## Video System
### Television Interface Adapter (TIA)
- Chip: Custom Atari TIA
- Display Resolution:
  - Horizontal: 160 pixels (color clocks)
  - Vertical: 192 scanlines (NTSC), 228-312 (PAL)
- Sync: No frame buffer, "racing the beam" design
- Refresh Rate: 60 Hz (NTSC), 50 Hz (PAL)

### Graphics Capabilities
- Playfield:
  - Resolution: 40 bits wide (stretched to 160 pixels)
  - Mirroring: None, normal, or inverse
  - Colors: 1 foreground, 1 background
  - Scrolling: None (must be done manually)
- Sprites (Players):
  - Number: 2 players
  - Resolution: 8 bits wide
  - Position: Pixel-precise horizontal
  - Colors: 1 color per player
  - Copies: Up to 3 copies per player
- Missiles:
  - Number: 2 (one per player)
  - Width: 1, 2, 4, or 8 pixels
  - Colors: Same as parent player
- Ball:
  - Number: 1
  - Width: 1, 2, 4, or 8 pixels
  - Color: Same as playfield

### Special Features
- Collision Detection:
  - Player-Player
  - Player-Playfield
  - Player-Ball
  - Player-Missile
  - Missile-Playfield
  - Missile-Ball
  - Ball-Playfield
- VBLANK and VSYNC control
- Color burst control
- Sprite stretching
- Player reflection
- Priority control

## Audio System
## Input/Output System
### Controller Ports
- Number of Ports: 2
- Connector Type: DE-9
- Supported Controllers:
  - Standard Joystick
    - 8-way digital direction
    - Single button
  - Paddle Controllers (pair)
    - Analog rotation
    - Single button
  - Keyboard Controllers
    - 12-key keypad
  - Driving Controller
    - Continuous rotation
    - Single button
  - Optional third and fourth controller ports

### Storage Interface
- Cartridge Slot:
  - ROM Size: 4K standard
  - Bank Switching: Supported
    - Up to 32K with various schemes
  - Edge connector design
  - Hot-swappable (not recommended)
- No built-in storage
- No save capability (standard)

### Video Output
- RF Output: Standard
- Video Formats:
  - NTSC: 60 Hz
  - PAL: 50 Hz
  - SECAM: 50 Hz (specific models)
- Resolution: 160×192 (NTSC)
- Colors:
  - NTSC: 128 colors
  - PAL: 104 colors
  - SECAM: 8 colors

### Power System
- Input Voltage: 9V DC
- Power Supply: External AC adapter
- Power Consumption: ~4W
- Power Switch: Integrated RF switch

## System Integration Features
### Hardware Variants
- Original VCS (1977)
  - Heavy Sixer
  - Light Sixer
- VCS 4-switch (1980)
- Atari 2600 Jr. (1986)

### Regional Differences
- NTSC (North America, Japan)
  - 60 Hz refresh
  - 262 scanlines
- PAL (Europe, Australia)
  - 50 Hz refresh
  - 312 scanlines
- SECAM (France)
  - 50 Hz refresh
  - Limited color palette

### Special Features
- TV Type Switch
- Difficulty Switches
- Game Select Switch
- Game Reset Switch
- Color/B&W Switch
- Channel Selection

## Technical Legacy
### Hardware Innovations
- First microprocessor-based console with ROM cartridges
- Racing the beam display system
- Programmable graphics system
- Multiple controller types
- Bank switching techniques

### Software Development
- No development system initially
- Assembly language programming
- Cycle-exact timing required
- Innovative programming techniques:
  - Display kernel routines
  - Bank switching methods
  - Sprite multiplexing
  - Sound effects timing

### Market Impact
- Production Run: 1977-1992
- Units Sold: ~30 million
- Games Released: ~500 officially
- Price Points:
  - Launch: $199
  - Final: $49
- Competition:
  - Intellivision
  - ColecoVision
  - Odyssey²







## Audio System
### Audio Hardware
- Sound Channels: 2
- Sample Rate: N/A (direct frequency control)
- Bit Depth: 4-bit volume control
- Audio Memory: None (direct register control)

### Channel Types
- Two identical channels with:
  - Volume control (4-bit)
  - Frequency control
  - Tone control (noise/pure tone)
  - No envelope generator

### Timing
- Audio Update Rate: Can be updated any time
- DMA Features: None
- Interrupt Sources: None

## System Timing
### Interrupts
- Types Available: None (not connected on 6507)
- Sources: N/A
- Timing: All timing must be done through cycle counting
- Priority: N/A

### DMA
- Transfer Rates: N/A
- Available Modes: N/A
- Timing Constraints: N/A

## GameVM Implementation Notes
### Compiler Considerations
- Preferred Code Generation Strategy: Pure native code
- Register Allocation Strategy:
  - Heavy use of zero page
  - Minimize stack usage
  - Keep critical variables in registers
- Memory Management Strategy:
  - Static allocation only
  - Careful stack management
  - Bank switching for large programs
- Optimization Opportunities:
  - Cycle-exact code generation
  - Zero page optimization critical
  - Kernel-style video generation

### Performance Targets
- Minimum Frame Rate: 60 FPS (NTSC), 50 FPS (PAL)
- Audio Update Frequency: As needed
- Memory Budget:
  - RAM: 128 bytes total
  - Zero Page/Stack: 128 bytes shared
  - ROM: 2KB minimum, up to 64KB with banking
- Known Limitations:
  - No video buffer
  - Extremely limited RAM
  - Real-time video generation required

### Special Handling
- Bank Switching Implementation:
  - Must track current bank
  - Bank switch only during VBLANK
  - Consider ROM layout carefully
- Interrupt Management:
  - No hardware interrupts
  - All timing done through cycle counting
- Audio Mixing Strategy:
  - Direct TIA register updates
  - Volume mixing in software if needed
- Graphics Pipeline:
  - Scanline-based rendering
  - Cycle-exact sprite positioning
  - Color changes must be cycle-timed

## References
- [Stella Programmer's Guide](https://alienbill.com/2600/101/docs/stella.html)
- [6502 Reference](http://www.obelisk.me.uk/6502/reference.html)
- [TIA Hardware Notes](https://www.atarihq.com/danb/files/TIA_HW_Notes.txt)
- [2600 Programming Guide](https://www.randomterrain.com/atari-2600-memories-tutorial-andrew-davie-01.html)
- [AtariAge Development Forum](https://atariage.com/forums/forum/50-atari-2600-programming/)
