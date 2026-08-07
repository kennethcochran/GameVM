# Type System Specification

## Purpose
Define the core type system used throughout GameVM, providing a unified type model that bridges different frontend programming languages while maintaining performance and safety. It covers core type definitions, type conversion rules, memory layout specifications, and cross-language type mapping.
Aspirational — not yet implemented.

## Requirements

### Requirement: Unified Type Hierarchy
GameVM MUST provide a unified type model rooted in a `Type` interface exposing a name, size, and alignment, with a hierarchy of primitive and composite types that bridges different programming languages.

#### Scenario: Primitive and composite classification
- **WHEN** a type is defined in the type system
- **THEN** it MUST be classified as either a primitive type (numeric, boolean, or character) or a composite type (struct, array, or union) under the shared `Type` interface

### Requirement: Primitive Type Support
GameVM MUST define a fixed set of primitive HLIR types with specified sizes and signedness, spanning signed and unsigned integers, floating-point types, `bool`, `char`, and `void`.

#### Scenario: Primitive type inventory
- **WHEN** a frontend emits a primitive type
- **THEN** it MUST map to one of `i8` (1 byte, signed), `u8` (1 byte, unsigned), `i16` (2 bytes, signed), `u16` (2 bytes, unsigned), `i32` (4 bytes, signed), `u32` (4 bytes, unsigned), `i64` (8 bytes, signed), `u64` (8 bytes, unsigned), `f32` (4 bytes), `f64` (8 bytes), `bool` (1 byte), `char` (4 bytes, UTF-32 code point), or `void` (0 bytes)

#### Scenario: Type naming
- **WHEN** a primitive type is referenced throughout the pipeline
- **THEN** it MUST use the HLIR name (`i8`, `u8`, `i16`, `u16`, `i32`, `u32`, `i64`, `u64`, `f32`, `f64`, `bool`, `char`, `void`)

### Requirement: Array Type
GameVM MUST support fixed-size array types in a composite type model, describing their element type and length.

#### Scenario: Array semantics
- **WHEN** an array type is used
- **THEN** it MUST be fixed-size, stored in contiguous memory, indexed from zero, and laid out in row-major order

### Requirement: Struct Type
GameVM MUST support struct types defined as a map of named fields to types, packed by default with no padding and a maximum alignment of 8 bytes, with explicit padding allowed.

#### Scenario: Struct memory layout
- **WHEN** a struct is laid out in memory without explicit padding or alignment modifiers
- **THEN** its fields MUST be packed back-to-back in declaration order with no inserted padding, as in a `packed record` / `#pragma pack(1)` layout

#### Scenario: Explicit padding
- **WHEN** a developer needs alignment guarantees in a struct
- **THEN** explicit padding MUST be added manually to align fields on target boundaries

### Requirement: Union Type
GameVM MUST support union types that share overlapping storage for a set of variants.

#### Scenario: Union storage
- **WHEN** a union type is declared
- **THEN** all of its member variants MUST share the same overlapping storage region, the union's size MUST equal the size of its largest member, and the union MUST be explicitly marked as a union

### Requirement: Implicit Type Conversion
GameVM MUST permit only implicit conversions that are guaranteed safe, including widening numeric conversions, `null` to reference types, and derived-to-base class conversions.

#### Scenario: Widening numeric conversion
- **WHEN** a value of a narrower numeric type is assigned to a wider numeric type that can represent all its values
- **THEN** the conversion MUST be performed implicitly with no required cast

#### Scenario: Null and derived-to-base
- **WHEN** `null` is assigned to a reference type, or a derived class value is used in a base-class position
- **THEN** the conversion MUST be allowed implicitly

### Requirement: Explicit Type Conversion
GameVM MUST allow explicit conversions for operations that are potentially unsafe or narrowing, including narrowing numeric conversions and conversions between unrelated types.

#### Scenario: Narrowing conversion requires a cast
- **WHEN** a value is converted to a narrower numeric type or between unrelated types
- **THEN** the conversion MUST be expressed explicitly by the programmer, as it may lose information or be unsafe

### Requirement: Cross-Language Type Mapping
GameVM MUST map types from each supported source language (C/C++, C#, Java) to the corresponding HLIR type.

#### Scenario: C/C++ mapping
- **WHEN** C/C++ source is compiled
- **THEN** `int32_t`, `unsigned int`, `float`, `double`, `bool`, and `char*` MUST map to `i32`, `u32`, `f32`, `f64`, `bool`, and `string` respectively

#### Scenario: C# mapping
- **WHEN** C# source is compiled
- **THEN** `int`, `uint`, `float`, `double`, `bool`, and `string` MUST map to `i32`, `u32`, `f32`, `f64`, `bool`, and `string` respectively

#### Scenario: Java mapping
- **WHEN** Java source is compiled
- **THEN** `int`, `long`, `float`, `double`, `boolean`, and `String` MUST map to `i32`, `i64`, `f32`, `f64`, `bool`, and `string` respectively

### Requirement: Memory Alignment Rules
GameVM MUST align types in memory according to defined rules: primitive types aligned to their own size, structs aligned to their strictest member, and arrays maintaining the element alignment.

#### Scenario: Type alignment
- **WHEN** a primitive is placed in memory
- **THEN** it MUST be aligned to its own size; a struct MUST be aligned to its strictest member; and an array MUST preserve the alignment of its element type

### Requirement: Packing Control
GameVM MUST support `#pragma pack(1)`-equivalent packing, manual padding, and platform-specific layout considerations.

#### Scenario: Packed layout
- **WHEN** a struct uses packed layout
- **THEN** its fields MUST be laid out without alignment padding as with `#pragma pack(1)`, with manual padding added when needed and platform-specific layout rules applied

### Requirement: Runtime Type Safety
GameVM MUST perform runtime safety checks including array bounds checking, null reference checking, and type casting verification.

#### Scenario: Bounds and null enforcement
- **WHEN** an array is indexed, a reference is dereferenced, or a value is cast at runtime
- **THEN** the runtime MUST check array bounds, null references, and type casting respectively to enforce type safety

### Requirement: Debug Type Features
GameVM MUST support type-oriented debug features in builds that include type information, runtime type identification, and reflection capabilities.

#### Scenario: Debug inspection
- **WHEN** a debug build contains type information
- **THEN** it MUST support runtime type identification and reflection capabilities over that type information

### Requirement: Value and Reference Type Semantics
GameVM MUST distinguish value types from reference types for optimization purposes.

#### Scenario: Value types
- **WHEN** a value type is used
- **THEN** it MUST be passed by value, stored inline, and require no heap allocation

#### Scenario: Reference types
- **WHEN** a reference type is used
- **THEN** it MUST be passed by reference, be garbage collected, and be nullable by default