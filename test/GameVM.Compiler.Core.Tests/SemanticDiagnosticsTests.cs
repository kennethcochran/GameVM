using System;
using System.Linq;
using GameVM.Compiler.Core.Interfaces;

namespace GameVM.Compiler.Core.Tests
{
    [TestFixture]
    public class SemanticDiagnosticsTests
    {
        [Test]
        public void Constructor_WithErrors_BuildsSummary()
        {
            var errors = new[]
            {
                new SemanticError("Undefined variable 'x'", "SEMANTIC_ERROR", 3, 7),
                new SemanticError("Type mismatch", "SEMANTIC_ERROR", 5, 2)
            };
            var diagnostics = new SemanticDiagnostics(errors);

            Assert.That(diagnostics.Errors, Has.Count.EqualTo(2));
            Assert.That(diagnostics.Summary, Does.Contain("3:7: Undefined variable 'x'"));
            Assert.That(diagnostics.Summary, Does.Contain("5:2: Type mismatch"));
        }

        [Test]
        public void Constructor_WithEmptyErrors_SummaryIsEmpty()
        {
            var diagnostics = new SemanticDiagnostics(Array.Empty<SemanticError>());

            Assert.That(diagnostics.Errors, Is.Empty);
            Assert.That(diagnostics.Summary, Is.Empty);
            Assert.That(diagnostics.IsEmpty, Is.True);
        }

        [Test]
        public void Constructor_SingleError_SummaryIsLineColonMessage()
        {
            var error = new SemanticError("Undefined variable 'y'", "SEMANTIC_ERROR", 2, 10);
            var diagnostics = new SemanticDiagnostics(new[] { error });

            Assert.That(diagnostics.Summary, Is.EqualTo("2:10: Undefined variable 'y'"));
        }
    }
}