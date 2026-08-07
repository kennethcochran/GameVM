# Common Language Features Specification

## Purpose
Defines the common features and constraints applied to all languages supported by GameVM, ensuring efficient compilation to retro gaming platforms while maintaining a modern development experience. Aspirational — not yet implemented.

## Requirements

### Requirement: Types and Variables
The compiler SHALL support the common basic types — integers (8-bit, 16-bit, 32-bit), fixed-point numbers (8.8, 16.16), booleans, characters, enums, fixed-size arrays, and structs/records — across all supported languages.

#### Scenario: Basic type support
- **WHEN** a source program in any supported language declares an integer, fixed-point, boolean, character, enum, fixed-size array, or struct/record
- **THEN** the compiler accepts the declaration and maps it to the corresponding GameVM type.

### Requirement: Static Type System
The type system SHALL be statically typed with explicit or inferred types resolved entirely at compile time, with no runtime type checks or dynamic type changes.

#### Scenario: Compile-time type resolution
- **WHEN** the compiler processes a program whose types are explicit or inferable
- **THEN** all type information is resolved at compile time and no runtime type checks or dynamic type changes are emitted.

### Requirement: Variables and Constants
Variables SHALL include locals, module/global variables (limited by platform RAM), and constants stored in ROM/Cartridge.

#### Scenario: Constants stored in ROM
- **WHEN** a program declares a constant
- **THEN** the compiler SHALL store the constant in ROM/Cartridge rather than RAM.

### Requirement: Memory Allocation Model
Memory allocation SHALL default to static allocation (pre-allocated at compile time) for all targets. Pool/region allocation SHALL be supported only on systems with more than 128KB of RAM to facilitate dynamic loading, and there SHALL be no general heap; it is replaced by deterministic region-based allocators.

#### Scenario: Static allocation by default
- **WHEN** a program targets any platform and performs memory allocation
- **THEN** the compiler SHALL pre-allocate memory at compile time by default.

#### Scenario: Pool allocation on high-RAM targets
- **WHEN** a program targets a system with more than 128KB of RAM and requests dynamic allocation
- **THEN** the compiler SHALL use pool/region-based allocation to facilitate dynamic loading.

#### Scenario: No general heap
- **WHEN** a program requests heap allocation
- **THEN** the compiler SHALL reject general heap allocation and use deterministic region-based allocators instead.

### Requirement: Control Flow Constructs
The compiler SHALL support conditionals (If/Else, Switch/Case optimized to jump tables), counted loops and While loops with Break/Continue, and functions with parameters, return values, and local functions.

#### Scenario: Switch optimized to jump table
- **WHEN** a program uses a Switch/Case statement whose branches can be represented as a compact dispatch
- **THEN** the compiler SHALL optimize the switch into a jump table.

#### Scenario: Loops and break/continue
- **WHEN** a program uses a For (counted) or While loop
- **THEN** the compiler SHALL support Break and Continue control-flow transfers within the loop.

### Requirement: Recursion Restrictions
Recursion SHALL be generally discouraged, and backends MAY enforce a hard depth limit for stack safety.

#### Scenario: Recursion depth limit
- **WHEN** a backend enforces a hard recursion depth limit and a program's recursion exceeds it
- **THEN** the backend SHALL reject or bound the recursion to protect stack safety.

### Requirement: Memory Regions
The memory model SHALL distinguish ROM/Flash for code and constants, RAM for variables and stack, a zero page or internal scratchpad fast SRAM region, memory-mapped I/O for direct hardware access, and dynamic module workspace using Overlay Regions for swapping code/data from slow media on higher-tier targets.

#### Scenario: Module overlay regions
- **WHEN** a higher-tier target defines Overlay Regions
- **THEN** modules SHALL be able to swap code/data from slow media through those regions.

### Requirement: Data Structures
The capability SHALL provide fixed-size arrays and static structs, bit fields for memory-efficient flags, and a pointer policy in which raw pointers are forbidden in application code, supported (Pointer, PByte) at Systems/HAL level for hardware access and the Standard Library, and References supported for pass-by-reference parameters.

#### Scenario: Raw pointers forbidden in application code
- **WHEN** application-level code attempts to use a raw pointer
- **THEN** the compiler SHALL reject the raw pointer for safety.

#### Scenario: Pointers allowed at systems level
- **WHEN** Systems/HAL-level code or the Standard Library uses Pointer or PByte types
- **THEN** the compiler SHALL allow the pointers for hardware access.

### Requirement: AOT Compilation of Dynamic Languages
For dynamic languages (Python, Ruby, Lua), programs SHALL be AOT compiled and transformed to LLIR with no runtime interpreter on the target. Dynamic resolution SHALL be supported via the Module Registry or ELF Loader. Runtime eval(), prototypes, and metatables SHALL be restricted, and memory SHALL be managed automatically via the compiler's static analysis with no runtime GC.

#### Scenario: Dynamic language AOT compilation
- **WHEN** a Python, Ruby, or Lua program is compiled
- **THEN** it SHALL be transformed to LLIR with no runtime interpreter on the target.

#### Scenario: Dynamic resolution via registry or ELF loader
- **WHEN** a dynamic-language program needs runtime module resolution
- **THEN** it SHALL resolve through the Module Registry or ELF Loader.

#### Scenario: Restricted dynamicism
- **WHEN** a dynamic-language program uses runtime eval(), prototypes, or metatables
- **THEN** the compiler SHALL restrict those runtime-dynamic features.

#### Scenario: Managed memory without runtime GC
- **WHEN** a dynamic-language program manages memory
- **THEN** it SHALL be managed automatically via the compiler's static analysis with no runtime garbage collector.

### Requirement: C-Family Language Constraints
For C#, Pascal, and C++, the compiler SHALL provide no garbage collection (explicit memory management in HAL, static allocation in App code), no exceptions (replaced by Error Handling codes), and generics resolved at compile time via monomorphization.

#### Scenario: No garbage collection
- **WHEN** a C#, Pascal, or C++ program runs
- **THEN** memory SHALL be managed explicitly in HAL and statically allocated in application code with no garbage collection.

#### Scenario: Exceptions replaced by error codes
- **WHEN** a C#, Pascal, or C++ program needs error handling
- **THEN** the compiler SHALL use Error Handling codes instead of exceptions.

#### Scenario: Limited generics via monomorphization
- **WHEN** a C#, Pascal, or C++ program uses generics
- **THEN** the compiler SHALL resolve them at compile time via monomorphization.

### Requirement: Common Optimizations
The compiler SHALL apply intrinsic promotion (promoting high-level functions to native VM instructions/superinstructions), bank optimization (automating bank-switching logic for large ROMs), and peephole and dead-code elimination plus host-side optimizations in MLIR.

#### Scenario: Intrinsic promotion to superinstructions
- **WHEN** a high-level function maps to a native VM instruction
- **THEN** the compiler SHALL promote the function to that native instruction as a superinstruction.

#### Scenario: Bank switching for large ROMs
- **WHEN** a program produces a large ROM that requires bank switching
- **THEN** the compiler SHALL automate the bank-switching logic.

#### Scenario: Dead code elimination
- **WHEN** a program contains dead code
- **THEN** the compiler SHALL eliminate the dead code and apply host-side MLIR optimizations.