# Data-Oriented Compilation Pipeline — Design

Status: draft
Branch: feature/dod-pipeline

Summary
-------
This document proposes a data-oriented (DOD) redesign of the **entire GameVM compilation
pipeline** — every stage from the language frontend through code generation — moving the
current object-heavy, pointer-rich intermediate representations (IRs) and their transformers
toward compact, POD-like, cache-friendly layouts (Structure-of-Arrays where beneficial).

The goal is high-throughput IR construction, analysis, and lowering; fast, deterministic
serialization; simpler memory management; and measurable performance improvements across the
whole `HLIR -> MLIR -> LLIR -> target` flow.

This supersedes the earlier HLIR-only proposal (`HLIR_DataOriented.md`): the scope is now the
full pipeline rather than a single IR level.

Scope
-----
In scope — the full compilation pipeline as orchestrated by `CompileUseCase.CompileInternal`:

1. Frontend AST -> HLIR construction (hand-written Pascal AST node types and
   `PascalAstToHlirTransformer`, `AstVisitor`, and the per-construct transformers/visitors).
2. HLIR data model (`HighLevelIR`).
3. HLIR -> MLIR lowering (`HlirToMlirTransformer`, exposed via `PascalFrontend.ConvertToMidLevelIR`
   and `MlirBuilder`).
4. MLIR data model (`MidLevelIR`).
5. Mid-level optimization (`IMidLevelOptimizer` / `DefaultMidLevelOptimizer`).
6. MLIR -> LLIR lowering (`IIRTransformer<MidLevelIR, LowLevelIR>` / `MidToLowLevelTransformer`,
   `LlirBuilder`).
7. LLIR data model (`LowLevelIR`).
8. Low-level optimization (`ILowLevelOptimizer` / `DefaultLowLevelOptimizer`).
9. Code generation (`ICodeGenerator` / `Atari2600CodeGenerator`, `M6502Emitter`).
10. Capability validation (`ICapabilityValidatorService`) where it walks IR.

**Explicitly out of scope: ANTLR-generated code.** The lexer, parser, and base visitor that
ANTLR generates from `src/GameVM.Compiler.Pascal/ANTLR/Pascal.g4` (e.g. `PascalLexer`,
`PascalParser`, `PascalBaseVisitor`) are machine-generated artifacts and are **not** to be
refactored to DOD. The DOD boundary at the frontend begins immediately after parsing: the
hand-written `AstVisitor`/`ProgramNode` tree and its conversion into HLIR are in scope; the
generated parse tree that feeds them is not. Where the parse tree is consumed, we adapt at the
boundary rather than modifying generated types.

Repository findings (current state)
------------------------------------
The pipeline today is a chain of independent object graphs. Each stage allocates a fresh tree
of reference-typed nodes, and each transformer walks one tree while allocating the next.

- **Frontend.** `PascalFrontend.Parse` runs ANTLR (generated `PascalLexer`/`PascalParser`),
  then an `AstVisitor` produces a hand-written `ProgramNode` AST (`PascalASTNode` subclasses),
  which `PascalAstToHlirTransformer` converts into `HighLevelIR`.
- **HLIR (`HighLevelIR.cs`).** A large object tree: `List<IRNode> TopLevel`,
  `Dictionary<string, IRSymbol> Globals`, `Dictionary<string, IRType> GlobalTypes`,
  `Dictionary<string, Function> GlobalFunctions`, `List<HlModule> Modules`. Statements and
  expressions are a deep class hierarchy (`Statement`, `Block`, `IfStatement`, `While`,
  `ForStatement`, `Assignment`, `Return`, `Expression`, `BinaryOp`, `UnaryOp`, `Literal`,
  `Identifier`, `FunctionCall`, `ArrayAccess`, ...), each carrying a `SourceFile` string and
  references to child nodes.
- **HLIR -> MLIR (`HlirToMlirTransformer`).** Walks the HLIR tree via `switch` on node runtime
  type and appends to `List<MLInstruction>` per function; strings are the universal operand type
  (`GetExpressionValue` returns a `string`).
- **MLIR (`MidLevelIR.cs`).** `List<MLModule>` -> `List<MLFunction>` -> `List<MLInstruction>`,
  where `MLInstruction` is an abstract base with subclasses `MLLabel`, `MLBranch`, `MLAssign`,
  `MLCall`. Operands are strings (`Target`, `Source`, `Arguments`).
- **Mid optimizer (`DefaultMidLevelOptimizer`).** Rebuilds the whole `MidLevelIR` into a new
  object graph; does string-based constant folding and duplicate-assignment removal.
- **MLIR -> LLIR (`MidToLowLevelTransformer`).** Allocates `LowLevelIR`, maps symbolic targets
  to addresses via a `Dictionary<string,string>`, flattens function instructions to a top-level
  list for test compatibility.
- **LLIR (`LowLevelIR.cs`).** `List<LLModule>` -> `List<LLFunction>` -> `List<LLInstruction>`
  plus a flattened top-level `List<LLInstruction>`. `LLInstruction` subclasses: `LLLoad`,
  `LLStore`, `LLCall`, `LLJump`, `LLLabel`. Operands are strings.
- **Low optimizer (`DefaultLowLevelOptimizer`).** Peephole over the flat instruction list
  (redundant load/store removal), rebuilding a new list.
- **Codegen (`Atari2600CodeGenerator` + `M6502Emitter`).** Iterates LLIR instructions, emits
  assembly strings, then assembles into a `byte[]` ROM.

Common cost drivers: many small heap allocations per node; pointer chasing during every walk;
runtime-type `switch`/`is` dispatch; `string` as the universal operand/type currency (parse +
allocate on nearly every access); full object-graph rebuilds in each pass.

Goals
-----
1. Make every IR level memory- and cache-friendly to accelerate construction, whole-program
   analyses, optimization, and lowering.
2. Preserve language- and target-agnostic semantics: each IR must still represent the same
   modules, types, globals, functions, statements/instructions consumed by the next stage.
3. Provide a smooth, stage-by-stage migration path with converters and compatibility views so
   the pipeline keeps working while stages are ported one at a time.
4. Replace `string`-typed operands/types with interned ids to remove per-access parsing and
   allocation.
5. Enable deterministic layout and compact serialization of any IR for cross-process tooling
   (e.g. separate optimization/analysis workers) and golden-file testing.

Design principles
-----------------
- **POD-first:** core IR data (types, signatures, blocks, variables, instructions) are plain
  value structs stored in contiguous vectors.
- **SoA for hot fields:** frequently scanned per-instruction fields (opcode, operand ids, type
  ids) live in parallel arrays for vectorizable, scan-friendly passes.
- **Indices, not references:** replace object references with 32-bit ids
  (`ModuleId`, `FunctionId`, `BlockId`, `InstrId`, `TypeId`, `SymbolId`, `StringId`). Ranges
  are `(start, count)` into shared arrays.
- **Interned strings:** one `StringPool` per compilation replaces the pervasive `SourceFile`
  and operand strings; equality and lookups become integer comparisons.
- **Immutable-by-default / rebuild-free passes:** passes operate on read-only views and emit
  new arrays or in-place edits via arenas rather than allocating fresh object graphs.
- **Arenas and pools:** bulk-allocate and bulk-free per-compilation data to improve locality
  and simplify ownership.
- **Uniform blob shape across levels:** HLIR, MLIR, and LLIR share the same container idioms
  (module table + SoA instruction/node stores + string pool + version header) so tooling,
  serialization, and tests are reusable.

Shared infrastructure (`GameVM.Compiler.Core/IR/Blob`)
------------------------------------------------------
A small set of building blocks reused by all three IR levels:

- **Id types:** `readonly struct StringId`, `TypeId`, `SymbolId`, `ModuleId`, `FunctionId`,
  `BlockId`, `InstrId` — each wrapping an `int` with a distinct type for safety.
- **`Range32`:** `readonly struct { int Start; int Count; }` for slices into arrays.
- **`StringPool`:** deduplicated string storage; `StringId Intern(string)`,
  `string Get(StringId)`.
- **`TypeTable`:** vector of `TypeRecord { TypeKind Kind; int Width; TypeId ElementType;
  int FieldLayoutId; StringId Name; }`.
- **`SymbolTable`:** vector of `SymbolRecord` (name id, type id, flags: constant/global,
  initial-value slot).
- **SoA instruction store:** `byte[] Opcodes`, `int[] Operands` (operand pool referenced by
  range), `TypeId[] Types`, plus a debug/source side-table indexed by `InstrId`.
- **`Arena`:** append-only bump allocator with capacity reservation and bulk reset.
- **`VersionInfo`:** magic, schema version, endianness, counts, checksum — one header per blob.

Per-stage design
================

1. Frontend AST -> HLIR (excluding ANTLR-generated code)
-------------------------------------------------------
The generated parser stays as-is. Immediately after `parser.program()`, adapt at the boundary:

- Keep the hand-written AST conceptually, but represent it data-oriented: an `AstBlob` with SoA
  node storage (`NodeKinds`, child `Range32`s, `StringId` names, literal payload slots) instead
  of one heap object per `PascalASTNode`. `AstVisitor` populates the blob; the generated parse
  tree is read but never stored.
- Alternatively (lower churn, phase 1): retain the current AST node classes but make
  `PascalAstToHlirTransformer` emit the HLIR blob directly rather than the HLIR object tree.
- `PascalAstToHlirTransformer` becomes an `AstBlob -> HLIRBlob` (or `ProgramNode -> HLIRBlob`)
  builder that reserves capacities up front and interns every string once.

2. HLIR data model (`HLIRBlob`)
-------------------------------
Top-level container:

- `HLIRBlob`
  - `modules: Vector<HLModule>`
  - `globals: Vector<SymbolRecord>`
  - `types: TypeTable`
  - `functions: Vector<HLFunction>`
  - `nodes: SoA statement/expression store` (module- or blob-level)
  - `strings: StringPool`
  - `errors: Vector<StringId>`
  - `version: VersionInfo`

- `HLModule (struct)`: `NameId`, `TypeRange`, `FunctionRange`, `VariableRange`, `Flags`.
- `HLFunction (struct)`: `NameId`, `Signature` (value type: return `TypeId`, param range),
  `BodyNodeRange`, `LocalRange`, `RequiredLevel`, `RequiredExtensionId (StringId)`, attributes.
- Statements/expressions: SoA node store with a `NodeKind` enum (Block, If, While, For, Repeat,
  Assignment, Return, ExpressionStmt, Literal, Identifier, BinaryOp, UnaryOp, FunctionCall,
  ArrayAccess). Per-node payload uses operand-id slots and child `Range32`s; a `TypeId` column
  carries expression types. Source locations move to a side-table indexed by node id.

3. HLIR -> MLIR lowering
------------------------
`HlirToMlirTransformer` is refactored to consume `HLIRBlob` and emit `MLIRBlob`:

- Replace runtime-type `switch(node)` with a tight loop over `NodeKinds` (dense switch on a
  byte enum; branch-predictor friendly).
- `GetExpressionValue` no longer returns `string`; it returns an operand id (into the MLIR
  operand pool / value table), so constant folding and identifier resolution work on ids.
- Emit instructions straight into the MLIR SoA store; record per-function instruction ranges.
- `MlirBuilder` and `PascalFrontend.ConvertToMidLevelIR` continue to delegate here, now
  producing the blob.

4. MLIR data model (`MLIRBlob`)
-------------------------------
- `MLIRBlob`: `modules: Vector<MLModule>`, `functions: Vector<MLFunction>`,
  `globals: Vector<SymbolRecord>`, SoA instruction store, `strings`, `version`.
- `MLModule (struct)`: `NameId`, `FunctionRange`.
- `MLFunction (struct)`: `NameId`, `InstrRange`.
- Instruction storage (SoA): `Opcodes: MLOpcode[]` (Label, Branch, Assign, Call), operand ids
  in the operand pool. E.g. `MLAssign` -> `{ target: SymbolId/ValueId, source: ValueId }`;
  `MLBranch` -> `{ target: StringId(label), condition: ValueId? }`;
  `MLCall` -> `{ name: StringId, args: Range32 into operand pool }`.

5. Mid-level optimization
-------------------------
`DefaultMidLevelOptimizer` operates over the MLIR SoA store:

- Constant folding works on the value/operand table (ids + a small constant pool), not on
  parsing `"(5 + 3)"` substrings.
- Dead-code / unreachable-block elimination becomes a scan over `Opcodes` with a compact
  bitset marking live instructions; output is a filtered range, not a rebuilt object graph.
- Duplicate-assignment removal uses a `SymbolId -> lastInstr` map keyed by int.

6. MLIR -> LLIR lowering
------------------------
`MidToLowLevelTransformer` consumes `MLIRBlob`, emits `LLIRBlob`:

- Address map keyed by `SymbolId`/`StringId` instead of `Dictionary<string,string>`.
- Emits into the LLIR SoA store; keeps the flattened top-level instruction range for current
  codegen/test expectations (as an explicit range view, not a duplicated list).

7. LLIR data model (`LLIRBlob`)
-------------------------------
- `LLIRBlob`: `modules`, `functions`, SoA instruction store, a top-level `InstrRange`,
  `strings`, `version`.
- `LLFunction (struct)`: `NameId`, `InstrRange`.
- Instruction storage (SoA): `Opcodes: LLOpcode[]` (Load, Store, Call, Jump, Label); operands
  as ids: `LLLoad -> { reg: RegId, value: ValueId }`, `LLStore -> { addr: AddrId, reg: RegId }`,
  `LLJump -> { target: StringId, condition: ValueId? }`, etc.

8. Low-level optimization
-------------------------
`DefaultLowLevelOptimizer` peephole passes scan the LLIR `Opcodes`/operand arrays directly
(e.g. redundant load/store detection compares operand ids in adjacent slots), emitting a
filtered instruction range.

9. Code generation
------------------
`Atari2600CodeGenerator` iterates the LLIR SoA store by `InstrRange`. Assembly emission can move
from building `List<string>` toward an opcode/operand table consumed by `M6502Emitter`, reducing
per-instruction string formatting until the final assembly text is needed. ROM layout logic is
unchanged. The MOS 6502 mnemonics/tables are target data, not generated code, so they remain in
scope for a data-oriented representation.

Capability validation
----------------------
`ICapabilityValidatorService.Validate(hlir, ...)` becomes a scan over the HLIR function table
(`RequiredLevel`, `RequiredExtensionId` columns) instead of a tree walk.

Migration strategy (compatibility-first, stage by stage)
--------------------------------------------------------
Because each transformer both consumes one IR and produces the next, we migrate along the
pipeline while keeping every boundary working:

1. Land shared `Blob` infrastructure (ids, ranges, string pool, SoA store, arena) with tests.
2. For each IR level, add the blob data model plus:
   - a **converter** `OldIR -> Blob` (reserve capacities, map references to ids, intern strings),
     and
   - a **compatibility view** (e.g. `BlobBasedHlirView`) exposing the minimal interface the
     existing consumer expects (Modules, Globals, function/instruction iteration) via
     index-based accessors.
3. Migrate one transformer at a time to read the upstream blob (via the view first, then native
   index loops) and write the downstream blob. Keep the object-model path available behind an
   option (`CompilationOptions.DataOriented`) until all stages are ported.
4. Once a stage's producer and consumer both use blobs, delete that compatibility view.
5. Finally, have the frontend emit `HLIRBlob` directly and remove the object-tree IRs.

Suggested order: HLIR blob + converter/view (unblocks `HlirToMlirTransformer`) -> MLIR blob ->
mid optimizer -> LLIR blob -> low optimizer -> codegen -> frontend direct emission -> remove
compatibility layers.

Serialization & versioning
--------------------------
- Each blob serializes to a compact binary format: header (`magic`, `schema version`,
  `endianness`, counts, checksum) followed by the string pool and SoA arrays.
- Deterministic layout enables golden-file tests and cross-process tooling (analysis/opt
  workers).
- Provide backward-compatible readers keyed off the schema version.

Passes, analyses, opportunities (enabled by the layout)
-------------------------------------------------------
- Opcode histograms / statistics: vector scans over `Opcodes`.
- Liveness/dataflow: compact bitsets indexed by `ValueId`/`InstrId`; SIMD-friendly set ops.
- Type/width analysis: scans over `TypeTable`.
- Hot-field grouping and deterministic serialization for LLIR lowering and codegen.

Tooling, testing, benchmarks
----------------------------
- Unit tests for each converter, compatibility view, and blob invariant (round-trip
  old -> blob -> view equals old-behavior).
- Golden serialization tests per IR level.
- Microbenchmarks per stage: build IR, run each optimizer, lower to the next IR, generate code;
  compare object-model vs blob.
- CI job running the benchmark suite on a limited dataset and reporting regressions.
- Keep existing pipeline tests green throughout via the compatibility views.

Roadmap & checklist (initial)
-----------------------------
- [ ] Write this pipeline-wide design doc (this file)
- [ ] Implement shared `Blob` infrastructure (ids, `Range32`, `StringPool`, `TypeTable`,
      SoA instruction store, `Arena`, `VersionInfo`) + tests
- [ ] HLIR: `HLIRBlob`, `HighLevelIR -> HLIRBlob` converter, `BlobBasedHlirView`
- [ ] Refactor `HlirToMlirTransformer` to consume the HLIR blob/view
- [ ] MLIR: `MLIRBlob`, converter, view; port `DefaultMidLevelOptimizer`
- [ ] LLIR: `LLIRBlob`, converter, view; port `MidToLowLevelTransformer`
- [ ] Port `DefaultLowLevelOptimizer` to LLIR SoA
- [ ] Port `Atari2600CodeGenerator` / `M6502Emitter` consumption to LLIR blob
- [ ] Port capability validation to HLIR table scan
- [ ] Frontend: emit `HLIRBlob` directly (AST blob or direct transformer emission)
- [ ] Add serialization + golden tests and microbenchmarks + CI job
- [ ] Remove compatibility views and legacy object-model IRs

Initial file & code layout suggestions
--------------------------------------
- docs/compiler/DataOrientedPipeline.md (this file)
- src/GameVM.Compiler.Core/IR/Blob/ (Ids.cs, Range32.cs, StringPool.cs, TypeTable.cs,
  SymbolTable.cs, InstructionStore.cs, Arena.cs, VersionInfo.cs)
- src/GameVM.Compiler.Core/IR/Blob/HLIRBlob.cs, MLIRBlob.cs, LLIRBlob.cs
- src/GameVM.Compiler.Core/IR/Transformers/HlirBlobConverter.cs, BlobBasedHlirView.cs (and
  MLIR/LLIR equivalents)
- src/GameVM.Compiler.Core/IR/Serialization/ (BlobWriter.cs, BlobReader.cs)
- tests/IRBlobTests/
- benchmarks/pipeline_microbench/

Open questions
--------------
1. Do we want one global string pool per compilation, or per-IR-level pools, to balance
   locality vs. dedup and cross-stage id stability?
2. Which instruction fields are "hot" per level and should be first-class SoA columns
   (opcode, type id, operand0/operand1 are likely candidates)?
3. How should the value/operand model unify literals, identifiers, and computed temporaries
   across HLIR/MLIR/LLIR (single `ValueId` space vs. per-level)?
4. Should the flattened top-level LLIR instruction list remain, or can codegen/tests move to
   per-function ranges (removing the duplication in `MidToLowLevelTransformer`)?
5. What non-pipeline consumers (DevTools, external analysis) need compatibility, and for how
   long?

Next steps
----------
- Land the shared `Blob` infrastructure and the HLIR blob + converter + view (compatibility
  first), then migrate `HlirToMlirTransformer`, proceeding stage by stage down the pipeline.
