---
title: "Dynamic Linking and Loading"
description: "Specification for dynamic module loading and relocation in GameVM"
author: "GameVM Team"
created: "2026-01-31"
updated: "2026-01-31"
version: "1.0.0"
---

# Dynamic Linking and Loading

## 1. Introduction

### 1.1 Purpose
While many retro gaming platforms use memory-mapped cartridges (IO-mapped into the CPU address space), later generations (4th-5th Gen) and disk-based systems require loading code and data from slow media into RAM. This document specifies the GameVM mechanism for dynamic loading, symbol relocation, and memory management for such platforms.

### 1.2 Scope
- **ELF (Executable and Linkable Format)**: The standard format for relocatable modules.
- **Dynamic Symbol Resolution**: Resolving cross-module references.
- **Overlay Management**: Swapping modules in shared RAM regions.
- **Async Loading**: Non-blocking I/O for CD-based systems.

### 1.3 Host/Target Roles
- **Host (PC)**: Generates a standard **ELF binary**. It maps LLIR to the target's specific emission format (STC calls, DTC/ITC address lists, or TTC tokens) and generates standard ELF relocation entries.
- **Target (Runtime)**: A minimal **ELF Loader** patches the addresses in RAM based on the relocation table and binds imports to the global symbol table.

## 2. Dynamic Loading Model

### 2.1 Static vs. Dynamic Linking
- **Static Linking**: Addresses are resolved at build time for the resident kernel.
- **Dynamic Linking**: Modules are emitted as relocatable ELF files.

### 2.2 The ELF Profile
GameVM uses a "Minimal ELF" profile for retro targets:
1. **ELF Header**: Standard identification and machine type.
2. **Program Headers**: Define loadable segments (Code/Data).
3. **Symbol Table (`.symtab`)**: For exported/imported LLIR entities.
4. **Relocation Table (`.rel`, `.rela`)**:
   - **STC**: Relocates native `CALL` targets.
   - **DTC/ITC**: Relocates the list of memory addresses.
   - **TTC**: Relocates the token-to-address jump table and data pointers.

## 3. The Runtime Loader

### 3.1 Loader Responsibilities
1. **Allocation**: Find a contiguous block of RAM suitable for the module.
2. **I/O**: Read the module from media (CD-ROM, Disk, etc.).
3. **Relocation**: Patch all internal pointers and branches to match the actual RAM address.
4. **Symbol Binding**: Resolve external dependencies against the Resident Module or other loaded modules.
5. **Initialization**: Execute module-level constructors/initializers.

### 3.2 Loader API (Standard Library)
```pascal
type
  TModuleHandle = Pointer;
  TLoadStatus = (lsSuccess, lsFileNotFound, lsOutOfMemory, lsRelocationError);

// Synchronous loading
function LoadModule(const ModuleName: string): TModuleHandle;

// Asynchronous loading
procedure LoadModuleAsync(const ModuleName: string; OnComplete: TLoadCallback);

// Unloading
procedure UnloadModule(Handle: TModuleHandle);

// Manual Symbol Retrieval
function GetModuleProc(Handle: TModuleHandle; const ProcName: string): Pointer;
```

## 4. Overlay Management

Overlays allow multiple modules to share the same memory address, swapping in and out as needed.

### 4.1 Overlay Regions
Developers can define "Overlay Regions" in their project configuration:
```json
{
  "memoryMap": {
    "regions": [
      { "name": "Resident", "start": "0x80000000", "size": "1MB" },
      { "name": "LevelOverlay", "start": "0x80100000", "size": "512KB", "type": "overlay" }
    ]
  }
}
```

### 4.2 Handling "Stale" Pointers
When an overlay is swapped, any pointers into the old module become invalid. The GameVM runtime can optionally track these if "Safe Mode" is enabled:
- **Reference Tracking**: Prevent unloading if active stack frames point into the module.
- **Stubbing**: Replace exported function pointers with a "Fault Handler" that throws an error or triggers a reload.

## 5. Media-Specific Optimizations

### 5.1 Slow Media Strategies
- **Batching**: Grouping modules into contiguous "Load Bundles" to minimize seek times on CD-ROMs.
- **Pre-fetching**: Loading the "next" expected module in the background while the game is running.
- **Double Buffering**: Using two memory regions to load a new level while the current one is still active.

## 6. Symbol Resolution Strategy

### 6.1 The Resident Module
The first module loaded is the "Resident Module" (or Kernel). It contains the Runtime Loader, basic HAL, and the Global Symbol Table. All subsequently loaded modules resolve their imports against this table.

### 6.2 Lazy Binding
(Optional for 5th Gen platforms)
Postpone symbol resolution until the first time a function is called. This speeds up initial loading but adds overhead to the first call.

## 7. Platform Specifics

### 7.1 Sony PlayStation (PS1)
- Uses DMA for high-speed streaming from CD-ROM to RAM.
- Relocation table is compressed to save space on disc.
- Supports "Object Overlays" for boss entities.

### 7.2 Nintendo 64 (N64)
- Primarily cartridge-based, but supports high-speed DMA from ROM to RDRAM.
- Dynamic loading is used to managed the limited 4MB/8MB RAM.
- Code often executes from the Command Cache or Instruction Cache after being DMAd.

## 8. Safety and Determinism

Dynamic loading introduces non-determinism (load times).
- **Static Analysis**: The build system should verify that the worst-case load time doesn't break frame-rate critical logic.
- **Fragmentation**: The loader uses a simple "Stack-based" or "Region-based" allocator to avoid heap fragmentation.

## 9. Dynamic Module Resolution
Dynamic loading is not limited to static path resolution. GameVM facilitates runtime determination of module names (e.g., Python's `__import__(name)`).

### 9.1 The Module Registry
A static "String-to-Pointer" map generated by the compiler. Primarily used for cartridge targets where all modules are built into the resident ROM.
- **Lookup**: O(log n) via binary search on sorted strings.
- **Result**: Returns the address of the module's Export Table (Vtable).

### 9.2 ELF Dispatch
For disk/slow-media targets, the dynamic `import` function acts as a wrapper for the ELF Loader:
1.  **Resolve**: Map the dynamic string to a file path (e.g., `modules/gfx_vga.elf`).
2.  **Load**: Invoke the async ELF loader.
3.  **Bind**: Return the module handle once relocation is complete.

## 10. Future Enhancements
- Incremental Hot-Reloading via ELF dynamic loading during development.
- Code Signing and Verification for dynamic modules.
- Compressed ELF segments for memory-constrained CD-based systems.
