using NUnit.Framework;
using GameVM.Compiler.Core.IR;

namespace GameVM.Compiler.Core.Tests.IR
{
    [TestFixture]
    public class LowLevelIRTests
    {
        #region Constructor and Property Tests

        [Test]
        public void LowLevelIR_DefaultConstructor_SetsDefaultValues()
        {
            // Arrange & Act
            var lowLevelIR = new LowLevelIR();

            // Assert
            Assert.That(lowLevelIR.SourceFile, Is.EqualTo(string.Empty));
            Assert.That(lowLevelIR.Modules, Is.Not.Null);
            Assert.That(lowLevelIR.Modules, Is.Empty);
            Assert.That(lowLevelIR.Instructions, Is.Not.Null);
            Assert.That(lowLevelIR.Instructions, Is.Empty);
        }

        [Test]
        public void LowLevelIR_CanSetSourceFile()
        {
            // Arrange
            var lowLevelIR = new LowLevelIR();

            // Act
            lowLevelIR.SourceFile = "test.pas";

            // Assert
            Assert.That(lowLevelIR.SourceFile, Is.EqualTo("test.pas"));
        }

        [Test]
        public void LowLevelIR_CanInitializeModules()
        {
            // Arrange
            var lowLevelIR = new LowLevelIR();
            var module = new LowLevelIR.LLModule { Name = "test_module" };

            // Act
            lowLevelIR.Modules.Add(module);

            // Assert
            Assert.That(lowLevelIR.Modules, Has.Count.EqualTo(1));
            Assert.That(lowLevelIR.Modules[0].Name, Is.EqualTo("test_module"));
        }

        [Test]
        public void LowLevelIR_CanInitializeInstructions()
        {
            // Arrange
            var lowLevelIR = new LowLevelIR();
            var instruction = new LowLevelIR.LLLabel { Name = "test_label" };

            // Act
            lowLevelIR.Instructions.Add(instruction);

            // Assert
            Assert.That(lowLevelIR.Instructions, Has.Count.EqualTo(1));
            Assert.That(lowLevelIR.Instructions[0], Is.InstanceOf<LowLevelIR.LLLabel>());
            var label = (LowLevelIR.LLLabel)lowLevelIR.Instructions[0];
            Assert.That(label.Name, Is.EqualTo("test_label"));
        }

        #endregion

        #region LLModule Tests

        [Test]
        public void LLModule_DefaultConstructor_SetsDefaultValues()
        {
            // Arrange & Act
            var module = new LowLevelIR.LLModule();

            // Assert
            Assert.That(module.Name, Is.EqualTo(string.Empty));
            Assert.That(module.Functions, Is.Not.Null);
            Assert.That(module.Functions, Is.Empty);
        }

        [Test]
        public void LLModule_CanSetProperties()
        {
            // Arrange
            var module = new LowLevelIR.LLModule();
            var function = new LowLevelIR.LLFunction { Name = "test_function" };

            // Act
            module.Name = "test_module";
            module.Functions.Add(function);

            // Assert
            Assert.That(module.Name, Is.EqualTo("test_module"));
            Assert.That(module.Functions, Has.Count.EqualTo(1));
            Assert.That(module.Functions[0].Name, Is.EqualTo("test_function"));
        }

        #endregion

        #region LLFunction Tests

        [Test]
        public void LLFunction_DefaultConstructor_SetsDefaultValues()
        {
            // Arrange & Act
            var function = new LowLevelIR.LLFunction();

            // Assert
            Assert.That(function.Name, Is.EqualTo(string.Empty));
            Assert.That(function.Instructions, Is.Not.Null);
            Assert.That(function.Instructions, Is.Empty);
        }

        [Test]
        public void LLFunction_CanSetProperties()
        {
            // Arrange
            var function = new LowLevelIR.LLFunction();
            var instruction = new LowLevelIR.LLLabel { Name = "test_label" };

            // Act
            function.Name = "test_function";
            function.Instructions.Add(instruction);

            // Assert
            Assert.That(function.Name, Is.EqualTo("test_function"));
            Assert.That(function.Instructions, Has.Count.EqualTo(1));
            Assert.That(function.Instructions[0], Is.InstanceOf<LowLevelIR.LLLabel>());
        }

        #endregion

        #region LLInstruction Tests

        [Test]
        public void LLInstruction_AbstractClass_HasDefaultOpCode()
        {
            // Arrange & Act
            var instruction = new TestLLInstruction();

            // Assert
            Assert.That(instruction.OpCode, Is.EqualTo("TestLLInstruction"));
        }

        #endregion

        #region LLLoad Tests

        [Test]
        public void LLLoad_DefaultConstructor_SetsDefaultValues()
        {
            // Arrange & Act
            var load = new LowLevelIR.LLLoad();

            // Assert
            Assert.That(load.Register, Is.EqualTo(string.Empty));
            Assert.That(load.Value, Is.EqualTo(string.Empty));
            Assert.That(load.OpCode, Is.EqualTo("LLLoad"));
        }

        [Test]
        public void LLLoad_CanSetProperties()
        {
            // Arrange
            var load = new LowLevelIR.LLLoad();

            // Act
            load.Register = "R1";
            load.Value = "42";

            // Assert
            Assert.That(load.Register, Is.EqualTo("R1"));
            Assert.That(load.Value, Is.EqualTo("42"));
        }

        #endregion

        #region LLStore Tests

        [Test]
        public void LLStore_DefaultConstructor_SetsDefaultValues()
        {
            // Arrange & Act
            var store = new LowLevelIR.LLStore();

            // Assert
            Assert.That(store.Address, Is.EqualTo(string.Empty));
            Assert.That(store.Register, Is.EqualTo(string.Empty));
            Assert.That(store.OpCode, Is.EqualTo("LLStore"));
        }

        [Test]
        public void LLStore_CanSetProperties()
        {
            // Arrange
            var store = new LowLevelIR.LLStore();

            // Act
            store.Address = "0x1000";
            store.Register = "R1";

            // Assert
            Assert.That(store.Address, Is.EqualTo("0x1000"));
            Assert.That(store.Register, Is.EqualTo("R1"));
        }

        #endregion

        #region LLCall Tests

        [Test]
        public void LLCall_DefaultConstructor_SetsDefaultValues()
        {
            // Arrange & Act
            var call = new LowLevelIR.LLCall();

            // Assert
            Assert.That(call.Label, Is.EqualTo(string.Empty));
            Assert.That(call.OpCode, Is.EqualTo("LLCall"));
        }

        [Test]
        public void LLCall_CanSetLabel()
        {
            // Arrange
            var call = new LowLevelIR.LLCall();

            // Act
            call.Label = "function_label";

            // Assert
            Assert.That(call.Label, Is.EqualTo("function_label"));
        }

        #endregion

        #region LLJump Tests

        [Test]
        public void LLJump_DefaultConstructor_SetsDefaultValues()
        {
            // Arrange & Act
            var jump = new LowLevelIR.LLJump();

            // Assert
            Assert.That(jump.Target, Is.EqualTo(string.Empty));
            Assert.That(jump.Condition, Is.Null);
            Assert.That(jump.OpCode, Is.EqualTo("LLJump"));
        }

        [Test]
        public void LLJump_CanSetProperties()
        {
            // Arrange
            var jump = new LowLevelIR.LLJump();

            // Act
            jump.Target = "label_target";
            jump.Condition = "R1 > 0";

            // Assert
            Assert.That(jump.Target, Is.EqualTo("label_target"));
            Assert.That(jump.Condition, Is.EqualTo("R1 > 0"));
        }

        [Test]
        public void LLJump_CanSetNullCondition()
        {
            // Arrange
            var jump = new LowLevelIR.LLJump { Condition = "test" };

            // Act
            jump.Condition = null;

            // Assert
            Assert.That(jump.Condition, Is.Null);
        }

        #endregion

        #region LLLabel Tests

        [Test]
        public void LLLabel_DefaultConstructor_SetsDefaultValues()
        {
            // Arrange & Act
            var label = new LowLevelIR.LLLabel();

            // Assert
            Assert.That(label.Name, Is.EqualTo(string.Empty));
            Assert.That(label.OpCode, Is.EqualTo("LLLabel"));
        }

        [Test]
        public void LLLabel_CanSetName()
        {
            // Arrange
            var label = new LowLevelIR.LLLabel();

            // Act
            label.Name = "test_label";

            // Assert
            Assert.That(label.Name, Is.EqualTo("test_label"));
        }

        #endregion

        #region Helper Test Class

        private class TestLLInstruction : LowLevelIR.LLInstruction
        {
            // Test implementation for abstract LLInstruction
        }

        #endregion
    }
}
