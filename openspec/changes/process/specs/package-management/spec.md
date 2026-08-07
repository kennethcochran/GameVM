# Package Management Specification

## Purpose

Defines the design of GameVM's package management system, which handles the distribution, versioning, and dependency resolution of modules across different platforms and languages.
Aspirational — not yet implemented.

## Requirements

### Requirement: Package Structure
A GameVM package MUST be a versioned collection of HLIR modules, optional source code, and hardware requirements.

#### Scenario: Package definition
- **WHEN** a distribution unit is created for the Standard Library or a third-party extension
- **THEN** it MUST be packaged as a versioned `.gvpkg` collection of HLIR modules, optional source code, and hardware requirements

#### Scenario: File organization
- **WHEN** a package is laid out on disk
- **THEN** it MUST contain a `package.yaml` manifest with hardware requirements and exports, a `lib/` directory with pre-compiled HLIR metadata, an optional `src/` directory with source for debugging and rebuilding, and a `docs/` directory with API documentation

### Requirement: Package Manifest
Each package MUST include a `package.yaml` manifest that defines the hardware contract and module exposure.

#### Scenario: Manifest contents
- **WHEN** a package manifest is authored
- **THEN** it MUST declare the package name and version, a description, required and recommended capabilities, exported modules with their HLIR paths, and declared dependencies with version ranges

#### Scenario: Hardware coin slot
- **WHEN** installation is attempted on a system
- **THEN** the manifest's `required` capabilities MUST prevent installation on incompatible systems, while `recommends` MAY be used to exploit optional extensions when available

### Requirement: Official System Libraries
The Standard Library and HAL MUST be decoupled from the compiler and distributed as official system packages hosted on the GameVM Registry.

#### Scenario: Core package set
- **WHEN** the official registry hosts system libraries
- **THEN** it MUST provide `GameVM.Core` (capabilities `CORE`), `GameVM.Math` (capabilities `M`, `F`), `GameVM.HAL.Gfx` (capabilities `T`, `S`, `A`), and `GameVM.HAL.Snd` (capabilities `P`, `W`, `D`) as system packages

### Requirement: HLIR-Based Distribution
Packages MUST distribute HLIR metadata to maximize portability and allow opaque implementation.

#### Scenario: Language portability
- **WHEN** a package written in one language is consumed by a frontend of another language
- **THEN** the consuming frontend MUST read the HLIR signatures rather than the original source code, enabling cross-language consumption

#### Scenario: Opaque implementation
- **WHEN** a package ships without source code
- **THEN** it MUST ship only the HLIR headers and the target-specific bytecode or interpreters, and it MUST be consumable without source

#### Scenario: Capability validation
- **WHEN** an HLIR library is resolved as a dependency
- **THEN** the compiler MUST validate the library's `required` capabilities during the Dependency Resolution phase

### Requirement: Dependency and Conflict Resolution
Dependencies MUST be resolved through a deterministic pipeline.

#### Scenario: Resolution pipeline
- **WHEN** package dependencies are resolved
- **THEN** the dependency graph MUST be flattened into a Strict DAG, all installed packages' required capabilities MUST be aggregated, and the aggregate requirements MUST be compared against the target hardware declared in `gamevm.yaml`

#### Scenario: Hardware mismatch
- **WHEN** an installed package requires a capability the target hardware lacks, such as `GameVM.HAL.Gfx` requiring `S` on an Atari 2600 with no `S`
- **THEN** the build MUST fail immediately

### Requirement: Package Security
Packages MUST be authenticated and scanned for vulnerabilities.

#### Scenario: Package signing
- **WHEN** a package is distributed
- **THEN** it MUST be covered by GPG signatures, code signing certificates, and package hashes

#### Scenario: Vulnerability scanning
- **WHEN** dependencies are managed
- **THEN** CVE database integration, dependency auditing, and license compliance MUST be applied

### Requirement: Package Performance
Package distribution MUST support caching and concurrent downloads.

#### Scenario: Caching
- **WHEN** packages are fetched
- **THEN** a local package cache, CDN support, and delta updates MUST be provided

#### Scenario: Parallel downloads
- **WHEN** multiple packages are fetched
- **THEN** concurrent package fetching, connection pooling, and resumable downloads MUST be supported

### Requirement: Package Tools
The project MUST provide a command-line interface and editor integration for package management.

#### Scenario: Command-line operations
- **WHEN** a user manages packages
- **THEN** commands MUST install dependencies (`gamevm install`), add a dependency (`gamevm add package@version`), update dependencies (`gamevm update`), publish a package (`gamevm publish`), audit vulnerabilities (`gamevm audit`), and list dependencies (`gamevm list`)

#### Scenario: Editor integration
- **WHEN** packages are managed within an editor
- **THEN** package discovery, version management, and dependency visualization MUST be supported

### Requirement: Migration
The system MUST support migration from other package managers.

#### Scenario: Conversion from foreign managers
- **WHEN** an existing project is migrated
- **THEN** conversion tools MUST be provided for NuGet (.NET), Maven (Java), npm (JavaScript), and vcpkg (C++)

#### Scenario: Version migration
- **WHEN** dependency versions are migrated
- **THEN** automatic updates, breaking change detection, and deprecation warnings MUST be applied