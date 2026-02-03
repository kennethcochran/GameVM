# Versioning Strategy: Toolchain & Projects

## 1. Toolchain Versioning
The GameVM compiler, CLI, and standard library follow **Semantic Versioning 2.0.0** (vMajor.Minor.Patch).
- **Major**: Breaking IR changes or backend removals.
- **Minor**: New targets, new language frontends, or library additions.
- **Patch**: Bug fixes and emitter optimizations.

## 2. Project & ROM Versioning
Game projects have specific versioning requirements due to hardware burnt into ROMs and persistent save data.

### 2.1 ROM Metadata
The compiler supports embedding version metadata into target-specific headers (e.g., the 16-character SNES title field or the Sega Genesis version byte).

```yaml
# gamevm.yaml
metadata:
  version: 1.0.4
  build_id: "20260131"
  region: NTSC-U
```

### 2.2 Save Game Compatibility
When the project's data structures (Structs/Records) change, save files may become incompatible.
- **Static Layout**: The GameVM compiler preserves field offsets by default.
- **Migration**: For major updates, developers should include a "Save Sync" routine that reads the embedded version ID from the save RAM and migrates the data mapping.

## 3. ELF & Module Versioning
For systems using the [Dynamic Loading](../compiler/DynamicLoading.md) (ELF) system, versioning ensures that levels or plugins match the core executable.

- **Interface IDs**: Every dynamic module includes a signature of its export Vtable.
- **Matching**: The ELF Loader verifies that the `BuildID` of a module matches the `BuildID` of the resident kernel before allowing a relocation/link.

## 4. LLIR Versioning
The LLIR ISA itself is versioned. Binaries emitted for LLIR v1.2 will include a header requirement.
- **Backward Compatibility**: Newer emitted interpreters (VMs) will support older LLIR bytecode patterns via "Compatibility Opcode Support" if enabled in the compiler configuration.

## 5. Deployment Tags
- **Proto**: Rapid iteration, debug headers enabled.
- **Release**: Full LTO (Link Time Optimization), no debug symbols, retail headers.
- **Revision**: PATCH updates for a released ROM (primarily for bug-fix releases of digital-only or flash-cart distributions).
