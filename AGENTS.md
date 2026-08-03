# GameVM Agent Instructions

This file provides guidance for AI assistants and coding agents working on the GameVM project.
It supersedes the previous `.github/copilot-instructions.md` and is vendor-neutral: it applies
to any agent (Copilot, Codex, Cursor, Claude, opencode, etc.).

This root file is the entry point for agents. It contains a high-level overview of the project
and the high-level rules that must always be followed. Area-specific guidance lives in the
nested AGENTS.md files under `src/`, `docs/`, and `test/`, referenced below.

## Project Overview

**GameVM** is a cross-compiler toolchain designed for retro video game development. It enables developers to write games in modern, high-level languages (Pascal, C, etc.) and compile them to optimized bytecode for 2nd-5th generation gaming consoles (NES, SNES, Genesis, N64, PlayStation, Atari 2600, etc.).

### Key Characteristics
- **Host/Target Philosophy**: Complex analysis/optimization happens on modern hosts; output is tailored binaries (ROM/bytecode)
- **Multi-stage IR Pipeline**: HLIR (High-Level) → MLIR (Mid-Level) → LLIR (Low-Level)
- **Platform-Agnostic**: Single codebase supports multiple retro hardware platforms
- **Early Development**: Project is in active development, not production-ready

## High-Level Rules (MUST ALWAYS FOLLOW)

1. **Documentation updates are mandatory** with every code change. See [docs/AGENTS.md](docs/AGENTS.md).
2. **Follow code standards** and architecture conventions. See [src/AGENTS.md](src/AGENTS.md).
3. **Add tests** for any functional change. See [test/AGENTS.md](test/AGENTS.md).
4. **Run SonarQube checks** after making changes. See the SonarQube section below.

### Nested Agent Instructions

- [src/AGENTS.md](src/AGENTS.md) — compiler architecture, development guidelines, code standards, common tasks
- [docs/AGENTS.md](docs/AGENTS.md) — documentation update rules, spec workflow, documentation references
- [test/AGENTS.md](test/AGENTS.md) — testing strategy

## SonarQube Integration — Practical Usage (MUST FOLLOW)

### GUIDE Phase — Before Generating Code
On **free tier SonarCloud**, Context Augmentation tools (`get_guidelines`, `get_current_architecture`) are unavailable. Instead:
1. **Reference project standards manually** - Review `/docs/compiler/` and `/docs/platforms/specs/` for language/platform guidance
2. **Check recent commits** for patterns - Use `git log -p -S "<pattern>" -- src/` to find similar implementations
3. **Locate existing code with grep/glob** - Use file search for similar functionality before implementing
4. **When changing architecture/dependencies** - Focus on architecture-independent optimizations first (MLIR level)

### VERIFY Phase — After Generating Code
Use a **combined MCP + CLI approach** based on what's available:

1. **Read Phase**: Load current state of relevant source files

2. **Analysis Phase** (use what's available):
   - **If MCP `run_advanced_code_analysis` is available** (paid tier):
     * Call with: filePath, branchName, fileScope (MAIN/TEST)
   - **Always available via CLI** (free tier):
     * `sonar list issues -p kennethcochran_GameVM --format table` - See all open issues
     * `sonar analyze --staged` - Analyze staged changes (secrets, deps, local analysis)
     * `sonar list issues -p kennethcochran_GameVM --resolved=false --format table` - Unresolved issues only
     * For specific files: `sonar analyze src/path/to/changes.cs` (analyze local changes)

3. **Evaluation & Remediation**:
   - **For HIGH/BLOCKER severity or SECURITY quality** (from any source):
     * Call `sonar show rule <RULE_KEY>` to get remediation guidance
     * Fix the issue according to rule description
   - **For CLI-detected secrets or dependency risks**:
     * Remove hardcoded secrets immediately
     * Update vulnerable dependencies per `sonar analyze dependency-risks` output

4. **Verification**: 
   - Re-run `sonar analyze --staged` or `sonar list issues` to confirm fixes
   - Check quality gate: `sonar show quality-gate -p kennethcochran_GameVM`

### WORKFLOW EXAMPLES
**Before implementing a feature**:
- Review similar implementations in codebase
- Check platform-specific constraints in `/docs/platforms/specs/`
- Implement following existing patterns

**After making changes**:
1. Run `sonar analyze --staged` to catch secrets, dependency risks, and basic issues
2. If MCP available: run `run_advanced_code_analysis` for deep analysis
3. For any findings: use `sonar show rule <RULE>` to understand fix
4. Fix HIGH/BLOCKER/SECURITY items before considering work complete
5. Verify with `sonar analyze --staged` again

### AVAILABLE TOOLS BY TIER
**Free SonarCloud (your current)**:
- CLI: `sonar analyze`, `sonar list issues`, `sonar show rule`, `sonar show quality-gate`, `sonar analyze secrets`, `sonar analyze dependency-risks`
- MCP (limited): `projects` toolset (basic project info), `rules` toolset (via `show_rule` if exposed), basic issue querying
- **NOT available**: `get_guidelines`, `get_current_architecture`, `run_advanced_code_analysis`, Sonar Vortex analysis

**Paid SonarCloud (Team/Enterprise)**:
- All CLI tools above
- Full MCP toolsets: `cag` (Context Augmentation), `analysis` (Agentic Analysis), plus all free-tier tools
- Enables full Guide/Verify workflow from the original article

### TROUBLESHOOTING
If SonarQube MCP shows "failed":
1. Verify `SONARQUBE_TOKEN` is exported in shell: `echo $SONARQUBE_TOKEN`
2. Restart opencode after exporting token
3. Check container logs: `docker logs sonarqube-mcp` (if running manually)
4. The server requires ~30-60s to initialize analyzers on first start

**Note**: The SonarQube MCP Server must be running (via Docker) for MCP directives to work.

## Common Pitfalls to Avoid
1. **Assuming 32-bit architecture**: Remember, targets range from 8-bit (Atari 2600) to 32-bit (N64)
2. **Ignoring memory constraints**: ROM/RAM is severely limited on retro platforms
3. **Forgetting register allocation**: Physical registers are scarce on 8/16-bit targets
4. **Platform-specific optimizations too early**: Focus on architecture-independent optimizations in MLIR first
5. **Missing width specifiers**: LLIR instructions require explicit width types
6. **Skipping documentation updates**: See [Documentation Update Rules](docs/AGENTS.md)
7. **Suppressing compiler warnings**: NEVER use `#pragma warning disable`, `#pragma warning restore`, or `.editorconfig` severity overrides for compiler warnings or SonarQube rules. The ONLY exception is ANTLR-generated parser/lexer/visitor/listener code in `src/*/ANTLR/` directories, where warnings 0162, 0219, 1591, 419, 1591 may be suppressed. All other suppression is forbidden — fix the root cause instead.

## Quick Links
- **GitHub Repository**: https://github.com/kennethcochran/GameVM
- **Main README**: [README.md](README.md)
- **License**: [Unlicense](LICENSE)
- **Code of Conduct**: [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md)

## Getting Help
- Check existing documentation in `/docs/` directory
- Review test cases for usage examples
- Look at existing backend implementations for patterns
- Consult the issue tracker for known problems and discussions
