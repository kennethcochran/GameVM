using System.Reflection;
using GameVM.TestRunner.Core;
using NSubstitute;
using NUnit.Framework;

namespace GameVM.TestRunner.Tests;

[TestFixture]
public class AssemblyTestRunnerTests
{
    private ITestExecutor _executor = null!;
    private List<string> _log = null!;
    private AssemblyTestRunner _runner = null!;

    [SetUp]
    public void SetUp()
    {
        _executor = Substitute.For<ITestExecutor>();
        _log = new List<string>();
        _runner = new AssemblyTestRunner(_executor, _log.Add);
    }

    [Test]
    public void Run_AllPass_ReturnsZero()
    {
        _executor.Execute(Arg.Any<Assembly>(), Arg.Any<string[]>()).Returns(0);
        var asmName = typeof(AssemblyTestRunnerTests).Assembly.GetName().Name!;

        var failed = _runner.Run([asmName], []);

        Assert.That(failed, Is.EqualTo(0));
    }

    [Test]
    public void Run_SomeFail_AggregatesCounts()
    {
        _executor.Execute(Arg.Any<Assembly>(), Arg.Any<string[]>()).Returns(3, 2);
        var asmName = typeof(AssemblyTestRunnerTests).Assembly.GetName().Name!;

        var failed = _runner.Run([asmName, asmName], []);

        Assert.That(failed, Is.EqualTo(5));
    }

    [Test]
    public void Run_MissingAssembly_CountsAsFailure()
    {
        var failed = _runner.Run(["Definitely.Not.A.Real.Assembly.Name"], []);

        Assert.That(failed, Is.EqualTo(1));
        Assert.That(_log.Any(l => l.Contains("could not load")), Is.True);
    }

    [Test]
    public void Run_ForwardsArgsToExecutor()
    {
        string[]? captured = null;
        _executor.Execute(Arg.Any<Assembly>(), Arg.Do<string[]>(a => captured = a)).Returns(0);
        var asmName = typeof(AssemblyTestRunnerTests).Assembly.GetName().Name!;

        _runner.Run([asmName], ["--test=Foo.Bar"]);

        Assert.That(captured, Is.EqualTo(new[] { "--test=Foo.Bar" }));
    }

    [Test]
    public void Run_LogsTotalFailed()
    {
        _executor.Execute(Arg.Any<Assembly>(), Arg.Any<string[]>()).Returns(4);
        var asmName = typeof(AssemblyTestRunnerTests).Assembly.GetName().Name!;

        _runner.Run([asmName], []);

        Assert.That(_log.Any(l => l.Contains("TOTAL FAILED: 4")), Is.True);
    }

    [Test]
    public void DefaultAssemblies_ContainsExpectedProjects()
    {
        Assert.That(AssemblyTestRunner.DefaultAssemblies, Has.Member("GameVM.DevTools.Tests"));
        Assert.That(AssemblyTestRunner.DefaultAssemblies, Has.Member("GameVM.Compiler.Specs"));
        Assert.That(AssemblyTestRunner.DefaultAssemblies.Length, Is.EqualTo(11));
    }
}
