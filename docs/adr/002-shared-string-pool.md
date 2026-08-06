# ADR-002: Shared String Pool Across IR Stages

Date: 2025-09-24

## Status

Accepted

## Context

The GameVM compiler supports multiple frontends (Pascal, C#) and outputs to multiple backends (NES, SNES, etc.). Passing resolved symbol names and identifier strings around the pipeline as `string` objects led to a massive amount of redundant memory usage and string comparison overhead.

## Decision

A single `StringPool` instance is created at parse time by the first frontend. This pool is **passed by reference (as a `StringPool` struct holding the internal byte buffer)** through every stage of the pipeline (`TransformSlab`, `OptimizeSlab`, `GenerateFromSlab`).

All operands that represent identifiers (variable names, function names, labels) are stored as 32-bit `uint` offsets into this pool, rather than as string values.

## Consequences

*   **Fast Symbol Resolution**: Looking up a symbol is a simple array lookup into the `StringPool`'s byte buffer.
*   **Deterministic Output**: Because string offsets are stable, the layout of the final binary is deterministic across builds, which is crucial for ROM checksum verification and reproducible builds.
*   **No Per-Stage Interning**: A `string` is interned exactly once, and the resulting offset remains valid for the entire lifetime of the compilation.
