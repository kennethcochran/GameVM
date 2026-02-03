# Entex Adventure Vision

## System Overview
- Profile: GV.Spec.L1
- Profile: GV.Spec.L1
- CPU: Intel 8048
- CPU Clock: 5.91 MHz
- Display: LED array with mechanical scanner
- Release Year: 1982
- Generation: 2nd
- Region: North America
- Predecessor: None
- Successor: None
- Notable Feature: Unique mechanical mirror-scanning LED display system

## CPU Details
### Intel 8048
#### Architecture Characteristics
- Processor: 8-bit microcontroller
- Word Size: 8-bit
- Endianness: Little-endian
- Register Set:
  - Accumulator (A)
  - Register Bank (R0-R7)
  - Program Counter
  - Status Word
  - Data Pointer
- Features:
  - Internal RAM
  - Built-in timer
  - Interrupt support
  - I/O ports

### System Architecture
- Display Controller
- Sound Generator
- Input Controller
- Memory Controller

## Memory Map
### System Memory
- Internal RAM: 64 bytes
- External RAM: 1KB
- ROM: 1KB (system)
- Cartridge ROM: 4KB

### Memory Layout
- $000-$3FF: System ROM
- $400-$7FF: Cartridge ROM
- $800-$BFF: External RAM
- Internal RAM:
  - 64 bytes (8048)
  - Register banks
  - Stack space

## Video System
### Display Hardware
- Type: LED array with mechanical scanner
- Resolution: 150×40 (effective)
- Colors: Monochrome (red)
- Refresh: 240 Hz

### Display Features
- Mechanical mirror drum
- LED array (150 elements)
- Persistence of vision
- Variable brightness

### Special Effects
- Brightness control
- Animation
- Motion effects
- Screen masking

## Audio System
### Sound Generation
- Type: Simple tone generator
- Channels: 1
- Features:
  - Square wave
  - Volume control
  - Frequency control
  - Mono output

### Audio Capabilities
- Single channel audio
- Basic sound effects
- Volume control
- Tone control
- Internal speaker

## Input/Output System
### Controller Interface
- Built-in controls
- Four action buttons
- Dual joysticks
- Menu controls

### Storage Interface
- Game cartridges:
  - 4KB ROM capacity
  - Simple connector
  - No save capability
- External:
  - Power input
  - Volume control

### Display Output
- Built-in LED display
- Mechanical scanner
- No external display
- Brightness control

## System Integration Features
### Hardware Variants
- Single model
- Development units
- No regional variants
- Prototype versions

### Regional Differences
- North America only
- Single power standard
- Single display type
- Universal cartridge

### Special Features
- Unique display system
- Portable design
- Built-in controls
- Battery operation

## Technical Legacy
### Hardware Innovations
- Mirror-scanning display
- LED array technology
- Mechanical solutions
- Portable design

### Software Development
- Development Tools:
  - Assembly language
  - Basic tools
  - Display utilities
  - Sound tools
- Programming Features:
  - Direct hardware control
  - Display timing
  - Sound generation
  - Input handling

### Market Impact
- Production Run: 1982-1983
- Units Sold: Unknown (rare)
- Games Released: 4
- Price Points:
  - Launch: $79.99
  - Final: $39.99
- Market Position:
  - Unique technology
  - Limited release
  - Experimental design
  - Collector's item

## GameVM Implementation Notes
### Compiler Considerations
- Code Generation:
  - 8048 instructions
  - Display timing
  - Memory management
  - I/O control
- Register Allocation:
  - Limited registers
  - Internal RAM
  - External RAM
- Memory Management:
  - Tight memory
  - Display buffer
  - Sound control
  - Input handling

### Performance Targets
- CPU: 5.91 MHz
- Display: 240 Hz refresh
- Audio: Single channel
- Memory Budget:
  - Internal RAM: 64 bytes
  - External RAM: 1KB
  - System ROM: 1KB
  - Cart ROM: 4KB

### Special Handling
- Display Timing
- Mirror Scanning
- LED Control
- Sound Generation
- Input Processing

## References
- [Adventure Vision Technical Manual](http://www.adventurevision.info)
- [Intel 8048 Datasheet](http://www.intel.com/8048)
- [Display System Documentation](http://www.ledscanning.org)
- [Game Development Guide](http://www.advdev.net)
