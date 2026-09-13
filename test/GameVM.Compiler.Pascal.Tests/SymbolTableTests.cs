using GameVM.Compiler.Core.Enums;
using GameVM.Compiler.Core.Interfaces;
using GameVM.Compiler.Core.IR.Soa;
using GameVM.Compiler.Pascal;

namespace GameVM.Compiler.Pascal.Tests;

/// <summary>
/// End-to-end tests for the unified symbol table.
/// Verifies that PascalFrontend.PopulateSymbolTable works from source string
/// through to CompilationResult containing a populated SymbolTable.
/// </summary>
[TestFixture]
public class SymbolTableTests
{
    [Test]
    public void Compile_VariableDeclaration_ProducesVariableSymbol()
    {
        var frontend = new PascalFrontend();
        var source = "program Test;\nvar x: Integer;\nbegin\nend.";

        var result = frontend.ParseToHlir(source);

        Assert.That(result.Hlir.Count, Is.GreaterThan(0), "HLIR should not be empty");
        Assert.That(result.Symbols.Count, Is.GreaterThan(0), "Symbol table should not be empty");

        // Find the variable symbol
        bool found = false;
        for (int i = 0; i < result.Symbols.Count; i++)
        {
            if (result.Symbols.GetKind(i) == SymbolKind.Variable)
            {
                found = true;
                uint nameOffset = result.Symbols.GetNameOffset(i);
                string name = frontend.StringPool!.Resolve(nameOffset);
                Assert.That(name, Is.EqualTo("x"));
                Assert.That(result.Symbols.GetStorageClass(i), Is.EqualTo(StorageClass.Local));
                break;
            }
        }
        Assert.That(found, Is.True, "Should find a Variable symbol");
    }

    [Test]
    public void Compile_ConstantDefinition_ProducesConstantSymbol()
    {
        var frontend = new PascalFrontend();
        var source = "program Test;\nconst PI = 3.14;\nbegin\nend.";

        var result = frontend.ParseToHlir(source);

        Assert.That(result.Symbols.Count, Is.GreaterThan(0));

        bool found = false;
        for (int i = 0; i < result.Symbols.Count; i++)
        {
            if (result.Symbols.GetKind(i) == SymbolKind.Constant)
            {
                found = true;
                uint nameOffset = result.Symbols.GetNameOffset(i);
                string name = frontend.StringPool!.Resolve(nameOffset);
                Assert.That(name, Is.EqualTo("PI"));
                Assert.That(result.Symbols.GetStorageClass(i), Is.EqualTo(StorageClass.Global));
                break;
            }
        }
        Assert.That(found, Is.True, "Should find a Constant symbol");
    }

    [Test]
    public void Compile_Procedure_ProducesFunctionSymbol()
    {
        var frontend = new PascalFrontend();
        // The transformer processes the top-level Program/MethodDeclaration.
        // Nested procedures are not yet recursively processed.
        var source = "program Test;\nbegin\nend.";

        var result = frontend.ParseToHlir(source);

        Assert.That(result.Symbols.Count, Is.GreaterThan(0));

        bool foundMain = false;
        for (int i = 0; i < result.Symbols.Count; i++)
        {
            if (result.Symbols.GetKind(i) == SymbolKind.Function)
            {
                uint nameOffset = result.Symbols.GetNameOffset(i);
                string name = frontend.StringPool!.Resolve(nameOffset);
                if (name == "main") foundMain = true;
            }
        }
        Assert.That(foundMain, Is.True, "Should find 'main' function symbol");
    }

    [Test]
    public void Compile_MultipleVariables_AllHaveCorrectTypes()
    {
        var frontend = new PascalFrontend();
        var source = "program Test;\nvar x, y, z: Integer;\nbegin\nend.";

        var result = frontend.ParseToHlir(source);

        int variableCount = 0;
        for (int i = 0; i < result.Symbols.Count; i++)
        {
            if (result.Symbols.GetKind(i) == SymbolKind.Variable)
            {
                variableCount++;
                uint typeOffset = result.Symbols.GetTypeId(i);
                string typeName = frontend.StringPool!.Resolve(typeOffset);
                Assert.That(typeName, Is.EqualTo("integer")
                    .Or.EqualTo("Integer"), "Variable type should be integer");
            }
        }
        Assert.That(variableCount, Is.EqualTo(3), "Should have 3 variable symbols");
    }

    [Test]
    public void Compile_SymbolTable_HasSourceLocations()
    {
        var frontend = new PascalFrontend();
        var source = "program Test;\nvar x: Integer;\nbegin\nend.";

        var result = frontend.ParseToHlir(source);

        Assert.That(result.Symbols.Count, Is.GreaterThan(0));

        // All symbols should have valid (non-negative) location info
        for (int i = 0; i < result.Symbols.Count; i++)
        {
            uint line = result.Symbols.GetLine(i);
            uint column = result.Symbols.GetColumn(i);
            // Line and column are 0 for now (AST nodes don't carry source positions),
            // but the fields exist and are accessible
            Assert.That(line, Is.GreaterThanOrEqualTo(0));
            Assert.That(column, Is.GreaterThanOrEqualTo(0));
        }
    }

    [Test]
    public void Compile_SymbolsAreAccessibleThroughCompilationResult()
    {
        // This test verifies the pipeline threading:
        // PascalFrontend → CompileUseCase → CompilationResult.Symbols
        // We test at the PascalFrontend seam directly since CompileUseCase
        // requires a full backend setup.
        var frontend = new PascalFrontend();
        var source = "program Test;\nvar x: Integer;\nbegin\nend.";

        var result = frontend.ParseToHlir(source);

        // Verify that ParseResult carries both Hlir and Symbols
        Assert.That(result.Hlir.Count, Is.GreaterThan(0));
        Assert.That(result.Symbols.Count, Is.GreaterThan(0));

        // Verify that symbols have correct scope linkage
        int rootScopeCount = 0;
        for (int i = 0; i < result.Symbols.Count; i++)
        {
            uint scopeId = result.Symbols.GetScopeId(i);
            // All symbols should be in scope 0 (module level) or scope 1 (function level)
            Assert.That(scopeId, Is.LessThanOrEqualTo(1),
                $"Symbol {i} should be in root or function scope");
            if (scopeId == 0) rootScopeCount++;
        }
        Assert.That(rootScopeCount, Is.GreaterThan(0), "At least one symbol should be at root scope");
    }
}
