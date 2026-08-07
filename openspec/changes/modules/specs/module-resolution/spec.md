# Module Resolution Specification

## Purpose
Defines GameVM's unified module resolution system, which enables seamless integration of modules across multiple programming languages through a canonical High-Level Intermediate Representation (HLIR). It implements a language-agnostic approach to dependency management, type resolution, and ABI compliance for polyglot development. Aspirational — not yet implemented.

## Requirements

### Requirement: Unified HLIR as Canonical Source of Truth
All languages SHALL compile to a single, language-agnostic HLIR, which serves as the single source of truth for types and interfaces, with a unified memory model and ABI, and a platform-independent representation.

#### Scenario: HLIR as universal intermediate
- **WHEN** a module in any supported language is compiled
- **THEN** it SHALL be compiled to the canonical language-agnostic HLIR as the first step after language-specific parsing, and HLIR SHALL be the common intermediate for all languages.

#### Scenario: HLIR key properties
- **WHEN** HLIR is produced
- **THEN** it SHALL be language-agnostic, carry complete semantics (declarations and implementations), remain type-rich, and preserve source mapping for debugging.

### Requirement: Structured Dependency Graph
The system SHALL maintain a semantic, structured dependency graph of module relationships with fine-grained dependency tracking to enable whole-program optimization.

#### Scenario: Semantic dependency tracking
- **WHEN** modules depend on each other
- **THEN** the system SHALL track those dependencies by HLIR module references rather than text-based includes, enabling whole-program optimization.

### Requirement: Compiler-Native Resolution
Resolution SHALL be native to the compiler rather than text inclusion, performing dependency analysis and supporting both static and dynamic linking models to enable precise dependency analysis.

#### Scenario: Compiler-native resolution
- **WHEN** a module references another module
- **THEN** the compiler SHALL resolve it semantically, supporting both static and dynamic linking models.

### Requirement: Core Components
The module resolution architecture SHALL include three core components: an HLIR Compiler (converts source modules to canonical HLIR, unifies the type system, handles ABI compliance), a Dependency Resolver (builds and analyzes the module dependency graph, implements a hybrid resolution algorithm, handles version constraints and conflicts), and a Module Loader (manages module lifecycle, handles dynamic loading and linking, maintains a module cache).

#### Scenario: Component responsibilities
- **WHEN** the module resolution system operates
- **THEN** the HLIR Compiler, Dependency Resolver, and Module Loader SHALL each perform their defined responsibilities: HLIR compilation and type/ABI management, dependency graph resolution with version handling, and module lifecycle/loading with caching.

### Requirement: Module Representation and Identification
Each module SHALL be a single, self-contained source entity identifiable by a logical name derived from its file path and its language's conventions. All entities SHALL be qualified by their module name in the HLIR; within a module, local names MAY be used without qualification.

#### Scenario: Module name derivation
- **WHEN** a source file is turned into a module
- **THEN** its logical name SHALL derive from its file path and language (e.g., `src/Game/Graphics.pas` -> `Game.Graphics`, `Game/Graphics.cs` -> `Game.Graphics`, `com/example/Graphics.java` -> `com.example.Graphics`, `game/graphics.py` -> `game.graphics`).

#### Scenario: Entity qualification
- **WHEN** a module is represented in HLIR
- **THEN** all entities SHALL be qualified by their module name (e.g., `Game.Graphics.Vector2`), and within the defining module local names SHALL be usable without qualification.

### Requirement: Language Integration
Each language frontend SHALL map its native module system to the canonical HLIR module format (C: header/source semantic analysis; C++: AST to HLIR; C#: metadata-based extraction; Java: class file parsing; Pascal: direct mapping; Lua: runtime module analysis).

#### Scenario: Per-language HLIR mapping
- **WHEN** a frontend compiles any supported language
- **THEN** it SHALL map the language's native module concept to HLIR modules according to its mapping strategy.

### Requirement: C/C++ Header Handling
For C/C++, headers (.h, .hpp) and source files (.c, .cpp, .cc) SHALL map to the same module; `#include` with either slash or angle-bracket form resolving to the same module; the preprocessor SHALL normalize all include paths to their canonical module names; and public declarations SHALL be extracted from headers during HLIR generation.

#### Scenario: Header/source unification
- **WHEN** C/C++ headers and source files are compiled
- **THEN** a header and its implementation unit SHALL map to the same module and includes in either form SHALL resolve to the same module.

#### Scenario: Include normalization
- **WHEN** a C/C++ include path is processed
- **THEN** the preprocessor SHALL normalize it to the canonical module name for HLIR.

### Requirement: Dependency Normalization
All language-specific import mechanisms SHALL be normalized to HLIR module references during compilation (e.g., Pascal units -> direct HLIR modules via `uses`, C# namespaces -> module prefixes via `using`, C/C++ headers -> modules via normalized includes, Java packages -> module hierarchy, JS imports -> module paths, Python -> dot notation, Lua require -> normalized module paths).

#### Scenario: Import normalization across languages
- **WHEN** a module imports another module in any supported language
- **THEN** the import SHALL be normalized to canonical HLIR module references.

### Requirement: Build Configuration
The compiler SHALL accept a build configuration that declares source modules by path, language, and build target, plus global build settings such as output directory, include paths, defines, and library sources.

#### Scenario: Source module declaration
- **WHEN** a project is configured
- **THEN** it SHALL declare each source module with its path, language, and target, and the compiler SHALL honor those settings.

### Requirement: Cross-Language Semantic Bridging
HLIR SHALL implement a "Common Denominator" mapping so the Standard Library and HAL are consumable by both procedural and object-oriented (OO) languages: (1) Procedural-to-OO Static Bridge — a procedural module is projected as a static class, global functions as static methods, global variables as static fields; (2) OO-to-Procedural Bridge — classes are projected as opaque structs/records of instance data, methods as static functions with an explicit self/first pointer, constructors as factory functions returning a pointer/handle to the new object, adopting an assembly-like naming convention such as `ClassName_MethodName`; (3) Type Normalization — HLIR enforces a strict mapping for interop types to prevent memory layout mismatches, projecting strings as length-prefixed `HLIR_STRING`, arrays as structures with a pointer and a length field, and pointers strictly typed to the HLIR entity they target.

#### Scenario: Procedural module consumed by OO language
- **WHEN** an OO language consumes a procedural module
- **THEN** the module SHALL be projected as a static class, its global functions as static methods, and its global variables as static fields.

#### Scenario: OO module consumed by procedural language
- **WHEN** a procedural language consumes an OO module
- **THEN** classes SHALL be projected as opaque structs of instance data, methods as static functions taking an explicit self pointer, and constructors as factory functions returning a handle to the new object with a `ClassName_MethodName` / `ClassName_Create` naming convention.

#### Scenario: Interop type normalization
- **WHEN** types cross language boundaries
- **THEN** HLIR SHALL normalize strings, arrays, and pointers to a common layout (length-prefixed strings, pointer-plus-length arrays, strictly typed pointers) to prevent memory layout mismatches.

### Requirement: Capability Bubbling
Modules SHALL be aware of the hardware they require, and the compiler SHALL propagate capability requirements (e.g., a HAL call `HAL_Tile_Draw` tags its module with `GFX_T`) through the dependency graph: if a module imports another module that requires a capability, the importer effectively requires it too. The compiler SHALL validate the total capability set of the main module against the target configuration, and a mismatch SHALL trigger a Hardware Contract Violation during resolution, preventing crashes from unsupported library features.

#### Scenario: Direct capability requirement
- **WHEN** a module uses a HAL call
- **THEN** the module SHALL be tagged with the corresponding capability requirement.

#### Scenario: Capability propagation and conflict
- **WHEN** a module imports another module that requires a capability
- **THEN** the importing module SHALL effectively also require the capability, and the compiler SHALL validate the main module's total capability set against the target; a mismatch SHALL produce a Hardware Contract Violation during resolution.

### Requirement: Graph-Based Resolution Lifecycle
The compiler SHALL enforce a strict directed acyclic graph (DAG) of modules; circular dependencies SHALL be a fatal compiler error. Resolution SHALL proceed in two passes: Pass 1 (Interface Recovery) performs a shallow scan of all modules, extracting only metadata (type names, function signatures, field offsets) and generating HLIR headers so all languages can see each other's symbols; Pass 2 (Implementation Lowering) performs full syntax/semantic analysis, converts implementation bodies to HLIR instructions, and resolves cross-module pointers and method calls using the Pass 1 metadata.

#### Scenario: DAG enforcement
- **WHEN** the module dependency graph contains a cycle
- **THEN** the compiler SHALL treat the circular dependency as a fatal error and fail the build.

#### Scenario: Two-pass resolution
- **WHEN** a project's modules are resolved and compiled
- **THEN** the compiler SHALL first recover interfaces from all modules, then lower implementations, resolving cross-module references from the first pass.

### Requirement: Version Conflict Resolution
The Dependency Resolver SHALL handle version constraints and resolve version conflicts within the module dependency graph.

#### Scenario: Version constraint handling
- **WHEN** a module declares a version constraint on a dependency
- **THEN** the resolver SHALL honor the constraint and resolve any version conflicts within the dependency graph.

### Requirement: Memory and Bank Affinity
For systems with banking, modules SHALL be units of allocation: critical HAL and core library code SHALL be placed in a fixed bank, game logic and asset-heavy modules SHALL be placed in switchable banks, and modules MAY request to be placed in the same bank via affinity tags to minimize bank-switching overhead.

#### Scenario: Fixed vs switchable bank placement
- **WHEN** a project targets a banking system
- **THEN** critical HAL/core code SHALL be placed in the fixed bank, game logic and asset-heavy modules in switchable banks.

#### Scenario: Bank affinity tagging
- **WHEN** modules need to share a bank
- **THEN** they SHALL be able to request that placement via affinity tags to minimize bank-switching overhead.