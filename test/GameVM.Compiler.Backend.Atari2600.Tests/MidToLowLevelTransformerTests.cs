using NUnit.Framework;
using GameVM.Compiler.Backend.Atari2600;
using GameVM.Compiler.Core.IR;

namespace GameVM.Compiler.Backend.Atari2600.Tests;

[TestFixture]
public class MidToLowLevelTransformerTests
{
    private MidToLowLevelTransformer _transformer;

    [SetUp]
    public void Setup()
    {
        _transformer = new MidToLowLevelTransformer();
    }

    #region Assignment Transformation Tests

    [Test]
    public void Transform_SimpleAssignment_GeneratesLoadAndStore()
    {
        // Arrange
        var mlir = new MidLevelIR();
        var module = new MidLevelIR.MLModule { Name = "test" };
        var function = new MidLevelIR.MLFunction { Name = "main" };
        function.Instructions.Add(new MidLevelIR.MLAssign
        {
            Target = "MyVar",
            Source = "42"
        });
        module.Functions.Add(function);
        mlir.Modules.Add(module);

        // Act
        var result = _transformer.Transform(mlir);

        // Assert
        Assert.That(result.Modules, Has.Count.EqualTo(1));
        var resultModule = result.Modules[0];
        Assert.That(resultModule.Functions, Has.Count.EqualTo(1));
        var resultFunction = resultModule.Functions[0];
        Assert.That(resultFunction.Instructions, Has.Count.EqualTo(2));
        
        var loadInstr = resultFunction.Instructions[0] as LowLevelIR.LLLoad;
        Assert.That(loadInstr, Is.Not.Null);
        Assert.That(loadInstr.Register, Is.EqualTo("A"));
        Assert.That(loadInstr.Value, Is.EqualTo("42"));
        
        var storeInstr = resultFunction.Instructions[1] as LowLevelIR.LLStore;
        Assert.That(storeInstr, Is.Not.Null);
        Assert.That(storeInstr.Register, Is.EqualTo("A"));
        Assert.That(storeInstr.Address, Is.EqualTo("$80")); // MyVar maps to $80
    }

    [Test]
    public void Transform_TIARegisterAssignment_MapsToCorrectAddress()
    {
        // Arrange
        var mlir = new MidLevelIR();
        var module = new MidLevelIR.MLModule { Name = "test" };
        var function = new MidLevelIR.MLFunction { Name = "main" };
        function.Instructions.Add(new MidLevelIR.MLAssign
        {
            Target = "COLUBK",
            Source = "10"
        });
        module.Functions.Add(function);
        mlir.Modules.Add(module);

        // Act
        var result = _transformer.Transform(mlir);

        // Assert
        var resultFunction = result.Modules[0].Functions[0];
        var storeInstr = resultFunction.Instructions[1] as LowLevelIR.LLStore;
        Assert.That(storeInstr, Is.Not.Null);
        Assert.That(storeInstr.Address, Is.EqualTo("$09")); // COLUBK maps to $09
    }

    [Test]
    public void Transform_COLUPF_Assignment_MapsToCorrectAddress()
    {
        // Arrange
        var mlir = new MidLevelIR();
        var module = new MidLevelIR.MLModule { Name = "test" };
        var function = new MidLevelIR.MLFunction { Name = "main" };
        function.Instructions.Add(new MidLevelIR.MLAssign
        {
            Target = "COLUPF",
            Source = "255"
        });
        module.Functions.Add(function);
        mlir.Modules.Add(module);

        // Act
        var result = _transformer.Transform(mlir);

        // Assert
        var resultFunction = result.Modules[0].Functions[0];
        var storeInstr = resultFunction.Instructions[1] as LowLevelIR.LLStore;
        Assert.That(storeInstr, Is.Not.Null);
        Assert.That(storeInstr.Address, Is.EqualTo("$08")); // COLUPF maps to $08
    }

    [Test]
    public void Transform_COLUP0_Assignment_MapsToCorrectAddress()
    {
        // Arrange
        var mlir = new MidLevelIR();
        var module = new MidLevelIR.MLModule { Name = "test" };
        var function = new MidLevelIR.MLFunction { Name = "main" };
        function.Instructions.Add(new MidLevelIR.MLAssign
        {
            Target = "COLUP0",
            Source = "128"
        });
        module.Functions.Add(function);
        mlir.Modules.Add(module);

        // Act
        var result = _transformer.Transform(mlir);

        // Assert
        var resultFunction = result.Modules[0].Functions[0];
        var storeInstr = resultFunction.Instructions[1] as LowLevelIR.LLStore;
        Assert.That(storeInstr, Is.Not.Null);
        Assert.That(storeInstr.Address, Is.EqualTo("$06")); // COLUP0 maps to $06
    }

    [Test]
    public void Transform_UnknownVariable_MapsToDefaultAddress()
    {
        // Arrange
        var mlir = new MidLevelIR();
        var module = new MidLevelIR.MLModule { Name = "test" };
        var function = new MidLevelIR.MLFunction { Name = "main" };
        function.Instructions.Add(new MidLevelIR.MLAssign
        {
            Target = "UnknownVar",
            Source = "99"
        });
        module.Functions.Add(function);
        mlir.Modules.Add(module);

        // Act
        var result = _transformer.Transform(mlir);

        // Assert
        var resultFunction = result.Modules[0].Functions[0];
        var storeInstr = resultFunction.Instructions[1] as LowLevelIR.LLStore;
        Assert.That(storeInstr, Is.Not.Null);
        Assert.That(storeInstr.Address, Is.EqualTo("$80")); // Default address
    }

    [Test]
    public void Transform_ExplicitAddress_PreservesAddress()
    {
        // Arrange
        var mlir = new MidLevelIR();
        var module = new MidLevelIR.MLModule { Name = "test" };
        var function = new MidLevelIR.MLFunction { Name = "main" };
        function.Instructions.Add(new MidLevelIR.MLAssign
        {
            Target = "$FF",
            Source = "42"
        });
        module.Functions.Add(function);
        mlir.Modules.Add(module);

        // Act
        var result = _transformer.Transform(mlir);

        // Assert
        var resultFunction = result.Modules[0].Functions[0];
        var storeInstr = resultFunction.Instructions[1] as LowLevelIR.LLStore;
        Assert.That(storeInstr, Is.Not.Null);
        Assert.That(storeInstr.Address, Is.EqualTo("$FF"));
    }

    #endregion

    #region Function Call Transformation Tests

    [Test]
    public void Transform_FunctionCall_GeneratesCallInstruction()
    {
        // Arrange
        var mlir = new MidLevelIR();
        var module = new MidLevelIR.MLModule { Name = "test" };
        var function = new MidLevelIR.MLFunction { Name = "main" };
        function.Instructions.Add(new MidLevelIR.MLCall
        {
            Name = "InitGame"
        });
        module.Functions.Add(function);
        mlir.Modules.Add(module);

        // Act
        var result = _transformer.Transform(mlir);

        // Assert
        var resultFunction = result.Modules[0].Functions[0];
        Assert.That(resultFunction.Instructions, Has.Count.EqualTo(1));
        
        var callInstr = resultFunction.Instructions[0] as LowLevelIR.LLCall;
        Assert.That(callInstr, Is.Not.Null);
        Assert.That(callInstr.Label, Is.EqualTo("InitGame"));
    }

    #endregion

    #region Multiple Instruction Tests

    [Test]
    public void Transform_MultipleAssignments_GeneratesCorrectSequence()
    {
        // Arrange
        var mlir = new MidLevelIR();
        var module = new MidLevelIR.MLModule { Name = "test" };
        var function = new MidLevelIR.MLFunction { Name = "main" };
        function.Instructions.Add(new MidLevelIR.MLAssign { Target = "COLUBK", Source = "10" });
        function.Instructions.Add(new MidLevelIR.MLAssign { Target = "MyVar", Source = "42" });
        module.Functions.Add(function);
        mlir.Modules.Add(module);

        // Act
        var result = _transformer.Transform(mlir);

        // Assert
        var resultFunction = result.Modules[0].Functions[0];
        Assert.That(resultFunction.Instructions, Has.Count.EqualTo(4)); // 2 loads + 2 stores
        
        // First assignment
        Assert.That(resultFunction.Instructions[0], Is.TypeOf<LowLevelIR.LLLoad>());
        Assert.That(resultFunction.Instructions[1], Is.TypeOf<LowLevelIR.LLStore>());
        
        // Second assignment
        Assert.That(resultFunction.Instructions[2], Is.TypeOf<LowLevelIR.LLLoad>());
        Assert.That(resultFunction.Instructions[3], Is.TypeOf<LowLevelIR.LLStore>());
    }

    [Test]
    public void Transform_MixedInstructions_GeneratesCorrectSequence()
    {
        // Arrange
        var mlir = new MidLevelIR();
        var module = new MidLevelIR.MLModule { Name = "test" };
        var function = new MidLevelIR.MLFunction { Name = "main" };
        function.Instructions.Add(new MidLevelIR.MLAssign { Target = "COLUBK", Source = "10" });
        function.Instructions.Add(new MidLevelIR.MLCall { Name = "InitGame" });
        function.Instructions.Add(new MidLevelIR.MLAssign { Target = "MyVar", Source = "42" });
        module.Functions.Add(function);
        mlir.Modules.Add(module);

        // Act
        var result = _transformer.Transform(mlir);

        // Assert
        var resultFunction = result.Modules[0].Functions[0];
        Assert.That(resultFunction.Instructions, Has.Count.EqualTo(5));
        
        Assert.That(resultFunction.Instructions[0], Is.TypeOf<LowLevelIR.LLLoad>());
        Assert.That(resultFunction.Instructions[1], Is.TypeOf<LowLevelIR.LLStore>());
        Assert.That(resultFunction.Instructions[2], Is.TypeOf<LowLevelIR.LLCall>());
        Assert.That(resultFunction.Instructions[3], Is.TypeOf<LowLevelIR.LLLoad>());
        Assert.That(resultFunction.Instructions[4], Is.TypeOf<LowLevelIR.LLStore>());
    }

    #endregion

    #region Edge Cases

    [Test]
    public void Transform_EmptyModule_ReturnsEmptyResult()
    {
        // Arrange
        var mlir = new MidLevelIR();

        // Act
        var result = _transformer.Transform(mlir);

        // Assert
        Assert.That(result.Modules, Is.Empty);
    }

    [Test]
    public void Transform_EmptyFunction_ReturnsEmptyInstructions()
    {
        // Arrange
        var mlir = new MidLevelIR();
        var module = new MidLevelIR.MLModule { Name = "test" };
        var function = new MidLevelIR.MLFunction { Name = "main" };
        module.Functions.Add(function);
        mlir.Modules.Add(module);

        // Act
        var result = _transformer.Transform(mlir);

        // Assert
        var resultFunction = result.Modules[0].Functions[0];
        Assert.That(resultFunction.Instructions, Is.Empty);
    }

    #endregion
}
