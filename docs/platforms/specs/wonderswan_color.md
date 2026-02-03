# Bandai WonderSwan Color

## System Overview
- Profile: GV.Spec.L5
- CPU: SPGY-1002 (NEC V30MZ 16-bit RISC-style clone)
- Clock Speed: 3.072 MHz
- Release Year: 2000
- Generation: 5th/6th (Handheld)
- Region: Japan
- Predecessor: Bandai WonderSwan (Monochrome)
- Successor: Bandai SwanCrystal

## CPU Details
### Architecture Characteristics
- Instruction Set Family: x86 (80186 compatible)
- Word Size: 16-bit
- Endianness: Little
- Register Set:
  - General Purpose: AX, BX, CX, DX
  - Index/Pointers: SI, DI, BP, SP
  - Segments: CS, DS, ES, SS
  - Flags: Standard x86 flags (Z, S, C, etc.)

### Memory Access
- Address Bus Width: 20-bit (segmented)
- Data Bus Width: 16-bit
- Memory Page Size: N/A (Segmented architecture)
- Zero Page: N/A
- Special Addressing Modes: Segment:Offset
- DMA Capabilities: Yes (General purpose DMA for block moves)

### Performance Considerations
- Instruction Timing: Improved over standard x86; many instructions in 1-2 cycles
- Pipeline Features: None (Hardwired logic)
- Known Bottlenecks: Memory speed and bus contention
- Optimization Opportunities: 16-bit wide data access; bitwise manipulation

## Memory Map
### RAM
- Total Size: 64 KB (Unified VRAM/WRAM)
- Layout: 0x00000-0x0FFFF
- Bank Switching: No (full 64KB accessible via DS/ES)
- Access Speed: 1 cycle
- Constraints: Shared by CPU, Graphics, and Audio

### ROM
- Total Size: Cartridges up to 128 MB
- Bank Switching Capabilities: Mapped into memory space via hardware registers
- Access Speed: 1 cycle
- Special Features: N/A

### Special Memory Regions
- VRAM: Segment of the 64KB RAM (Programmable mapping)
- Audio Memory: Integrated in 64KB RAM
- I/O Registers: 0x0000-0x00FF (I/O space)
- System Vectors: 0xFFFF0 (Reset)

## Video System
### Display Characteristics
- Resolution: 224x144 pixels
- Color Depth: 12-bit (4096 colors)
- Refresh Rate: 75 Hz
- Video RAM: Part of shared 64KB

### Graphics Capabilities
- Sprite Support: Yes
  - Max Sprites: 128
  - Sprite Size: 8x8 pixels
  - Colors per Sprite: 16 (15 + transparent)
  - Limitations: 28-32 sprites per scanline
- Background Layers: 2
  - Number of Layers: 2 (BG0, BG1)
  - Tile Size: 8x8 pixels
  - Colors per Tile: 16 from sub-palettes
- Special Effects: Hardware scrolling, Windowing (BG clip/masks), Rotation (BG2/Sprite rotation support in some modes)

### Timing
- VBLANK Duration: approx. 1 ms
- HBLANK Duration: Standard
- Access Windows: Unrestricted (Unified memory)

## Audio System
### Audio Hardware
- Sound Channels: 4 (Digital PCM)
- Sample Rate: Variable (4-bit or 8-bit samples)
- Bit Depth: 4-bit/8-bit
- Audio Memory: Shared

### Channel Types
- Square Wave: 4 channels
- Noise: Integrated in channel 4
- Sample Playback: Yes (Wavetable memory support)
- Special Features: Volume envelope, Stereo output

## System Timing
### Interrupts
- Types Available: Hardware interrupts (VBLANK, HBLANK, Serial, Key, etc.)
- Sources: 8 sources
- Timing: Programmable
- Priority: Fixed

## GameVM Implementation Notes
### Compiler Considerations
- Preferred Code Generation Strategy: Native (x86-style)
- Register Allocation Strategy: Use SI/DI/BP for intermediate pointers
- Memory Management Strategy: Shared 64KB mapping
- Optimization Opportunities: Segmented memory tricks for fast context switching

### Performance Targets
- Minimum Frame Rate: 60-75 fps
- Audio Update Frequency: 75 Hz
- Memory Budget: 64 KB total
- Known Limitations: Non-backlit screen makes high-speed motion blurred

## References
- WonderSwan Color Hardware Specification (isie.pl)
- Bandai SPGY-1002 Technical Manual
- NEC V30MZ Programmer's Reference
