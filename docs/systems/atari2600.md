# Atari Video Computer System (VCS/2600)

## System Overview
- CPU: MOS 6507 (variant of 6502)
- Clock Speed: 1.19 MHz (NTSC), 1.182097 MHz (PAL)
- Release Year: 1977
- Generation: 2nd
- Region: Worldwide
- Predecessor: Atari Pong consoles
- Successor: Atari 5200

## CPU Details
### Architecture Characteristics
- Instruction Set Family: MOS 6502
- Word Size: 8-bit
- Endianness: Little-endian
- Register Set:
  - General Purpose: A (accumulator), X, Y (index)
  - Special Purpose: SP (stack pointer), PC (program counter)
  - Status Flags: N (negative), V (overflow), B (break), D (decimal), I (interrupt), Z (zero), C (carry)
- Notable Limitations:
  - 13-bit address bus (vs 6502's 16-bit)
  - No interrupts (IRQ and NMI pins not connected)
  - Limited stack space (128 bytes)

### Memory Access
- Address Bus Width: 13 bits
- Data Bus Width: 8 bits
- Memory Page Size: 256 bytes
- Zero Page: Yes (crucial for performance)
- Special Addressing Modes: Same as 6502 (zero page modes critical)
- DMA Capabilities: None (all video/audio timing done by CPU)

### Performance Considerations
- Instruction Timing: 2-7 cycles per instruction
- Pipeline Features: None
- Known Bottlenecks:
  - No video buffer (must generate scan lines in real-time)
  - CPU shares cycles with TIA for memory access
  - Limited ROM space requires frequent bank switching
- Optimization Opportunities:
  - Zero page usage critical for performance
  - Cycle-counted code for video timing
  - Careful use of vertical blank for processing

## Memory Map
### RAM
- Total Size: 128 bytes
- Layout:
  - $0080-$00FF: System RAM
  - Zero Page: $0080-$00FF (shares space with stack)
  - Stack: $0080-$00FF (128 bytes, shared with zero page)
- Bank Switching: None for RAM
- Access Speed: 3 cycles for write, 2 cycles for read
- Constraints:
  - Extremely limited space
  - Stack shares space with variables
  - No memory-mapped I/O besides TIA/RIOT

### ROM
- Cartridge Size: 2KB-64KB (with bank switching)
- Bank Switching Capabilities:
  - Multiple schemes available (F8, F6, F4, etc.)
  - Most common: F8 (8KB, 2 banks)
  - Advanced: F4 (32KB, 8 banks)
- Access Speed: 3 cycles
- Special Features:
  - ROM must start at $F000
  - Various bank switching schemes
  - Some cartridges include RAM

### Special Memory Regions
- TIA Registers: $00-$7F
- PIA/RIOT Registers: $280-$297
- System Vectors:
  - $FFFC-$FFFD: Reset vector
  - No IRQ/NMI vectors (not connected)

## Video System
### Display Characteristics
- Resolution: 160×192 (NTSC), 160×228 (PAL)
- Color Depth: 4-bit (16 colors)
- Refresh Rate: 60 Hz (NTSC), 50 Hz (PAL)
- Video RAM: None (real-time generation)

### Graphics Capabilities
- Sprite Support:
  - Max Sprites: 2 (player), 2 (missiles), 1 (ball)
  - Sprite Size: 8-bit width (players), 1-bit (missiles/ball)
  - Colors per Sprite: 1 color per sprite
  - Limitations: Must be manually positioned
- Background:
  - 40-bit playfield register
  - Mirror or repeat modes
  - 1 color at a time (can change per scanline)
- Special Effects:
  - Player reflection/delay
  - HMOVE fine positioning
  - Color changes per scanline
  - Sprite stretching

### Timing
- VBLANK Duration: 37 scanlines
- HBLANK Duration: 68 color clocks
- Access Windows: Entire frame must be generated in real-time

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
