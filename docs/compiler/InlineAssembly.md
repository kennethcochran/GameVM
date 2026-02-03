# Inline Assembly

## Overview

GameVM supports inline assembly across all frontend languages, allowing developers to write low-level LLIR instructions directly within their high-level code. This enables performance-critical optimizations, hardware-specific operations, and precise control over code generation.

### Design Philosophy

Inline assembly in GameVM is designed to:

1. **Integrate with the compilation pipeline** - Assembly is parsed and transformed like any other code
2. **Maintain type safety** - Assembly operations participate in the type system
3. **Enable optimizations** - Assembly can be analyzed and optimized by the compiler
4. **Provide cross-language consistency** - Same assembly syntax across all supported languages
5. **Support hardware-specific features** - Direct access to target hardware capabilities

### Key Features

- **Unified syntax** across all frontend languages
- **ANTLR-based parsing** for consistent error handling
- **Type checking and validation** against the GameVM type system
- **Integration with HLIR/MLIR optimization passes**
- **Support for all LLIR instructions and addressing modes**
- **Register allocation hints** for performance optimization
- **Conditional compilation** based on target platform

## Syntax and Grammar

### Assembly Block Structure

Inline assembly is enclosed in `asm` blocks with the following structure:

```pascal
asm
  // Assembly statements
  MOV R0, [A], TYPE_INT32
  MUL R0, [B], TYPE_INT32
  ADD R0, [C], TYPE_INT32
  MOV [Result], R0, TYPE_INT32
end;
```

### ANTLR Grammar

The inline assembly uses a dedicated ANTLR grammar that integrates with each language frontend:

```antlr
grammar VirtualAssembly;

asmBlock
    : 'asm' (NEWLINE)* '{' assemblyStatement* '}' (NEWLINE)*
    | 'asm' (NEWLINE)* assemblyStatement+ 'end' (NEWLINE)*
    ;

assemblyStatement
    : instruction
    | label
    | directive
    | comment
    ;

instruction
    : OPCODE operand (',' operand)* (NEWLINE)*
    ;

label
    : IDENTIFIER ':' (NEWLINE)*
    ;

directive
    : '.' IDENTIFIER (operand (',' operand)*)? (NEWLINE)*
    ;

comment
    : ';' ~[\r\n]* (NEWLINE)*
    ;

operand
    : REGISTER
    | IMMEDIATE
    | MEMORY_REF
    | IDENTIFIER
    | STRING_LITERAL
    ;

REGISTER
    : 'R' [0-9]
    | 'SP'
    | 'FP'
    | 'PC'
    ;

IMMEDIATE
    : [0-9]+
    | '0x' [0-9A-Fa-f]+
    | '#' [0-9]+
    ;

MEMORY_REF
    : '[' expression ']'
    ;

IDENTIFIER
    : [a-zA-Z_][a-zA-Z0-9_]*
    ;

OPCODE
    : 'MOV' | 'ADD' | 'SUB' | 'MUL' | 'DIV' | 'MOD'
    | 'AND' | 'OR' | 'XOR' | 'NOT' | 'SHL' | 'SHR'
    | 'CMP' | 'JMP' | 'JEQ' | 'JNE' | 'JGT' | 'JLT' | 'JGE' | 'JLE'
    | 'CALL' | 'RET' | 'PUSH' | 'POP'
    | 'LOAD' | 'STORE' | 'LEA'
    | 'NOP' | 'HALT'
    ;

TYPE_SPECIFIER
    : 'TYPE_INT8' | 'TYPE_INT16' | 'TYPE_INT32' | 'TYPE_INT64'
    | 'TYPE_UINT8' | 'TYPE_UINT16' | 'TYPE_UINT32' | 'TYPE_UINT64'
    | 'TYPE_FLOAT32' | 'TYPE_FLOAT64'
    | 'TYPE_PTR' | 'TYPE_BOOL'
    ;
```

### Expression Syntax

Memory references and expressions support the following syntax:

```pascal
// Direct memory access
MOV R0, [0x1000], TYPE_INT32

// Variable access
MOV R0, [myVariable], TYPE_INT32

// Array access
MOV R0, [myArray + index * 4], TYPE_INT32

// Struct field access
MOV R0, [myStruct.field], TYPE_INT32

// Register indirect
MOV R0, [R1 + 4], TYPE_INT32
```

## Language Integration

### Pascal Frontend

```pascal
procedure FastMulAdd(var Result: Integer; A, B, C: Integer);
begin
  asm
    MOV R0, [A], TYPE_INT32
    MUL R0, [B], TYPE_INT32
    ADD R0, [C], TYPE_INT32
    MOV [Result], R0, TYPE_INT32
  end;
end;

// With type annotations
procedure DisplayKernel;
begin
  asm
    // Atari 2600 TIA register access
    MOV R0, #$42, TYPE_UINT8
    MOV [TIA_COLUBK], R0, TYPE_UINT8
    
    // Timing-critical loop
    MOV R1, #192, TYPE_UINT8  // Scanline count
  scanline_loop:
    // Display processing here
    SUB R1, #1, TYPE_UINT8
    JNE scanline_loop
  end;
end;
```

### C Frontend

```c
void fast_mul_add(int *result, int a, int b, int c) {
    asm {
        MOV R0, [a], TYPE_INT32
        MUL R0, [b], TYPE_INT32
        ADD R0, [c], TYPE_INT32
        MOV [result], R0, TYPE_INT32
    }
}

// Hardware-specific example
void atari2600_display_kernel(void) {
    asm {
        // Set background color
        MOV R0, #0x42, TYPE_UINT8
        MOV [TIA_COLUBK], R0, TYPE_UINT8
        
        // Display kernel loop
        MOV R1, #192, TYPE_UINT8
    scanline_loop:
        // Display processing
        SUB R1, #1, TYPE_UINT8
        JNE scanline_loop
    }
}
```

### Python Frontend

```python
def fast_mul_add(result, a, b, c):
    asm("""
        MOV R0, [a], TYPE_INT32
        MUL R0, [b], TYPE_INT32
        ADD R0, [c], TYPE_INT32
        MOV [result], R0, TYPE_INT32
    """)

def atari2600_display_kernel():
    asm("""
        # Set background color
        MOV R0, #0x42, TYPE_UINT8
        MOV [TIA_COLUBK], R0, TYPE_UINT8
        
        # Display kernel loop
        MOV R1, #192, TYPE_UINT8
    scanline_loop:
        # Display processing
        SUB R1, #1, TYPE_UINT8
        JNE scanline_loop
    """)
```

## Type System Integration

### Type Checking

Inline assembly participates in the GameVM type system:

1. **Operand type validation** - Ensures operands match instruction requirements
2. **Memory access type checking** - Validates memory reference types
3. **Register type tracking** - Tracks register contents for optimization
4. **Function parameter mapping** - Maps function parameters to assembly operands

### Type Annotations

Assembly instructions include type specifiers for precise control:

```pascal
// Explicit type specification
MOV R0, [variable], TYPE_INT32    // Load 32-bit integer
MOV R1, [variable], TYPE_UINT8    // Load 8-bit unsigned

// Type conversion
MOV R0, [int_var], TYPE_INT32
MOV R1, R0, TYPE_UINT8            // Truncate to 8-bit

// Pointer operations
MOV R0, [ptr_var], TYPE_PTR
MOV R1, [R0], TYPE_INT32          // Dereference pointer
```

### Register Allocation Hints

Developers can provide hints for register allocation:

```pascal
asm {
    // Prefer R0 for result
    MOV R0, [input], TYPE_INT32
    ADD R0, [input + 4], TYPE_INT32
    
    // Use R1 for temporary
    MOV R1, #42, TYPE_INT32
    MUL R0, R1, TYPE_INT32
}
```

## Optimization Considerations

### Compiler Optimizations

Inline assembly participates in several optimization passes:

1. **Constant folding** - Evaluates constant expressions at compile time
2. **Dead code elimination** - Removes unused assembly instructions
3. **Instruction scheduling** - Reorders instructions for better performance
4. **Register allocation** - Optimizes register usage across assembly blocks
5. **Peephole optimization** - Replaces instruction sequences with more efficient alternatives

### Optimization Directives

Developers can control optimization behavior:

```pascal
asm {
    // Prevent optimization
    .optimize off
    NOP  // Timing-critical NOP
    .optimize on
    
    // Force inline
    .inline always
    
    // Specify target platform
    .target atari2600
    MOV R0, [TIA_COLUBK], TYPE_UINT8
}
```

### Performance Guidelines

1. **Minimize register pressure** - Use registers efficiently
2. **Leverage type information** - Provide explicit type specifiers
3. **Consider target constraints** - Write assembly for specific hardware
4. **Use appropriate addressing modes** - Choose optimal memory access patterns
5. **Profile and optimize** - Measure actual performance impact

## Backend Processing

### LLIR Generation

Assembly blocks are transformed into LLIR instructions:

```pascal
// Source assembly
asm {
    MOV R0, [A], TYPE_INT32
    MUL R0, [B], TYPE_INT32
    MOV [Result], R0, TYPE_INT32
}

// Generated LLIR
LOAD R0, A, TYPE_INT32
MUL R0, B, TYPE_INT32
STORE Result, R0, TYPE_INT32
```

### Target-Specific Optimization

The backend optimizes assembly for each target:

1. **Instruction selection** - Chooses optimal machine instructions
2. **Register allocation** - Maps virtual registers to physical registers
3. **Instruction scheduling** - Orders instructions for pipeline efficiency
4. **Peephole optimization** - Applies target-specific optimizations

### Error Handling

Assembly errors are reported with context:

```
Error in inline assembly at line 42:
    MOV R0, [invalid_var], TYPE_INT32
         ^^^^
Undefined variable: invalid_var
```

## Examples

### Performance-Critical Math

```pascal
// Fast multiplication without hardware multiplier
function FastMul(A, B: Integer): Integer;
begin
  asm {
    MOV R0, [A], TYPE_INT32      // R0 = A
    MOV R1, [B], TYPE_INT32      // R1 = B
    MOV R2, #0, TYPE_INT32       // R2 = result
    
    // Shift-add multiplication
    MOV R3, #32, TYPE_INT32      // Bit counter
  mul_loop:
    TEST R1, #1, TYPE_INT32      // Check LSB of B
    JZ skip_add
    ADD R2, R0, TYPE_INT32       // Add A to result if bit set
  skip_add:
    SHL R0, #1, TYPE_INT32       // A <<= 1
    SHR R1, #1, TYPE_INT32       // B >>= 1
    SUB R3, #1, TYPE_INT32       // Decrement counter
    JNE mul_loop
    
    MOV [Result], R2, TYPE_INT32
  }
end;
```

### Hardware-Specific Operations

```pascal
// Atari 2600 display kernel
procedure DisplayKernel;
begin
  asm {
    // VBlank setup
    MOV R0, #0, TYPE_UINT8
    MOV [TIA_VSYNC], R0, TYPE_UINT8
    MOV [TIA_VBLANK], R0, TYPE_UINT8
    
    // Wait for VSYNC end
    MOV R1, #3, TYPE_UINT8       // 3 scanlines
  vsync_loop:
    SUB R1, #1, TYPE_UINT8
    JNE vsync_loop
    
    // Enable display
    MOV R0, #1, TYPE_UINT8
    MOV [TIA_VSYNC], R0, TYPE_UINT8
    MOV [TIA_VBLANK], R0, TYPE_UINT8
    
    // Display kernel (192 scanlines)
    MOV R1, #192, TYPE_UINT8
  display_loop:
    // Display processing here
    SUB R1, #1, TYPE_UINT8
    JNE display_loop
    
    // Overscan
    MOV R0, #0, TYPE_UINT8
    MOV [TIA_VBLANK], R0, TYPE_UINT8
  }
end;
```

### Memory Management

```pascal
// Custom memory allocator
procedure AllocateBlock(Size: Integer; var Result: Pointer);
begin
  asm {
    MOV R0, [Size], TYPE_INT32
    MOV R1, [HeapPtr], TYPE_PTR    // Current heap pointer
    MOV R2, R1, TYPE_PTR           // Save old pointer
    ADD R1, R0, TYPE_PTR           // New heap pointer
    MOV [HeapPtr], R1, TYPE_PTR    // Update heap pointer
    MOV [Result], R2, TYPE_PTR     // Return old pointer
  }
end;
```

## Implementation Notes

### ANTLR Integration

The assembly grammar is integrated into each language frontend:

1. **Grammar import** - Each language imports the VirtualAssembly grammar
2. **Parser rules** - Assembly blocks are parsed as separate AST nodes
3. **Semantic analysis** - Assembly nodes are processed during semantic analysis
4. **Code generation** - Assembly nodes generate LLIR instructions

### Error Recovery

Assembly parsing includes robust error recovery:

1. **Syntax errors** - Continue parsing after syntax errors
2. **Semantic errors** - Report type and reference errors
3. **Recovery strategies** - Skip invalid statements when possible
4. **Error context** - Provide detailed error messages with line numbers

### Testing Strategy

Assembly testing includes:

1. **Syntax validation** - Test all assembly constructs
2. **Type checking** - Verify type system integration
3. **Code generation** - Test LLIR output
4. **Optimization** - Verify optimization passes
5. **Target-specific** - Test assembly on each target platform

### Future Enhancements

Planned improvements to inline assembly:

1. **Macro support** - Define reusable assembly macros
2. **Conditional assembly** - Platform-specific assembly code
3. **Register allocation control** - Fine-grained register management
4. **Instruction scheduling hints** - Guide the optimizer
5. **Debug information** - Generate debug info for assembly code

## See Also

- [LLIR Design Document](LLIR.md) - Low-level IR and virtual machine architecture
- [LLIR ISA Specification](LLIR_ISA.md) - Complete instruction set reference
- [Compiler Architecture](compiler_architecture.md) - Compilation pipeline and optimization
- [Type System](TypeSystem.md) - Type system and type checking
- [Hardware Abstraction Layer](../api/hal/HAL.md) - Platform-specific hardware interfaces