# GameVM Copilot Instructions

This file provides guidance for AI assistants and Copilot models working on the GameVM project.

## Project Overview

**GameVM** is a cross-compiler toolchain designed for retro video game development. It enables developers to write games in modern, high-level languages (Pascal, C, etc.) and compile them to optimized bytecode for 2nd-5th generation gaming consoles (NES, SNES, Genesis, N64, PlayStation, Atari 2600, etc.).

### Key Characteristics
- **Host/Target Philosophy**: Complex analysis/optimization happens on modern hosts; output is tailored binaries (ROM/bytecode)
- **Multi-stage IR Pipeline**: HLIR (High-Level) → MLIR (Mid-Level) → LLIR (Low-Level)
- **Platform-Agnostic**: Single codebase supports multiple retro hardware platforms
- **Early Development**: Project is in active development, not production-ready

## Architecture & Core Concepts

### Intermediate Representations (IRs)
1. **HLIR (High-Level IR)**: Language-agnostic AST preserving high-level semantics
2. **MLIR (Mid-Level IR)**: Focuses on optimizations and resource management (graph-based structure)
3. **LLIR (Low-Level IR)**: Accumulator-based VM ISA, close to machine code

### Key Components
- **Frontends**: Language-specific parsers (Pascal, C)
- **Compiler Core**: IR transformations and optimizations
- **Backends**: Platform-specific code generation
- **Module System**: Dependency management across the codebase
- **Type System**: Type safety across language boundaries

### Dispatch Techniques
The LLIR can be executed via multiple strategies:
- **Token Threaded Code (TTC)**: Compact bytecode (1-byte tokens)
- **Indirect Threaded Code (ITC)**: Jump table dispatch
- **Direct Threaded Code (DTC)**: Direct implementation pointers
- **Subroutine Threaded Code (STC)**: Native CALL/RET patterns
- **Native Code**: Direct machine code generation

### Superinstructions
- Developer-marked methods using `[Super]` attribute become superinstructions
- Compiler analyzes candidate methods against structural requirements (size, complexity, parameters)
- Can combine with inline LLIR assembly for hand-optimized versions

## Project Structure

```
GameVM/
├── src/                              # Source code
│   ├── GameVM.Compiler.Core/        # Core compiler infrastructure
│   ├── GameVM.Compiler.Pascal/      # Pascal frontend
│   ├── GameVM.Compiler.Application/ # CLI application
│   ├── GameVM.Compiler.Interaction/ # Language interaction layer
│   ├── GameVM.Compiler.Backend.Atari2600/
│   ├── GameVM.Compiler.Optimizers.MidLevel/
│   ├── GameVM.Compiler.Optimizers.LowLevel/
│   └── GameVM.DevTools/             # Development utilities
├── test/                             # Test suites
│   ├── GameVM.Compiler.*.Tests/     # Unit tests (NUnit)
│   └── GameVM.Compiler.Specs/       # BDD/Scenario tests (Reqnroll)
├── docs/                             # Documentation
│   ├── compiler/                    # Compiler design docs
│   │   ├── HLIR.md, MLIR.md, LLIR.md
│   │   ├── LLIR_ISA.md              # ISA specification
│   │   ├── InlineAssembly.md
│   │   └── [others]
│   ├── platforms/specs/             # Hardware specifications
│   ├── architecture/                # High-level design
│   └── api/                         # API documentation
└── GameVM.sln                        # Visual Studio solution

```

## Development Guidelines

### Technology Stack
- **.NET 10 SDK** required (project uses modern C#)
- **JDK 21+** required only for ANTLR (parser generation during build)
- **NUnit** for unit tests
- **Reqnroll** for BDD scenario tests
- **ANTLR** for language parsing (Java-based, run at build time)

### Build & Testing
```bash
# Restore dependencies
dotnet restore

# Build solution
dotnet build

# Run all tests
dotnet test

# Build specific project
dotnet build src/GameVM.Compiler.Core/GameVM.Compiler.Core.csproj
```

### Code Organization
- **Core compiler logic**: `GameVM.Compiler.Core` namespace
- **Language frontends**: `GameVM.Compiler.Pascal`, `GameVM.Compiler.C` (planned)
- **IR implementations**: Separate files for HLIR, MLIR, LLIR
- **Backend implementations**: Per-platform backend projects
- **Optimization passes**: Separate projects for mid-level and low-level optimizers

### IR-Specific Considerations

#### When Working on HLIR
- Focus on language semantics and high-level constructs
- Preserve source location information for debugging
- Ensure type information is complete and accurate
- Consider interaction with the module system

#### When Working on MLIR
- Leverage graph structure for control flow analysis
- Implement resource analysis (memory usage, stack tracking)
- Focus on architecture-independent optimizations
- Coordinate with optimization passes

#### When Working on LLIR
- Remember it's the virtual machine ISA specification
- Reference `/docs/compiler/LLIR_ISA.md` for instruction definitions
- Consider register allocation constraints
- Account for width types (INT8, INT16, INT32, INT64, PTR, FLOAT32, FLOAT64, BOOL)
- LLIR instructions: LOAD, STORE, MOV, ADD, SUB, MUL, DIV, MOD, AND, OR, XOR, NOT, SHL, SHR, ROL, ROR, CMP, TEST, JUMP/conditional jumps, CALL, RET, CAST, MEMCPY, MEMSET, MEMCMP, etc.

### Platform Specifications

Key target platforms and their constraints:
- **Atari 2600**: 2KB RAM, 6502 @ 1.19 MHz (8-bit)
- **NES**: 2KB RAM, 6502 @ 1.79 MHz (8-bit)
- **SNES**: 128KB RAM, 65816 @ 3.58 MHz (16-bit)
- **Genesis/Mega Drive**: 64KB RAM, 68000 @ 7.67 MHz (16-bit)
- **Nintendo 64**: 4-8MB RAM, R4300i @ 93.75 MHz (32-bit, with JIT support)
- **PlayStation 1**: 2MB RAM, R3000 @ 33.8688 MHz (32-bit, with basic JIT)
- **Sega Saturn**: 2MB RAM, dual SH-2 @ 28.6 MHz (32-bit, dual-CPU aware)

Documentation for platforms is in `/docs/platforms/specs/`.

## Common Tasks

### Adding a New Instruction to LLIR
1. Define the instruction in `/docs/compiler/LLIR_ISA.md`
2. Create instruction class in `GameVM.Compiler.Core/IR/LowLevelIR.cs`
3. Implement handling in optimizer passes
4. Implement in target backends (Atari2600, etc.)
5. Add corresponding test cases

### Implementing a Backend Optimization
1. Create file in appropriate optimizer project (MidLevel/LowLevel)
2. Implement the optimization pass
3. Add unit tests in corresponding `.Tests` project
4. Consider performance impact and benchmarks

### Adding Documentation
- Follow the existing documentation structure
- Reference related documents using relative links
- Include code examples where appropriate
- Update `/docs/README.md` if adding new sections
- See `/docs/architecture/DocumentationStandards.md` for style guidelines

## Testing Strategy

### Unit Tests (NUnit)
- Located in `test/GameVM.Compiler.*.Tests/`
- Test individual components in isolation
- Use descriptive test names following pattern: `Method_Scenario_ExpectedResult`

### BDD Tests (Reqnroll/Gherkin)
- Located in `test/GameVM.Compiler.Specs/`
- Scenario-based end-to-end tests covering language features
- Validate backend code generation
- Verify behavior correctness

### Running Tests
```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test test/GameVM.Compiler.Core.Tests/

# Run with coverage
dotnet test /p:CollectCoverage=true
```

## Code Standards

### Naming Conventions
- **Classes**: PascalCase (e.g., `HighLevelIR`, `AtariBackend`)
- **Methods**: PascalCase (e.g., `CompileModule`, `OptimizeInstructions`)
- **Variables**: camelCase (e.g., `instructionList`, `optimizationPass`)
- **Constants**: UPPER_SNAKE_CASE (e.g., `MAX_REGISTER_COUNT`)
- **Namespaces**: PascalCase following project hierarchy

### Code Quality
- Follow C# style guidelines
- Use XML documentation comments for public APIs
- Maintain clear separation of concerns
- Avoid deep nesting
- Write testable code with dependency injection

## Contributing

See [CONTRIBUTING.md](../../CONTRIBUTING.md) for detailed contribution guidelines including:
- Fork and clone workflow
- Branch naming conventions
- Commit message standards
- Pull request process
- Code review expectations

## Documentation References

Key documents to consult:
- **Architecture**: [Architecture Overview](../../docs/architecture/ArchitectureOverview.md)
- **LLIR Specification**: [LLIR ISA](../../docs/compiler/LLIR_ISA.md)
- **Inline Assembly**: [Inline Assembly Guide](../../docs/compiler/InlineAssembly.md)
- **Optimization**: [Optimization Features](../../docs/optimization.md)
- **Code Generation**: [Code Generation Strategies](../../docs/code-generation.md)

## Common Pitfalls to Avoid

1. **Assuming 32-bit architecture**: Remember, targets range from 8-bit (Atari 2600) to 32-bit (N64)
2. **Ignoring memory constraints**: ROM/RAM is severely limited on retro platforms
3. **Forgetting register allocation**: Physical registers are scarce on 8/16-bit targets
4. **Platform-specific optimizations too early**: Focus on architecture-independent optimizations in MLIR first
5. **Missing width specifiers**: LLIR instructions require explicit width types

## Quick Links

- **GitHub Repository**: https://github.com/kennethcochran/GameVM
- **Main README**: [README.md](../../README.md)
- **License**: [Unlicense](../../LICENSE)
- **Code of Conduct**: [CODE_OF_CONDUCT.md](../../CODE_OF_CONDUCT.md)

## Getting Help

- Check existing documentation in `/docs/` directory
- Review test cases for usage examples
- Look at existing backend implementations for patterns
- Consult the issue tracker for known problems and discussions
