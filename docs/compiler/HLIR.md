# High-Level Intermediate Representation (HLIR) [implemented]

## Overview [implemented]

The High-Level Intermediate Representation (HLIR) is the language-independent, first *lowered* stage in the compilation process. Unlike the old object-graph design, HLIR is **not a tree or object hierarchy** — it is a **Struct-of-Arrays (SoA) `InstList`** (parallel `uint[]` arrays), exactly like every other IR stage. The frontend (Pascal/CSharp) first emits an **AST `InstList`** (`AstNodeKind` tags) from the parse tree, and `AstSlabToHlirSlabTransformer` lowers that AST slab into the HLIR `InstList`.

The HLIR strips language-specific AST detail (declarations, expression trees, statement lists) down to a compact, sequential instruction stream that is trivially iterable and transformable. Because it is a plain `InstList`, it shares the exact same representation, builder, and operand-access API as MLIR and LLIR.

## Data Structure [implemented]

An HLIR program is an `InstList` — a set of *parallel* arrays indexed by instruction position:

| Array | Type | Meaning |
| ----- | ---- | ------- |
| `Tags` | `byte[]` | Instruction kind (an `MlirInstructionKind` / `HlirInstructionKind` value) |
| `Flags` | `ushort[]` | Bitwise `InstructionFlag` flags (`Terminator`, `Diagnostic`) |
| `ArgCounts` | `ushort[]` | Number of operands for the instruction |
| `FixedOps` | `uint[]` | Fixed operand slots — `MAX_FIXED_OPS` (4) slots per instruction |
| `Extra` | `uint[]` | Variable-length operand pool (overflow beyond 4 operands) |
| `ExtraOffsets` | `uint[]` | Per-instruction offset into the extra pool |
| `BlockIds` | `int[]` | CFG basic-block ID per instruction (`0` = unassigned) |

Each instruction has an immutable `InstMetadata` view (`Kind`, `Flags`, `ArgCount`, `BlockId`, `Index`) read back through `InstList[int]`. The public read API is:

- `ReadOnlySpan<uint> GetOperands(int instIdx)` — the operand span (fast path over `FixedOps` when `ArgCount <= 4`, otherwise a contiguous region of `Extra`).
- `uint GetOperand(int instIdx, int operandIdx)` — a single operand.
- `int GetOperandOffset(int instIdx, int operandIdx)` — absolute index into `FixedOps`/`Extra` (used at codegen time for address resolution).
- `byte GetKind(int instIdx)`, `ushort GetFlags(int)`, `ushort GetArgCount(int)`, `int GetBlockId(int)`.
- Stride-only iteration over raw planes: `Tags`, `Flags`, `ArgCounts`, `BlockIds`, `FixedOps`, `Extra`.
- `InstMetadata GetOperands(InstList)` / `Index` / `IsTerminator` / `IsDiagnostic` for convenience when holding a handle.

## Construction [implemented]

HL is produced via `InstListBuilder`, never hand-assembled arrays:

```csharp
var builder = new InstListBuilder();
builder.Add((byte)MlirInstructionKind.Label, InstructionFlag.None, 0, functionNameHash);
builder.Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 2, targetPoolOffset, valuePoolOffset);
InstList hlir = builder.Build();
```

`InstListBuilder` offers overloads for 0–4 operands plus an arbitrary-count `Add(kind, flags, blockId, ReadOnlySpan<uint>)` that spills into the extra pool when the count exceeds `InstConstants.MAX_FIXED_OPS` (4). `Build()` trims the arrays to exactly `Count`/`ExtraUsed`, and each `Add` returns the new instruction's index.

## Instruction Kinds [implemented]

The HLIR stage reuses the `MlirInstructionKind` enumerator (the kind byte is opaque at this level), the values most relevant to HLIR being:

| Kind | Value | Operands |
| ---- | ----- | -------- |
| `Label` | 128 | `[functionNameHash]` — marks a function entry point / control-flow label |
| `Branch` | 129 | `[target/targetId, ...]` — conditional or unconditional branch (may carry a label id) |
| `Assign` | 130 | `[targetPoolOffset, valuePoolOffset]` — assign a value (interned string) to a slot |
| `Call` | 131 | — function call |
| `Return` | 16 | `[expr?]` — return from the current function (optionally with a value) |
| `Variable`, `ExpressionStatement` | 8, 17 | treated as expression statements (assign to `_temp`) |

Operands are either **`InstIndex` handles** to other instructions (in the AST stage / structural nodes) or **StringPool offsets** interning names the HLIR transformer interns. Identifiers are referenced by pool offset (32-bit integer) throughout the pipeline — never by `string.GetHashCode()`, which is non-reversible.

HLIR is produced by `AstSlabToHlirSlabTransformer.Transform(astSlab)` and consumed by `IMidLevelOptimizer.OptimizeSlab(hlirSlab, stringPool, level)`, which optimizes and lowers it to MLIR (see `MLIR.md`).

## Purpose

HLIR serves as the foundation for further lowering (HLIR → MLIR → LLIR) and for optimizations that are still close to statements (dead-code elimination, constant folding, branch simplification). It retains just enough structure to recover identifiers via the `StringPool` while remaining a flat, cache-friendly array.
