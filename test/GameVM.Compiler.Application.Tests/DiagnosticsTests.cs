using System.Linq;
using GameVM.Compiler.Application;
using GameVM.Compiler.Core.Interfaces;
using GameVM.Compiler.Core.IR.Buffers;
using GameVM.Compiler.Core.Enums;
using GameVM.Compiler.Core.IR.Interfaces;
using Moq;
using NUnit.Framework;

namespace UnitTests.Application;

[TestFixture]
public class DiagnosticsTests
{
    private static string PascalCodeWithSemanticError =>
        "program Test;\nvar x: Integer;\nbegin\n  y := 5;\nend.";

    [Test]
    public void Execute_WhenSemanticErrors_StructuresDiagnostics()
    {
        var mockFrontend = new Mock<ILanguageFrontend>();
        var semanticError = new SemanticError(
            "Undefined variable 'y'", "SEMANTIC_ERROR", 4, 5);
        mockFrontend.Setup(f => f.SemanticErrors)
            .Returns(new[] { semanticError });
        mockFrontend.Setup(f => f.LastParseErrors)
            .Returns(new[] { semanticError.Message });
        mockFrontend.Setup(f => f.ParseToHlir(It.IsAny<string>()))
            .Returns(new ParseResult(new GameVM.Compiler.Core.IR.Hlir.HlirTree(), default));
        mockFrontend.Setup(f => f.StringPool).Returns(new StringPool());

        var options = new CompilationOptions
        {
            Target = Architecture.Atari2600,
            DispatchStrategy = DispatchStrategy.DirectThreadedCode,
            GenerateDebugInfo = false,
            Optimize = true,
            Profile = CapabilityLevel.L1,
            Enforcement = EnforcementLevel.Strict,
            OptimizationLevel = OptimizationLevel.Basic
        };

        var useCase = new CompileUseCase(
            mockFrontend.Object,
            Mock.Of<GameVM.Compiler.Application.Services.IMidLevelOptimizer>(),
            Mock.Of<GameVM.Compiler.Application.Services.ILowLevelOptimizer>(),
            Mock.Of<IIRSlabTransformer>(),
            Mock.Of<ICodeGenerator>(),
            Mock.Of<ICapabilityProvider>(),
            Mock.Of<GameVM.Compiler.Application.Services.ICapabilityValidatorService>(),
            Mock.Of<ISemanticAnalyzer>());

        var result = useCase.Execute(PascalCodeWithSemanticError, ".pas", options);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics, Is.Not.Null);
        Assert.That(result.Diagnostics.Errors, Has.Count.EqualTo(1));
        Assert.That(result.Diagnostics.Errors[0].Message, Does.Contain("Undefined variable 'y'"));
        Assert.That(result.Diagnostics.Errors[0].Line, Is.EqualTo(4));
        Assert.That(result.Diagnostics.Errors[0].Column, Is.EqualTo(5));
    }

    [Test]
    public void Diagnostics_Summary_RendersLineColumnMessage()
    {
        var semanticError = new SemanticError(
            "Undefined variable 'y'", "SEMANTIC_ERROR", 4, 5);
        var diagnostics = new SemanticDiagnostics(new[] { semanticError });

        Assert.That(diagnostics.Summary, Is.EqualTo("4:5: Undefined variable 'y'"));
    }
}