# Compiler Documentation

This directory contains documentation about the GameVM compiler architecture and implementation.

## Overview

The GameVM compiler is a multi-language, multi-target compiler system designed to support various programming languages and target platforms.

## Key Components

- **Frontend**: Language-specific parsers and semantic analyzers
- **Middle-end**: Optimization passes and intermediate representations
- **Backend**: Target-specific code generation

## Supported Languages

- Pascal
- C# (planned)
- Additional languages (planned)

## Supported Targets

See the [platforms](../platforms/README.md) directory for information about supported target platforms.

## Documentation Structure

- `compiler_architecture.md` - High-level architecture overview
- `atari2600_implementation.md` - Atari 2600 backend implementation details
- `ErrorHandling.md` - Compiler error handling strategy
- `HLIR.md` - High-Level Intermediate Representation
- `LLIR.md` - Low-Level Intermediate Representation
- `MLIR.md` - MLIR integration details
- `Parser.md` - Parsing architecture
- `BuildSystem.md` - Compiler build system
- `TypeSystem.md` - Type system implementation
- `InlineAssembly.md` - Inline assembly support
- `InternalAssemblyAPI.md` - Internal assembly API
- `LanguageIntegration.md` - Language integration guide
- `ModuleResolution.md` - Module resolution system
- `DynamicLoading.md` - Dynamic loading support
- `optimization.md` - Optimization strategies
- `debugging.md` - Debugging support
- `testing_strategy.md` - Testing strategy
- `code-generation.md` - Code generation details
