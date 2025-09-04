# Architecture Overview

## Core Components

GameVM uses a flexible, multi-level architecture enabling efficient game development across diverse gaming platforms:

1. **Compiler Pipeline**: Transforms high-level code into optimized platform-specific code
2. **Hardware Abstraction Layer**: Provides unified access to diverse gaming hardware
3. **Runtime System**: Handles execution, memory management, and platform-specific optimizations

## Hardware Abstraction Layer

GameVM's HAL is designed specifically for retro gaming hardware, balancing abstraction with performance:

### Tiered Abstraction Levels
- High-level portable APIs for cross-platform development
- Mid-level APIs exposing platform-specific optimizations
- Low-level direct hardware access when needed

### Core Subsystems

#### Video
- Abstracts various video chips (TIA, PPU, VDP) with common primitives
- Sprite management
- Tile-based backgrounds
- Hardware scrolling
- Color palette management

#### Audio
- Unified interface for different sound hardware
- Pulse/square wave generation
- Frequency modulation
- Sample playback (where supported)

#### Input
- Normalizes different controller types
- Digital pad mapping
- Analog input scaling
- Multi-player support

#### Memory
- Smart memory management
- Bank switching support
- Memory-mapped I/O handling
- Zero-page optimization for 6502

## Interoperability

GameVM provides seamless interoperability between bytecode and native machine code:

### Zero-Cost Bridging
- Direct function calls between bytecode and machine code
- No marshalling overhead for primitive types
- Efficient object representation sharing
- Register-aware calling conventions

### Runtime Memory Management
- Unified memory model across bytecode and native code
- Automatic stack frame alignment
- Shared heap management
- Zero-copy data access where possible
