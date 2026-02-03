# Nintendo 64

## System Overview
- Profile: GV.Spec.L7
- CPU: NEC VR4300 (MIPS R4300i)
- CPU Clock: 93.75 MHz
- GPU: SGI Reality Co-Processor (RCP)
- Release Year: 1996
- Generation: 5th
- Region: Worldwide
- Predecessor: Super Nintendo Entertainment System
- Successor: GameCube
- Notable Feature: First 64-bit gaming console with built-in 3D graphics capabilities

## CPU Details
### VR4300 (MIPS R4300i)
#### Architecture Characteristics
- Processor: 64-bit RISC MIPS III
- Word Size: 64-bit internal, 32-bit external bus
- Endianness: Big-endian
- Register Set:
  - 32 64-bit General Purpose Registers
  - 32 64-bit Floating Point Registers
  - System Control Registers
  - Program Counter (64-bit)
- Features:
  - 5-stage pipeline
  - Superscalar execution
  - Branch prediction
  - Virtual memory support

### Reality Co-Processor (RCP)
#### Reality Signal Processor (RSP)
- MIPS R4000 based vector unit
- 8 vector registers
- Vector operations
- Audio processing
- Custom microcode

#### Reality Display Processor (RDP)
- Texture mapping
- Z-buffering
- Anti-aliasing
- Frame buffer operations

## Memory Map
### System Memory
- Main RAM: 4MB (expandable to 8MB)
- RDRAM Type: Rambus
- ROM: Up to 64MB (cartridge)
- Cache:
  - L1 I-Cache: 16KB
  - L1 D-Cache: 8KB
  - RCP Cache: 4KB

### Memory Layout
- $00000000-$003FFFFF: Main RAM
- $04000000-$04003FFF: RSP DMEM
- $04004000-$04007FFF: RSP IMEM
- $10000000-$1FBFFFFF: Cartridge Domain 1
- $1FC00000-$1FC007FF: PIF Boot ROM
- $80000000-$FFFFFFFF: Virtual Memory Map

## Video System
### Graphics Features
- Resolution: 240p/288p to 480i
- Colors: 16.7M (32-bit)
- Fillrate: 62.5M pixels/sec
- Anti-aliasing: MSAA

### Display Features
- Resolutions:
  - 320×240
  - 640×240
  - 256×240
  - 512×240
- Color Depths:
  - 16-bit (32K colors)
  - 32-bit (16.7M colors)
- Features:
  - Perspective correction
  - Mip-mapping
  - Bilinear filtering
  - Z-buffering

### Special Effects
- Texture mapping
- Environment mapping
- Trilinear filtering
- Alpha blending
- Fog
- Display lists

## Audio System
### Audio Processing
- Sample Rate: Up to 44.1 kHz
- Channels: 16-24 (programmable)
- Features:
  - ADPCM compression
  - Wavetable synthesis
  - 3D positional audio
  - Real-time effects

### Audio Capabilities
- Sample playback
- MIDI synthesis
- Positional audio
- Stereo output
- Digital filtering
- Sound effects

## Input/Output System
### Controller Interface
- Four controller ports
- Memory card slot (Controller Pak)
- Rumble Pak support
- Transfer Pak support

### Storage Interface
- Game cartridges:
  - Up to 64MB
  - EEPROM/SRAM/Flash
  - Save states
- Controller Pak:
  - 256Kb storage
  - Game saves
  - Ghost data

### Video Output
- Composite video
- S-Video
- RF output
- RGB (some models)

## System Integration Features
### Hardware Variants
- Basic model
- Expansion Pak model
- iQue Player (China)
- Development units

### Regional Differences
- TV standards (NTSC/PAL)
- Cartridge shape
- Case design
- Game library

### Special Features
- Expansion Pak (4MB RAM)
- Controller accessories
- 64DD (Japan only)
- Development tools

## Technical Legacy
### Hardware Innovations
- 64-bit architecture
- Built-in 3D capabilities
- Unified memory architecture
- Custom vector processor

### Software Development
- Development Tools:
  - Nintendo 64 SDK
  - SGI Workstations
  - Partner-N64
  - NuSystem
- Programming Features:
  - Microcode
  - Display lists
  - RSP programming
  - Audio synthesis

### Market Impact
- Production Run: 1996-2002
- Units Sold: 32.93 million
- Games Released: 388
- Price Points:
  - Launch: $199
  - Final: $99
- Market Position:
  - 3D gaming pioneer
  - Cartridge-based
  - Strong first-party
  - Limited third-party

## GameVM Implementation Notes
### Compiler Considerations
- Code Generation:
  - MIPS III instructions
  - RSP vector operations
  - RDP commands
  - Display lists
- Register Allocation:
  - 64-bit registers
  - Vector registers
  - Coprocessor registers
- Memory Management:
  - RDRAM access
  - Cache control
  - DMA operations

### Performance Targets
- CPU: 93.75 MHz
- RCP: 62.5M pixels/sec
- Audio: 44.1 kHz
- Memory Budget:
  - Main RAM: 4MB/8MB
  - RSP DMEM: 4KB
  - RSP IMEM: 4KB
  - RDP Cache: 4KB

### Special Handling
- RSP Microcode
- RDP Commands
- Memory Access
- Audio Processing
- Controller Input

## References
- [N64 Programming Manual](http://n64dev.org/docs)
- [RCP Architecture Guide](http://n64.icequake.net/doc/n64intro)
- [VR4300 Technical Reference](http://datasheets.chipdb.org/NEC/VR4300)
- [Nintendo 64 Development Tools](http://ultra64.ca/files/documentation)
