# Compiler Architecture

## Overview

The GameVM compiler is a **cross-compiler** that runs on modern workstations to produce specialized binaries for retro gaming consoles. It models a hybrid accumulator-based Virtual Machine architecture (LLIR), which can be emitted in several ways depending on the developer's choice:

1.  **Pure Native Machine Code**: The LLIR is AOT-compiled directly into the target's native machine instructions.
2.  **Subroutine Threaded Code (STC)**: Each LLIR instruction is emitted as a native **call** to a subroutine.
3.  **Direct Threaded Code (DTC)**: Each LLIR instruction is emitted as a **list of addresses** pointing directly to handler code.
4.  **Indirect Threaded Code (ITC)**: Each LLIR instruction is emitted as a **list of pointers** to code-fields.
5.  **Token Threaded Code (TTC)**: The only true **bytecode** format; instructions are emitted as a stream of 1-byte (or 2-byte) **tokens** used as indices into a lookup table.

For all threaded techniques (STC, DTC, ITC, TTC), the compiler also emits a tailored, minimal interpreter or "inner loop" optimized for the chosen platform.

## Key Requirements

### Multi-Language Support
- Language selection via file extension
- Common IR representation for all supported languages
- Language-specific frontends implementing the `ILanguageFrontend` interface
- Language support roadmap:
  - **In Development**: Pascal (primary focus for 8-bit era authenticity)
  - **Planned**: C (historically accurate for retro game development)
  - **Future**: Additional 8-bit era languages as needed

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

1. **Native Code Generation (Pure Machine Code)**
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
- **Developer-Guided Superinstructions**: Functions marked with `[Super]` attribute for custom opcode generation
- **Hand-Optimized Superinstructions**: Combine `[Super]` with inline LLIR assembly for maximum performance
- **Compiler Validation**: Structural criteria validation with diagnostic feedback
- **Specialized Variants**: Creation of specialized instruction variants based on usage patterns
- **Profile-Guided Optimization**: Optional runtime profiling to guide optimization

#### Superinstruction Generation Strategies

**1. Developer-Guided (Default)**
- Functions marked with `[Super]` attribute
- Compiler validation against criteria
- Hand-optimized with inline assembly support

**2. Automatic Generation (Optional)**
- Pattern detection for common sequences
- Profile-guided optimization
- Configurable aggressiveness levels

**3. Hybrid Mode (Recommended for Performance)**
- Manual `[Super]` functions always prioritized
- Automatic generation for additional optimizations
- Compiler directives for fine control

#### Superinstruction Intent Signaling

```pascal
// Strong hints (always attempt)
[Super] function CriticalFunction(...);

// Gentle hints (attempt if beneficial)
[Super(Auto)] function ImportantFunction(...);

// Strong preference (attempt but can override)
[Super(Aggressive)] function PerformanceFunction(...);

// Prevent superinstruction
[NoSuper] function DebugFunction(...);
```

#### Superinstruction Criteria
Functions must meet requirements to become superinstructions:

- **Size Limit**: ≤ 20-30 LLIR instructions (configurable)
- **Control Flow**: Simple loops and conditional branches allowed
- **Parameter Limit**: ≤ 6 parameters (including hardware registers)
- **Hardware Access**: Direct memory-mapped I/O access permitted
- **Cycle-Critical**: Deterministic timing requirements take priority
- **Inline Assembly**: Must use inline LLIR assembly for hardware-specific code

**Developer Responsibility:**
When using superinstructions, developers must:
- Document timing requirements and cycle counts
- Ensure compatibility across target dispatch techniques
- Test on actual hardware or accurate emulators
- Provide fallback implementations for unsupported targets

#### Performance-Critical Superinstructions

Display kernels and similar timing-critical code are ideal superinstruction candidates:
```pascal
[Super]
procedure CustomDisplayKernel;  // User-defined display kernel
begin
  asm
    // Complex timing-critical code with loops
    // Direct TIA register access
    // Precise cycle counting
    // Hardware-specific optimizations
  end;
end;
```

#### Superinstruction Modes
```pascal
// Standard mode (default criteria)
[Super] function FastAdd(A, B: Integer): Integer;

// Auto mode (compiler decides based on analysis)
[Super(Auto)] function GraphicsRoutine;

// Aggressive mode (maximum optimization)
[Super(Aggressive)] function CriticalPath;

// Prevent superinstruction
[NoSuper] function DebugFunction(...);
```

#### Standard Library and HAL Integration
The GameVM Standard Library and Hardware Abstraction Layer include numerous built-in superinstructions for common operations:

**Standard Library Superinstructions:**
- Graphics primitives and sprite operations
- Audio processing and mixing
- Input handling and controller state
- Physics calculations and collision detection
- Animation and timing functions

**HAL Superinstructions:**
- Display kernel implementations for each target
- Hardware register access and manipulation
- DMA transfer operations
- Interrupt handling and timing
- Platform-specific optimizations

See the [Standard Library](../api/stdlib/StandardLibrary.md) and [HAL](../api/hal/HAL.md) documentation for complete lists of built-in superinstructions.

#### Hand-Optimized Superinstructions
Developers can combine `[Super]` with inline assembly:
```pascal
[Super]
function FastMulAdd(var Result: Integer; A, B, C: Integer): Integer;
begin
  asm
    MOV R0, [A], TYPE_INT32
    MUL R0, [B], TYPE_INT32
    ADD R0, [C], TYPE_INT32
    MOV [Result], R0, TYPE_INT32
  end;
end;
```

#### Performance Benefits
- **Normal Function Call**: ~20-50 cycles (dispatch overhead + call/ret)
- **Generated Superinstruction**: ~5-15 cycles (direct implementation)
- **Hand-Optimized Superinstruction**: ~3-8 cycles (assembly-tuned)
- Performance gains vary by target and dispatch technique

#### Performance Characteristics

| Technique   | Code Size | Speed | Memory Usage | Best For                   |
| ----------- | --------- | ----- | ------------ | -------------------------- |
| Native Code | Large     | ★★★★★ | Medium       | Performance-critical code  |
| DTC         | Medium    | ★★★★☆ | Medium       | 8/16-bit systems           |
| ITC         | Small     | ★★★☆☆ | Low          | ROM-constrained systems    |
| STC         | Large     | ★★★★☆ | Medium       | Modern emulation           |
| TTC         | Smallest  | ★★☆☆☆ | Lowest       | Memory-constrained systems |

#### Platform-Specific Optimization
- Each dispatch technique has platform-specific optimizations
- Memory layout optimized for the target hardware
- Special considerations for:
  - Cache line alignment
  - Branch prediction hints
  - Pipeline optimization
  - Memory access patterns

## Inline Assembly Integration

GameVM supports inline assembly across all frontend languages, allowing developers to write LLIR instructions directly within their high-level code. The inline assembly system uses a dedicated ANTLR grammar that integrates with each language frontend.

### Assembly Processing Pipeline

1. **Parsing Phase**: Assembly blocks are parsed using the VirtualAssembly grammar
2. **Semantic Analysis**: Assembly participates in type checking and validation
3. **Optimization**: Assembly can be optimized by standard compiler passes
4. **LLIR Generation**: Assembly is transformed into LLIR instructions
5. **Backend Processing**: LLIR assembly instructions are processed like any other code

### Key Features

- **Unified syntax** across all frontend languages
- **Type system integration** with full type checking
- **Optimization participation** in HLIR/MLIR passes
- **Hardware-specific support** for target platforms
- **Register allocation hints** for performance optimization

For detailed information about inline assembly syntax, language integration, and examples, see the [Inline Assembly](InlineAssembly.md) document.

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
- ROM/Executable image generation
- Relocation table generation for Dynamic Loading (if applicable)
- Platform-specific memory layout
- Debug information embedding (if enabled)

## Command Line Interface

```
GameVM build [options] [input-files]

Options:
  --target <console>        Target gaming console (required)
  --dispatch <technique>    Code dispatch technique (default: DTC)
  --output <file>          Output ROM file
  --debug                  Include debug information
  --opt-level <0-3>        Optimization level
  --language <lang>        Force specific language (optional)
  --super <mode>           Superinstruction mode (off, manual, auto, aggressive)
```

## Configuration Files

Support for project configuration files (gamevm.json):
```json
{
  "target": "genesis",
  "dispatch": "dtc",
  "optimizationLevel": 2,
  "debug": false,
  "superinstructions": "auto",
  "sources": [
    "src/main.pas",
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

## See Also

- [HLIR Design](HLIR.md) - High-level intermediate representation
- [MLIR Design](MLIR.md) - Mid-level intermediate representation
- [LLIR Design](LLIR.md) - Low-level intermediate representation
- [Dynamic Loading](DynamicLoading.md) - Dynamic linking and relocation
- [Module System](ModuleResolution.md) - Module system design
