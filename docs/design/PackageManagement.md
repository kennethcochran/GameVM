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

## 2. Package Structure

### 2.1 Package Layout
A package is a distributable unit that contains one or more modules:

```
Game.Physics-2.3.1/
├── modules/               # One directory per module
│   ├── Game.Physics/      # Core physics module
│   │   ├── Game.Physics.pas
│   │   ├── Collision.pas
│   │   └── Dynamics.pas
│   └── Game.Physics2D/    # Optional 2D physics module
│       └── Game.Physics2D.pas
├── include/               # Public headers (for C/C++)
│   └── Game.Physics.h
├── lib/                   # Precompiled binaries
│   ├── win32/
│   ├── linux/
│   └── wasm/
├── docs/                  # Documentation
└── package.json           # Package metadata
```

### 2.2 Package Manifest (package.json)
```json
{
  "name": "Game.Physics",
  "version": "2.3.1",
  "displayName": "Game Physics Engine",
  "description": "Advanced physics simulation for games",
  
  "modules": [
    {
      "name": "Game.Physics",
      "path": "modules/Game.Physics",
      "type": "library"
    },
    {
      "name": "Game.Physics2D",
      "path": "modules/Game.Physics2D",
      "type": "library"
    }
  ],
  
  "dependencies": {
    "Game.Math": "^2.0.0",
    "System.Memory": "~1.2.0"
  },
  
  "repository": {
    "type": "git",
    "url": "https://github.com/gamevm/physics.git"
  },
  
  "license": "MIT",
  "authors": ["GameVM Team <team@gamevm.io>"],
  "keywords": ["physics", "simulation", "game"]
}
```

## 3. Dependency Resolution

### 3.1 Version Constraints
- `^1.2.3`: Compatible with 1.2.3, excluding 2.0.0
- `~1.2.3`: 1.2.x, excluding 1.3.0
- `>=1.2.3 <2.0.0`: Version range
- `1.2.x` or `1.2.*`: Patch updates only
- `*`: Any version (not recommended)

### 3.2 Resolution Strategy
1. **Flattening**
   - Create a flat list of all dependencies
   - Resolve version conflicts
   - Prefer highest compatible version

2. **Lock File**
   - Generate a lock file with exact versions
   - Ensure reproducible builds
   - Track dependency trees

## 4. Repository System

### 4.1 Repository Types
1. **Local**
   - File system paths
   - Development packages

2. **Git**
   - Version control repositories
   - Branch/tag/commit references

3. **HTTP/HTTPS**
   - Remote package servers
   - Private registries

4. **GameVM Registry**
   - Official package registry
   - Community packages
   - Verified publishers

### 4.2 Authentication
- API keys
- OAuth2
- SSH keys for Git
- Environment variables

## 5. Package Lifecycle

### 5.1 Publishing
1. Version bump
2. Build artifacts
3. Generate documentation
4. Create package archive
5. Publish to registry

### 5.2 Installation
1. Resolve dependencies
2. Download packages
3. Verify integrity
4. Extract to cache
5. Link to project

## 6. Package Structure

### 6.1 File Organization
```
package-name-version.zip
├── pkg/                    # Package root
│   ├── com/
│   │   └── example/
│   │       └── mypackage/  # Reverse domain name structure
│   │           ├── module1.hlir
│   │           └── module2.hlir
├── src/                    # Optional source code
│   └── com/example/mypackage/
│       ├── Module1.pas
│       └── Module2.cs
├── docs/                   # Optional documentation
│   ├── index.html
│   └── api/
└── package.json           # Package manifest
```

### 6.2 Package Manifest
```json
{
  "name": "com.example.mypackage",
  "version": "1.0.0",
  "displayName": "My Game Package",
  "description": "A package that does something useful",
  "unity": "2021.3",  // Optional: If Unity-specific
  "targets": [
    "generic",  // Runs on any target
    "snes",     // Super Nintendo specific
    "genesis",  // Sega Genesis specific
    "gba"       // Game Boy Advance specific
  ],
  "modules": [
    {
      "name": "com.example.mypackage.module1",
      "path": "com/example/mypackage/module1.hlir",
      "targets": ["generic", "snes"]
    },
    {
      "name": "com.example.mypackage.module2",
      "path": "com/example/mypackage/module2.hlir",
      "targets": ["genesis", "gba"]
    }
  ],
  "dependencies": {
    "com.other.package": "^2.0.0"
  },
  "repository": {
    "type": "git",
    "url": "https://github.com/example/mypackage.git"
  }
}
```

### 6.3 Repository Support
- **Local Repositories**:
  - Directory containing .zip packages
  - Useful for development and testing

- **Remote Repositories**:
  - HTTP/HTTPS servers serving .zip packages
  - Support for authentication
  - Package signing verification

### 6.4 Console-Specific Modules
Packages can include modules that target specific consoles while providing a common interface:

```json
{
  "name": "com.example.graphics",
  "version": "1.0.0",
  "modules": [
    {
      "name": "com.example.graphics.api",
      "path": "com/example/graphics/api.hlir",
      "targets": ["generic"]
    },
    {
      "name": "com.example.graphics.impl.snes",
      "path": "com/example/graphics/impl/snes.hlir",
      "targets": ["snes"],
      "provides": ["com.example.graphics.api"]
    },
    {
      "name": "com.example.graphics.impl.genesis",
      "path": "com/example/graphics/impl/genesis.hlir",
      "targets": ["genesis"],
      "provides": ["com.example.graphics.api"]
    }
  ]
}
```
      }
    }
  }
}
```

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
