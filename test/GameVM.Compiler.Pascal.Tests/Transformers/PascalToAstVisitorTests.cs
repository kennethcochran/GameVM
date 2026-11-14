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
        public void CaseOfProducesDispatchNodeNotNop()
        {
            // `case...of` currently falls through VisitStatement to a Nop:
            // the selector and every branch vanish with no error anywhere.
            // The visitor must produce a Case node carrying the selector, one
            // CaseBranch per case list element (each holding its constant
            // labels, then its statement), and the else statements as a
            // trailing Block — the shape the grammar itself describes
            // (`caseListElement : constList COLON statement`, optional
            // `(SEMI ELSE statements)?`). A branch with several labels
            // (`1, 2: ...`) carries one LiteralInt per label.
            string code = @"
                program Test;
                var x: integer;
                begin
                    x := 2;
                    case x of
                        1, 2: x := 10;
                        3: x := 20;
                    else
                        x := 30
                    end;
                end.
            ";

            var context = Parse(code);
            var builder = new AstBuilder();
            var pool = new StringPool();
            var visitor = new PascalToAstVisitor(builder, pool);
            visitor.Visit(context);

            AstTree tree = visitor.BuildTree();
            Assert.That(tree.Count, Is.GreaterThan(0));

            int caseIdx = -1;
            for (int i = 0; i < tree.Count; i++)
            {
                if (tree.GetKind(i) == (byte)PascalAstNodeKind.Case)
                {
                    caseIdx = i;
                    break;
                }
            }
            Assert.That(caseIdx, Is.GreaterThanOrEqualTo(0),
                "Case statement should be present in the method body, not dropped to Nop");

            var caseNode = tree[caseIdx];
            Assert.That(caseNode.ChildCount, Is.EqualTo(4),
                "Case should have 4 children (selector, two branches, else block)");

            var caseChildren = tree.Children(caseIdx);
            int selectorIdx = caseChildren[0];
            using (Assert.EnterMultipleScope())
            {
                Assert.That(tree.GetKind(selectorIdx), Is.EqualTo((byte)PascalAstNodeKind.Identifier),
                    "Case selector must be the identifier expression");
                Assert.That(pool.Resolve(tree[selectorIdx].Payload), Is.EqualTo("x"),
                    "Case selector must be x");
            }

            // First branch: labels 1 and 2, then the assignment.
            int branch12Idx = caseChildren[1];
            Assert.That(tree.GetKind(branch12Idx), Is.EqualTo((byte)PascalAstNodeKind.CaseBranch),
                "Each case list element must be a CaseBranch node");
            var branch12 = tree[branch12Idx];
            Assert.That(branch12.ChildCount, Is.EqualTo(3),
                "A branch with two labels must have 3 children (label, label, statement)");
            var branch12Children = tree.Children(branch12Idx);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(tree.GetKind(branch12Children[0]), Is.EqualTo((byte)PascalAstNodeKind.LiteralInt));
                Assert.That(pool.Resolve(tree[branch12Children[0]].Payload), Is.EqualTo("1"),
                    "First label of the first branch must be 1");
                Assert.That(tree.GetKind(branch12Children[1]), Is.EqualTo((byte)PascalAstNodeKind.LiteralInt));
                Assert.That(pool.Resolve(tree[branch12Children[1]].Payload), Is.EqualTo("2"),
                    "Second label of the first branch must be 2");
                Assert.That(tree.GetKind(branch12Children[2]), Is.EqualTo((byte)PascalAstNodeKind.Assignment),
                    "The last child of a branch must be its statement");
            }

            // Second branch: label 3, then the assignment.
            int branch3Idx = caseChildren[2];
            Assert.That(tree.GetKind(branch3Idx), Is.EqualTo((byte)PascalAstNodeKind.CaseBranch));
            var branch3 = tree[branch3Idx];
            Assert.That(branch3.ChildCount, Is.EqualTo(2),
                "A branch with one label must have 2 children (label, statement)");
            var branch3Children = tree.Children(branch3Idx);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(tree.GetKind(branch3Children[0]), Is.EqualTo((byte)PascalAstNodeKind.LiteralInt));
                Assert.That(pool.Resolve(tree[branch3Children[0]].Payload), Is.EqualTo("3"),
                    "Label of the second branch must be 3");
                Assert.That(tree.GetKind(branch3Children[1]), Is.EqualTo((byte)PascalAstNodeKind.Assignment),
                    "The last child of a branch must be its statement");
            }

            // Else: a Block holding the else statements.
            int elseIdx = caseChildren[3];
            Assert.That(tree.GetKind(elseIdx), Is.EqualTo((byte)PascalAstNodeKind.Block),
                "The else part must be a Block of its statements");
            var elseChildren = tree.Children(elseIdx);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(elseChildren.Length, Is.EqualTo(1),
                    "The else block must hold the single else statement");
                Assert.That(tree.GetKind(elseChildren[0]), Is.EqualTo((byte)PascalAstNodeKind.Assignment),
                    "The else statement must be the `x := 30` assignment");
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
    }
}