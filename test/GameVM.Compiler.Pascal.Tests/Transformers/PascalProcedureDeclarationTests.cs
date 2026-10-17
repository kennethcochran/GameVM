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

            Assert.That(methodDeclIdx, Is.GreaterThanOrEqualTo(0),
                "a declared procedure must appear in the AST as a MethodDeclaration");
            Assert.That(pool.Resolve(ast[methodDeclIdx].Payload), Is.EqualTo("Bump"));
        }

        [Test]
        public void ParseToHlir_ProcedureDeclaration_ProducesNestedFunctionDeclarationWithBodyAndReturn()
        {
            var frontend = new PascalFrontend();
            var result = frontend.ParseToHlir(ProcedureProgram);
            var hlir = result.Hlir;

            var functions = FindByKind(hlir, HlirNodeKind.FunctionDeclaration);
            Assert.That(functions.Count, Is.EqualTo(2),
                "HLIR must contain the program entry plus the declared procedure");

            int bumpIdx = functions.First(i =>
                frontend.StringPool!.Resolve(hlir[i].Payload) == "Bump");
            var bumpSubtree = Subtree(hlir, bumpIdx).ToList();

            Assert.That(bumpSubtree.Any(i => hlir.GetKind(i) == (byte)HlirNodeKind.Assign),
                Is.True, "the procedure body (x := 42) must be inside the nested FunctionDeclaration");
            Assert.That(bumpSubtree.Any(i => hlir.GetKind(i) == (byte)HlirNodeKind.Return),
                Is.True, "a procedure body must end in an implicit return so HLIR->MLIR flags it CalleeReturn");
        }

        [Test]
        public void ParseToHlir_ProcedureDeclaration_MainBodyCallsProcedure()
        {
            var frontend = new PascalFrontend();
            var result = frontend.ParseToHlir(ProcedureProgram);
            var hlir = result.Hlir;

            var calls = FindByKind(hlir, HlirNodeKind.Call);
            Assert.That(calls.Any(i => frontend.StringPool!.Resolve(hlir[i].Payload) == "Bump"),
                Is.True, "the statement call in the main body must lower to an HLIR Call naming Bump");
        }

        [Test]
        public void ParseToHlir_ProcedureBodyUsingGlobalVariable_HasNoSemanticErrors()
        {
            var frontend = new PascalFrontend();
            var result = frontend.ParseToHlir(ProcedureProgram);

            Assert.That(result.Hlir.Count, Is.GreaterThan(0));
            Assert.That(frontend.LastParseErrors, Is.Null.Or.Empty,
                "assigning to a program-level variable inside a procedure body is valid Pascal");
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

            Assert.That(result.Hlir.Count, Is.EqualTo(0),
                "a semantic violation inside a procedure body must still reject the program");
            Assert.That(frontend.LastParseErrors,
                Has.Some.Contains("Undefined variable 'y'"));
        }
    }
}
