using NUnit.Framework;
using GameVM.Compiler.Backend.Atari2600;
using GameVM.Compiler.Core.IR;
using System.Linq;

namespace GameVM.Compiler.Backend.Atari2600.Tests.Transformers;

/// <summary>
/// Tests for MLIR to LLIR transformation.
/// Validates register assignments, memory layout, control flow, function calls, and type coercion.
/// </summary>
[TestFixture]
public class MLIRToLLIRTransformerTests
{
    private MidToLowLevelTransformer _transformer;

    [SetUp]
    public void Setup()
    {
        _transformer = new MidToLowLevelTransformer();
    }

    #region Register Assignment Tests

    [Test]
    public void Transform_RegisterAssignment_AllocatesAccumulatorRegister()
    {
        // Arrange
        var mlir = CreateSimpleMLIR();
        var function = new MidLevelIR.MLFunction { Name = "main" };
        function.Instructions.Add(new MidLevelIR.MLAssign { Target = "x", Source = "42" });
        mlir.Modules[0].Functions.Add(function);

        // Act
        var result = _transformer.Transform(mlir);

        // Assert
        Assert.That(result.Instructions, Is.Not.Empty);
        var loadInstr = result.Instructions.OfType<LowLevelIR.LLLoad>().FirstOrDefault();
        Assert.That(loadInstr, Is.Not.Null);
        Assert.That(loadInstr.Register, Is.EqualTo("A"), "Should use accumulator register A");
        Assert.That(loadInstr.Value, Is.EqualTo("42"));
    }

    [Test]
    public void Transform_MultipleAssignments_UsesAccumulatorRegister()
    {
        // Arrange
        var mlir = CreateSimpleMLIR();
        var function = new MidLevelIR.MLFunction { Name = "main" };
        function.Instructions.Add(new MidLevelIR.MLAssign { Target = "x", Source = "10" });
        function.Instructions.Add(new MidLevelIR.MLAssign { Target = "y", Source = "20" });
        mlir.Modules[0].Functions.Add(function);

        // Act
        var result = _transformer.Transform(mlir);

        // Assert
        var loadInstructions = result.Instructions.OfType<LowLevelIR.LLLoad>().ToList();
        Assert.That(loadInstructions, Has.Count.EqualTo(2));
        Assert.That(loadInstructions.All(l => l.Register == "A"), Is.True, "All loads should use accumulator register A");
    }

    [Test]
    public void Transform_RegisterState_PreservedAcrossInstructions()
    {
        // Arrange
        var mlir = CreateSimpleMLIR();
        var function = new MidLevelIR.MLFunction { Name = "main" };
        function.Instructions.Add(new MidLevelIR.MLAssign { Target = "temp", Source = "5" });
        function.Instructions.Add(new MidLevelIR.MLAssign { Target = "result", Source = "temp" });
        
        // Add function to the first module (or create one if none exists)
        if (mlir.Modules.Count == 0)
        {
            mlir.Modules.Add(new MidLevelIR.MLModule { Name = "default" });
        }
        mlir.Modules[0].Functions.Add(function);

        // Act
        var result = _transformer.Transform(mlir);

        // Assert
        // Should generate: LDA 5, STA temp, LDA temp, STA result
        var instructions = result.Instructions.ToList();
        Assert.That(instructions.Count, Is.GreaterThanOrEqualTo(4));
        
        var firstLoad = instructions.OfType<LowLevelIR.LLLoad>().First();
        Assert.That(firstLoad.Register, Is.EqualTo("A"));
        Assert.That(firstLoad.Value, Is.EqualTo("5"));
    }

    #endregion

    #region Memory Layout Tests

    [Test]
    public void Transform_ZeroPageAddressing_UsesZeroPageAddresses()
    {
        // Arrange
        var mlir = CreateSimpleMLIR();
        var function = new MidLevelIR.MLFunction { Name = "main" };
        function.Instructions.Add(new MidLevelIR.MLAssign { Target = "$80", Source = "42" });
        mlir.Modules[0].Functions.Add(function);

        // Act
        var result = _transformer.Transform(mlir);

        // Assert
        var storeInstr = result.Instructions.OfType<LowLevelIR.LLStore>().FirstOrDefault();
        Assert.That(storeInstr, Is.Not.Null);
        Assert.That(storeInstr.Address, Is.EqualTo("$80"), "MYVAR should map to zero-page address $80");
        Assert.That(storeInstr.Address.StartsWith("$"), Is.True, "Address should be in hex format");
    }

    [Test]
    public void Transform_TIARegisterAddressing_MapsToCorrectTIAAddresses()
    {
        // Arrange
        var mlir = CreateSimpleMLIR();
        var function = new MidLevelIR.MLFunction { Name = "main" };
        function.Instructions.Add(new MidLevelIR.MLAssign { Target = "COLUBK", Source = "42" });
        mlir.Modules[0].Functions.Add(function);

        // Act
        var result = _transformer.Transform(mlir);

        // Assert
        var storeInstr = result.Instructions.OfType<LowLevelIR.LLStore>().FirstOrDefault();
        Assert.That(storeInstr, Is.Not.Null);
        Assert.That(storeInstr.Address, Is.EqualTo("$09"), "COLUBK should map to TIA register $09");
    }

    [Test]
    public void Transform_AbsoluteAddressing_PreservesAbsoluteAddresses()
    {
        // Arrange
        var mlir = CreateSimpleMLIR();
        var function = new MidLevelIR.MLFunction { Name = "main" };
        function.Instructions.Add(new MidLevelIR.MLAssign { Target = "$1000", Source = "42" });
        mlir.Modules[0].Functions.Add(function);

        // Act
        var result = _transformer.Transform(mlir);

        // Assert
        var storeInstr = result.Instructions.OfType<LowLevelIR.LLStore>().FirstOrDefault();
        Assert.That(storeInstr, Is.Not.Null);
        Assert.That(storeInstr.Address, Is.EqualTo("$1000"), "Absolute addresses should be preserved");
    }

    [Test]
    public void Transform_MemoryLayout_VariablesAssignedToZeroPage()
    {
        // Arrange
        var mlir = CreateSimpleMLIR();
        var function = new MidLevelIR.MLFunction { Name = "main" };
        function.Instructions.Add(new MidLevelIR.MLAssign { Target = "var1", Source = "1" });
        function.Instructions.Add(new MidLevelIR.MLAssign { Target = "var2", Source = "2" });
        mlir.Modules[0].Functions.Add(function);

        // Act
        var result = _transformer.Transform(mlir);

        // Assert
        var storeInstructions = result.Instructions.OfType<LowLevelIR.LLStore>().ToList();
        Assert.That(storeInstructions, Has.Count.EqualTo(2));
        // Both should map to zero-page (default $80 for unknown vars)
        Assert.That(storeInstructions.All(s => s.Address.StartsWith("$")), Is.True);
    }

    #endregion

    #region Control Flow Tests

    [Test]
    public void Transform_FunctionLabel_CreatesLabelInstruction()
    {
        // Arrange
        var mlir = CreateSimpleMLIR();
        var function = new MidLevelIR.MLFunction { Name = "myFunction" };
        function.Instructions.Add(new MidLevelIR.MLAssign { Target = "x", Source = "42" });
        mlir.Modules[0].Functions.Add(function);

        // Act
        var result = _transformer.Transform(mlir);

        // Assert
        var labelInstr = result.Instructions.OfType<LowLevelIR.LLLabel>().FirstOrDefault();
        Assert.That(labelInstr, Is.Not.Null);
        Assert.That(labelInstr.Name, Is.EqualTo("myFunction"), "Function name should become label");
    }

    [Test]
    public void Transform_MultipleFunctions_CreatesMultipleLabels()
    {
        // Arrange
        var mlir = CreateSimpleMLIR();
        mlir.Modules[0].Functions.Add(new MidLevelIR.MLFunction { Name = "func1" });
        mlir.Modules[0].Functions.Add(new MidLevelIR.MLFunction { Name = "func2" });
        mlir.Modules[0].Functions.Add(new MidLevelIR.MLFunction { Name = "main" });

        // Act
        var result = _transformer.Transform(mlir);

        // Assert
        var labels = result.Instructions.OfType<LowLevelIR.LLLabel>().ToList();
        Assert.That(labels, Has.Count.EqualTo(3));
        Assert.That(labels.Select(l => l.Name), Contains.Item("func1"));
        Assert.That(labels.Select(l => l.Name), Contains.Item("func2"));
        Assert.That(labels.Select(l => l.Name), Contains.Item("main"));
    }

    // Note: Control flow instructions (if/while/for/case) are not yet implemented in MidLevelIR
    // These tests will be added when MLBranch, MLIf, etc. are added to MidLevelIR

    #endregion

    #region Function Call Tests

    [Test]
    public void Transform_FunctionCall_GeneratesCallInstruction()
    {
        // Arrange
        var mlir = CreateSimpleMLIR();
        var function = new MidLevelIR.MLFunction { Name = "main" };
        function.Instructions.Add(new MidLevelIR.MLCall { Name = "myFunction" });
        mlir.Modules[0].Functions.Add(function);

        // Act
        var result = _transformer.Transform(mlir);

        // Assert
        var callInstr = result.Instructions.OfType<LowLevelIR.LLCall>().FirstOrDefault();
        Assert.That(callInstr, Is.Not.Null);
        Assert.That(callInstr.Label, Is.EqualTo("myFunction"), "Function call should map to label");
    }

    [Test]
    public void Transform_FunctionCallWithArguments_PreservesCallStructure()
    {
        // Arrange
        var mlir = CreateSimpleMLIR();
        var function = new MidLevelIR.MLFunction { Name = "main" };
        var call = new MidLevelIR.MLCall { Name = "Add" };
        call.Arguments.Add("5");
        call.Arguments.Add("3");
        function.Instructions.Add(call);
        mlir.Modules[0].Functions.Add(function);

        // Act
        var result = _transformer.Transform(mlir);

        // Assert
        var callInstr = result.Instructions.OfType<LowLevelIR.LLCall>().FirstOrDefault();
        Assert.That(callInstr, Is.Not.Null);
        Assert.That(callInstr.Label, Is.EqualTo("Add"));
        // Note: Argument handling would be implemented in the transformer when needed
    }

    [Test]
    public void Transform_NestedFunctionCalls_GeneratesCallSequence()
    {
        // Arrange
        var mlir = CreateSimpleMLIR();
        var function = new MidLevelIR.MLFunction { Name = "main" };
        function.Instructions.Add(new MidLevelIR.MLCall { Name = "func1" });
        function.Instructions.Add(new MidLevelIR.MLCall { Name = "func2" });
        mlir.Modules[0].Functions.Add(function);

        // Act
        var result = _transformer.Transform(mlir);

        // Assert
        var callInstructions = result.Instructions.OfType<LowLevelIR.LLCall>().ToList();
        Assert.That(callInstructions, Has.Count.EqualTo(2));
        Assert.That(callInstructions[0].Label, Is.EqualTo("func1"));
        Assert.That(callInstructions[1].Label, Is.EqualTo("func2"));
    }

    [Test]
    public void Transform_FunctionCallStackFrame_GeneratesCallBeforeLabel()
    {
        // Arrange
        var mlir = CreateSimpleMLIR();
        var mainFunc = new MidLevelIR.MLFunction { Name = "main" };
        mainFunc.Instructions.Add(new MidLevelIR.MLCall { Name = "Helper" });
        mlir.Modules[0].Functions.Add(mainFunc);
        mlir.Modules[0].Functions.Add(new MidLevelIR.MLFunction { Name = "Helper" });

        // Act
        var result = _transformer.Transform(mlir);

        // Assert
        var instructions = result.Instructions.ToList();
        var mainLabelIndex = instructions.FindIndex(i => i is LowLevelIR.LLLabel l && l.Name == "main");
        var callIndex = instructions.FindIndex(i => i is LowLevelIR.LLCall c && c.Label == "Helper");
        var helperLabelIndex = instructions.FindIndex(i => i is LowLevelIR.LLLabel l && l.Name == "Helper");
        
        Assert.That(mainLabelIndex, Is.GreaterThanOrEqualTo(0));
        Assert.That(callIndex, Is.GreaterThan(mainLabelIndex));
        Assert.That(helperLabelIndex, Is.GreaterThan(callIndex), "Helper function label should come after call");
    }

    #endregion

    #region Type Coercion Tests

    [Test]
    public void Transform_IntegerLiteral_PreservesAsString()
    {
        // Arrange
        var mlir = CreateSimpleMLIR();
        var function = new MidLevelIR.MLFunction { Name = "main" };
        function.Instructions.Add(new MidLevelIR.MLAssign { Target = "x", Source = "42" });
        mlir.Modules[0].Functions.Add(function);

        // Act
        var result = _transformer.Transform(mlir);

        // Assert
        var loadInstr = result.Instructions.OfType<LowLevelIR.LLLoad>().FirstOrDefault();
        Assert.That(loadInstr, Is.Not.Null);
        Assert.That(loadInstr.Value, Is.EqualTo("42"), "Integer literal should be preserved as string");
    }

    [Test]
    public void Transform_ByteValue_HandlesByteCoercion()
    {
        // Arrange
        var mlir = CreateSimpleMLIR();
        var function = new MidLevelIR.MLFunction { Name = "main" };
        // Simulating int→byte coercion: value should be within byte range
        function.Instructions.Add(new MidLevelIR.MLAssign { Target = "byteVar", Source = "255" });
        mlir.Modules[0].Functions.Add(function);

        // Act
        var result = _transformer.Transform(mlir);

        // Assert
        var loadInstr = result.Instructions.OfType<LowLevelIR.LLLoad>().FirstOrDefault();
        Assert.That(loadInstr, Is.Not.Null);
        Assert.That(loadInstr.Value, Is.EqualTo("255"));
        // Note: Actual byte coercion would be validated in type checking phase
    }

    [Test]
    public void Transform_ImplicitConversion_PreservesSourceValue()
    {
        // Arrange
        var mlir = CreateSimpleMLIR();
        var function = new MidLevelIR.MLFunction { Name = "main" };
        // Simulating implicit int→real conversion (value preserved, type handled elsewhere)
        function.Instructions.Add(new MidLevelIR.MLAssign { Target = "realVar", Source = "42" });
        mlir.Modules[0].Functions.Add(function);

        // Act
        var result = _transformer.Transform(mlir);

        // Assert
        var loadInstr = result.Instructions.OfType<LowLevelIR.LLLoad>().FirstOrDefault();
        Assert.That(loadInstr, Is.Not.Null);
        Assert.That(loadInstr.Value, Is.EqualTo("42"), "Implicit conversion should preserve source value");
    }

    #endregion

    #region Complex Transformation Tests

    [Test]
    public void Transform_MixedInstructions_GeneratesCorrectSequence()
    {
        // Arrange
        var mlir = CreateSimpleMLIR();
        var function = new MidLevelIR.MLFunction { Name = "main" };
        function.Instructions.Add(new MidLevelIR.MLAssign { Target = "x", Source = "10" });
        function.Instructions.Add(new MidLevelIR.MLAssign { Target = "y", Source = "20" });
        function.Instructions.Add(new MidLevelIR.MLAssign { Target = "z", Source = "x" });
        mlir.Modules[0].Functions.Add(function);

        // Act
        var result = _transformer.Transform(mlir);

        // Assert
        var instructions = result.Instructions.ToList();
        // Should have: label, load, store, load, store, load, store
        Assert.That(instructions.Count, Is.GreaterThanOrEqualTo(7));
        // Should have: label, load, store, call, load, store
        Assert.That(instructions.Count, Is.GreaterThanOrEqualTo(6));
        
        Assert.That(instructions[0], Is.TypeOf<LowLevelIR.LLLabel>());
        Assert.That(instructions[1], Is.TypeOf<LowLevelIR.LLLoad>());
        Assert.That(instructions[2], Is.TypeOf<LowLevelIR.LLStore>());
        Assert.That(instructions[3], Is.TypeOf<LowLevelIR.LLCall>());
        Assert.That(instructions[4], Is.TypeOf<LowLevelIR.LLLoad>());
        Assert.That(instructions[5], Is.TypeOf<LowLevelIR.LLStore>());
    }

    [Test]
    public void Transform_MultipleFunctions_TransformsAllFunctions()
    {
        // Arrange
        var mlir = CreateSimpleMLIR();
        var func1 = new MidLevelIR.MLFunction { Name = "func1" };
        func1.Instructions.Add(new MidLevelIR.MLAssign { Target = "x", Source = "1" });
        mlir.Modules[0].Functions.Add(func1);

        var func2 = new MidLevelIR.MLFunction { Name = "func2" };
        func2.Instructions.Add(new MidLevelIR.MLAssign { Target = "y", Source = "2" });
        mlir.Modules[0].Functions.Add(func2);

        // Act
        var result = _transformer.Transform(mlir);

        // Assert
        var labels = result.Instructions.OfType<LowLevelIR.LLLabel>().ToList();
        Assert.That(labels, Has.Count.EqualTo(2));
        
        var loads = result.Instructions.OfType<LowLevelIR.LLLoad>().ToList();
        Assert.That(loads, Has.Count.EqualTo(2));
    }

    #endregion

    #region Edge Cases

    [Test]
    public void Transform_EmptyFunction_ReturnsOnlyLabel()
    {
        // Arrange
        var mlir = CreateSimpleMLIR();
        var function = new MidLevelIR.MLFunction { Name = "empty" };
        mlir.Modules[0].Functions.Add(function);

        // Act
        var result = _transformer.Transform(mlir);

        // Assert
        Assert.That(result.Instructions, Has.Count.EqualTo(1));
        Assert.That(result.Instructions[0], Is.TypeOf<LowLevelIR.LLLabel>());
        Assert.That(((LowLevelIR.LLLabel)result.Instructions[0]).Name, Is.EqualTo("empty"));
    }

    [Test]
    public void Transform_EmptyMLIR_ReturnsEmptyResult()
    {
        // Arrange
        var mlir = CreateSimpleMLIR();

        // Act
        var result = _transformer.Transform(mlir);

        // Assert
        Assert.That(result.Instructions, Is.Empty);
    }

    #endregion

    #region Helper Methods

    private MidLevelIR CreateSimpleMLIR()
    {
        var mlir = new MidLevelIR { SourceFile = "test.ir" };
        var module = new MidLevelIR.MLModule { Name = "test" };
        mlir.Modules.Add(module);
        return mlir;
    }

    #endregion
}

