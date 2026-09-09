using GameVM.Compiler.Core.SemanticAnalysis;
using GameVM.Compiler.Core.IR.Soa;
using GameVM.Compiler.Core.IR.Transformers;
using GameVM.Compiler.Pascal;

namespace GameVM.Compiler.Core.Tests
{
    [TestFixture]
    public class BasicSemanticAnalyzerTests
    {
        private PascalFrontend _frontend = null!;
        private BasicSemanticAnalyzer _analyzer = null!;
        private static InstList BuildSlab(params byte[] kinds)
        {
            if (kinds.Length == 0)
                return new InstList(Array.Empty<byte>(), Array.Empty<ushort>(), Array.Empty<ushort>(), Array.Empty<uint>(), Array.Empty<uint>(), Array.Empty<uint>(), Array.Empty<int>(), 0, 0);

            var tags = new byte[kinds.Length];
            var flags = new ushort[kinds.Length];
            var argCounts = new ushort[kinds.Length];
            var fixedOps = new uint[kinds.Length * InstConstants.MAX_FIXED_OPS];
            var extra = new uint[0];
            var extraOffsets = new uint[kinds.Length];
            var blockIds = new int[kinds.Length];

            for (int i = 0; i < kinds.Length; i++)
            {
                tags[i] = kinds[i];
                flags[i] = 0;
                argCounts[i] = 0;
                Array.Fill(fixedOps, 0u, i * InstConstants.MAX_FIXED_OPS, InstConstants.MAX_FIXED_OPS);
                extraOffsets[i] = 0;
                blockIds[i] = 0;
            }

            return new InstList(tags, flags, argCounts, fixedOps, extra, extraOffsets, blockIds, kinds.Length, 0);
        }

        [SetUp]
        public void Setup()
        {
            _frontend = new PascalFrontend();
            _analyzer = new BasicSemanticAnalyzer();
        }

        [Test]
        public void AnalyzeSlab_ReturnsSuccess_ForValidSimpleProgram()
        {
            // Arrange
            var sourceCode = "program Test;\nvar x: Integer;\nbegin\n  x := 42;\nend.";
            var astSlab = _frontend.ParseToSlab(sourceCode);
            var hlirTree = _frontend.ConvertToHlirSlab(astSlab);
            var stringPool = _frontend.StringPool!;
            var mlir = new HlirTreeToMlirTransformer(stringPool).Transform(hlirTree);

            // Act
            var result = _analyzer.AnalyzeSlab(mlir, stringPool);

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.Errors, Is.Empty);
        }

        [Test]
        public void AnalyzeSlab_ReturnsSuccess_ForCompatibleTypes()
        {
            // Arrange - Integer to Real assignment is structurally valid; the new analyzer
            // only performs structural checks and no longer enforces type compatibility.
            var sourceCode = "program Test;\nvar x: Real;\nbegin\n  x := 42;\nend.";
            var astSlab = _frontend.ParseToSlab(sourceCode);
            var hlirTree = _frontend.ConvertToHlirSlab(astSlab);
            var stringPool = _frontend.StringPool!;
            var mlir = new HlirTreeToMlirTransformer(stringPool).Transform(hlirTree);

            // Act
            var result = _analyzer.AnalyzeSlab(mlir, stringPool);

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.Errors, Is.Empty);
        }

        [Test]
        public void AnalyzeSlab_ReturnsSuccess_ForFunctionWithReturn()
        {
            // Arrange
            var sourceCode = @"
                program Test;
                function Add(a, b: Integer): Integer;
                begin
                  Add := a + b;
                end;
                begin
                end.";
            var astSlab = _frontend.ParseToSlab(sourceCode);
            var hlirTree = _frontend.ConvertToHlirSlab(astSlab);
            var stringPool = _frontend.StringPool!;
            var mlir = new HlirTreeToMlirTransformer(stringPool).Transform(hlirTree);

            // Act
            var result = _analyzer.AnalyzeSlab(mlir, stringPool);

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.Errors, Is.Empty);
        }

        [Test]
        public void AnalyzeSlab_ReturnsError_ForEmptySlab()
        {
            // Act
            var result = _analyzer.AnalyzeSlab(BuildSlab(), _frontend.StringPool!);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.Errors, Is.Not.Empty);
        }

        [Test]
        public void AnalyzeSlab_DoesNotValidateMagicNumber_ForNonEmptySlab()
        {
            // The rewritten analyzer performs only structural checks (undefined
            // variables, empty stream) and no longer validates a magic number on the
            // flat MLIR stream.
            var slab = BuildSlab((byte)0x01);
            var stringPool = _frontend.StringPool!;
            var result = _analyzer.AnalyzeSlab(slab, stringPool);

            // Assert: any non-empty stream is accepted; the tag bytes aren't validated.
            Assert.That(result.Success, Is.True);
            Assert.That(result.Errors, Is.Empty);
        }

        [Test]
        public void AnalyzeSlab_DoesNotValidateStage_ForNonEmptySlab()
        {
            // Stage/version validation was removed together with the magic-number
            // check; a non-empty stream is accepted whatever its stage marker is.
            var slab = BuildSlab((byte)0x02);
            var stringPool = _frontend.StringPool!;
            var result = _analyzer.AnalyzeSlab(slab, stringPool);

            // Assert: the analyzer succeeds on a structurally non-empty stream.
            Assert.That(result.Success, Is.True);
            Assert.That(result.Errors, Is.Empty);
        }
    }
}