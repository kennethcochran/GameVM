using System;
using NUnit.Framework;
using GameVM.Compiler.Application;
using GameVM.Compiler.Application.Services;
using GameVM.Compiler.Core.IR.Interfaces;
using GameVM.Compiler.Core.Interfaces;
using GameVM.Compiler.Core.Enums;
using System.Collections.Generic;
using GameVM.Compiler.Core.IR.Hlir;
using GameVM.Compiler.Core.IR.Buffers;
using GameVM.Compiler.Core.IR.Soa;

namespace UnitTests.Application
{
    [TestFixture]
    public class CompileUseCaseTests
    {
        private CompileUseCase _compileUseCase = null!;

        [SetUp]
        public void Setup()
        {
            _compileUseCase = new CompileUseCase(
                new MockFrontend(),
                new MockMidLevelOptimizer(),
                new MockLowLevelOptimizer(),
                new MockMlirToLlir(),
                new MockCodeGenerator(),
                new MockCapabilityProvider(),
                new MockCapabilityValidator());
        }

        [Test]
        public void Compile_ValidPascalProgram_ReturnsSuccess()
        {
            var result = _compileUseCase.Execute("program test;\nvar x: Integer;\nbegin\n  x := 1;\nend.", ".pas", new CompilationOptions { Target = Architecture.Atari2600 });

            Assert.That(result.Success, Is.True);
            Assert.That(string.IsNullOrEmpty(result.ErrorMessage));
        }

        [Test]
        public void Compile_InvalidPascalProgram_ReturnsError()
        {
            var invalidFrontend = new MockFrontend(isInvalid: true);
            var useCase = new CompileUseCase(
                invalidFrontend,
                new MockMidLevelOptimizer(),
                new MockLowLevelOptimizer(),
                new MockMlirToLlir(),
                new MockCodeGenerator(),
                new MockCapabilityProvider(),
                new MockCapabilityValidator());
            var result = useCase.Execute("program invalid;\nbegin", ".pas", new CompilationOptions { Target = Architecture.Atari2600 });

            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Is.Not.Null);
        }

        private class MockFrontend : ILanguageFrontend
        {
            private readonly bool _isInvalid;

            public MockFrontend(bool isInvalid = false) => _isInvalid = isInvalid;

            public ParseResult ParseToHlir(string sourceCode)
            {
                if (_isInvalid) return new ParseResult(HlirTree.Empty, new SymbolTable());
                var builder = new HlirBuilder();
                builder.Add((byte)HlirNodeKind.Nop);
                return new ParseResult(builder.Build(), new SymbolTable());
            }
            public IReadOnlyList<string>? LastParseErrors => null;
            public StringPool? StringPool => new();
            public IReadOnlyList<SemanticError>? SemanticErrors => null;
        }

        private class MockMidLevelOptimizer : IMidLevelOptimizer
        {
            public InstList OptimizeSlab(InstList hlirSlab, StringPool stringPool, OptimizationLevel optimizationLevel)
            {
                return new InstList(
                    new byte[] { 0x80 },
                    new ushort[] { 0x0000 },
                    new ushort[] { 0x0000 },
                    new uint[] { 0x00000000 },
                    new uint[] { },
                    new uint[] { 0x00000000 },
                    new int[] { 0 },
                    1, 0);
            }
        }

        private class MockLowLevelOptimizer : ILowLevelOptimizer
        {
            public InstList OptimizeSlab(InstList llirSlab, StringPool stringPool, OptimizationLevel optimizationLevel)
            {
                return new InstList(
                    new byte[] { 0x47, 0x49, 0x4D, 0x4C, 1, 3, 0, 0 },
                    new ushort[] { 0x0000 },
                    new ushort[] { 0x0000 },
                    new uint[] { 0x00000000, 0x00000000, 0x00000000, 0x00000000 },
                    new uint[] { },
                    new uint[] { 0x00000000 },
                    new int[] { 0 },
                    1, 0);
            }
        }

        private class MockMlirToLlir : IIRSlabTransformer
        {
            public InstList TransformSlab(InstList inputSlab, StringPool stringPool)
            {
                return new InstList(
                    new byte[] { 0x47, 0x49, 0x4D, 0x4C, 1, 3, 0, 0 },
                    new ushort[] { 0x0000 },
                    new ushort[] { 0x0000 },
                    new uint[] { 0x00000000, 0x00000000, 0x00000000, 0x00000000 },
                    new uint[] { },
                    new uint[] { 0x00000000 },
                    new int[] { 0 },
                    1, 0);
            }
        }

        private class MockCodeGenerator : ICodeGenerator
        {
            public byte[] GenerateFromSlab(InstList llirSlab, StringPool stringPool, CodeGenOptions options)
            {
                return new byte[] { 1, 2, 3 };
            }
        }

        private class MockCapabilityProvider : ICapabilityProvider
        {
            public CapabilityProfile GetCapabilityProfile() => new() { BaseLevel = CapabilityLevel.L3 };
            public IEnumerable<string> GetSupportedExtensions() => new List<string>();
        }

        private class MockCapabilityValidator : ICapabilityValidatorService
        {
            public IEnumerable<string> Validate(uint[] hlirSlab, CapabilityLevel profile, List<string> systemExtensions) => new List<string>();
        }

        [Test]
        public void Compile_NullSource_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                _compileUseCase.Execute(null!, ".pas", new CompilationOptions { Target = Architecture.Atari2600 }));
        }

        [Test]
        public void Compile_EmptyHlir_ReturnsFailure()
        {
            var invalidFrontend = new MockFrontend(isInvalid: true);
            var useCase = new CompileUseCase(
                invalidFrontend,
                new MockMidLevelOptimizer(),
                new MockLowLevelOptimizer(),
                new MockMlirToLlir(),
                new MockCodeGenerator(),
                new MockCapabilityProvider(),
                new MockCapabilityValidator());

            var result = useCase.Execute("program test; begin end.", ".pas",
                new CompilationOptions { Target = Architecture.Atari2600 });

            Assert.That(result.Success, Is.False);
        }

        [Test]
        public void Compile_NullStringPool_ReturnsFailure()
        {
            var nullPoolFrontend = new MockFrontendNullPool();
            var useCase = new CompileUseCase(
                nullPoolFrontend,
                new MockMidLevelOptimizer(),
                new MockLowLevelOptimizer(),
                new MockMlirToLlir(),
                new MockCodeGenerator(),
                new MockCapabilityProvider(),
                new MockCapabilityValidator());

            var result = useCase.Execute("program test; begin end.", ".pas",
                new CompilationOptions { Target = Architecture.Atari2600 });

            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("String pool"));
        }

        private class MockFrontendNullPool : ILanguageFrontend
        {
            public ParseResult ParseToHlir(string sourceCode)
            {
                var builder = new HlirBuilder();
                builder.Add((byte)HlirNodeKind.Nop);
                return new ParseResult(builder.Build(), new SymbolTable());
            }
            public IReadOnlyList<string>? LastParseErrors => null;
            public StringPool? StringPool => null;
            public IReadOnlyList<SemanticError>? SemanticErrors => null;
        }
    }
}