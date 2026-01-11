# GameVM Compiler Test Expansion Plan

**Date**: January 10, 2026  
**Objective**: Eliminate critical test gaps across optimizations, error handling, code generation, and end-to-end compilation

## Executive Summary

Current testing (88-90 tests) is heavily skewed toward low-level unit tests (74%) with minimal integration (3%) and E2E testing (2%). **8+ major compiler components have no tests**, and nearly all optimizers are completely unimplemented and untested. This plan expands test coverage to **150-160 total tests** while maintaining unit-test preference (targeting 60% unit, 30% integration, 10% E2E distribution).

**Testing Gap Overview**:
- ✅ Solid: Parser, AST, HLIR node creation (~65 existing tests)
- ⚠️ Weak: Mid-level transformations, control flow (MLIR/LLIR)
- ❌ Missing: All 3 optimizer stages, full compilation pipeline, error handling, code generation, register allocation

---

## Test Infrastructure Current State

### Test Projects by Type

| Project | Framework | Status | Test Count |
|---------|-----------|--------|-----------|
| GameVM.Compiler.Core.Tests | NUnit | ✅ Active | 22 |
| GameVM.Compiler.Pascal.Tests | NUnit | ✅ Active | 18 |
| GameVM.Compiler.Backend.Atari2600.Tests | NUnit | ⚠️ Partial | 23 |
| UnitTests | NUnit | ⚠️ Minimal | 3 |
| GameVM.Compiler.Specs | Reqnroll | ❌ Empty | 0 |
| GameVM.Compiler.E2E.Tests | NUnit | ❌ Mostly Empty | ~2 |
| GameVM.Compiler.Atari2600.Tests | LLVM lit | ⚠️ Minimal | 2 |
| **TOTAL** | - | - | **~88-90** |

### Test Distribution Target

```
Current State:          Target State:
- Unit: 74% (65)   →   - Unit: 60% (90-95)
- Integration: 3% (3)  →   - Integration: 30% (45-50)
- E2E: 2% (2)          →   - E2E: 10% (15-20)
- BDD: 0% (0)          →   - BDD: 5-10 scenarios
```

---

## Phase 1: Foundation - Unit Tests for Transformations (Weeks 1-2)

**Goal**: Ensure all IR transformations are correctly tested with actual transformation validation, not just null checks.

### 1.1 Fix Existing MLIR Transformation Tests

**File**: [GameVM.Compiler.Backend.Atari2600.Tests/MLIRToLLIRTransformerTests.cs](GameVM.Compiler.Backend.Atari2600.Tests/MLIRToLLIRTransformerTests.cs)

**Current Issue**: Tests check non-null results but don't validate actual transformation correctness.

**Improvements** (+5-8 tests):
- Add assertions for register assignments (verify X, Y, A register state after transformation)
- Add assertions for memory layout (verify variable addresses in zero-page/absolute addressing)
- Add control flow transformation tests (if/while/for/case statements)
- Add function call transformation tests (call stack frame generation)
- Add type coercion transformation tests (int→byte, implicit conversions)

### 1.2 Create HLIR-to-MLIR Transformer Tests

**New Test File**: `GameVM.Compiler.Core.Tests/Transformers/HLIRToMLIRTransformerTests.cs`

**Coverage** (+15-18 tests):
- Variable declarations → MLIR variables
- Function declarations → MLIR functions
- Type system mapping (primitive types, arrays, records, pointers)
- Control flow statements (if, while, for, case, repeat-until)
- Operator expressions (arithmetic, logical, bitwise, comparison)
- Built-in function calls (write, writeln)
- Error cases (undefined variables, type mismatches, invalid statements)

**Test Patterns**:
```csharp
[Test]
public void Transform_SimpleVariableDeclaration_CreatesMLIRVariable()
{
    // Arrange
    var hlir = CreateSimpleProgram();
    var transformer = new HLIRToMLIRTransformer();
    
    // Act
    var result = transformer.Transform(hlir);
    
    // Assert
    Assert.That(result.Variables, Has.Count.EqualTo(1));
    Assert.That(result.Variables[0].Name, Is.EqualTo("x"));
    Assert.That(result.Variables[0].Type.Name, Is.EqualTo("Integer"));
}
```

### 1.3 Create LLIR-to-Final Transformer Tests

**New Test File**: `GameVM.Compiler.Backend.Atari2600.Tests/LLIRToFinalTransformerTests.cs`

**Coverage** (+10-12 tests):
- Assembly instruction generation from LLIR blocks
- Register state management (A, X, Y across blocks)
- Stack management (push/pop sequences)
- Branch/jump target resolution
- Function prologue/epilogue generation
- Memory address resolution (direct, zero-page, indexed)

---

## Phase 2: Optimization Tests (Weeks 2-3)

**Goal**: Implement and test all three optimizer stages (mid-level, low-level, final IR).

### 2.1 DefaultMidLevelOptimizer Tests

**New Test File**: `GameVM.Compiler.Core.Tests/Optimizers/MidLevelOptimizerTests.cs`

**Coverage** (+10-12 tests):
- Dead code elimination (unreachable statements)
- Constant propagation
- Common subexpression elimination
- Loop invariant code motion
- Function inlining (small functions)
- Unused variable removal

**Test Structure**:
```csharp
[TestFixture]
public class MidLevelOptimizerTests
{
    private DefaultMidLevelOptimizer _optimizer;
    
    [SetUp]
    public void Setup() => _optimizer = new DefaultMidLevelOptimizer();
    
    [Test]
    public void Optimize_DeadCodePath_RemovesUnreachableStatements() { }
    
    [Test]
    public void Optimize_ConstantPropagate_SimplifiesExpressions() { }
    
    [Test]
    public void Optimize_CommonSubexpressions_EliminatesDuplicates() { }
}
```

### 2.2 DefaultLowLevelOptimizer Tests

**New Test File**: `GameVM.Compiler.Backend.Atari2600.Tests/LowLevelOptimizerTests.cs`

**Coverage** (+10-12 tests):
- Register allocation optimization
- Instruction peepholing (e.g., LDA → STA without intermediate store)
- Branch optimization (remove redundant jumps)
- Load/store optimization (combine adjacent operations)
- Memory access optimization
- Loop unrolling (small loops)

### 2.3 DefaultFinalIROptimizer Tests

**New Test File**: `GameVM.Compiler.Backend.Atari2600.Tests/FinalIROptimizerTests.cs`

**Coverage** (+8-10 tests):
- Final assembly optimization
- Unused register cleanup
- Stack frame optimization
- ROM size optimization
- Debug info optimization

---

## Phase 3: Error Handling & Negative Testing (Weeks 3-4)

**Goal**: Ensure all error paths are properly handled and reported.

### 3.1 Parser Error Tests

**New Test File**: `GameVM.Compiler.Pascal.Tests/ParserErrorTests.cs`

**Coverage** (+8-10 tests):
- Syntax errors (missing semicolons, invalid tokens)
- Unexpected EOF
- Mismatched parentheses/brackets
- Invalid declarations
- Error recovery scenarios

### 3.2 Type System Validation Tests

**New Test File**: `GameVM.Compiler.Core.Tests/TypeSystemValidationTests.cs`

**Coverage** (+10-12 tests):
- Type mismatch detection (int ≠ string)
- Array bounds validation
- Function signature mismatches
- Undefined variable references
- Implicit type conversion edge cases
- Circular type definitions

### 3.3 Compilation Pipeline Error Tests

**File**: Enhance `GameVM.Compiler.Application/CompileUseCase` tests

**Coverage** (+5-7 tests):
- Remove mocking; test real compilation with invalid inputs
- Resource constraint errors (ROM overflow, RAM overflow)
- Invalid optimization level selection
- Missing language service
- Malformed IR input

### 3.4 Edge Case Tests

**New Test File**: `GameVM.Compiler.Pascal.Tests/EdgeCaseTests.cs`

**Coverage** (+8-10 tests):
- Empty program
- Maximum integer values (overflow)
- Deep nesting (50+ levels)
- Large programs (1000+ lines)
- Unicode/special characters in comments
- Whitespace variations

---

## Phase 4: Integration Tests (Weeks 4-5)

**Goal**: Test multi-stage compilation without mocks; validate full pipeline correctness.

### 4.1 Compilation Pipeline Integration Tests

**New Test File**: `GameVM.Compiler.E2E.Tests/CompilationPipelineTests.cs`

**Coverage** (+15-18 tests):

```csharp
[TestFixture]
public class CompilationPipelineTests
{
    private CompileUseCase _compiler;
    
    [SetUp]
    public void Setup()
    {
        // Create real compiler without mocks
        _compiler = new CompileUseCase(
            new PascalFrontend(),
            new MidLevelOptimizer(),
            new LowLevelOptimizer(),
            new Atari2600Backend());
    }
    
    [Test]
    public void Compile_SimpleHelloWorld_ProducesValidBytecode() { }
    
    [Test]
    public void Compile_WithOptimizations_ReducesROMSize() { }
    
    [Test]
    public void Compile_InvalidProgram_ReturnsCompilationErrors() { }
    
    [Test]
    public void Compile_ExceedsROMLimit_ReportsResourceError() { }
}
```

**Test Scenarios**:
- Simple variable declaration and assignment
- Control flow (if/else, loops, switch)
- Function calls and recursion
- Built-in functions (write, writeln)
- Array operations
- Type conversions
- Error propagation across stages
- Optimization impact (ROM size reduction)

### 4.2 Language Feature Integration Tests

**New Test File**: `GameVM.Compiler.E2E.Tests/LanguageFeatureTests.cs`

**Coverage** (+12-15 tests):
- Pascal language features end-to-end
- Type system completeness
- Control flow compilation
- Memory layout validation

---

## Phase 5: LLVM LIT Regression Tests (Weeks 5-6)

**Goal**: Expand lit test framework for Atari 2600 validation; add regression coverage.

### 5.1 Expand Feature Tests

**Directory**: `GameVM.Compiler.Atari2600.Tests/Features/`

**New Test Files** (+12-15 tests):
- `control_flow.ll` - if/else, loops, switch statements
- `function_calls.ll` - function calls, return values, stack frames
- `register_allocation.ll` - variable assignment to X/Y/A
- `memory_layout.ll` - zero-page, absolute addressing, bank switching
- `rom_generation.ll` - 4KB/8KB ROM structure validation
- `builtin_functions.ll` - write, writeln, graphics calls
- `edge_cases.ll` - empty program, max values, deep nesting

### 5.2 Add Regression Tests

**Directory**: `GameVM.Compiler.Atari2600.Tests/Regression/`

**Coverage** (+8-10 tests):
- Document all past bugs with regression tests
- Test historical compiler failures
- Performance regression detection

### 5.3 MAME Execution Validation

**Existing Files**: [GameVM.Compiler.Atari2600.Tests/Features/bytecode_output_validation.ll](GameVM.Compiler.Atari2600.Tests/Features/bytecode_output_validation.ll), [GameVM.Compiler.Atari2600.Tests/Features/mame_execution_validation.ll](GameVM.Compiler.Atari2600.Tests/Features/mame_execution_validation.ll)

**Enhancements**:
- Expand MAME validator to check visual output (TIA graphics)
- Validate joystick input handling
- Test sound/SFX generation
- Performance profiling (CPU cycles)

---

## Phase 6: BDD Specification Tests (Weeks 6-7)

**Goal**: Implement feature-driven development tests using Reqnroll.

### 6.1 Create Feature Files

**Directory**: `GameVM.Compiler.Specs/Features/`

**Feature Files** (5-10 features):

```gherkin
# Features/Compilation.feature
Feature: Compilation Pipeline
  Scenario: Compile simple Pascal program
    Given a Pascal program with a single write statement
    When I compile the program
    Then the compilation succeeds
    And the output binary contains valid 6502 instructions

# Features/ErrorHandling.feature
Feature: Error Handling
  Scenario: Undefined variable error
    Given a Pascal program with an undefined variable reference
    When I compile the program
    Then the compilation fails
    And the error message contains "undefined variable"
    
# Features/Optimization.feature
Feature: Code Optimization
  Scenario: Dead code elimination
    Given a Pascal program with unreachable code
    When I compile with optimization enabled
    Then the generated binary is smaller
    And no dead code instructions appear in output
```

### 6.2 Implement Step Definitions

**File**: `GameVM.Compiler.Specs/Steps/CompilationSteps.cs`

**Coverage**:
- Program creation and setup
- Compilation execution
- Output validation
- Error assertion
- Performance metrics

---

## Phase 7: Test Infrastructure Refactoring (Ongoing)

### 7.1 Remove Placeholder Tests

**Action**: Delete all test methods that only contain `Assert.Pass()` or similar placeholders

**Files to Clean**:
- [GameVM.Compiler.Core.Tests/](GameVM.Compiler.Core.Tests/) - 1 placeholder
- [GameVM.Compiler.Backend.Atari2600.Tests/](GameVM.Compiler.Backend.Atari2600.Tests/) - 1 placeholder

### 7.2 Create Test Fixtures & Builders

**New File**: `GameVM.Compiler.Core.Tests/Fixtures/IRBuilder.cs`

**Purpose**: Simplify IR construction in tests

```csharp
public class IRBuilder
{
    public HLIRFunction CreateFunction(string name, params Variable[] variables) { }
    public Variable CreateVariable(string name, string type) { }
    public HLIRBlock CreateBlock(params Statement[] statements) { }
    public IfStatement CreateIf(Expression condition, HLIRBlock trueBlock, HLIRBlock falseBlock) { }
    // ... more builders
}
```

**New File**: `GameVM.Compiler.Pascal.Tests/Fixtures/PascalProgramBuilder.cs`

**Purpose**: Build Pascal programs for testing

```csharp
public class PascalProgramBuilder
{
    public PascalProgram WithVariable(string name, string type) { }
    public PascalProgram WithFunction(string name, string returnType) { }
    public PascalProgram WithStatement(string statement) { }
    public PascalProgram Build() { }
}
```

### 7.3 Parametrized Tests

**Action**: Convert repetitive tests to parametrized versions

**Example**:
```csharp
[TestCaseSource(nameof(GetM6502Instructions))]
public void Emitter_GeneratesCorrectBytecode(M6502Instruction instruction, byte[] expectedBytes)
{
    // Act
    var result = _emitter.Emit(instruction);
    
    // Assert
    Assert.That(result, Is.EqualTo(expectedBytes));
}

private static IEnumerable<TestCaseData> GetM6502Instructions()
{
    yield return new TestCaseData(new LDA { Mode = AddressingMode.Immediate, Value = 0x42 }, new byte[] { 0xA9, 0x42 });
    yield return new TestCaseData(new LDA { Mode = AddressingMode.ZeroPage, Value = 0x80 }, new byte[] { 0xA5, 0x80 });
    // ... more cases
}
```

### 7.4 Configure Mutation Testing

**New File**: `stryker-config.json`

**Purpose**: Detect weak tests with mutation testing

```json
{
  "projectUnderTest": "GameVM.Compiler.Core.csproj",
  "testRunner": "nunit",
  "reporters": ["html"],
  "thresholds": {
    "high": 85,
    "low": 70
  }
}
```

---

## Implementation Timeline

| Phase | Duration | Tests | Effort | Priority |
|-------|----------|-------|--------|----------|
| 1: Transformations | 2 weeks | +30-38 | HIGH | 🔴 CRITICAL |
| 2: Optimizers | 1 week | +28-34 | HIGH | 🔴 CRITICAL |
| 3: Error Handling | 1 week | +31-39 | HIGH | 🔴 CRITICAL |
| 4: Integration | 1 week | +27-33 | MEDIUM | 🟡 IMPORTANT |
| 5: LLVM LIT | 1 week | +20-25 | MEDIUM | 🟡 IMPORTANT |
| 6: BDD | 1 week | +5-10 | LOW | 🟢 NICE-TO-HAVE |
| 7: Refactoring | Ongoing | N/A | LOW | 🟢 NICE-TO-HAVE |

**Total New Tests**: 141-179 tests  
**Total Tests After Plan**: ~230-270 tests

---

## Test Distribution Results (After Plan)

```
Target Distribution:
├─ Unit Tests:           ~95-110 (42%)
├─ Integration Tests:    ~80-100 (38%)
├─ E2E Tests:           ~25-35 (12%)
├─ BDD Tests:           ~5-10 (4%)
├─ Regression Tests:    ~8-10 (4%)
```

**Coverage by Component**:
- ✅ Parser/AST: 18 tests (maintained)
- ✅ HLIR: 22 tests (maintained)
- ✅ Transformations: 50-60 tests (NEW - was ~10)
- ✅ Optimizers: 28-34 tests (NEW - was 0)
- ✅ Code Generation: 35-45 tests (expanded - was ~12)
- ✅ Error Handling: 31-39 tests (NEW - was ~2)
- ✅ Integration Pipeline: 27-33 tests (NEW - was 3 mocked)
- ✅ E2E/Regression: 25-35 tests (expanded - was 2)

---

## Success Criteria

- [ ] All placeholder tests replaced with real implementations
- [ ] Zero TODOs in critical components (optimizers, transformers)
- [ ] Error handling tests cover all documented error cases
- [ ] Integration tests pass with real (unmocked) components
- [ ] Code coverage ≥75% for core compiler modules
- [ ] LLVM lit tests validated with MAME execution
- [ ] BDD scenarios map to actual test implementations
- [ ] Mutation testing shows >80% test efficacy
- [ ] All tests execute in <5 minutes (CI/CD friendly)

---

## Rollout Strategy

1. **Week 1**: Phase 1 (Transformations) - Priority for correctness
2. **Week 2**: Phase 2 (Optimizers) + Phase 3 (Error Handling) - Priority for completeness
3. **Week 3**: Phase 4 (Integration) + Phase 5 (LLVM LIT) - Validate full pipeline
4. **Week 4**: Phase 6 (BDD) + Phase 7 (Refactoring) - Polish and documentation

**Continuous Integration**: Each phase should pass all existing tests before merging.

---

## Risk Mitigation

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|-----------|
| Tests take too long | Medium | HIGH | Use parametrized tests; optimize lit test execution |
| Coverage remains incomplete | Low | HIGH | Track coverage metrics; prioritize unit > integration |
| Optimizer implementation incomplete | Medium | HIGH | Start with simple optimizations; expand iteratively |
| MAME validation flaky | Medium | MEDIUM | Mock MAME interface for reliability; keep real tests minimal |
| Test maintenance burden | High | MEDIUM | Use builders/fixtures; avoid brittle assertions |

---

## Notes

- **Focus on correctness**: Unit tests should validate actual transformation correctness, not just null checks
- **Avoid integration test proliferation**: Strict separation of unit vs integration; mocks allowed in unit tests only
- **BDD as documentation**: Feature files should describe behavior in human-readable terms
- **Optimization tests critical**: Currently unimplemented optimizers are major tech debt
- **Error handling often forgotten**: Dedicate full phase to negative testing and error paths

