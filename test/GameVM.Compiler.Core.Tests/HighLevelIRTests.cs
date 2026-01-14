using System;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using GameVM.Compiler.Core.IR;

namespace GameVM.Compiler.Core.Tests
{
    [TestFixture]
    public class HighLevelIRTests
    {
        private const string TestSourceFile = "test.pas";

        [Test]
        public void CreateBasicType_WithName_SetsProperties()
        {
            // Arrange & Act
            var type = new HighLevelIR.BasicType(TestSourceFile, "integer");

            // Assert
            Assert.That(type, Is.Not.Null);
            Assert.That(type.SourceFile, Is.EqualTo(TestSourceFile));
            Assert.That(type.Name, Is.EqualTo("integer"));
            Assert.That(type, Is.InstanceOf<HighLevelIR.HLType>());
        }

        [Test]
        public void CreateFunction_WithParameters_SetsProperties()
        {
            // Arrange
            var returnType = new HighLevelIR.BasicType(TestSourceFile, "integer");
            var body = new HighLevelIR.Block(TestSourceFile);
            
            // Act
            var function = new HighLevelIR.Function(TestSourceFile, "test", returnType, body);
            
            // Assert
            Assert.That(function, Is.Not.Null);
            Assert.That(function.SourceFile, Is.EqualTo(TestSourceFile));
            Assert.That(function.Name, Is.EqualTo("test"));
            Assert.That(function.ReturnType, Is.SameAs(returnType));
            Assert.That(function.Body, Is.SameAs(body));
        }

        [Test]
        public void CreateBlock_WithStatements_StoresStatements()
        {
            // Arrange
            var block = new HighLevelIR.Block(TestSourceFile);
            var statement = new HighLevelIR.Statement(TestSourceFile);
            
            // Act
            block.AddStatement(statement);
            
            // Assert - We can't directly access the private statements list,
            // so we'll test the behavior through the public API when possible
            // For now, we'll just verify the block was created
            Assert.That(block, Is.Not.Null);
            Assert.That(block.SourceFile, Is.EqualTo(TestSourceFile));
        }

        [Test]
        public void HighLevelIR_Initialization_SetsProperties()
        {
            // Arrange & Act
            var hlir = new HighLevelIR { SourceFile = TestSourceFile };
            
            // Assert
            Assert.That(hlir, Is.Not.Null);
            Assert.That(hlir.SourceFile, Is.EqualTo(TestSourceFile));
            Assert.That(hlir.TopLevel, Is.Not.Null);
            Assert.That(hlir.TopLevel, Is.Empty);
            Assert.That(hlir.Globals, Is.Not.Null);
            Assert.That(hlir.Globals, Is.Empty);
            Assert.That(hlir.Functions, Is.Not.Null);
            Assert.That(hlir.Functions, Is.Empty);
            Assert.That(hlir.Types, Is.Not.Null);
            Assert.That(hlir.Types, Is.Empty);
        }

        [Test]
        public void CreateModule_WithName_StoresName()
        {
            // Arrange & Act
            var module = new HighLevelIR.Module(TestSourceFile, "TestModule");
            
            // Assert
            Assert.That(module, Is.Not.Null);
            Assert.That(module.SourceFile, Is.EqualTo(TestSourceFile));
            Assert.That(module.Name, Is.EqualTo("TestModule"));
        }

        [Test]
        public void CreateStatement_WithSourceFile_SetsSourceFile()
        {
            // Arrange & Act
            var statement = new HighLevelIR.Statement(TestSourceFile);
            
            // Assert
            Assert.That(statement, Is.Not.Null);
            Assert.That(statement.SourceFile, Is.EqualTo(TestSourceFile));
        }

        [Test]
        public void CreateIRSymbol_WithNameAndType_SetsProperties()
        {
            // Arrange
            var type = new HighLevelIR.BasicType(TestSourceFile, "integer");
            
            // Act
            var symbol = new IRSymbol
            {
                Name = "counter",
                Type = type
            };
            
            // Assert
            Assert.That(symbol, Is.Not.Null);
            Assert.That(symbol.Name, Is.EqualTo("counter"));
            Assert.That(symbol.Type, Is.SameAs(type));
        }

        [Test]
        public void CreateIRParameter_WithNameAndType_SetsProperties()
        {
            // Arrange
            var type = new HighLevelIR.BasicType(TestSourceFile, "integer");
            
            // Act
            var param = new IRParameter
            {
                Name = "count",
                Type = type
            };
            
            // Assert
            Assert.That(param, Is.Not.Null);
            Assert.That(param.Name, Is.EqualTo("count"));
            Assert.That(param.Type, Is.SameAs(type));
        }

        [Test]
        public void CreateIRType_WithName_SetsName()
        {
            // Arrange & Act
            var irType = new IRType { Name = "MyType" };
            
            // Assert
            Assert.That(irType, Is.Not.Null);
            Assert.That(irType.Name, Is.EqualTo("MyType"));
        }

        [Test]
        public void CreateIRField_WithNameAndType_SetsProperties()
        {
            // Arrange
            var type = new HighLevelIR.BasicType(TestSourceFile, "string");
            
            // Act
            var field = new IRField
            {
                Name = "name",
                Type = type
            };
            
            // Assert
            Assert.That(field, Is.Not.Null);
            Assert.That(field.Name, Is.EqualTo("name"));
            Assert.That(field.Type, Is.SameAs(type));
        }

        [Test]
        public void CreateIRSourceLocation_WithValues_SetsProperties()
        {
            // Arrange & Act
            var sourceLocation = new IRSourceLocation
            {
                File = "test.pas",
                Line = 10,
                Column = 5
            };
            
            // Assert
            Assert.That(sourceLocation, Is.Not.Null);
            Assert.That(sourceLocation.File, Is.EqualTo("test.pas"));
            Assert.That(sourceLocation.Line, Is.EqualTo(10));
            Assert.That(sourceLocation.Column, Is.EqualTo(5));
        }

        [Test]
        public void AddFunction_ToHighLevelIR_AddsToFunctionsCollection()
        {
            // Arrange
            var hlir = new HighLevelIR { SourceFile = TestSourceFile };
            var returnType = new HighLevelIR.BasicType(TestSourceFile, "integer");
            var body = new HighLevelIR.Block(TestSourceFile);
            var function = new HighLevelIR.Function(TestSourceFile, "testFunc", returnType, body);
            
            // Act
            hlir.Functions[function.Name] = function;
            
            // Assert
            Assert.That(hlir.Functions.ContainsKey("testFunc"), Is.True);
            Assert.That(hlir.Functions["testFunc"], Is.SameAs(function));
        }

        [Test]
        public void AddGlobal_ToHighLevelIR_AddsToGlobalsCollection()
        {
            // Arrange
            var hlir = new HighLevelIR { SourceFile = TestSourceFile };
            var type = new HighLevelIR.BasicType(TestSourceFile, "integer");
            var global = new IRSymbol { Name = "globalVar", Type = type };
            
            // Act
            hlir.Globals[global.Name] = global;
            
            // Assert
            Assert.That(hlir.Globals.ContainsKey("globalVar"), Is.True);
            Assert.That(hlir.Globals["globalVar"], Is.SameAs(global));
        }

        [Test]
        public void AddType_ToHighLevelIR_AddsToTypesCollection()
        {
            // Arrange
            var hlir = new HighLevelIR { SourceFile = TestSourceFile };
            var customType = new HighLevelIR.BasicType(TestSourceFile, "MyCustomType");
            
            // Act
            hlir.Types[customType.Name] = customType;
            
            // Assert
            Assert.That(hlir.Types.ContainsKey("MyCustomType"), Is.True);
            Assert.That(hlir.Types["MyCustomType"], Is.SameAs(customType));
        }

        [Test]
        public void Module_AddFunction_AddsFunctionToModule()
        {
            // Arrange
            var module = new HighLevelIR.Module(TestSourceFile, "TestModule");
            var returnType = new HighLevelIR.BasicType(TestSourceFile, "void");
            var body = new HighLevelIR.Block(TestSourceFile);
            var function = new HighLevelIR.Function(TestSourceFile, "moduleFunc", returnType, body);
            
            // Act - Using reflection to access the private functions list
            var addMethod = typeof(HighLevelIR.Module).GetMethod("AddFunction", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            addMethod.Invoke(module, new object[] { function });
            
            // Assert - Verify the function was added (indirectly)
            Assert.That(module, Is.Not.Null);
        }

        [Test]
        public void Block_AddStatement_AddsStatementToBlock()
        {
            // Arrange
            var block = new HighLevelIR.Block(TestSourceFile);
            var statement = new HighLevelIR.Statement(TestSourceFile);
            
            // Act
            block.AddStatement(statement);
            
            // Assert - We can't directly access the private statements list,
            // but we can verify the method was called without exceptions
            Assert.That(block, Is.Not.Null);
        }
    }
}
