# Atari 7800 ProSystem

## System Overview
- Profile: GV.Spec.L2
- CPU: MOS Technology 6502C (Sally)
- Clock Speed: 1.79 MHz (NTSC), 1.66 MHz (PAL)
- Release Year: 1986 (NA), 1987 (EU)
- Generation: 3rd
- Region: North America, Europe
- Predecessor: Atari 5200
- Successor: Atari XEGS

## CPU Details
### Architecture Characteristics
- Instruction Set: MOS 6502 (with halt capability)
- Word Size: 8-bit
- Endianness: Little-endian
- Register Set:
  - General Purpose: A (accumulator), X, Y (index)
  - Special Purpose: SP (stack pointer), PC (program counter)
  - Status Flags: N (negative), V (overflow), B (break), D (decimal), I (interrupt), Z (zero), C (carry)
- Special Features:
  - HALT instruction for MARIA synchronization
  - No decimal mode (like NES)
  - Shared memory access with MARIA

### Memory Access
- Address Bus Width: 16 bits
- Data Bus Width: 8 bits
- Memory Page Size: 256 bytes
- Zero Page: Yes (first 256 bytes)
- DMA: Controlled by MARIA for graphics
- Memory Access Timing:
  - Read: 3 cycles
  - Write: 2 cycles
  - Read-Modify-Write: 5 cycles

### Performance Considerations
- Instruction Timing: 2-7 cycles per instruction
- MARIA Halt States: CPU halted during MARIA DMA
- Known Bottlenecks:
  - MARIA DMA interference
  - Limited RAM
  - Shared memory bus
- Optimization Opportunities:
  - Zero page usage
  - Strategic MARIA synchronization
  - Efficient bank switching

## Memory Map
### System Memory
- Total RAM: 4 KB main + 4 KB MARIA + 2 KB zero-page
- ROM: 4 KB BIOS
- Cartridge ROM: Up to 48 KB (expandable with banking)
- Special Memory:
  - TIA Audio: 32 bytes
  - MARIA registers: 32 bytes
  - 6532 RIOT: 128 bytes

### Memory Layout
- $0000-$001F: TIA registers (write only)
- $0020-$003F: MARIA registers
- $0040-$00FF: Zero page RAM
- $0100-$013F: 6532 RIOT I/O & Timer
- $0140-$01FF: 6532 RIOT RAM
- $0200-$17FF: System RAM
- $1800-$203F: MARIA RAM
- $2040-$20FF: High RAM
- $4000-$FF7F: Cartridge ROM space
- $FF80-$FFFF: BIOS ROM

## Video System
### MARIA Graphics Processor
- Chip: Custom MARIA (Mathematical And Rendering Intelligent Assistant)
- Clock Speed: 7.16 MHz (4× CPU clock)
- Display Processor: Object-based graphics system
- Access: Direct Memory Access (DMA)

### Display Modes
- 160-A Mode:
  - Resolution: 160×240
  - Colors per line: 25
  - Object width: 4-16 pixels
  - Best for: Arcade-style graphics
- 320-A Mode:
  - Resolution: 320×240
  - Colors per line: 25
  - Object width: 4-32 pixels
  - Best for: Detailed graphics
- 320-B Mode:
  - Resolution: 320×240/480 (interlaced)
  - Colors per line: 25
  - Object width: 4-32 pixels
  - Best for: High-resolution text
- 320-C Mode:
  - Resolution: 320×240/480 (interlaced)
  - Colors per line: 25
  - Object width: 4-32 pixels
  - Best for: Mixed graphics/text

### Graphics Capabilities
- Color Palette:
  - Total Colors: 256
  - Hues: 16
  - Luminances: 16
- Objects per Scanline: Up to 100
- Sprites per Scanline: Up to 30
- Object Sizes: 4 to 160 pixels wide
- Graphics Features:
  - Direct Memory Access (DMA)
  - Hardware collision detection
  - Multiple display lists
  - Dual playfield support
  - Character-mapped displays
  - Bitmap displays
  - Variable width objects
  - Header/Write VRAM modes

### Display Processing
- Display List Processing:
  - Multiple lists per frame
  - Dynamic list modification
  - Zone-based rendering
- DMA Characteristics:
  - Priority-based scheduling
  - Automatic CPU halting
  - Direct memory access
- Special Effects:
  - Hardware scrolling
  - Object multiplexing
  - Scanline interrupts
  - Palette manipulation

## Audio System
### TIA Audio (Television Interface Adapter)
- Chip: Custom Atari TIA (same as 2600)
- Channels: 2 independent channels
- Features per Channel:
  - Frequency Range: 30 Hz - 50 kHz
  - Volume Control: 4-bit (16 levels)
  - Waveforms: Multiple types
    - Pure tone
    - Square wave
    - Periodic noise
    - Random noise
  - Distortion control
  - Clock divider control

### POKEY Audio Support (Optional)
- Chip: POKEY (Pot Keyboard Integrated Circuit)
- Channels: 4 independent channels
- Features:
  - Frequency Range: 15 Hz - 64 kHz
  - Volume Control: 4-bit per channel
  - High-pass filters
  - 17-bit random number generator
  - Keyboard scanning
  - Potentiometer reading
  - Serial I/O support

## Input/Output System
### Controller Support
- Ports: Two DE-9 connectors
- Compatible Controllers:
  - Standard 2600-style joysticks
  - ProLine joysticks
  - Light gun
  - Driving controller
  - Keypad controller
  - Trak-Ball
- Features:
  - Auto-detection of controller type
  - Dual-button support on ProLine
  - Digital/analog input support

### Storage Interfaces
- Cartridge System:
  - ROM Size: Up to 48 KB standard
  - Bankswitching: Supported (larger games)
  - 2600 Compatibility: Full
  - Access Time: Immediate
- Expansion Port:
  - High-speed peripheral interface
  - Memory expansion capability
  - Additional audio support

### Video Output
- RF Output: Standard
- Composite Video: Available on some models
- Display Formats:
  - NTSC: 60 Hz
  - PAL: 50 Hz
- Resolution Support:
  - Standard: 160-320×240
  - Interlaced: Up to 320×480

## System Integration Features
### Backwards Compatibility
- Full Atari 2600 Support:
  - TIA chip compatibility
  - Memory mapping compatibility
  - Controller compatibility
  - CPU compatibility mode

### Hardware Features
- Built-in Games (some models):
  - Asteroids
  - Missile Command
  - Built-in BIOS
- System Controls:
  - Pause button
  - Reset switch
  - Difficulty switches
  - TV type switch

### Regional Differences
- No region lock
- TV Standard Adaptation:
  - NTSC: 60 Hz
  - PAL: 50 Hz
- Power Supply:
  - NA: 110V
  - EU: 220V

### Performance Features
- MARIA/CPU Synchronization
- Programmable interrupt system
- DMA-based graphics
- Multiple graphics modes
- Flexible memory management

## GameVM Implementation Notes
### Compiler Considerations
- **Tier Assignment**: **GV.Spec.L2**.
- **Rationale**: While the **MARIA** chip is highly capable for sprites/colors, it lacks dedicated hardware for tile-based smooth scrolling (requiring CPU-driven display list management). The base **TIA** audio is limited to 2 channels. 
- **Extensions**: Supports `Ext.Snd.Pokey` for L3-grade audio, as used in select historical cartridges.
- Preferred Code Generation Strategy: Native (6502)
- Register Allocation Strategy: Minimal (A, X, Y)
- Memory Management Strategy: Static 4 KB
- Optimization Opportunities: Use of MARIA's high sprite throughput
