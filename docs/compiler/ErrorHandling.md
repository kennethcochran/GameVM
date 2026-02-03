---
title: "Error Handling"
description: "Error handling strategy and implementation in GameVM"
author: "GameVM Team"
created: "2025-09-20"
updated: "2025-09-20"
version: "1.0.0"
---

# Error Handling in GameVM

## 1. Introduction

### 1.1 Purpose
This document defines the error handling strategy for GameVM, ensuring consistent error reporting and handling across all components and language boundaries.

### 1.2 Key Principles
- **Consistency**: Uniform error handling across all modules
- **Clarity**: Clear error messages with context
- **Recovery**: Support for error recovery where possible
- **Performance**: Minimal overhead in the success path
- **Safety**: Prevent error conditions from causing undefined behavior

### 1.3 Related Documents
- [Architecture Overview](../architecture/ArchitectureOverview.md)
- [Type System](./TypeSystem.md)
- [Module System](./ModuleResolution.md)

## 2. Error Types

### 2.1 Error Categories

| Category      | Description                          | Recovery Strategy           | Example                      |
|---------------|--------------------------------------|-----------------------------|------------------------------|
| **Fatal**     | Unrecoverable system error           | Log and terminate           | Out of memory                |
| **Error**     | Recoverable error condition          | Return error, allow retry   | File not found               |
| **Warning**   | Non-critical issue                   | Log and continue            | Deprecated API usage         |
| **Info**      | Informational message                | Log and continue            | Configuration loaded         |

### 2.2 Error Codes

#### 2.2.1 Core Error Codes
```cpp
enum class ErrorCode : uint32_t {
    // General errors (0x0000-0x0FFF)
    Success = 0x0000,
    OutOfMemory = 0x0001,
    InvalidArgument = 0x0002,
    
    // File system errors (0x1000-0x1FFF)
    FileNotFound = 0x1000,
    FileAccessDenied = 0x1001,
    FileCorrupt = 0x1002,
    
    // Network errors (0x2000-0x2FFF)
    NetworkUnreachable = 0x2000,
    ConnectionRefused = 0x2001,
    
    // Module system errors (0x3000-0x3FFF)
    ModuleNotFound = 0x3000,
    ModuleLoadFailed = 0x3001,
    
    // ... other error codes
};
```

## 3. Error Reporting

### 3.1 Error Structure
```typescript
interface ErrorInfo {
    code: number;           // Error code
    message: string;        // Human-readable message
    module: string;         // Originating module
    file: string;           // Source file
    line: number;           // Line number
    timestamp: number;      // Unix timestamp
    context: Map<string, any>; // Additional context
    innerError?: ErrorInfo; // Nested error
}
```

### 3.2 Error Context
- **Required Fields**:
  - `code`: Numeric error code
  - `message`: Human-readable description
  - `module`: Originating module name
  - `timestamp`: When the error occurred

- **Optional Fields**:
  - `stackTrace`: Call stack information
  - `suggestion`: How to fix the issue
  - `documentation`: Link to relevant docs

## 4. Cross-Language Error Handling

### 4.1 Language-Specific Mappings

#### 4.1.1 C++
```cpp
class Error {
public:
    Error(ErrorCode code, std::string message);
    
    // Check if operation succeeded
    explicit operator bool() const { return code_ != ErrorCode::Success; }
    
    // Get error details
    ErrorCode code() const { return code_; }
    const std::string& message() const { return message_; }
    
    // Factory methods
    static Error success() { return Error(ErrorCode::Success, ""); }
    static Error notFound(std::string what) { 
        return Error(ErrorCode::NotFound, "Not found: " + what); 
    }
    
private:
    ErrorCode code_;
    std::string message_;
};
```

#### 4.1.2 C#
```csharp
public class GameVmException : Exception
{
    public ErrorCode ErrorCode { get; }
    public IReadOnlyDictionary<string, object> Context { get; }
    
    public GameVmException(ErrorCode code, string message, 
        Dictionary<string, object> context = null, 
        Exception innerException = null)
        : base(message, innerException)
    {
        ErrorCode = code;
        Context = context?.AsReadOnly() ?? new Dictionary<string, object>();
    }
}
```

#### 4.1.3 JavaScript/TypeScript
```typescript
type Result<T, E = Error> = 
    | { success: true; value: T }
    | { success: false; error: E };

function divide(a: number, b: number): Result<number, Error> {
    if (b === 0) {
        return {
            success: false,
            error: new Error('Division by zero')
        };
    }
    return { success: true, value: a / b };
}
```

## 5. Error Recovery

### 5.1 Retry Strategies

#### 5.1.1 Exponential Backoff
```cpp
Error retryWithBackoff(std::function<Error()> operation, int maxRetries = 3) {
    int retryCount = 0;
    while (true) {
        Error result = operation();
        if (result) {  // Success
            return Error::success();
        }
        
        if (++retryCount >= maxRetries) {
            return result;
        }
        
        // Exponential backoff: 100ms, 200ms, 400ms, etc.
        int delayMs = 100 * (1 << (retryCount - 1));
        std::this_thread::sleep_for(std::chrono::milliseconds(delayMs));
    }
}
```

### 5.2 Fallback Mechanisms
```csharp
public T GetResource<T>(string path, T fallback = default)
{
    try {
        return resourceLoader.Load<T>(path);
    }
    catch (ResourceLoadException ex) {
        logger.Warn($"Failed to load resource {path}, using fallback", ex);
        return fallback;
    }
}
```

## 6. Logging and Diagnostics

### 6.1 Log Levels
```cpp
enum class LogLevel {
    Trace,   // Detailed debugging information
    Debug,   // Debugging information
    Info,    // General information
    Warning, // Warnings
    Error,   // Recoverable errors
    Fatal    // Unrecoverable errors
};
```

### 6.2 Structured Logging
```typescript
interface LogEntry {
    timestamp: Date;
    level: LogLevel;
    message: string;
    context: Record<string, any>;
    error?: Error;
}

class Logger {
    log(level: LogLevel, message: string, context: Record<string, any> = {}) {
        const entry: LogEntry = {
            timestamp: new Date(),
            level,
            message,
            context,
        };
        // Process and output the log entry
    }
    
    error(error: Error, context: Record<string, any> = {}) {
        this.log(LogLevel.Error, error.message, {
            ...context,
            error: error.stack,
        });
    }
}
```

## 7. Best Practices

### 7.1 Do's and Don'ts

#### Do:
- Use specific error types/codes
- Include context with errors
- Document possible errors in function contracts
- Handle errors at the appropriate level
- Log errors with sufficient context

#### Don't:
- Use exceptions for control flow
- Catch and ignore errors without logging
- Expose implementation details in error messages
- Use magic numbers for error codes

### 7.2 Error Message Guidelines
- Be concise but descriptive
- Include relevant identifiers
- Use consistent formatting
- Avoid technical jargon when possible
- Provide actionable information

## 8. Testing Error Cases

### 8.1 Unit Testing
```csharp
[Test]
public void Divide_ByZero_ThrowsException()
{
    // Arrange
    var calculator = new Calculator();
    
    // Act & Assert
    var ex = Assert.Throws<DivideByZeroException>(
        () => calculator.Divide(10, 0));
        
    Assert.That(ex.Message, Does.Contain("divide by zero"));
}
```

### 8.2 Property-Based Testing
```haskell
-- Using QuickCheck/Hedgehog
prop_divide_by_nonzero_multiplicative_inverse a b = 
    b /= 0 ==> (a / b) * b == a
```

## 9. Performance Considerations

### 9.1 Zero-Cost Error Handling
- Use return values for common error cases
- Reserve exceptions for exceptional circumstances
- Consider using `std::expected` (C++23) or similar

### 9.2 Error Context Allocation
- Avoid allocations in the happy path
- Use small string optimization where possible
- Consider thread-local storage for error contexts

## 10. Security Considerations

### 10.1 Error Information Leakage
- Sanitize error messages in production
- Don't expose stack traces to end users
- Be careful with error messages that might reveal system information

### 10.2 Error Handling in Security-Sensitive Code
- Validate all error conditions
- Use constant-time comparisons for security checks
- Ensure error states don't leave the system in an insecure state

## 11. References
1. [Error Handling in C++ - Best Practices](https://isocpp.org/wiki/faq/exceptions)
2. [Error Handling in .NET](https://docs.microsoft.com/en-us/dotnet/standard/exceptions/)
3. [Node.js Error Handling](https://nodejs.org/en/docs/guides/error-handling/)

## Changelog

### [1.0.0] - 2025-09-16
- Initial version
