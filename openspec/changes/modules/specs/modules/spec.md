# Modules Specification

## Purpose
Defines GameVM modules as the fundamental compilation units that bridge language-specific source code with the language-agnostic HLIR (High-Level Intermediate Representation), enabling modules to be independently compiled and consumed across different programming languages through a universal HLIR ABI. Aspirational — not yet implemented.

## Requirements

### Requirement: Module Lifecycle
A module SHALL flow through a well-defined lifecycle: source definition (a language-specific source file), HLIR transformation (Source Files -> Language Frontend -> HLIR Modules), binary compilation (HLIR Modules -> HLIR Compiler -> Binary HLIR), library assembly (Binary HLIR -> Library Linker -> Libraries), and package distribution (Libraries -> Packager -> Packages).

#### Scenario: Source to HLIR transformation
- **WHEN** a language-specific source file is compiled
- **THEN** the language frontend SHALL transform it into a language-agnostic HLIR module.

#### Scenario: Binary compilation and packaging
- **WHEN** HLIR modules are further compiled
- **THEN** the HLIR compiler SHALL produce binary HLIR, the library linker SHALL assemble libraries from binary HLIR, and the packager SHALL produce distribution packages.

### Requirement: Module Structure
Each source file SHALL define a single module with explicit boundaries, and a module SHALL be represented in HLIR as a self-contained structure that carries module identity (name, source file, language, version, exports, imports), semantic entities (types, functions, constants, variables), metadata (author, license, description, keywords, targetRequirements), and compilation context (source map, compilation options).

#### Scenario: HLIR module representation
- **WHEN** a source module is compiled to HLIR
- **THEN** the resulting HLIR module SHALL carry its identity, public interface, semantic entities, metadata, and compilation context.

### Requirement: Module Naming Conventions
Module names SHALL be derived from the source file path and language conventions, separated by dots between logical components, case-preserving per the source language, unique within the dependency graph, and descriptive of purpose and scope.

#### Scenario: File path to module name mapping
- **WHEN** a source file is compiled
- **THEN** its module name SHALL derive from the file path and language (e.g., `src/Game/Graphics.pas` -> `Game.Graphics`, `src/Math/Vector.cpp` -> `Math.Vector`, `src/utils/logging.py` -> `utils.logging`).

#### Scenario: Unique module names
- **WHEN** modules are composed into a dependency graph
- **THEN** module names SHALL be unique within that graph.

### Requirement: Module Boundaries (Exports)
Modules SHALL explicitly declare what they make available to other modules through explicit exports, represented in HLIR as export entries naming the exported entity, its kind (type/function/variable), visibility, internal name, and optionally a function signature.

#### Scenario: Explicit and public exports
- **WHEN** a module defines a public interface
- **THEN** it SHALL declare explicit exports with name, kind, visibility, and internal name, and the HLIR SHALL record them.

### Requirement: Module Boundaries (Imports)
Modules SHALL declare their dependencies explicitly using compile-time resolvable imports, mapping each language's import syntax to HLIR module references. Imports SHALL support importing an entire module or specific members, with optional aliasing and version constraints.

#### Scenario: Declared imports
- **WHEN** a module depends on other modules
- **THEN** it SHALL declare those dependencies explicitly, including member selection, aliasing, and version constraints.

### Requirement: Compile-Time Import Restrictions
All require/import statements SHALL be compile-time resolvable, using string literals, compile-time constants, CTFE function results, or concatenations of compile-time values. Runtime variables, runtime evaluation, and dynamic selection that is not compile-time resolvable SHALL be rejected with compiler errors identifying the offending expression and the module that could not be resolved.

While the dependency graph MUST be static, the actual loading of these modules into memory MAY be deferred to runtime on systems with slow media or RAM constraints via the Dynamic Loading system.

#### Scenario: Compile-time-resolvable imports allowed
- **WHEN** an import statement uses a string literal, a compile-time constant, a CTFE function result, or a concatenation of compile-time values
- **THEN** the import SHALL be accepted and resolved during compilation.

#### Scenario: Runtime import variables rejected
- **WHEN** an import statement uses a runtime variable or a non-compile-time-resolvable expression
- **THEN** the compiler SHALL emit a compile-time error identifying the statement and the non-constant argument.

#### Scenario: Missing module error
- **WHEN** an import names an unknown module
- **THEN** the compiler SHALL emit a compile-time error identifying the module and listing the available modules.

### Requirement: Dynamic Module Resolution
While GameVM is compiled, it SHALL support dynamic module resolution (common in Python, Lua, and Ruby) through two mechanisms: the Module Registry for cartridge/resident systems, and the ELF Loader for disk/network systems. Dynamic resolution SHALL be implemented as wrappers around the Dynamic Loading system and SHALL return Module Objects.

#### Scenario: Module Registry resolution
- **WHEN** all code must reside in the primary ROM and a dynamic import occurs
- **THEN** the compiler SHALL generate a static, sorted Module Registry mapping module names to export tables, and resolution SHALL be performed by binary search in that registry.

#### Scenario: ELF Loader resolution
- **WHEN** a system uses slow media and a dynamic import occurs
- **THEN** the dynamic module name SHALL map to a file reference, the runtime SHALL invoke the ELF Loader to pull the module into RAM and relocate it, and resolution SHALL return a handle to the loaded and bound module.

#### Scenario: Module Objects for dynamic dispatch
- **WHEN** a module is resolved dynamically
- **THEN** it SHALL be returned as a Module Object (a struct of function pointers); calls to the module SHALL be dispatched via this vtable, ensuring high performance once resolved.

### Requirement: Dynamic Candidates Declaration
To prevent ROM bloat, the compiler MUST support explicit declaration of "Dynamic Candidates" in the project configuration, constraining which modules may be registered for dynamic resolution or emitted as ELF.

#### Scenario: Dynamic candidates restriction
- **WHEN** a project enables dynamic resolution
- **THEN** the project configuration SHALL be able to declare the modules eligible for registration in the registry or emission as ELF, and only those modules SHALL be handled dynamically.

### Requirement: Compile-Time Import Processing and Validation
The compiler SHALL process all import statements during compilation: scan source import statements, validate they are compile-time resolvable, resolve module dependencies, and build the dependency graph. It SHALL validate each import, test whether an expression is a string literal, compile-time constant, CTFE result, or compile-time concatenation, and detect runtime variables, and it SHALL reject runtime-variable imports with compile errors.

#### Scenario: Import processing pipeline
- **WHEN** a module is compiled
- **THEN** the compiler SHALL scan import statements, validate compile-time resolvability, resolve module dependencies, and construct the dependency graph before HLIR generation.

### Requirement: Cross-Language Interoperability via Universal ABI
HLIR SHALL serve as the universal ABI enabling cross-language interoperability: it SHALL unify language types to HLIR types (e.g., Pascal `Integer` -> `i32`, C++ `float` -> `f32`, C# `string` -> `string`, Java `boolean` -> `bool`, Python `list` -> `array<T>`), and SHALL use a single HLIR calling convention (`hlir_std`) across all languages so that modules compiled from one language are callable from another.

#### Scenario: Cross-language interop type mapping
- **WHEN** a module is consumed across languages
- **THEN** the HLIR SHALL unify the type of each language entity, so a Pascal `Integer` is a 32-bit signed integer and a C# `string` is a Unicode string.

#### Scenario: Universal calling convention
- **WHEN** a function is called from another language
- **THEN** it SHALL use the universal HLIR calling convention, resolving through compile-time-known modules rather than text inclusion.

### Requirement: Module Compilation Pipeline
The compiler SHALL compile each module through a defined pipeline: (1) Source Parsing (language-specific parser creating an AST); (2) Import Resolution (validate compile-time resolvable imports, resolve dependencies, build the dependency graph); (3) Semantic Analysis (type checking, name resolution, scope analysis, cross-module type validation); (4) HLIR Generation (AST to HLIR, language-specific optimizations, module metadata); (5) Binary Compilation (compile HLIR to binary HLIR, cross-module optimizations, debug information). The compiler SHALL report compile errors for invalid constructs (e.g., circular dependencies), SHALL detect circular dependencies at compile time, and SHALL reject them as fatal errors.

#### Scenario: Source-level circular dependency
- **WHEN** two modules import each other (e.g., ModuleA uses ModuleB and ModuleB uses ModuleA)
- **THEN** the compiler SHALL detect and report a circular dependency error with the cycle and the offending import locations.

### Requirement: Compilation Options
Module compilation SHALL be configurable through compilation options that control target, optimization level, debug info, RTTI, bounds checking, exceptions, import validation strictness, and enabled feature set (e.g., inline assembly, superinstructions, vector primitives).

#### Scenario: Configurable compilation
- **WHEN** a module is compiled
- **THEN** the compiler SHALL honor compilation options covering target, optimization, debug, bounds checking, exceptions, import validation strictness, and feature enablement.

### Requirement: Dependency Graph and Version Constraints
Modules SHALL form a directed acyclic graph (DAG) of dependencies, and modules SHALL specify version constraints on their dependencies, with a reason for each constraint.

#### Scenario: Dependency graph construction
- **WHEN** a project's modules import other modules
- **THEN** the compiler SHALL build the transitive dependency DAG.

#### Scenario: Version constraint declaration
- **WHEN** a module declares a dependency
- **THEN** it SHALL be able to specify a version constraint and a reason for that constraint.

### Requirement: Module Metadata
Each module SHALL expose standard metadata: identity (name, version, description), authorship (author, license, homepage), classification (keywords, category), technical data (language, target requirements of minRomSize, minRamSize, required and optional features), dependencies, and build data (build date, compiler version, and compilation options). Modules SHALL also support custom metadata for specific uses and SHALL include embedded tests compiled under a tests flag and support cross-language integration tests.

#### Scenario: Standard module metadata
- **WHEN** a module is built
- **THEN** it SHALL carry identity, authorship, classification, technical target requirements, dependency, and build metadata.

#### Scenario: Custom metadata
- **WHEN** a module requires domain-specific metadata
- **THEN** it SHALL support custom metadata fields.

#### Scenario: Embedded and integration testing
- **WHEN** a module is developed
- **THEN** it SHALL be able to include embedded unit tests under a build flag and support cross-language integration tests.