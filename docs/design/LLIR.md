---
title: "Low-Level Intermediate Representation (LLIR)"
description: "Specification for the Low-Level Intermediate Representation and virtual machine design"
author: "GameVM Team"
created: "2025-09-20"
updated: "2025-09-20"
version: "1.0.0"
---

# Low-Level Intermediate Representation (LLIR)

## Overview
The Low-Level Intermediate Representation (LLIR) is a virtual machine design that serves as the final compilation target before generating native machine code. It is specifically designed as an accumulator-based architecture to closely model the behavior of classic 8-bit and 16-bit processors while providing a consistent target for compilation.

## Design Philosophy

The LLIR is designed with these key principles:

1. **Accumulator-Centric**: Operations primarily work with a single accumulator register, matching the architecture of many retro systems.
2. **Simple Addressing Modes**: Supports addressing modes common to 6502, Z80, and similar processors.
3. **Deterministic Execution**: Predictable timing and behavior for accurate cycle counting.
4. **Hardware Abstraction**: Abstracts away hardware-specific details while preserving performance characteristics.

## Virtual Machine Architecture

### Registers

| Register | Description                                      |
|----------|--------------------------------------------------|
| `A`      | 8/16-bit Accumulator (primary working register)  |
| `X`      | 8/16-bit Index Register X                        |
| `Y`      | 8/16-bit Index Register Y                        |
| `PC`     | Program Counter (16-bit)                         |
| `SP`     | Stack Pointer (8/16-bit)                         |
| `SR`     | Status Register (flags)                          |

### Status Register (SR) Flags

| Bit | Flag | Description                          |
|----|------|--------------------------------------|
| 7  | `N`  | Negative (sign)                      |
| 6  | `V`  | Overflow                             |
| 5  | `-`  | Unused                               |
| 4  | `B`  | Break (interrupt)                    |
| 3  | `D`  | Decimal mode (BCD operations)        |
| 2  | `I`  | Interrupt Disable                    |
| 1  | `Z`  | Zero                                 |
| 0  | `C`  | Carry                                |

## Instruction Set Architecture (ISA)

### Data Transfer

| Instruction | Opcode | Description                        | Cycles |
|-------------|--------|------------------------------------|--------|
| `LDA #imm`  | 0xA9   | Load accumulator with immediate    | 2      |
| `LDA addr`  | 0xAD   | Load accumulator from memory       | 4      |
| `STA addr`  | 0x8D   | Store accumulator to memory        | 4      |
| `TAX`       | 0xAA   | Transfer A to X                    | 2      |
| `TXA`       | 0x8A   | Transfer X to A                    | 2      |
| `TAY`       | 0xA8   | Transfer A to Y                    | 2      |
| `TYA`       | 0x98   | Transfer Y to A                    | 2      |

### Arithmetic and Logic

| Instruction | Opcode | Description                        | Cycles |
|-------------|--------|------------------------------------|--------|
| `ADC #imm`  | 0x69   | Add with carry                     | 2      |
| `SBC #imm`  | 0xE9   | Subtract with carry                | 2      |
| `AND #imm`  | 0x29   | Logical AND                        | 2      |
| `ORA #imm`  | 0x09   | Logical OR                         | 2      |
| `EOR #imm`  | 0x49   | Logical XOR                        | 2      |
| `CMP #imm`  | 0xC9   | Compare with accumulator           | 2      |

### Control Flow

| Instruction | Opcode | Description                        | Cycles |
|-------------|--------|------------------------------------|--------|
| `JMP addr`  | 0x4C   | Jump to address                    | 3      |
| `JSR addr`  | 0x20   | Jump to subroutine                 | 6      |
| `RTS`       | 0x60   | Return from subroutine             | 6      |
| `BNE rel`   | 0xD0   | Branch if not equal (Z=0)          | 2/3/4  |
| `BEQ rel`   | 0xF0   | Branch if equal (Z=1)              | 2/3/4  |

### Stack Operations

| Instruction | Opcode | Description                        | Cycles |
|-------------|--------|------------------------------------|--------|
| `PHA`       | 0x48   | Push accumulator                   | 3      |
| `PLA`       | 0x68   | Pull accumulator                   | 4      |
| `PHP`       | 0x08   | Push processor status              | 3      |
| `PLP`       | 0x28   | Pull processor status              | 4      |

## Memory Model

The VM uses a 64KB address space (16-bit addressing) with the following layout:

```
$0000-$00FF: Zero Page (direct addressing)
$0100-$01FF: Stack
$0200-$BFFF: General Purpose RAM
$C000-$FFFF: ROM/Banked Memory (system dependent)
```

## Implementation Notes

### Cycle Counting
Each instruction includes cycle counts that match or approximate the timing of real hardware. This is crucial for:
- Accurate emulation of timing-sensitive code
- Proper synchronization with video and audio hardware
- Cycle-exact emulation when required

### Bank Switching
For systems with banked memory (e.g., NES mappers), the VM provides special instructions to manage memory banks:

```
BANK n        ; Switch to memory bank n (0-255)
BANK_GET      ; Get current bank number
```

### Interrupt Handling

```
SEI           ; Disable interrupts
CLI           ; Enable interrupts
BRK           ; Software interrupt
RTI           ; Return from interrupt
```

## Example Code

### Simple Addition
```asm
; Add two numbers: A = 5 + 3
LDA #5        ; Load 5 into accumulator
CLC           ; Clear carry
ADC #3        ; Add 3 to accumulator
STA $0200     ; Store result at $0200
```

### Memory Copy
```asm
; Copy 256 bytes from $1000 to $2000
LDX #0        ; Initialize counter
loop:
LDA $1000,X   ; Load byte from source
STA $2000,X   ; Store to destination
INX           ; Increment counter
BNE loop      ; Loop until X wraps to 0
```

## Integration with Higher-Level IR

The LLIR is generated from the Mid-Level IR (MLIR) through a series of lowering passes:

1. **Register Allocation**: Maps virtual registers to physical registers or stack locations
2. **Instruction Selection**: Chooses optimal instruction sequences
3. **Peephole Optimization**: Performs local optimizations
4. **Code Emission**: Generates final bytecode

## Performance Considerations

- **Register Pressure**: The accumulator architecture may require more instructions for complex expressions
- **Memory Access**: Minimize zero page and stack usage for better performance
- **Branch Prediction**: Use short branches for critical loops
- **Cycle Counting**: Be mindful of instruction timing for accurate emulation

## See Also

- [HLIR Documentation](HLIR.md) - Higher-Level IR design
- [MLIR Documentation](MLIR.md) - Mid-Level IR design
- [Internal Assembly API](InternalAssemblyAPI.md) - For direct code generation
