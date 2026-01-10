# Refactoring Plan: Pascal Compiler

**Goal:** To refactor the Pascal compiler into smaller, more manageable classes to improve maintainability, testability, and ease of use with AI coding assistants.

**Overall Strategy:** Break down the AST generation and AST to HLIR transformation into discrete, focused classes.

## I. Phase 1: AST Node Refactoring (Estimated time: 1 day)

*   **Objective:** Move each AST node class in `PascalASTNode.cs` into its own individual file.
*   **Tasks:**
    1.  [x] Create a new file for each AST node class (e.g., `ProgramNode.cs`, `VariableNode.cs`, `IfNode.cs`).
    2.  [x] Move the corresponding class definition into its new file.
    3.  [x] Update namespaces and using statements in each file as necessary.
    4.  [x] Remove all class definitions from `PascalASTNode.cs`, leaving only the base `PascalASTNode` class.
    5.  [x] Update all references to the AST node classes to use the new file locations (in ASTVisitor.cs and PascalAstToHlirTransformer.cs).
    6.  [ ] Compile and run existing tests to ensure that no functionality has been broken.
*   **Status:** Completed
*   **Completion Date:** TBD

## II. Phase 2: HLIR Builder Creation (Estimated time: 1 day)

*   **Objective:** Create the HLIRBuilder class with methods to generate HLIR objects in a consistent way.
*   **Tasks:**
    1.  Create a new directory named `Builders` inside the `GameVM.Compiler.Core` project.
    2.  Create a new file named `HLIRBuilder.cs` inside the `Builders` directory.
    3.  Implement methods within the `HLIRBuilder` class for creating HLIR elements. e.g. `CreateIntegerLiteralExpression`, `CreateVariableReferenceExpression`, `CreateAssignmentStatement` etc. These methods should take all the necessary parameters to define each HLIR element.
*   **Status:** Not Started
*   **Completion Date:** TBD

## III. Phase 3: Transformer Refactoring (Estimated time: 2 days)

*   **Objective:** Break down the `PascalAstToHlirTransformer` class into smaller, more focused transformers (`StatementTransformer`, `ExpressionTransformer`, `DeclarationTransformer`).
*   **Tasks:**
    1.  Create new files for each transformer class (e.g., `StatementTransformer.cs`, `ExpressionTransformer.cs`, `DeclarationTransformer.cs`) inside the `GameVM.Compiler.Pascal` directory.
    2.  Move the appropriate logic from `PascalAstToHlirTransformer` into each new transformer class.
    3.  Create the main `PascalAstToHlirTransformer` to orchestrate calls to the sub transformers. It should implement the `ILanguageFrontend` interface, and take the sub transformers as dependencies in the constructor.
    4.  Update references to the transformer classes and methods.
*   **Status:** Not Started
*   **Completion Date:** TBD

## IV. Phase 4: AST Visitor Refactoring (Estimated time: 2 days)

*   **Objective:** Break down the `ASTVisitor` class into smaller, more focused visitors (`StatementVisitor`, `ExpressionVisitor`, `DeclarationVisitor`).
*   **Tasks:**
    1.  Create new files for each visitor class (e.g., `StatementVisitor.cs`, `ExpressionVisitor.cs`, `DeclarationVisitor.cs`) inside the `GameVM.Compiler.Pascal` directory.
    2.  Move the appropriate logic from `ASTVisitor` into each new visitor class.
    3.  Update the main `ASTVisitor` class to orchestrate calls to the sub-visitors.
    4.  Update references to the visitor classes and methods.
*   **Status:** Not Started
*   **Completion Date:** TBD

## V. Phase 5: Context Object Creation (Estimated time: 1 day)

*   **Objective:** Create context objects to hold shared state during the AST visiting and HLIR transformation processes.
*   **Tasks:**
    1.  Create a new class named `TransformationContext` inside the `GameVM.Compiler.Pascal` directory.
    2.  Add properties to the `TransformationContext` class for:
        *   Symbol table
        *   Type cache
        *   Current function scope
        *   Error list
    3.  Modify the visitor and transformer classes to accept a `TransformationContext` object in their constructors and methods.
*   **Status:** Not Started
*   **Completion Date:** TBD

## VI. Phase 6: ASTBuilder Integration (Estimated time: 1 day)

*   **Objective:** Update the AST visitors to use the `ASTBuilder` to create AST nodes.
*   **Tasks:**
    1.  Create an `ASTBuilder` class in the `GameVM.Compiler.Pascal` directory.
    2.  Modify the visitor classes to use the `ASTBuilder` to create AST nodes.
    3.  Pass the `ASTBuilder` object to the visitor classes.
*   **Status:** Not Started
*   **Completion Date:** TBD

## VII. Phase 7: Testing and Validation (Estimated time: 1 day)

*   **Objective:** Compile and run existing unit tests to ensure that no functionality has been broken during the refactoring process.
*   **Tasks:**
    1.  Compile the solution.
    2.  Run all unit tests.
    3.  Fix any broken tests.
    4.  Add new unit tests as needed to cover the refactored code.
*   **Status:** Not Started
*   **Completion Date:** TBD

## VIII. Future phases:
*   Add Binary and Unary Expression processing.
*   Add Use statement resolution.
*   Continue implementing missing logic in the various processors.Pausing work on Pascal Compiler refactoring for the day. Will continue tomorrow.
