using GameVM.Compiler.Pascal;
using GameVM.Compiler.Core.SemanticAnalysis;
using GameVM.Compiler.Core.IR.Transformers;

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
            var sourceCode = "program Test; var x: Integer; begin x := 1 + 2; end.";
            var hlirTree = _frontend.ParseToHlir(sourceCode);
            var stringPool = _frontend.StringPool!;
            Assert.That(hlirTree.Count, Is.GreaterThan(0));

            var mlir = new HlirTreeToMlirTransformer(stringPool).Transform(hlirTree);
            var result = _analyzer.AnalyzeSlab(mlir, stringPool);

            Assert.That(result.Success, Is.True);
        }

        [Test]
        public void AnalyzeSlab_WithWhileLoop_ShouldSucceed()
        {
            var sourceCode = "program Test; var x: Integer; begin x := 0; while x < 5 do begin x := x + 1; end; end.";
            var hlirTree = _frontend.ParseToHlir(sourceCode);
            var stringPool = _frontend.StringPool!;
            Assert.That(hlirTree.Count, Is.GreaterThan(0));

            var mlir = new HlirTreeToMlirTransformer(stringPool).Transform(hlirTree);
            var result = _analyzer.AnalyzeSlab(mlir, stringPool);

            Assert.That(result.Success, Is.True);
        }

        [Test]
        public void AnalyzeSlab_WithForLoop_ShouldSucceed()
        {
            var sourceCode = "program Test; var i: Integer; begin for i := 1 to 5 do begin end; end.";
            var hlirTree = _frontend.ParseToHlir(sourceCode);
            var stringPool = _frontend.StringPool!;
            Assert.That(hlirTree.Count, Is.GreaterThan(0));

            var mlir = new HlirTreeToMlirTransformer(stringPool).Transform(hlirTree);
            var result = _analyzer.AnalyzeSlab(mlir, stringPool);

            Assert.That(result.Success, Is.True);
        }

        [Test]
        public void AnalyzeSlab_WithIfElse_ShouldSucceed()
        {
            var sourceCode = "program Test; var x: Integer; begin x := 1; if x = 1 then begin x := 2; end else begin x := 3; end; end.";
            var hlirTree = _frontend.ParseToHlir(sourceCode);
            var stringPool = _frontend.StringPool!;
            Assert.That(hlirTree.Count, Is.GreaterThan(0));

            var mlir = new HlirTreeToMlirTransformer(stringPool).Transform(hlirTree);
            var result = _analyzer.AnalyzeSlab(mlir, stringPool);

            Assert.That(result.Success, Is.True);
        }

        [Test]
        public void AnalyzeSlab_WithNestedBlocks_ShouldSucceed()
        {
            var sourceCode = "program Test; begin if true then begin if true then begin end; end; end.";
            var hlirTree = _frontend.ParseToHlir(sourceCode);
            var stringPool = _frontend.StringPool!;
            Assert.That(hlirTree.Count, Is.GreaterThan(0));

            var mlir = new HlirTreeToMlirTransformer(stringPool).Transform(hlirTree);
            var result = _analyzer.AnalyzeSlab(mlir, stringPool);

            Assert.That(result.Success, Is.True);
        }

        [Test]
        public void AnalyzeSlab_WithFunctionAndProcedure_ShouldSucceed()
        {
            var sourceCode = "program Test; function Add(a, b: Integer): Integer; begin Add := a + b; end; procedure Foo; begin end; begin end.";
            var hlirTree = _frontend.ParseToHlir(sourceCode);
            var stringPool = _frontend.StringPool!;
            Assert.That(hlirTree.Count, Is.GreaterThan(0));

            var mlir = new HlirTreeToMlirTransformer(stringPool).Transform(hlirTree);
            var result = _analyzer.AnalyzeSlab(mlir, stringPool);

            Assert.That(result.Success, Is.True);
        }

        [Test]
        public void AnalyzeSlab_WithRealType_ShouldSucceed()
        {
            var sourceCode = "program Test; var x: Real; begin x := 3.14; end.";
            var hlirTree = _frontend.ParseToHlir(sourceCode);
            var stringPool = _frontend.StringPool!;
            Assert.That(hlirTree.Count, Is.GreaterThan(0));

            var mlir = new HlirTreeToMlirTransformer(stringPool).Transform(hlirTree);
            var result = _analyzer.AnalyzeSlab(mlir, stringPool);

            Assert.That(result.Success, Is.True);
        }

        [Test]
        public void AnalyzeSlab_WithStringLiteral_ShouldSucceed()
        {
            var sourceCode = "program Test; var s: string; begin s := 'Hello'; end.";
            var hlirTree = _frontend.ParseToHlir(sourceCode);
            var stringPool = _frontend.StringPool!;
            Assert.That(hlirTree.Count, Is.GreaterThan(0));

            var mlir = new HlirTreeToMlirTransformer(stringPool).Transform(hlirTree);
            var result = _analyzer.AnalyzeSlab(mlir, stringPool);

            Assert.That(result.Success, Is.True, $"Semantic analysis failed with errors: {string.Join("; ", result.Errors)}");
        }

        [Test]
        public void AnalyzeSlab_WithMultipleVariables_ShouldSucceed()
        {
            var sourceCode = "program Test; var x, y, z: Integer; begin x := 1; y := 2; z := x + y; end.";
            var hlirTree = _frontend.ParseToHlir(sourceCode);
            var stringPool = _frontend.StringPool!;
            Assert.That(hlirTree.Count, Is.GreaterThan(0));

            var mlir = new HlirTreeToMlirTransformer(stringPool).Transform(hlirTree);
            var result = _analyzer.AnalyzeSlab(mlir, stringPool);

            Assert.That(result.Success, Is.True);
        }

        [Test]
        public void AnalyzeSlab_WithComplexExpressions_ShouldSucceed()
        {
            var sourceCode = "program Test; var x, y, z: Integer; begin x := 1; y := 2; z := (x + y) * (x - y); end.";
            var hlirTree = _frontend.ParseToHlir(sourceCode);
            var stringPool = _frontend.StringPool!;
            Assert.That(hlirTree.Count, Is.GreaterThan(0));

            var mlir = new HlirTreeToMlirTransformer(stringPool).Transform(hlirTree);
            var result = _analyzer.AnalyzeSlab(mlir, stringPool);

            Assert.That(result.Success, Is.True);
        }

        [Test]
        public void AnalyzeSlab_WithBuiltinWriteln_ShouldSucceed()
        {
            var sourceCode = "program Test; begin writeln('Hello, World!'); end.";
            var hlirTree = _frontend.ParseToHlir(sourceCode);
            var stringPool = _frontend.StringPool!;
            Assert.That(hlirTree.Count, Is.GreaterThan(0));

            var mlir = new HlirTreeToMlirTransformer(stringPool).Transform(hlirTree);
            var result = _analyzer.AnalyzeSlab(mlir, stringPool);

            Assert.That(result.Success, Is.True);
        }
    }
}