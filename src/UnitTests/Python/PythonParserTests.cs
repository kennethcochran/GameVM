using NUnit.Framework;
using GameVM.Compiler.Python.ANTLR;
using Antlr4.Runtime;

namespace UnitTests.Python
{
    public abstract class PythonParserTestBase
    {
        protected static PythonParser CreateParser(string input)
        {
            var inputStream = new AntlrInputStream(input);
            var lexer = new PythonLexer(inputStream);
            var tokenStream = new CommonTokenStream(lexer);
            return new PythonParser(tokenStream);
        }
    }

    [TestFixture]
    public class PythonParserSliceTests : PythonParserTestBase
    {
        [Test]
        public void Slices_EmptySlice_ParsesCorrectly()
        {
            // Arrange
            var parser = CreateParser(":");
            
            // Act
            var ctx = parser.slices();
            
            // Assert
            Assert.That(ctx, Is.Not.Null);
            Assert.That(ctx.GetText().TrimEnd('\n'), Is.EqualTo(":"));
        }

        [Test]
        public void Slices_StepOnlySlice_ParsesCorrectly()
        {
            // Arrange
            var parser = CreateParser("::2");
            
            // Act
            var ctx = parser.slices();
            
            // Assert
            Assert.That(ctx, Is.Not.Null);
            Assert.That(ctx.GetText().TrimEnd('\n'), Is.EqualTo("::2"));
        }

        [Test]
        public void Slices_StartEndSlice_ParsesCorrectly()
        {
            // Arrange
            var parser = CreateParser("1:10");
            
            // Act
            var ctx = parser.slices();
            
            // Assert
            Assert.That(ctx, Is.Not.Null);
            Assert.That(ctx.GetText().TrimEnd('\n'), Is.EqualTo("1:10"));
        }

        [Test]
        public void Slices_StartEndStepSlice_ParsesCorrectly()
        {
            // Arrange
            var parser = CreateParser("1:10:2");
            
            // Act
            var ctx = parser.slices();
            
            // Assert
            Assert.That(ctx, Is.Not.Null);
            Assert.That(ctx.GetText().TrimEnd('\n'), Is.EqualTo("1:10:2"));
        }

        [Test]
        public void Slices_NegativeIndices_ParsesCorrectly()
        {
            // Arrange
            var parser = CreateParser("-2:-1");
            
            // Act
            var ctx = parser.slices();
            
            // Assert
            Assert.That(ctx, Is.Not.Null);
            Assert.That(ctx.GetText().TrimEnd('\n'), Is.EqualTo("-2:-1"));
        }

        [Test]
        public void Slices_MultipleSlices_ParsesCorrectly()
        {
            // Arrange
            var parser = CreateParser("1:10, :, ::2");
            
            // Act
            var ctx = parser.slices();
            
            // Assert
            Assert.That(ctx, Is.Not.Null);
            Assert.That(ctx.GetText().TrimEnd('\n'), Is.EqualTo("1:10,:,::2"));
        }

        [Test]
        public void Slices_ComplexExpressions_ParsesCorrectly()
        {
            // Arrange
            var parser = CreateParser("a+1:b*2:c-3");
            
            // Act
            var ctx = parser.slices();
            
            // Assert
            Assert.That(ctx, Is.Not.Null);
            Assert.That(ctx.GetText().TrimEnd('\n'), Is.EqualTo("a+1:b*2:c-3"));
        }

        [Test]
        public void Slices_NestedSlices_ParsesCorrectly()
        {
            // Arrange
            var parser = CreateParser("1:10:2, (x+1):(y-1)");
            
            // Act
            var ctx = parser.slices();
            
            // Assert
            Assert.That(ctx, Is.Not.Null);
            Assert.That(ctx.GetText().TrimEnd('\n'), Is.EqualTo("1:10:2,(x+1):(y-1)"));
        }
    }

    [TestFixture]
    public class PythonParserLambdaTests : PythonParserTestBase
    {
        [Test]
        public void LambdaParameters_SingleParameter_ParsesCorrectly()
        {
            // Arrange
            var parser = CreateParser("x");
            
            // Act
            var ctx = parser.lambda_parameters();
            
            // Assert
            Assert.That(ctx, Is.Not.Null);
            Assert.That(ctx.GetText().TrimEnd('\n'), Is.EqualTo("x"));
        }

        [Test]
        public void LambdaParameters_MultipleParameters_ParsesCorrectly()
        {
            // Arrange
            var parser = CreateParser("x, y, z");
            
            // Act
            var ctx = parser.lambda_parameters();
            
            // Assert
            Assert.That(ctx, Is.Not.Null);
            Assert.That(ctx.GetText().TrimEnd('\n'), Is.EqualTo("x,y,z"));
        }

        [Test]
        public void LambdaParameters_DefaultValues_ParsesCorrectly()
        {
            // Arrange
            var parser = CreateParser("x=1, y='test'");
            
            // Act
            var ctx = parser.lambda_parameters();
            
            // Assert
            Assert.That(ctx, Is.Not.Null);
            Assert.That(ctx.GetText().TrimEnd('\n'), Is.EqualTo("x=1,y='test'"));
        }

        [Test]
        public void LambdaParameters_StarArgs_ParsesCorrectly()
        {
            // Arrange
            var parser = CreateParser("*args");
            
            // Act
            var ctx = parser.lambda_parameters();
            
            // Assert
            Assert.That(ctx, Is.Not.Null);
            Assert.That(ctx.GetText().TrimEnd('\n'), Is.EqualTo("*args"));
        }

        [Test]
        public void LambdaParameters_KwArgs_ParsesCorrectly()
        {
            // Arrange
            var parser = CreateParser("**kwargs");
            
            // Act
            var ctx = parser.lambda_parameters();
            
            // Assert
            Assert.That(ctx, Is.Not.Null);
            Assert.That(ctx.GetText().TrimEnd('\n'), Is.EqualTo("**kwargs"));
        }

        [Test]
        public void LambdaParameters_MixedParameters_ParsesCorrectly()
        {
            // Arrange
            var parser = CreateParser("x, y=2, *args, **kwargs");
            
            // Act
            var ctx = parser.lambda_parameters();
            
            // Assert
            Assert.That(ctx, Is.Not.Null);
            Assert.That(ctx.GetText().TrimEnd('\n'), Is.EqualTo("x,y=2,*args,**kwargs"));
        }

        [Test]
        public void LambdaParameters_PositionalOnly_ParsesCorrectly()
        {
            // Arrange
            var parser = CreateParser("x, y, /, z");
            
            // Act
            var ctx = parser.lambda_parameters();
            
            // Assert
            Assert.That(ctx, Is.Not.Null);
            Assert.That(ctx.GetText().TrimEnd('\n'), Is.EqualTo("x,y,/,z"));
        }

        [Test]
        public void LambdaParameters_KeywordOnly_ParsesCorrectly()
        {
            // Arrange
            var parser = CreateParser("x, *, y, z");
            
            // Act
            var ctx = parser.lambda_parameters();
            
            // Assert
            Assert.That(ctx, Is.Not.Null);
            Assert.That(ctx.GetText().TrimEnd('\n'), Is.EqualTo("x,*,y,z"));
        }

        [Test]
        public void LambdaParameters_ComplexMix_ParsesCorrectly()
        {
            // Arrange
            var parser = CreateParser("a, b=1, /, c, *, d=4, **kwargs");
            
            // Act
            var ctx = parser.lambda_parameters();
            
            // Assert
            Assert.That(ctx, Is.Not.Null);
            Assert.That(ctx.GetText().TrimEnd('\n'), Is.EqualTo("a,b=1,/,c,*,d=4,**kwargs"));
        }

        [Test]
        public void LambdaParameters_NoParameters_ParsesCorrectly()
        {
            // Arrange
            var parser = CreateParser("");
            
            // Act
            var ctx = parser.lambda_parameters();
            
            // Assert
            Assert.That(ctx, Is.Not.Null);
            Assert.That(ctx.GetText().TrimEnd('\n'), Is.EqualTo(""));
        }

        [Test]
        public void LambdaParameters_ComplexDefaultValues_ParsesCorrectly()
        {
            // Arrange
            var parser = CreateParser("x=[1, 2], y={'a': 1}, z=(1, 2)");
            
            // Act
            var ctx = parser.lambda_parameters();
            
            // Assert
            Assert.That(ctx, Is.Not.Null);
            Assert.That(ctx.GetText().TrimEnd('\n'), Is.EqualTo("x=[1,2],y={'a':1},z=(1,2)"));
        }
    }

    [TestFixture]
    public class PythonParserArgsTests : PythonParserTestBase
    {
        [Test]
        public void Args_SimpleArguments_ParsesCorrectly()
        {
            // Arrange
            var parser = CreateParser("a, b, c");
            
            // Act
            var ctx = parser.args();
            
            // Assert
            Assert.That(ctx, Is.Not.Null);
            Assert.That(ctx.GetText().TrimEnd('\n'), Is.EqualTo("a,b,c"));
        }

        [Test]
        public void Args_KeywordArguments_ParsesCorrectly()
        {
            // Arrange
            var parser = CreateParser("x=1, y=2");
            
            // Act
            var ctx = parser.kwargs();
            
            // Assert
            Assert.That(ctx, Is.Not.Null);
            Assert.That(ctx.GetText().TrimEnd('\n'), Is.EqualTo("x=1,y=2"));
        }

        [Test]
        public void Args_MixedArguments_ParsesCorrectly()
        {
            // Arrange
            var parser = CreateParser("a, b, x=1, y=2");
            
            // Act
            var ctx = parser.args();
            
            // Assert
            Assert.That(ctx, Is.Not.Null);
            Assert.That(ctx.GetText().TrimEnd('\n'), Is.EqualTo("a,b,x=1,y=2"));
        }

        [Test]
        public void Args_StarArgs_ParsesCorrectly()
        {
            // Arrange
            var parser = CreateParser("*args");
            
            // Act
            var ctx = parser.starred_expression();
            
            // Assert
            Assert.That(ctx, Is.Not.Null);
            Assert.That(ctx.GetText().TrimEnd('\n'), Is.EqualTo("*args"));
        }

        [Test]
        public void Args_KwArgs_ParsesCorrectly()
        {
            // Arrange
            var parser = CreateParser("**kwargs");
            
            // Act
            var ctx = parser.kwarg_or_double_starred();
            
            // Assert
            Assert.That(ctx, Is.Not.Null);
            Assert.That(ctx.GetText().TrimEnd('\n'), Is.EqualTo("**kwargs"));
        }

        [Test]
        public void Args_MixedStarAndKeywordArguments_ParsesCorrectly()
        {
            // Arrange
            var parser = CreateParser("a, b, *args, x=1, y=2, **kwargs");
            
            // Act
            var ctx = parser.args();
            
            // Assert
            Assert.That(ctx, Is.Not.Null);
            Assert.That(ctx.GetText().TrimEnd('\n'), Is.EqualTo("a,b,*args,x=1,y=2,**kwargs"));
        }
    }

    [TestFixture]
    public class PythonParserYieldTests : PythonParserTestBase
    {
        [Test]
        public void YieldExpr_SimpleYield_ParsesCorrectly()
        {
            // Arrange
            var parser = CreateParser("yield x");
            
            // Act
            var ctx = parser.yield_stmt();
            
            // Assert
            Assert.That(ctx, Is.Not.Null);
            Assert.That(ctx.GetText().TrimEnd('\n'), Is.EqualTo("yieldx"));
        }

        [Test]
        public void YieldExpr_YieldFrom_ParsesCorrectly()
        {
            // Arrange
            var parser = CreateParser("yield from generator()");
            
            // Act
            var ctx = parser.yield_stmt();
            
            // Assert
            Assert.That(ctx, Is.Not.Null);
            Assert.That(ctx.GetText().TrimEnd('\n'), Is.EqualTo("yieldfromgenerator()"));
        }
    }

    [TestFixture]
    public class PythonParserParametersTests : PythonParserTestBase
    {
        [Test]
        public void Parameters_SimpleParameters_ParsesCorrectly()
        {
            // Arrange
            var parser = CreateParser("a, b, c");
            
            // Act
            var ctx = parser.parameters();
            
            // Assert
            Assert.That(ctx, Is.Not.Null);
            Assert.That(ctx.GetText().TrimEnd('\n'), Is.EqualTo("a,b,c"));
        }

        [Test]
        public void Parameters_DefaultValues_ParsesCorrectly()
        {
            // Arrange
            var parser = CreateParser("a, b=2, c='default'");
            
            // Act
            var ctx = parser.parameters();
            
            // Assert
            Assert.That(ctx, Is.Not.Null);
            Assert.That(ctx.GetText().TrimEnd('\n'), Is.EqualTo("a,b=2,c='default'"));
        }

        [Test]
        public void Parameters_StarArgs_ParsesCorrectly()
        {
            // Arrange
            var parser = CreateParser("*args");
            
            // Act
            var ctx = parser.star_etc();
            
            // Assert
            Assert.That(ctx, Is.Not.Null);
            Assert.That(ctx.GetText().TrimEnd('\n'), Is.EqualTo("*args"));
        }

        [Test]
        public void Parameters_KwArgs_ParsesCorrectly()
        {
            // Arrange
            var parser = CreateParser("**kwargs");
            
            // Act
            var ctx = parser.kwds();
            
            // Assert
            Assert.That(ctx, Is.Not.Null);
            Assert.That(ctx.GetText().TrimEnd('\n'), Is.EqualTo("**kwargs"));
        }

        [Test]
        public void Parameters_MixedStarAndKeywordArguments_ParsesCorrectly()
        {
            // Arrange
            var parser = CreateParser("a, b, *args, x=1, y=2, **kwargs");
            
            // Act
            var ctx = parser.parameters();
            
            // Assert
            Assert.That(ctx, Is.Not.Null);
            Assert.That(ctx.GetText().TrimEnd('\n'), Is.EqualTo("a,b,*args,x=1,y=2,**kwargs"));
        }
    }

    [TestFixture]
    public class PythonParserAssignmentTests : PythonParserTestBase
    {
        [Test]
        public void Assignment_SimpleAssignment_ParsesCorrectly()
        {
            // Arrange
            var parser = CreateParser("x = 42");
            
            // Act
            var ctx = parser.assignment();
            
            // Assert
            Assert.That(ctx, Is.Not.Null);
            Assert.That(ctx.GetText().TrimEnd('\n'), Is.EqualTo("x=42"));
        }

        [Test]
        public void Assignment_AugmentedAssignment_ParsesCorrectly()
        {
            // Arrange
            var parser = CreateParser("x += 1");
            
            // Act
            var ctx = parser.assignment();
            
            // Assert
            Assert.That(ctx, Is.Not.Null);
            Assert.That(ctx.GetText().TrimEnd('\n'), Is.EqualTo("x+=1"));
        }

        [Test]
        public void Assignment_AnnotatedAssignment_ParsesCorrectly()
        {
            // Arrange
            var parser = CreateParser("x: int = 42");
            
            // Act
            var ctx = parser.assignment();
            
            // Assert
            Assert.That(ctx, Is.Not.Null);
            Assert.That(ctx.GetText().TrimEnd('\n'), Is.EqualTo("x:int=42"));
        }

        [Test]
        public void Assignment_AnnotatedAssignmentWithType_ParsesCorrectly()
        {
            // Arrange
            var parser = CreateParser("y: float = 42.0");
            
            // Act
            var ctx = parser.assignment();
            
            // Assert
            Assert.That(ctx, Is.Not.Null);
            Assert.That(ctx.GetText().TrimEnd('\n'), Is.EqualTo("y:float=42.0"));
        }

        [Test]
        public void Assignment_MultipleTargets_ParsesCorrectly()
        {
            // Arrange
            var parser = CreateParser("a[i], b.x, *c = values");
            
            // Act
            var ctx = parser.assignment();
            
            // Assert
            Assert.That(ctx, Is.Not.Null);
            Assert.That(ctx.GetText().TrimEnd('\n'), Is.EqualTo("a[i],b.x,*c=values"));
        }

        [Test]
        public void Assignment_ComplexAnnotatedAssignment_ParsesCorrectly()
        {
            // Arrange
            var parser = CreateParser("x: list[tuple[int, str]] = [(1, 'a')]");
            
            // Act
            var ctx = parser.assignment();
            
            // Assert
            Assert.That(ctx, Is.Not.Null);
            Assert.That(ctx.GetText().TrimEnd('\n'), Is.EqualTo("x:list[tuple[int,str]]=[(1,'a')]"));
        }
    }

    [TestFixture]
    public class PythonParserClassTests : PythonParserTestBase
    {
        [Test]
        public void Class_SimpleClass_ParsesCorrectly()
        {
            // Arrange
            var parser = CreateParser("class MyClass: pass");
            
            // Act
            var ctx = parser.class_def();
            
            // Assert
            Assert.That(ctx, Is.Not.Null);
            Assert.That(ctx.GetText().TrimEnd('\n'), Is.EqualTo("classMyClass:pass"));
        }

        [Test]
        public void Class_WithInheritance_ParsesCorrectly()
        {
            // Arrange
            var parser = CreateParser("class MyClass(BaseClass): pass");
            
            // Act
            var ctx = parser.class_def();
            
            // Assert
            Assert.That(ctx, Is.Not.Null);
            Assert.That(ctx.GetText().TrimEnd('\n'), Is.EqualTo("classMyClass(BaseClass):pass"));
        }
    }

    [TestFixture]
    public class PythonParserIfTests : PythonParserTestBase
    {
        [Test]
        public void If_SimpleIf_ParsesCorrectly()
        {
            // Arrange
            var parser = CreateParser("if x > 0: pass");
            
            // Act
            var ctx = parser.if_stmt();
            
            // Assert
            Assert.That(ctx, Is.Not.Null);
            Assert.That(ctx.GetText().TrimEnd('\n'), Is.EqualTo("ifx>0:pass"));
        }

        [Test]
        public void If_WithElif_ParsesCorrectly()
        {
            // Arrange
            var parser = CreateParser("if x > 0: pass\nelif x < 0: pass");
            
            // Act
            var ctx = parser.if_stmt();
            
            // Assert
            Assert.That(ctx, Is.Not.Null);
            Assert.That(ctx.GetText().TrimEnd('\n'), Is.EqualTo("ifx>0:pass\nelifx<0:pass"));
        }
    }

    [TestFixture]
    public class PythonParserWhileTests : PythonParserTestBase
    {
        [Test]
        public void While_SimpleWhile_ParsesCorrectly()
        {
            // Arrange
            var parser = CreateParser("while True: pass");
            
            // Act
            var ctx = parser.while_stmt();
            
            // Assert
            Assert.That(ctx, Is.Not.Null);
            Assert.That(ctx.GetText().TrimEnd('\n'), Is.EqualTo("whileTrue:pass"));
        }
    }

    [TestFixture]
    public class PythonParserForTests : PythonParserTestBase
    {
        [Test]
        public void For_SimpleFor_ParsesCorrectly()
        {
            // Arrange
            var parser = CreateParser("for x in range(10): pass");
            
            // Act
            var ctx = parser.for_stmt();
            
            // Assert
            Assert.That(ctx, Is.Not.Null);
            Assert.That(ctx.GetText().TrimEnd('\n'), Is.EqualTo("forxinrange(10):pass"));
        }
    }

    [TestFixture]
    public class PythonParserTryTests : PythonParserTestBase
    {
        [Test]
        public void Try_SimpleTry_ParsesCorrectly()
        {
            // Arrange
            var parser = CreateParser("try: pass\nexcept: pass");
            
            // Act
            var ctx = parser.try_stmt();
            
            // Assert
            Assert.That(ctx, Is.Not.Null);
            Assert.That(ctx.GetText().TrimEnd('\n'), Is.EqualTo("try:pass\nexcept:pass"));
        }

        [Test]
        public void Try_WithFinally_ParsesCorrectly()
        {
            // Arrange
            var parser = CreateParser("try: pass\nfinally: pass");
            
            // Act
            var ctx = parser.try_stmt();
            
            // Assert
            Assert.That(ctx, Is.Not.Null);
            Assert.That(ctx.GetText().TrimEnd('\n'), Is.EqualTo("try:pass\nfinally:pass"));
        }
    }

    [TestFixture]
    public class PythonParserImportTests : PythonParserTestBase
    {
        [Test]
        public void Import_SimpleImport_ParsesCorrectly()
        {
            // Arrange
            var parser = CreateParser("import sys");
            
            // Act
            var ctx = parser.import_name();
            
            // Assert
            Assert.That(ctx, Is.Not.Null);
            Assert.That(ctx.GetText().TrimEnd('\n'), Is.EqualTo("importsys"));
        }

        [Test]
        public void Import_FromImport_ParsesCorrectly()
        {
            // Arrange
            var parser = CreateParser("from os import path");
            
            // Act
            var ctx = parser.import_from();
            
            // Assert
            Assert.That(ctx, Is.Not.Null);
            Assert.That(ctx.GetText().TrimEnd('\n'), Is.EqualTo("fromosimportpath"));
        }
    }
}
