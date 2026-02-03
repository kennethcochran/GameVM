# GameVM Package Management Design

## 1. Introduction

### 1.1 Purpose
This document outlines the design of GameVM's package management system, which handles the distribution, versioning, and dependency resolution of modules across different platforms and languages.

### 1.2 Key Features
- Semantic versioning
- Dependency resolution
- Multiple repository support
- Platform-specific packages
- Binary caching

## 2. Package Structure (The .gvpkg Model)

A GameVM package is a versioned collection of HLIR modules, optional source code, and hardware requirements. It is the primary unit of sharing for both the Standard Library and third-party extensions.

### 2.1 File Organization
```
GameVM.HAL.SpriteGfx-1.2.0.gvpkg/
├── package.yaml           # Package manifest and hardware requirements
├── lib/                   
│   └── SpriteGfx.hlir     # Pre-compiled language-agnostic metadata
├── src/                   # Optional: Source for debugging/rebuilding
│   └── SpriteGfx.cs       
└── docs/                  # API documentation
```

### 2.2 The Package Manifest (`package.yaml`)
The manifest defines the "Hardware Contract" for the entire library.

```yaml
name: GameVM.HAL.SpriteGfx
version: 1.2.0
description: "Hardware sprite management for 3rd-5th gen systems"

# Hardware Coin Slot: Prevents installation on incompatible systems
capabilities:
  required: ["CORE", "S"]  # Requires the Sprite extension
  recommends: ["M"]       # Uses Math extension for hardware acceleration if available

# Module exposure
exports:
  - name: HAL.Sprite
    path: lib/SpriteGfx.hlir

dependencies:
  GameVM.HAL.Core: "^1.0.0"
```

---

## 3. Official System Libraries

The Standard Library and HAL are decoupled from the compiler. They are distributed as official system packages hosted on the **GameVM Registry**.

### 3.1 Core Packages
| Package Name     | Included Capabilities | Purpose                                  |
| :--------------- | :-------------------- | :--------------------------------------- |
| `GameVM.Core`    | `CORE`                | Basic types, memory, and flow.           |
| `GameVM.Math`    | `M`, `F`              | Hardware-accelerated math/fixed-point.   |
| `GameVM.HAL.Gfx` | `T`, `S`, `A`         | Tile, Sprite, and Alpha-blending HAL.    |
| `GameVM.HAL.Snd` | `P`, `W`, `D`         | Pulse, Wavetable, and Digital audio HAL. |

---

## 4. HLIR-Based Distribution (The "Assembly" Model)

Following the .NET Assembly model, GameVM packages prioritize **HLIR Metadata**.

1.  **Language Portability**: A package written in C# is compiled to HLIR. A Pascal developer can consume this package because the Pascal compiler reads the HLIR signatures, not the original C# code.
2.  **Opaque Implementation**: Packages can ship without source code, providing only the HLIR headers and the target-specific bytecode/interpreters.
3.  **Capability Validation**: The compiler validates the `required` capabilities of every HLIR library during the **Dependency Resolution** phase.

---

## 5. Dependency & Conflict Resolution

### 5.1 The Resolution Pipeline
1.  **Flattening**: Resolve the dependency graph into a Strict DAG.
2.  **Capability Audit**: The compiler aggregates the required capabilities of all installed packages.
3.  **Hardware Match**: The compiler compares the aggregate requirements against the `gamevm.yaml` target hardware.
    -   *Example*: If `GameVM.HAL.Gfx` (requires `S`) is installed but the target is `Atari 2600` (no `S`), the build fails immediately.

## 7. Security

### 7.1 Package Signing
- GPG signatures
- Code signing certificates
- Package hashes

### 7.2 Vulnerability Scanning
- CVE database integration
- Dependency auditing
- License compliance

## 8. Performance

### 8.1 Caching
- Local package cache
- CDN support
- Delta updates

### 8.2 Parallel Downloads
- Concurrent package fetching
- Connection pooling
- Resumable downloads

## 9. Tools

### 9.1 Command Line
```bash
# Install dependencies
gamevm install

# Add a dependency
gamevm add package@version

# Update dependencies
gamevm update

# Publish a package
gamevm publish

# Audit for vulnerabilities
gamevm audit

# List dependencies
gamevm list
```

### 9.2 Editor Integration
- Package discovery
- Version management
- Dependency visualization

## 10. Migration

### 10.1 From Other Package Managers
- Conversion tools for:
  - NuGet (.NET)
  - Maven (Java)
  - npm (JavaScript)
  - vcpkg (C++)

### 10.2 Version Migration
- Automatic updates
- Breaking change detection
- Deprecation warnings
