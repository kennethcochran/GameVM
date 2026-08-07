# Language Integration Specification

## Purpose
Outline the design for cross-language integration in GameVM, enabling seamless interaction between modules written in different programming languages. It covers type system mapping between languages, cross-language function calls, memory management across language boundaries, and exception handling integration.
Aspirational — not yet implemented.

## Requirements
### Requirement: Core Type Mappings
GameVM MUST map each HLIR type to the equivalent types of every supported language (Pascal, C/C++, C#, Java) so that modules across languages share a consistent type vocabulary.

#### Scenario: HLIR-to-language mapping
- **WHEN** a value typed in HLIR (e.g., `i32`/`u32`) is exchanged with a module written in a supported language
- **THEN** it MUST map to that language's matching type: Pascal `Integer`, C/C++ `int32_t`, C# `int`/`uint`, and Java `int`

#### Scenario: Composite and interface mapping
- **WHEN** a composite or interface type crosses a language boundary
- **THEN** `array<T>` MUST map to `array of T` / `T[]`, `struct` to `record` / `struct` / `class`, and `interface` to `interface` (or `struct` + vtable in C/C++)

### Requirement: Type Marshaling
GameVM MUST marshal values across language boundaries, using direct memory copy, endianness conversion, and platform alignment handling for value types, and coordinated GC, reference counting, and pinning for reference types.

#### Scenario: Marshaling value types
- **WHEN** a value type crosses a boundary
- **THEN** it MUST use a direct memory copy when possible, apply endianness conversion if needed, and handle alignment per platform

#### Scenario: Marshaling reference types
- **WHEN** a reference type crosses a boundary
- **THEN** garbage collection MUST be coordinated, shared objects MUST be reference counted, and pinning MUST be provided for native interop

### Requirement: Call Stubs
GameVM MUST generate call stubs that marshal parameters into a native argument block, invoke the native function, and free the argument block before returning the result.

#### Scenario: Calling a native function
- **WHEN** a high-level module calls a native function such as `AddNumbers(a, b)`
- **THEN** the generated HLIR stub MUST marshal the parameters into the call argument buffer, invoke the native function, free the buffer, and return the result

### Requirement: Calling Conventions
GameVM MUST support a set of calling conventions including `cdecl`, `stdcall`, `fastcall`, `thiscall`, and `vectorcall`.

#### Scenario: Selecting a calling convention
- **WHEN** a cross-language or native call is declared
- **THEN** the caller MUST be able to select the appropriate convention: `cdecl` (C-style, caller cleans up), `stdcall` (Windows API), `fastcall` (register-based), `thiscall` (C++ member functions), or `vectorcall` (SIMD optimization)

### Requirement: Ownership Models
GameVM MUST support transfer of ownership, borrowed references, and shared ownership across language boundaries.

#### Scenario: Transfer ownership
- **WHEN** a callee must assume stewardship of a passed object, as with factory functions
- **THEN** the caller MUST transfer ownership to the callee

#### Scenario: Borrowed reference
- **WHEN** a caller retains ownership of a passed object
- **THEN** the callee MUST treat it as a borrowed reference with no memory management, and the lifetime MUST be managed carefully by the caller

#### Scenario: Shared ownership
- **WHEN** multiple parties share an object
- **THEN** it MUST be reference counted and released automatically when the last reference is dropped

### Requirement: Garbage Collection Coordination
GameVM MUST coordinate garbage collection across language boundaries, including integration with language GCs, finalizer support, and weak reference support.

#### Scenario: Cross-boundary GC coordination
- **WHEN** an object managed by a language GC is shared across a boundary
- **THEN** the runtime MUST integrate with the language GC, honor finalizers, and support weak references

### Requirement: Exception Translation
GameVM MUST translate exceptions that cross language boundaries into HLIR exceptions so a host language handles them with its native mechanism.

#### Scenario: Catching a foreign exception
- **WHEN** a foreign call raises an exception
- **THEN** it MUST be converted to an HLIR exception (e.g., via a `CreateException` helper) that the calling language (e.g., Pascal `try`/`except`) can catch and handle

### Requirement: Error Codes
GameVM MUST provide standard error code mapping, custom error domains, and error chaining for cross-boundary failures.

#### Scenario: Propagating errors
- **WHEN** an error crosses a module boundary
- **THEN** it MUST map to a standard error code, support custom error domains, and allow error chaining

### Requirement: Module Initialization
GameVM MUST initialize modules in a defined order: static data, global constructors, module constructors, then thread-local storage setup.

#### Scenario: Initialization sequencing
- **WHEN** a cross-language module is loaded
- **THEN** its exports MUST initialize in fixed order: static data, global constructors, module constructors, and thread-local storage

### Requirement: Module Shutdown
GameVM MUST shut down modules in the reverse order of initialization, cleaning up resources and finishing threads.

#### Scenario: Ordered shutdown
- **WHEN** a module is unloaded
- **THEN** shutdown MUST run in reverse initialization order, clean up resources, and tear down threads

### Requirement: Callbacks
GameVM MUST support registering native function calls as callbacks that can be invoked directly from foreign or managed code.

#### Scenario: Registering a callback
- **WHEN** a managed delegate is passed to a foreign function as a callback
- **THEN** the delegate MUST be marshaled to a function pointer with a specified calling convention and be invokable by the foreign code

### Requirement: Threading Support
GameVM MUST support cross-language threading including OS-thread-local storage, synchronization primitives, and task scheduling.

#### Scenario: Threaded modules
- **WHEN** modules interact across threads
- **THEN** thread-local storage, synchronization primitives, and task scheduling MUST be made available across the boundary

### Requirement: Call Performance
GameVM MUST minimize cross-language call overhead through inline caching, direct calls when possible, and batch operations.

#### Scenario: Low-overhead calls
- **WHEN** a cross-language hot path executes
- **THEN** the runtime SHOULD use inline caching, issue direct calls where possible, and batch operations to reduce call overhead

### Requirement: Memory Access Performance
GameVM MUST optimize cross-boundary memory access using structure packing, cache alignment, and zero-copy operations.

#### Scenario: Efficient memory transfer
- **WHEN** data crosses a language boundary
- **THEN** it SHOULD use structure packing, cache-aligned layouts, and zero-copy operations to minimize memory access cost

### Requirement: Runtime Type Safety
GameVM MUST apply runtime type checking, array bounds checking, and null reference checking across language boundaries.

#### Scenario: Cross-language safety checks
- **WHEN** data or references are used / dereferenced across a boundary
- **THEN** runtime type, array bounds, and null reference checks MUST be enforced

### Requirement: Sandboxing
GameVM MUST support arranging trusted execution of cross-language modules via restricted execution contexts, capability-based security, and resource limits.

#### Scenario: Confining a module
- **WHEN** a cross-language module executes
- **THEN** it MUST be possible to run it in a restricted execution context with capability-based access control and enforced resource limits