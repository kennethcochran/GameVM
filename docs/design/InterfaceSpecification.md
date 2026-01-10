---
title: "Interface Specifications"
description: "Formal interface contracts for GameVM components"
author: "GameVM Team"
created: "2025-09-24"
updated: "2025-09-24"
version: "1.0.0"
---

# Interface Specifications

## 1. Compiler Interfaces

### 1.1 Frontend Interface
```typescript
interface FrontendCompiler {
  /**
   * Parse source code into HLIR
   * @param source Source code to parse
   * @param options Compilation options
   * @returns HLIR representation of the source
   * @throws SyntaxError on invalid syntax
   */
  parse(source: string, options: CompileOptions): HLIRModule;
}
```

### 1.2 HLIR to MLIR Conversion
```typescript
interface HLIRToMLIRConverter {
  /**
   * Convert HLIR to MLIR
   * @param hlir HLIR module to convert
   * @param options Conversion options
   * @returns MLIR module
   * @throws CompilationError on unsupported features
   */
  convert(hlir: HLIRModule, options: ConvertOptions): MLIRModule;
}
```

## 2. Runtime Interfaces

### 2.1 Virtual Machine
```typescript
interface VirtualMachine {
  /**
   * Execute a compiled module
   * @param module Compiled module to execute
   * @param entryPoint Optional entry point (defaults to 'main')
   * @param args Command line arguments
   * @returns Program exit code
   */
  execute(module: CompiledModule, entryPoint?: string, args?: string[]): Promise<number>;
}
```

## 3. Error Handling

### 3.1 Error Codes
| Code | Name | Description |
|------|------|-------------|
| 1001 | E_COMPILE_ERROR | General compilation error |
| 1002 | E_RUNTIME_ERROR | Runtime execution error |
| 1003 | E_TYPE_MISMATCH | Type checking failure |
| 1004 | E_UNDEFINED_SYMBOL | Reference to undefined symbol |

## 4. Data Types

### 4.1 Type System
```typescript
interface TypeSystem {
  /**
   * Check type compatibility
   * @param source Source type
   * @param target Target type
   * @returns True if source is assignable to target
   */
  isAssignable(source: Type, target: Type): boolean;
}
```

## 5. Module System

### 5.1 Module Interface
```typescript
interface Module {
  /**
   * Import a module by name
   * @param name Module name to import
   * @returns The imported module
   * @throws ModuleNotFoundError if module cannot be found
   */
  import(name: string): Promise<Module>;
}
```

## 6. Build System

### 6.1 Build Target
```typescript
interface BuildTarget {
  /**
   * Compile source files for this target
   * @param sources Source files to compile
   * @param options Build options
   * @returns Promise that resolves when compilation is complete
   */
  compile(sources: string[], options: BuildOptions): Promise<BuildArtifact>;
}
```

## 7. Versioning
All interfaces are versioned using Semantic Versioning (SemVer). Each interface includes a `@since` tag indicating the version it was introduced in.

## 8. Stability Index
- **Stable**: Public API, follows semantic versioning
- **Experimental**: May change in minor versions
- **Internal**: Not part of public API, may change without notice
