---
title: "API Documentation"
description: "Public API reference for GameVM"
author: "GameVM Team"
created: "2025-09-24"
updated: "2025-09-24"
version: "1.0.0"
---

# GameVM API Documentation

## 1. Core API

### 1.1 Compiler

#### `compile(source: string, options?: CompileOptions): Promise<CompileResult>`
Compiles source code to target platform.

**Parameters:**
- `source`: Source code to compile
- `options`: Optional compilation options

**Returns:** Promise that resolves to compilation result

**Example:**
```typescript
const result = await compile('fn main() { print("Hello"); }', {
  target: 'nes',
  optimization: 'size'
});
```

### 1.2 Virtual Machine

#### `class VirtualMachine`
Manages execution of GameVM bytecode.

**Methods:**
- `load(module: Module): Promise<void>`
- `execute(entryPoint: string = 'main'): Promise<number>`
- `getGlobal(name: string): unknown`

## 2. Standard Library

### 2.1 Console I/O

#### `print(...args: any[]): void`
Prints values to standard output.

### 2.2 Math

#### `Math`
Standard mathematical functions.

**Constants:**
- `PI`: Ratio of a circle's circumference to its diameter
- `E`: Euler's number

**Methods:**
- `sin(x: number): number`
- `cos(x: number): number`
- `sqrt(x: number): number`

## 3. Platform-Specific APIs

### 3.1 NES

#### `nes.graphics`
Access to NES PPU functionality.

**Methods:**
- `setBackgroundColor(color: number): void`
- `drawSprite(x: number, y: number, tile: number): void`

## 4. Error Handling

### 4.1 Error Types

#### `class GameVMError extends Error`
Base class for all GameVM errors.

#### `class CompileError extends GameVMError`
Errors during compilation.

## 5. Type Definitions

### 5.1 Compiler Options
```typescript
interface CompileOptions {
  target?: 'nes' | 'snes' | 'genesis';
  optimization?: 'none' | 'size' | 'speed';
  debug?: boolean;
  output?: string;
}
```

### 5.2 Compile Result
```typescript
interface CompileResult {
  success: boolean;
  output?: Uint8Array;
  errors: CompileError[];
  warnings: string[];
}
```

## 6. Examples

### 6.1 Basic Usage
```typescript
import { compile, VirtualMachine } from 'gamevm';

async function run() {
  const result = await compile('fn main() { print("Hello, World!"); }');
  if (result.success) {
    const vm = new VirtualMachine();
    await vm.load(result.output);
    await vm.execute();
  }
}
```

## 7. Deprecations

### 7.1 Deprecated APIs
- `oldFunction()`: Use `newFunction()` instead
- `legacyModule`: Will be removed in v2.0.0

## 8. Performance Considerations
- Avoid excessive memory allocations in hot paths
- Use typed arrays for performance-critical code
- Enable optimizations for release builds

## 9. Security
- All I/O operations are sandboxed by default
- Memory-safe by design
- No eval() or dynamic code execution

## 10. Contributing
See [CONTRIBUTING.md](../../CONTRIBUTING.md) for API contribution guidelines.
