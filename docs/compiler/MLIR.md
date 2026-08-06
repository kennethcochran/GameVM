# Mid-Level Intermediate Representation (MLIR) [implemented]

## Overview [implemented]

The Mid-Level Intermediate Representation (MLIR) is a **host-only** IR stage focused on *resource management* and *optimization opportunities*. Like HLIR and LLIR, MLIR is a **SoA `InstList`** with parallel arrays (`byte[] Tags`, `ushort[] Flags`, etc.) and the same fast-path operand accessors (`GetOperands`, `GetOperand`, `GetOperandOffset`).

The MLIR abstracts away hardware details while preserving enough structure for optimizations such as dead-code elimination, constant propagation, and register allocation. All transformation passes (e.g., `IMidLevelOptimizer.OptimizeSlab`) operate directly on the `InstList` structure, never requiring an object-graph or extra pointer chasing.

## Data Structure [implemented]

An MLIR program is an `InstList` — the exact same parallel-array layout as HLIR and LLIR. The instruction set uses `MlirInstructionKind` values:

| Kind | Hex | Operands | Notes |
| ---- | --- | -------- | ----- |
| `Label` | 0x80 | `[functionNameHash]` — basic-block entry (also acts as function entry) |
| `Branch` | 0x81 | `[conditionPoolOffset?, targetLabel]` — conditional branch (fallthrough or jump) |
| `Assign` | 0x82 | `[targetSlot, valueSlot]` — data movement (both are `uint` slot identifiers) |
| `Call` | 0x83 | — low-level call (`MidToLowLevelTransformer` maps to `LlirInstructionKind.Call`) |
| `Return` | 0x10 | `[expr?]` — return from the current function |
| `Variable`, `ExpressionStatement` | 0x08, 0x11 | expression nodes re-emitted as `Assign` in most cases |

Operands are either **slot IDs** (offsets into the `StringPool` for temporaries, constants, labels) or **`InstIndex` handles** when referencing other MLIR instructions. The `HlirSlabToMlirSlabTransformer` lowers from HLIR by reading operand spans with `GetOperands(i)` and, when necessary, re-emitting into the MLIR `InstListBuilder`.

## Construction [implemented]

MLIR is produced by a single transformer:

```csharp
InstList mlir = new HlirSlabToMlirSlabTransformer()
    .Transform(hlirSlab);
```

The transformer iterates through each HLIR instruction and emits zero or more MLIR instructions into a new `InstListBuilder`. It uses the same `InstList` builder API (e.g., `Add((byte)MlirInstructionKind.Assign, InstructionFlag.None, 2, target, value)`).

## Operand Access Patterns [implemented]

All MLIR readers use the canonical SoA accessors:

- `ReadOnlySpan<uint> GetOperands(int instIdx)` — always safe, returns either contiguous `FixedOps` (<=4) or `Extra` region (>4). Fast path for the common case (<=4 operands).
- `uint GetOperand(int instIdx, int operandIdx)` — for single accesses (e.g., `GetOperand(instIdx, 0)`).
- `int GetOperandOffset(int instIdx, int operandIdx)` — absolute index into the underlying flat operand arrays for codegen.

Because the underlying arrays are `uint[]`, all reads are cheap memory loads without per-instruction object overhead.

## Optimizations [implemented]

The `IMidLevelOptimizer.OptimizeSlab(InstList hlirSlab, StringPool stringPool, OptimizationLevel)` takes the HLIR `InstList` and produces an optimized MLIR `InstList`. Typical optimizations are:

- **Dead-Code Elimination** — instructions with unused results are removed, and block structure is updated via `InstListBuilder`.
- **Constant Folding** — simple arithmetic on literal operands is evaluated at compile time.
- **Common Subexpression Elimination** — identical operand spans are deduplicated, possibly promoting them to a constant pool entry.
- **Loop Optimization** — loop-invariant code motion, induction variable simplification.
- **Register Allocation Previews** — estimate the minimal virtual register pressure per function (works over the `BlockIds` plane).

Optimizations never reconstruct the old OOP AST nodes; they work directly on the flat SoA layout.

## Purpose [implemented]

MLIR forms the *host-level optimization sweet spot*. It is the stage where language-specific features (e.g., high-level types) are lowered to platform-agnostic resource models (memory width, cycle budget). The `InstList` format allows downstream phases (register allocation, code generation) to proceed without transformation, preserving determinism and enabling cycle-accurate backends (e.g., Atari 2600).

Because everything is an `InstList`, the implementation of optimizations, transformation, and code emission can share identical iteration patterns — a key reason the new DOD pipeline is both simpler and faster than the old OOP AST.
