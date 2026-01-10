# GameVM Language Integration Design

## 1. Introduction

### 1.1 Purpose
This document outlines the design for cross-language integration in GameVM, enabling seamless interaction between modules written in different programming languages.

### 1.2 Key Features
- Type system mapping between languages
- Cross-language function calls
- Memory management across language boundaries
- Exception handling integration

## 2. Type System

### 2.1 Core Type Mappings

| HLIR Type     | Pascal          | C/C++           | C#              | Java            |
|---------------|-----------------|-----------------|-----------------|-----------------|
| `i8`/`u8`    | `ShortInt`      | `int8_t`        | `sbyte`/`byte`  | `byte`          |
| `i16`/`u16`  | `SmallInt`      | `int16_t`       | `short`/`ushort`| `short`/`char`  |
| `i32`/`u32`  | `Integer`       | `int32_t`       | `int`/`uint`    | `int`           |
| `i64`/`u64`  | `Int64`         | `int64_t`       | `long`/`ulong`  | `long`          |
| `f32`        | `Single`        | `float`         | `float`         | `float`         |
| `f64`        | `Double`        | `double`        | `double`        | `double`        |
| `bool`       | `Boolean`       | `bool`          | `bool`          | `boolean`       |
| `string`     | `String`        | `const char*`   | `string`        | `String`        |
| `array<T>`   | `array of T`    | `T[]`           | `T[]`           | `T[]`           |
| `struct`     | `record`        | `struct`        | `struct`        | `class`         |
| `interface`  | `interface`     | `struct` + vtable | `interface`    | `interface`     |

### 2.2 Type Marshaling

#### 2.2.1 Value Types
- Direct memory copy when possible
- Endianness conversion if needed
- Alignment handling per platform

#### 2.2.2 Reference Types
- Garbage collection coordination
- Reference counting for shared objects
- Pinning for native interop

## 3. Function Calling Convention

### 3.1 Call Stubs
```csharp
// C# calling C example
[DllImport("mylib", CallingConvention = CallingConvention.Cdecl)]
private static extern int AddNumbers(int a, int b);

// Generated HLIR call stub
public static int CallAddNumbers(int a, int b)
{
    // 1. Marshal parameters
    IntPtr args = AllocCallArgs(8);
    Marshal.WriteInt32(args, 0, a);
    Marshal.WriteInt32(args, 4, b);
    
    // 2. Call native function
    int result = CallNativeFunction("mylib", "AddNumbers", args);
    
    // 3. Clean up and return
    FreeCallArgs(args);
    return result;
}
```

### 3.2 Calling Conventions
- **cdecl**: C-style (caller cleans up)
- **stdcall**: Windows API standard
- **fastcall**: Register-based for performance
- **thiscall**: C++ member functions
- **vectorcall**: SIMD optimization

## 4. Memory Management

### 4.1 Ownership Models
1. **Transfer Ownership**
   - Caller transfers ownership to callee
   - Common for factory functions

2. **Borrowed Reference**
   - Caller retains ownership
   - No memory management needed
   - Lifetime must be carefully managed

3. **Shared Ownership**
   - Reference counted
   - Automatic cleanup when last reference is dropped

### 4.2 Garbage Collection
- Integration with language GCs
- Finalizer support
- Weak references

## 5. Exception Handling

### 5.1 Exception Translation
```pascal
// Pascal code that might throw
try
  CallExternalCode();
except on E: Exception do
  // Handle exception
end;

// HLIR exception handling
void* CallWithExceptionHandling(void* context, void* (*func)(void*)) {
    try {
        return func(context);
    } catch (const std::exception& e) {
        // Convert to HLIR exception
        return CreateException(e.what());
    }
}
```

### 5.2 Error Codes
- Standard error code mapping
- Custom error domains
- Error chaining

## 6. Module Initialization

### 6.1 Initialization Order
1. Static data initialization
2. Global constructors
3. Module constructors
4. Thread-local storage setup

### 6.2 Shutdown
- Reverse initialization order
- Resource cleanup
- Thread cleanup

## 7. Advanced Features

### 7.1 Callbacks
```csharp
// C# delegate to C function pointer
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
delegate void CallbackDelegate(int value);

[DllImport("mylib")]
private static extern void RegisterCallback(CallbackDelegate callback);

// Usage
var callback = (int value) => Console.WriteLine(value);
RegisterCallback(callback);
```

### 7.2 Threading
- Thread-local storage
- Synchronization primitives
- Task scheduling

## 8. Performance Considerations

### 8.1 Call Overhead
- Inline caching
- Direct calls when possible
- Batch operations

### 8.2 Memory Access
- Structure packing
- Cache alignment
- Zero-copy operations

## 9. Security

### 9.1 Type Safety
- Runtime type checking
- Array bounds checking
- Null reference checking

### 9.2 Sandboxing
- Restricted execution contexts
- Capability-based security
- Resource limits
