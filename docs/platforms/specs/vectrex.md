# Vectrex (MB Vector Display Gaming System)

## System Overview
- Profile: GV.Spec.L1
- CPU: Motorola 6809 @ 1.5 MHz
- Display: Samsung 240RB40 9-inch monochrome vector monitor
- Release Year: 1982
- Generation: 2nd
- Region: Worldwide
- Predecessor: None (GCE/MB's first console)
- Successor: None (cancelled Mini-Cade)
- Unique Feature: Built-in vector display

## CPU Details
### Architecture Characteristics
- Instruction Set: Motorola 6809
- Word Size: 8-bit with 16-bit operations
- Endianness: Big-endian
- Register Set:
  - Accumulators: A, B (8-bit), D (16-bit A:B)
  - Index Registers: X, Y (16-bit)
  - Stack Pointers: U (user), S (system)
  - Program Counter: PC (16-bit)
  - Direct Page: DP (8-bit)
  - Condition Code: CC
    - E (entire)
    - F (fast interrupt mask)
    - H (half carry)
    - I (interrupt mask)
    - N (negative)
    - Z (zero)
    - V (overflow)
    - C (carry)
- Special Features:
  - Position-independent code support
  - 16-bit arithmetic
  - Hardware multiplication
  - Stack indexing modes

### Memory Access
- Address Bus Width: 16 bits
- Data Bus Width: 8 bits
- Memory Page Size: 256 bytes
- Memory Access Timing:
  - Basic Memory Cycle: 2-5 cycles
  - Register Operations: 2 cycles
  - Extended Addressing: 5+ cycles
- DMA: Not available
- Vector Generation: Memory-mapped

### Performance Considerations
- Instruction Timing: 2-12 cycles
- Interrupt Types:
  - NMI (Non-Maskable)
  - IRQ (Standard)
  - FIRQ (Fast)
- Known Bottlenecks:
  - Vector drawing time
  - Display list processing
  - RAM limitations
- Optimization Opportunities:
  - Position-independent code
  - Fast interrupt usage
  - Register-based operations

## Memory Map
### System Memory
- RAM: 1 KB (1024 bytes)
- ROM: 8 KB (built-in MineStorm game)
- Cartridge ROM: Up to 32 KB
- Vector RAM: Shared with main RAM

### Memory Layout
- $0000-$03FF: System RAM (1 KB)
- $0400-$7FFF: Cartridge ROM
- $8000-$9FFF: Reserved
- $A000-$BFFF: Reserved
- $C000-$DFFF: Vector ROM
- $E000-$FFFF: System ROM
- Special Registers:
  - $F000: Vec_Music_Work
  - $F100: Vec_ADSR_Table
  - $F200: Vec_Shadow_Pattern
  - $F300: Vec_Music_Waveform
  - $F400: Vec_Dot_Pattern
  - $F500: Vec_Ramp

## Vector Display System
### Display Hardware
- Type: Samsung 240RB40 CRT
- Screen Size: 9-inch diagonal
- Phosphor: P31 green
- Beam Deflection: Electromagnetic
- Refresh Rate: 50 Hz
- Resolution: ~32,768 addressable points (theoretical)
- Practical Resolution: ~256×256 points
- Aspect Ratio: 1:1 (square display)

### Vector Generation
- Digital to Analog Converters:
  - Two 8-bit DACs for X/Y positioning
  - One 8-bit DAC for brightness control
- Vector Types:
  - Zero-Length (dots)
  - Relative vectors
  - Absolute vectors
- Drawing Speed: ~30,000 vectors/second
- Intensity Levels: 128 (7-bit)
- Vector Length: Variable (hardware limited)

### Graphics Capabilities
- Drawing Features:
  - Lines (vectors)
  - Points
  - Characters
  - Shapes
  - Rotation
  - Scaling
- Hardware Assist:
  - Zero detect circuitry
  - Beam intensity control
  - Vector timing control
  - Auto-centering
- Special Effects:
  - Intensity modulation
  - Vector rotation
  - Vector scaling
  - 3D perspective simulation

### Display Control
- Beam Position Control:
  - X position (8-bit)
  - Y position (8-bit)
  - Z (intensity) control
- Timing Control:
  - Vector generation timing
  - Beam settling time
  - Frame synchronization
- Hardware Registers:
  - Vector generator control
  - Position registers
  - Scale registers
  - Brightness control

## Audio System
## Input/Output System
### Controller Interface
- Number of Ports: 1 (plus 3 optional)
- Built-in Controller:
  - 4-way digital joystick
  - 4 action buttons
  - Self-centering mechanism
  - Digital potentiometer
- Optional Controllers:
  - Light Pen
  - 3D Imager glasses
  - Additional player controllers

### Storage Interface
- Cartridge Slot:
  - ROM Size: Up to 32 KB
  - Edge connector: 30-pin
  - Auto-detection
  - No bank switching
- Built-in Storage:
  - 8 KB ROM (MineStorm game)
  - No save capability

### Display Output
- Integrated Monitor:
  - Vector CRT display
  - Green phosphor
  - 9-inch diagonal
  - Built-in brightness control
  - Contrast control
- External Connections:
  - Audio output jack
  - Optional RGB mod points

### Power System
- Input Voltage: 120V AC (US), 220-240V AC (EU)
- Power Consumption: ~50W
- Internal Power Supply
- Voltage Regulators:
  - +5V digital
  - ±12V analog
  - High voltage for CRT

## System Integration Features
### Hardware Design
- All-in-one Unit:
  - Integrated vector display
  - Built-in controller
  - Internal power supply
  - Tilt stand
- Overlay System:
  - Plastic color overlays
  - Artwork for games
  - Screen protection
  - Color simulation

### Special Features
- Vector Graphics System:
  - Real-time rotation
  - Hardware scaling
  - Intensity control
  - 3D capabilities
- Built-in Game:
  - MineStorm (ROM)
  - Auto-boot capability
- 3D System Support:
  - 3D Imager peripheral
  - Mechanical color wheel
  - Synchronized shuttering

### Regional Differences
- NTSC (North America):
  - 120V AC power
  - 60 Hz refresh
- PAL (Europe):
  - 220-240V AC power
  - 50 Hz refresh
- Universal vector display timing

## Technical Legacy
### Hardware Innovations
- First home vector display system
- Integrated monitor design
- Real-time 3D capabilities
- Hardware rotation/scaling

### Software Development
- Development System:
  - 6809 assembly language
  - Vector generation tools
  - Sound programming tools
- Programming Features:
  - Vector drawing routines
  - Music macros
  - 3D mathematics
  - Display list management

### Market Impact
- Production Run: 1982-1984
- Units Sold: ~500,000
- Games Released: ~30 officially
- Price Points:
  - Launch: $199
  - Final: $100
- Competition:
  - Atari 2600
  - Intellivision
  - ColecoVision

### Programming Resources
- Built-in ROM Routines:
  - Vector generation
  - Character drawing
  - Sound generation
  - Controller reading
- Development Tools:
  - Vector list compiler
  - Music composition
  - Character set editor
  - 3D modeling tools







## Audio System (AY-3-8912)
### Audio Hardware
- Sound Channels: 3
- Sample Rate: N/A (tone generation)
- Bit Depth: 4-bit volume per channel
- Audio Memory: Direct register control

### Channel Types
- Three identical channels:
  - Frequency control
  - Volume control
  - Envelope control
  - Noise mixing

### Timing
- Audio Update Rate: Any time
- DMA Features: None
- Interrupt Sources: None

## System Timing
### Interrupts
- Types Available:
  - Frame interrupt (50/60 Hz)
  - VIA timer interrupts
- Sources:
  - Display refresh
  - Timer system
- Timing: Frame-based
- Priority: IRQ and FIRQ

### DMA
- Transfer Rates: N/A
- Available Modes: N/A
- Timing Constraints: N/A

## GameVM Implementation Notes
### Compiler Considerations
- Preferred Code Generation Strategy:
  - Vector-aware code
  - Efficient 6809 usage
  - Position-independent code
- Register Allocation Strategy:
  - Use 16-bit registers
  - Stack pointer optimization
  - Index register efficiency
- Memory Management Strategy:
  - Static allocation
  - Vector list management
  - Display list optimization
- Optimization Opportunities:
  - Vector path optimization
  - Display list sorting
  - Register usage patterns

### Performance Targets
- Minimum Frame Rate: 50/60 Hz
- Vector Count: System dependent
- Memory Budget:
  - RAM: 1KB
  - ROM: Up to 32KB
  - Vector List: RAM dependent
- Known Limitations:
  - Vector count per frame
  - RAM size
  - Drawing time constraints

### Special Handling
- Vector Generation:
  - Path optimization
  - Intensity control
  - Beam positioning
  - List management
- Display Management:
  - Frame timing
  - Vector counting
  - Display list sorting
  - Clipping implementation
- Memory Management:
  - Vector list allocation
  - RAM optimization
  - Stack management

## References
- [Vectrex Technical Manual](http://www.playvectrex.com/designit/chrissalo/vectrex1.pdf)
- [6809 Programming Manual](http://www.classiccmp.org/dunfield/r/6809prog.pdf)
- [Vectrex Hardware Documentation](http://www.playvectrex.com/designit/chrissalo/vectrex3.pdf)
- [AY-3-8912 Documentation](http://map.grauw.nl/resources/sound/generalinstrument_ay-3-8910.pdf)
- [Vectrex Programming Tutorial](http://vide.malban.de/documentation/vectrex-programming-tutorial)
