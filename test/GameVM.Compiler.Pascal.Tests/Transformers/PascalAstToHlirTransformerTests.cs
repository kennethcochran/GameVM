using GameVM.Compiler.Core.IR.Ast;
using GameVM.Compiler.Core.IR.Buffers;
using GameVM.Compiler.Core.IR.Hlir;
using GameVM.Compiler.Core.IR.Soa;
using GameVM.Compiler.Pascal.Transformers;

namespace GameVM.Compiler.Pascal.Tests.Transformers
{
    /// <summary>
    /// AST -> HLIR seam tests. The SUT is PascalAstToHlirTransformer alone: its
    /// input AstTree is hand-built with AstBuilder (no parser involved), so only
    /// the transformer under test is exercised.
    /// </summary>
    [TestFixture]
    public class PascalAstToHlirTransformerTests
    {
        /// <summary>Builds the AST for `var x: integer; begin x := 5; end.` with children
        /// before parents (AstBuilder post-order contract).</summary>
        private static AstTree BuildAssignmentAst(StringPool pool)
        {
            var builder = new AstBuilder();
            uint xOffset = pool.Intern("x");
            uint intOffset = pool.Intern("integer");

            // Assignment target and value (leaves first)
            int target = builder.Add((byte)PascalAstNodeKind.Identifier, 0, xOffset);
            int value = builder.Add((byte)PascalAstNodeKind.LiteralInt, 0, pool.Intern("5"));
            int assign = builder.Add((byte)PascalAstNodeKind.Assignment, 0, 0, target, value);

            // Variable declaration: Identifier x, TypeDefinition integer
            int varId = builder.Add((byte)PascalAstNodeKind.Identifier, 0, xOffset);
            int typeDef = builder.Add((byte)PascalAstNodeKind.TypeDefinition, 0, intOffset);
            int varDecl = builder.Add((byte)PascalAstNodeKind.VariableDeclaration, 0, 0, varId, typeDef);

            // Body block -> method -> program
            int block = builder.Add((byte)PascalAstNodeKind.Block, 0, 0, assign);
            int method = builder.Add((byte)PascalAstNodeKind.MethodDeclaration, 0, pool.Intern("main"), varDecl, block);
            builder.Add((byte)PascalAstNodeKind.Program, 0, 0, method);
            return builder.Build();
        }

        [Test]
        public void Transform_ConstantAssignment_ProducesAssignWithLiteralChild()
        {
            var pool = new StringPool();
            var ast = BuildAssignmentAst(pool);
            var transformer = new PascalAstToHlirTransformer(pool);

            var hlir = transformer.Transform(ast);

            int assignIdx = -1;
            for (int i = 0; i < hlir.Count; i++)
            {
                if (hlir.GetKind(i) == (byte)HlirNodeKind.Assign)
                {
                    assignIdx = i;
                    break;
                }
            }

            Assert.That(assignIdx, Is.GreaterThanOrEqualTo(0), "Assign node should be present in HLIR");
            var children = hlir.Children(assignIdx);
            Assert.That(children.Length, Is.EqualTo(2), "Assign should have target and value children");
            Assert.That(hlir.GetKind(children[0]), Is.EqualTo((byte)HlirNodeKind.Identifier),
                "Assign target should be an Identifier");
            Assert.That(hlir.GetKind(children[1]), Is.EqualTo((byte)HlirNodeKind.LiteralInt),
                "Assign value should be a LiteralInt");
        }
    }
}