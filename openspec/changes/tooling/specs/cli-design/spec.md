# CLI Design Specification

## Purpose
Defines the design of a configuration-driven, fluent command-line interface for the GameVM platform. The design follows industry best practices from .NET Core, Cargo, and npm, providing simple verb-only commands backed by rich configuration files and an extensible plugin architecture. Aspirational — not yet implemented.

## Requirements

### Requirement: Simple Verb-Only Commands
The CLI MUST provide simple verb-only primary commands (`build`, `test`, `run`, `analyze`) that use sensible defaults from the project configuration.

#### Scenario: Bare build command
- **WHEN** the user invokes `GameVM build` in a directory containing `gamevm.json`
- **THEN** the CLI finds the project configuration, loads the build settings, and compiles the main source for the configured target

#### Scenario: Bare test command
- **WHEN** the user invokes `GameVM test` in a project directory
- **THEN** the CLI loads the test configuration and runs the configured test files on the configured target and harness

#### Scenario: Bare run command
- **WHEN** the user invokes `GameVM run` in a project directory
- **THEN** the CLI loads the run configuration and executes the built ROM on the configured target in the configured mode

### Requirement: Configuration-Driven Defaults
The CLI MUST be configuration-driven, reading defaults from a project `gamevm.json` file and a user global configuration file so that verb-only commands work without additional flags.

#### Scenario: Project configuration drives the build
- **WHEN** a `gamevm.json` file defines build settings such as target, dispatch, and optimization
- **THEN** `GameVM build` applies those settings without requiring command-line flags

### Requirement: Progressive Disclosure
The CLI MUST support progressive disclosure with three levels: verb-only (simplest), override specific values, and full fluent sentence-like syntax.

#### Scenario: Override a specific value
- **WHEN** the user invokes `GameVM build --target Genesis`
- **THEN** the CLI builds the project for the Genesis target, overriding the configured default

#### Scenario: Full fluent syntax
- **WHEN** the user invokes `GameVM build Game.pas for Genesis as Native Code --optimize --debug`
- **THEN** the CLI parses the fluent sentence to compile Game.pas for Genesis as native code with optimization and debug enabled

### Requirement: Configuration Discovery Priority
The CLI MUST resolve configuration using a defined discovery priority: command-line overrides, local project config, parent project config, user global config, then system defaults.

#### Scenario: Configuration lookup order
- **WHEN** configuration is resolved
- **THEN** command-line overrides win over the local `gamevm.json`, which wins over the user global config, which wins over built-in system defaults

### Requirement: Fluent Individual Tool Interfaces
The CLI MUST expose individual tools (GameVM.Compile, GameVM.Test, GameVM.Run, GameVM.Analyze) each with a consistent fluent sentence syntax describing the source, target, mode, and options.

#### Scenario: Direct compilation tool
- **WHEN** the user invokes `GameVM.Compile Engine.c for Genesis as Native Code`
- **THEN** the tool compiles Engine.c for the Genesis target as native code

#### Scenario: Fluent test tool with options
- **WHEN** the user invokes `GameVM.Test Game.rom on Atari2600 with MAME --verbose`
- **THEN** the tool runs Game.rom on Atari2600 with the MAME harness in verbose mode

#### Scenario: Run tool with breakpoints
- **WHEN** the user invokes `GameVM.Run Game.bin on Genesis in debug mode --breakpoints main`
- **THEN** the tool executes Game.bin on Genesis in debug mode with a breakpoint at `main`

#### Scenario: Analyze tool with report type
- **WHEN** the user invokes `GameVM.Analyze Game.rom for memory usage`
- **THEN** the tool analyzes Game.rom for memory usage and reports the metrics

### Requirement: Configuration Management Commands
The CLI MUST provide `config` commands to show, set, and reset configuration values.

#### Scenario: Show configuration
- **WHEN** the user invokes `GameVM config show build`
- **THEN** the CLI displays the build section of the effective configuration

#### Scenario: Set a configuration value
- **WHEN** the user invokes `GameVM config set build.target Genesis`
- **THEN** the CLI updates the build target configuration to Genesis

#### Scenario: Reset configuration
- **WHEN** the user invokes `GameVM config reset all`
- **THEN** the CLI resets all configuration to default values

### Requirement: Project Creation
The CLI MUST provide a `new` command to create projects from templates.

#### Scenario: Create a project without a template
- **WHEN** the user invokes `GameVM new project MyRetroGame`
- **THEN** the CLI scaffolds a default GameVM project directory for MyRetroGame

#### Scenario: Create a project from a template
- **WHEN** the user invokes `GameVM new project MyRetroGame --template atari2600`
- **THEN** the CLI scaffolds a project preconfigured for the Atari 2600 target

### Requirement: Extensible Plugin Architecture
The CLI MUST be extensible through a plugin system defined by the `IGameVMPlugin` interface, discovering and registering plugin commands into the root command.

#### Scenario: Plugin command registration
- **WHEN** the CLI starts and plugins are loaded
- **THEN** each plugin's registered commands are added to the root command and become invocable

#### Scenario: Plugin install and uninstall
- **WHEN** a user runs `GameVM plugin install GameVM.MAME`
- **THEN** the plugin manager downloads, verifies, and installs the plugin from the configured registry

### Requirement: Plugin Management Commands
The CLI MUST provide plugin management commands for listing, searching, showing info, installing, updating, and uninstalling plugins, plus managing plugin sources.

#### Scenario: List installed plugins
- **WHEN** the user invokes `GameVM plugin list`
- **THEN** the CLI lists the currently installed plugins with their versions and enabled state

#### Scenario: Manage plugin sources
- **WHEN** the user invokes `GameVM plugin source add https://custom-plugins.example.com`
- **THEN** the CLI registers the custom plugin source for subsequent plugin discovery

### Requirement: Plugin Development Support
The CLI MUST support plugin development workflows including create, build, publish, and test of plugins.

#### Scenario: Create a plugin
- **WHEN** the user invokes `GameVM plugin create MyPlugin --template basic`
- **THEN** the CLI scaffolds a new plugin project from the basic template with the plugin interface