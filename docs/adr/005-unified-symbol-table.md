# ADR-005: Unified Symbol Table in HLIR Module

The compiler uses a unified symbol table embedded in the HLIR module file, following the Java `.class` pattern: code, symbols, types, scopes, and optional debug attributes are all sections in one self-describing format. Debug info is an optional attribute layer, not a separate file.

## Context

GameVM needs symbol information for three consumers: debug info (line numbers, variable locations for debuggers), module resolution (matching functions/parameters/types across compiled HLIR modules), and profiling/instrumentation (mapping execution back to source). The compiler also needs symbol data during semantic analysis, type checking, and error reporting.

The existing `StringPool` interns identifiers as `uint` offsets but carries no metadata — no types, no scopes, no source locations. The current `IRSymbol` class exists but is unused in the pipeline. The `HashedSymbolTable` is declared but barely wired.

Research into 11 production compilers (Carbon, Zig, GCC, LLVM, .NET/RyuJIT, JDK/javac, Roslyn, Odin, ack, UCSD Pascal, do-compiler) confirmed a consistent pattern: the two DoD compilers (Carbon, Zig) use flat arrays indexed by 32-bit typed handles — the same pattern GameVM already uses for `InstList` and `StringPool`. The .NET and Java compilers both use unified structures where the symbol table IS the debug info source, with debug data as optional attributes or separate serializations.

## Decision

**Unified structure.** The symbol table is a single SoA (Struct-of-Arrays) data structure that serves both the compiler during compilation and external tools (debuggers, profilers, module resolvers) after compilation. Debug info is an optional attribute layer within the HLIR module, not a separate data structure or file.

**SoA storage.** The symbol table uses parallel arrays indexed by typed `uint` handles (`SymbolId`, `TypeId`, `ScopeId`), consistent with `InstList` and the Carbon/Zig pattern.

**Flat handles into existing HLIR types.** Symbol type references are `TypeId` handles into the existing HLIR type infrastructure (`IRType`, `IRField`, `IRParameter`). No duplicate type representation.

**Flat scope handles.** Declaration contexts (which container owns a symbol) are tracked via a `ScopeId` handle into a `DeclarationContext` table. Each entry carries `ParentContextId` + `OwnerSymbolId` + `Kind`. This is structural ownership, not language-specific visibility rules.

**Source files as StringPool offsets.** Source file paths are interned in the existing `StringPool`. `FileIndex` is a `uint` offset into the pool.

**Self-describing HLIR module.** The HLIR module file carries all metadata in-section format:

```
[Header]          magic, version, flags
[StringPool]      interned strings (identifiers, file paths, type names)
[SymbolTable]     SoA: Name[], Kind[], TypeId[], ScopeId[], FileId[], Line[], Col[], StorageClass[]
[TypeTable]       SoA: TypeId → (kind, size, signedness, element TypeId, field count...)
[ScopeTable]      SoA: ScopeId → (parent ScopeId, owner SymbolId, kind)
[Code]            HLIR (not MLIR/LLIR — preserves semantic context for whole-program optimization)
[DebugAttributes] optional: LineNumberTable, LocalVariableTable, ScopeTree
```

HLIR is stored in the module (not MLIR or LLIR) to preserve type information, scope structure, and source locations needed for whole-program optimization and cross-module type checking.

## Considered Options

- **Separate symbol table and debug info files** (LLVM pattern): Rejected because GameVM is a self-contained compiler, not a library with multiple frontends. The separation exists in LLVM because the debug info layer is shared across Clang/Rust/Swift. GameVM has no such architectural need.
- **Extend StringPool with metadata slots**: Rejected because it bloats the hot path (string interning) with cold data (debug info). Two structures composed at the interface boundary is cleaner.
- **AoS (Array-of-Structs)**: Considered but rejected in favor of SoA. SoA is cache-friendly for iteration ("list all variables in scope") and allows adding new fields without changing the stride of existing arrays.
- **Store MLIR/LLIR in the module** (.NET/Java pattern): Rejected because lower-level IRs strip semantic context needed for whole-program optimization. HLIR preserves the full type system and scope structure.

## Consequences

*   **Single source of truth**: Symbol data, debug info, and module metadata are never out of sync — they're the same structure.
*   **Module resolution is self-describing**: A consumer reads the HLIR module file and gets everything: code, symbols, types, scopes, debug info. No separate files to track.
*   **Debug info is opt-in**: `DebugAttributes` section is absent in release builds, present in debug builds. The compiler pays no cost for debug info it doesn't emit.
*   **Whole-program optimization is supported**: HLIR in the module preserves semantic context. The optimizer can read imported modules' HLIR and combine it with the current module before optimization.
*   **Extensible**: New symbol fields, type kinds, or debug attributes can be added to the SoA arrays without breaking existing consumers. Version field in the header handles schema evolution.
