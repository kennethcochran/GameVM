using GameVM.Compiler.Pascal.Ast;
using GameVM.Compiler.Core.IR.Buffers;
using GameVM.Compiler.Core.IR.Hlir;

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
        [Test]
        public void Transform_UnaryMinusVariable_ProducesUnaryOpNode()
        {
            // x := -y  ->  Assign(Identifier x, UnaryOp('-', Identifier y))
            var pool = new StringPool();
            var builder = new AstBuilder();
            uint xOffset = pool.Intern("x");
            uint yOffset = pool.Intern("y");
            uint intOffset = pool.Intern("integer");

            int target = builder.Add((byte)PascalAstNodeKind.Identifier, 0, xOffset);
            int operand = builder.Add((byte)PascalAstNodeKind.Identifier, 0, yOffset);
            int unary = builder.Add((byte)PascalAstNodeKind.UnaryOp, 0, (uint)'-', operand);
            int assign = builder.Add((byte)PascalAstNodeKind.Assignment, 0, 0, target, unary);

            int xId = builder.Add((byte)PascalAstNodeKind.Identifier, 0, xOffset);
            int xType = builder.Add((byte)PascalAstNodeKind.TypeDefinition, 0, intOffset);
            int xDecl = builder.Add((byte)PascalAstNodeKind.VariableDeclaration, 0, 0, xId, xType);
            int yId = builder.Add((byte)PascalAstNodeKind.Identifier, 0, yOffset);
            int yType = builder.Add((byte)PascalAstNodeKind.TypeDefinition, 0, intOffset);
            int yDecl = builder.Add((byte)PascalAstNodeKind.VariableDeclaration, 0, 0, yId, yType);

            int block = builder.Add((byte)PascalAstNodeKind.Block, 0, 0, assign);
            int method = builder.Add((byte)PascalAstNodeKind.MethodDeclaration, 0, pool.Intern("main"), xDecl, yDecl, block);
            builder.Add((byte)PascalAstNodeKind.Program, 0, 0, method);

            var transformer = new PascalAstToHlirTransformer(pool);
            var hlir = transformer.Transform(builder.Build());

            int assignIdx = -1;
            for (int i = 0; i < hlir.Count; i++)
                if (hlir.GetKind(i) == (byte)HlirNodeKind.Assign) { assignIdx = i; break; }
            Assert.That(assignIdx, Is.GreaterThanOrEqualTo(0), "Assign node should be present");

            var assignChildren = hlir.Children(assignIdx);
            Assert.That(assignChildren.Length, Is.EqualTo(2));
            Assert.That(hlir.GetKind(assignChildren[1]), Is.EqualTo((byte)HlirNodeKind.UnaryOp),
                "x := -y must lower to an HLIR UnaryOp, not vanish");

            int unaryIdx = assignChildren[1];
            Assert.That(hlir[unaryIdx].Payload, Is.EqualTo((uint)'-'), "UnaryOp payload must be the '-' op char");
            var unaryChildren = hlir.Children(unaryIdx);
            Assert.That(unaryChildren.Length, Is.EqualTo(1), "UnaryOp must have exactly one operand child");
            Assert.That(hlir.GetKind(unaryChildren[0]), Is.EqualTo((byte)HlirNodeKind.Identifier));
            Assert.That(hlir[unaryChildren[0]].Payload, Is.EqualTo(yOffset), "operand must be y");
        }

        [Test]
        public void Transform_UnaryMinusLiteral_FoldsToTwosComplement()
        {
            // x := -5  ->  Assign(Identifier x, LiteralInt "251")
            var pool = new StringPool();
            var builder = new AstBuilder();
            uint xOffset = pool.Intern("x");
            uint intOffset = pool.Intern("integer");

            int target = builder.Add((byte)PascalAstNodeKind.Identifier, 0, xOffset);
            int literal = builder.Add((byte)PascalAstNodeKind.LiteralInt, 0, pool.Intern("5"));
            int unary = builder.Add((byte)PascalAstNodeKind.UnaryOp, 0, (uint)'-', literal);
            int assign = builder.Add((byte)PascalAstNodeKind.Assignment, 0, 0, target, unary);

            int xId = builder.Add((byte)PascalAstNodeKind.Identifier, 0, xOffset);
            int xType = builder.Add((byte)PascalAstNodeKind.TypeDefinition, 0, intOffset);
            int xDecl = builder.Add((byte)PascalAstNodeKind.VariableDeclaration, 0, 0, xId, xType);

            int block = builder.Add((byte)PascalAstNodeKind.Block, 0, 0, assign);
            int method = builder.Add((byte)PascalAstNodeKind.MethodDeclaration, 0, pool.Intern("main"), xDecl, block);
            builder.Add((byte)PascalAstNodeKind.Program, 0, 0, method);

            var transformer = new PascalAstToHlirTransformer(pool);
            var hlir = transformer.Transform(builder.Build());

            int assignIdx = -1;
            for (int i = 0; i < hlir.Count; i++)
                if (hlir.GetKind(i) == (byte)HlirNodeKind.Assign) { assignIdx = i; break; }
            Assert.That(assignIdx, Is.GreaterThanOrEqualTo(0));

            var assignChildren = hlir.Children(assignIdx);
            Assert.That(hlir.GetKind(assignChildren[1]), Is.EqualTo((byte)HlirNodeKind.LiteralInt),
                "-5 must constant-fold to a LiteralInt");
            Assert.That(pool.Resolve(hlir[assignChildren[1]].Payload), Is.EqualTo("251"),
                "-5 folds to its 8-bit two's complement value 251");
        }

        [Test]
        public void Transform_UnaryMinusNamedConstant_FoldsToTwosComplement()
        {
            // const FIVE = 5; x := -FIVE  ->  Assign(Identifier x, LiteralInt "251")
            var pool = new StringPool();
            var builder = new AstBuilder();
            uint xOffset = pool.Intern("x");
            uint fiveOffset = pool.Intern("FIVE");
            uint intOffset = pool.Intern("integer");

            int constName = builder.Add((byte)PascalAstNodeKind.Identifier, 0, fiveOffset);
            int constVal = builder.Add((byte)PascalAstNodeKind.LiteralInt, 0, pool.Intern("5"));
            int constDef = builder.Add((byte)PascalAstNodeKind.ConstantDefinition, 0, 0, constName, constVal);

            int target = builder.Add((byte)PascalAstNodeKind.Identifier, 0, xOffset);
            int operand = builder.Add((byte)PascalAstNodeKind.Identifier, 0, fiveOffset);
            int unary = builder.Add((byte)PascalAstNodeKind.UnaryOp, 0, (uint)'-', operand);
            int assign = builder.Add((byte)PascalAstNodeKind.Assignment, 0, 0, target, unary);

            int xId = builder.Add((byte)PascalAstNodeKind.Identifier, 0, xOffset);
            int xType = builder.Add((byte)PascalAstNodeKind.TypeDefinition, 0, intOffset);
            int xDecl = builder.Add((byte)PascalAstNodeKind.VariableDeclaration, 0, 0, xId, xType);

            int block = builder.Add((byte)PascalAstNodeKind.Block, 0, 0, assign);
            int method = builder.Add((byte)PascalAstNodeKind.MethodDeclaration, 0, pool.Intern("main"), constDef, xDecl, block);
            builder.Add((byte)PascalAstNodeKind.Program, 0, 0, method);

            var transformer = new PascalAstToHlirTransformer(pool);
            var hlir = transformer.Transform(builder.Build());

            int assignIdx = -1;
            for (int i = 0; i < hlir.Count; i++)
                if (hlir.GetKind(i) == (byte)HlirNodeKind.Assign) { assignIdx = i; break; }
            Assert.That(assignIdx, Is.GreaterThanOrEqualTo(0));

            var assignChildren = hlir.Children(assignIdx);
            Assert.That(hlir.GetKind(assignChildren[1]), Is.EqualTo((byte)HlirNodeKind.LiteralInt));
            Assert.That(pool.Resolve(hlir[assignChildren[1]].Payload), Is.EqualTo("251"));
        }
    }
}