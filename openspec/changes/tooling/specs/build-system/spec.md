# Build System Specification

## Purpose
Defines the design of the GameVM build system, which orchestrates the compilation and linking of modules across multiple languages for retro gaming console targets (2nd-5th generation). It integrates with the Module System and Type System, providing incremental compilation, parallel execution, cross-language dependency resolution, and distributed build caching. Aspirational — not yet implemented.

## Requirements

### Requirement: Incremental Compilation with Dependency Tracking
The build system MUST support incremental compilation with precise dependency tracking so that only changed modules and their dependents are rebuilt.

#### Scenario: Rebuilding a single changed module
- **WHEN** a source file in one module is modified and the user invokes a build
- **THEN** the build system recompiles only the changed module and its transitive dependents, reusing cached artifacts for unchanged modules

#### Scenario: Detecting stale dependents
- **WHEN** a module's public interface changes and another module depends on it
- **THEN** the dependent module MUST be rebuilt along with the changed module

### Requirement: Parallel Build Execution
The build system MUST support parallel build execution across all available cores, including file-level parallelism, module-level parallelism, and distributed build clusters.

#### Scenario: Parallel module compilation
- **WHEN** a project declares multiple independent modules
- **THEN** the build system schedules and compiles the independent modules in parallel across available cores

#### Scenario: DAG-ordered scheduling
- **WHEN** modules have declared dependencies on other modules
- **THEN** the scheduler MUST order module compilation so that each module builds only after its dependencies complete

### Requirement: Cross-Language Dependency Resolution
The build system MUST resolve dependencies across modules written in different languages, performing type mapping, ABI compatibility checks, and interface definition sharing.

#### Scenario: Cross-language module reference
- **WHEN** a module written in one language depends on a module written in another language
- **THEN** the build system maps types between the languages, verifies ABI compatibility, and shares interface definitions

### Requirement: Cross-Console Compilation Support
The build system MUST support compiling for multiple console targets spanning generations 2 through 5, using console-specific toolchains, optimizations, memory layout, and hardware register access.

#### Scenario: Multi-target project
- **WHEN** a project declares multiple console targets (e.g., NES, SNES, Genesis)
- **THEN** the build system generates a ROM for each declared target using generation-appropriate toolchains and hardware configuration

#### Scenario: Target-specific configuration overrides
- **WHEN** a target declares target-specific settings such as mapper, ROM size, or memory map
- **THEN** the build system applies those settings to that target's ROM generation

### Requirement: Distributed Build Caching
The build system MUST support content-addressable local and remote build caches to reuse previously built artifacts.

#### Scenario: Local cache reuse
- **WHEN** a module's inputs are unchanged as determined by their content hash
- **THEN** the build system reuses the cached output instead of recompiling, up to the configured cache size

#### Scenario: Remote cache push and pull
- **WHEN** a remote cache is configured with credentials
- **THEN** the build system pushes artifacts to the remote cache in CI environments and pulls cached artifacts when a match exists

### Requirement: Build Configuration via Project File
The build system MUST read project configuration from a `gamevm.yaml` file defining project metadata, targets, modules, toolchains, optimization levels, feature flags, and target-specific overrides.

#### Scenario: Default target build
- **WHEN** a project declares a default target and multiple build targets
- **THEN** a bare build produces the default target ROM unless a target is specified

#### Scenario: Target-specific overrides
- **WHEN** a target declares target-specific settings such as mapper, ROM size, or feature flags
- **THEN** the build system applies those settings to that target's ROM generation

### Requirement: Build Profiles
The build system MUST support named build profiles (Debug, Dev, Release, Perf, Cartridge) that control optimization level, debug info generation, and assertion behavior.

#### Scenario: Release ROM build
- **WHEN** a Release profile build is requested
- **THEN** the build produces a size-optimized ROM with no debug info and assertions disabled

#### Scenario: Debug development build
- **WHEN** a Debug profile build is requested
- **THEN** the build enables assertions and full debug information for development

### Requirement: Module Configuration
The build system MUST accept per-module configuration including language, target, sources, includes, defines, dependencies, and resource assets.

#### Scenario: Static library module
- **WHEN** a module declares a language, source globs, and a target
- **THEN** the build system compiles those sources with the appropriate toolchain for the target

#### Scenario: Resource conversion
- **WHEN** a module declares resources such as images or audio samples
- **THEN** the build system converts them to the console-native format (e.g., CHR, VRAM, DPCM)

### Requirement: ROM Building Process
The build system MUST produce a final ROM through coordinated phases including initialization, asset processing, compilation, linking, and ROM generation.

#### Scenario: Assets converted to console-native format
- **WHEN** graphics and audio assets are processed for a target
- **THEN** they are converted to console-native formats (graphics to CHR/VRAM, audio compressed as DPCM/VGM)

#### Scenario: ROM header generation
- **WHEN** object files are linked for a target
- **THEN** the linker applies memory map constraints and generates a console-appropriate ROM header (iNES, SNES, Genesis)

#### Scenario: Checksum and patch generation
- **WHEN** the final ROM is generated
- **THEN** the build adds checksums and can generate `.ips` patches for distribution

### Requirement: ROM Optimization
The build system MUST optimize generated ROMs for size and performance.

#### Scenario: Size optimization
- **WHEN** a size-optimized build is requested
- **THEN** the build performs dead code elimination, unused asset removal, bank packing, and text/data compression

#### Scenario: Performance tuning
- **WHEN** a performance-critical build is produced
- **THEN** the build performs critical path optimization, VBlank/NMI-safe code analysis, memory access pattern optimization, and ROM banking strategy

### Requirement: Region Variants
The build MUST support region-specific variants (NTSC, PAL, JP) that define preprocessor defines, timing, and text encoding.

#### Scenario: PAL region build
- **WHEN** a PAL variant is selected
- **THEN** the build defines `PAL` and uses 50.0 Hz timing

#### Scenario: JP region build
- **WHEN** a JP variant is selected
- **THEN** the build defines `NTSC` and `JAPAN`, uses 60.0 Hz timing, and applies shift-jis text encoding

### Requirement: Custom Build Tools
The build system MUST support invoking custom build tools on module inputs to produce transformed outputs.

#### Scenario: Custom conversion tool
- **WHEN** a module declares a tool with an input glob and command template
- **THEN** the build runs the tool for each matching input to produce the declared outputs

### Requirement: Module Build Hooks
The build system MUST support pre/post build steps and custom build rules per module.

#### Scenario: Pre- and post-build steps
- **WHEN** a module declares pre_build and post_build commands
- **THEN** the build executes them before and after the module's normal build

#### Scenario: Custom build rule
- **WHEN** a module declares a custom build rule matching an input pattern
- **THEN** the rule's command is invoked for each matching input to produce the declared output

### Requirement: Module System Integration
The build system MUST integrate with the module resolution system at build time to parse module manifests, build a dependency graph, and detect version conflicts, enforcing strict DAG ordering.

#### Scenario: Build-time dependency resolution
- **WHEN** the build system collects dependencies from module manifests
- **THEN** it constructs a dependency graph, detects version conflicts, and enforces strict DAG ordering before compiling

### Requirement: Build Sandboxing and Security
The build system MUST sandbox build execution with isolated environments, restricted filesystem access, and network access controls.

#### Scenario: Isolated build environment
- **WHEN** a build is executed
- **THEN** it runs in an isolated environment with restricted filesystem and network access

### Requirement: Reproducible Builds
The build system MUST produce deterministic and reproducible output via build environment isolation and pinned dependencies.

#### Scenario: Deterministic output
- **WHEN** the same inputs are used with pinned dependencies in an isolated environment
- **THEN** the build generates identical output bytes

### Requirement: IDE Integration
The build system MUST generate project files and support development tools for common IDEs.

#### Scenario: Project generation
- **WHEN** project generation is requested
- **THEN** the build system emits project files for Visual Studio, Xcode, VSCode, and CLion

### Requirement: Continuous Integration
The build pipeline MUST integrate with CI workflows across multiple platforms, including SonarQube static analysis with quality gate enforcement.

#### Scenario: Cross-platform CI matrix
- **WHEN** a commit or pull request is pushed
- **THEN** the CI workflow builds the project on the configured OS matrix and uploads the build artifacts

#### Scenario: Quality gate enforcement
- **WHEN** the CI SonarQube job runs static analysis
- **THEN** the build fails when the SonarQube Quality Gate conditions are not met

### Requirement: Build Database and Debugging
The build system MUST expose verbose logging, dependency graph export, build step tracing, and cache diagnostics for troubleshooting.

#### Scenario: Dependency graph export
- **WHEN** a user requests build troubleshooting data
- **THEN** the build system exports the dependency graph and per-step trace information