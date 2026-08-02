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
See `opencode.json` for MCP server configuration.