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

    [Test]
    public void RepeatUntilLoopRunningUntilConditionMet()
    {
        // `repeat x := x + 1 until x = 3` must run its body (x reaches 3 by
        // repeated increments) and stop when the condition holds. At the ROM
        // level: the body increment is emitted, the `x = 3` test follows the
        // body (the body runs before the first test), and a BNE branches back
        // to the body start while x is not yet 3. Today the visitor drops the
        // whole repeat to a Nop, so the ROM contains no increment at all.
        var result = Compile(RepeatUntilProgram);
        Assert.That(result.Success, Is.True, result.ErrorMessage);
        Assert.That(result.Code, Is.Not.Null);
        Assert.That(result.Code!.Length, Is.EqualTo(4096));

        byte[] rom = result.Code;
        // Body: LDA $80; CLC; ADC #1; STA $80 (x := x + 1).
        int bodyAt = IndexOfSequence(rom, 0, 0xA5, 0x80, 0x18, 0x69, 0x01, 0x85, 0x80);
        Assert.That(bodyAt, Is.GreaterThanOrEqualTo(0),
            "the ROM must contain the repeat body (LDA $80; CLC; ADC #1; STA $80)");

        // Exit condition, tested after the body: LDA $80; CMP #3; BNE back.
        int condAt = IndexOfSequence(rom, bodyAt + 7, 0xA5, 0x80, 0xC9, 0x03, 0xD0);
        Assert.That(condAt, Is.GreaterThanOrEqualTo(0),
            "the ROM must test `x = 3` after the body — the body runs before the first test");
        int bneOffset = (sbyte)rom[condAt + 5];
        int bneTarget = condAt + 6 + bneOffset;
        Assert.That(bneTarget, Is.EqualTo(bodyAt),
            "the BNE back-edge must land on the body start, so the loop repeats while x <> 3");

        // Falling through past the BNE (x = 3) must reach the halt loop, not
        // run off into padding: a JMP to itself sits right after the branch.
        Assert.That(rom[condAt + 6], Is.EqualTo(0x4C),
            "a JMP must follow the back-edge branch so the program halts once the condition holds");
        int jmpTarget = rom[condAt + 7] | (rom[condAt + 8] << 8);
        Assert.That(jmpTarget - 0xF000, Is.EqualTo(condAt + 6),
            "the JMP after the loop must target itself (halt loop)");
    }

    [Test]
    public void WhileLoopStillRunsBodyAndExits()
    {
        // Guard: `while` already works and this slice must not disturb it.
        // The condition is tested before the body (CMP #3; BCS past the
        // body), the body increments x, and a JMP returns to the test.
        var result = Compile(WhileLoopProgram);
        Assert.That(result.Success, Is.True, result.ErrorMessage);
        Assert.That(result.Code, Is.Not.Null);
        Assert.That(result.Code!.Length, Is.EqualTo(4096));
        Assert.That(ContainsSequence(result.Code,
            0xA9, 0x00, 0x85, 0x80, 0xA9, 0x00, 0x85, 0x80,
            0xC9, 0x03, 0xB0, 0x0A,
            0xA5, 0x80, 0x18, 0x69, 0x01, 0x85, 0x80,
            0x4C, 0x15, 0xF0), Is.True,
            "`while x < 3` must keep its established lowering (test before body, JMP back to the test)");
    }
    [Test]
    public void ProgramUsingAndOrAndNot()
    {
        // End to end: `and`/`or` must compute bitwise values with the
        // 6502's AND/ORA, and `not` must complement with EOR #$FF.
        // Today `and`/`or` fall through to the comparison path (the ROM
        // holds CMP $82; STA $80 — the value 1, whatever the bits are),
        // literal `and`/`or` fold to 0, and both `not` statements vanish
        // entirely — the middle end rejects the single-child node, so the
        // enclosing statement is silently dropped. This slice covers
        // bitwise `and`/`or`/`not` on values only; comparisons are separate
        // slices, and there is no short-circuit evaluation (Pascal's
        // `and`/`or` here are bitwise, evaluated eagerly).
        //
        // Implementer note: the new LLIR kinds must also join the
        // low-level optimizer's accumulator-consumer list
        // (DefaultLowLevelOptimizer.IsOverwrittenLoad — the same fix shape
        // PR #19 applied for Multiply), or the Load before a repeated
        // bitwise op is dropped as overwritten and the statement computes
        // with a stale accumulator.
        var result = Compile(BitwiseProgram);

        Assert.That(result.Success, Is.True, result.ErrorMessage);
        Assert.That(result.Code, Is.Not.Null);
        Assert.That(result.Code!.Length, Is.EqualTo(4096));

        byte[] rom = result.Code;
        const int bodyStart = 15; // past the reset prologue (SEI..BNE clear loop)

        // Setup stays intact, and pins the addresses the op sequences
        // below rely on: variables allocate from $80 in declaration order
        // (x = $80, y = $81, z = $82).
        int yStore = IndexOfSequence(rom, bodyStart, 0xA9, 0x0C, 0x85, 0x81);
        Assert.That(yStore, Is.GreaterThanOrEqualTo(0),
            "the ROM must contain y := 12 (LDA #12; STA $81)");
        int zStore = IndexOfSequence(rom, yStore + 4, 0xA9, 0x0A, 0x85, 0x82);
        Assert.That(zStore, Is.GreaterThanOrEqualTo(0),
            "the ROM must contain z := 10 (LDA #10; STA $82)");

        // y and z (12 and 10 = 8): LDA $81; AND $82; STA $80.
        Assert.That(IndexOfSequence(rom, zStore + 4, 0xA5, 0x81, 0x25, 0x82, 0x85, 0x80),
            Is.GreaterThanOrEqualTo(0),
            "x := y and z must use the 6502 AND (LDA $81; AND $82; STA $80), not the comparison path");

        // y or z (12 or 10 = 14): LDA $81; ORA $82; STA $80.
        Assert.That(IndexOfSequence(rom, zStore + 4, 0xA5, 0x81, 0x05, 0x82, 0x85, 0x80),
            Is.GreaterThanOrEqualTo(0),
            "x := y or z must use the 6502 ORA (LDA $81; ORA $82; STA $80), not the comparison path");

        // not y (not 12 = 243): LDA $81; EOR #$FF; STA $80.
        Assert.That(IndexOfSequence(rom, zStore + 4, 0xA5, 0x81, 0x49, 0xFF, 0x85, 0x80),
            Is.GreaterThanOrEqualTo(0),
            "x := not y must complement with EOR #$FF (LDA $81; EOR #$FF; STA $80) — the statement must not be dropped");

        // Literal operands must fold to their bitwise values, not to 0.
        Assert.That(IndexOfSequence(rom, zStore + 4, 0xA9, 0x08, 0x85, 0x80),
            Is.GreaterThanOrEqualTo(0),
            "x := 12 and 10 must fold to 8 (LDA #8; STA $80), not to 0");
        Assert.That(IndexOfSequence(rom, zStore + 4, 0xA9, 0x0E, 0x85, 0x80),
            Is.GreaterThanOrEqualTo(0),
            "x := 12 or 10 must fold to 14 (LDA #14; STA $80), not to 0");
        Assert.That(IndexOfSequence(rom, zStore + 4, 0xA9, 0xF3, 0x85, 0x80),
            Is.GreaterThanOrEqualTo(0),
            "x := not 12 must fold to 243 (LDA #243; STA $80) — the statement must not be dropped");

        // The comparison path must be gone: no CMP $82 anywhere in the
        // program body (the program contains no comparison).
        Assert.That(ContainsSequence(rom, 0xC5, 0x82), Is.False,
            "`and`/`or` must not lower through a CMP — there is no comparison in this program");
    }


    private const string RepeatUntilProgram =
        "program Probe;\n" +
        "var x: Integer;\n" +
        "begin\n" +
        "  x := 0;\n" +
        "  repeat\n" +
        "    x := x + 1;\n" +
        "  until x = 3;\n" +
        "end.";

    private const string WhileLoopProgram =
        "program Probe;\n" +
        "var x: Integer;\n" +
        "begin\n" +
        "  x := 0;\n" +
        "  while x < 3 do\n" +
        "    x := x + 1;\n" +
        "end.";
    private const string BitwiseProgram =
        "program Probe;\n" +
        "var x, y, z: Integer;\n" +
        "begin\n" +
        "  y := 12;\n" +
        "  z := 10;\n" +
        "  x := y and z;\n" +
        "  x := y or z;\n" +
        "  x := not y;\n" +
        "  x := 12 and 10;\n" +
        "  x := 12 or 10;\n" +
        "  x := not 12;\n" +
        "end.";


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
