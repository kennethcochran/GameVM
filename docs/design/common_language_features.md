# Common Language Features and Constraints

This document describes the common features and constraints that apply to all languages supported by GameVM. These constraints ensure efficient compilation to retro gaming platforms while maintaining a modern development experience.

## Core Language Features

### Types and Variables

1. **Basic Types**
   - Integer (8-bit, 16-bit)
   - Fixed-point numbers (8.8, 16.16)
   - Boolean
   - Characters
   - Enums
   - Arrays (fixed-size)
   - Structs/Records

2. **Type System**
   - Static typing (either explicit or inferred)
   - No dynamic type changes
   - No runtime type checking
   - Compile-time type resolution

3. **Variables**
   - Local variables
   - Global variables (limited)
   - Constants
   - Static arrays
   - No dynamic allocation

### Control Flow

1. **Conditionals**
   - If/else statements
   - Switch/case statements
   - Conditional expressions

2. **Loops**
   - For loops (counted iterations)
   - While loops
   - Break and continue
   - No unbounded recursion

3. **Functions**
   - Named functions
   - Function parameters
   - Return values
   - Local functions
   - Limited call stack depth

### Memory Model

1. **Static Allocation**
   - All memory allocated at compile time
   - Fixed array sizes
   - No heap allocation
   - No garbage collection

2. **Memory Regions**
   - ROM (code and constants)
   - RAM (variables and stack)
   - Zero page (6502 platforms)
   - Memory-mapped I/O

3. **Data Structures**
   - Fixed-size arrays
   - Static structs/records
   - Bit fields
   - No pointers/references

## Common Constraints

### Resource Limitations

1. **Memory**
   - Limited global variables
   - Fixed stack size
   - No recursive data structures
   - No dynamic memory allocation

2. **Computation**
   - No floating-point arithmetic
   - Limited call stack depth
   - Bounded loop iterations
   - No exceptions/error handling

3. **Code Size**
   - Limited function size
   - Inline function restrictions
   - No large string literals
   - Code must fit in ROM banks

### Platform Constraints

1. **Hardware Access**
   - Direct memory-mapped I/O
   - Hardware register access
   - Interrupt handlers
   - Timing-critical code

2. **Banking**
   - ROM bank switching
   - RAM bank switching
   - Fixed bank sizes
   - Bank-aware calls

## Language-Specific Features

While each language maintains its familiar syntax, certain features are restricted or modified:

### Python
- No dynamic typing
- No list comprehensions
- No generators/iterators
- Limited standard library

### JavaScript
- No dynamic object creation
- No prototypes
- No closures
- Static typing required

### Lua
- No metatables
- No coroutines
- Static typing required
- No dynamic loading

### C#
- No garbage collection
- No delegates
- No LINQ
- Limited generics

### Common Optimizations

1. **Compile-Time**
   - Constant folding
   - Dead code elimination
   - Loop unrolling
   - Inline expansion

2. **Memory**
   - Stack frame optimization
   - Register allocation
   - Zero-page usage (6502)
   - Bank optimization

3. **Code Generation**
   - Specialized instructions
   - Platform-specific optimizations
   - Superinstructions
   - Common subexpression elimination

## Development Guidelines

1. **Best Practices**
   - Use compile-time constants
   - Avoid deep call stacks
   - Keep functions small
   - Use static arrays

2. **Performance Tips**
   - Use fixed-point math
   - Minimize bank switching
   - Reuse variables
   - Unroll critical loops

3. **Memory Management**
   - Preallocate arrays
   - Reuse memory buffers
   - Use bit fields
   - Share temporary variables

4. **Code Organization**
   - Group related functions
   - Minimize cross-bank calls
   - Keep critical code in one bank
   - Use meaningful constants
