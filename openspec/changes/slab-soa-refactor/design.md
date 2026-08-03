## Context

GameVM is a retro game console cross-compiler (Atari 2600, NES, SNES, Genesis, etc.) with a multi-IR pipeline:
Parse Tree → AST → HLIR → MLIR → LLIR → [Machine Code | Bytecode | DTC | ITC | STC].

The OOP→DOD migration (completed) established a functional-style pipeline: each phase reads an immutable input slab and produces a new output slab, all backed by a bump-pointer `ArenaAllocator`. All stages currently use opaque `uint[]` arrays with bit-packed `InstructionMetadata` headers, forcing decode operations on every access.

**Reference compilers analyzed for best-of-breed per stage:**
- **Zig** (`std.MultiArrayList`, `Air.zig`, `Ast.zig`): SoA for tokens/nodes/instructions (parallel `tags[]`/`data[]`/`extra[]`), `u32` index handles, `extra_data[]` pool.
- **Carbon** (`BlockValueStore`, `ValueStore`, `parse/tree.h`): block-based value store with slab allocation, typed `NodeId`/`InstId` handles, fixed-size `NodeImpl` (8 bytes) in `SmallVector`, postorder iterator.
- **Odin** (`parser.hpp`, `parser.cpp`): tagged-union AoS AST (`struct Ast { kind; union { variants } }`), arena-allocated per node, raw pointers. Valid for front-end mutation-heavy phase.

**Constraints:** .NET 10, C# 13, value types only in IR (`struct`/`readonly struct`), no managed refs, preserve five-stage pipeline + `SlabHeader` + `StringPool` + `ArenaAllocator`. Conventional commits; every change includes doc update.

## Goals / Non-Goals

**Goals:**
- Give each IR stage (AST, HLIR, MLIR, LLIR) the DOD layout best matched to its access patterns, chosen from reference compilers.
- Replace `uint[]` bit-packed slabs with SoA `InstList` at HLIR+.
- Introduce typed handles (`InstIndex`, `BlockId`, `SymbolId`, `SlotIndex`).
- Enable stride-only iteration on homogeneous field arrays (SIMD-ready).
- Keep functional (immutable-input/new-output) pipeline; arena-backed.
- Preserve `SlabHeader`, `StringPool`, `ArenaAllocator`, five-stage pipeline, dispatch strategies.

**Non-Goals:**
- SIMD vectorization of passes (follow-up; SoA layout is the prerequisite).
- SoA in front-end parsing phase (write-once, high node variance → Odin-style AoS is correct).
- Changing LLIR ISA or Atari 2600 bytecode semantics.
- New optimization passes.
- In-place/ping-pong arena reuse (functional style is deliberate and correct for this compiler).

## Decisions

### D1. Stage-by-Stage DOD Layout (from reference compilers)

| Stage | Data Shape | Reference Model | DOD Layout | Rationale |
|-------|-----------|-----------------|------------|-----------|
| **AST** | Tree | **Zig Ast** (SoA MultiArrayList) | **`InstList` SoA** | Unified layout across all stages: contiguous parallel arrays, typed `InstIndex` handles, `fixedOps`+``extra` pool for variable arity. Enables uniform tooling (one `SlabPrinter`), eliminates bit-packing debt entirely. Zig's own AST uses this pattern. |
| **HLIR** | Tree | **Zig Ast** (SoA MultiArrayList) | **`InstList` SoA** | Tree-shaped but undergoes opt passes (const fold, algebra, CSE, inlining). ≤3 operands for 95% nodes → `fixedOps` fast path. |
| **MLIR** | CFG (graph) | **Carbon SemIR** (ValueStore) + **Zig Air** | **`InstList` SoA + `CfgTable`** | Graph algorithms (dom, loops, DF) on `CfgTable`; instruction fields SoA for linear passes. `BlockId`/`SlotIndex` handles. |
| **LLIR** | List + CFG | **Zig Air** (SoA) | **`InstList` SoA + `CfgTable`** | Linear scan passes (peephole, regalloc, scheduling) stride `tags[]`/`fixedOps[]`. |
| **Bytecode/DTC/ITC/STC** | Flat list / tables | **LLVM MC** | **`byte[]` / `Inst[]` tables** | Final emit; contiguous instruction stream. Dispatch tables: DTC = label-address array, ITC = jump table, STC = call targets. |

### D2. SoA `InstList` Structure (HLIR/MLIR/LLIR)

```csharp
readonly struct InstList {
    readonly byte[]    tags;          // instruction kind enum (u8)
    readonly ushort[]  flags;         // Terminator|Diagnostic|reserved (u16)
    readonly ushort[]  argCounts;     // operand count (u16)
    readonly uint[]    fixedOps;      // MAX_FIXED_OPS * count (u32)
    readonly uint[]    extra;         // variable-length operand pool
    readonly uint[]    extraOffsets;  // per-inst offset into extra
    readonly uint[]    extraLengths;  // per-inst overflow length
    readonly int[]     blockIds;      // CFG: instruction → BlockId (0 = unassigned)
    readonly int       count;
    readonly uint      extraUsed;
}
```

**Rationale:** Mirrors Zig's `MultiArrayList` (tags + data) + `extra` pool, and Carbon's `ValueStore` slab pattern. `MAX_FIXED_OPS = 4` (chosen over design's 3): covers FOR_STATEMENT (5→1 extra) and most CALLs; memory delta negligible (+40 KB per 10K insts).

**Alternatives considered:**
- Always `extra` for all operands: simpler but kills SIMD stride on 95% of accesses.
- `MAX_FIXED_OPS = 3`: covers 90%; FOR(5) and CALL(>2 args) overflow more often. 4 is the sweet spot.
- Separate `extraOffset[]`+`extraLength[]` vs sentinel in `fixedOps`: separate arrays chosen (zero sentinel checks).

### D3. Typed Handles

```csharp
readonly struct InstIndex { readonly int value; }  // 0 = valid first inst; -1 = invalid
readonly struct BlockId   { readonly int value; }  // 0 = unassigned; -1 = invalid
readonly struct SymbolId  { readonly int value; }  // -1 = invalid
readonly struct SlotIndex { readonly int value; }  // 0 = valid first slot; -1 = invalid
```

**Rationale:** Matches Carbon's `NodeId`/typed IDs and Zig's `Node.Index`/`TokenIndex`. Separate types for type safety (prevents mixing InstIndex vs BlockId). Zero overhead in Release (readonly struct).

**Alternatives:**
- Single `Handle<T>` generic: less type safety.
- Raw `int`: no type safety.
- Separate types chosen.

### D4. Metadata Replacement — Direct Field Access

Remove `InstructionMetadata.Encode/Decode`. Passes access fields directly:
```csharp
// Before: var kind = DecodeKind(slab[offset]); var size = DecodeSize(...);
// After:  var kind = instList.Tags[instIdx];
//         var argCount = instList.ArgCounts[instIdx];
```

**Rationale:** Removes bit-shift/mask overhead (~30% of instruction-access instructions). Enables SIMD (`Vector128<byte>` on `Tags[]`, `Vector128<ushort>` on `Flags[]`/`ArgCounts[]`).

### D5. Fixed-Operand Fast Path with Extra Pool

`fixedOps` = `uint[MAX_FIXED_OPS * count]`. `GetOperands(instIdx)` returns `ReadOnlySpan<uint>`:
- If `argCounts[idx] <= MAX_FIXED_OPS`: span over `fixedOps[idx*MAX_FIXED_OPS ..]`.
- Else: first 3 from `fixedOps`, overflow from `extra[extraOffsets[idx] .. +extraLengths[idx]]`.

`InstListBuilder` appends via `SetOperands(idx, span)` with automatic extra pool growth (start 1024 u32, double on overflow).

### D6. CFG Integration — `BlockId` Handles

`InstList.blockIds[]` (int[]) maps instruction → `BlockId` (0 = unassigned). `CfgTable` remains separate parallel arrays keyed by `BlockId`:
```csharp
struct CfgTable {
    int[] blockOffsets;  // BlockId → first inst index
    int[] cfgEdges;      // flat (src, dst) pairs
    int[] edgeStart;     // per-block edge start
    int[] edgeCount;     // per-block edge count
}
```
CFG construction pass assigns `BlockId` handles and populates `blockIds[]` + `CfgTable`. Graph algorithms (dom tree, loops, dominator frontier) operate on `CfgTable`.

### D7. Functional Pipeline (Keep)

Each phase: `InstListIn → InstListOut` (new arena region). Do NOT switch to in-place/ping-pong:
- **Memory**: peak ~300–500 KB for typical programs; arena bump-pointer = zero per-object overhead, no GC pressure. Negligible on any host.
- **Benefits**: no aliasing, parallel-phase-ready, easy debugging (all slabs preserved), trivial rollback, easy incremental compilation.

**Alternatives:** In-place ping-pong (2 arenas) saves ~50% peak (~200 KB) at 10× complexity (ownership, aliasing, can't parallelize). Not worth it.

### D8. AST Stage — Move to `InstList` SoA (Zig-style)

The AST stage is converted to the same SoA `InstList` pattern used by all subsequent stages (rather than keeping a legacy `uint[]` slab with bit-packed `InstructionMetadata`).

**Rationale**:
- **Uniformity**: One mental model and one data structure (`InstList`) across the entire compiler pipeline.
- **Eliminate Bit-Packing entirely**: Deleting the opaque `uint[]` AST slab means we can completely remove the `InstructionMetadata` encode/decode helper and all bit-shifting math from the compiler core.
- **Ergonomics & Safety**: AST nodes are built and traversed via structured `InstIndex` handles instead of raw slab offset integers.
- **Tooling Reuse**: The `SlabPrinter` only needs to know how to print `InstList` parallel arrays.
- **Precedent**: Zig's `Ast.zig` uses `MultiArrayList(Node)` (SoA parallel arrays for tags/tokens/data) for memory compactness and ease of query.

The `PascalToSlabVisitor` will be rewritten to emit directly into an `InstListBuilder` for the AST stage, and `AstSlabToHlirSlabTransformer` will take an AST `InstList` and output an HLIR `InstList`.

### D9. Dispatch-Strategy Emitters (LLIR is accumulator-based)

LLIR is an **accumulator-based ISA** (primary register `A`), not a stack machine. This maps naturally to 8/16-bit hardware (6502, 68000, etc.) and avoids stack manipulation overhead on register-starved targets.

Final lowering produces dispatch-specific contiguous outputs from the same accumulator LLIR:
- **Bytecode** (future stack-VM targets): `byte[]` stream — *not used by current backends*
- **DTC** (Direct Threaded Code): `void*[]` / `uint[]` label-address table + code
- **ITC** (Indirect Threaded Code): jump-table `uint[]`
- **STC** (Subroutine Threaded Code): `Inst[]` with call targets
- **Native** (Atari 2600, NES, Genesis, etc.): machine code bytes
Each emitter iterates LLIR `InstList` linearly (`Tags[]`/`FixedOps[]`), consumes `CfgTable` for label resolution.

### D10. Serialization

- **In-memory**: SoA `InstList` (performance).
- **On-disk**: Keep existing `uint[]` slab format (compatibility). `InstList.ToSlab()` / `InstList.FromSlab()` linear conversions at I/O boundaries.
- `SlabHeader` (magic/version/stage/count/symbolTableOffset) retained.

## Risks / Trade-offs

| Risk | Mitigation |
|------|------------|
| **Migration scope** — touches every IR consumer | Single atomic commit (proven by OOP→DOD migration); comprehensive test coverage first; CI gate. |
| **SIMD not immediately used** | SoA layout is the prerequisite; vectorization incremental follow-up. |
| **Handle-to-offset translation** for codegen | `InstList.GetOperandOffset(instIdx, operandIdx)` helper at codegen time. |
| **Extra pool fragmentation** | Append-only; `InstList.CompactExtra()` if needed. |
| **Test migration effort** | Rewrite tests to assert on SoA field arrays + `extra` pool; `InstListBuilder` helper. |
| **MAX_FIXED_OPS wrong** | Measure operand distribution during implementation; adjust constant if needed. |

## Migration Plan

1. Add SoA primitives: `InstList`, `InstListBuilder`, handles, `InstFlags` in `GameVM.Compiler.Core/IR/Soa/`.
2. Rewrite `PascalToSlabVisitor` → keep emitting `uint[]` AST (no change).
3. Rewrite `AstSlabToHlirSlabTransformer` → `uint[]` AST → `InstList` HLIR.
4. Rewrite `HlirSlabToMlirSlabTransformer` → `InstList` HLIR → `InstList` MLIR (+ `CfgTable`).
5. Rewrite `MidToLowLevelTransformer` → `InstList` MLIR → `InstList` LLIR (+ `CfgTable`).
6. Rewrite optimizers (`DefaultMidLevelOptimizer`, `DefaultLowLevelOptimizer`) to iterate `Tags[]`/`FixedOps[]` spans.
7. Rewrite backends (`Atari2600CodeGenerator`) + dispatch emitters to consume `InstList`.
8. Rewrite `SlabPrinter` to print from SoA field arrays.
9. Update all tests to construct/assert on `InstList` state.
10. Delete `InstructionMetadata`, `InstructionMetadataFlags`, `SlabIterator`, `SlabCompactionUtility` (replaced by `InstList` + `InstListBuilder`).
11. Update docs (`docs/compiler/`, `docs/architecture/`, `docs/api/`).
12. CI gate: build + all tests green.

**Rollback:** Single commit revert if CI fails.

## Open Questions

1. **MAX_FIXED_OPS**: 4 (chosen) — verify operand distribution during implementation; adjust if FOR/CALL overflow rate is high.
2. **CFG table ownership**: Keep `CfgTable` separate (keyed by `BlockId`) vs fold into `InstList`. Leaning separate — CFG is a derived view.
3. **Dispatch emitter API**: One emitter per strategy vs one emitter with strategy switch? Existing `DispatchStrategy` enum + `CodeGenOptions` suggest one emitter with switch (current `Atari2600CodeGenerator`).
4. **Handle validity debugging**: `Debug.Assert` in `InstList` indexer getters (compiled out in Release).