using NUnit.Framework;
using GameVM.Compiler.Application;
using GameVM.Compiler.Core.Enums;
using GameVM.Compiler.Core.IR;
using GameVM.Compiler.Core.IR.Soa;
using GameVM.Compiler.Core.IR.Transformers;
using GameVM.Compiler.Pascal;
using GameVM.Compiler.Optimizers.MidLevel;
using GameVM.Compiler.Optimizers.LowLevel;
using GameVM.Compiler.Backend.Atari2600;
using GameVM.Compiler.Capabilities;

namespace GameVM.Compiler.Specs;

/// <summary>
/// End-to-end acceptance tests for zero-argument Pascal procedure
/// declarations: Pascal source through HLIR -> MLIR -> LLIR to Atari 2600
/// ROM bytes. The declared body must be emitted once, reached only via a
/// JSR to its own label (never the $F000 reset fallback for unresolved
/// labels), and terminated by an RTS.
/// </summary>
[TestFixture]
public class PascalProcedureRomTests
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

    private static CompilationResult Compile(string source)
    {
        var codeGen = new Atari2600CodeGenerator();
        var useCase = new CompileUseCase(
            new PascalFrontend(), new DefaultMidLevelOptimizer(), new DefaultLowLevelOptimizer(),
            new MidToLowLevelTransformer(), codeGen, codeGen, new CapabilityValidatorService());
        return useCase.Execute(source, ".pas", new CompilationOptions
        {
            Target = Architecture.Atari2600,
            DispatchStrategy = DispatchStrategy.DirectThreadedCode,
            GenerateDebugInfo = false,
            Optimize = false
        });
    }

    private static bool ContainsSequence(byte[] rom, params byte[] sequence)
    {
        for (int i = 0; i + sequence.Length <= rom.Length; i++)
        {
            bool match = true;
            for (int j = 0; j < sequence.Length; j++)
            {
                if (rom[i + j] != sequence[j]) { match = false; break; }
            }
            if (match) return true;
        }
        return false;
    }

    /// <summary>Finds a JSR (0x20) whose absolute target lands exactly on
    /// the given byte sequence inside the ROM image.</summary>
    private static bool HasJsrToSequence(byte[] rom, params byte[] sequence)
    {
        for (int i = 0; i + 2 < rom.Length; i++)
        {
            if (rom[i] != 0x20) continue;
            int target = (rom[i + 1] | (rom[i + 2] << 8)) - 0xF000;
            if (target < 0 || target + sequence.Length > rom.Length) continue;
            bool match = true;
            for (int j = 0; j < sequence.Length; j++)
            {
                if (rom[target + j] != sequence[j]) { match = false; break; }
            }
            if (match) return true;
        }
        return false;
    }

    [Test]
    public void Compile_ProcedureProgram_EmitsBodyReachedByJsrEndingInRts()
    {
        var result = Compile(ProcedureProgram);

        Assert.That(result.Success, Is.True, result.ErrorMessage);
        Assert.That(result.Code, Is.Not.Null);
        Assert.That(result.Code!.Length, Is.EqualTo(4096));

        byte[] rom = result.Code;
        // Body: LDA #42 (A9 2A); STA zero-page $80 (85 80); RTS (60).
        Assert.That(HasJsrToSequence(rom, 0xA9, 0x2A, 0x85, 0x80, 0x60), Is.True,
            "the ROM must contain the procedure body (LDA #42; STA $80; RTS) and a JSR targeting it");
        // The unresolved-label fallback JSR $F000 must not appear.
        Assert.That(ContainsSequence(rom, 0x20, 0x00, 0xF0), Is.False,
            "a call to a declared procedure must never fall back to JSR $F000 (reset entry)");
    }

    [Test]
    public void Compile_ProcedureProgram_MlirBranchesOverBodyAndFlagsCalleeReturn()
    {
        var frontend = new PascalFrontend();
        var parsed = frontend.ParseToHlir(ProcedureProgram);
        Assert.That(parsed.Hlir.Count, Is.GreaterThan(0), "frontend must produce HLIR");

        var mlir = new HlirTreeToMlirTransformer(frontend.StringPool!)
            .Transform(parsed.Hlir);

        uint bumpOffset = frontend.StringPool!.Intern("Bump");
        int labelIdx = -1;
        int callIdx = -1;
        int returnIdx = -1;
        for (int i = 0; i < mlir.Count; i++)
        {
            var kind = (MlirInstructionKind)mlir.GetKind(i);
            var ops = mlir.GetOperands(i);
            if (kind == MlirInstructionKind.Label && ops.Length > 0 && ops[0] == bumpOffset)
                labelIdx = i;
            if (kind == MlirInstructionKind.Call && ops.Length > 0 && ops[0] == bumpOffset)
                callIdx = i;
            if (kind == MlirInstructionKind.Return &&
                ((InstructionFlag)mlir.GetFlags(i)).HasFlag(InstructionFlag.CalleeReturn))
                returnIdx = i;
        }

        Assert.That(labelIdx, Is.GreaterThanOrEqualTo(0), "MLIR must carry a label for Bump");
        Assert.That(callIdx, Is.GreaterThan(labelIdx),
            "the call site sits in the main body, after the branched-over declaration");
        Assert.That(returnIdx, Is.GreaterThan(labelIdx),
            "the procedure body must end in a CalleeReturn-flagged MLIR Return");
        // The instruction immediately before the Bump label must be the
        // unconditional branch over the body (entry path never falls in).
        Assert.That((MlirInstructionKind)mlir.GetKind(labelIdx - 1),
            Is.EqualTo(MlirInstructionKind.Branch),
            "sequential entry flow must branch over the procedure body");
    }
}
