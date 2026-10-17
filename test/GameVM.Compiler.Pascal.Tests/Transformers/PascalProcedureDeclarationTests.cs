using Antlr4.Runtime;
using GameVM.Compiler.Core.IR.Buffers;
using GameVM.Compiler.Core.IR.Hlir;
using GameVM.Compiler.Pascal.ANTLR;
using GameVM.Compiler.Pascal.Ast;
using GameVM.Compiler.Pascal.Transformers;

namespace GameVM.Compiler.Pascal.Tests.Transformers
{
    /// <summary>
    /// Acceptance tests for user-defined, zero-argument Pascal procedure
    /// declarations (slice: Pascal source -> AST -> semantics -> HLIR).
    /// A declared procedure must reach HLIR as a nested FunctionDeclaration
    /// carrying its body and an implicit return; before this slice the
    /// visitor dropped declarations entirely and the body never existed
    /// downstream.
    /// </summary>
    [TestFixture]
    public class PascalProcedureDeclarationTests
    {
        private const string ProcedureProgram =
            "program Probe;\n" +
            "var x: Integer;\n" +
            "procedure Bump;\n" +
            "begin\n" +
            "  x := 42;\n" +
            "end;\n" +
            "begin\n" +
            "  x := 0;\n" +
            "  Bump;\n" +
            "end.";

        private static AstTree BuildAst(string source, StringPool pool)
        {
            var inputStream = new AntlrInputStream(source);
            var lexer = new PascalLexer(inputStream);
            var tokenStream = new CommonTokenStream(lexer);
            var parser = new PascalParser(tokenStream);
            var context = parser.program();
            var visitor = new PascalToAstVisitor(new AstBuilder(), pool);
            visitor.Visit(context);
            return visitor.BuildTree();
        }

        private static List<int> FindByKind(HlirTree hlir, HlirNodeKind kind)
        {
            var found = new List<int>();
            for (int i = 0; i < hlir.Count; i++)
            {
                if (hlir.GetKind(i) == (byte)kind)
                    found.Add(i);
            }
            return found;
        }

        private static List<int> Subtree(HlirTree hlir, int root)
        {
            var result = new List<int>();
            var stack = new Stack<int>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                int current = stack.Pop();
                result.Add(current);
                var children = hlir.Children(current);
                for (int i = children.Length - 1; i >= 0; i--)
                    stack.Push(children[i]);
            }
            return result;
        }

        [Test]
        public void Visitor_ProcedureDeclaration_EmitsMethodDeclarationNamedForProcedure()
        {
            var pool = new StringPool();
            var ast = BuildAst(ProcedureProgram, pool);

            int methodDeclIdx = -1;
            for (int i = 0; i < ast.Count; i++)
            {
                if ((PascalAstNodeKind)ast.GetKind(i) == PascalAstNodeKind.MethodDeclaration)
                    methodDeclIdx = i;
            }

            Assert.Multiple(() =>
            {
                Assert.That(methodDeclIdx, Is.GreaterThanOrEqualTo(0),
                    "a declared procedure must appear in the AST as a MethodDeclaration");
                Assert.That(pool.Resolve(ast[methodDeclIdx].Payload), Is.EqualTo("bump"));
            });
        }

        [Test]
        public void ParseToHlir_ProcedureDeclaration_ProducesNestedFunctionDeclarationWithBodyAndReturn()
        {
            var frontend = new PascalFrontend();
            var result = frontend.ParseToHlir(ProcedureProgram);
            var hlir = result.Hlir;

            var functions = FindByKind(hlir, HlirNodeKind.FunctionDeclaration);
            int bumpIdx = functions.First(i =>
                frontend.StringPool!.Resolve(hlir[i].Payload) == "bump");
            var bumpSubtree = Subtree(hlir, bumpIdx).ToList();

            Assert.Multiple(() =>
            {
                Assert.That(functions, Has.Count.EqualTo(2),
                    "HLIR must contain the program entry plus the declared procedure");
                Assert.That(bumpSubtree.Any(i => hlir.GetKind(i) == (byte)HlirNodeKind.Assign),
                    Is.True, "the procedure body (x := 42) must be inside the nested FunctionDeclaration");
                Assert.That(bumpSubtree.Any(i => hlir.GetKind(i) == (byte)HlirNodeKind.Return),
                    Is.True, "a procedure body must end in an implicit return so HLIR->MLIR flags it CalleeReturn");
            });
        }

        [Test]
        public void ParseToHlir_ProcedureDeclaration_MainBodyCallsProcedure()
        {
            var frontend = new PascalFrontend();
            var result = frontend.ParseToHlir(ProcedureProgram);
            var hlir = result.Hlir;

            var calls = FindByKind(hlir, HlirNodeKind.Call);
            Assert.That(calls.Any(i => frontend.StringPool!.Resolve(hlir[i].Payload) == "bump"),
                Is.True, "the statement call in the main body must lower to an HLIR Call naming Bump");
        }

        [Test]
        public void ParseToHlir_ProcedureBodyUsingGlobalVariable_HasNoSemanticErrors()
        {
            var frontend = new PascalFrontend();
            var result = frontend.ParseToHlir(ProcedureProgram);

            Assert.Multiple(() =>
            {
                Assert.That(result.Hlir.Count, Is.GreaterThan(0));
                Assert.That(frontend.LastParseErrors, Is.Null.Or.Empty,
                    "assigning to a program-level variable inside a procedure body is valid Pascal");
            });
        }

        [Test]
        public void ParseToHlir_ProcedureLocalConstantShadowingGlobal_MainBodyKeepsGlobalValue()
        {
            // A procedure-local const must not leak into the enclosing scope:
            // the main body's `x := k` folds the PROGRAM-level k (1), the
            // procedure body's folds its own k (2).
            var frontend = new PascalFrontend();
            string source =
                "program Probe;\n" +
                "const k = 1;\n" +
                "var x: Integer;\n" +
                "procedure Bump;\n" +
                "const k = 2;\n" +
                "begin\n" +
                "  x := k;\n" +
                "end;\n" +
                "begin\n" +
                "  x := k;\n" +
                "  Bump;\n" +
                "end.";

            var result = frontend.ParseToHlir(source);
            var hlir = result.Hlir;
            Assert.That(hlir.Count, Is.GreaterThan(0), "program must lower");

            var functions = FindByKind(hlir, HlirNodeKind.FunctionDeclaration);
            int bumpIdx = functions.First(i =>
                frontend.StringPool!.Resolve(hlir[i].Payload) == "bump");
            var bumpNodes = Subtree(hlir, bumpIdx);

            uint FoldedAssignValue(List<int> scope)
            {
                int assignIdx = scope.First(i => hlir.GetKind(i) == (byte)HlirNodeKind.Assign);
                int valueIdx = hlir.Children(assignIdx)[1];
                Assert.That(hlir.GetKind(valueIdx), Is.EqualTo((byte)HlirNodeKind.LiteralInt),
                    "a named-constant assignment must fold to a literal");
                return hlir[valueIdx].Payload;
            }

            Assert.That(FoldedAssignValue(bumpNodes), Is.EqualTo(2u),
                "inside Bump, k is the procedure-local constant");
            var mainNodes = Enumerable.Range(0, hlir.Count).Except(bumpNodes).ToList();
            Assert.That(FoldedAssignValue(mainNodes), Is.EqualTo(1u),
                "in the main body, k is the program-level constant — the procedure's k must not leak");
        }

        [Test]
        public void ParseToHlir_ProcedureLocalConstant_NotVisibleInMainBodySemantics()
        {
            var frontend = new PascalFrontend();
            string source =
                "program Probe;\n" +
                "var x: Integer;\n" +
                "procedure Bump;\n" +
                "const k = 2;\n" +
                "begin\n" +
                "  x := k;\n" +
                "end;\n" +
                "begin\n" +
                "  x := k;\n" +
                "end.";

            var result = frontend.ParseToHlir(source);

            Assert.Multiple(() =>
            {
                Assert.That(result.Hlir.Count, Is.Zero,
                    "using a procedure-local constant in the main body is a semantic error");
                Assert.That(frontend.LastParseErrors,
                    Has.Some.Contains("Undefined variable 'k'"));
            });
        }

        [Test]
        public void ParseToHlir_MixedCaseProcedureCall_ResolvesToDeclaration()
        {
            // Pascal identifiers are case-insensitive: `bump;` must resolve to
            // the procedure declared as `Bump`, i.e. the HLIR Call and the
            // FunctionDeclaration carry the same name pool offset.
            var frontend = new PascalFrontend();
            string source =
                "program Probe;\n" +
                "var x: Integer;\n" +
                "procedure Bump;\n" +
                "begin\n" +
                "  x := 42;\n" +
                "end;\n" +
                "begin\n" +
                "  bump;\n" +
                "end.";

            var result = frontend.ParseToHlir(source);
            var hlir = result.Hlir;

            var functions = FindByKind(hlir, HlirNodeKind.FunctionDeclaration);
            int bumpIdx = functions.First(i =>
                string.Equals(frontend.StringPool!.Resolve(hlir[i].Payload), "Bump",
                    StringComparison.OrdinalIgnoreCase));
            var calls = FindByKind(hlir, HlirNodeKind.Call);
            Assert.Multiple(() =>
            {
                Assert.That(calls, Is.Not.Empty, "the call must lower");
                Assert.That(hlir[calls[0]].Payload, Is.EqualTo(hlir[bumpIdx].Payload),
                    "call site and declaration must share one pool offset, or the label never resolves");
            });
        }

        [Test]
        public void ParseToHlir_ProcedureBodyUsingUndeclaredVariable_ReportsUndefinedVariable()
        {
            var frontend = new PascalFrontend();
            string source =
                "program Probe;\n" +
                "procedure Bump;\n" +
                "begin\n" +
                "  y := 42;\n" +
                "end;\n" +
                "begin\n" +
                "  Bump;\n" +
                "end.";

            var result = frontend.ParseToHlir(source);

            Assert.Multiple(() =>
            {
                Assert.That(result.Hlir.Count, Is.Zero,
                    "a semantic violation inside a procedure body must still reject the program");
                Assert.That(frontend.LastParseErrors,
                    Has.Some.Contains("Undefined variable 'y'"));
            });
        }
    }
}
