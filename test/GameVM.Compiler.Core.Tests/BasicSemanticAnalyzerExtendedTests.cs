using NUnit.Framework;
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
            var sourceCode = "program Test;\nvar x, y, z: Integer;\nbegin\n  x := 5;\n  y := 3;\n  z := x + y;\n  z := x - y;\n  z := x * y;\n  z := x div y;\n  z := x mod y;\nend.";
            var astSlab = _frontend.ParseToSlab(sourceCode);
            var hlirSlab = _frontend.ConvertToHlirSlab(astSlab);
            Assert.That(hlirSlab, Is.Not.Null.And.Not.Empty);

            var result = _analyzer.AnalyzeSlab(hlirSlab);

            Assert.That(result.Success, Is.True);
        }

        [Test]
        public void AnalyzeSlab_WithWhileLoop_ShouldSucceed()
        {
            var sourceCode = "program Test;\nvar i, sum: Integer;\nbegin\n  sum := 0;\n  i := 1;\n  while i <= 10 do\n  begin\n    sum := sum + i;\n    i := i + 1;\n  end;\nend.";
            var astSlab = _frontend.ParseToSlab(sourceCode);
            var hlirSlab = _frontend.ConvertToHlirSlab(astSlab);
            Assert.That(hlirSlab, Is.Not.Null.And.Not.Empty);

            var result = _analyzer.AnalyzeSlab(hlirSlab);

            Assert.That(result.Success, Is.True);
        }

        [Test]
        public void AnalyzeSlab_WithForLoop_ShouldSucceed()
        {
            var sourceCode = "program Test;\nvar i, sum: Integer;\nbegin\n  sum := 0;\n  for i := 1 to 10 do\n    sum := sum + i;\nend.";
            var astSlab = _frontend.ParseToSlab(sourceCode);
            var hlirSlab = _frontend.ConvertToHlirSlab(astSlab);
            Assert.That(hlirSlab, Is.Not.Null.And.Not.Empty);

            var result = _analyzer.AnalyzeSlab(hlirSlab);

            Assert.That(result.Success, Is.True);
        }

        [Test]
        public void AnalyzeSlab_WithIfElse_ShouldSucceed()
        {
            var sourceCode = "program Test;\nvar x, y: Integer;\nbegin\n  x := 10;\n  if x > 0 then\n    y := 1\n  else\n    y := -1;\nend.";
            var astSlab = _frontend.ParseToSlab(sourceCode);
            var hlirSlab = _frontend.ConvertToHlirSlab(astSlab);
            Assert.That(hlirSlab, Is.Not.Null.And.Not.Empty);

            var result = _analyzer.AnalyzeSlab(hlirSlab);

            Assert.That(result.Success, Is.True);
        }

        [Test]
        public void AnalyzeSlab_WithNestedBlocks_ShouldSucceed()
        {
            var sourceCode = "program Test;\nvar x: Integer;\nbegin\n  x := 1;\n  begin\n    x := x + 1;\n    begin\n      x := x + 1;\n    end;\n  end;\nend.";
            var astSlab = _frontend.ParseToSlab(sourceCode);
            var hlirSlab = _frontend.ConvertToHlirSlab(astSlab);
            Assert.That(hlirSlab, Is.Not.Null.And.Not.Empty);

            var result = _analyzer.AnalyzeSlab(hlirSlab);

            Assert.That(result.Success, Is.True);
        }

        [Test]
        public void AnalyzeSlab_WithFunctionAndProcedure_ShouldSucceed()
        {
            var sourceCode = @"
                program Test;
                var global: Integer;
                procedure IncrementBy(n: Integer);
                begin
                  global := global + n;
                end;
                function Double(x: Integer): Integer;
                begin
                  Double := x * 2;
                end;
                begin
                  global := 5;
                  IncrementBy(3);
                  global := Double(global);
                end.";
            var astSlab = _frontend.ParseToSlab(sourceCode);
            var hlirSlab = _frontend.ConvertToHlirSlab(astSlab);
            Assert.That(hlirSlab, Is.Not.Null.And.Not.Empty);

            var result = _analyzer.AnalyzeSlab(hlirSlab);

            Assert.That(result.Success, Is.True);
        }

        [Test]
        public void AnalyzeSlab_WithRealType_ShouldSucceed()
        {
            var sourceCode = "program Test;\nvar x: Real;\nbegin\n  x := 3.14;\n  x := 42;\nend.";
            var astSlab = _frontend.ParseToSlab(sourceCode);
            var hlirSlab = _frontend.ConvertToHlirSlab(astSlab);
            Assert.That(hlirSlab, Is.Not.Null.And.Not.Empty);

            var result = _analyzer.AnalyzeSlab(hlirSlab);

            Assert.That(result.Success, Is.True);
        }

        [Test]
        public void AnalyzeSlab_WithStringLiteral_ShouldSucceed()
        {
            var sourceCode = "program Test;\nvar s: String;\nbegin\n  s := 'hello';\nend.";
            var astSlab = _frontend.ParseToSlab(sourceCode);
            var hlirSlab = _frontend.ConvertToHlirSlab(astSlab);
            Assert.That(hlirSlab, Is.Not.Null.And.Not.Empty);

            var result = _analyzer.AnalyzeSlab(hlirSlab);

            Assert.That(result.Success, Is.True);
        }

        [Test]
        public void AnalyzeSlab_WithMultipleVariables_ShouldSucceed()
        {
            var sourceCode = "program Test;\nvar a, b, c, d: Integer;\nvar x, y: Real;\nbegin\n  a := 1;\n  b := 2;\n  c := a + b;\n  d := c * 2;\n  x := 1.5;\n  y := x + 2.5;\nend.";
            var astSlab = _frontend.ParseToSlab(sourceCode);
            var hlirSlab = _frontend.ConvertToHlirSlab(astSlab);
            Assert.That(hlirSlab, Is.Not.Null.And.Not.Empty);

            var result = _analyzer.AnalyzeSlab(hlirSlab);

            Assert.That(result.Success, Is.True);
        }

        [Test]
        public void AnalyzeSlab_WithComplexExpressions_ShouldSucceed()
        {
            var sourceCode = "program Test;\nvar x, y, z: Integer;\nbegin\n  x := 5;\n  y := 3;\n  z := (x + y) * 2 - 1;\nend.";
            var astSlab = _frontend.ParseToSlab(sourceCode);
            var hlirSlab = _frontend.ConvertToHlirSlab(astSlab);
            Assert.That(hlirSlab, Is.Not.Null.And.Not.Empty);

            var result = _analyzer.AnalyzeSlab(hlirSlab);

            Assert.That(result.Success, Is.True);
        }

        [Test]
        public void AnalyzeSlab_WithBuiltinWriteln_ShouldSucceed()
        {
            var sourceCode = "program Test;\nbegin\n  writeln('hello');\n  writeln(42);\n  writeln(3.14);\n  writeln(true);\nend.";
            var astSlab = _frontend.ParseToSlab(sourceCode);
            var hlirSlab = _frontend.ConvertToHlirSlab(astSlab);
            Assert.That(hlirSlab, Is.Not.Null.And.Not.Empty);

            var result = _analyzer.AnalyzeSlab(hlirSlab);

            Assert.That(result.Success, Is.True);
        }
    }
}