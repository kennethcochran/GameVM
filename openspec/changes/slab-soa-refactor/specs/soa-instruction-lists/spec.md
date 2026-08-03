## ADDED Requirements

### Requirement: SoA Instruction List (InstList)
The compiler MUST represent each IR stage body (HLIR, MLIR, LLIR) as a single `InstList` value-type struct containing parallel arrays for homogeneous instruction fields, plus a separate pool for variable-length operands.

#### Scenario: Creating a new instruction list
- **WHEN** a new IR stage begins (e.g., HLIR after AST lowering)
- **THEN** the system allocates an `InstList` with all parallel arrays sized to initial capacity (or zero), and returns it by value.

#### Scenario: Adding an instruction
- **WHEN** a transformer emits an instruction (e.g., `AstSlabToHlirSlabTransformer` creating an HLIR assignment)
- **THEN** it appends the instruction's tag, flags, argCount, fixed operands to the respective parallel arrays, and returns the new `InstIndex`.

#### Scenario: Accessing instruction fields
- **WHEN** a pass reads an instruction (e.g., `DefaultMidLevelOptimizer` processing `MLIR_ASSIGN`)
- **THEN** it indexes directly into `instList.Tags[idx]`, `instList.Flags[idx]`, `instList.ArgCounts[idx]`, `instList.FixedOps[idx * MAX_FIXED_OPS + operandIdx]` without decode overhead.

### Requirement: Typed Index Handles
All cross-references in IR MUST use typed `readonly struct` handles (`InstIndex`, `BlockId`, `SymbolId`, `SlotIndex`) wrapping `int`, never raw slab offsets or object references.

#### Scenario: Referencing an instruction operand
- **WHEN** an instruction needs to reference another instruction's result (e.g., SSA value)
- **THEN** the operand stores an `InstIndex` handle; passes resolve it via `instList.GetOperand(instIdx, operandIdx)` returning `InstIndex`.

#### Scenario: CFG block reference
- **WHEN** a branch instruction targets a basic block
- **THEN** it stores a `BlockId` handle; the CFG tables (`CfgTable`) are indexed by `BlockId`.

#### Scenario: Symbol table lookup
- **WHEN** resolving an identifier
- **THEN** the symbol table returns a `SymbolId` handle; the handle indexes into `SymbolTable` parallel arrays.

#### Scenario: Invalid handle sentinel
- **WHEN** a pass needs to represent "no value" for an instruction/slot/symbol handle
- **THEN** it uses `-1` as the invalid sentinel; for `BlockId`, `0` additionally means "unassigned to any block".

### Requirement: Fixed-Operand Fast Path with Extra Pool
Each instruction MUST store up to `MAX_FIXED_OPS` operands inline in a contiguous `fixedOps` array; overflow operands go to a shared `extra` pool tracked by per-instruction offset/length arrays.

#### Scenario: Instruction with ≤4 operands (fast path)
- **WHEN** an instruction is created with ≤4 operands (e.g., `LOAD A, $80`)
- **THEN** all operands are written to `fixedOps[instIdx * MAX_FIXED_OPS + 0..3]`; `extraOffset[idx] = 0`, `extraLength[idx] = 0`.

#### Scenario: Instruction with >4 operands (slow path)
- **WHEN** an instruction has >4 operands (e.g., `CALL` with many args, `FOR_STATEMENT` with 5)
- **THEN** first `MAX_FIXED_OPS` operands go to `fixedOps`, remaining operands are appended to `extra` pool; `extraOffset[idx]` and `extraLength[idx]` record the slice.

### Requirement: SoA Field-Access API Replacing Metadata Bit-Packing
All passes MUST access instruction metadata via direct SoA field arrays; the `InstructionMetadata` encode/decode bit-packing API MUST be removed.

#### Scenario: Reading instruction kind
- **WHEN** a pass needs the instruction kind (e.g., `SlabPrinter` formatting output)
- **THEN** it reads `instList.Tags[instIdx]` directly (no bit shifts).

#### Scenario: Reading arg count
- **WHEN** a pass needs the argument count (e.g., operand iterator)
- **THEN** it reads `instList.ArgCounts[instIdx]` directly.

#### Scenario: Setting terminator flag
- **WHEN** a CFG pass marks a block terminator
- **THEN** it sets `instList.Flags[instIdx] |= InstFlags.Terminator`.

### Requirement: CFG BlockId Array in InstList
The `InstList` MUST include a parallel `BlockId[]` array mapping each instruction to its owning basic block (0 = unassigned).

#### Scenario: CFG construction pass
- **WHEN** the CFG pass identifies basic blocks
- **THEN** it writes `instList.BlockIds[instIdx] = blockId` for each instruction in the block.

#### Scenario: CFG table queries
- **WHEN** a pass needs a block's first instruction index
- **THEN** it queries `cfgTable.GetBlockOffset(blockId)` (CFG table remains separate, keyed by `BlockId`).

### Requirement: SIMD-Ready Stride Access
The `InstList` MUST provide stride-only iteration over homogeneous field arrays (`Tags[]` as `byte[]`, `Flags[]`/`ArgCounts[]` as `ushort[]`) to enable SIMD vectorization of passes.

#### Scenario: Kind-filtered pass
- **WHEN** a pass needs to find all instructions of a kind (e.g., all `MLIR_ASSIGN`)
- **THEN** it can load `Tags[]` into `Vector128<byte>` and match in bulk, then process matches by index.

### Requirement: InstListBuilder
An `InstListBuilder` MUST provide incremental construction (Append/Add methods, auto-resize of all parallel arrays and extra pool).

#### Scenario: Building an InstList in a transformer
- **WHEN** a transformer emits instructions in order
- **THEN** it calls `InstListBuilder.Add(tag, flags, operands)` which appends to parallel arrays, grows them as needed, and returns the new `InstIndex`.