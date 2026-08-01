using NUnit.Framework;
using GameVM.Compiler.Capabilities;
using GameVM.Compiler.Core;
using GameVM.Compiler.Core.Enums;
using GameVM.Compiler.Core.IR;
using System.Collections.Generic;
using System.Linq;

namespace UnitTests.Application
{
    [TestFixture]
    public class CapabilityValidatorServiceTests
    {
        [Test]
        public void Validate_ReturnsEmptyList_WhenNoViolations()
        {
            // Arrange
            var validatorService = new CapabilityValidatorService();
            var hlir = new HighLevelIR();
            
            // Act
            var result = validatorService.Validate(hlir, CapabilityLevel.L1, new List<string>());
            
            // Assert
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void Validate_ReturnsViolations_WhenModuleFunctionExceedsProfile()
        {
            // Arrange
            var validatorService = new CapabilityValidatorService();
            var hlir = new HighLevelIR();
            var module = new HlModule { Name = "TestModule" };
            var function = new HighLevelIR.Function 
            { 
                Name = "TestFunction",
                RequiredLevel = CapabilityLevel.L3 
            };
            module.Functions.Add(function);
            hlir.Modules.Add(module);
            
            // Act
            var result = validatorService.Validate(hlir, CapabilityLevel.L1, new List<string>());
            
            // Assert
            Assert.That(result, Is.Not.Empty);
            Assert.That(result.First(), Does.Contain("Function 'TestFunction' requires L3"));
        }

        [Test]
        public void Validate_ReturnsViolations_WhenGlobalFunctionExceedsProfile()
        {
            // Arrange
            var validatorService = new CapabilityValidatorService();
            var hlir = new HighLevelIR();
            var function = new HighLevelIR.Function 
            { 
                Name = "TestFunction",
                RequiredLevel = CapabilityLevel.L2 
            };
            hlir.GlobalFunctions.Add("TestFunction", function);
            
            // Act
            var result = validatorService.Validate(hlir, CapabilityLevel.L1, new List<string>());
            
            // Assert
            Assert.That(result, Is.Not.Empty);
            Assert.That(result.First(), Does.Contain("Function 'TestFunction' requires L2"));
        }

        [Test]
        public void Validate_AllowsFunction_WhenExtensionIsPresent()
        {
            // Arrange
            var validatorService = new CapabilityValidatorService();
            var hlir = new HighLevelIR();
            var hlModule = new HlModule { Name = "TestModule" };
            var function = new HighLevelIR.Function 
            { 
                Name = "TestFunction",
                RequiredLevel = CapabilityLevel.L5,
                RequiredExtensionId = "Ext.Sound.Music"
            };
            hlModule.Functions.Add(function);
            hlir.Modules.Add(hlModule);
            
            // Act - with extension present
            var result = validatorService.Validate(hlir, CapabilityLevel.L1, new List<string> { "Ext.Sound.Music" });
            
            // Assert
            Assert.That(result, Is.Empty, "Function requiring L5 with Ext.Sound.Music should be allowed when extension is present");
        }

        [Test]
        public void Validate_RejectsFunction_WhenExtensionIsMissing()
        {
            // Arrange
            var validatorService = new CapabilityValidatorService();
            var hlir = new HighLevelIR();
            var hlModule = new HlModule { Name = "TestModule" };
            var function = new HighLevelIR.Function 
            { 
                Name = "TestFunction",
                RequiredLevel = CapabilityLevel.L5,
                RequiredExtensionId = "Ext.Sound.Music"
            };
            hlModule.Functions.Add(function);
            hlir.Modules.Add(hlModule);
            
            // Act - without extension
            var result = validatorService.Validate(hlir, CapabilityLevel.L1, new List<string>());
            
            // Assert
            Assert.That(result, Is.Not.Empty);
            Assert.That(result.First(), Does.Contain("Function 'TestFunction' requires L5"));
        }

        [Test]
        public void Validate_ReturnsMultipleViolations_WhenMultipleFunctionsViolate()
        {
            // Arrange
            var validatorService = new CapabilityValidatorService();
            var hlir = new HighLevelIR();
            var hlModule = new HlModule { Name = "TestModule" };
            
            // Function requiring L2
            var func1 = new HighLevelIR.Function { Name = "Function1", RequiredLevel = CapabilityLevel.L2 };
            hlModule.Functions.Add(func1);
            
            // Function requiring L3
            var func2 = new HighLevelIR.Function { Name = "Function2", RequiredLevel = CapabilityLevel.L3 };
            hlModule.Functions.Add(func2);
            
            hlir.Modules.Add(hlModule);
            
            // Act
            var result = validatorService.Validate(hlir, CapabilityLevel.L1, new List<string>());
            
            // Assert
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result.First(), Does.Contain("Function1"));
            Assert.That(result.Last(), Does.Contain("Function2"));
        }
    }
}