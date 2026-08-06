using NUnit.Framework;
using GameVM.Compiler.Application;
using GameVM.Compiler.Core.Enums;
using GameVM.Compiler.Core.IR.Slab;
using GameVM.Compiler.Pascal;
using GameVM.Compiler.Optimizers.MidLevel;
using GameVM.Compiler.Optimizers.LowLevel;
using GameVM.Compiler.Backend.Atari2600;
using GameVM.Compiler.Capabilities;
using GameVM.Compiler.Core.SemanticAnalysis;
using GameVM.Compiler.Core.IR.Soa;

namespace GameVM.Compiler.Specs;

[TestFixture]
public class DebugPipelineTests
{

    [Test]
    public void Debug_PipelineStages()
    {
        var frontend = new PascalFrontend();
        var midOptimizer = new DefaultMidLevelOptimizer();
        var lowOptimizer = new DefaultLowLevelOptimizer();
        var transformer = new MidToLowLevelTransformer();
        var codeGen = new Atari2600CodeGenerator();
        var capValidator = new CapabilityValidatorService();

        var useCase = new CompileUseCase(
            frontend, midOptimizer, lowOptimizer, transformer, codeGen, codeGen, capValidator, new BasicSemanticAnalyzer());

        var sourceCode = "program Arithmetic;\nvar x, y, z: Integer;\nbegin\n    x := 5;\nend.";

        var options = new CompilationOptions
        {
            Target = Architecture.Atari2600,
            DispatchStrategy = DispatchStrategy.DirectThreadedCode,
            GenerateDebugInfo = false,
            Optimize = false
        };

        var result = useCase.Execute(sourceCode, ".pas", options);

        TestContext.WriteLine($"Success: {result.Success}");
        TestContext.WriteLine($"Error: {result.ErrorMessage}");
        TestContext.WriteLine($"Code length: {result.Code?.Length ?? 0}");

        if (result.Code != null && result.Code.Length > 0)
        {
            var firstBytes = string.Join(" ", result.Code.Take(20).Select(b => b.ToString("X2")));
            TestContext.WriteLine($"First bytes: {firstBytes}");
        }

        var astSlab = frontend.ParseToSlab(sourceCode);
        TestContext.WriteLine($"AST slab Count: {astSlab.Count}");
        if (astSlab.Count > 0)
        {
            for (int i = 0; i < Math.Min(20, astSlab.Count); i++)
                TestContext.WriteLine($"  [{i}]=Kind:{astSlab.GetKind(i)}, Args:{astSlab.GetArgCount(i)}");
        }

        InstList hlirSlab = frontend.ConvertToHlirSlab(astSlab);
        TestContext.WriteLine($"HLIR slab Count: {hlirSlab.Count}");
        if (hlirSlab.Count > 0)
        {
            for (int i = 0; i < Math.Min(20, hlirSlab.Count); i++)
                TestContext.WriteLine($"  [{i}]=Kind:{hlirSlab.GetKind(i)}, Args:{hlirSlab.GetArgCount(i)}");
        }

        InstList mlirSlab = default;
        if (hlirSlab.Count > 0)
        {
            mlirSlab = midOptimizer.OptimizeSlab(hlirSlab, frontend.StringPool!, OptimizationLevel.None);
            TestContext.WriteLine($"MLIR slab count: {mlirSlab.Count}");
            for (int i = 0; i < Math.Min(20, mlirSlab.Count); i++)
                TestContext.WriteLine($"  [{i}]=Kind:{mlirSlab.GetKind(i)}, Args:{mlirSlab.GetArgCount(i)}");
        }

        InstList llirSlab;
        if (mlirSlab.Count > 0 && frontend.StringPool != null)
        {
            llirSlab = transformer.TransformSlab(mlirSlab, frontend.StringPool!);
            TestContext.WriteLine($"LLIR slab count: {llirSlab.Count}");
            for (int i = 0; i < Math.Min(20, llirSlab.Count); i++)
                TestContext.WriteLine($"  [{i}]=Kind:{llirSlab.GetKind(i)}, Args:{llirSlab.GetArgCount(i)}");
        }


        Assert.Pass("Debug test completed");
    }
}