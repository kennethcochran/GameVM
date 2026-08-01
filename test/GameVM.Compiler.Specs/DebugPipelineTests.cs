using NUnit.Framework;
using GameVM.Compiler.Application;
using GameVM.Compiler.Application.Services;
using GameVM.Compiler.Core.Enums;
using GameVM.Compiler.Core.IR.Slab;
using GameVM.Compiler.Pascal;
using GameVM.Compiler.Optimizers.MidLevel;
using GameVM.Compiler.Optimizers.LowLevel;
using GameVM.Compiler.Backend.Atari2600;
using GameVM.Compiler.Capabilities;
using GameVM.Compiler.Core.SemanticAnalysis;
using System;
using System.Linq;

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
        TestContext.WriteLine($"AST slab length: {astSlab?.Length ?? 0}");
        if (astSlab != null && astSlab.Length > 0)
        {
            var header = SlabHeader.Read(astSlab);
            TestContext.WriteLine($"AST: Stage={header.IrStage}, ElemCount={header.ElementCount}");
            for (int i = 0; i < Math.Min(20, astSlab.Length); i++)
                TestContext.WriteLine($"  [{i}]=0x{astSlab[i]:X8}");
        }

        uint[]? hlirSlab = astSlab != null ? frontend.ConvertToHlirSlab(astSlab) : null;
        TestContext.WriteLine($"HLIR slab length: {hlirSlab?.Length ?? 0}");
        if (hlirSlab != null && hlirSlab.Length > 0)
        {
            var header = SlabHeader.Read(hlirSlab);
            TestContext.WriteLine($"HLIR: Stage={header.IrStage}, ElemCount={header.ElementCount}");
            for (int i = 0; i < Math.Min(20, hlirSlab.Length); i++)
                TestContext.WriteLine($"  [{i}]=0x{hlirSlab[i]:X8}");
        }

        uint[]? mlirSlab = null;
        if (hlirSlab != null)
        {
            mlirSlab = midOptimizer.OptimizeSlab(hlirSlab, OptimizationLevel.None);
            TestContext.WriteLine($"MLIR slab length: {mlirSlab?.Length ?? 0}");
            if (mlirSlab != null && mlirSlab.Length > 0)
            {
                var header = SlabHeader.Read(mlirSlab);
                TestContext.WriteLine($"MLIR: Stage={header.IrStage}, ElemCount={header.ElementCount}");
                for (int i = 0; i < Math.Min(20, mlirSlab.Length); i++)
                    TestContext.WriteLine($"  [{i}]=0x{mlirSlab[i]:X8}");
            }
        }

        uint[]? llirSlab = null;
        if (mlirSlab != null)
        {
            llirSlab = transformer.TransformSlab(mlirSlab);
            TestContext.WriteLine($"LLIR slab length: {llirSlab?.Length ?? 0}");
            if (llirSlab != null && llirSlab.Length > 0)
            {
                var header = SlabHeader.Read(llirSlab);
                TestContext.WriteLine($"LLIR: Stage={header.IrStage}, ElemCount={header.ElementCount}");
                for (int i = 0; i < Math.Min(20, llirSlab.Length); i++)
                    TestContext.WriteLine($"  [{i}]=0x{llirSlab[i]:X8}");
            }
        }

        Assert.Pass("Debug test completed");
    }
}
