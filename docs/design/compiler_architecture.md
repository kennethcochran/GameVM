---
title: "Compiler Architecture"
description: "Architecture and design of the GameVM multi-language compiler"
author: "GameVM Team"
created: "2025-09-20"
updated: "2025-09-20"
version: "1.0.0"
---

# Compiler Architecture

## Overview

The GameVM compiler is a multi-language compiler that produces ROM images for various retro gaming consoles. It handles the entire compilation pipeline from source code to final ROM image, supporting multiple input languages and dispatch techniques.

## Key Requirements

### Multi-Language Support
- Language selection via file extension
- Common IR representation for all supported languages
- Language-specific frontends implementing the `ILanguageFrontend` interface
- Initial language support:
  - Python
  - Pascal
  - (Additional languages to be added)

### Target Platform Support
- Compiler switch to specify target console generation (2nd-5th generation)
- Generation-specific optimizations for each console family
- ROM image generation following platform specifications
- Memory map and hardware register management
- Support for generation-specific features:
  - 2nd Gen: Limited memory, simple graphics
  - 3rd Gen: Improved graphics, sound chips
  - 4th Gen: Advanced graphics, more memory
  - 5th Gen: 3D capabilities, CD-based media

### Code Generation Options

#### Dispatch Techniques
The compiler supports multiple dispatch techniques, each with different performance and size characteristics. The technique can be selected via compiler switch:

1. **AOT Compilation (Pure Machine Code)**
   - Generates native machine code for the target platform
   - No interpreter overhead - maximum performance
   - Larger code size
   - No runtime interpretation
   - Best for: Performance-critical code on platforms with sufficient ROM

2. **Direct Threaded Code (DTC)**
   - Each opcode is replaced with the address of its implementation
   - Fast dispatch using indirect jumps
   - Good balance of performance and code size
   - Well-suited for retro hardware with simple branch prediction
   - Best for: 8-bit and 16-bit systems with limited memory

3. **Indirect Threaded Code (ITC)**
   - Uses a table of function pointers for opcode dispatch
   - More compact than DTC
   - Slightly slower dispatch than DTC
   - Better code density
   - Best for: Systems with limited ROM but some CPU headroom

4. **Subroutine Threaded Code (STC)**
   - Each opcode is implemented as a subroutine
   - Uses standard call/return instructions
   - Better performance on modern CPUs with branch prediction
   - Larger code size due to call/return overhead
   - Best for: Modern emulation on powerful hardware

5. **Token Threaded Code (TTC)**
   - Most compact representation
   - Simple bytecode interpreter
   - Each opcode is a token (byte or word)
   - Slower execution but minimal memory footprint
   - Best for: Very memory-constrained systems

#### Custom Interpreter Generation
For all interpreted dispatch techniques (TTC, ITC, DTC, STC), the compiler generates a custom interpreter tailored to the specific program:

- **Dead Code Elimination**: Aggressive removal of unused opcode handlers
- **Superinstructions**: Automatic generation of combined instructions for common sequences
- **Specialized Variants**: Creation of specialized instruction variants based on usage patterns
- **Profile-Guided Optimization**: Optional runtime profiling to guide optimization

#### Performance Characteristics

| Technique | Code Size | Speed | Memory Usage | Best For |
|-----------|-----------|-------|--------------|-----------|
| AOT       | Large     | ★★★★★ | Medium       | Performance-critical code |
| DTC       | Medium    | ★★★★☆ | Medium       | 8/16-bit systems |
| ITC       | Small     | ★★★☆☆ | Low          | ROM-constrained systems |
| STC       | Large     | ★★★★☆ | Medium       | Modern emulation |
| TTC       | Smallest  | ★★☆☆☆ | Lowest       | Memory-constrained systems |

#### Platform-Specific Optimization
- Each dispatch technique has platform-specific optimizations
- Memory layout optimized for the target hardware
- Special considerations for:
  - Cache line alignment
  - Branch prediction hints
  - Pipeline optimization
  - Memory access patterns

## Compilation Pipeline

### 1. Frontend Phase
- Source file parsing
- Language-specific AST generation
- Translation to HLIR (High-Level IR)
- Language-specific optimizations
  - Idiom recognition
  - Language-specific pattern matching
  - Domain-specific optimizations

### 2. Middle Phase
- Language-independent optimizations on HLIR
  - Common subexpression elimination
  - Dead code elimination
  - Constant propagation
  - Loop optimizations
  - Control flow optimizations
- HLIR to MLIR transformation
- Resource analysis using platform profile

### 3. Backend Phase
- MLIR to LLIR transformation
- Platform-specific optimizations
- Register allocation
- Memory layout planning

### 4. ROM Generation Phase
- Final code generation using selected dispatch technique
- ROM header generation
- Platform-specific memory layout
- Debug information embedding (if enabled)

## Command Line Interface

```
gamevm compile [options] <input-files>

Options:
  --target <console>        Target gaming console (required)
  --dispatch <technique>    Code dispatch technique (default: DTC)
  --output <file>          Output ROM file
  --debug                  Include debug information
  --opt-level <0-3>        Optimization level
  --language <lang>        Force specific language (optional)
```

## Configuration Files

Support for project configuration files (gamevm.json):
```json
{
  "target": "genesis",
  "dispatch": "dtc",
  "optimizationLevel": 2,
  "debug": false,
  "sources": [
    "src/main.py",
    "src/graphics.pas"
  ],
  "output": "game.rom"
}
```

## Platform Profiles

Each target console has a comprehensive profile that defines its characteristics and constraints:

### Hardware Profile
- Memory map specification
  - ROM regions (size, banking capabilities)
  - RAM regions (zero page, stack, general purpose)
  - Memory-mapped I/O addresses
  - DMA regions
- CPU specifications
  - Instruction set
  - Register set
  - Addressing modes
  - Clock speed
- Video capabilities
  - Resolution
  - Color depth
  - Sprite limitations
  - Tile limitations
- Audio capabilities
  - Sound channels
  - Sample rate limitations
  - Audio memory constraints

### Constraint Checking
- Static memory analysis during compilation
  - ROM space utilization tracking
  - Static RAM allocation analysis
  - Maximum stack depth estimation
  - Bank-switched memory management
- Resource limit validation
  - Sprite count per scanline
  - Audio channel usage
  - DMA timing constraints
- Early warning system
  - Warnings at 75% resource usage
  - Errors at resource limits
  - Suggestions for optimization

### Profile-Driven Optimization
- Target-specific optimization strategies
- Memory layout optimization
- Bank switching optimization
- CPU-specific instruction selection
- Platform-specific code patterns

## Additional Considerations

### ROM Image Generation
- Platform-specific ROM header generation
- Checksum calculation and insertion
- Bank switching setup if required
- Memory map validation

### Debug Support
- Source-level debugging information
- Symbol table generation
- Line number mapping
- Platform-specific debug features

### Error Handling
- Clear error messages with source locations
- Warning system for potential issues
- Platform-specific constraint violations
- Resource usage warnings

### Build System Integration
- Support for build system integration
- Incremental compilation capabilities
- Dependency tracking
- Build cache support

## Future Considerations

### Potential Enhancements
- Cross-language optimization
- Link-time optimization
- Profile-guided optimization
- Platform-specific tooling integration
- Source-level debugger integration with MAME

### Performance Monitoring
- Compilation time metrics
- Generated code size analysis
- Resource usage reporting
- Performance prediction tools

## Implementation Strategy

### Phase 1
- Basic multi-language support
- Essential target platforms
- Core dispatch techniques
- Basic ROM generation

### Phase 2
- Advanced optimizations
- Additional target platforms
- Debug information generation
- Build system integration

### Phase 3
- Performance tools
- Advanced debugging features
- Profile-guided optimization
- Additional language support
