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

    private const string IfTrueAndIfFalseProgram =
        "program Test;\n" +
        "var x: Integer;\n" +
        "begin\n" +
        "  x := 0;\n" +
        "  if true then x := 1;\n" +
        "  if false then x := 2;\n" +
        "end.";

    /// <summary>Finds the first occurrence of a byte sequence at or after
    /// <paramref name="from"/>, or -1.</summary>
    private static int IndexOfSequence(byte[] rom, int from, params byte[] sequence)
    {
        for (int i = from; i + sequence.Length <= rom.Length; i++)
        {
            bool match = true;
            for (int j = 0; j < sequence.Length; j++)
            {
                if (rom[i + j] != sequence[j]) { match = false; break; }
            }
            if (match) return i;
        }
        return -1;
    }

    [Test]
    public void ProgramUsingIfTrueAndIfFalse()
    {
        // End to end: the true arm runs and the false arm is skipped.
        // The false arm's code is still emitted in the ROM — only its
        // execution is skipped — so both assertions are on control flow.
        // The true arm must be reached by falling through from the
        // preceding store, and the false arm must be jumped over.
        var result = Compile(IfTrueAndIfFalseProgram);

        Assert.That(result.Success, Is.True, result.ErrorMessage);
        Assert.That(result.Code, Is.Not.Null);
        Assert.That(result.Code!.Length, Is.EqualTo(4096));

        byte[] rom = result.Code;
        // True arm: load immediate 1 and store it to zero-page $80. It must
        // sit immediately after the `x := 0` store (load immediate 0, store
        // to $80) — no branch between them, so execution falls through into
        // it. Today the visitor drops the `true` literal to a Nop and the
        // AST-to-HLIR transformer drops the whole if statement (it has no
        // lowering for a Nop condition), so the arm's code is never emitted.
        int trueArm = IndexOfSequence(rom, 0, 0xA9, 0x01, 0x85, 0x80);
        Assert.That(trueArm, Is.GreaterThanOrEqualTo(0),
            "the ROM must contain the true arm (LDA #1; STA $80)");
        Assert.That(trueArm - 4, Is.GreaterThanOrEqualTo(0));
        Assert.That(rom.AsSpan(trueArm - 4, 4).ToArray(),
            Is.EqualTo(new byte[] { 0xA9, 0x00, 0x85, 0x80 }),
            "the true arm must directly follow the x := 0 store (fall-through, no branch)");

        // False arm is emitted but must be jumped over: load immediate 2
        // and store it to $80, immediately preceded by a JMP to the byte
        // just past the store.
        int falseArm = IndexOfSequence(rom, trueArm + 4, 0xA9, 0x02, 0x85, 0x80);
        Assert.That(falseArm, Is.GreaterThanOrEqualTo(0),
            "the ROM must contain the false arm (LDA #2; STA $80)");
        Assert.That(falseArm - 3, Is.GreaterThanOrEqualTo(0));
        Assert.That(rom[falseArm - 3], Is.EqualTo(0x4C),
            "a JMP must sit immediately before the false arm so it never executes");
        int jmpTarget = rom[falseArm - 2] | (rom[falseArm - 1] << 8);
        Assert.That(jmpTarget - 0xF000, Is.EqualTo(falseArm + 4),
            "the JMP over the false arm must land just past its STA $80");
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

        // "bump" is the canonical lowercase spelling the visitor interns
        // procedure identifiers under (Pascal is case-insensitive).
        uint bumpOffset = frontend.StringPool!.Intern("bump");
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

    [Test]
    public void ProgramBranchingOnLessThan()
    {
        // `x < 5` must skip its then-arm with BCS (0xB0, carry set after
        // CMP): after CMP, carry is set exactly when x >= 5, i.e. when
        // the `<` condition is false — so BCS skips the then-arm exactly
        // when the condition fails. Today it emits BNE (0xD0),
        // the '=' branch, so `if x < 5` behaves as `if x = 5`. This slice
        // covers '<' only; '>' is a separate slice.
        var lessThan = Compile(LessThanProgram);
        Assert.That(lessThan.Success, Is.True, lessThan.ErrorMessage);
        Assert.That(lessThan.Code, Is.Not.Null);
        Assert.That(lessThan.Code!.Length, Is.EqualTo(4096));
        // LDA $80; CMP #5; BCS +4; LDA #1; STA $80.
        Assert.That(ContainsSequence(lessThan.Code,
            0xA5, 0x80, 0xC9, 0x05, 0xB0, 0x04, 0xA9, 0x01, 0x85, 0x80), Is.True,
            "`if x < 5` must guard its then-arm with BCS, not the '=' branch");
    }

    [Test]
    public void ProgramBranchingOnEquality_StillSkipsWithNotEqual()
    {
        // Guard: `if x = 5` keeps its established lowering, a BNE skip over
        // the then-arm; adding ordering comparisons must not disturb it.
        var result = Compile(EqualityProgram);
        Assert.That(result.Success, Is.True, result.ErrorMessage);
        Assert.That(result.Code, Is.Not.Null);
        Assert.That(result.Code!.Length, Is.EqualTo(4096));
        // LDA $80; CMP #5; BNE +4; LDA #1; STA $80.
        Assert.That(ContainsSequence(result.Code,
            0xA5, 0x80, 0xC9, 0x05, 0xD0, 0x04, 0xA9, 0x01, 0x85, 0x80), Is.True,
            "`if x = 5` must still guard its then-arm with BNE");
    }

    private const string LessThanProgram =
        "program Probe;\n" +
        "var x: Integer;\n" +
        "begin\n" +
        "  x := 3;\n" +
        "  if x < 5 then x := 1;\n" +
        "end.";

    private const string EqualityProgram =
        "program Probe;\n" +
        "var x: Integer;\n" +
        "begin\n" +
        "  x := 5;\n" +
        "  if x = 5 then x := 1;\n" +
        "end.";
}
