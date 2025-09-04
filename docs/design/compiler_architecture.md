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
- Compiler switch to specify target console
- Platform-specific code generation and optimization
- ROM image generation following platform specifications
- Memory map and hardware register management
- Support for platform-specific features (bank switching, special hardware)

### Code Generation Options
- Selectable dispatch technique via compiler switch:
  - Token Threaded Code (TTC) - Most compact
  - Indirect Threaded Code (ITC) - Balance of size/speed
  - Direct Threaded Code (DTC) - Good for retro hardware
  - Subroutine Threaded Code (STC) - Modern CPU focused
  - Pure machine code - Maximum performance
- Platform-specific optimization for each technique
- Debug information generation option

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
