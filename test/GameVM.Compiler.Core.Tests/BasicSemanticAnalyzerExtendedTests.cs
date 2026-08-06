using GameVM.Compiler.Core.SemanticAnalysis;
using GameVM.Compiler.Pascal;

namespace GameVM.Compiler.Core.Tests
{
    [TestFixture]
    public class BasicSemanticAnalyzerExtendedTests
    {
        private PascalFrontend _frontend = null!;
        private BasicSemanticAnalyzer _analyzer = null!;

        [SetUp]
        public void Setup()
        {
            _frontend = new PascalFrontend();
            _analyzer = new BasicSemanticAnalyzer();
        }

        [Test]
        public void AnalyzeSlab_ValidArithmetic_ShouldSucceed()
        {
            var sourceCode = "program Test;\nvar x: Integer;\nbegin\n  x := 5 + 3 * 2;\nend.";
            var astSlab = _frontend.ParseToSlab(sourceCode);
            var hlirSlab = _frontend.ConvertToHlirSlab(astSlab);
            var stringPool = _frontend.StringPool!;
            Assert.That(hlirSlab, Is.Not.Empty);

            var result = _analyzer.AnalyzeSlab(hlirSlab, stringPool);

            Assert.That(result.Success, Is.True);
        }

        [Test]
        public void AnalyzeSlab_WithWhileLoop_ShouldSucceed()
        {
            var sourceCode = "program Test;\nvar i: Integer;\nbegin\n  i := 1;\n  while i < 10 do\n    i := i + 1;\nend.";
            var astSlab = _frontend.ParseToSlab(sourceCode);
            var hlirSlab = _frontend.ConvertToHlirSlab(astSlab);
            var stringPool = _frontend.StringPool!;
            Assert.That(hlirSlab, Is.Not.Empty);

            var result = _analyzer.AnalyzeSlab(hlirSlab, stringPool);

            Assert.That(result.Success, Is.True);
        }

        [Test]
        public void AnalyzeSlab_WithForLoop_ShouldSucceed()
        {
            var sourceCode = "program Test;\nvar i: Integer;\nbegin\n  for i := 1 to 10 do\n    writeln(i);\nend.";
            var astSlab = _frontend.ParseToSlab(sourceCode);
            var hlirSlab = _frontend.ConvertToHlirSlab(astSlab);
            var stringPool = _frontend.StringPool!;
            Assert.That(hlirSlab, Is.Not.Empty);

            var result = _analyzer.AnalyzeSlab(hlirSlab, stringPool);

            Assert.That(result.Success, Is.True);
        }

        [Test]
        public void AnalyzeSlab_WithIfElse_ShouldSucceed()
        {
            var sourceCode = "program Test;\nvar x: Integer;\nbegin\n  readln(x);\n  if x > 0 then\n    writeln('positive')\n  else\n    writeln('non-positive');\nend.";
            var astSlab = _frontend.ParseToSlab(sourceCode);
            var hlirSlab = _frontend.ConvertToHlirSlab(astSlab);
            var stringPool = _frontend.StringPool!;
            Assert.That(hlirSlab, Is.Not.Empty);

            var result = _analyzer.AnalyzeSlab(hlirSlab, stringPool);

            Assert.That(result.Success, Is.True);
        }

        [Test]
        public void AnalyzeSlab_WithNestedBlocks_ShouldSucceed()
        {
            var sourceCode = "program Test;\nvar x: Integer;\nbegin\n  x := 1;\n  begin\n    x := x + 1;\n    begin\n      x := x + 1;\n    end;\n  end;\nend.";
            var astSlab = _frontend.ParseToSlab(sourceCode);
            var hlirSlab = _frontend.ConvertToHlirSlab(astSlab);
            var stringPool = _frontend.StringPool!;
            Assert.That(hlirSlab, Is.Not.Empty);

            var result = _analyzer.AnalyzeSlab(hlirSlab, stringPool);

            Assert.That(result.Success, Is.True);
        }

        [Test]
        public void AnalyzeSlab_WithFunctionAndProcedure_ShouldSucceed()
        {
            var sourceCode = @"
                program Test;
                function Add(a, b: Integer): Integer;
                begin
                  Add := a + b;
                end;
                procedure Foo;
                begin
                end;
                begin
                end.";
            var astSlab = _frontend.ParseToSlab(sourceCode);
            var hlirSlab = _frontend.ConvertToHlirSlab(astSlab);
            var stringPool = _frontend.StringPool!;
            Assert.That(hlirSlab, Is.Not.Empty);

            var result = _analyzer.AnalyzeSlab(hlirSlab, stringPool);

            Assert.That(result.Success, Is.True);
        }

        [Test]
        public void AnalyzeSlab_WithRealType_ShouldSucceed()
        {
            var sourceCode = "program Test;\nvar x: Real;\nbegin\n  x := 3.14;\nend.";
            var astSlab = _frontend.ParseToSlab(sourceCode);
            var hlirSlab = _frontend.ConvertToHlirSlab(astSlab);
            var stringPool = _frontend.StringPool!;
            Assert.That(hlirSlab, Is.Not.Empty);

            var result = _analyzer.AnalyzeSlab(hlirSlab, stringPool);

            Assert.That(result.Success, Is.True);
        }

        [Test]
        public void AnalyzeSlab_WithStringLiteral_ShouldSucceed()
        {
            var sourceCode = "program Test;\nvar msg: String;\nbegin\n  msg := 'Hello, World!';\n  WriteLn(msg);\nend.";
            var astSlab = _frontend.ParseToSlab(sourceCode);
            var hlirSlab = _frontend.ConvertToHlirSlab(astSlab);
            var stringPool = _frontend.StringPool!;
            Assert.That(hlirSlab, Is.Not.Empty);

            var result = _analyzer.AnalyzeSlab(hlirSlab, stringPool);

            Assert.That(result.Success, Is.True, $"Semantic analysis failed with errors: {string.Join("; ", result.Errors)}");
        }

        [Test]
        public void AnalyzeSlab_WithMultipleVariables_ShouldSucceed()
        {
            var sourceCode = "program Test;\nvar a, b, c, d: Integer;\nbegin\n  a := 1;\n  b := 2;\n  c := 3;\n  d := 4;\nend.";
            var astSlab = _frontend.ParseToSlab(sourceCode);
            var hlirSlab = _frontend.ConvertToHlirSlab(astSlab);
            var stringPool = _frontend.StringPool!;
            Assert.That(hlirSlab, Is.Not.Empty);

            var result = _analyzer.AnalyzeSlab(hlirSlab, stringPool);

            Assert.That(result.Success, Is.True);
        }

        [Test]
        public void AnalyzeSlab_WithComplexExpressions_ShouldSucceed()
        {
            var sourceCode = "program Test;\nvar x: Integer;\nbegin\n  x := (5 + 3) * 2 - 4 / 2;\nend.";
            var astSlab = _frontend.ParseToSlab(sourceCode);
            var hlirSlab = _frontend.ConvertToHlirSlab(astSlab);
            var stringPool = _frontend.StringPool!;
            Assert.That(hlirSlab, Is.Not.Empty);

            var result = _analyzer.AnalyzeSlab(hlirSlab, stringPool);

            Assert.That(result.Success, Is.True);
        }

        [Test]
        public void AnalyzeSlab_WithBuiltinWriteln_ShouldSucceed()
        {
            var sourceCode = "program Test;\nbegin\n  WriteLn('Hello, World!');\nend.";
            var astSlab = _frontend.ParseToSlab(sourceCode);
            var hlirSlab = _frontend.ConvertToHlirSlab(astSlab);
            var stringPool = _frontend.StringPool!;
            Assert.That(hlirSlab, Is.Not.Empty);

            var result = _analyzer.AnalyzeSlab(hlirSlab, stringPool);

            Assert.That(result.Success, Is.True);
        }
    }
}