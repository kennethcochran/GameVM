# GameVM

A virtual machine specifically designed for video games, making retro console development easier by providing modern tools while respecting vintage hardware constraints.

## Quick Start

### Prerequisites

GameVM requires:
- .NET 8 SDK ([download](https://dotnet.microsoft.com/download/dotnet/8.0))
- Java Development Kit 21 or later ([download](https://adoptium.net/))

Both must be installed and available in your system's PATH.

You can verify your setup by running:
```shell
dotnet --version  # Should show 8.x.x
java -version     # Should show 21.x.x or higher
```

### Build
```pwsh
dotnet build
```

## Features

- Flexible code generation (VM-based or native)
- Dynamic VM generation tailored to your game
- Modern language support with compile-time checking
- Cross-platform development within and across console generations
- Comprehensive development tools and analysis

## Documentation

- [Getting Started](docs/getting-started.md)
- [Code Generation Strategies](docs/code-generation.md)
- [Architecture Overview](docs/architecture.md)
- [Optimization Guide](docs/optimization.md)
- [Debugging Features](docs/debugging.md)
- [Full Documentation](docs/README.md)

### Supported Systems
- Atari 2600 (MOS 6507)
- ColecoVision (Z80)
- Fairchild Channel F (F8)
- Intellivision (CP1610)
- Nintendo Entertainment System (6502)
- Vectrex (6809)

See [systems documentation](docs/systems/) for detailed hardware references.

## Development Status

GameVM is under active development. We follow [Semantic Versioning](https://semver.org/) and maintain a [changelog](CHANGELOG.md).

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for development setup and guidelines.

## Code of Conduct

This project follows the [Contributor Covenant](CODE_OF_CONDUCT.md).

## License

TBD - See [LICENSE](LICENSE) for details.

2. **Direct Threaded Code (DTC)**
   - Faster execution than TTC
   - Moderate code size
   - Good balance of speed and size
   - Ideal for: General-purpose game code

3. **Subroutine Threaded Code (STC)**
   - Fastest VM execution
   - Larger code size
   - Platform-specific optimizations
   - Ideal for: Performance-critical code needing portability

### Native Code Generation
- Direct machine code output
- No runtime overhead
- Platform-specific code
- Ideal for: Timing-critical routines

### Mixed-Mode Execution
- Combine different strategies within the same game
- Use native code for timing-critical sections
- Use threaded code for general game logic
- Balance between size and speed per module

## Dynamic VM Generation

GameVM analyzes your code at build time to create a specialized VM:

### Analysis Phase
- Identifies actually used instructions
- Discovers common code patterns
- Maps data access patterns
- Analyzes timing requirements

### Optimization Phase
- Creates custom opcodes for common patterns
- Optimizes memory layout for your game
- Generates specialized dispatch code
- Implements platform-specific features

### Resource Analysis
- Detailed size analysis for each strategy
- Memory usage estimates
- Execution speed comparisons
- Custom opcode efficiency metrics

## Development Experience

GameVM provides modern development tools while targeting vintage hardware:

### Build-Time Analysis
- ROM and RAM usage analysis
- Cycle-accurate timing analysis
- Bank switching optimization
- Custom opcode suggestions

### Development Tools
- Source-level debugging
- Performance profiling
- Memory usage visualization
- Code size analysis

### Configuration Options
- Code generation strategy selection
- Optimization preferences
- Platform-specific settings
- Mixed-mode execution controls

## Architecture Overview

GameVM uses a flexible, multi-level architecture that enables efficient game development across diverse gaming platforms. The system consists of three main components:

1. **Compiler Pipeline**: Transforms high-level code into optimized platform-specific code
2. **Hardware Abstraction Layer**: Provides unified access to diverse gaming hardware
3. **Runtime System**: Handles execution, memory management, and platform-specific optimizations

## Intermediate Representation (IR) Design

GameVM uses a multi-level IR design that bridges the gap between high-level languages and various retro gaming platforms. Each IR level serves a specific purpose in the compilation pipeline:

### High-Level IR (HLIR)
- Language-independent representation of high-level constructs
- Preserves program structure and semantics
- Features:
  - Control flow structures (loops, conditionals)
  - Function and method calls
  - Object operations and data structures
  - Variable scoping and visibility
  - Type information where available
  - Platform-agnostic optimizations

### Mid-Level IR (MLIR)
- Architecture-neutral optimization layer
- Focuses on program analysis and transformation
- Features:
  - Control and data flow analysis
  - Memory access pattern optimization
  - Stack frame management
  - Dead code elimination
  - Constant propagation
  - Static resource analysis (ROM, RAM, stack)
  - Bank switching analysis
  - Critical path identification

### Low-Level IR (LLIR)
- Target-specific representation
- Bridges architecture differences
- Features:
  - Hybrid instruction model
    - Register-based operations (Z80, 68000)
    - Accumulator-based operations (6502 family)
  - Explicit memory operations
  - Hardware-specific optimizations
  - Bank switching implementation
  - Platform-specific calling conventions

## Hardware Abstraction Layer for GameVM

GameVM's HAL is designed specifically for retro gaming hardware, balancing abstraction with performance. Unlike traditional HALs that completely hide hardware details, GameVM's HAL provides:

1. **Tiered Abstraction Levels**
   - High-level portable APIs for cross-platform development
   - Mid-level APIs exposing platform-specific optimizations
   - Low-level direct hardware access when needed

2. **Core Subsystems**
   - **Video**: Abstracts various video chips (TIA, PPU, VDP) with common primitives
     - Sprite management
     - Tile-based backgrounds
     - Hardware scrolling
     - Color palette management
   - **Audio**: Unified interface for different sound hardware
     - Pulse/square wave generation
     - Frequency modulation
     - Sample playback (where supported)
   - **Input**: Normalizes different controller types
     - Digital pad mapping
     - Analog input scaling
     - Multi-player support
   - **Memory**: Smart memory management
     - Bank switching support
     - Memory-mapped I/O handling
     - Zero-page optimization for 6502

## Seamless Interop Between Bytecode and Machine Code

GameVM provides a sophisticated interoperability system between bytecode and native machine code, enabling optimal performance without sacrificing development flexibility:

1. **Zero-Cost Bridging**
   - Direct function calls between bytecode and machine code
   - No marshalling overhead for primitive types
   - Efficient object representation sharing
   - Register-aware calling conventions

2. **Runtime Memory Management**
   - Unified memory model across bytecode and native code
   - Automatic stack frame alignment
   - Shared heap management
   - Zero-copy data access where possible

## Integrated Debugging Environment

GameVM aims to provide a seamless debugging experience by integrating with popular emulators through the LibRetro API. This integration enables developers to debug their games in their actual target environment.

### Emulator Integration

- LibRetro/RetroArch integration for cross-platform debugging support
- Direct memory inspection and modification during runtime
- Breakpoint support across both GameVM bytecode and native code
- Real-time state inspection while the game is running

### Debug Features

1. **Source-Level Debugging**
   - Step through high-level code while seeing actual console state
   - Map between source code and generated bytecode/native code
   - Variable inspection in original source language
   - Conditional breakpoints with high-level expressions

2. **Hardware State Visualization**
   - Real-time visualization of sprite tables and tile maps
   - Audio channel state monitoring
   - CPU register and memory state inspection
   - Bank switching state tracking

## Optimization Features

#### Developer-Suggested Superinstructions
GameVM allows developers to suggest methods as candidates for superinstruction creation. Similar to function inlining hints in modern languages, these suggestions help the compiler identify potentially beneficial optimization opportunities.

Example (syntax varies by language):
```
// Developer suggests this method as a superinstruction candidate
calculate_sum(x, y) {
    result = x + y
    store_value(result)
}
```

The compiler considers these suggestions alongside other criteria:
- Method complexity and size
- Number of parameters and locals
- Usage frequency in the codebase
- Potential performance impact
- Available instruction space

Developer-suggested methods are prioritized in the analysis phase but may not become superinstructions if:
- The method is too complex to be efficiently implemented as a single instruction
- The potential performance gain doesn't justify the increased code size
- The method is rarely called in practice
- It would exceed the maximum allowed superinstructions

This approach balances developer insight with compiler optimization expertise.

#### Automatic Superinstruction Detection
GameVM automatically identifies and optimizes frequently occurring instruction sequences:

- **Pattern Analysis**: Analyzes the bytecode to identify common instruction sequences
- **Frequency Threshold**: Creates superinstructions for sequences that appear more than a configurable threshold
- **Cost-Benefit Analysis**: Evaluates trade-offs between code size and execution speed
- **Cross-Module Analysis**: Detects patterns across different source files

Example of automatically detected patterns:
```csharp
// Original code sequences that appear frequently
load_local    // Load variable
load_const    // Load constant
multiply      // Multiply values
store_local   // Store result

// Automatically combined into superinstruction
load_multiply_store  // Single optimized instruction
```

Configuration options allow fine-tuning of the detection process:
```json
{
    "superinstructions": {
        "minFrequency": 10,        // Minimum occurrences needed
        "maxLength": 4,            // Maximum instructions per sequence
        "maxSuperInstructions": 32 // Maximum number to generate
    }
}
```

## Fine-Grained Compile Time Control

GameVM provides developers with precise control over the compilation process, enabling optimal performance across different console generations:

1. **Compilation Strategies**
   - Function-level compilation decisions
   - Basic block level optimization
   - Loop-specific compilation policies
   - Conditional compilation based on target platform

2. **Developer Controls**
   - Compile to native or bytecode
   - Optimize for size, speed, or balanced
   - Specify memory constraints
   - Enable or disable specific optimizations

## Optional JIT Compilation

GameVM includes optional JIT compilation capabilities for platforms with sufficient resources. This feature is primarily targeted at 5th generation consoles with adequate RAM and CPU power. For all other platforms, GameVM defaults to efficient ahead-of-time compilation and optimized interpreter execution.

### Supported Platforms

JIT compilation is available on:

- Nintendo 64 (4MB-8MB RAM, MIPS R4300i @ 93.75 MHz)
  - Full method JIT compilation
  - Advanced register allocation
  - Loop unrolling
  - Constant propagation
  - Code cache up to 512KB
  - Profile-guided optimization

- Sony PlayStation (2MB RAM, MIPS R3000 @ 33.8688 MHz)
  - Basic block JIT for hot paths
  - Simple register allocation
  - Delay slot optimization
  - Limited method inlining
  - Code cache limited to 128-256KB

- Sega Saturn (2MB RAM, 2x Hitachi SH-2 @ 28.6 MHz)
  - Basic block JIT for critical paths
  - Dual-CPU aware optimization
  - Simple method inlining
  - Code cache limited to 64-128KB per CPU

## Compilation Pipeline

GameVM uses a multi-stage compilation process designed for retro gaming platforms:

1. **Frontend**
   - Source language parsing (ANTLR4-based)
   - AST generation
   - Translation to common IR

2. **Middle-end**
   - Dead code elimination
   - Constant folding
   - Basic loop optimization
   - Superinstruction processing:
     - Recognition of developer-suggested superinstructions
     - Common sequence detection
     - Platform-specific patterns

3. **Backend**
   - Target selection (VM bytecode or native)
   - Register/accumulator allocation
   - Memory layout planning
   - Platform-specific code generation
   - ROM bank assignment

4. **Post-processing**
   - Resource placement
   - ROM bank linking
   - Checksum generation

## Licensing

TBD
