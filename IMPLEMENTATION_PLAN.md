# Pascal Compiler Refactoring Implementation Plan

## Overview
This document provides a detailed, step-by-step implementation plan for breaking up `ASTVisitor.cs` and `PascalAstToHlirTransformer.cs` into smaller, independently maintainable classes using the **delegation pattern**.

---

## Current Focus: Phase 6 - Future Improvements

### Architecture Decision: Delegation Pattern
Each visitor class (ExpressionVisitor, StatementVisitor, DeclarationVisitor) is a separate class that inherits from `PascalBaseVisitor<PascalASTNode>`. The main `ASTVisitor` orchestrates calls to these sub-visitors through simple delegation methods.

**Rationale:**
- Clear separation of concerns
- Each visitor independently testable
- Easy dependency injection
- Scales well for future extensions
- Consistent with planned transformer architecture

---

## Phase 4: AST Visitor Refactoring - IN PROGRESS

### Objective
Break down the monolithic `ASTVisitor` class (1,069 lines) into three focused visitor classes:
- `StatementVisitor.cs` - handles statement-related visits ✓
- `ExpressionVisitor.cs` - handles expression-related visits ✓
- `DeclarationVisitor.cs` - handles declaration-related visits ✓
- Main `ASTVisitor.cs` - orchestrator that delegates to sub-visitors ✓

### Current Status: PARTIALLY COMPLETE
- ✓ Phase 4.1: ErrorNode and TypeIdentifierNode extracted
- ✓ Phase 4.2: ExpressionVisitor.cs created
- ✓ Phase 4.3: StatementVisitor.cs created
- ✓ Phase 4.4: DeclarationVisitor.cs created
- ✓ Phase 4.5: Main ASTVisitor.cs refactored to orchestrator
- ✓ Phase 4.6: Code review and optimization
- ⏳ Phase 4.7: Integration testing and validation (NEXT)

### Step 4.1: Extract ErrorNode and TypeIdentifierNode to Separate Files

**Files to Create:**
- `/home/kenneth/Projects/GameVM/src/GameVM.Compiler.Pascal/ErrorNode.cs`
- `/home/kenneth/Projects/GameVM/src/GameVM.Compiler.Pascal/TypeIdentifierNode.cs`

**Instructions:**
1. Create `ErrorNode.cs` with the `ErrorNode` class currently at lines 1054-1062 in `ASTVisitor.cs`
2. Create `TypeIdentifierNode.cs` with the `TypeIdentifierNode` class currently at lines 1064-1068 in `ASTVisitor.cs`
3. Both classes should inherit from `PascalASTNode` or `TypeNode` as appropriate
4. Remove these class definitions from `ASTVisitor.cs` after extraction

**Unit Testing (Integrated):**
- Verify ErrorNode and TypeIdentifierNode can be instantiated
- Verify they inherit from correct base classes
- Run full test suite: `dotnet test /home/kenneth/Projects/GameVM/src/GameVM.Compiler.Pascal.Tests/`
- Expected: All existing tests pass with no regressions

**Reference in ASTVisitor.cs:**
```
Lines 1054-1062: ErrorNode class
Lines 1064-1068: TypeIdentifierNode class
```

---

### Step 4.2: Create ExpressionVisitor.cs

**File to Create:**
- `/home/kenneth/Projects/GameVM/src/GameVM.Compiler.Pascal/ExpressionVisitor.cs`

**Base Class:** `PascalBaseVisitor<PascalASTNode>`

**Methods to Move from ASTVisitor.cs:**
- `VisitExpression()` - line 177
- `VisitSimpleExpression()` - line 198
- `VisitTerm()` - line 219
- `VisitSignedFactor()` - line 240
- `VisitFactor()` - line 263
- `VisitSet_()` - line 1016
- Any helper methods related to expressions

**Constructor:**
```csharp
public ExpressionVisitor()
{
}
```

**Key Responsibilities:**
- Handle all expression-related parsing
- Return `ExpressionNode` or derived types
- Handle operators: relational, additive, multiplicative, unary
- Handle literals, variables, and parenthesized expressions

**Unit Testing (Integrated):**
- Verify ExpressionVisitor can be instantiated
- Test each Visit method with valid and invalid inputs
- Verify proper error handling (ErrorNode returns)
- Run full test suite after implementation
- Expected: All existing tests pass with no regressions

---

### Step 4.3: Create StatementVisitor.cs

**File to Create:**
- `/home/kenneth/Projects/GameVM/src/GameVM.Compiler.Pascal/StatementVisitor.cs`

**Base Class:** `PascalBaseVisitor<PascalASTNode>`

**Methods to Move from ASTVisitor.cs:**
- `VisitCompoundStatement()` - line 128
- `VisitStatement()` - line 144
- `VisitAssignmentStatement()` - line 152
- `VisitIfStatement()` - (find in full file)
- `VisitWhileStatement()` - (find in full file)
- `VisitRepeatStatement()` - (find in full file)
- `VisitForStatement()` - (find in full file)
- `VisitCaseStatement()` - (find in full file)
- `VisitWithStatement()` - line 965
- Any other statement-related Visit methods

**Constructor:**
```csharp
public StatementVisitor(ExpressionVisitor expressionVisitor)
{
    _expressionVisitor = expressionVisitor;
}
```

**Dependencies:**
- `ExpressionVisitor` - for parsing expressions within statements

**Key Responsibilities:**
- Handle all statement-related parsing
- Delegate expression parsing to `ExpressionVisitor`
- Return `StatementNode` or derived types

**Unit Testing (Integrated):**
- Verify StatementVisitor can be instantiated with ExpressionVisitor dependency
- Test each Visit method with valid and invalid inputs
- Verify proper delegation to ExpressionVisitor
- Verify proper error handling (ErrorNode returns)
- Run full test suite after implementation
- Expected: All existing tests pass with no regressions

---

### Step 4.4: Create DeclarationVisitor.cs

**File to Create:**
- `/home/kenneth/Projects/GameVM/src/GameVM.Compiler.Pascal/DeclarationVisitor.cs`

**Base Class:** `PascalBaseVisitor<PascalASTNode>`

**Methods to Move from ASTVisitor.cs:**
- `VisitProcedureDeclaration()` - line 290
- `VisitFunctionDeclaration()` - (find in full file)
- `VisitVariableDeclaration()` - (find in full file)
- `VisitTypeDefinition()` - (find in full file)
- `VisitTypeDenoter()` - (find in full file)
- `VisitSimpleType()` - (find in full file)
- `VisitStructuredType()` - (find in full file)
- `VisitArrayType()` - (find in full file)
- `VisitRecordType()` - (find in full file)
- `VisitSetType()` - (find in full file)
- `VisitFileType()` - line 956
- `VisitUnpackedStructuredType()` - line 994
- Helper methods: `BuildRecordTypeNode()`, `VisitSetTypeWithDefaultName()`, `VisitFileTypeWithDefaultName()`

**Constructor:**
```csharp
public DeclarationVisitor(ExpressionVisitor expressionVisitor)
{
    _expressionVisitor = expressionVisitor;
}
```

**Dependencies:**
- `ExpressionVisitor` - for parsing expressions in type definitions

**Key Responsibilities:**
- Handle all declaration-related parsing (procedures, functions, variables, types)
- Delegate expression parsing to `ExpressionVisitor`
- Return declaration nodes or type nodes

**Unit Testing (Integrated):**
- Verify DeclarationVisitor can be instantiated with ExpressionVisitor dependency
- Test each Visit method with valid and invalid inputs
- Verify proper delegation to ExpressionVisitor
- Verify proper error handling (ErrorNode returns)
- Test complex type definitions (arrays, records, sets, etc.)
- Run full test suite after implementation
- Expected: All existing tests pass with no regressions

---

### Step 4.5: Refactor Main ASTVisitor.cs

**File to Modify:**
- `/home/kenneth/Projects/GameVM/src/GameVM.Compiler.Pascal/ASTVisitor.cs`

**Instructions:**
1. Keep the class definition and base class inheritance
2. Add private fields for sub-visitors:
   ```csharp
   private readonly ExpressionVisitor _expressionVisitor;
   private readonly StatementVisitor _statementVisitor;
   private readonly DeclarationVisitor _declarationVisitor;
   ```
3. Update constructor:
   ```csharp
   public ASTVisitor()
   {
       _expressionVisitor = new ExpressionVisitor();
       _statementVisitor = new StatementVisitor(_expressionVisitor);
       _declarationVisitor = new DeclarationVisitor(_expressionVisitor);
   }
   ```
4. Keep only orchestration methods:
   - `VisitProgram()` - line 30
   - `VisitBlock()` - line 48
   - `VisitVariable()` - line 167
5. Delegate all other Visit methods to appropriate sub-visitors
6. Remove all moved methods
7. Remove `ErrorNode` and `TypeIdentifierNode` class definitions
8. Clean up redundant `using GameVM.Compiler.Pascal;` statements (lines 7-24)

**Methods to Keep:**
- `VisitProgram()` - orchestrates block parsing
- `VisitBlock()` - orchestrates all block-level parsing
- `VisitVariable()` - simple variable parsing (can stay or move to ExpressionVisitor)

**Methods to Delegate:**
- All expression-related methods → `ExpressionVisitor`
- All statement-related methods → `StatementVisitor`
- All declaration-related methods → `DeclarationVisitor`

**Unit Testing (Integrated):**
- Verify ASTVisitor can be instantiated with all sub-visitors
- Test that VisitProgram and VisitBlock work correctly
- Verify proper delegation to all sub-visitors
- Test complex programs with mixed expressions, statements, and declarations
- Verify no methods are missing from delegation
- Run full test suite after implementation
- Expected: All existing tests pass with no regressions (23+ tests passing)

---

### Step 4.6: Code Review and Optimization

**Objectives:**
1. Review each visitor class for code quality and consistency
2. Ensure all methods follow the same naming and style conventions
3. Verify proper error handling in each visitor
4. Check for any missed Visit methods that should be delegated
5. Optimize delegation methods for performance

**Checklist:**
- [x] ExpressionVisitor.cs - review and optimize (removed unused using statements)
- [x] StatementVisitor.cs - review and optimize (removed unused using statements)
- [x] DeclarationVisitor.cs - review and optimize (removed unused using statements)
- [x] ASTVisitor.cs - verify orchestration is clean
- [x] All using statements are necessary
- [x] No duplicate code between visitors

**Unit Testing (Integrated):**
- Run full test suite to verify code quality improvements
- Verify no regressions introduced by optimizations
- Expected: All existing tests pass (23+ tests passing)

---

### Step 4.7: Integration Testing and Validation

**Objectives:**
1. Run existing Pascal compiler tests
2. Verify no regressions in AST generation
3. Test complex programs with mixed expressions, statements, and declarations
4. Validate that refactoring maintains 100% functional compatibility

**Commands:**
```bash
# Build the solution
dotnet build /home/kenneth/Projects/GameVM/GameVM.sln

# Run Pascal compiler tests
dotnet test /home/kenneth/Projects/GameVM/src/GameVM.Compiler.Pascal.Tests/ --verbosity normal

# Run full solution tests
dotnet test /home/kenneth/Projects/GameVM/ --verbosity normal
```

**Expected Results:**
- Build succeeds with 0 errors
- 23+ tests pass in Pascal compiler tests
- No regressions in other projects
- All visitor delegation working correctly

---

## Phase 5: Transformer Refactoring (COMPLETE ✓)

### Objective
Break down the monolithic `PascalAstToHlirTransformer` class (686 lines) into three focused transformer classes using the same delegation pattern as Phase 4:
- [x] `StatementTransformer.cs` - handles statement transformations
- [x] `ExpressionTransformer.cs` - handles expression transformations
- [x] `DeclarationTransformer.cs` - handles declaration transformations
- [x] Main `PascalAstToHlirTransformer.cs` - orchestrator

### Unit Testing Strategy for Phase 5 (Completed)
Unit testing was integrated into each step:
- **Step 5.1-5.3**: Verified after creating each transformer
- **Step 5.4**: Verified after refactoring main transformer
- **Final Validation**: All 26+ tests pass with no regressions

### Step 5.1: Create ExpressionTransformer.cs (Complete)

**File to Create:**
- `/home/kenneth/Projects/GameVM/src/GameVM.Compiler.Pascal/ExpressionTransformer.cs`

**Constructor:**
```csharp
public ExpressionTransformer(string sourceFile, Dictionary<string, HighLevelIR.HLType> typeCache, Dictionary<string, IRSymbol> symbolTable)
{
    _sourceFile = sourceFile;
    _typeCache = typeCache;
    _symbolTable = symbolTable;
}
```

**Methods to Move from PascalAstToHlirTransformer:**
- All methods that transform expression nodes
- Helper methods for expression transformation

**Key Responsibilities:**
- Transform expression AST nodes to HLIR expressions
- Handle operators, literals, variables
- Use shared type cache and symbol table

**Unit Testing (Integrated):**
- Verify ExpressionTransformer can be instantiated with required dependencies
- Test each transformation method with valid and invalid inputs
- Verify proper error handling
- Run full test suite after implementation
- Expected: All existing tests pass with no regressions

---

### Step 5.2: Create StatementTransformer.cs

**File to Create:**
- `/home/kenneth/Projects/GameVM/src/GameVM.Compiler.Pascal/StatementTransformer.cs`

**Constructor:**
```csharp
public StatementTransformer(string sourceFile, ExpressionTransformer expressionTransformer, Dictionary<string, HighLevelIR.HLType> typeCache, Dictionary<string, IRSymbol> symbolTable)
{
    _sourceFile = sourceFile;
    _expressionTransformer = expressionTransformer;
    _typeCache = typeCache;
    _symbolTable = symbolTable;
}
```

**Methods to Move from PascalAstToHlirTransformer:**
- All methods that transform statement nodes
- Helper methods for statement transformation

**Key Responsibilities:**
- Transform statement AST nodes to HLIR statements
- Delegate expression transformation to `ExpressionTransformer`
- Use shared type cache and symbol table

**Unit Testing (Integrated):**
- Verify StatementTransformer can be instantiated with required dependencies
- Test each transformation method with valid and invalid inputs
- Verify proper delegation to ExpressionTransformer
- Verify proper error handling
- Run full test suite after implementation
- Expected: All existing tests pass with no regressions

---

### Step 5.3: Create DeclarationTransformer.cs

**File to Create:**
- `/home/kenneth/Projects/GameVM/src/GameVM.Compiler.Pascal/DeclarationTransformer.cs`

**Constructor:**
```csharp
public DeclarationTransformer(string sourceFile, ExpressionTransformer expressionTransformer, Dictionary<string, HighLevelIR.HLType> typeCache, Dictionary<string, IRSymbol> symbolTable, Stack<HighLevelIR.Function> currentFunctionScope)
{
    _sourceFile = sourceFile;
    _expressionTransformer = expressionTransformer;
    _typeCache = typeCache;
    _symbolTable = symbolTable;
    _currentFunctionScope = currentFunctionScope;
}
```

**Methods to Move from PascalAstToHlirTransformer:**
- All methods that transform declaration nodes
- Helper methods for declaration transformation

**Key Responsibilities:**
- Transform declaration AST nodes to HLIR declarations
- Delegate expression transformation to `ExpressionTransformer`
- Manage function scope stack
- Use shared type cache and symbol table

**Unit Testing (Integrated):**
- Verify DeclarationTransformer can be instantiated with required dependencies
- Test each transformation method with valid and invalid inputs
- Verify proper delegation to ExpressionTransformer
- Verify proper function scope management
- Verify proper error handling
- Run full test suite after implementation
- Expected: All existing tests pass with no regressions

---

### Step 5.4: Refactor Main PascalAstToHlirTransformer.cs

**File to Modify:**
- `/home/kenneth/Projects/GameVM/src/GameVM.Compiler.Pascal/PascalAstToHlirTransformer.cs`

**Instructions:**
1. Keep shared state fields:
   ```csharp
   private readonly string _sourceFile;
   private readonly HighLevelIR _ir;
   private readonly Stack<HighLevelIR.Function> _currentFunctionScope;
   private readonly Dictionary<string, HighLevelIR.HLType> _typeCache;
   private readonly Dictionary<string, IRSymbol> _symbolTable;
   ```
2. Add private fields for sub-transformers:
   ```csharp
   private readonly ExpressionTransformer _expressionTransformer;
   private readonly StatementTransformer _statementTransformer;
   private readonly DeclarationTransformer _declarationTransformer;
   ```
3. Update constructor to initialize sub-transformers:
   ```csharp
   public PascalAstToHlirTransformer(string sourceFile = null)
   {
       _sourceFile = sourceFile ?? "<unknown>";
       _ir = new HighLevelIR { SourceFile = _sourceFile };
       _expressionTransformer = new ExpressionTransformer(_sourceFile, _typeCache, _symbolTable);
       _statementTransformer = new StatementTransformer(_sourceFile, _expressionTransformer, _typeCache, _symbolTable);
       _declarationTransformer = new DeclarationTransformer(_sourceFile, _expressionTransformer, _typeCache, _symbolTable, _currentFunctionScope);
   }
   ```
4. Keep only orchestration methods:
   - `Transform()` - line 67
   - `ProcessProgram()` - line 88
5. Keep helper methods:
   - `GetOrCreateBasicType()` - line 21
   - `CreateErrorStatement()` - line 37
   - `CreateErrorExpression()` - line 44
6. Delegate all transformation methods to appropriate sub-transformers
7. Remove all moved methods

**Unit Testing (Integrated):**
- Verify main PascalAstToHlirTransformer can be instantiated with all sub-transformers
- Test Transform() method with various program structures
- Verify proper delegation to all sub-transformers
- Verify all transformer tests pass (currently 3 failing tests should now pass)
- Run full test suite after implementation
- Expected: All 26+ tests pass with no regressions

---

## Unit Testing Philosophy

**Unit testing is NOT a separate phase.** It is an integral part of every implementation step:

1. **During Development**: Write tests as you implement each visitor/transformer
2. **After Each Step**: Run the full test suite to verify no regressions
3. **Integration**: Test that delegation works correctly between components
4. **Continuous Validation**: Maintain test coverage throughout the refactoring process

**Testing Commands (Run After Each Step):**
```bash
# Quick verification
dotnet clean /home/kenneth/Projects/GameVM/GameVM.sln
dotnet build /home/kenneth/Projects/GameVM/GameVM.sln
dotnet test /home/kenneth/Projects/GameVM/src/GameVM.Compiler.Pascal.Tests/ --verbosity normal

# Full validation
dotnet test /home/kenneth/Projects/GameVM/ --verbosity normal
```

---

## Testing and Validation

### AFTER EACH CODE CHANGE - Run Full Test Suite

**Quick Verification (after each file modification):**
```bash
# 1. Clean and rebuild
dotnet clean /home/kenneth/Projects/GameVM/GameVM.sln
dotnet build /home/kenneth/Projects/GameVM/GameVM.sln

# 2. Run Pascal compiler tests
dotnet test /home/kenneth/Projects/GameVM/src/GameVM.Compiler.Pascal.Tests/ --verbosity normal

# 3. Run all project tests
dotnet test /home/kenneth/Projects/GameVM/ --verbosity normal
```

### Step 7.1: Compile the Solution

**After each code change, verify compilation:**
```bash
dotnet build /home/kenneth/Projects/GameVM/GameVM.sln
```

**Expected Result:**
- ✓ Build succeeds with no errors
- ✓ Only pre-existing warnings (if any) appear
- ✓ No new compilation errors introduced

**If build fails:**
- Check error messages for missing using statements
- Verify all moved methods are properly removed from source
- Ensure all dependencies are properly injected
- Check for typos in class/method names

---

### Step 7.2: Run Pascal Compiler Unit Tests

**After each phase step, run Pascal-specific tests:**
```bash
dotnet test /home/kenneth/Projects/GameVM/src/GameVM.Compiler.Pascal.Tests/ --verbosity normal
```

**Expected Results:**
- ✓ All existing tests pass
- ✓ No test failures or skipped tests
- ✓ Test output shows green checkmarks

**If tests fail:**
- Review the test failure message for details
- Check if the failure is related to your changes
- Verify the AST structure hasn't changed
- Run tests with `--verbosity detailed` for more information

---

### Step 7.3: Run Full Solution Test Suite

**After each phase completion, run all tests:**
```bash
dotnet test /home/kenneth/Projects/GameVM/ --verbosity normal --logger "console;verbosity=normal"
```

**Expected Results:**
- ✓ All tests pass across all projects
- ✓ No regressions in other compiler components
- ✓ Total test count remains the same or increases

**If tests fail in other projects:**
- Verify the failure is not caused by your changes
- Check if there are pre-existing failures
- Review the error message for context

---

### Step 7.4: Verify No Regressions

**Checklist after each phase:**
- [ ] All existing tests pass
- [ ] No new compilation errors
- [ ] No breaking changes to public APIs
- [ ] AST structure unchanged
- [ ] Visitor behavior unchanged
- [ ] No performance regressions

**Regression Testing Commands:**
```bash
# Run tests with detailed output
dotnet test /home/kenneth/Projects/GameVM/src/GameVM.Compiler.Pascal.Tests/ --verbosity detailed

# Run specific test class
dotnet test /home/kenneth/Projects/GameVM/src/GameVM.Compiler.Pascal.Tests/ --filter "ClassName"

# Run tests and generate coverage report
dotnet test /home/kenneth/Projects/GameVM/src/GameVM.Compiler.Pascal.Tests/ /p:CollectCoverage=true
```

---

### Testing Checklist for Each Phase

**After Each Step (4.1-4.6):**
```bash
dotnet build /home/kenneth/Projects/GameVM/GameVM.sln
dotnet test /home/kenneth/Projects/GameVM/src/GameVM.Compiler.Pascal.Tests/ --verbosity normal
```

**After Phase 4.7 (Integration Testing and Validation):**
```bash
dotnet clean /home/kenneth/Projects/GameVM/GameVM.sln
dotnet build /home/kenneth/Projects/GameVM/GameVM.sln
dotnet test /home/kenneth/Projects/GameVM/ --verbosity normal
```

**After Each Step (5.1-5.4):**
```bash
dotnet build /home/kenneth/Projects/GameVM/GameVM.sln
dotnet test /home/kenneth/Projects/GameVM/src/GameVM.Compiler.Pascal.Tests/ --verbosity normal
```

**Final Validation After Phase 5:**
```bash
dotnet clean /home/kenneth/Projects/GameVM/GameVM.sln
dotnet build /home/kenneth/Projects/GameVM/GameVM.sln
dotnet test /home/kenneth/Projects/GameVM/ --verbosity normal
```

---

## Implementation Order

### Phase 4: AST Visitor Refactoring (COMPLETE ✓)
1. ✓ **Phase 4.1**: Extract `ErrorNode` and `TypeIdentifierNode` (with integrated unit testing)
2. ✓ **Phase 4.2**: Create `ExpressionVisitor.cs` (with integrated unit testing)
3. ✓ **Phase 4.3**: Create `StatementVisitor.cs` (with integrated unit testing)
4. ✓ **Phase 4.4**: Create `DeclarationVisitor.cs` (with integrated unit testing)
5. ✓ **Phase 4.5**: Refactor main `ASTVisitor.cs` (with integrated unit testing)
6. ✓ **Phase 4.6**: Code review and optimization (with integrated unit testing)
7. ✓ **Phase 4.7**: Integration testing and validation (COMPLETE - All 26 tests passing)

### Phase 5: Transformer Refactoring (AFTER PHASE 4 COMPLETE)
8. **Phase 5.1**: Create `ExpressionTransformer.cs` (with integrated unit testing)
9. **Phase 5.2**: Create `StatementTransformer.cs` (with integrated unit testing)
10. **Phase 5.3**: Create `DeclarationTransformer.cs` (with integrated unit testing)
11. **Phase 5.4**: Refactor main `PascalAstToHlirTransformer.cs` (with integrated unit testing)
12. **Final validation and full test suite**

**Note**: Unit testing is integrated into every step, not treated as a separate phase.

---

## Reference: Current File Locations

- **Main files to refactor:**
  - `/home/kenneth/Projects/GameVM/src/GameVM.Compiler.Pascal/ASTVisitor.cs` (1,069 lines)
  - `/home/kenneth/Projects/GameVM/src/GameVM.Compiler.Pascal/PascalAstToHlirTransformer.cs` (686 lines)

- **Base classes:**
  - `/home/kenneth/Projects/GameVM/src/GameVM.Compiler.Pascal/PascalASTNode.cs`
  - ANTLR generated: `/home/kenneth/Projects/GameVM/src/GameVM.Compiler.Pascal/ANTLR/`

- **Test files:**
  - `/home/kenneth/Projects/GameVM/src/GameVM.Compiler.Pascal.Tests/`

---

## Notes

- After each file modification, refer back to this plan to verify all changes align with the implementation strategy
- Maintain consistent naming conventions and code style
- Ensure all dependencies are properly injected through constructors
- Keep shared state (type cache, symbol table) accessible to all transformers
- Document any new public methods or classes

## Phase 1: AST Node Refactoring - FULLY COMPLETE ✓

All AST node classes have been extracted from `PascalASTNode.cs` into individual files:
- ✓ `PascalASTNode.cs` now contains ONLY the base `PascalASTNode` class
- ✓ All 50+ node classes are in individual files
- ✓ Removed duplicate definitions (SetTypeNode, PointerTypeNode)
- ✓ Fixed type references (SetTypeNode.cs, TypeIdentifierNode.cs)
- ✓ Extracted remaining nodes: TypeNode, VariableDeclarationNode, LabelNode, GotoNode, VariantRecordNode, VariantCaseNode, PackedTypeNode, EnumeratedTypeNode, FunctionNode

## Duplicate Class Definitions - FIXED ✓

The following duplicate definitions have been resolved:
- ✓ Removed `SetTypeNode` from `PascalASTNode.cs` (kept in `SetTypeNode.cs`)
- ✓ Removed `PointerTypeNode` from `PascalASTNode.cs` (kept in `PointerTypeNode.cs`)
- ✓ Fixed `SetTypeNode.cs` to use `TypeNode` instead of `HLType`
- ✓ Removed duplicate `TypeName` property from `TypeIdentifierNode.cs`

## Phase 4 Refactoring Status: IN PROGRESS

**Completed:**
- ✓ Phase 4.1-4.5: All sub-visitors created and main ASTVisitor refactored
- ✓ All duplicate class definitions fixed
- ✓ Code compiles without new errors

**Next Steps:**
- ⏳ Phase 4.6: Code review and optimization
- ⏳ Phase 4.7: Unit tests for each visitor
- ✓ Phase 4.8: Integration testing
- ✓ Phase 5: Transformer Refactoring (COMPLETE)

## Phase 6: Future Improvements (Proposed)

### Objective
Further improve the compiler architecture based on `RefactoringPlan.md`.

### Phase 6.1: Context Object Creation
- [x] **Goal**: Create `TransformationContext` to hold shared state (`SymbolTable`, `TypeCache`, etc.) instead of passing multiple dictionary arguments.
- [x] **Refactoring**: Update all Visitors and Transformers to use `TransformationContext`.

### Phase 6.2: ASTBuilder Integration (COMPLETE)
- [x] **Goal**: Standardize AST creation using a Builder pattern.
- [x] Create ASTBuilder class
- [x] Refactor ExpressionVisitor to use ASTBuilder
- [x] Refactor StatementVisitor to use ASTBuilder
- [x] Refactor DeclarationVisitor to use ASTBuilder

### Phase 6.3: HLIR Builder
- **Goal**: Standardize HLIR creation.
