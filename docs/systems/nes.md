# Nintendo Entertainment System (NES/Famicom)

## System Overview
- CPU: Ricoh 2A03 (based on MOS 6502)
- Clock Speed: 1.79 MHz (NTSC), 1.66 MHz (PAL)
- Release Year: 1983 (JP), 1985 (NA), 1986 (EU)
- Generation: 3rd
- Region: Japan (Famicom), North America (NES), Europe (NES)
- Predecessor: Nintendo Color TV-Game
- Successor: Super Nintendo Entertainment System

## CPU Details
### Architecture Characteristics
- Instruction Set Family: MOS 6502 (without decimal mode)
- Word Size: 8-bit
- Endianness: Little-endian
- Register Set:
  - General Purpose: A (accumulator), X, Y (index)
  - Special Purpose: SP (stack pointer), PC (program counter)
  - Status Flags: N (negative), V (overflow), B (break), D (decimal, disabled), I (interrupt), Z (zero), C (carry)

### Memory Access
- Address Bus Width: 16 bits
- Data Bus Width: 8 bits
- Memory Page Size: 256 bytes
- Zero Page: Yes (first 256 bytes of memory)
- Special Addressing Modes: Zero page, Zero page indexed, Absolute, Absolute indexed, Indirect indexed, Indexed indirect
- DMA Capabilities: Single DMA channel for sprite data transfer (256 bytes)

### Performance Considerations
- Instruction Timing: 2-7 cycles per instruction
- Pipeline Features: None
- Known Bottlenecks:
  - Page boundary crossing (+1 cycle)
  - Limited sprite count per scanline
  - VRAM access only during VBLANK or HBLANK
- Optimization Opportunities:
  - Zero page addressing for faster memory access
  - Self-modifying code for indirect addressing
  - Cycle-counted loops for timing-critical code

## Memory Map
### RAM
- Total Size: 2KB internal RAM
- Layout:
  - $0000-$07FF: Internal RAM
  - $0800-$1FFF: RAM mirrors
- Bank Switching: None for main RAM
- Access Speed: 2 cycles
- Constraints: No wait states, always accessible

### ROM
- Cartridge Size: 16KB-4MB (with mapper)
- Bank Switching Capabilities: Varies by mapper
- Access Speed: 2 cycles
- Special Features:
  - Multiple mappers available (MMC1, MMC3, etc.)
  - PRG-ROM banking
  - CHR-ROM/RAM banking

### Special Memory Regions
- VRAM: 2KB for nametables (plus mirrors)
- Pattern Tables: 8KB (CHR-ROM or CHR-RAM)
- I/O Registers: $2000-$2007 (PPU), $4000-$4017 (APU and I/O)
- System Vectors:
  - $FFFA-$FFFB: NMI vector
  - $FFFC-$FFFD: Reset vector
  - $FFFE-$FFFF: IRQ/BRK vector

## Video System
### Display Characteristics
- Resolution: 256×240 (NTSC), 256×224 (PAL)
- Color Depth: 4-bit
- Refresh Rate: 60 Hz (NTSC), 50 Hz (PAL)
- Video RAM: 2KB nametable RAM, 256 bytes OAM

### Graphics Capabilities
- Sprite Support:
  - Max Sprites: 64 total, 8 per scanline
  - Sprite Size: 8×8 or 8×16 pixels
  - Colors per Sprite: 3 colors + transparency
  - Limitations: 8 sprites per scanline limit
- Background Layers:
  - Number of Layers: 1 background + 1 sprite layer
  - Tile Size: 8×8 pixels
  - Colors per Tile: 4 colors per palette
- Special Effects:
  - Scrolling (horizontal and vertical)
  - Split-screen effects (through mid-frame updates)
  - Sprite 0 hit detection

### Timing
- VBLANK Duration: 20 scanlines
- HBLANK Duration: 85.2 PPU cycles
- Access Windows: During VBLANK or HBLANK only

## Audio System
### Audio Hardware
- Sound Channels: 5
- Sample Rate: ~44.1 KHz (CPU clock / 40)
- Bit Depth: Variable by channel
- Audio Memory: None (direct register control)

### Channel Types
- Square Wave 1: Variable duty cycle, sweep, envelope
- Square Wave 2: Variable duty cycle, envelope
- Triangle Wave: Fixed volume, linear counter
- Noise: Pseudo-random generator, envelope
- DMC (Delta Modulation): 7-bit digital samples

### Timing
- Audio Update Rate: 240 Hz (quarter frame)
- DMA Features: DMC sample playback
- Interrupt Sources: IRQ from frame counter

## System Timing
### Interrupts
- Types Available: NMI, IRQ, BRK (software)
- Sources:
  - NMI: VBLANK
  - IRQ: Mapper, APU frame counter, DMC
- Timing: 
  - NMI: Every VBLANK (60 Hz NTSC, 50 Hz PAL)
  - IRQ: Varies by source
- Priority: NMI > IRQ > BRK

### DMA
- Transfer Rates: 256 bytes in 512 CPU cycles
- Available Modes: Sprite OAM DMA only
- Timing Constraints: CPU halted during DMA

## GameVM Implementation Notes
### Compiler Considerations
- Preferred Code Generation Strategy: Native code for performance-critical sections
- Register Allocation Strategy:
  - Prioritize zero page usage
  - Map frequently used variables to A, X, Y registers
- Memory Management Strategy:
  - Static allocation in zero page for critical variables
  - Bank switching for large programs
- Optimization Opportunities:
  - Zero page addressing modes
  - Self-modifying code for indirect access
  - Unrolled loops for timing-critical code

### Performance Targets
- Minimum Frame Rate: 60 FPS (NTSC), 50 FPS (PAL)
- Audio Update Frequency: 240 Hz
- Memory Budget:
  - RAM: 2KB total
  - Zero Page: 256 bytes
  - Stack: 256 bytes
- Known Limitations:
  - Sprite per scanline limit
  - VRAM access timing
  - Audio channel conflicts

### Special Handling
- Bank Switching Implementation:
  - Mapper-specific code generation
  - Bank tracking and switching overhead
- Interrupt Management:
  - NMI for frame timing
  - IRQ for audio and mapper events
- Audio Mixing Strategy:
  - Channel priority system
  - Volume mixing in software
- Graphics Pipeline:
  - Sprite multiplexing for >8 sprites per line
  - Background update during VBLANK
  - Split-screen effect support

## References
- [NESDev Wiki](http://wiki.nesdev.com/)
- [NESDoc](http://nesdev.com/NESDoc.pdf)
- [6502 Reference](http://www.obelisk.me.uk/6502/reference.html)
- [NesDev Forums](https://forums.nesdev.org/)
