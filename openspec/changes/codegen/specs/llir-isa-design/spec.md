# Llir-Isa-Design Specification

## Purpose
Describes the aspirational low-level intermediate representation (LLIR) design: an accumulator-based, hardware-agnostic virtual machine that serves as the final compilation target before native or threaded-code generation, with width-aware instructions, a hybrid accumulator/general-register model, bespoke interpreter generation, developer-guided superinstructions, and portable inline assembly. Aspirational — not yet implemented.

## Requirements

### Requirement: Width-Aware Instructions
Every LLIR instruction MUST be explicitly typed by width (8, 16, 32, or 64-bit), and the backend MUST ensure that an operation of a given width behaves identically on every target (e.g. an 8-bit ADD on a 64-bit MIPS behaves identically to an 8-bit ADD on a 6502).

#### Scenario: Width-typed memory operation
- **WHEN** a LOAD/STORE memory operation carries explicit width
- **THEN** the operation loads or stores only that many bits and behaves identically on every target

#### Scenario: Width-consistent arithmetic across targets
- **WHEN** an arithmetic operation of a specific width executes
- **THEN** its behavior (overflow, carry, memory limits) is preserved identically across backends of differing host widths

### Requirement: Hybrid Accumulator Model
The design MUST feature a lead accumulator register (`A`) for 8-bit efficiency and a pool of general-purpose registers (`R0`-`R15`) for 32/64-bit register-rich architectures.

#### Scenario: Lead accumulator for 8-bit efficiency
- **WHEN** 8-bit operations dominate a workload
- **THEN** the lead accumulator register (`A`) provides efficient 8-bit operation

#### Scenario: General registers for wide architectures
- **WHEN** a register-rich target executes 32/64-bit work
- **THEN** the R0-R15 general-purpose register pool is used

### Requirement: Virtual Register File
The virtual machine MUST expose a register file including attributes accumulator `A`, general-purpose registers `R0`-`R15`, `PC`, `SP`, and `FLAGS`, with a `A` register that can operate at 8/16/32/64-bit widths.

#### Scenario: Virtual register file layout
- **WHEN** code references a virtual register
- **THEN** it resolves to either the `A` accumulator, one of the `R0`-`R15` general-purpose registers, `PC`, `SP`, or `FLAGS`

#### Scenario: Width flexible registers
- **WHEN** a register is used with a given width
- **THEN** each register operates on 8, 16, 32, or 64-bit values

#### Scenario: Backend register mapping
- **WHEN** targeting a concrete platform
- **THEN** the backend maps virtual registers to physical registers (e.g. Atari 2600 maps A→A, R0→X, R1→Y; N64 maps to physical registers)

### Requirement: Status Flags
The design MUST define status flags (Zero, Negative, Carry, Overflow) whose set conditions are defined (Z on result zero, N on most-significant-bit of the result, C on unsigned overflow/borrow, V on signed overflow), with reported behavior for arithmetic, comparison, and logical operations.

#### Scenario: Arithmetic flag setting
- **WHEN** an arithmetic operation executes
- **THEN** all relevant flags are set

#### Scenario: Comparison flag setting
- **WHEN** a comparison operation executes
- **THEN** flags are set based on the result

#### Scenario: Logical flag setting
- **WHEN** a logical operation executes
- **THEN** the Z and N flags are set

#### Scenario: Backend flag optimization
- **WHEN** a backend generates flag-handling
- **THEN** it may optimize flag handling for the target hardware

### Requirement: Flat Virtual Memory Space
LLIR MUST use a flat, byte-addressable 32-bit virtual memory space with no hardware-specific regions, the stack growing downward from high addresses, and all memory-mapping optimizations delegated to the backend.

#### Scenario: Flat byte-addressable addressing
- **WHEN** code addresses memory
- **THEN** addressing is flat, byte-addressable, and 32-bit virtual, with no zero-page or bank regions exposed in the ISA

#### Scenario: Stack growth direction
- **WHEN** the stack is used
- **THEN** the stack grows downward from high addresses

#### Scenario: Backend memory mapping
- **WHEN** LLIR memory operations target a specific platform
- **THEN** the backend handles memory-mapping optimizations (e.g. Atari 2600 maps frequent accesses to zero page and implements bank switching; N64 uses virtual memory and cache optimization)

### Requirement: Width-Aware Memory Operations
Memory operations (LOAD/STORE/PUSH/POP) MUST be width-aware and target-agnostic.

#### Scenario: Loading from memory at a width
- **WHEN** load executes
- **THEN** it loads from memory into a register at the specified width

#### Scenario: Storing to memory at a width
- **WHEN** store executes
- **THEN** it stores the register to memory at the specified width

#### Scenario: Stack push and pop
- **WHEN** push or pop executes
- **THEN** it pushes a register to or pops a register from the stack at the specified width

### Requirement: Threaded Code Support
The design MUST optimize for multiple threaded code execution models: Direct Threaded Code (DTC), Indirect Threaded Code (ITC), Token Threaded Code (TTC), and Subroutine Threaded Code (STC).

#### Scenario: Direct Threaded Code dispatch
- **WHEN** DTC is used
- **THEN** each instruction points directly to its implementation code with minimal dispatch overhead, suiting register-poor targets

#### Scenario: Indirect Threaded Code dispatch
- **WHEN** ITC is used
- **THEN** instructions contain indices opcodes into a dispatch table, giving compact bytecode and a balance of size and speed

#### Scenario: Token Threaded Code dispatch
- **WHEN** TTC is used
- **THEN** single-byte tokens provide maximum compactness via dispatch-table lookup plus jump, best for memory-constrained targets

#### Scenario: Subroutine Threaded Code dispatch
- **WHEN** STC is used
- **THEN** instructions are compiled to native subroutines with a CALL/RET dispatch pattern, good for complex instruction sequences

### Requirement: Bespoke Interpreter Generation
The compiler MUST generate game-specific interpreters through dead code elimination, developer-guided superinstructions, hand-optimization, target tailoring, and size minimization.

#### Scenario: Dead code elimination in interpreters
- **WHEN** generating an interpreter
- **THEN** only instructions actually used in the program are included

#### Scenario: Developer-guided superinstructions in interpreters
- **WHEN** a function is marked with the `[Super]` attribute and used
- **THEN** it can become a native-speed superinstruction in the generated interpreter

#### Scenario: Hand-optimized superinstructions
- **WHEN** a critical function combines `[Super]` with inline assembly
- **THEN** it may be hand-optimized for maximum performance

#### Scenario: Target and size optimization
- **WHEN** a bespoke interpreter is generated
- **THEN** it is tailored to the target hardware and minimizes ROM footprint for constrained targets

### Requirement: Developer-Guided Superinstructions
Developers MUST be able to signal superinstruction intent, e.g. with a `[Super]` attribute or a `super` keyword, so functions can be marked as superinstruction candidates.

#### Scenario: Signaling intent via attribute
- **WHEN** a developer annotates a function with `[Super]`
- **THEN** the compiler treats the function as a superinstruction candidate

#### Scenario: Signaling intent via keyword
- **WHEN** a developer declares a function with the `super` keyword
- **THEN** the compiler treats the function as a superinstruction candidate

### Requirement: Superinstruction Structural Requirements
Candidate functions MUST satisfy structural requirements to be promoted via superinstructions: a configurable size limit (approximately 5-10 LLIR instructions), no complex control flow (no loops, recursion, or complex branching), at most 3-4 parameters, simple variable access (no complex data structures or pointer arithmetic), deterministic execution with no side effects preventing inlining, no shared-state modification, and thread safety.

#### Scenario: Size-limited bodies
- **WHEN** a candidate function exceeds the configured superinstruction size limit
- **THEN** it fails validation and is not promoted via superinstruction

#### Scenario: Rejecting complex control flow
- **WHEN** a candidate function contains loops, recursion, or complex branching
- **THEN** it is rejected as a superinstruction

#### Scenario: Parameter limit
- **WHEN** a candidate function has more parameters than the limit (3-4)
- **THEN** it is rejected as a superinstruction

#### Scenario: Restricting side-effect-free bodies
- **WHEN** a candidate function has complex variable access, side effects preventing inlining, or modifies shared state
- **THEN** it is rejected as a superinstruction (it must also be thread-safe and deterministic)

### Requirement: Superinstruction Analysis and Generation
When a candidate function passes validation, the compiler MUST promote it to a superinstruction (assigning a dynamic opcode and dispatch-table entry); when it fails, the compiler MUST fall back to a normal function call with a diagnostic.

#### Scenario: Successful superinstruction generation
- **WHEN** a `[Super]` function meets all criteria
- **THEN** it is generated as a superinstruction, and an opcode is assigned dynamically with a dispatch-table entry pointing to its implementation

#### Scenario: Failed generation with diagnostic
- **WHEN** a `[Super]` function fails the criteria
- **THEN** it is emitted as a normal function call with a warning diagnostic explaining which criteria failed

### Requirement: Superinstruction Opcode Management
Superinstruction opcodes MUST be allocated and registered dynamically by the compiler manager.

#### Scenario: Allocating superinstruction opcodes
- **WHEN** a superinstruction is generated
- **THEN** a new opcode is allocated dynamically and the superinstruction (name, opcode, implementation) is registered

#### Scenario: Validation pipeline
- **WHEN** a function is validated for superinstruction candidacy
- **THEN** it is rejected if it has too many instructions, contains loops, or has too many parameters, and accepted otherwise

### Requirement: Performance Benefits
Superinstruction execution MUST reduce call overhead compared to normal function calls (normal calls roughly 20-50 cycles depending on dispatch; superinstructions roughly 5-15 cycles; hand-optimized superinstructions roughly 3-8 cycles).

#### Scenario: Reduced overhead of superinstructions
- **WHEN** a superinstruction executes in place of a normal function call
- **THEN** execution incurs lower overhead (approximately 5-15 cycles vs 20-50 cycles for a call, and approximately 3-8 cycles for a hand-optimized superinstruction)

### Requirement: Superinstruction Development Workflow
A performance-engineering workflow MUST be provided: profile, mark `[Super]` critical functions, test generation, add inline assembly for maximum performance, and validate cycle counts and ROM usage.

#### Scenario: Performance engineering loop
- **WHEN** a developer optimizes a bottleneck
- **THEN** they profile, mark critical functions with `[Super]`, verify superinstruction generation, add inline assembly for maximum performance, and validate cycle counts and ROM usage

#### Scenario: Superinstruction diagnostics
- **WHEN** superinstruction diagnostics are enabled
- **THEN** the compiler reports the generated superinstruction, its opcode, size, estimated cycles, and ROM savings versus a function call

### Requirement: Inline LLIR Assembly
LLIR MUST serve as the portable assembly language usable directly in source code for performance-critical sections, providing the same optimizations across all supported targets.

#### Scenario: Portable inline assembly
- **WHEN** a developer writes inline assembly in a supported frontend language
- **THEN** the same assembly works on all targets (e.g. Atari 2600, Genesis, N64) and backends optimize it for the target hardware

#### Scenario: Assembly to LLIR mapping
- **WHEN** an inline assembly block is compiled
- **THEN** it is parsed and transformed directly into LLIR instructions

#### Scenario: Hardware access and cycle-precise kernels
- **WHEN** a developer needs direct control over critical algorithms
- **THEN** inline assembly provides direct control, cycle-precise optimization for display kernels, and access to hardware-specific features through the HAL

### Requirement: Inline Assembly Compiler Integration
Inline assembly MUST integrate with the compiler: variable access to locals/globals/fields/array elements, automatic register preservation and mapping of virtual registers to physical registers per backend, and participation in the optimizer passes (including dead code elimination across assembly boundaries).

#### Scenario: Accessing program variables in assembly
- **WHEN** inline assembly references a symbol
- **THEN** it accesses local variables, globals, object fields, and array elements of the source program

#### Scenario: Automatic register management
- **WHEN** a developer uses virtual registers in inline assembly
- **THEN** the compiler automatically manages register preservation and spills, mapping virtual registers to physical registers per backend

#### Scenario: Assembly participation in optimization
- **WHEN** inline assembly is part of a compilation unit
- **THEN** the assembly integrates with optimizer passes and dead code elimination works across assembly boundaries

### Requirement: Type Safety of Inline Assembly
The compiler MUST validate width parameters and perform automatic type conversion, with clear error messages for mismatches.

#### Scenario: Validating assembly operand widths
- **WHEN** inline assembly specifies operand widths
- **THEN** the compiler validates the width parameters, applies automatic conversion where appropriate, and reports clear errors for type mismatches

### Requirement: Integration with Higher-Level IR
LLIR MUST be generated from MLIR through transformation passes (type lowering, register allocation, instruction selection, width analysis, optimization, and code generation).

#### Scenario: Lowering MLIR to LLIR
- **WHEN** MLIR is lowered to LLIR
- **THEN** transformation passes perform type lowering itself, register allocation, instruction selection, width analysis, optimization (peephole optimization and dead code elimination), and code generation

### Requirement: Accumulator Model Performance
The accumulator model MUST provide performance characteristics: minimal interpreter state, efficient threading (DTC/ITC/TTC), direct mapping for 8-bit targets, and cache-friendly interpreter cores.

#### Scenario: Minimal interpreter state and efficient threading
- **WHEN** an interpreter is implemented on the accumulator model
- **THEN** it requires minimal interpreter state and naturally supports efficient threaded dispatch

#### Scenario: 8-bit target optimization and cache friendliness
- **WHEN** targeting an accumulator-based 8-bit machine
- **THEN** the model maps directly to the hardware and the smaller interpreter core fits in limited caches

### Requirement: Backend Optimization Opportunities
Backends MUST be able to optimize register mapping, fuse multiple LLIR instructions into native operations, optimize memory layout, and tailor dispatch to target capabilities, as well as perform superinstruction-related pattern detection, interpreter specialization, and size-versus-speed balancing.

#### Scenario: Backend register mapping and instruction fusion
- **WHEN** a backend generates native code
- **THEN** it optimizes register mapping for target hardware and fuses multiple LLIR instructions into native operations

#### Scenario: Memory layout and dispatch optimization
- **WHEN** a backend generates native code
- **THEN** it optimizes memory access patterns and tailors the dispatch mechanism to target capabilities

#### Scenario: Superinstruction pattern detection and specialization
- **WHEN** optimizing execution
- **THEN** the compiler detects common instruction sequences, generates game-specific superinstructions, only includes actually used interpreter specialization instructions, and balances interpreter size against execution speed