# Low-Level Intermediate Representation (LLIR)

## Overview
The Low-Level Intermediate Representation (LLIR) is a hardware-agnostic virtual machine design that serves as the final compilation target before generating native machine code or threaded code interpreters. It is specifically designed as an accumulator-based architecture to optimize for:

1. **Efficient threaded code dispatch** (DTC, ITC, TTC) on constrained hardware
2. **Natural mapping to 8-bit/16-bit processors** (6502, Z80, 68000)
3. **Scalable performance** on modern register-rich targets
4. **Bespoke VM generation** with game-specific interpreter optimization

## Design Philosophy

The LLIR is designed around a singular thought experiment: **"If you could go back in time to the 8-bit era and design a CPU that was 'forward compatible'—knowing how hardware would evolve into the 32/64-bit eras—how would it look?"**

### 1. The Portability Goal
A game written for the **Atari 2600** that utilizes only the Standard Library and the HAL should be portable to the **PlayStation** or **Nintendo 64** with a simple compiler flag change and a recompile. The LLIR exists to bridge this gap by abstracting the physical hardware while preserving the deterministic behavior (overflow, carry, memory limits) of the 8-bit roots.

### 2. Core Principles
1. **Width-Awareness**: Every instruction is explicitly typed by width (8, 16, 32, 64-bit). The backend ensures that an 8-bit ADD on a 64-bit MIPS processor behaves identically to an 8-bit ADD on a 6502.
2. **Hybrid Accumulator Model**: Features a lead register (`A`) for 8-bit efficiency and a pool of general registers (`R0-R15`) for 32/64-bit register-rich architectures.
3. **Bespoke Interpretation**: The ISA is designed to be emitted as optimized, non-switched machine code (via the [Internal Assembly API](InternalAssemblyAPI.md)), making the virtual "CPU" feel as fast as a native one.
4. **Intrinsic Promotion**: Blurs the line between functions and opcodes, allowing complex game logic (like `MoveSprites`) to be promoted to a native-speed "Superinstruction."

## Virtual Machine Architecture

### Virtual Register File

| Register | Description                                    |
| -------- | ---------------------------------------------- |
| `A`      | Primary accumulator register (8/16/32/64-bit)  |
| `R0-R15` | General purpose registers (8/16/32/64-bit)     |
| `PC`     | Program counter (target-dependent width)       |
| `SP`     | Stack pointer (target-dependent width)         |
| `FLAGS`  | Status flags (Zero, Negative, Carry, Overflow) |

**Design Notes:**
- **A register** is the primary working register for most operations
- **R0-R15** provide additional storage for complex expressions and temporaries
- **Backend mapping**: Atari 2600 might map A→A, R0→X, R1→Y; N64 might map to physical registers
- **Width flexibility**: Each register can operate on 8, 16, 32, or 64-bit values

### Status Flags

| Flag | Description | Set Condition                       |
| ---- | ----------- | ----------------------------------- |
| `Z`  | Zero        | Result equals zero                  |
| `N`  | Negative    | Result has most significant bit set |
| `C`  | Carry       | Unsigned overflow/borrow            |
| `V`  | Overflow    | Signed overflow                     |

**Flag Behavior:**
- Arithmetic operations set all relevant flags
- Comparison operations set flags based on result
- Logical operations set Z and N flags
- Backends may optimize flag handling for target hardware

## Memory Model

### Virtual Memory Space

The LLIR uses a flat, byte-addressable virtual memory space:

```
- 32-bit virtual addresses (conceptual)
- Byte-addressable memory
- No hardware-specific regions (zero page, banks, etc.)
- Stack grows downward from high addresses
- Backend handles all memory mapping optimizations
```

### Memory Operations

Memory operations are width-aware and target-agnostic:

```
LOAD  Rd, [address], width    ; Load from memory to register
STORE [address], Rs, width    ; Store register to memory
PUSH  Rs, width              ; Push register to stack
POP   Rd, width              ; Pop from stack to register
```

**Backend Responsibilities:**
- **Atari 2600**: Map frequent accesses to zero page, implement bank switching
- **Genesis**: Use 68000 addressing modes, handle memory layout  
- **N64**: Use virtual memory, cache optimization

## Execution Models

### Threaded Code Support

LLIR is optimized for multiple threaded code execution models:

#### **Direct Threaded Code (DTC)**
- Each instruction points directly to implementation code
- Minimal dispatch overhead
- Excellent for register-poor targets

#### **Indirect Threaded Code (ITC)**  
- Instructions contain opcodes that index into dispatch table
- Compact bytecode representation
- Good balance of size and speed

#### **Token Threaded Code (TTC)**
- Single-byte tokens for maximum compactness
- Dispatch table lookup + jump
- Best for memory-constrained targets

#### **Subroutine Threaded Code (STC)**
- Instructions compiled to native subroutines
- CALL/RET dispatch pattern
- Good for complex instruction sequences

### Bespoke Interpreter Generation

The compiler can generate game-specific interpreters:

1. **Dead Code Elimination**: Only include instructions actually used
2. **Developer-Guided Superinstructions**: Functions marked with `[Super]` attribute
3. **Hand-Optimized Superinstructions**: Combine with inline assembly for maximum performance
4. **Target Optimization**: Tailor interpreter to specific hardware
5. **Size Optimization**: Minimize ROM footprint for constrained targets

## Developer-Guided Superinstructions

### Intent Signaling

Developers can signal superinstruction intent using attributes or keywords:

#### **Attribute Approach (Recommended)**
```pascal
[Super]
function FastAdd(var X, Y: Integer): Integer;
begin
  Result := X + Y;
end;

[Super]
procedure SwapVars(var X, Y: Integer);
var
  Temp: Integer;
begin
  Temp := X;
  X := Y;
  Y := Temp;
end;
```

#### **Keyword Approach (Alternative)**
```pascal
super function FastAdd(var X, Y: Integer): Integer;
begin
  Result := X + Y;
end;
```

### Compiler Criteria for Superinstruction Generation

Functions must meet specific requirements to become superinstructions:

#### **Structural Requirements**
- **Size Limit**: ≤ 5-10 LLIR instructions (configurable)
- **No Complex Control Flow**: No loops, recursion, or complex branching
- **Parameter Limit**: ≤ 3-4 parameters
- **Simple Variable Access**: No complex data structures or pointer arithmetic
- **Deterministic Execution**: No side effects that prevent inlining
- **Thread Safety**: No shared state modification

#### **Analysis Process**
```
Developer marks function as [Super]
        ↓
Compiler analyzes function structure
        ↓
Check against superinstruction criteria
        ↓
If passes → Generate as superinstruction
If fails → Fall back to normal function call with diagnostic
```

### Hand-Optimized Superinstructions

The ultimate performance optimization: combine `[Super]` with inline LLIR assembly.

#### **High-Performance Math Operations**
```pascal
[Super]
function FastMulAdd(var Result: Integer; A, B, C: Integer): Integer;
// Result = A * B + C
begin
  asm
    MOV R0, [A], TYPE_INT32      ; R0 = A
    MUL R0, [B], TYPE_INT32      ; R0 = A * B
    ADD R0, [C], TYPE_INT32      ; R0 = A * B + C
    MOV [Result], R0, TYPE_INT32 ; Store result
  end;
end;
```

#### **Graphics Kernel Optimizations**
```pascal
[Super]
function SetPixelFast(Screen: PByte; X, Y, Color: Integer);
// Optimized pixel plotting for display kernels
begin
  asm
    MOV R0, [Y], TYPE_INT16       ; R0 = Y
    SHL R0, #6, TYPE_INT16        ; R0 = Y * 64 (assuming 64-pixel width)
    ADD R0, [X], TYPE_INT16       ; R0 = Y * 64 + X
    MOV R1, [Screen], TYPE_PTR    ; R1 = screen pointer
    STORE [R1 + R0], [Color], TYPE_UINT8 ; screen[Y*64+X] = color
  end;
end;
```

#### **Game Logic Optimizations**
```pascal
[Super]
function UpdatePlayerPosition(var Player: TPlayer; DX, DY: Integer);
begin
  asm
    MOV R0, [Player], TYPE_PTR    ; R0 = player pointer
    LOAD A, [R0 + TPlayer.X], TYPE_INT16 ; A = player.x
    ADD A, [DX], TYPE_INT16        ; A = player.x + dx
    STORE [R0 + TPlayer.X], A, TYPE_INT16 ; player.x = A
    
    LOAD A, [R0 + TPlayer.Y], TYPE_INT16 ; A = player.y
    ADD A, [DY], TYPE_INT16        ; A = player.y + dy
    STORE [R0 + TPlayer.Y], A, TYPE_INT16 ; player.y = A
  end;
end;
```

#### **Bit Manipulation Superinstructions**
```pascal
[Super]
function SetBit(var Value: Byte; Bit: Integer): Byte;
begin
  asm
    LOAD A, [Value], TYPE_UINT8   ; A = value
    MOV R0, #1, TYPE_UINT8        ; R0 = 1
    SHL R0, [Bit], TYPE_UINT8      ; R0 = 1 << bit
    OR A, R0, TYPE_UINT8           ; A = value | (1 << bit)
    MOV [Value], A, TYPE_UINT8     ; value = A
  end;
end;
```

### Superinstruction Generation Process

#### **Successful Generation**
```
[Super] function FastAdd(X, Y: Integer): Integer;
begin
  Result := X + Y;
end;

↓ Compiler Analysis ↓

✅ Function meets criteria:
   - 3 LLIR instructions
   - No control flow
   - 2 parameters
   - Simple operations

↓ Generation ↓

Opcode: 0xF1 (assigned dynamically)
Implementation: LOAD A, [X]; ADD A, [Y]; MOV [Result], A
Dispatch Table Entry: [0xF1] → FastAdd_implementation
```

#### **Hand-Optimized Generation**
```
[Super]
function OptimizedSwap(var X, Y: Integer);
begin
  asm
    MOV R0, [X], TYPE_INT32
    XCHG R0, [Y], TYPE_INT32
    MOV [X], R0, TYPE_INT32
  end;
end;

↓ Compiler Analysis ↓

✅ Function meets criteria:
   - 3 LLIR instructions (inline assembly)
   - No control flow
   - 2 parameters
   - Hand-optimized assembly

↓ Generation ↓

Opcode: 0xF2 (assigned dynamically)
Implementation: MOV R0, [X]; XCHG R0, [Y]; MOV [X], R0
Dispatch Table Entry: [0xF2] → OptimizedSwap_implementation
```

#### **Failed Generation with Diagnostics**
```
[Super]
function ComplexMath(X, Y: Integer): Integer;
var
  I: Integer;
begin
  Result := 0;
  for I := 1 to X do
    Result := Result + Y;
end;

↓ Compiler Analysis ↓

❌ Function rejected for superinstruction:
   - Contains loop construct
   - Variable instruction count
   - Non-deterministic execution time

↓ Fallback ↓
Generated as normal function with warning:
"Warning: Function 'ComplexMath' marked as [Super] but rejected:
   - Contains loop construct (not allowed)
   - Consider simplifying or using normal function"
```

### Performance Benefits

#### **Normal Function Call Overhead**
```
CALL FastAdd, X, Y, Result    ; ~20-50 cycles depending on dispatch
```

#### **Superinstruction Execution**
```
FASTADD X, Y, Result         ; ~5-15 cycles (direct implementation)
```

#### **Hand-Optimized Superinstruction**
```
OPTIMIZED_SWAP X, Y          ; ~3-8 cycles (hand-tuned assembly)
```

### Compiler Integration

#### **Opcode Management**
```csharp
class SuperinstructionManager {
    private byte nextSuperOpcode = 0xF0;
    
    byte AllocateSuperOpcode() {
        return nextSuperOpcode++;
    }
    
    void RegisterSuperinstruction(string name, byte opcode, LLIRInstruction[] implementation) {
        superinstructions[opcode] = new Superinstruction {
            Name = name,
            Opcode = opcode,
            Implementation = implementation
        };
    }
}
```

#### **Validation Pipeline**
```csharp
class SuperinstructionValidator {
    ValidationResult ValidateForSuperinstruction(Function function) {
        if (function.InstructionCount > MaxInstructions)
            return ValidationResult.Fail("Too many instructions");
            
        if (ContainsLoops(function))
            return ValidationResult.Fail("Contains loop construct");
            
        if (function.Parameters.Count > MaxParameters)
            return ValidationResult.Fail("Too many parameters");
            
        return ValidationResult.Success();
    }
}
```

### Developer Experience

#### **Performance Engineering Workflow**
1. **Profile**: Identify performance bottlenecks
2. **Mark**: Add `[Super]` to critical functions
3. **Test**: Verify superinstruction generation
4. **Optimize**: Add inline assembly for maximum performance
5. **Validate**: Check cycle counts and ROM usage

#### **Debugging Support**
```pascal
// Compiler can generate diagnostics
{$SUPERINSTRUCTION_DIAGNOSTICS}
[Super]
function TestFunction(X: Integer): Integer;
begin
  Result := X * 2;
end;

// Compilation output:
// Info: Function 'TestFunction' generated as superinstruction:
//   - Opcode: 0xF1
//   - Size: 2 LLIR instructions
//   - Estimated cycles: 8 (Atari 2600 DTC)
//   - ROM savings: 12 bytes vs function call
```

This combination of **developer-guided superinstructions** and **hand-optimized inline assembly** gives developers unprecedented control over performance while maintaining cross-platform portability!

## Example LLIR Code

### Simple Arithmetic Operations
```llir
; Add two 16-bit numbers: result = a + b
LOAD  A, [a_addr], TYPE_INT16
ADD   A, [b_addr], TYPE_INT16
STORE [result_addr], A, TYPE_INT16
```

### Control Flow Example
```llir
; Simple loop: sum array elements
PROC sum_array
    LOAD  A, #0, TYPE_INT32          ; sum = 0
    LOAD  R0, [array_addr], TYPE_PTR ; ptr = array
    LOAD  R1, [array_size], TYPE_INT32 ; count = size
    
loop_start:
    CMP   R1, #0, TYPE_INT32         ; if count == 0
    JUMPNZ loop_end                 ;   jump to end
    
    LOAD  A, [R0], TYPE_INT32       ; load *ptr
    ADD   A, [sum_var], TYPE_INT32  ; sum += *ptr
    STORE [sum_var], A, TYPE_INT32   ; store sum
    
    ADD   R0, #4, TYPE_PTR           ; ptr += 4 (32-bit elements)
    SUB   R1, #1, TYPE_INT32         ; count--
    JUMP  loop_start
    
loop_end:
    RET
ENDPROC
```

### Backend Translation Examples

#### **Atari 2600 Backend (6502)**
```asm
; LOAD A, [addr], TYPE_INT16 becomes:
LDA addr_low
STA temp_low
LDA addr_high
STA temp_high

; ADD A, [addr], TYPE_INT16 becomes:
CLC
LDA temp_low
ADC addr_low
STA temp_low
LDA temp_high
ADC addr_high
STA temp_high
```

#### **Genesis Backend (68000)**
```asm
; LOAD A, [addr], TYPE_INT16 becomes:
MOVE.W (A0), D0

; ADD A, [addr], TYPE_INT16 becomes:
ADD.W (A1), D0
```

#### **N64 Backend (MIPS)**
```asm
; LOAD A, [addr], TYPE_INT16 becomes:
LH   $t0, 0($t1)

; ADD A, [addr], TYPE_INT16 becomes:
ADD  $t0, $t0, $t2
```

## Inline LLIR Assembly

### Portable Assembly Language

LLIR serves as a **portable assembly language** that developers can use directly in their source code for performance-critical sections. This provides the unique ability to write low-level optimizations that work across all target platforms.

### Language Integration

#### **Pascal Inline Assembly**
```pascal
procedure FastSwap(var X, Y: LongInt);
begin
  asm
    MOV R0, [X], TYPE_INT32     ; Load X into R0
    XCHG R0, [Y], TYPE_INT32    ; Exchange R0 with Y
    MOV [X], R0, TYPE_INT32     ; Store result back to X
  end;
end;

procedure OptimizedCopy(src, dst: Pointer; count: Integer);
begin
  asm
    MOV R0, [src], TYPE_PTR      ; R0 = source pointer
    MOV R1, [dst], TYPE_PTR      ; R1 = destination pointer
    MOV R2, [count], TYPE_INT32  ; R2 = byte count
    
copy_loop:
    LOAD A, [R0], TYPE_UINT8     ; Load byte from source
    STORE [R1], A, TYPE_UINT8    ; Store byte to destination
    ADD R0, #1, TYPE_PTR         ; Increment source pointer
    ADD R1, #1, TYPE_PTR         ; Increment destination pointer
    SUB R2, #1, TYPE_INT32       ; Decrement counter
    JUMPNZ copy_loop             ; Continue if not done
  end;
end;
```

### Inline Assembly Benefits

#### **1. Portable Performance**
- Same assembly code works on Atari 2600, Genesis, N64, etc.
- Backends optimize for target hardware automatically
- No platform-specific assembly knowledge required

#### **2. Hardware Access When Needed**
- Direct control over critical algorithms
- Cycle-precise optimization for display kernels
- Access to hardware-specific features through HAL

#### **3. Algorithm Optimization**
- Bit manipulation optimizations
- Memory operation fine-tuning
- Custom data structure operations

### Compiler Integration

#### **Variable Access**
```pascal
procedure Example;
var
  local_var: Integer;
  global_var: Integer;
begin
  asm
    MOV R0, [local_var], TYPE_INT32    ; Access local variable
    MOV R1, [global_var], TYPE_INT32   ; Access global variable
    MOV R2, [self.field], TYPE_INT16  ; Access object field
    MOV R3, [array[index]], TYPE_INT8 ; Access array element
  end;
end;
```

#### **Register Management**
```pascal
// Compiler automatically handles register preservation
// Developers can use any virtual registers (R0-R15, A)
procedure CriticalSection;
begin
  asm
    // Use registers freely - compiler saves/restores as needed
    MOV R0, #100, TYPE_INT32
    MOV R1, #200, TYPE_INT32
    ADD R0, R1, TYPE_INT32
    STORE [result], R0, TYPE_INT32
  end;
end;
```

### Backend Translation Examples

#### **Atari 2600 Backend (6502)**
```asm
; MOV R0, [X], TYPE_INT32 becomes:
LDA X_low
STA R0_low
LDA X_high  
STA R0_high
LDA X_bank
STA R0_bank

; XCHG R0, [Y], TYPE_INT32 becomes:
; (Uses temporary storage since 6502 has no XCHG)
STA temp
LDA Y_low
STA Y_low_new
LDA temp
STA Y_low
; ... repeat for high and bank bytes
```

#### **Genesis Backend (68000)**
```asm
; MOV R0, [X], TYPE_INT32 becomes:
MOVE.L (A0), D0

; XCHG R0, [Y], TYPE_INT32 becomes:
EXG D0, (A1)
```

#### **N64 Backend (MIPS)**
```asm
; MOV R0, [X], TYPE_INT32 becomes:
LW $t0, 0($t1)

; XCHG R0, [Y], TYPE_INT32 becomes:
MOVE $t2, $t0      ; temp = R0
LW $t0, 0($t3)     ; R0 = [Y]
SW $t2, 0($t3)     ; [Y] = temp
```

### Design Considerations

#### **Type Safety**
- Compiler validates width parameters
- Automatic type conversion where appropriate
- Clear error messages for type mismatches

#### **Register Allocation**
- Virtual registers map to physical registers per backend
- Compiler handles register spills automatically
- Developers can use any virtual register freely

#### **Optimization Integration**
- Inline assembly integrates with optimizer passes
- Dead code elimination works across assembly boundaries
- Superinstruction generation can include assembly patterns

## Integration with Higher-Level IR

The LLIR is generated from the Mid-Level IR (MLIR) through transformation passes:

1. **Type Lowering**: Convert high-level types to primitive operations
2. **Register Allocation**: Map MLIR temporaries to LLIR registers
3. **Instruction Selection**: Choose optimal LLIR instruction sequences
4. **Width Analysis**: Determine appropriate operation widths
5. **Optimization**: Peephole optimization and dead code elimination
6. **Code Generation**: Emit final LLIR bytecode or native code

## Performance Considerations

### Accumulator Model Benefits
- **Minimal Interpreter State**: Fewer registers to manage during dispatch
- **Efficient Threading**: Natural fit for DTC/ITC/TTC dispatch patterns
- **8-bit Target Optimization**: Direct mapping to accumulator-based hardware
- **Cache Friendly**: Smaller interpreter core fits in limited caches

### Backend Optimization Opportunities
- **Register Mapping**: Backends can optimize register usage for target hardware
- **Instruction Fusion**: Combine multiple LLIR instructions into native operations
- **Memory Layout**: Optimize memory access patterns for target architecture
- **Dispatch Optimization**: Tailor dispatch mechanism to target capabilities

### Superinstruction Generation
- **Pattern Detection**: Identify common instruction sequences during compilation
- **Custom Instructions**: Generate game-specific superinstructions
- **Interpreter Specialization**: Only include actually used instructions
- **Size vs Speed Tradeoff**: Balance interpreter size against execution speed

## Inline Assembly Support

The LLIR is the target for inline assembly written in GameVM frontend languages. Assembly blocks are parsed and transformed directly into LLIR instructions, maintaining the same level of control and optimization as hand-written LLIR.

### Assembly to LLIR Mapping

Inline assembly statements map directly to LLIR instructions:

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

### Benefits of LLIR-Based Assembly

1. **Unified pipeline** - Assembly goes through the same optimization passes as regular code
2. **Type safety** - Assembly operations participate in the type system
3. **Optimization** - Assembly can be optimized by the compiler
4. **Debugging** - Assembly integrates with debugging information
5. **Portability** - Assembly works across all supported targets

## See Also

- [LLIR ISA Specification](LLIR_ISA.md) - Complete instruction set reference
- [Inline Assembly Guide](InlineAssembly.md) - Portable assembly programming
- [HLIR Documentation](HLIR.md) - Higher-Level IR design
- [MLIR Documentation](MLIR.md) - Mid-Level IR design
