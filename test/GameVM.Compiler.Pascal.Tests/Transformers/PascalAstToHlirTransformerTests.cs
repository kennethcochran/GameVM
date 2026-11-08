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

        [Test]
        public void UnaryMinusOfLiteralFoldsToSixteenBitTwosComplement()
        {
            // -N must fold to its 16-bit two's complement value (65536 - N),
            // not the 8-bit one: -5 is 65531, not 251. Today the fold masks
            // to 8 bits ((256 - (N & 0xFF)) & 0xFF), so -5 folds to 251 and
            // -256 folds to 0 — both silently wrong once values are 16-bit.
            var cases = new (string Literal, string Expected)[]
            {
                ("5", "65531"),
                ("1", "65535"),
                ("256", "65280"),
            };
            Assert.Multiple(() =>
            {
                foreach (var (literal, expected) in cases)
                {
                    string actual = FoldUnaryMinusOfLiteral(literal);
                    Assert.That(actual, Is.EqualTo(expected),
                        $"-{literal} must fold to its 16-bit two's complement value {expected}");
                }
            });
        }

        [Test]
        public void Transform_SmallLiteralAssignment_FoldsUnchanged()
        {
            // Guard: widening values to 16 bits must not disturb small
            // literals — x := 42 still folds to "42".
            Assert.That(AssignedLiteralValue("42"), Is.EqualTo("42"));
        }

        /// <summary>Builds `x := -N;` for a literal N, lowers it, and
        /// returns the folded value as decimal text.</summary>
        private static string FoldUnaryMinusOfLiteral(string literal)
        {
            var pool = new StringPool();
            var builder = new AstBuilder();
            uint xOffset = pool.Intern("x");
            uint intOffset = pool.Intern("integer");

            int target = builder.Add((byte)PascalAstNodeKind.Identifier, 0, xOffset);
            int literalNode = builder.Add((byte)PascalAstNodeKind.LiteralInt, 0, pool.Intern(literal));
            int unary = builder.Add((byte)PascalAstNodeKind.UnaryOp, 0, (uint)'-', literalNode);
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
            Assert.That(assignIdx, Is.GreaterThanOrEqualTo(0), "Assign node should be present");

            var assignChildren = hlir.Children(assignIdx);
            Assert.That(hlir.GetKind(assignChildren[1]), Is.EqualTo((byte)HlirNodeKind.LiteralInt),
                "-N must constant-fold to a LiteralInt");
            return pool.Resolve(hlir[assignChildren[1]].Payload);
        }

        /// <summary>Builds `x := N;` for a literal N, lowers it, and
        /// returns the assigned value as decimal text.</summary>
        private static string AssignedLiteralValue(string literal)
        {
            var pool = new StringPool();
            var builder = new AstBuilder();
            uint xOffset = pool.Intern("x");
            uint intOffset = pool.Intern("integer");

            int target = builder.Add((byte)PascalAstNodeKind.Identifier, 0, xOffset);
            int value = builder.Add((byte)PascalAstNodeKind.LiteralInt, 0, pool.Intern(literal));
            int assign = builder.Add((byte)PascalAstNodeKind.Assignment, 0, 0, target, value);

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
            Assert.That(assignIdx, Is.GreaterThanOrEqualTo(0), "Assign node should be present");

            var assignChildren = hlir.Children(assignIdx);
            Assert.That(hlir.GetKind(assignChildren[1]), Is.EqualTo((byte)HlirNodeKind.LiteralInt),
                "a literal assignment must lower to a LiteralInt");
            return pool.Resolve(hlir[assignChildren[1]].Payload);
        }

        private static AstTree BuildCallStatementAst(StringPool pool, string callee)
        {
            var builder = new AstBuilder();
            // Argument-less call statement: MethodCall carries the callee name
            // in its payload and has NO children (children are the arguments).
            int call = builder.Add((byte)PascalAstNodeKind.MethodCall, 0, pool.Intern(callee));
            int block = builder.Add((byte)PascalAstNodeKind.Block, 0, 0, call);
            int method = builder.Add((byte)PascalAstNodeKind.MethodDeclaration, 0, pool.Intern("main"), block);
            builder.Add((byte)PascalAstNodeKind.Program, 0, 0, method);
            return builder.Build();
        }

        [Test]
        public void Transform_ZeroArgProcedureCallStatement_ProducesCallNode()
        {
            // Regression: ProcessMethodCall used to return -1 for childless
            // MethodCall nodes, silently discarding argument-less calls.
            var pool = new StringPool();
            var transformer = new PascalAstToHlirTransformer(pool);
            var hlir = transformer.Transform(BuildCallStatementAst(pool, "TestProc"));

            int callIdx = -1;
            for (int i = 0; i < hlir.Count; i++)
                if (hlir.GetKind(i) == (byte)HlirNodeKind.Call) { callIdx = i; break; }

            Assert.That(callIdx, Is.GreaterThanOrEqualTo(0), "zero-arg procedure call must lower to an HLIR Call");
            Assert.That(pool.Resolve(hlir[callIdx].Payload), Is.EqualTo("TestProc"));
        }

        [Test]
        public void Transform_BareWritelnStatement_ProducesOutputNode()
        {
            // Bare writeln; (no arguments) must still reach the Output
            // lowering so console-less targets can reject it loudly.
            var pool = new StringPool();
            var transformer = new PascalAstToHlirTransformer(pool);
            var hlir = transformer.Transform(BuildCallStatementAst(pool, "writeln"));

            int outputIdx = -1;
            for (int i = 0; i < hlir.Count; i++)
                if (hlir.GetKind(i) == (byte)HlirNodeKind.Output) { outputIdx = i; break; }

            Assert.That(outputIdx, Is.GreaterThanOrEqualTo(0), "bare writeln; must lower to an HLIR Output node");
            Assert.That(hlir[outputIdx].Payload, Is.EqualTo(1u), "writeln payload is 1");
        }

        [Test]
        public void LessThanOfLiteralsFoldsToCorrectValue()
        {
            // The visitor encodes '<=' as 'L' and '>=' as 'G' (single-char
            // payloads); 8-bit unsigned semantics throughout. This slice
            // covers '<' only; '>', '<=', '>=' are separate slices.
            var cases = new (string Left, char Op, string Right, string Expected)[]
            {
                ("1", '<', "2", "1"),
                ("2", '<', "1", "0"),
            };
            Assert.Multiple(() =>
            {
                foreach (var (left, op, right, expected) in cases)
                {
                    string actual = FoldComparison(left, op, right);
                    Assert.That(actual, Is.EqualTo(expected),
                        $"{left} {OpName(op)} {right} must fold to {expected}");
                }
            });
        }

        private static string OpName(char op)
        {
            return op switch
            {
                '<' => "<",
                '>' => ">",
                'L' => "<=",
                'G' => ">=",
                _ => op.ToString(),
            };
        }

        /// <summary>Builds `x := left op right;` for literal operands, lowers
        /// it, and returns the folded value as decimal text.</summary>
        private static string FoldComparison(string left, char op, string right)
        {
            var pool = new StringPool();
            var builder = new AstBuilder();
            uint xOffset = pool.Intern("x");
            uint intOffset = pool.Intern("integer");

            int target = builder.Add((byte)PascalAstNodeKind.Identifier, 0, xOffset);
            int leftLit = builder.Add((byte)PascalAstNodeKind.LiteralInt, 0, pool.Intern(left));
            int rightLit = builder.Add((byte)PascalAstNodeKind.LiteralInt, 0, pool.Intern(right));
            int opNode = builder.Add((byte)PascalAstNodeKind.BinaryOp, 0, (uint)op);
            int comparison = builder.Add((byte)PascalAstNodeKind.BinaryOp, 0, 0, leftLit, rightLit, opNode);
            int assign = builder.Add((byte)PascalAstNodeKind.Assignment, 0, 0, target, comparison);

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
            Assert.That(assignIdx, Is.GreaterThanOrEqualTo(0), "Assign node should be present");

            var assignChildren = hlir.Children(assignIdx);
            Assert.That(hlir.GetKind(assignChildren[1]), Is.EqualTo((byte)HlirNodeKind.LiteralInt),
                "a literal ordering comparison must constant-fold to a LiteralInt");
            return pool.Resolve(hlir[assignChildren[1]].Payload);
        }

        [Test]
        public void RelationalComparisonOfVariables_PreservesEqualityOperator()
        {
            // Guard: a non-literal equality comparison still lowers to a
            // BinaryOp carrying '='; adding ordering comparisons must not
            // disturb equality.
            var pool = new StringPool();
            var builder = new AstBuilder();
            uint xOffset = pool.Intern("x");
            uint yOffset = pool.Intern("y");
            uint intOffset = pool.Intern("integer");

            int target = builder.Add((byte)PascalAstNodeKind.Identifier, 0, xOffset);
            int left = builder.Add((byte)PascalAstNodeKind.Identifier, 0, yOffset);
            int right = builder.Add((byte)PascalAstNodeKind.LiteralInt, 0, pool.Intern("0"));
            int opNode = builder.Add((byte)PascalAstNodeKind.BinaryOp, 0, (uint)'=');
            int comparison = builder.Add((byte)PascalAstNodeKind.BinaryOp, 0, 0, left, right, opNode);
            int assign = builder.Add((byte)PascalAstNodeKind.Assignment, 0, 0, target, comparison);

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
            Assert.That(hlir.GetKind(assignChildren[1]), Is.EqualTo((byte)HlirNodeKind.BinaryOp),
                "a non-literal comparison must lower to an HLIR BinaryOp, not fold");
            Assert.That(hlir[assignChildren[1]].Payload, Is.EqualTo((uint)'='),
                "the BinaryOp must carry the '=' operator");
        }
    }
}