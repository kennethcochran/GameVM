# Runtime-Interfaces Specification

## Purpose
Describes the formal interface contracts for GameVM components, including frontend/compiler interfaces, the virtual machine runtime interface, error codes, the type system, module system, build system, and versioning/stability conventions. Aspirational — not yet implemented.

## Requirements

### Requirement: Frontend Interface
The frontend MUST transform source code into an HLIR representation.

#### Scenario: Parsing source into HLIR
- **WHEN** parse is invoked with source code and compilation options
- **THEN** it returns an HLIR representation of the source
- **AND** it throws a syntax error on invalid syntax

### Requirement: HLIR to MLIR Conversion
The compiler MUST convert an HLIR module into an MLIR module.

#### Scenario: Converting HLIR to MLIR
- **WHEN** convert is invoked with an HLIR module and conversion options
- **THEN** it returns an MLIR module
- **AND** it throws a compilation error on unsupported features

### Requirement: Virtual Machine Execution
The runtime MUST execute a compiled module and return the program exit code.

#### Scenario: Executing a compiled module
- **WHEN** execute is invoked with a compiled module, an optional entry point, and optional command-line arguments
- **THEN** the module is executed and the program exit code is returned, defaulting to the `main` entry point when none is given

### Requirement: Error Handling
Error conditions MUST be surfaced through a defined set of error codes.

#### Scenario: Reporting a general compilation error
- **WHEN** a general compilation error occurs
- **THEN** error code `1001` (E_COMPILE_ERROR) is reported

#### Scenario: Reporting a runtime error
- **WHEN** a runtime execution error occurs
- **THEN** error code `1002` (E_RUNTIME_ERROR) is reported

#### Scenario: Reporting a type mismatch
- **WHEN** type checking fails
- **THEN** error code `1003` (E_TYPE_MISMATCH) is reported

#### Scenario: Reporting an undefined symbol
- **WHEN** a reference to an undefined symbol occurs
- **THEN** error code `1004` (E_UNDEFINED_SYMBOL) is reported

### Requirement: Type System
The type system MUST determine type compatibility between a source and a target type.

#### Scenario: Checking assignability
- **WHEN** isAssignable is invoked with a source type and a target type
- **THEN** it returns true if the source type is assignable to the target type, otherwise false

### Requirement: Module System
Modules MUST be importable by name at runtime.

#### Scenario: Importing a module
- **WHEN** import is invoked with a module name
- **THEN** the named module is imported and returned
- **AND** it throws ModuleNotFoundError if the module cannot be found

### Requirement: Build System
Build targets MUST compile source files into build artifacts.

#### Scenario: Compiling source files for a target
- **WHEN** compile is invoked with source files and build options
- **THEN** it resolves once compilation is complete and produces a build artifact

### Requirement: Versioning
All interfaces MUST be versioned using Semantic Versioning (SemVer), with each interface carrying a `@since` tag indicating the version in which it was introduced.

#### Scenario: Versioning an interface
- **WHEN** an interface is published
- **THEN** it follows Semantic Versioning and includes a `@since` tag indicating the version it was introduced in

### Requirement: Stability Index
Interfaces MUST be classified by stability: Stable interfaces follow semantic versioning, Experimental interfaces may change in minor versions, and Internal interfaces are not part of the public API and may change without notice.

#### Scenario: Stable interface guarantees
- **WHEN** an interface is marked Stable
- **THEN** it follows semantic versioning

#### Scenario: Experimental interface changes
- **WHEN** an interface is marked Experimental
- **THEN** it may change in minor versions

#### Scenario: Internal interface changes
- **WHEN** an interface is marked Internal
- **THEN** it is not part of the public API and may change without notice