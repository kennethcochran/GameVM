# SonarQube Integration Skill
A skill for integrating SonarQube Cloud analysis into OpenCode workflows, optimized for free tier limitations.

## Description
Provides practical SonarQube usage patterns that work with free SonarCloud tier, combining MCP server capabilities (where available) with CLI commands for comprehensive code quality and security checking.

## When to Use
Use this skill when:
- Writing new code and want to check for issues before committing
- Reviewing existing code for quality/security problems  
- Preparing code for review and want to run automated checks
- Need to understand specific SonarQube rules and remediation guidance

## Commands and Usage

### Basic Issue Checking
```bash
# List all issues in project (free tier CLI)
sonar list issues -p kennethcochran_GameVM --format table

# List only unresolved issues
sonar list issues -p kennethcochran_GameVM --resolved=false --format table

# Check specific file for issues
sonar analyze src/GameVM.Compiler.Core/SomeFile.cs
```

### Secrets and Dependency Scanning (Free Tier CLI)
```bash
# Scan for hardcoded secrets
sonar analyze secrets src/config.cs

# Scan dependencies for vulnerabilities
sonar analyze dependency-risks
```

### Rule Guidance (Works with Free Tier)
```bash
# Get details about a specific rule
sonar show rule S112 --format json

# Get remediation guidance for a rule
sonar show rule S5747
```

### Quality Gate Status
```bash
# Check if project passes quality gate
sonar show quality-gate -p kennethcochran_GameVM
```

### MCP Server Usage (When Available)
If SonarQube MCP server is connected and tools are available:

```text
# For paid tier users with Context Augmentation:
get_guidelines           # Get project coding standards
get_current_architecture # Understand project structure  

# For verification phase:
run_advanced_code_analysis {
  filePath: "src/GameVM.Compiler.Core/File.cs",
  branchName: "main", 
  fileScope: "MAIN"
}
show_rule { ruleKey: "S112" }  # Get rule details
```

## Best Practices

### Before Writing Code (Guide Phase)
1. **Manual Standards Review**: Check `/docs/compiler/` and `/docs/platforms/specs/` for language/platform guidance
2. **Pattern Matching**: Use `git log -p -S "<pattern>" -- src/` to find similar implementations
3. **Existing Code Search**: Use file search tools to locate similar functionality

### After Writing Code (Verify Phase)
1. **Secrets Check**: Always run `sonar analyze secrets` on new/modified files
2. **Basic Analysis**: Run `sonar analyze --staged` on staged changes
3. **Issue Review**: Check `sonar list issues` for new HIGH/BLOCKER/SECURITY findings
4. **Rule Understanding**: Use `sonar show rule` to understand any triggered rules
5. **Fix and Re-check**: Fix issues and re-run analysis to confirm resolution

### Workflow Integration
For OpenCode agents:
1. When starting a coding task, first check for similar patterns in codebase
2. After generating code, run CLI-based verification:
   - `sonar analyze secrets` on modified files
   - `sonar analyze --staged` on staged changes
   - If MCP available and tools work, use `run_advanced_code_analysis`
3. For any findings, use `sonar show rule` to understand remediation
4. Fix HIGH/BLOCKER/SECURITY issues before considering work complete
5. Always verify fixes with follow-up analysis

## Error Handling
- If MCP tools fail with "Connection closed" or similar, fall back to CLI equivalents
- If `run_advanced_code_analysis` is not available (free tier), rely on CLI tools
- Always verify token is available: `echo $SONARQUBE_TOKEN` should show value
- First-time MCP startup may take 60+ seconds to download analyzers

## Related Files
- `opencode.json` - MCP server configuration
- `/docs/compiler/` - Language and IR specifications  
- `/docs/platforms/specs/` - Platform-specific constraints
- `.opencode/skills/` - Location for this skill file