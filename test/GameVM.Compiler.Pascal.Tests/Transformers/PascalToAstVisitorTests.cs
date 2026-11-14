using GameVM.Compiler.Pascal.Ast;

using GameVM.Compiler.Pascal.ANTLR;
using GameVM.Compiler.Pascal.Transformers;
using GameVM.Compiler.Core.IR.Buffers;
using Antlr4.Runtime;

namespace GameVM.Compiler.Pascal.Tests.Transformers
{
    [TestFixture]
    public class PascalToAstVisitorTests
    {
        private static PascalParser.ProgramContext Parse(string code)
        {
            var inputStream = new AntlrInputStream(code);
            var lexer = new PascalLexer(inputStream);
            var tokenStream = new CommonTokenStream(lexer);
            var parser = new PascalParser(tokenStream);
            return parser.program();
        }

        [Test]
        public void Visitor_HandlesSimpleProgram()
        {
            string code = @"
                program Test;
                begin
                end.
            ";

            var context = Parse(code);
            var builder = new AstBuilder();
            var visitor = new PascalToAstVisitor(builder, new StringPool());
            visitor.Visit(context);
            AstTree tree = visitor.BuildTree();
            Assert.That(tree.Count, Is.GreaterThan(0));
            Console.WriteLine($"Node count: {tree.Count}");
            for (int i = 0; i < tree.Count; i++)
            {
                var node = tree[i];
                var children = tree.Children(i);
                Console.WriteLine($"  [{i}] Kind: {(PascalAstNodeKind)node.Kind}, Flags: {node.Flags}, Payload: {node.Payload}, FirstChild: {node.FirstChild}, ChildCount: {node.ChildCount}");
                if (children.Length > 0)
                {
                    Console.Write("    Children: ");
                    for (int c = 0; c < children.Length; c++)
                    {
                        Console.Write($"{(PascalAstNodeKind)tree[children[c]].Kind} ");
                    }
                    Console.WriteLine();
                }
            }
        }

        [Test]
        public void Visitor_HandlesVariableDeclaration()
        {
            string code = @"
                program Test;
                var x: integer;
                begin
                end.
            ";

            var context = Parse(code);
            var builder = new AstBuilder();
            var visitor = new PascalToAstVisitor(builder, new StringPool());
            visitor.Visit(context);

            AstTree tree = visitor.BuildTree();
            Assert.That(tree.Count, Is.GreaterThan(0));

            // Check for METHOD_DECLARATION (kind 10) — Program node wraps the method
            bool foundMethodDecl = false;
            for (int i = 0; i < tree.Count; i++)
            {
                if (tree.GetKind(i) == (byte)PascalAstNodeKind.MethodDeclaration || tree.GetKind(i) == (byte)PascalAstNodeKind.Program)
                {
                    foundMethodDecl = true;
                    break;
                }
            }
            Assert.That(foundMethodDecl, Is.True, "Method declaration should be present");

            Console.WriteLine($"Node count: {tree.Count}");
            for (int i = 0; i < tree.Count; i++)
            {
                var node = tree[i];
                var children = tree.Children(i);
                Console.WriteLine($"  [{i}] Kind: {(PascalAstNodeKind)node.Kind}, Flags: {node.Flags}, Payload: {node.Payload}, Children: {children.Length}");
            }
        }

        [Test]
        public void Visitor_HandlesAssignmentStatement()
        {
            string code = @"
                program Test;
                var x: integer;
                begin
                    x := 42;
                end.
            ";

            var context = Parse(code);
            var builder = new AstBuilder();
            var visitor = new PascalToAstVisitor(builder, new StringPool());
            visitor.Visit(context);

            AstTree tree = visitor.BuildTree();
            Assert.That(tree.Count, Is.GreaterThan(0));

            // Check for ASSIGNMENT (kind 7)
            bool foundAssignment = false;
            for (int i = 0; i < tree.Count; i++)
            {
                if (tree.GetKind(i) == (byte)PascalAstNodeKind.Assignment)
                {
                    foundAssignment = true;
                    var node = tree[i];
                    Assert.That(node.ChildCount, Is.EqualTo(2), "Assignment should have 2 children (target, value)");
                    break;
                }
            }
            Assert.That(foundAssignment, Is.True, "Assignment should be present in the method body");

            Console.WriteLine($"Node count: {tree.Count}");
            for (int i = 0; i < tree.Count; i++)
            {
                var node = tree[i];
                var children = tree.Children(i);
                Console.WriteLine($"  [{i}] Kind: {(PascalAstNodeKind)node.Kind}, Flags: {node.Flags}, Payload: {node.Payload}, Children: {children.Length}");
            }
        }

        [Test]
        public void Visitor_HandlesIfStatement()
        {
            string code = @"
                program Test;
                var x: integer;
                begin
                    if x > 0 then
                        x := 1
                    else
                        x := -1;
                end.
            ";

            var context = Parse(code);
            var builder = new AstBuilder();
            var visitor = new PascalToAstVisitor(builder, new StringPool());
            visitor.Visit(context);

            AstTree tree = visitor.BuildTree();
            Assert.That(tree.Count, Is.GreaterThan(0));

            // Check for IF_STATEMENT (kind 12)
            bool foundIf = false;
            for (int i = 0; i < tree.Count; i++)
            {
                if (tree.GetKind(i) == (byte)PascalAstNodeKind.IfStatement)
                {
                    foundIf = true;
                    var node = tree[i];
                    Assert.That(node.ChildCount >= 2, "If statement should have at least 2 children (condition, then)");
                    break;
                }
            }
            Assert.That(foundIf, Is.True, "If statement should be present in the method body");

            Console.WriteLine($"Node count: {tree.Count}");
            for (int i = 0; i < tree.Count; i++)
            {
                var node = tree[i];
                var children = tree.Children(i);
                Console.WriteLine($"  [{i}] Kind: {(PascalAstNodeKind)node.Kind}, Flags: {node.Flags}, Payload: {node.Payload}, Children: {children.Length}");
            }
        }

        [Test]
        public void Visitor_HandlesWhileLoop()
        {
            string code = @"
                program Test;
                var x: integer;
                begin
                    while x > 0 do
                        x := x - 1;
                end.
            ";

            var context = Parse(code);
            var builder = new AstBuilder();
            var visitor = new PascalToAstVisitor(builder, new StringPool());
            visitor.Visit(context);

            AstTree tree = visitor.BuildTree();
            Assert.That(tree.Count, Is.GreaterThan(0));

            // Check for WHILE_STATEMENT (kind 13)
            bool foundWhile = false;
            for (int i = 0; i < tree.Count; i++)
            {
                if (tree.GetKind(i) == (byte)PascalAstNodeKind.WhileStatement)
                {
                    foundWhile = true;
                    var node = tree[i];
                    Assert.That(node.ChildCount == 2, "While statement should have 2 children (condition, body)");
                    break;
                }
            }
            Assert.That(foundWhile, Is.True, "While statement should be present in the method body");

            Console.WriteLine($"Node count: {tree.Count}");
            for (int i = 0; i < tree.Count; i++)
            {
                var node = tree[i];
                var children = tree.Children(i);
                Console.WriteLine($"  [{i}] Kind: {(PascalAstNodeKind)node.Kind}, Flags: {node.Flags}, Payload: {node.Payload}, Children: {children.Length}");
            }
        }

        [Test]
        public void Visitor_HandlesLiteralTypes()
        {
            string code = @"
                program Test;
                var x: integer;
                var s: string;
                begin
                    x := 42;
                    s := 'hello';
                end.
            ";

            var context = Parse(code);
            var builder = new AstBuilder();
            var visitor = new PascalToAstVisitor(builder, new StringPool());
            visitor.Visit(context);

            AstTree tree = visitor.BuildTree();
            Assert.That(tree.Count, Is.GreaterThan(0));

            // Check for literal types
            bool foundIntLiteral = false;
            bool foundStringLiteral = false;
            for (int i = 0; i < tree.Count; i++)
            {
                var kind = (PascalAstNodeKind)tree.GetKind(i);
                if (kind == PascalAstNodeKind.LiteralInt)
                    foundIntLiteral = true;
                else if (kind == PascalAstNodeKind.LiteralString)
                    foundStringLiteral = true;
            }
            Assert.That(foundIntLiteral, Is.True, "Integer literal should be present");
            Assert.That(foundStringLiteral, Is.True, "String literal should be present");
            for (int i = 0; i < tree.Count; i++)
            {
                var node = tree[i];
                var children = tree.Children(i);
                Console.WriteLine($"  [{i}] Kind: {(PascalAstNodeKind)node.Kind}, Flags: {node.Flags}, Payload: {node.Payload}, Children: {children.Length}");
            }
        }
        [Test]
        public void TrueAndFalseProduceLiteralBoolWithPayloadOneAndZero()
        {
            string code = @"
                program Test;
                var b: boolean;
                begin
                    b := true;
                    b := false;
                end.
            ";

            var context = Parse(code);
            var builder = new AstBuilder();
            var visitor = new PascalToAstVisitor(builder, new StringPool());
            visitor.Visit(context);
            AstTree tree = visitor.BuildTree();

            // Boolean literals are values, not names: true is 1, false is 0.
            // Today the visitor drops them in expression position — VisitFactor
            // has no bool_ case, so each literal becomes a Nop and no
            // LiteralBool node exists at all. (VisitBool_ itself interns
            // "true"/"false" as strings, but nothing reaches it from an
            // expression.) The fix must both visit bool_ as a factor and
            // carry 1 for true and 0 for false as the payload.
            var boolPayloads = new List<uint>();
            for (int i = 0; i < tree.Count; i++)
            {
                if (tree.GetKind(i) == (byte)PascalAstNodeKind.LiteralBool)
                    boolPayloads.Add(tree[i].Payload);
            }
            using (Assert.EnterMultipleScope())
            {
                Assert.That(boolPayloads, Has.Count.EqualTo(2), "exactly two boolean literals expected");
                Assert.That(boolPayloads[0], Is.EqualTo(1u), "true must carry payload 1");
                Assert.That(boolPayloads[1], Is.Zero, "false must carry payload 0");
            }
        }

        [Test]
        public void Visitor_EmitsUnaryOpForUnaryMinus()
        {
            string code = @"
                program Test;
                var x, y: integer;
                begin
                    x := -y;
                end.
            ";

            var context = Parse(code);
            var builder = new AstBuilder();
            var pool = new StringPool();
            var visitor = new PascalToAstVisitor(builder, pool);
            visitor.Visit(context);
            AstTree tree = visitor.BuildTree();

            // The assignment must survive lowering (previously the whole
            // statement was silently dropped).
            bool foundAssignment = false;
            for (int i = 0; i < tree.Count; i++)
            {
                if (tree.GetKind(i) == (byte)PascalAstNodeKind.Assignment)
                {
                    foundAssignment = true;
                    break;
                }
            }
            Assert.That(foundAssignment, Is.True, "x := -y must not be dropped");

            // Exactly one UnaryOp node: payload '-', exactly one child (the operand).
            uint yOffset = pool.Intern("y");
            int unaryCount = 0;
            int yOperandCount = 0;
            for (int i = 0; i < tree.Count; i++)
            {
                if (tree.GetKind(i) == (byte)PascalAstNodeKind.UnaryOp)
                {
                    unaryCount++;
                    Assert.That(tree[i].Payload, Is.EqualTo((uint)'-'), "UnaryOp payload must be the '-' op char");
                    Assert.That(tree[i].ChildCount, Is.EqualTo(1), "UnaryOp must wrap exactly one operand");
                    var operand = tree[tree.Children(i)[0]];
                    Assert.That((PascalAstNodeKind)operand.Kind, Is.EqualTo(PascalAstNodeKind.Identifier));
                    Assert.That(operand.Payload, Is.EqualTo(yOffset));
                    yOperandCount++;
                }
            }
            Assert.That(unaryCount, Is.EqualTo(1), "exactly one UnaryOp node expected");
            Assert.That(yOperandCount, Is.EqualTo(1), "the factor must be visited exactly once (no duplicate subtree)");
        }

        [Test]
        public void NotEmitsUnaryOpNotSingleChildBinaryOp()
        {
            string code = @"
                program Test;
                var x, y: integer;
                begin
                    x := not y;
                end.
            ";

            var context = Parse(code);
            var builder = new AstBuilder();
            var pool = new StringPool();
            var visitor = new PascalToAstVisitor(builder, pool);
            visitor.Visit(context);
            AstTree tree = visitor.BuildTree();

            // The assignment must survive lowering (previously the whole
            // statement was silently dropped).
            bool foundAssignment = false;
            for (int i = 0; i < tree.Count; i++)
            {
                if (tree.GetKind(i) == (byte)PascalAstNodeKind.Assignment)
                {
                    foundAssignment = true;
                    break;
                }
            }
            Assert.That(foundAssignment, Is.True, "x := not y must not be dropped");

            // Exactly one UnaryOp node: payload '!', exactly one child (the operand).
            // Today `not` is emitted as a BinaryOp with payload '!' and a
            // single child, a shape the middle end rejects — so the whole
            // enclosing statement is silently dropped. No BinaryOp node may
            // carry the '!' payload.
            uint yOffset = pool.Intern("y");
            int unaryCount = 0;
            int yOperandCount = 0;
            using (Assert.EnterMultipleScope())
            {
                for (int i = 0; i < tree.Count; i++)
                {
                    if (tree.GetKind(i) == (byte)PascalAstNodeKind.UnaryOp)
                    {
                        unaryCount++;
                        Assert.That(tree[i].Payload, Is.EqualTo((uint)'!'), "UnaryOp payload must be the '!' op char");
                        Assert.That(tree[i].ChildCount, Is.EqualTo(1), "UnaryOp must wrap exactly one operand");
                        var operand = tree[tree.Children(i)[0]];
                        Assert.That((PascalAstNodeKind)operand.Kind, Is.EqualTo(PascalAstNodeKind.Identifier));
                        Assert.That(operand.Payload, Is.EqualTo(yOffset));
                        yOperandCount++;
                    }
                    if (tree.GetKind(i) == (byte)PascalAstNodeKind.BinaryOp)
                    {
                        Assert.That(tree[i].Payload, Is.Not.EqualTo((uint)'!'),
                            "a BinaryOp must never carry the '!' payload — `not` is a unary op, not a two-operand one");
                    }
                }
                Assert.That(unaryCount, Is.EqualTo(1), "exactly one UnaryOp node expected");
                Assert.That(yOperandCount, Is.EqualTo(1), "the factor must be visited exactly once (no duplicate subtree)");
            }
        }

        [Test]
        public void Visitor_EmitsUnaryOpForNegativeLiteral()
        {
            string code = @"
                program Test;
                var x: integer;
                begin
                    x := -5;
                end.
            ";

            var context = Parse(code);
            var builder = new AstBuilder();
            var visitor = new PascalToAstVisitor(builder, new StringPool());
            visitor.Visit(context);
            AstTree tree = visitor.BuildTree();

            bool foundUnary = false;
            for (int i = 0; i < tree.Count; i++)
            {
                if (tree.GetKind(i) == (byte)PascalAstNodeKind.UnaryOp)
                {
                    foundUnary = true;
                    Assert.That(tree[i].ChildCount, Is.EqualTo(1));
                    var operand = tree[tree.Children(i)[0]];
                    Assert.That((PascalAstNodeKind)operand.Kind, Is.EqualTo(PascalAstNodeKind.LiteralInt));
                    break;
                }
            }
            Assert.That(foundUnary, Is.True, "x := -5 must produce a UnaryOp node");
        }

        private static AstTree BuildTree(string code, out StringPool pool)
        {
            var context = Parse(code);
            var builder = new AstBuilder();
            pool = new StringPool();
            var visitor = new PascalToAstVisitor(builder, pool);
            visitor.Visit(context);
            return visitor.BuildTree();
        }

        // A type definition binds a name to its underlying type: a
        // TypeDefinition node with an Identifier child carrying the name.
        // (A bare TypeDefinition leaf is a type *reference*, the shape
        // variable declarations already emit; only a definition has
        // children.)
        private static int FindTypeDefinition(AstTree tree, StringPool pool, string name)
        {
            for (int i = 0; i < tree.Count; i++)
            {
                if (tree.GetKind(i) != (byte)PascalAstNodeKind.TypeDefinition)
                    continue;
                foreach (int childIdx in tree.Children(i))
                {
                    if (tree.GetKind(childIdx) == (byte)PascalAstNodeKind.Identifier &&
                        string.Equals(pool.Resolve(tree[childIdx].Payload), name, StringComparison.OrdinalIgnoreCase))
                        return i;
                }
            }
            return -1;
        }

        private static bool SubtreeContainsIdentifier(AstTree tree, StringPool pool, int rootIdx, string name)
        {
            if (tree.GetKind(rootIdx) == (byte)PascalAstNodeKind.Identifier &&
                string.Equals(pool.Resolve(tree[rootIdx].Payload), name, StringComparison.OrdinalIgnoreCase))
                return true;
            foreach (int childIdx in tree.Children(rootIdx))
            {
                if (SubtreeContainsIdentifier(tree, pool, childIdx, name))
                    return true;
            }
            return false;
        }

        private static bool SubtreeContainsLiteralInt(AstTree tree, int rootIdx, uint value)
        {
            if (tree.GetKind(rootIdx) == (byte)PascalAstNodeKind.LiteralInt && tree[rootIdx].Payload == value)
                return true;
            foreach (int childIdx in tree.Children(rootIdx))
            {
                if (SubtreeContainsLiteralInt(tree, childIdx, value))
                    return true;
            }
            return false;
        }

        [Test]
        public void TypeDeclarationProducesTypeDefinition()
        {
            string code = @"
                program Test;
                type TScore = Integer;
                var s: TScore;
                begin
                    s := 42;
                end.
            ";

            AstTree tree = BuildTree(code, out var pool);
            int defIdx = FindTypeDefinition(tree, pool, "tscore");

            bool bindsUnderlyingType = false;
            if (defIdx >= 0)
            {
                foreach (int childIdx in tree.Children(defIdx))
                {
                    if (tree.GetKind(childIdx) == (byte)PascalAstNodeKind.TypeDefinition &&
                        string.Equals(pool.Resolve(tree[childIdx].Payload), "integer", StringComparison.OrdinalIgnoreCase))
                        bindsUnderlyingType = true;
                }
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(defIdx, Is.GreaterThanOrEqualTo(0),
                    "a type section must produce a TypeDefinition binding the declared name — today the type section is never visited and the definition is dropped");
                Assert.That(bindsUnderlyingType, Is.True,
                    "the TypeDefinition must carry its underlying type ('integer') as a type-reference child");
            }
        }

        [Test]
        public void EnumeratedTypeDeclarationProducesTypeDefinitionWithMembers()
        {
            string code = @"
                program Test;
                type TColor = (Red, Green, Blue);
                var c: TColor;
                begin
                end.
            ";

            AstTree tree = BuildTree(code, out var pool);
            int defIdx = FindTypeDefinition(tree, pool, "tcolor");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(defIdx, Is.GreaterThanOrEqualTo(0),
                    "an enumerated type declaration must produce a TypeDefinition binding 'tcolor'");
                Assert.That(defIdx >= 0 && SubtreeContainsIdentifier(tree, pool, defIdx, "red"), Is.True,
                    "the definition must carry the 'Red' member");
                Assert.That(defIdx >= 0 && SubtreeContainsIdentifier(tree, pool, defIdx, "green"), Is.True,
                    "the definition must carry the 'Green' member");
                Assert.That(defIdx >= 0 && SubtreeContainsIdentifier(tree, pool, defIdx, "blue"), Is.True,
                    "the definition must carry the 'Blue' member");
            }
        }

        [Test]
        public void SubrangeTypeDeclarationProducesTypeDefinitionWithBounds()
        {
            string code = @"
                program Test;
                type TDigit = 0..9;
                var d: TDigit;
                begin
                end.
            ";

            AstTree tree = BuildTree(code, out var pool);
            int defIdx = FindTypeDefinition(tree, pool, "tdigit");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(defIdx, Is.GreaterThanOrEqualTo(0),
                    "a subrange type declaration must produce a TypeDefinition binding 'tdigit'");
                Assert.That(defIdx >= 0 && SubtreeContainsLiteralInt(tree, defIdx, 0), Is.True,
                    "the definition must carry the lower bound 0");
                Assert.That(defIdx >= 0 && SubtreeContainsLiteralInt(tree, defIdx, 9), Is.True,
                    "the definition must carry the upper bound 9");
            }
        }

        [Test]
        public void ProgramWithoutTypeSectionProducesNoTypeDefinition()
        {
            string code = @"
                program Test;
                var x: integer;
                begin
                    x := 1;
                end.
            ";

            AstTree tree = BuildTree(code, out var pool);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(FindTypeDefinition(tree, pool, "integer"), Is.EqualTo(-1),
                    "a program with no type section must not gain a type definition");
                Assert.That(FindTypeDefinition(tree, pool, "x"), Is.EqualTo(-1),
                    "a variable declaration is a type reference, not a type definition");
            }
        }
    }
}