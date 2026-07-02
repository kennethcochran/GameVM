# Data-Oriented HLIR — Design

Status: draft
Branch: feature/data-oriented-hlir

Summary
-------
This document proposes a data-oriented redesign of the High-Level Intermediate Representation (HLIR) for GameVM. The goal is to move HLIR structures from object-heavy, pointer-rich representations to compact, POD-like, cache-friendly layouts (SoA where beneficial), enabling high-throughput IR passes, fast serialization, simpler memory management, and measurable performance improvements when lowering to MLIR and LLIR.

Repository findings (brief)
-------------------------
- Compiler docs already define the three-tier IR pipeline (HLIR -> MLIR -> LLIR) and an MLIR design focused on graphs and resource analysis (docs/compiler/MLIR.md).
- HlirToMlirTransformer.cs converts the existing HighLevelIR into MidLevelIR by syncing globals, processing modules, and converting function statement bodies into MLFunctions. This indicates existing consumers expect HLIR to expose modules, functions, globals, and structured statements.
- The Pascal frontend uses ANTLR grammar (src/GameVM.Compiler.Pascal/ANTLR/Pascal.g4) and an AST/visitor pipeline producing PascalAstNode tree types. AST -> HLIR conversion occurs in the frontend (frontend.Parse and frontend.ConvertToMidLevelIR are used in compilation flow).

Notes: I performed code search across the repository for the Pascal frontend and MLIR/HLIR artifacts; results may be incomplete. You can view more search results in the GitHub UI: https://github.com/kennethcochran/GameVM/search?q=pascal

Goals
-----
1. Make HLIR memory- and cache-friendly to accelerate whole-program analyses and MLIR lowering passes.
2. Preserve language-agnostic semantics: new HLIR must still represent modules, types, globals, functions, and statements at a semantic level consumable by HlirToMlirTransformer (and other tools).
3. Provide a smooth migration path from current HLIR to the data-oriented HLIR with conversion utilities and compatibility layers.
4. Enable easy serialization/versioning and deterministic layout for cross-process tooling (e.g., separate optimization worker processes).

Design principles
-----------------
- POD-first: HLIR core data (types, function signatures, basic blocks, variables) should be represented as plain-old-data structures (structs) with contiguous storage.
- SoA for hot fields: separate arrays for frequently accessed per-node fields (e.g., instruction opcode, operand indexes, type IDs) for SIMD/vectorizable passes and scan-friendly operations.
- Immutable-by-default / epoch-based updates: passes produce new versions or record diffs; reduce pointer chasing by using indices into vectors rather than references.
- Explicit metadata: attach access-pattern annotations, hotness heuristics, and memory budgets to modules and functions to guide layout decisions.
- Arenas and pools: allocate many small objects in arenas to improve locality and implement fast bulk frees.

High-level HLIR layout (proposal)
---------------------------------
Top-level container (Host process friendly):
- HLModuleTable
  - modules: Vector<Module>
  - globals: Vector<Global>
  - types: Vector<TypeRecord>
  - strings: StringPool (deduplicated)
  - metadata: Version, schema, endianness

Module (struct)
- name_id (string pool index)
- type_range (start,count)
- function_range (start,count)
- variable_range (start,count)
- flags (visibility, system)

Function (struct)
- name_id
- signature: FunctionSignature (value-type)
- basic_block_range (start,count)
- instruction_range (start,count) -- instructions live in a module-level instruction array
- local_range (start,count)
- attributes (inline/hot/entry)

Instruction storage (SoA-friendly)
- module.InstructionOpcodes: byte[] / enum[]
- module.InstructionOperands: int[] (sequence of operand indices)
- module.InstructionTypes: int[] (type ids)
- module.InstructionDebug: small index into debug table
- instructionIndex is the implicit instruction id; a function's instruction range points into these arrays

Types and values
- Types: vector of TypeRecords with a compact kind enum and fixed-size payload (width, element_type_id, field_layout_id)
- Values: value slots represented by indexes with a typed union encoded by a ValueKind enum

Reference model
- Replace most object references with 32-bit indices (ModuleId, FunctionId, InstrId, TypeId, StringId)
- Provide accessor wrappers so existing code can still get high-level objects when needed, but encourage index-based APIs for passes

API examples (C#-pseudocode)
---------------------------
public readonly struct HLModule { public int NameId; public int FunctionStart; public int FunctionCount; /* ... */ }

public class HLIRBlob
{
    public ArraySegment<HLModule> Modules;
    public int[] InstructionOpcodes;
    public int[] InstructionOperands;
    public TypeRecord[] Types;
    public string[] StringPool;
    public VersionInfo Version;
}

Conversion helper (existing HLIR -> new HLIR)
---------------------------------------------
- Provide a single-step converter that walks the current HighLevelIR tree and emits the new blob:
  - Reserve capacities based on counts to avoid reallocations
  - Map old references to new indices via dictionaries
  - Emit strings into the string pool
  - Emit instructions into contiguous arrays and record ranges on functions

Frontend changes (Pascal and others)
-------------------------------------
Two viable approaches:
1) Incrementally: Keep current in-memory HLIR and add a serializer that emits the data-oriented HLIR blob; runs during compilation when options.DataOriented = true. This minimizes frontend churn.
2) Directly emit data-oriented HLIR: Update frontend's HLIR construction APIs to populate the new data structures (faster; more invasive).

Given the current HlirToMlirTransformer reads HighLevelIR in object form, prefer approach (1) initially: implement a converter to new blob and add a compatibility shim that exposes index-based accessors to the transformer. Once passes are updated, migrate frontends to emit the blob directly.

Changes to HlirToMlirTransformer
--------------------------------
- Create a compatibility layer: BlobBasedHlirView that implements the minimal interface HlirToMlirTransformer expects (Modules, Globals, Function bodies iteration). Implement iteration using indices and narrow accessors.
- Gradually refactor transformer code to use the blob view APIs; replace foreach node traversals with tight index-based loops for performance-critical paths.

Memory / ownership model
------------------------
- HLIRBlob owned by the compilation session; passes operate on read-only views producing diffs or new HLIRBlobs.
- For incremental compilation, implement small mutable arenas for localized edits and a fast merge operation into the main blob.

Passes, analyses and opportunities
---------------------------------
- Scans and statistics: compute opcode histograms from InstructionOpcodes (fast vector scan)
- Liveness and dataflow: use compact bitsets indexed by ValueId; bitset operations become SIMD-friendly
- Type lowering and width analysis: operate over TypeRecord vectors
- Hot-field grouping: annotate fields to be colocated in output serialization and LLIR lowering

Serialization & Versioning
--------------------------
- Define a compact binary blob format with a header: magic, version, schema flags, counts, checksums
- Provide backward-compatible readers for older compiler tool versions

Tooling, testing, benchmarks
---------------------------
- Add microbenchmarks: iterate function instructions, compute liveness, lower to MLIR; compare old HLIR vs data-oriented HLIR
- Unit tests for converter and compatibility layer
- CI job: run benchmarks (limited dataset) on PRs and report regressions

Roadmap & Checklist (initial)
-----------------------------
- [ ] Create design doc (this file)
- [ ] Implement HLIRBlob data model (docs + code skeleton)
- [ ] Implement converter: HighLevelIR -> HLIRBlob (compatibility shim)
- [ ] Implement BlobBasedHlirView used by HlirToMlirTransformer
- [ ] Add unit tests for conversion and blob invariants
- [ ] Add microbenchmarks and CI workflow
- [ ] Iterate: refactor frontends to emit blob directly, remove compatibility layer

Initial file & code layout suggestions
-------------------------------------
- docs/compiler/HLIR_DataOriented.md (this file)
- src/GameVM.Compiler.Core/IR/Blob/HLIRBlob.cs (.h/.cpp equivalents for non-C# languages)
- src/GameVM.Compiler.Core/IR/Transformers/BlobBasedHlirView.cs
- src/GameVM.Compiler.Core/IR/Transformers/HlirBlobConverter.cs
- tests/HLIRBlobTests/
- benchmarks/hlir_blob_microbench/

Open questions
--------------
1. What consumers aside from HlirToMlirTransformer require immediate compatibility? (e.g., tooling, external analysis tools)
2. Do we want a single global string pool or per-module string pools to balance locality vs deduplication?
3. Which fields in current instructions are "hot" and should be moved to SoA arrays first? (opcode, type id, operand0, operand1 are likely candidates)

Next steps I can take now
------------------------
- Create the initial HLIRBlob.cs skeleton and BlobBasedHlirView on branch feature/data-oriented-hlir.
- Implement the converter that walks the existing HighLevelIR and emits the blob (compatibility-first approach).

Which next step do you want me to take? (create docs only / add skeleton code / implement converter / open draft PR)
