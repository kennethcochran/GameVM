---
title: "GameVM Fluent CLI Design"
description: "Configuration-driven, fluent command-line interface design for GameVM platform"
author: "GameVM Team"
created: "2025-01-31"
updated: "2025-01-31"
---

# GameVM Fluent CLI Design

## Overview

This document outlines the design of a configuration-driven, fluent command-line interface for the GameVM platform. The design follows industry best practices from .NET Core, Cargo, and npm, providing simple verb-only commands backed by rich configuration files and extensible tooling architecture.

## Design Philosophy

### **Core Principles**
1. **Simple by Default**: `GameVM build`, `GameVM test`, `GameVM run`
2. **Configuration-Driven**: Sensible defaults via `gamevm.json`
3. **Progressive Disclosure**: Simple → Override → Full Fluent
4. **Extensible Architecture**: Plugin system for third-party tools
5. **Unified Fluent Interface**: All tools use sentence-like syntax

### **Industry Inspiration**
```bash
# .NET Core
dotnet build          # Uses project.json/csproj defaults
dotnet test           # Uses test project defaults

# Cargo  
cargo build          # Uses Cargo.toml defaults
cargo test           # Uses Cargo.toml test config

# npm
npm build            # Uses package.json scripts
npm test             # Uses package.json test script
```

## Configuration System

### **Project Configuration (gamevm.json)**

#### **Complete Configuration Example**
```json
{
  "project": {
    "name": "MyRetroGame",
    "version": "1.0.0",
    "description": "A retro game for Atari 2600",
    "main": "src/Game.pas",
    "language": "Pascal",
    "author": "Game Developer",
    "license": "MIT"
  },
  "build": {
    "target": "Atari2600",
    "dispatch": "DTC",
    "output": "build/game.rom",
    "optimize": true,
    "debug": false,
    "superinstructions": "auto",
    "optimizationLevel": "Balanced",
    "generateDebugInfo": false,
    "warningsAsErrors": false,
    "additionalOptions": []
  },
  "test": {
    "target": "Atari2600",
    "harness": "MAME",
    "testFiles": [
      "tests/Game.feature",
      "tests/Graphics.feature",
      "tests/Input.feature"
    ],
    "timeout": 30,
    "verbose": false,
    "generateReport": true,
    "reportFormat": "junit",
    "coverage": false,
    "parallel": false
  },
  "run": {
    "target": "Atari2600",
    "mode": "normal",
    "debug": false,
    "breakpoints": [],
    "watch": false,
    "autoReload": false,
    "environment": {}
  },
  "analyze": {
    "target": "Atari2600",
    "reportType": "performance",
    "output": "reports/analysis.json",
    "includeCycles": true,
    "includeMemory": true,
    "includeSize": true,
    "baseline": null
  },
  "tools": {
    "mame": {
      "path": "/usr/local/bin/mame",
      "config": "mame.ini",
      "romPath": "roms/",
      "samplesPath": "samples/"
    },
    "emulator": {
      "preferred": "Stella",
      "alternatives": ["Stella", "Z26", "a2600"]
    }
  },
  "scripts": {
    "prebuild": "echo 'Starting build...'",
    "postbuild": "echo 'Build complete!'",
    "pretest": "echo 'Running tests...'",
    "posttest": "GameVM analyze build/game.rom for performance"
  }
}
```

### **Global Configuration (~/.gamevm/config.json)**
```json
{
  "defaults": {
    "target": "Atari2600",
    "dispatch": "DTC",
    "harness": "MAME",
    "language": "Pascal"
  },
  "paths": {
    "mame": "/usr/local/bin/mame",
    "templates": "~/.gamevm/templates",
    "cache": "~/.gamevm/cache",
    "logs": "~/.gamevm/logs",
    "tools": "~/.gamevm/tools"
  },
  "editor": {
    "name": "code",
    "args": ["--goto", "{file}:{line}"]
  },
  "ui": {
    "theme": "auto",
    "verbosity": "normal",
    "progress": "interactive"
  },
  "plugins": {
    "autoUpdate": true,
    "sources": ["https://api.gamevm.org/plugins"],
    "installed": [
      {
        "name": "GameVM.MAME",
        "version": "1.0.0",
        "enabled": true
      }
    ]
  }
}
```

### **Configuration Discovery Priority**
1. **Command-line overrides** (if specified)
2. **Local project config** (`./gamevm.json`)
3. **Parent project config** (`../gamevm.json`, `../../gamevm.json`)
4. **User global config** (`~/.gamevm/config.json`)
5. **System defaults** (built-in)

## Core Fluent CLI Interface

### **Primary Commands (Verb-Only)**

#### **GameVM build**
```bash
# Simple usage
GameVM build

# What happens internally:
# 1. Find gamevm.json in current directory
# 2. Load build configuration
# 3. Execute: GameVM.Compile Game.pas for Atari2600 as DTC --optimize
```

#### **GameVM test**
```bash
# Simple usage
GameVM test

# What happens internally:
# 1. Find gamevm.json in current directory
# 2. Load test configuration
# 3. Execute: GameVM.Test Game.rom on Atari2600 with MAME
```

#### **GameVM run**
```bash
# Simple usage
GameVM run

# What happens internally:
# 1. Find gamevm.json in current directory
# 2. Load run configuration
# 3. Execute: GameVM.Run Game.rom on Atari2600 in normal mode
```

### **Progressive Disclosure Interface**

#### **Level 1: Verb-Only (Simplest)**
```bash
GameVM build
GameVM test
GameVM run
GameVM analyze
```

#### **Level 2: Override Specific Values**
```bash
GameVM build --target Genesis
GameVM test --verbose
GameVM run --debug
GameVM analyze --reportType memory
```

#### **Level 3: Full Fluent (Advanced)**
```bash
GameVM build Game.pas for Genesis as Native Code --optimize --debug
GameVM test Game.rom on Genesis with custom --verbose
GameVM run Game.rom on Genesis in debug mode with breakpoints
```

## Individual Tool Fluent Interfaces

### **GameVM.Compile - Direct Compilation Tool**

#### **Fluent Interface**
```bash
# Basic compilation
GameVM.Compile Game.pas for Atari2600 as DTC
GameVM.Compile Engine.c for Genesis as Native Code
GameVM.Compile Program.py for N64 as ITC

# With options
GameVM.Compile Game.pas for Atari2600 as DTC --optimize --debug
GameVM.Compile Engine.c for Genesis as Native Code --output game.bin
GameVM.Compile Program.py for N64 as ITC --super aggressive
```

### **GameVM.Test - Testing Tool**

#### **Fluent Interface**
```bash
# Basic testing
GameVM.Test Game.rom on Atari2600 with MAME
GameVM.Test Suite.feature on Genesis with custom harness
GameVM.Test Program.rom on N64 with coverage

# With options
GameVM.Test Game.rom on Atari2600 with MAME --verbose
GameVM.Test Suite.feature on Genesis with custom --timeout 60
GameVM.Test Program.rom on N64 with coverage --report junit
```

### **GameVM.Run - Runtime Tool**

#### **Fluent Interface**
```bash
# Basic execution
GameVM.Run Game.rom on Atari2600 in normal mode
GameVM.Run Game.bin on Genesis in debug mode
GameVM.Run Program.rom on N64 with profiling

# With options
GameVM.Run Game.rom on Atari2600 in normal mode --watch
GameVM.Run Game.bin on Genesis in debug mode --breakpoints main
GameVM.Run Program.rom on N64 with profiling --output perf.json
```

### **GameVM.Analyze - Analysis Tool**

#### **Fluent Interface**
```bash
# Basic analysis
GameVM.Analyze Game.rom for performance metrics
GameVM.Analyze Game.rom for memory usage with detailed report
GameVM.Analyze Game.rom for cycle counts on Atari2600

# With options
GameVM.Analyze Game.rom for performance with detailed report
GameVM.Analyze Game.rom for memory usage --baseline baseline.json
GameVM.Analyze Game.rom for cycles --target-specific
```

## Configuration Management

### **Configuration Commands**

#### **GameVM config**
```bash
# Show all configuration
GameVM config show

# Show specific section
GameVM config show build
GameVM config show test
GameVM config show run

# Set configuration values
GameVM config set build.target Genesis
GameVM config set build.dispatch DTC
GameVM config set test.harness MAME
GameVM config set run.mode debug

# Reset to defaults
GameVM config reset build
GameVM config reset all
```

### **GameVM new - Project Creation**

#### **Project Templates**
```bash
# Create new project
GameVM new project MyRetroGame

# Create with specific template
GameVM new project MyRetroGame --template atari2600
GameVM new project MyRetroGame --template genesis
GameVM new project MyRetroGame --template multi-target
```

## Extensibility and Plugin System

### **Current State: GameVM.DevTools**

#### **Existing Implementation**
```csharp
// Current hardcoded MAME installation
public class MameInstaller
{
    public async Task InstallAsync()
    {
        // Hardcoded MAME download and installation
        var mameUrl = "https://github.com/mamedev/mame/releases/latest";
        // Installation logic...
    }
}
```

### **Future Plugin Architecture (Aspirational)**

#### **Plugin System Design**
```csharp
// Plugin interface
public interface IGameVMPlugin
{
    string Name { get; }
    string Version { get; }
    string Description { get; }
    Task<PluginInfo> GetInfoAsync();
    Task InstallAsync();
    Task UninstallAsync();
    Task<bool> IsInstalledAsync();
    Task<IEnumerable<Command>> GetCommandsAsync();
}

// Plugin manager
public class PluginManager
{
    public async Task<IEnumerable<IGameVMPlugin>> ListPluginsAsync();
    public async Task<IGameVMPlugin> GetPluginAsync(string name);
    public async Task InstallPluginAsync(string name, string version = null);
    public async Task UninstallPluginAsync(string name);
    public async Task UpdatePluginAsync(string name);
    public async Task LoadPluginAsync(string name);
}
```

#### **Plugin Repository System**
```json
// Plugin registry (https://api.gamevm.org/plugins)
{
  "plugins": [
    {
      "name": "GameVM.MAME",
      "version": "1.2.0",
      "description": "MAME integration for testing and analysis",
      "author": "GameVM Team",
      "category": "Testing",
      "platforms": ["linux", "windows", "macos"],
      "dependencies": [],
      "downloadUrl": "https://releases.gamevm.org/plugins/GameVM.MAME.1.2.0.zip",
      "checksum": "sha256:abc123...",
      "commands": ["mame-install", "mame-test", "mame-analyze"]
    },
    {
      "name": "GameVM.Stella",
      "version": "1.0.0", 
      "description": "Stella Atari 2600 emulator integration",
      "author": "GameVM Community",
      "category": "Runtime",
      "platforms": ["linux", "windows", "macos"],
      "dependencies": [],
      "downloadUrl": "https://releases.gamevm.org/plugins/GameVM.Stella.1.0.0.zip",
      "checksum": "sha256:def456...",
      "commands": ["stella-run", "stella-debug"]
    }
  ]
}
```

#### **Plugin Commands**
```bash
# Plugin management
GameVM plugin list
GameVM plugin search testing
GameVM plugin info GameVM.MAME
GameVM plugin install GameVM.MAME
GameVM plugin install GameVM.MAME --version 1.2.0
GameVM plugin update GameVM.MAME
GameVM plugin uninstall GameVM.MAME

# Plugin sources
GameVM plugin source add https://custom-plugins.example.com
GameVM plugin source list
GameVM plugin source remove https://custom-plugins.example.com

# Plugin development
GameVM plugin create MyPlugin --template basic
GameVM plugin build MyPlugin
GameVM plugin publish MyPlugin
GameVM plugin test MyPlugin
```

### **Migration Path from GameVM.DevTools**

#### **Phase 1: Refactor Existing Code**
```csharp
// Extract MAME installation into plugin interface
public class MamePlugin : IGameVMPlugin
{
    // Migrate existing GameVM.DevTools.MameInstaller logic
    public async Task InstallAsync()
    {
        // Use existing installation code
        var installer = new MameInstaller();
        await installer.InstallAsync();
    }
}
```

#### **Phase 2: Create Plugin Manager**
```csharp
// Basic plugin manager
public class PluginManager
{
    private readonly List<IGameVMPlugin> _plugins = new();
    
    public async Task LoadPluginAsync(string pluginPath)
    {
        // Load plugin assembly
        var assembly = Assembly.LoadFrom(pluginPath);
        var pluginTypes = assembly.GetTypes()
            .Where(t => typeof(IGameVMPlugin).IsAssignableFrom(t));
        
        foreach (var pluginType in pluginTypes)
        {
            var plugin = (IGameVMPlugin)Activator.CreateInstance(pluginType);
            _plugins.Add(plugin);
        }
    }
}
```

#### **Phase 3: Plugin Registry**
```csharp
// Plugin registry for command registration
public static class PluginRegistry
{
    private static readonly Dictionary<string, Command> _commands = new();
    
    public static void RegisterCommand(Command command)
    {
        _commands[command.Name] = command;
    }
    
    public static IEnumerable<Command> GetCommands()
    {
        return _commands.Values;
    }
}
```

#### **Phase 4: CLI Integration**
```csharp
// Integrate plugin commands into main CLI
public static class Program
{
    public static async Task Main(string[] args)
    {
        var rootCommand = new RootCommand("GameVM");
        
        // Load built-in commands
        rootCommand.AddCommand(BuildCommand());
        rootCommand.AddCommand(TestCommand());
        
        // Load plugin commands
        var pluginManager = new PluginManager();
        await pluginManager.LoadPluginsAsync();
        
        foreach (var command in PluginRegistry.GetCommands())
        {
            rootCommand.AddCommand(command);
        }
        
        await rootCommand.InvokeAsync(args);
    }
}
```

## Implementation Roadmap

### **Phase 1: Core Fluent CLI (Immediate)**
- [ ] Implement configuration system
- [ ] Create verb-only commands (build, test, run, analyze)
- [ ] Migrate existing GameVM.Compile to fluent interface
- [ ] Add configuration management commands

### **Phase 2: Tool Separation (Short-term)**
- [ ] Separate GameVM.Test as standalone tool
- [ ] Separate GameVM.Run as standalone tool
- [ ] Separate GameVM.Analyze as standalone tool
- [ ] Create GameVM orchestrator CLI

### **Phase 3: Plugin Foundation (Medium-term)**
- [ ] Refactor GameVM.DevTools into plugin architecture
- [ ] Create IGameVMPlugin interface
- [ ] Implement basic plugin manager
- [ ] Create plugin registry system

### **Phase 4: Plugin Ecosystem (Long-term)**
- [ ] Implement plugin repository system
- [ ] Create plugin development tools
- [ ] Add third-party plugin support
- [ ] Create plugin marketplace

### **Phase 5: Advanced Features (Future)**
- [ ] Plugin auto-update system
- [ ] Plugin dependency management
- [ ] Plugin security verification
- [ ] Plugin analytics and telemetry

## Benefits of This Design

### **Developer Experience**
```bash
# Get started immediately
GameVM new project MyGame
GameVM build
GameVM run
```

### **Professional Platform Feel**
- Matches industry standards (dotnet, cargo, npm)
- Progressive disclosure from simple to complex
- Configuration-driven development workflow

### **Extensibility**
- Plugin system for third-party tools
- Easy to add new targets and features
- Community-driven ecosystem

### **Unified Interface**
- All tools use consistent fluent syntax
- Learning one tool teaches you all tools
- Natural language reduces cognitive load

This design creates a **professional, extensible development platform** that can grow from a simple compiler to a complete retro game development ecosystem while maintaining the fluent, natural language interface throughout.