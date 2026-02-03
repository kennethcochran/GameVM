# Sega Genesis/Mega Drive

## System Overview
- Profile: GV.Spec.L4
- Main CPU: Motorola 68000
- Sound CPU: Zilog Z80
- Main CPU Clock: 7.67 MHz (PAL), 7.60 MHz (NTSC)
- Release Year: 1988 (JP), 1989 (NA), 1990 (EU)
- Generation: 4th
- Region: Worldwide
- Predecessor: Sega Master System
- Successor: Sega Saturn
- Notable Feature: Backward compatibility with Master System (with adapter)

## CPU Details
### Main CPU (68000)
#### Architecture Characteristics
- Processor: Motorola 68000
- Word Size: 16-bit (internally 32-bit)
- Endianness: Big-endian
- Register Set:
  - Data Registers (D0-D7): 32-bit
  - Address Registers (A0-A7): 32-bit
  - Program Counter (PC): 32-bit
  - Status Register (SR):
    - Trace Mode (T)
    - Supervisor State (S)
    - Interrupt Mask (I0-I2)
    - Extend (X)
    - Negative (N)
    - Zero (Z)
    - Overflow (V)
    - Carry (C)
- Special Features:
  - 32-bit internal architecture
  - 16 general-purpose registers
  - 14 addressing modes
  - Hardware multiply/divide
  - Supervisor/user modes

#### Memory Access
- Address Bus Width: 24 bits
- Data Bus Width: 16 bits
- Memory Access Timing:
  - Standard Access: 4 cycles
  - Word Access: 4 cycles
  - Long Word Access: 8 cycles
- Memory Types:
  - Fast ROM access
  - Word-aligned access
  - Byte-swapped access

### Sound CPU (Z80)
#### Architecture Characteristics
- Processor: Zilog Z80
- Clock Speed: 3.58 MHz
- Word Size: 8-bit
- Register Set:
  - Main/Alternate (A, B, C, D, E, H, L)
  - Index Registers (IX, IY)
  - Special Purpose (I, R, SP, PC)
  - Flags Register

#### Memory Access
- Address Space: 64 KB
- Access to:
  - Sound RAM (8 KB)
  - Sound chips (YM2612, PSG)
  - 68000 bus (when granted)

## Memory Map
### System Memory
- Main RAM: 64 KB
- Video RAM: 64 KB
- Sound RAM: 8 KB
- Boot ROM: 2 KB
- Cartridge ROM: Up to 4 MB (more with banking)

### Memory Layout
#### 68000 Address Space
- $000000-$3FFFFF: Cartridge ROM
- $400000-$7FFFFF: Reserved
- $800000-$9FFFFF: Reserved
- $A00000-$A1FFFF: Z80 and Sound Control
- $A10000-$A10FFF: I/O Control
- $C00000-$C0001F: VDP Control
- $E00000-$FFFFFF: RAM

#### Z80 Address Space
- $0000-$1FFF: Sound RAM
- $2000-$3FFF: Reserved
- $4000-$4003: YM2612
- $7F11: PSG
- $8000-$FFFF: Bank Control

## Video System
### Video Display Processor (VDP)
- Type: Custom Yamaha YM7101
- Resolution: 320×224 (NTSC), 320×240 (PAL)
- Colors: 512 (9-bit)
- Colors on Screen: 64 from 512
- Refresh Rate: 60 Hz (NTSC), 50 Hz (PAL)

### Graphics Capabilities
#### Sprites
- Number: 80 total, 20 per scanline
- Size: 8×8 to 32×32 pixels
- Colors: 16 per sprite from 512
- Features:
  - Priority levels
  - Flipping (H/V)
  - Size linking
  - Scaling/rotation (with software)

#### Background Planes
- Two scrollable planes
- 64×32 cells (512×256 pixels)
- Cell size: 8×8 pixels
- Colors: 16 per tile from 512
- Features:
  - Independent scrolling
  - Line-by-line scrolling
  - Column scrolling
  - Window plane

### Special Effects
- Shadow/Highlight
- Interlacing
- Hardware scrolling
- Raster effects
- Window clipping

## Audio System
### YM2612 FM Synthesizer
- Channels: 6 total
- Features per Channel:
  - 4 operators
  - Multiple waveforms
  - Envelope control
  - LFO modulation
  - Stereo output
- Special Features:
  - PCM channel
  - Digital stereo
  - DAC mode

### PSG (SN76489)
- Channels: 4 total
  - 3 square wave
  - 1 noise
- Features:
  - Volume control
  - Frequency control
  - Noise patterns

### Z80 Sound Control
- Direct sound chip access
- 8 KB sound RAM
- PCM playback control
- Sound driver execution

## Input/Output System
### Controller Interface
- Two DE-9 ports
- Standard Controller:
  - D-Pad
  - Start button
  - A, B, C buttons
- Six-Button Controller:
  - Additional X, Y, Z buttons
  - Mode button
- Support for:
  - Light gun
  - Mouse
  - Multi-tap

### Storage Interface
- Cartridge Slot:
  - ROM: Up to 4 MB (standard)
  - Save RAM: Optional
  - Bank switching support
  - Special chip support
- Expansion Port:
  - Sega CD
  - 32X
  - Power Base Converter

### Video Output
- RF Output
- Composite Video
- RGB (European models)
- Resolution Modes:
  - 256×224
  - 320×224
  - 256×240 (PAL)
  - 320×240 (PAL)

## System Integration Features
### Special Chips
- Sega Virtua Processor (SVP)
  - DSP for 3D calculations
  - Used in Virtua Racing
- Various mapper chips:
  - Super Street Fighter II
  - Pier Solar
  - Custom ROM/RAM configurations

### Add-on Systems
#### Sega CD/Mega CD
- Additional 68000 CPU
- CD-ROM drive
- PCM audio
- Scaling/rotation hardware
- Additional RAM

#### Sega 32X
- Two SH2 32-bit RISC processors
- Enhanced color capabilities
- Additional frame buffer
- PWM sound channels

### Regional Variants
- Japanese (Mega Drive)
  - Different cartridge shape
  - FM sound enabled by default
- North American (Genesis)
  - Lockout chip
  - RF shielding
- European (Mega Drive)
  - PAL video
  - RGB output
  - 50 Hz operation

## Technical Legacy
### Hardware Innovations
- First 16-bit console success in NA
- Backward compatibility
- Expandability
- Multiple sound processors

### Software Development
- Official dev kit
- Third-party tools
- Multiple programming levels:
  - 68000 assembly
  - Z80 sound programming
  - C development
  - Asset tools

### Market Impact
- Production Run: 1988-1997
- Units Sold: 30.75 million
- Games Released: Over 900
- Price Points:
  - Launch: US$189
  - Final: US$99

## GameVM Implementation Notes
### Compiler Considerations
- Preferred Code Generation Strategy:
  - 68000 native code
  - Z80 sound code
  - DMA optimization
  - VDP synchronization
- Register Allocation Strategy:
  - Data/Address register balance
  - Z80 register usage
  - Stack frame optimization
- Memory Management:
  - ROM banking
  - VDP memory access
  - Sound memory coordination

### Performance Targets
- Frame Rate: 60/50 Hz
- Audio Update: Per frame
- DMA Transfer: During blanking
- Memory Budget:
  - Main RAM: 64 KB
  - VRAM: 64 KB
  - Sound RAM: 8 KB
  - ROM: Up to 4 MB standard

### Special Handling
- Dual CPU Coordination
- VDP Access Timing
- Sound Generation
- Add-on Support
- Regional Differences

## References
- [Genesis Technical Manual](https://segaretro.org/images/a/a2/Genesis_Software_Manual.pdf)
- [68000 User Manual](https://www.nxp.com/docs/en/reference-manual/MC68000UM.pdf)
- [VDP Documentation](https://www.plutiedev.com/genesis-vdp)
- [YM2612 Documentation](https://www.smspower.org/maxim/Documents/YM2612)
