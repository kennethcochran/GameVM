using NUnit.Framework;
using Moq;
using GameVM.Compiler.Application;
using GameVM.Compiler.Application.Services;
using GameVM.Compiler.Core.IR;
using GameVM.Compiler.Core.IR.Interfaces;
using GameVM.Compiler.Core.IR.Buffers;
using Moq.AutoMock;
using GameVM.Compiler.Core.Interfaces;
using GameVM.Compiler.Core.Enums;
using System.Collections.Generic;
using GameVM.Compiler.Core.IR.Hlir;
using GameVM.Compiler.Core.IR.Soa;

namespace UnitTests.Application
{
    [TestFixture]
    public class CompileUseCaseTests
    {
        private AutoMocker _mocker = null!;
        private CompileUseCase _compileUseCase = null!;
        private string _tempFilePath = null!;

        [SetUp]
        public void Setup()
        {
            _mocker = new AutoMocker();
            _mocker.Use<_Frontend, MockFrontend>();
            _mocker.Use<IMidLevelOptimizer, MockMidLevelOptimizer>();
            _mocker.Use<ILowLevelOptimizer, MockLowLevelOptimizer>();
            _mocker.Use<IIRSlabTransformer, MockMlirToLlir>();
            _mocker.Use<ICodeGenerator, MockCodeGenerator>();
            _mocker.Use<ICapabilityProvider, MockCapabilityProvider>();
            _mocker.Use<ICapabilityValidatorService, MockCapabilityValidator>();
            _compileUseCase = _mocker.CreateInstance<CompileUseCase>();
        }

        [Test]
        public void Compile_ValidPascalProgram_ReturnsSuccess()
        {
            // Act
            var result = _compileUseCase.Compile("program test; begin end.", "pas", new CompilationOptions { Target = TargetPlatform.Atari2600 });

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.ErrorMessage, Is.Null.Or.Empty);
        }

        [Test]
        public void Compile_InvalidPascalProgram_ReturnsError()
        {
            // Act
            var result = _compileUseCase.Compile("program invalid; begin", "pas", new CompilationOptions { Target = TargetPlatform.Atari2600 });

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Is.Not.NullOrEmpty);
        }

        // Mock classes for testing
        private class MockFrontend : ILanguageFrontend
        {
            public ParseResult ParseToHlir(string sourceCode) => new(new HlirTree(new HlirProcedure(Array.Empty<HlirStatement>(), Array.Empty<HlirProcedure>(), new SymbolTable())), new SymbolTable());

            public IReadOnlyList<string>? LastParseErrors => null;

            public StringPool? StringPool => null;

            public IReadOnlyList<SemanticError>? SemanticErrors => null;
        }

        private class MockMidLevelOptimizer : IMidLevelOptimizer
        {
            public (bool Success, string[] Errors) Optimize(HlirTree program, CapabilityProfile capabilityProfile)
            {
                return (true, Array.Empty<string>());
            }
        }

        private class MockLowLevelOptimizer : ILowLevelOptimizer
        {
            public (bool Success, string[] Errors) Optimize(MidLevelRepresentation program, CapabilityProfile capabilityProfile)
            {
                return (true, Array.Empty<string>());
            }
        }

        private class MockMlirToLlir : IIRSlabTransformer
        {
            public (bool Success, string[] Errors) TransformSlab(MidLevelRepresentation slab, CapabilityProfile capabilityProfile)
            {
                return (true, Array.Empty<string>());
            }

            public (bool Success, string[] Errors) TransformSlab(MidLevelRepresentation slab, CapabilityProfile capabilityProfile, out MidLevelRepresentation result)
            {
                result = slab;
                return (true, Array.Empty<string>());
            }
        }

        private class MockCodeGenerator : ICodeGenerator
        {
            public CompilationResult Generate(HlirTree program, MidLevelRepresentation transformer, CompilationOptions options)
            {
                return new CompilationResult
                {
                    Success = true,
                    Code = Array.Empty<byte>(),
                    SourceFile = options.SourceFile,
                    Target = options.Target,
                    ErrorMessage = null
                };
            }
        }

        private class MockCapabilityProvider : ICapabilityProvider
        {
            public CapabilityProfile Profile => new();

            public (bool Success, string[] Errors) Validate(string source, string extension, CompilationOptions options)
            {
                return (true, Array.Empty<string>());
            }
        }

        private class MockCapabilityValidator : ICapabilityValidatorService
        {
            public (bool Success, string[] Errors) Validate(CapabilityProfile capabilityProfile)
            {
                return (true, Array.Empty<string>());
            }
        }
    }
}
