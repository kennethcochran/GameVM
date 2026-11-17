using GameVM.Compiler.Core.IR.Buffers;
using GameVM.Compiler.Core.IR.Hlir;
using GameVM.Compiler.Pascal.Ast;
using GameVM.Compiler.Pascal.Transformers;

namespace GameVM.Compiler.Pascal.Tests.Transformers
{
    /// <summary>
    /// Regression tests for case-label lowering (PR #25 fix round 5,
    /// CodeRabbit first-review findings). A case label must reach the
    /// HLIR as a LiteralInt whose payload is the pool offset of the
    /// label's decimal text — the convention every downstream layer
    /// relies on when it resolves the label through the string pool.
    /// </summary>
    [TestFixture]
    public class PascalCaseLabelTests
    {
        private static int FindCase(HlirTree hlir)
        {
            for (int i = 0; i < hlir.Count; i++)
            {
                if (hlir.GetKind(i) == (byte)HlirNodeKind.Case)
                    return i;
            }
            return -1;
        }

        [Test]
        public void CaseLabel_NamedIntegerConstant_ResolvesToTheConstantsValueText()
        {
            // `case x of k: ...` with `const k = 2` must test the selector
            // against the constant's value, exactly as the literal label
            // `2` does. BuildExpression's named-constant fold emitted the
            // raw value (2) as the LiteralInt payload, while a literal
            // label carries the pool offset of its text; the MLIR layer
            // passes the payload through as a pool offset and the backend
            // resolves it as one, so offset 2 — unrelated pool bytes —
            // was classified as a zero-page name and the case compared
            // the selector against uninitialized memory.
            var frontend = new PascalFrontend();
            string source =
                "program Probe;\n" +
                "const k = 2;\n" +
                "var x: Integer;\n" +
                "begin\n" +
                "  x := 2;\n" +
                "  case x of\n" +
                "    k: x := 20;\n" +
                "  else\n" +
                "    x := 40\n" +
                "  end;\n" +
                "end.";

            var result = frontend.ParseToHlir(source);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(frontend.LastParseErrors, Is.Null, "the program must lower without errors");
                var hlir = result.Hlir;
                int caseIdx = FindCase(hlir);
                Assert.That(caseIdx, Is.GreaterThanOrEqualTo(0), "the HLIR must contain a Case node");
                int labelIdx = hlir.Children(caseIdx)[1];
                Assert.That(hlir.GetKind(labelIdx), Is.EqualTo((byte)HlirNodeKind.LiteralInt),
                    "a named-constant case label must lower to a LiteralInt");
                Assert.That(frontend.StringPool!.Resolve(hlir[labelIdx].Payload), Is.EqualTo("2"),
                    "the label payload must resolve through the pool to the constant's value text, as a literal label's does");
            }
        }

        [Test]
        public void CaseLabel_UppercaseNamedConstant_LowersAndResolves()
        {
            // `const Limit = 2` referenced as `Limit:` in a case must
            // resolve to the constant. The visitor interned the
            // declaration name as written but the case-label identifier
            // through the lowercasing InternIdentifier, so the two pool
            // offsets differed: semantic analysis reported the label an
            // undefined variable and the program never lowered.
            var frontend = new PascalFrontend();
            string source =
                "program Probe;\n" +
                "const Limit = 2;\n" +
                "var x: Integer;\n" +
                "begin\n" +
                "  x := 2;\n" +
                "  case x of\n" +
                "    Limit: x := 20;\n" +
                "  else\n" +
                "    x := 40\n" +
                "  end;\n" +
                "end.";

            var result = frontend.ParseToHlir(source);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(frontend.LastParseErrors, Is.Null,
                    "an uppercase named constant must be usable as a case label without errors");
                var hlir = result.Hlir;
                int caseIdx = FindCase(hlir);
                Assert.That(caseIdx, Is.GreaterThanOrEqualTo(0), "the HLIR must contain a Case node");
                int labelIdx = hlir.Children(caseIdx)[1];
                Assert.That(hlir.GetKind(labelIdx), Is.EqualTo((byte)HlirNodeKind.LiteralInt),
                    "a named-constant case label must lower to a LiteralInt");
                Assert.That(frontend.StringPool!.Resolve(hlir[labelIdx].Payload), Is.EqualTo("2"),
                    "the label payload must resolve through the pool to the constant's value text");
            }
        }

        [Test]
        public void CaseLabel_NegativeLiteral_WhenPoolSizePutsUnaryMinusInRange_StillLowers()
        {
            // A negative case label is a UnaryOp node whose payload is
            // the '-' character (45), not a pool offset. BuildCaseLabel
            // resolved that payload through the string pool to build its
            // diagnostic text; when the pool buffer is 46-48 bytes long,
            // offset 45 passes the pool's bounds check and the 4-byte
            // length read then runs past the end of the buffer, throwing
            // ArgumentOutOfRangeException — a valid negative label failed
            // the compile with a framework exception instead of lowering.
            // This fixture pads the pool to exactly 46 bytes so the
            // scenario is deterministic.
            var pool = new StringPool();
            var builder = new AstBuilder();
            uint xOffset = pool.Intern("x");
            uint intOffset = pool.Intern("integer");

            int labelLiteral = builder.Add((byte)PascalAstNodeKind.LiteralInt, 0, pool.Intern("5"));
            int label = builder.Add((byte)PascalAstNodeKind.UnaryOp, 0, (uint)'-', labelLiteral);
            int armTarget = builder.Add((byte)PascalAstNodeKind.Identifier, 0, xOffset);
            int armValue = builder.Add((byte)PascalAstNodeKind.LiteralInt, 0, pool.Intern("5"));
            int arm = builder.Add((byte)PascalAstNodeKind.Assignment, 0, 0, armTarget, armValue);
            int branch = builder.Add((byte)PascalAstNodeKind.CaseBranch, 0, 0, label, arm);
            int selector = builder.Add((byte)PascalAstNodeKind.Identifier, 0, xOffset);
            int caseNode = builder.Add((byte)PascalAstNodeKind.Case, 0, 0, selector, branch);

            int xId = builder.Add((byte)PascalAstNodeKind.Identifier, 0, xOffset);
            int xType = builder.Add((byte)PascalAstNodeKind.TypeDefinition, 0, intOffset);
            int xDecl = builder.Add((byte)PascalAstNodeKind.VariableDeclaration, 0, 0, xId, xType);

            int block = builder.Add((byte)PascalAstNodeKind.Block, 0, 0, caseNode);
            int method = builder.Add((byte)PascalAstNodeKind.MethodDeclaration, 0, pool.Intern("main"), xDecl, block);
            builder.Add((byte)PascalAstNodeKind.Program, 0, 0, method);

            // Pool buffer so far: "" (5 bytes) + "x" (6) + "integer" (12)
            // + "5" (6) + "main" (9) = 38 bytes. Pad to exactly 46 so that
            // resolving offset 45 lands inside the buffer while its
            // 4-byte length prefix read runs past the end. (Measured via
            // ToByteArray: StringPool.Size is not maintained on Intern.)
            using (Assert.EnterMultipleScope())
            {
                Assert.That(pool.ToByteArray(), Has.Length.EqualTo(38), "fixture precondition: pool size before padding");
                pool.Intern("fff");
                Assert.That(pool.ToByteArray(), Has.Length.EqualTo(46), "fixture precondition: pool padded to 46 bytes");

                var transformer = new PascalAstToHlirTransformer(pool);
                HlirTree hlir = default;
                Assert.That(() => { hlir = transformer.Transform(builder.Build()); },
                    Throws.Nothing,
                    "a negative case label must lower, not throw out of the string pool");
                int caseIdx = FindCase(hlir);
                Assert.That(caseIdx, Is.GreaterThanOrEqualTo(0), "the HLIR must contain a Case node");
                int labelIdx = hlir.Children(caseIdx)[1];
                Assert.That(hlir.GetKind(labelIdx), Is.EqualTo((byte)HlirNodeKind.LiteralInt),
                    "the negative label must fold to a LiteralInt");
                Assert.That(pool.Resolve(hlir[labelIdx].Payload), Is.EqualTo("251"),
                    "-5 folds to its 8-bit two's complement value 251");
            }
        }
    }
}
