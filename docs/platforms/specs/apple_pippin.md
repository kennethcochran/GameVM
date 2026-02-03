# Apple Pippin

## System Overview
- Profile: GV.Spec.L6
- CPU: PowerPC 603
- CPU Clock: 66 MHz
- GPU: Custom video controller
- Release Year: 1996
- Generation: 5th
- Region: Japan, USA
- Predecessor: None (Apple's first console)
- Successor: None
- Notable Feature: Mac OS-based gaming platform

## CPU Details
### PowerPC 603
#### Architecture Characteristics
- Processor: 32-bit RISC
- Word Size: 32-bit
- Endianness: Big-endian
- Register Set:
  - 32 General Purpose Registers (32-bit)
  - 32 Floating Point Registers (64-bit)
  - Special Purpose Registers
  - Machine State Register
- Features:
  - Superscalar execution
  - Branch prediction
  - Separate caches
  - Power management

### System Architecture
- Memory Management Unit
- Floating Point Unit
- Branch Processing Unit
- System Interface Unit

## Memory Map
### System Memory
- Main RAM: 6MB
- Video RAM: 2MB (shared)
- ROM: 4MB (system)
- Cache:
  - L1 Data: 16KB
  - L1 Instruction: 16KB

### Memory Layout
- ROM space
- RAM space
- PCI device space
- I/O space
- System registers

## Video System
### Graphics Processing
- Resolution: 640×480
- Colors: 16.7M (24-bit)
- Refresh Rate: 60 Hz
- Features:
  - QuickDraw acceleration
  - Hardware scaling
  - Video overlay
  - Multiple buffers

### Display Features
- Resolution Modes:
  - 640×480
  - 512×384
  - 320×240
- Color Depths:
  - 24-bit (16.7M colors)
  - 16-bit (65,536 colors)
  - 8-bit (256 colors)
- Features:
  - Double buffering
  - Hardware scaling
  - QuickDraw support
  - Video playback

### Special Effects
- Hardware scaling
- Alpha blending
- Video overlay
- Color effects
- QuickTime support

## Audio System
### Sound Processing
- Sample Rate: 44.1 kHz
- Channels: 16-bit stereo
- Features:
  - CD-quality audio
  - MIDI support
  - Digital effects
  - Sound Manager

### Audio Capabilities
- CD audio playback
- Digital audio
- MIDI synthesis
- Real-time effects
- Multiple channels
- Volume control

## Input/Output System
### Controller Interface
- ADB (Apple Desktop Bus) ports
- AppleJack controller port
- Keyboard support
- Mouse support

### Storage Interface
- CD-ROM Drive:
  - 4x speed
  - 650MB capacity
  - PhotoCD support
  - Audio CD support
- Optional:
  - Floppy drive
  - Hard drive
  - Memory modules

### Video Output
- Composite video
- S-Video
- VGA output
- RF output (some models)

## System Integration Features
### Hardware Variants
- Bandai Pippin ATMARK
- Bandai Pippin @WORLD
- Development units
- Prototypes

### Regional Differences
- TV standards (NTSC/PAL)
- Power supply
- Case design
- ROM versions

### Special Features
- Mac OS based
- Internet capable
- Expansion dock
- GeoPort support

## Technical Legacy
### Hardware Innovations
- PowerPC architecture
- Mac OS integration
- Network support
- Multimedia features

### Software Development
- Development Tools:
  - CodeWarrior
  - MPW
  - QuickDraw
  - Sound Manager
- Programming Features:
  - Mac Toolbox
  - PowerPC native code
  - QuickTime support
  - Networking APIs

### Market Impact
- Production Run: 1996-1997
- Units Sold: ~42,000
- Games Released: ~80
- Price Points:
  - Launch: $599
  - Final: $299
- Market Position:
  - High-end system
  - Network computer
  - Limited success
  - Mac OS based

## GameVM Implementation Notes
### Compiler Considerations
- Code Generation:
  - PowerPC instructions
  - Mac Toolbox calls
  - Resource management
  - Memory model
- Register Allocation:
  - PowerPC registers
  - FPU registers
  - System resources
- Memory Management:
  - Virtual memory
  - Resource management
  - Handle-based memory

### Performance Targets
- CPU: 66 MHz PowerPC
- Graphics: 640×480
- Audio: CD quality
- Memory Budget:
  - Main RAM: 6MB
  - Video RAM: 2MB
  - ROM: 4MB
  - Cache: 32KB total

### Special Handling
- Mac OS Integration
- PowerPC Architecture
- Resource Management
- Network Support
- CD-ROM Access

## References
- [Pippin Technical Specifications](http://www.apple-history.com/pippin)
- [PowerPC 603 Reference Manual](http://www.ibm.com/chips/powerpc)
- [Mac OS Development Guide](http://www.apple.com/legacy)
- [Pippin SDK Documentation](http://www.pippindev.org)
