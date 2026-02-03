# LLIR Instruction Set Architecture (ISA) Specification

## Overview

This document provides the complete specification for the LLIR instruction set. The LLIR is an accumulator-based virtual ISA designed for efficient threaded code execution across 2nd-5th generation gaming consoles.

## Instruction Format

### Basic Instruction Structure
```
[Opcode] [Operand1] [Operand2] [Width] [Flags]
```

- **Opcode**: Operation code (1 byte in bytecode)
- **Operand1**: Primary operand (register, immediate, or address)
- **Operand2**: Secondary operand (for binary operations)
- **Width**: Operation width (TYPE_INT8, TYPE_INT16, TYPE_INT32, TYPE_INT64)
- **Flags**: Optional modifier flags

### Width Types
```
TYPE_INT8   - 8-bit signed integer
TYPE_INT16  - 16-bit signed integer  
TYPE_INT32  - 32-bit signed integer
TYPE_INT64  - 64-bit signed integer
TYPE_UINT8  - 8-bit unsigned integer
TYPE_UINT16 - 16-bit unsigned integer
TYPE_UINT32 - 32-bit unsigned integer
TYPE_UINT64 - 64-bit unsigned integer
TYPE_PTR    - Pointer (target-dependent width)
TYPE_BOOL   - Boolean (1 byte)
TYPE_FLOAT32 - 32-bit floating point
TYPE_FLOAT64 - 64-bit floating point
```

## Register Set

### Virtual Registers
```
A    - Primary accumulator register
R0   - General purpose register 0
R1   - General purpose register 1
R2   - General purpose register 2
R3   - General purpose register 3
R4   - General purpose register 4
R5   - General purpose register 5
R6   - General purpose register 6
R7   - General purpose register 7
R8   - General purpose register 8
R9   - General purpose register 9
R10  - General purpose register 10
R11  - General purpose register 11
R12  - General purpose register 12
R13  - General purpose register 13
R14  - General purpose register 14
R15  - General purpose register 15
PC   - Program counter (implicit)
SP   - Stack pointer (implicit)
FLAGS - Status flags (implicit)
```

## Instruction Categories

### 1. Data Movement Instructions

#### LOAD - Load from Memory
```
LOAD  Rd, [address], width
LOAD  Rd, [Rs+offset], width
LOAD  Rd, immediate, width
```
**Description**: Load data from memory or immediate into register
**Flags**: Z, N set based on loaded value
**Examples**:
```
LOAD  A, [0x1000], TYPE_INT16     ; Load 16-bit from address
LOAD  R0, [R1+4], TYPE_INT32      ; Load with offset
LOAD  A, #42, TYPE_INT8           ; Load immediate
```

#### STORE - Store to Memory
```
STORE [address], Rs, width
STORE [Rd+offset], Rs, width
```
**Description**: Store register data to memory
**Flags**: No flags affected
**Examples**:
```
STORE [0x2000], A, TYPE_INT16     ; Store 16-bit to address
STORE [R0+8], R1, TYPE_INT32      ; Store with offset
```

#### MOV - Register Transfer
```
MOV   Rd, Rs, width
MOV   Rd, immediate, width
```
**Description**: Copy data between registers or load immediate
**Flags**: Z, N set based on moved value
**Examples**:
```
MOV   A, R0, TYPE_INT32           ; Copy register
MOV   R1, #100, TYPE_INT16        ; Load immediate
```

#### XCHG - Exchange
```
XCHG  Rd, [address], width
XCHG  Rd, Rs, width
```
**Description**: Exchange values between register and memory, or between registers
**Flags**: Z, N set based on value in Rd after exchange
**Examples**:
```
XCHG  A, [var_x], TYPE_INT32       ; Exchange A with memory variable
XCHG  R0, R1, TYPE_INT16           ; Exchange two registers
```
**Notes**: Atomic operation - useful for data exchange between asynchronous processors (e.g. Master/Slave CPUs)

#### PUSH/POP - Stack Operations
```
PUSH  Rs, width
POP   Rd, width
```
**Description**: Push register to stack or pop from stack
**Flags**: No flags affected
**Examples**:
```
PUSH  A, TYPE_INT32                ; Push accumulator
POP   R0, TYPE_INT16               ; Pop to register
```

### 2. Arithmetic Instructions

#### ADD - Addition
```
ADD   Rd, Rs, width
ADD   Rd, immediate, width
```
**Description**: Add source to destination
**Flags**: Z, N, C, V set based on result
**Examples**:
```
ADD   A, R0, TYPE_INT16            ; A += R0
ADD   R1, #5, TYPE_INT8            ; R1 += 5
```

#### SUB - Subtraction
```
SUB   Rd, Rs, width
SUB   Rd, immediate, width
```
**Description**: Subtract source from destination
**Flags**: Z, N, C, V set based on result
**Examples**:
```
SUB   A, R1, TYPE_INT32            ; A -= R1
SUB   R0, #10, TYPE_INT8           ; R0 -= 10
```

#### MUL - Multiplication
```
MUL   Rd, Rs, width
MUL   Rd, immediate, width
```
**Description**: Multiply destination by source
**Flags**: Z, N, V set based on result (C undefined)
**Examples**:
```
MUL   A, R2, TYPE_INT16            ; A *= R2
MUL   R0, #3, TYPE_INT32           ; R0 *= 3
```

#### DIV - Division
```
DIV   Rd, Rs, width
DIV   Rd, immediate, width
```
**Description**: Divide destination by source
**Flags**: Z, N set based on result (C, V undefined)
**Notes**: Division by zero results in undefined behavior
**Examples**:
```
DIV   A, R3, TYPE_INT16            ; A /= R3
DIV   R1, #2, TYPE_INT8            ; R1 /= 2
```

#### MOD - Modulo
```
MOD   Rd, Rs, width
MOD   Rd, immediate, width
```
**Description**: Remainder of division
**Flags**: Z, N set based on result
**Examples**:
```
MOD   A, R4, TYPE_INT32            ; A %= R4
MOD   R0, #7, TYPE_INT8            ; R0 %= 7
```

#### INC/DEC - Increment/Decrement
```
INC   Rd, width
DEC   Rd, width
```
**Description**: Add or subtract 1 from register
**Flags**: Z, N, C, V set based on result
**Examples**:
```
INC   A, TYPE_INT16                ; A++
DEC   R0, TYPE_INT32               ; R0--
```

### 3. Bitwise Instructions

#### AND - Logical AND
```
AND   Rd, Rs, width
AND   Rd, immediate, width
```
**Description**: Bitwise AND operation
**Flags**: Z, N set based on result (C, V cleared)
**Examples**:
```
AND   A, R1, TYPE_INT8             ; A &= R1
AND   R0, #0xFF, TYPE_INT16        ; R0 &= 0xFF
```

#### OR - Logical OR
```
OR    Rd, Rs, width
OR    Rd, immediate, width
```
**Description**: Bitwise OR operation
**Flags**: Z, N set based on result (C, V cleared)
**Examples**:
```
OR    A, R2, TYPE_INT32            ; A |= R2
OR    R1, #0x80, TYPE_INT8         ; R1 |= 0x80
```

#### XOR - Logical XOR
```
XOR   Rd, Rs, width
XOR   Rd, immediate, width
```
**Description**: Bitwise XOR operation
**Flags**: Z, N set based on result (C, V cleared)
**Examples**:
```
XOR   A, R3, TYPE_INT16            ; A ^= R3
XOR   R0, #0xFF, TYPE_INT8         ; R0 ^= 0xFF
```

#### NOT - Logical NOT
```
NOT   Rd, width
```
**Description**: Bitwise NOT operation
**Flags**: Z, N set based on result (C, V cleared)
**Examples**:
```
NOT   A, TYPE_INT32                ; A = ~A
NOT   R1, TYPE_INT8                ; R1 = ~R1
```

#### SHL/SHR - Shift Operations
```
SHL   Rd, count, width
SHR   Rd, count, width
```
**Description**: Shift left/right by specified count
**Flags**: Z, N, C set based on result (V cleared)
**Examples**:
```
SHL   A, #1, TYPE_INT16            ; A <<= 1
SHR   R0, #2, TYPE_INT8            ; R0 >>= 2
```

#### ROL/ROR - Rotate Operations
```
ROL   Rd, count, width
ROR   Rd, count, width
```
**Description**: Rotate left/right through carry
**Flags**: Z, N, C set based on result (V cleared)
**Examples**:
```
ROL   A, #1, TYPE_INT8             ; Rotate left through carry
ROR   R1, #3, TYPE_INT32           ; Rotate right through carry
```

### 4. Comparison Instructions

#### CMP - Compare
```
CMP   Rd, Rs, width
CMP   Rd, immediate, width
```
**Description**: Compare registers (subtract without storing result)
**Flags**: Z, N, C, V set based on comparison result
**Examples**:
```
CMP   A, R0, TYPE_INT16            ; Compare A with R0
CMP   R1, #100, TYPE_INT8          ; Compare R1 with 100
```

#### TEST - Test Bits
```
TEST  Rd, Rs, width
TEST  Rd, immediate, width
```
**Description**: Bitwise AND without storing result (for testing)
**Flags**: Z, N set based on AND result (C, V cleared)
**Examples**:
```
TEST  A, #0x80, TYPE_INT8          ; Test if high bit set
TEST  R0, R1, TYPE_INT32           ; Test overlapping bits
```

### 5. Control Flow Instructions

#### JUMP - Unconditional Jump
```
JUMP  label
JUMP  [address]                    ; Indirect jump
```
**Description**: Transfer control to target address
**Flags**: No flags affected
**Examples**:
```
JUMP  loop_start                   ; Jump to label
JUMP  [R0]                         ; Indirect jump through register
```

#### Conditional Jumps
```
JUMPZ label                        ; Jump if zero flag set
JUMPNZ label                       ; Jump if zero flag not set
JUMPN label                        ; Jump if negative flag set
JUMPP label                        ; Jump if positive (not negative)
JUMPC label                        ; Jump if carry flag set
JUMPNC label                       ; Jump if carry flag not set
JUMPV label                        ; Jump if overflow flag set
JUMPNV label                       ; Jump if overflow flag not set
```
**Description**: Conditional jumps based on flag states
**Flags**: No flags affected
**Examples**:
```
CMP   A, #0, TYPE_INT32
JUMPZ zero_case                    ; Jump if A == 0

TEST  A, #1, TYPE_INT8
JUMPNZ bit_set                     ; Jump if bit 0 is set
```

#### CALL/RET - Subroutine Calls
```
CALL  label
CALL  [address]                    ; Indirect call
RET                                ; Return from subroutine
```
**Description**: Subroutine call and return
**Flags**: No flags affected
**Stack Behavior**: CALL pushes return address, RET pops it
**Examples**:
```
CALL  subroutine_name              ; Call subroutine
CALL  [R0]                         ; Indirect call through register
RET                                ; Return from current subroutine
```

### 6. Type Conversion Instructions

#### CAST - Type Conversion
```
CAST  Rd, source_width, target_width
```
**Description**: Convert between different data types
**Flags**: Z, N set based on converted value
**Examples**:
```
CAST  A, TYPE_INT16, TYPE_INT8     ; Truncate 16-bit to 8-bit
CAST  R0, TYPE_INT8, TYPE_INT32    ; Sign-extend 8-bit to 32-bit
```

#### SIGN_EXTEND - Sign Extension
```
SIGN_EXTEND Rd, from_width, to_width
```
**Description**: Sign-extend smaller type to larger type
**Flags**: Z, N set based on extended value
**Examples**:
```
SIGN_EXTEND A, TYPE_INT8, TYPE_INT16   ; Sign-extend 8-bit to 16-bit
```

#### ZERO_EXTEND - Zero Extension
```
ZERO_EXTEND Rd, from_width, to_width
```
**Description**: Zero-extend smaller type to larger type
**Flags**: Z, N set based on extended value
**Examples**:
```
ZERO_EXTEND A, TYPE_INT8, TYPE_INT32   ; Zero-extend 8-bit to 32-bit
```

#### TRUNCATE - Truncate
```
TRUNCATE Rd, from_width, to_width
```
**Description**: Truncate larger type to smaller type
**Flags**: Z, N set based on truncated value
**Examples**:
```
TRUNCATE A, TYPE_INT32, TYPE_INT8     ; Truncate 32-bit to 8-bit
```

### 7. Memory Block Instructions

#### MEMCPY - Memory Copy
```
MEMCPY [dest_addr], [src_addr], count, width
```
**Description**: Copy block of memory
**Flags**: No flags affected
**Examples**:
```
MEMCPY [R0], [R1], #100, TYPE_INT8   ; Copy 100 bytes
```

#### MEMSET - Memory Set
```
MEMSET [dest_addr], value, count, width
```
**Description**: Fill block of memory with value
**Flags**: No flags affected
**Examples**:
```
MEMSET [R0], #0, #256, TYPE_INT8     ; Clear 256 bytes
```

#### MEMCMP - Memory Compare
```
MEMCMP [addr1], [addr2], count, width
```
**Description**: Compare two memory blocks
**Flags**: Z, N, C set based on comparison result
**Examples**:
```
MEMCMP [R0], [R1], #50, TYPE_INT16   ; Compare 50 16-bit values
```

### 8. Special Instructions

#### NOP - No Operation
```
NOP
```
**Description**: Do nothing (useful for alignment, timing)
**Flags**: No flags affected

#### BARRIER - Memory Barrier
```
BARRIER
```
**Description**: Ensures all memory operations complete before next instruction.
**Flags**: No flags affected
**Notes**: Essential for coordinating DMA transfers and asynchronous coprocessor access.

#### SYNC - Processor Synchronization
```
SYNC
```
**Description**: Synchronization point for co-executing processors.
**Flags**: No flags affected
**Notes**: Used for Master/Slave task synchronization (e.g., Sega Saturn Dual SH-2).

#### DEBUG - Debug Information
```
DEBUG  value
```
**Description**: Emit debug information
**Flags**: No flags affected
**Notes**: May be compiled out in release builds

## Procedure and Function Support

### PROC/ENDPROC - Procedure Definition
```
PROC  procedure_name
    ; Procedure body
ENDPROC
```
**Description**: Define procedure boundaries
**Flags**: No flags affected

### PARAM/LOCAL - Parameter and Local Variables
```
PARAM param_name, width, type
LOCAL  local_name, width, type
```
**Description**: Declare procedure parameters and locals
**Flags**: No flags affected

## Superinstruction Generation

### Common Patterns
The compiler can generate superinstructions for common patterns:

#### Load-Modify-Store
```
SUPER_ADD_MEMORY [addr], immediate, width
; Equivalent to:
; LOAD A, [addr], width
; ADD A, immediate, width  
; STORE [addr], A, width
```

#### Compare and Branch
```
SUPER_JUMPZ_IF_EQUAL Rd, immediate, width, label
; Equivalent to:
; CMP Rd, immediate, width
; JUMPZ label
```

#### Function Call Patterns
```
SUPER_CALL_2ARG func_addr, arg1, arg2, width
; Equivalent to optimized call sequence
```

## Opcode Assignment

### Opcode Ranges
```
0x00-0x1F: Data movement instructions
0x20-0x3F: Arithmetic instructions  
0x40-0x5F: Bitwise instructions
0x60-0x7F: Comparison instructions
0x80-0x9F: Control flow instructions
0xA0-0xBF: Type conversion instructions
0xC0-0xDF: Memory block instructions
0xE0-0xEF: Special instructions
0xF0-0xFF: Superinstructions (game-specific)
```

### Specific Opcodes
```
0x01: LOAD
0x02: STORE
0x03: MOV
0x04: XCHG
0x05: PUSH
0x06: POP

0x20: ADD
0x21: SUB
0x22: MUL
0x23: DIV
0x24: MOD
0x25: INC
0x26: DEC

0x40: AND
0x41: OR
0x42: XOR
0x43: NOT
0x44: SHL
0x45: SHR
0x46: ROL
0x47: ROR

0x60: CMP
0x61: TEST

0x80: JUMP
0x81: JUMPZ
0x82: JUMPNZ
0x83: JUMPN
0x84: JUMPP
0x85: JUMPC
0x86: JUMPNC
0x87: JUMPV
0x88: JUMPNV
0x89: CALL
0x8A: RET

0xA0: CAST
0xA1: SIGN_EXTEND
0xA2: ZERO_EXTEND
0xA3: TRUNCATE

0xC0: MEMCPY
0xC1: MEMSET
0xC2: MEMCMP

0xE0: NOP
0xE1: BARRIER
0xE2: SYNC
0xE3: DEBUG
```

## Backend Implementation Guidelines

### 8-bit Targets (Atari 2600, NES)
- Map A register to accumulator
- Map R0-R2 to X, Y, and zero-page locations
- Implement multi-width operations with multiple instructions
- Optimize for zero-page usage
- XCHG implemented with temporary storage (no native exchange)

### 16-bit Targets (Genesis, SNES)
- Map A register to primary accumulator
- Map R0-R7 to data registers
- Use native 16-bit operations where available
- Implement 32/64-bit operations with multiple instructions
- XCHG maps to EXG instruction where available

### 32/64-bit Targets (N64, PlayStation)
- Map registers to physical registers.
- **Native Floating Point**: Use Coprocessor 1 (FPU) for `TYPE_FLOAT32/64` operations.
- Optimize for pipelining and cache performance.
- Consider vector operations where available.
- XCHG implemented with register swap or temporary storage.

## Performance Considerations

### Instruction Frequency Optimization
- Prioritize fast paths for common operations (LOAD, ADD, CMP)
- Optimize conditional jumps for branch prediction
- Minimize flag updates where not needed

### Memory Access Patterns
- Prefer sequential memory access
- Minimize indirection where possible
- Consider cache line boundaries

### Register Usage
- Keep frequently used values in registers
- Minimize register spills to memory
- Use register allocation algorithms

### Superinstruction Performance
- **Normal Function Call**: ~20-50 cycles (dispatch overhead + call/ret)
- **Generated Superinstruction**: ~5-15 cycles (direct implementation)
- **Hand-Optimized Superinstruction**: ~3-8 cycles (assembly-tuned)
- Performance gains vary by target and dispatch technique

## Inline Assembly Integration

The LLIR ISA serves as the instruction set for inline assembly in GameVM frontend languages. Developers can write LLIR instructions directly using the assembly syntax, which are then parsed and validated against this specification.

### Assembly Syntax Mapping

Inline assembly uses the same instruction format as defined in this specification:

```pascal
// Inline assembly example
asm {
    LOAD R0, variable, TYPE_INT32
    MUL R0, R0, #42, TYPE_INT32
    STORE result, R0, TYPE_INT32
}
```

### Type Specifiers in Assembly

Assembly instructions include explicit type specifiers as defined in the width section:

- **TYPE_INT8, TYPE_INT16, TYPE_INT32, TYPE_INT64** - Signed integers
- **TYPE_UINT8, TYPE_UINT16, TYPE_UINT32, TYPE_UINT64** - Unsigned integers
- **TYPE_FLOAT32, TYPE_FLOAT64** - IEEE-754 Floating point
- **TYPE_PTR** - Pointer values
- **TYPE_BOOL** - Boolean values

### Register Usage

Inline assembly follows the primary register file conventions:

- **A**: Primary Accumulator
- **R0-R15**: General purpose registers
- **SP**: Stack pointer
- **PC**: Program counter (read-only)
- **FLAGS**: Status flags (implicit use)

For detailed information about inline assembly syntax, language integration, and examples, see the [Inline Assembly](InlineAssembly.md) document.

## See Also

- [LLIR Design Document](LLIR.md) - Architecture overview and inline assembly
