# ADR-001: Struct-of-Arrays (SoA) IR Representation

Date: 2025-09-24

## Status

Accepted

## Context

Earlier designs used a traditional Object-Oriented (OO) AST node hierarchy (`PascalAstNode`, `ASTBuilder`) for the compiler pipeline. This introduced several problems for a retro-compiler targeting memory-constrained systems:

*   **Allocation Overhead**: Creating millions of transient node objects caused pressure on the .NET garbage collector, which is unacceptable for a fast compiler targeting embedded systems.
*   **Cache Inefficiency**: Pointer-chasing between nodes caused significant performance penalties on modern CPUs, and would be catastrophic if ever transpiled to targets with no MMU.
*   **Serialization Pain**: OO representations are difficult to serialize for incremental compilation or cross-stage optimization persistence.

## Decision

We will represent every stage of the Intermediate Representation (AST, HLIR, MLIR, LLIR) as a single `InstList` struct, utilizing a **Struct-of-Arrays (SoA)** layout.

This means:

1.  All instructions are stored in **parallel, flat arrays** indexed by instruction position.
2.  There are no objects for individual instructions, only raw numeric values stored in typed arrays (`byte[] Tags`, `uint[] FixedOps`, etc.).
3.  Operand references are `InstIndex` handles (integers), not OOP object references.

## Consequences

*   **Performance**: Compilation is significantly faster due to cache-friendly iteration and near-zero GC allocations during the transform passes.
*   **Tooling Clarity**: Tools like `SlabPrinter` access fields directly via array indices, making the compiler's internal state fully transparent and inspectable.
*   **Code Verbosity**: Access is slightly lower-level (array indexing vs. `.Left.Child()`), but the trade-off is considered worth it for the performance and determinism it grants on retro targets.
