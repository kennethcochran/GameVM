using TechTalk.SpecFlow;
using GameVM.Compiler.Core.IR;
using GameVM.Compiler.Core.IR.Interfaces;
using GameVM.Compiler.Core.Exceptions;
using GameVM.Compiler.Python;
using Microsoft.Extensions.DependencyInjection;
using GameVM.Compiler.Core.Interfaces;
using GameVM.Compiler.Application;
using GameVM.Compiler.Core.IR.Transformers;
using GameVM.Compiler.Core.CodeGen;
using System.IO;
using NUnit.Framework;
using GameVM.Compiler.Application.Services;
using GameVM.Compiler.Services;

namespace GameVM.Compiler.Specs.Steps;

[Binding]
public class PythonCompilationSteps
{
    private readonly IServiceProvider _serviceProvider;
    private string _pythonCode = string.Empty;
    private string _outputFile = string.Empty;
    private string _result = string.Empty;
    private Exception? _compileException;

    public PythonCompilationSteps(ScenarioContext scenarioContext)
    {
        var services = new ServiceCollection();

        // Register core compiler components
        services.AddScoped<ILanguageFrontend, PythonFrontend>();
        services.AddScoped<IPythonASTBuilder>(_ => new PythonParseTreeToAST(""));
        services.AddScoped<IMidLevelOptimizer, DefaultMidLevelOptimizer>();
        services.AddScoped<ILowLevelOptimizer, DefaultLowLevelOptimizer>();
        services.AddScoped<IFinalIROptimizer, DefaultFinalIROptimizer>();
        services.AddScoped<IIRTransformer<MidLevelIR, LowLevelIR>, MidToLowLevelTransformer>();
        services.AddScoped<IIRTransformer<LowLevelIR, FinalIR>, LowToFinalTransformer>();
        services.AddScoped<ICodeGenerator, DefaultCodeGenerator>();
        services.AddScoped<ICompileUseCase, CompileUseCase>();

        _serviceProvider = services.BuildServiceProvider();
    }

    [Given(@"I have initialized the compiler with Python language support")]
    public void GivenIHaveInitializedTheCompilerWithPythonLanguageSupport()
    {
        // Already done in constructor
    }

    [Given(@"I have the following Python code:")]
    public void GivenIHaveTheFollowingPythonCode(string pythonCode)
    {
        _pythonCode = pythonCode;
        _outputFile = Path.GetTempFileName();
    }

    [When(@"I compile the code")]
    public void WhenICompileTheCode()
    {
        try
        {
            var useCase = _serviceProvider.GetRequiredService<ICompileUseCase>();
            var request = new CompileRequest(_pythonCode, "python", _outputFile);
            var options = new CompilationOptions(); // Assuming default options. Adjust as needed.
            _result = useCase.Execute(request.SourceCode, request.Language, options).ErrorMessage;
        }
        catch (Exception ex)
        {
            _compileException = ex;
        }
    }

    [Then(@"compilation should succeed")]
    public void ThenCompilationShouldSucceed()
    {
        Assert.That(_compileException, Is.Null);
        Assert.That(_result, Is.Not.Null);
        Assert.That(File.Exists(_outputFile), Is.True);
    }

    [Then(@"compilation should fail")]
    public void ThenCompilationShouldFail()
    {
        Assert.That(_compileException, Is.Not.Null);
    }

    [Then(@"the error message should contain ""(.*)""")]
    public void ThenTheErrorMessageShouldContain(string expectedError)
    {
        Assert.That(_compileException?.Message, Does.Contain(expectedError));
    }

    [AfterScenario]
    public void CleanupTempFile()
    {
        if (File.Exists(_outputFile))
        {
            File.Delete(_outputFile);
        }
    }
}
