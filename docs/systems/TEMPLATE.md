# [System Name]

## System Overview
- CPU: [CPU model and family]
- Clock Speed: [frequency]
- Release Year: [year]
- Generation: [2nd/3rd/4th/5th]
- Region: [regions where released]
- Predecessor: [previous system in family]
- Successor: [next system in family]

## CPU Details
### Architecture Characteristics
- Instruction Set Family: [e.g., 6502, Z80, 68000]
- Word Size: [8/16/32-bit]
- Endianness: [little/big]
- Register Set:
  - General Purpose: [list]
  - Special Purpose: [list]
  - Status Flags: [list]

### Memory Access
- Address Bus Width: [bits]
- Data Bus Width: [bits]
- Memory Page Size: [if applicable]
- Zero Page: [for 6502 family]
- Special Addressing Modes: [list]
- DMA Capabilities: [if available]

### Performance Considerations
- Instruction Timing: [cycles for common operations]
- Pipeline Features: [if any]
- Known Bottlenecks: [common performance issues]
- Optimization Opportunities: [special instructions or techniques]

## Memory Map
### RAM
- Total Size: [amount]
- Layout: [regions and their purposes]
- Bank Switching: [if applicable]
- Access Speed: [cycles]
- Constraints: [alignment, timing, etc.]

### ROM
- Cartridge Size: [typical and maximum]
- Bank Switching Capabilities: [if applicable]
- Access Speed: [cycles]
- Special Features: [mappers, etc.]

### Special Memory Regions
- VRAM: [if separate]
- Audio Memory: [if applicable]
- I/O Registers: [address ranges]
- System Vectors: [if applicable]

## Video System
### Display Characteristics
- Resolution: [pixels]
- Color Depth: [bits]
- Refresh Rate: [Hz]
- Video RAM: [amount and configuration]

### Graphics Capabilities
- Sprite Support:
  - Max Sprites: [number]
  - Sprite Size: [dimensions]
  - Colors per Sprite: [number]
  - Limitations: [per scanline, etc.]
- Background Layers:
  - Number of Layers: [count]
  - Tile Size: [dimensions]
  - Colors per Tile: [number]
- Special Effects: [hardware features]

### Timing
- VBLANK Duration: [lines/cycles]
- HBLANK Duration: [cycles]
- Access Windows: [when VRAM can be modified]

## Audio System
### Audio Hardware
- Sound Channels: [number and types]
- Sample Rate: [if applicable]
- Bit Depth: [if applicable]
- Audio Memory: [if separate]

### Channel Types
- Square Wave: [capabilities]
- Triangle Wave: [capabilities]
- Noise: [capabilities]
- Sample Playback: [if supported]
- Special Features: [FM, etc.]

### Timing
- Audio Update Rate: [frequency]
- DMA Features: [if available]
- Interrupt Sources: [if any]

## System Timing
### Interrupts
- Types Available: [NMI, IRQ, etc.]
- Sources: [what triggers them]
- Timing: [when they occur]
- Priority: [if multiple]

### DMA
- Transfer Rates: [if applicable]
- Available Modes: [list]
- Timing Constraints: [when usable]

## GameVM Implementation Notes
### Compiler Considerations
- Preferred Code Generation Strategy: [bytecode/native/hybrid]
- Register Allocation Strategy: [approach]
- Memory Management Strategy: [approach]
- Optimization Opportunities: [system-specific]

### Performance Targets
- Minimum Frame Rate: [fps]
- Audio Update Frequency: [Hz]
- Memory Budget: [breakdown]
- Known Limitations: [list]

### Special Handling
- Bank Switching Implementation: [if needed]
- Interrupt Management: [approach]
- Audio Mixing Strategy: [approach]
- Graphics Pipeline: [implementation]

## References
- [Technical Documentation]
- [Development Guides]
- [Hardware Specifications]
- [Programming Manuals]
