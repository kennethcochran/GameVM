using GameVM.Compiler.Core.Exceptions;
using GameVM.Compiler.Python;
using Moq;
using Moq.AutoMock;
using GameVM.Compiler.Core.IR;
using Antlr4.Runtime.Tree;
using GameVM.Compiler.Python.AST;

namespace UnitTests.Python;

[TestFixture]
public class PythonFrontendTests
{
    private AutoMocker _mocker;

    [SetUp]
    public void SetUp()
    {
        _mocker = new AutoMocker();
    }

    [Test]
    public void Parse_WithValidPythonCode_ReturnsHLIR()
    {
        // Arrange
        var pythonCode = "x = 42";
        _mocker.GetMock<IPythonASTBuilder>()
            .Setup(x => x.Build(It.IsAny<IParseTree>()))
            .Returns(new PythonModule("test.py"));
        var frontend = _mocker.CreateInstance<PythonFrontend>();

        // Act
        var result = frontend.Parse(pythonCode);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<HighLevelIR>());
    }

    [Test]
    public void Parse_WithInvalidPythonCode_ThrowsParserException()
    {
        // Arrange
        var pythonCode = "def invalid syntax:"; // Invalid Python syntax
        var frontend = _mocker.CreateInstance<PythonFrontend>();

        // Act & Assert
        var ex = Assert.Throws<ParserException>(() => frontend.Parse(pythonCode));
        Assert.That(ex.Message, Does.Contain("mismatched input 'syntax'"));
    }

    [Test]
    public void Parse_WithEmptyCode_ThrowsArgumentNullException()
    {
        // Arrange
        var pythonCode = "";
        var frontend = _mocker.CreateInstance<PythonFrontend>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => frontend.Parse(pythonCode));
    }
}
