# GameVM Context & Glossary

> **CONTEXT.md** is the high-fidelity, single-file source of truth for the GameVM compiler. It is **owned by the `docs/AGENTS.md` "Three-Pocket Strategy"** (Pocket #1: Implemented Reality). If it describes something not in the code, move the description to `openspec/specs/` as a spec.

---

## 1. Domain Glossary

| Term | Definition |
|---|---|
| **InstList** | Struct-of-Arrays (SoA) instruction container. Holds `byte[] Tags`, `ushort[] Flags`, `ushort[] ArgCounts`, `uint[] FixedOps`, `uint[] Extra`, `uint[] ExtraOffsets`, `int[] BlockIds`. |
| **InstListBuilder** | Auto-resizing incremental builder for `InstList`. |
| **StringPool** | Interned byte-buffer for identifiers; returns `uint` offsets. Shared across all IR stages. |
| **AST** | Parse-time `InstList` (tags = `AstNodeKind`). |
| **HLIR** | Language-agnostic `InstList` (tags = `MlirInstructionKind`, operands = `StringPool` offsets). |
| **MLIR** | Target-independent optimization stage `InstList`. |
| **LLIR** | Virtual Machine ISA `InstList` (`LlirInstructionKind`). |
| **IR Transformation** | `IIRSlabTransformer.TransformSlab(inputSlab, stringPool) -> InstList`. |
| **Mid Level Optimizer** | `IMidLevelOptimizer.OptimizeSlab(hlirSlab, stringPool, level) -> InstList`. |
| **Low Level Optimizer** | `ILowLevelOptimizer.OptimizeSlab(llirSlab, stringPool, level) -> InstList`. |
| **Backend** | `ICodeGenerator.GenerateFromSlab(llirSlab, stringPool, options) -> byte[]`. |
| **Frontend** | A language parser (`PascalFrontend`, `CSharpFrontend`) emitting an AST `InstList`. |
| **String Handle** | A `uint` offset into the `StringPool`. |
| **InstIndex** | A readonly struct wrapping an instruction position in an `InstList`. |
| **BlockId** | A readonly struct wrapping a basic-block ID. |
| **SymbolId** | A readonly struct wrapping a symbol-table offset. |
| **SlotIndex** | A readonly struct wrapping a local/abstract-stack slot. |

---

## 2. High-Level Architecture

GameVM is a **cross-compiler**: complex analysis, optimization, and transformation happen on the host (PC/Mac/Linux), producing a tailored binary (ROM or disk image) for a retro gaming target.

```
Pascal / C# source
   ↓ ParseToSlab         (PascalFrontend / CSharpFrontend)
AST InstList              (tags = AstNodeKind, operands = child InstIndex / StringPool)
   ↓ AstSlabToHlirSlabTransformer
HLIR InstList             (tags = MlirInstructionKind, operands = StringPool offsets)
   ↓ HlirSlabToMlirSlabTransformer
MLIR InstList             (optimizable)
   ↓ IMidLevelOptimizer.OptimizeSlab
Optimized MLIR
   ↓ MidToLowLevelTransformer.TransformSlab
LLIR InstList             (tags = LlirInstructionKind, 6502-adjacent)
   ↓ ILowLevelOptimizer.OptimizeSlab
Optimized LLIR
   ↓ Atari2600CodeGenerator.GenerateFromSlab
Atari 2600 ROM (4KB, $F000-$FFFF)
```

### Key Invariants

- **No OOP AST node hierarchy:** The parse tree is lowered directly into an AST `InstList` via a slab visitor (`PascalToSlabVisitor`). There is no `PascalAstNode` class tree.
- **Shared StringPool:** A single `StringPool` is created during `ParseToSlab` and threaded through every stage. Symbols never leave the pool as strings.
- **Handle-based addressing:** Cross-stage references between instructions, blocks, symbols, and slots use strongly-typed readonly structs (`InstIndex`, `BlockId`, `SymbolId`, `SlotIndex`).
- **Immutable InstList:** All `InstList` instances are constructed via `InstListBuilder` and are treated as immutable thereafter. Optimizations produce a new `InstList`.

### Core APIs (C#)

```csharp
// Located in GameVM.Compiler.Core.IR.Soa
public readonly struct InstList { ... }
public struct InstListBuilder { ... }

// Located in GameVM.Compiler.Core.IR.Interfaces
public interface IIRSlabTransformer {
    InstList TransformSlab(InstList inputSlab, StringPool stringPool);
}

// Located in GameVM.Compiler.Application.Services
public interface IMidLevelOptimizer {
    InstList OptimizeSlab(InstList hlirSlab, StringPool stringPool, OptimizationLevel optimizationLevel);
}
public interface ILowLevelOptimizer {
    InstList OptimizeSlab(InstList llirSlab, StringPool stringPool, OptimizationLevel optimizationLevel);
}

// Located in GameVM.Compiler.Core.CodeGen
public interface ICodeGenerator {
    byte[] GenerateFromSlab(InstList llirSlab, StringPool stringPool, CodeGenOptions options);
}
```

### Operand Access

```csharp
// Fast path: <= 4 operands from FixedOps array
// Slow path: > 4 operands from Extra pool + ExtraOffsets
ReadOnlySpan<uint> InstList.GetOperands(int instIdx);
uint InstList.GetOperand(int instIdx, int operandIdx);
int InstList.GetOperandOffset(int instIdx, int operandIdx); // absolute index into flat operand arrays
```

### IR Stage Instruction Kinds

The AST/HLIR/MLIR stages reuse the `MlirInstructionKind` tag bytes; only the low-level stage switches to `LlirInstructionKind`:

| Kind (`MlirInstructionKind`) | Byte | Operands | Meaning |
| `Label` | 0x80 | `[functionNameHash]` | function entry / control-flow label |
| `Branch` | 0x81 | `[condition?, targetLabel]` | conditional / unconditional branch |
| `Assign` | 0x82 | `[targetSlot, valueSlot]` | data movement (slot / pool-offset operands) |
| `Call` | 0x83 | — | function call |
| `Return` | 0x10 | `[expr?]` | return from current function |
| `Variable` / `ExpressionStatement` | 0x08 / 0x11 | — | expression nodes re-emitted as `Assign` in most cases |

Operands are either **slot IDs** (`StringPool` offsets for temporaries/constants/labels) or **`InstIndex` handles** referencing other instructions. Identifiers are always interned pool offsets — never `string.GetHashCode()`, which is non-reversible.

**LLIR ISA** — a minimal, 6502-adjacent virtual ISA (`LlirInstructionKind`):

| Kind | Byte | Operands | Description |
|---|---|---|---|
| `Label` | 0xC0 | `[funcNameHash]` | entry point / control-flow label |
| `Load` | 0xC1 | `[reg, value]` or `[reg, addrLow, addrHigh]` | load immediate or from address |
| `Store` | 0xC2 | `[reg, addrLow, addrHigh?]` | store; **zero-page** (`0x85`) when address `<$100`, else absolute (`0x8D`) |
| `Call` | 0xC3 | `[target]` | JSR |
| `Jump` | 0xC4 | `[target]` | JMP |
| `Branch` | 0xC5 | `[condition?, target]` | conditional branch (JUMPZ/JUMPN etc.) |
| `Return` | 0xC6 | — | RTS |
| `Syscall` | 0xC7 | `[vector]` | OS / TIA vector trap |

Register encoding: `0` = accumulator (A), `1+` = R0, R1, …. Addresses are low-byte-first; zero-page form is chosen when the resolved address `< $100` (variable in Atari zero-page `$80`-`$FF`). Function names and identifiers are 32-bit `StringPool` hashes resolved by `MidToLowLevelTransformer`.

### Error Handling

- Errors are categorised **Fatal / Error / Warning / Info**, reported through `GameVmException` (C#) carrying an `ErrorCode` and a context dictionary.
- Compiler diagnostics are written to a lightweight **Diagnostic Journal**, never mixed into the executable instruction slab (metadata header has a Diagnostic Present flag on the affected instruction).
- Error codes are grouped by domain: general (0x0001 OutOfMemory, 0x0002 InvalidArgument), file (0x1000+), network (0x2000+), module (0x3000+).

---


## 3. Current Platform Reality

- **Backend:** Atari 2600 (`Atari2600CodeGenerator`) — emits 4KB ROM images (`$F000`-`$FFFF`). Uses zero-page pointer promotion and strict cycle budgeting.
- **Atari 2600 codegen mechanics:** `GenerateFromSlab` emits 6502 opcodes — `LLIR_LOAD` → `LDA #imm` (`0xA9`); `LLIR_STORE` → `STA` zero-page (`0x85`) when addr `< $100`, else absolute (`0x8D`). A loaded cartridge has total machine control, so the emitted program never returns: codegen appends a **self-loop** (`JMP *` → `0x4C <self>`) so the CPU holds the final state instead of falling into zeroed ROM. Reset/IRQ vectors (`$FFFC`-`$FFFF`) point to `$F000`. Variables resolve via the shared `StringPool`: known TIA registers map to hardware (`COLUBK`→`$09`, `COLUPF`→`$08`, `COLUP0`→`$06`, `COLUP1`→`$07`); others allocate sequentially from zero-page `$80`.
- **Frontend:** Pascal (`PascalFrontend`, `PascalToSlabVisitor`) — parses directly into an AST `InstList`.
- **Optimization:** `DefaultMidLevelOptimizer` (host-side) and `DefaultLowLevelOptimizer` (target-aware, Atari-specific).
- **Dispatch:** Direct Threaded Code (DTC) and Token Threaded Code (TTC) are implemented. Subroutine Threaded Code (STC) and Indirect Threaded Code (ITC) are planned but currently *outdated/aspirational* and live in `openspec/specs/`.
- **Testing:** 484 tests, `dotnet test` must pass. SonarQube quality gate is enforced on CI.

### Aspirational / Not Yet Implemented

The following live in `openspec/specs/` as proposals, not here:
- Hardware Abstraction Layer (HAL) for graphics/audio/input.
- Virtual Machine target runtime with dynamic loading.
- Package management system.
