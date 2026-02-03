# GameVM Documentation

GameVM is a high-performance **cross-compiler toolchain** for retro gaming development. It enables developers to write games in modern, high-level languages and compile them into highly optimized binaries for systems ranging from the 2nd to the 5th generation of gaming consoles.

## 📚 Documentation Pillars

### 1. [Compiler Design](compiler/)
Technical specifications for the toolchain, intermediate representations, and code generation.
- [Architecture Overview](architecture/ArchitectureOverview.md)
- [LLIR ISA Specification](compiler/LLIR_ISA.md)
- [Module Resolution](compiler/ModuleResolution.md)
- [Capability Enforcement](compiler/CapabilityImplementationBlueprint.md)

### 2. [Developer API](api/)
Reference documentation for the Standard Library and Hardware Abstraction Layer.
- [Standard Library](api/stdlib/StandardLibrary.md)
- [Hardware Abstraction Layer (HAL)](api/hal/HAL.md)
- [Common Language Features](api/common_language_features.md)

### 3. [Platforms & Capabilities](platforms/)
Hardware specifications and the Capability Profile system.
- [Capability Profiles](platforms/CapabilityProfiles.md)
- [System Reference Specs](platforms/specs/) (NES, PS1, Atari, etc.)

### 4. [Architecture & Strategy](architecture/)
High-level project goals, testing strategies, and versioning.
- [Architecture Overview](architecture/ArchitectureOverview.md)
- [Testing Strategy](architecture/TestingStrategy.md)
- [Versioning Strategy](architecture/VersioningStrategy.md)

---

## Contributing

To contribute to the documentation, please follow the [Documentation Standards](architecture/DocumentationStandards.md).
