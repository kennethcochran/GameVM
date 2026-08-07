# Inline Assembly Specification

## Purpose
Describe GameVM's support for inline assembly across all frontend languages, allowing developers to write low-level LLIR instructions directly within their high-level code for performance-critical optimizations, hardware-specific operations, and precise control over code generation.
Aspirational — not yet implemented.

## Requirements

### Requirement: Unified Inline Assembly Support
GameVM MUST support inline assembly across all frontend languages using a unified assembly syntax, so performance-critical and hardware-specific code can be written consistently regardless of the host language.

#### Scenario: Assembly in any frontend
- **WHEN** a developer writes an `asm` block in Pascal, C, Python, or another supported frontend
- **THEN** the block MUST use the same assembly syntax and participate in the normal compilation pipeline

### Requirement: Assembly Block Structure
GameVM MUST parse inline assembly blocks delimited by `asm` with either curly-brace (`{ ... }`) or Pascal-style (`... end`) terminators, containing statements that are instructions, labels, directives, or comments.

#### Scenario: Curly-brace block
- **WHEN** a frontend uses `asm { ... }` syntax
- **THEN** the assembly statements between the braces MUST be parsed as a single assembly block

#### Scenario: Pascal-style block
- **WHEN** a frontend uses `asm` ... `end` syntax
- **THEN** the statements up to the terminating `end` MUST be parsed as a single assembly block

### Requirement: Assembly Instruction Grammar
GameVM MUST parse each assembly instruction as an opcode followed by one or more comma-separated operands, where an operand is a register, immediate, memory reference, identifier, or string literal.

#### Scenario: Instruction recognition
- **WHEN** an instruction like `MOV R0, [A], TYPE_INT32` is encountered
- **THEN** it MUST parse the opcode (`MOV`, `ADD`, `SUB`, `MUL`, `DIV`, `MOD`, `AND`, `OR`, `XOR`, `NOT`, `SHL`, `SHR`, `CMP`, `JMP`, `JEQ`, `JNE`, `JGT`, `JLT`, `JGE`, `JLE`, `CALL`, `RET`, `PUSH`, `POP`, `LOAD`, `STORE`, `LEA`, `NOP`, `HALT`) followed by its operands

### Requirement: Register and Immediate Syntax
GameVM MUST recognize virtual registers (`R0`..`R9`, `SP`, `FP`, `PC`) and immediate values in decimal (`123`), hexadecimal (`0xFF`), or `#`-prefixed decimal form.

#### Scenario: Register recognition
- **WHEN** an operand is `R0`-`R9`, `SP`, `FP`, or `PC`
- **THEN** it MUST be recognized as a register operand

#### Scenario: Immediate recognition
- **WHEN** an operand is a `[0-9]+`, a `0x[0-9A-Fa-f]+`, or a `#<digits>` literal
- **THEN** it MUST be recognized as an immediate value

### Requirement: Memory Reference and Label Syntax
GameVM MUST support memory references `[...]` containing an expression, and labels (`IDENTIFIER:`) usable as branch and loop targets.

#### Scenario: Memory reference forms
- **WHEN** a memory operand is written as `[0x1000]`, `[variable]`, `[array + index * 4]`, `[struct.field]`, or `[R1 + 4]`
- **THEN** it MUST resolve to the corresponding direct address, variable, array element, struct field, or register-indirect address

#### Scenario: Label branches
- **WHEN** a branch instruction (e.g., `JNE scanline_loop`) references a declared label
- **THEN** control MUST target the labeled position within the assembly block

### Requirement: Type Annotations
Inline assembly MUST support specifying a type per operand/instruction using `TYPE_*` specifiers (`TYPE_INT8/16/32/64`, `TYPE_UINT8/16/32/64`, `TYPE_FLOAT32/64`, `TYPE_PTR`, `TYPE_BOOL`), enabling conversions such as truncation.

#### Scenario: Explicit type specification
- **WHEN** an instruction carries an explicit type specifier (e.g., `MOV R0, [var], TYPE_INT32`)
- **THEN** the operation MUST be performed using that annotated type

#### Scenario: Type truncation
- **WHEN** a larger value is moved with a narrower type specifier (e.g., `MOV R1, R0, TYPE_UINT8`)
- **THEN** the value MUST be truncated to that narrower type

### Requirement: Assembly Type Checking
Inline assembly MUST participate in the GameVM type system, validating operand types, checking memory reference types, tracking register contents, and mapping function parameters to assembly operands.

#### Scenario: Operand validation
- **WHEN** an assembly instruction is analyzed
- **THEN** the compiler MUST validate that operand types match the instruction requirements and that memory reference types are correct

#### Scenario: Register type tracking
- **WHEN** registers are used within an assembly block
- **THEN** the compiler MUST track register contents to enable type checking and optimization, and map function parameters to their operands

### Requirement: Register Allocation Hints
Developers MUST be able to provide register allocation hints within assembly blocks (e.g., preferring `R0` for a result or `R1` for a temporary) to guide the compiler.

#### Scenario: Hinted registers
- **WHEN** a developer assigns a hot value to a specific register within an `asm` block
- **THEN** those register assignments SHOULD be honored as hints by the register allocator

### Requirement: Compiler Optimization of Assembly
Inline assembly MUST be analyzable and optimizable by the compiler through constant folding, dead code elimination, instruction scheduling, register allocation, and peephole optimization.

#### Scenario: Optimization passes
- **WHEN** an assembly block is compiled
- **THEN** the compiler MUST be able to fold constant expressions, eliminate dead instructions, schedule instructions, allocate virtual registers, and apply peephole optimizations across the block

### Requirement: Optimization Directives
Assembly blocks MUST support directives (declared as `.IDENTIFIER` with optional operands) that control compiler behavior, including `.optimize`, `.inline`, and `.target`.

#### Scenario: Disabling optimization
- **WHEN** a developer writes `.optimize off` around timing-critical instructions
- **THEN** the marked instructions MUST NOT be optimized away or reordered until `.optimize on`

#### Scenario: Force inlining and target selection
- **WHEN** a developer writes `.inline always` or `.target atari2600`
- **THEN** the block MUST be forced inline and compiled for the specified target respectively

### Requirement: LLIR Generation from Assembly
GameVM MUST transform assembly blocks into equivalent HLIR/LLIR instructions during backend processing.

#### Scenario: Instruction lowering
- **WHEN** an assembly block is lowered
- **THEN** `MOV` into a register from memory MUST become a `LOAD`, and `MOV` from a register to memory MUST become a `STORE`, with the remaining instructions mapped to their LLIR equivalents

### Requirement: Target-Specific Optimization
The backend MUST optimize assembly for each target using instruction selection, register mapping to physical registers, instruction scheduling, and peephole (target-specific) optimization.

#### Scenario: Backend lower-to-target
- **WHEN** assembly is emitted for a target
- **THEN** the backend MUST select optimal machine instructions, map virtual registers to physical registers, schedule instructions for pipeline efficiency, and apply target-specific peephole optimizations

### Requirement: Assembly Error Reporting
Assembly errors MUST be reported with helpful context, identifying the source line, the offending fragment, and the underlying error (e.g., undefined variable).

#### Scenario: Semantic error message
- **WHEN** an instruction references an undefined variable such as `invalid_var`
- **THEN** the compiler MUST report the error with the assembly line number, the offending text (with a caret pointer), and an explanatory message such as `Undefined variable: invalid_var`

### Requirement: Assembly Error Recovery
The assembly parser MUST recover gracefully, continuing after syntax errors, reporting type and reference errors, skipping invalid statements when possible, and providing detailed error messages with line numbers.

#### Scenario: Continuing after errors
- **WHEN** a syntax or semantic error occurs in an assembly block
- **THEN** the parser MUST attempt recovery, skipping invalid statements where possible, while reporting every error with detailed context

### Requirement: Frontend Grammar Integration
Each language frontend MUST integrate the VirtualAssembly grammar by importing the grammar, parsing assembly blocks as separate AST nodes, processing them during semantic analysis, and generating LLIR (through HLIR/MLIR) during code generation.

#### Scenario: End-to-end assembly pipeline
- **WHEN** a frontend compiles an `asm` block
- **THEN** the frontend MUST import the assembly grammar, parse the block as an AST node, run semantic analysis over it, and emit the LLIR instructions for the code generation phase

### Requirement: Performance Guidelines
Developers MUST follow documented guidelines for assembly performance: minimal register pressure use, explicit type specifiers, target constraint awareness, appropriate addressing modes, and profiling to measure impact.

#### Scenario: Recommended assembly style
- **WHEN** a developer writes assembly for a hot path
- **THEN** they SHOULD minimise register pressure, provide explicit type specifiers, consider target hardware constraints, choose optimal addressing modes, and profile the result