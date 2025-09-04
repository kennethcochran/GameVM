# Super Nintendo Entertainment System (SNES/Super Famicom)

## System Overview
- CPU: Ricoh 5A22 (based on 65816)
- CPU Clock: 2.68 MHz, 3.58 MHz (fast mode)
- Release Year: 1990 (JP), 1991 (NA), 1992 (EU)
- Generation: 4th
- Region: Worldwide
- Predecessor: Nintendo Entertainment System
- Successor: Nintendo 64
- Notable Feature: Mode 7 graphics, custom audio DSP

## CPU Details
### Architecture Characteristics
- Processor: Ricoh 5A22 (65C816-based)
- Word Size: 16-bit with 8-bit bus
- Endianness: Little-endian
- Register Set:
  - Accumulator (A): 16-bit/8-bit
  - Index X (X): 16-bit/8-bit
  - Index Y (Y): 16-bit/8-bit
  - Stack Pointer (S): 16-bit
  - Direct Page (D): 16-bit
  - Program Counter (PC): 16-bit
  - Program Bank (PB): 8-bit
  - Data Bank (DB): 8-bit
  - Status Register (P):
    - N (Negative)
    - V (Overflow)
    - M (Memory/Accumulator Select)
    - X (Index Register Select)
    - D (Decimal)
    - I (Interrupt)
    - Z (Zero)
    - C (Carry)
- Special Features:
  - 8-bit/16-bit switchable registers
  - Hardware multiplication/division
  - DMA controller
  - Memory mapping unit
  - Variable clock speeds

### Memory Access
- Address Bus Width: 24 bits
- Data Bus Width: 8 bits
- Memory Page Size: 256 bytes
- Memory Access Timing:
  - Fast ROM: 6 cycles
  - Slow ROM: 8 cycles
  - RAM: 6 cycles
  - VRAM: 8 cycles
- DMA Features:
  - 8 DMA channels
  - HDMA (scanline-based)
  - Memory to memory
  - Various transfer modes

### Performance Considerations
- Instruction Timing: 2-8 cycles
- Memory Speed Modes:
  - 2.68 MHz (normal)
  - 3.58 MHz (fast ROM)
- Known Bottlenecks:
  - 8-bit data bus
  - VRAM access restrictions
  - ROM access speed
- Optimization Opportunities:
  - DMA/HDMA usage
  - Fast ROM support
  - 16-bit register modes
  - Zero page addressing

## Memory Map
### System Memory
- Work RAM: 128 KB
- Video RAM: 64 KB
- Sprite RAM: 544 bytes
- Palette RAM: 512 bytes
- ROM: Up to 6 MB (with banking)
- Save RAM: Up to 32 KB

### Memory Layout
- $00-$3F,$80-$BF: First Mirror
  - $0000-$1FFF: Work RAM (lower 8KB mirror)
  - $2100-$213F: PPU registers
  - $2140-$217F: APU registers
  - $4000-$41FF: Old CPU registers
  - $4200-$44FF: New CPU registers
  - $6000-$7FFF: Expansion
  - $8000-$FFFF: ROM
- $40-$7D,$C0-$FF: ROM/RAM
- $7E-$7F: Work RAM (128 KB)

## Video System
### Picture Processing Unit (PPU)
#### PPU1 (S-PPU1)
- Background Processing
- Object Processing
- Color Math
- Window Processing

#### PPU2 (S-PPU2)
- Video Output
- Color Palette
- DAC

### Display Capabilities
- Resolution: 256×224/256×239 pixels
- Colors: 32,768 (15-bit)
- Colors per Tile: 256
- Simultaneous Colors: 256
- Sprites:
  - 128 sprites total
  - Up to 32 per line
  - Size: 8×8 to 64×64
  - 16 colors per sprite

### Background Modes
- Mode 0: 4 layers, 4 colors each
- Mode 1: 3 layers, 16/4/4 colors
- Mode 2: 2 layers, 16 colors + effects
- Mode 3: 2 layers, 256/16 colors
- Mode 4: 2 layers, 256/4 colors
- Mode 5: 2 layers, 16/4 colors (interlaced)
- Mode 6: 1 layer, 16 colors (interlaced)
- Mode 7: 1 rotating/scaling layer, 256 colors

### Special Features
- Mode 7:
  - Rotation
  - Scaling
  - Perspective effects
  - HDMA modifications
- Color Math:
  - Addition/subtraction
  - Fixed color
  - Window masking
- Transparency:
  - Per-pixel blending
  - Color math effects
  - Window clipping
- Mosaic Effects:
  - 1×1 to 16×16 pixels
  - Independent per layer

## Audio System
### Sony S-SMP Audio Processing Unit
- 8-bit CPU (Sony SPC700)
- 64 KB RAM
- 16-bit stereo output
- 32 KHz sample rate

### Sound DSP
- 8 channels
- 16-bit sample playback
- ADSR envelope control
- Echo/FIR filter effects
- Noise generation
- Pitch modulation

### Channel Features
- Sample Playback:
  - 16-bit samples
  - Variable rate
  - Loop points
  - Reverse playback
- ADSR Envelope:
  - Attack rate
  - Decay rate
  - Sustain level
  - Release rate
- Effects:
  - Echo (FIR filter)
  - Pitch modulation
  - Noise mixing
  - Channel mixing

### Audio Memory
- 64 KB sound RAM
- Sample storage
- Program code
- Echo buffer
- Sound variables

## Input/Output System
### Controller Interface
- Two 7-pin ports
- Controller Features:
  - D-Pad
  - Start/Select
  - A/B/X/Y buttons
  - L/R shoulder buttons
- Multitap Support:
  - Up to 5 players
  - Compatible with most games

### Expansion Port
- Multi-player adapters
- Specialty controllers
- Development tools
- Region adapters

### Storage Interface
- Game Pak ROM:
  - Size: 256 KB to 6 MB
  - Access Time: 200ns/270ns
  - Bank Switching
  - Special chips support
- Save RAM:
  - Battery backed
  - Size: 2 KB to 32 KB
  - Persistent storage

## System Integration Features
### Special Chips
- Super FX:
  - 3D polygon rendering
  - Texture mapping
  - Memory management
  - Clock: 10.74 MHz/21.48 MHz
- SA-1:
  - Fast ROM access
  - Memory mapping
  - DMA control
  - Math functions
- DSP Series:
  - DSP-1: 3D calculations
  - DSP-2: Sprite scaling
  - DSP-3: Data compression
  - DSP-4: Sprite positioning
- Others:
  - CX4: 3D math
  - S-DD1: Data decompression
  - SPC7110: Data decompression/RTC
  - ST018: ARM-based coprocessor

### Regional Variants
- NTSC (JP/NA):
  - 60 Hz refresh
  - 256×224 resolution
  - CPU: 3.58 MHz
- PAL (EU/AU):
  - 50 Hz refresh
  - 256×239 resolution
  - CPU: 3.55 MHz

### Hardware Revisions
- Original Model (1990)
- New-Style SNES (1997)
- Regional variations

## Technical Legacy
### Hardware Innovations
- Mode 7 graphics
- Custom sound DSP
- Enhancement chips
- 16-bit architecture

### Software Development
- Development Tools:
  - Official dev kit
  - Various enhancement chips
  - Debugging hardware
- Programming Features:
  - Assembly/C development
  - Multiple memory models
  - DMA optimization
  - Mode 7 effects

### Market Impact
- Production Run: 1990-2003
- Units Sold: 49.1 million
- Games Released: 1,757
- Price Points:
  - Launch: ¥25,000/US$199
  - Final: US$99

## GameVM Implementation Notes
### Compiler Considerations
- Preferred Code Generation Strategy:
  - 65816 native code
  - Memory model awareness
  - DMA optimization
  - Enhancement chip support
- Register Allocation Strategy:
  - 8/16-bit register modes
  - Index optimization
  - Zero page usage
  - Stack relative addressing
- Memory Management:
  - Bank switching
  - DMA coordination
  - VRAM access timing
  - Enhancement chip memory

### Performance Targets
- Frame Rate: 60/50 Hz
- Audio Rate: 32 KHz
- DMA Transfer: During HBLANK/VBLANK
- Memory Budget:
  - WRAM: 128 KB
  - VRAM: 64 KB
  - Audio RAM: 64 KB
  - Save RAM: Up to 32 KB

### Special Handling
- Mode 7 Effects
- Enhancement Chips
- DMA/HDMA Timing
- Audio Processing
- Regional Differences

## References
- [SNES Development Manual](https://www.nintendo.co.jp/support/manual/pdf/SNESDevManual.pdf)
- [65816 Programming Manual](http://6502.org/documents/datasheets/wdc/wdc_65816_programming_manual.pdf)
- [SNES Hardware Specifications](https://problemkaputt.de/fullsnes.htm)
- [Audio Development Guide](https://www.romhacking.net/documents/191/)
