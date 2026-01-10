---
title: "Type System"
description: "Specification of the GameVM type system and type conversion rules"
author: "GameVM Team"
created: "2025-09-20"
updated: "2025-09-20"
version: "1.0.0"
---

# GameVM Type System

## 1. Introduction

### 1.1 Purpose
This document defines the core type system used throughout GameVM, providing a unified type model that bridges different programming languages while maintaining performance and safety.

### 1.2 Scope
- Core type definitions
- Type conversion rules
- Memory layout specifications
- Cross-language type mapping

## 2. Core Types

### 2.1 Type Hierarchy

```mermaid
classDiagram
    class Type {
        <<interface>>
        +name: string
        +size: int
        +alignment: int
    }
    
    class PrimitiveType {
        +isSigned: bool
    }
    
    class NumericType {
        +minValue: number
        +maxValue: number
    }
    
    class IntegerType {
        +isSigned: bool
    }
    
    class FloatType {
        +precision: int
    }
    
    class CompositeType {
        <<interface>>
        +members: Type[]
    }
    
    class StructType {
        +fields: Map~string, Type~
    }
    
    class ArrayType {
        +elementType: Type
        +length: int
    }
    
    class UnionType {
        +variants: Type[]
    }
    
    Type <|-- PrimitiveType
    Type <|-- CompositeType
    
    PrimitiveType <|-- NumericType
    PrimitiveType <|-- BooleanType
    PrimitiveType <|-- CharType
    
    NumericType <|-- IntegerType
    NumericType <|-- FloatType
    
    CompositeType <|-- StructType
    CompositeType <|-- ArrayType
    CompositeType <|-- UnionType
    
    %% Define styles for different class types
    class Type interface
    class PrimitiveType interface
    class CompositeType interface
    class NumericType primitive
    class IntegerType primitive
    class FloatType primitive
    class BooleanType primitive
    class CharType primitive
    class StructType composite
    class ArrayType composite
    class UnionType composite
    
    %% Style definitions
    classDef interface fill:#e1f5fe,stroke:#0288d1,stroke-width:2px;
    classDef primitive fill:#e8f5e9,stroke:#43a047,stroke-width:2px;
    classDef composite fill:#fff3e0,stroke:#f57c00,stroke-width:2px;
```

### 2.2 Primitive Types

| HLIR Type | Size  | Signed | Description           |
|-----------|-------|--------|-----------------------|
| `i8`      | 1 byte| Yes    | 8-bit integer         |
| `u8`      | 1 byte| No     | 8-bit unsigned integer|
| `i16`     | 2 bytes| Yes   | 16-bit integer        |
| `u16`     | 2 bytes| No    | 16-bit unsigned integer|
| `i32`     | 4 bytes| Yes   | 32-bit integer        |
| `u32`     | 4 bytes| No    | 32-bit unsigned integer|
| `i64`     | 8 bytes| Yes   | 64-bit integer        |
| `u64`     | 8 bytes| No    | 64-bit unsigned integer|
| `f32`     | 4 bytes| N/A   | 32-bit floating point |
| `f64`     | 8 bytes| N/A   | 64-bit floating point |
| `bool`    | 1 byte| N/A    | Boolean value         |
| `char`    | 4 bytes| N/A   | UTF-32 code point     |
| `void`    | 0     | N/A    | No type/return value  |

### 2.2 Composite Types

#### 2.2.1 Arrays
- Fixed-size, contiguous memory
- Zero-based indexing
- Stored in row-major order

```c
// Example: Array of 10 integers
int32_t[10] numbers;
```

#### 2.2.2 Structs
- Packed by default (no padding)
- Explicit padding can be added
- Maximum alignment of 8 bytes

```mermaid
graph TD
    subgraph Point_Struct["Point Struct"]
        direction TB
        P1["x: i32 (4 bytes)"]
        P2["y: i32 (4 bytes)"]
        P3["color: u32 (4 bytes)"]
        P4["visible: bool (1 byte)"]
        P5["padding (3 bytes)"]
    end
    
    subgraph Memory_Layout["Memory Layout"]
        direction TB
        M1["0-3: x (i32)"]
        M2["4-7: y (i32)"]
        M3["8-11: color (u32)"]
        M4["12: visible (bool)"]
        M5["13-15: [padding]"]
    end
    
    Point_Struct -->|"memory layout"| Memory_Layout
    
    classDef struct fill:#e3f2fd,stroke:#1976d2,stroke-width:2px,color:#333;
    classDef memory fill:#f5f5f5,stroke:#9e9e9e,stroke-width:1px,color:#333,font-family:monospace;
    
    class Point_Struct struct;
    class Memory_Layout memory;
    
    %% Add a title
    classDef titleStyle fill:none,stroke:none,font-weight:bold,font-size:16px
    Title[Struct Memory Layout Example]:::titleStyle
```

```pascal
type
  TPoint = packed record
    X, Y: Int32;
  end;
```

#### 2.2.3 Unions
- Overlapping storage
- Size of largest member
- Explicitly marked as union

```c
union Value {
    int32_t as_int;
    float as_float;
    char* as_string;
};
```

## 3. Type Conversion

### 3.1 Implicit Conversions
- Widening numeric conversions
- `null` to reference types
- Derived to base class

### 3.2 Explicit Conversions
- Narrowing numeric conversions
- Between unrelated types
- Potentially unsafe operations

### 3.3 Cross-Language Type Mapping

#### 3.3.1 C/C++
```c
// C/C++ to HLIR
int32_t        → i32
unsigned int   → u32
float          → f32
double         → f64
bool           → bool
char*          → string
```

#### 3.3.2 C#
```csharp
// C# to HLIR
int            → i32
uint           → u32
float          → f32
double         → f64
bool           → bool
string         → string
```

#### 3.3.3 Java
```java
// Java to HLIR
int            → i32
long           → i64
float          → f32
double         → f64
boolean        → bool
String         → string
```

## 4. Memory Layout

### 4.1 Alignment Rules
- Primitive types aligned to their size
- Structs aligned to strictest member
- Arrays maintain element alignment

### 4.2 Packing
- `#pragma pack(1)` equivalent
- Manual padding when needed
- Platform-specific considerations

## 5. Type Safety

### 5.1 Runtime Checks
- Array bounds checking
- Null reference checking
- Type casting verification

### 5.2 Debug Features
- Type information in debug builds
- Runtime type identification
- Reflection capabilities

## 6. Optimization Considerations

### 6.1 Value Types
- Passed by value
- Stored inline
- No heap allocation

### 6.2 Reference Types
- Passed by reference
- Garbage collected
- Nullable by default

## 7. Related Documents
- [Architecture Overview](./ArchitectureOverview.md)
- [Language Integration](./LanguageIntegration.md)
- [Module System](./ModuleResolution.md)

## Changelog

### [1.0.0] - 2025-09-16
- Initial version
