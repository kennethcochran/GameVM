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

    private const string ProductProgram =
        "program Test;\n" +
        "var x: Integer;\n" +
        "var y: Integer;\n" +
        "begin\n" +
        "  x := 6;\n" +
        "  y := 7;\n" +
        "  x := x * y;\n" +
        "end.";

    private const string ConsecutiveProductsProgram =
        "program Test;\n" +
        "var x: Integer;\n" +
        "var y: Integer;\n" +
        "var z: Integer;\n" +
        "var a: Integer;\n" +
        "var b: Integer;\n" +
        "begin\n" +
        "  x := 6;\n" +
        "  y := 7;\n" +
        "  z := 8;\n" +
        "  a := x * y;\n" +
        "  b := x * z;\n" +
        "end.";

    /// <summary>Scans for multiply machinery at or after
    /// <paramref name="from"/>: a JSR to an in-ROM target (0x20), a
    /// backward relative branch (a branch opcode with offset &gt;= 0x80),
    /// or an ASL (0x0A/0x06/0x16/0x0E/0x1E). The 6502 has no multiply
    /// instruction, so any real 8-bit multiply is a subroutine call, a
    /// loop, or shift-and-add steps. The reset prologue's own BNE clear
    /// loop sits before <paramref name="from"/> and is never counted.</summary>
    private static bool HasMultiplyMachinery(byte[] rom, int from)
    {
        for (int i = from; i < rom.Length; i++)
        {
            byte op = rom[i];
            if (op == 0x20 && i + 2 < rom.Length)
            {
                int target = rom[i + 1] | (rom[i + 2] << 8);
                if (target > 0xF000 && target < 0xF000 + rom.Length) return true;
                continue;
            }
            if (op == 0x0A || op == 0x06 || op == 0x16 || op == 0x0E || op == 0x1E) return true;
            if ((op == 0x10 || op == 0x30 || op == 0x50 || op == 0x70 ||
                 op == 0x90 || op == 0xB0 || op == 0xD0 || op == 0xF0) &&
                i + 1 < rom.Length && rom[i + 1] >= 0x80)
                return true;
        }
        return false;
    }

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
    public void ProgramComputingAProduct()
    {
        // End to end: 6 * 7 must be computed as a product, not a sum. Both
        // operands are variables: only literal-vs-literal multiplication
        // folds correctly today, in the constant folder. The 6502 has no
        // multiply instruction, so a real multiply lowers to a routine —
        // a subroutine call, a loop, or unrolled shift-and-add — never to
        // the single ADC the buggy lowering emits.
        var result = Compile(ProductProgram);

        Assert.That(result.Success, Is.True, result.ErrorMessage);
        Assert.That(result.Code, Is.Not.Null);
        Assert.That(result.Code!.Length, Is.EqualTo(4096));

        byte[] rom = result.Code;
        const int bodyStart = 15; // past the reset prologue (SEI..BNE clear loop)

        // Setup stays intact: x := 6 and y := 7 store their literals.
        // Variables allocate from $80 in first-use order: x = $80, y = $81.
        int xStore = IndexOfSequence(rom, bodyStart, 0xA9, 0x06, 0x85, 0x80);
        Assert.That(xStore, Is.GreaterThanOrEqualTo(0),
            "the ROM must contain x := 6 (LDA #6; STA $80)");
        int yStore = IndexOfSequence(rom, xStore + 4, 0xA9, 0x07, 0x85, 0x81);
        Assert.That(yStore, Is.GreaterThanOrEqualTo(0),
            "the ROM must contain y := 7 (LDA #7; STA $81)");

        // The miscompilation must be gone: the product statement must not
        // lower to LDA $80; CLC; ADC $81; STA $80 (x + y, stored to x).
        // No correct multiply emits that exact sequence — a shift-and-add
        // or repeated-addition multiply adds the multiplicand to a
        // partial-product temp, never x + y back into x with nothing
        // between.
        Assert.That(IndexOfSequence(rom, bodyStart, 0xA5, 0x80, 0x18, 0x65, 0x81, 0x85, 0x80),
            Is.EqualTo(-1),
            "x := x * y must not lower to LDA $80; CLC; ADC $81; STA $80 — that computes x + y");

        // A real 8-bit multiply routine must be present past the prologue.
        // The buggy lowering emits no JSR, no branch, and no ASL here.
        Assert.That(HasMultiplyMachinery(rom, bodyStart), Is.True,
            "the ROM must contain multiply machinery (a JSR, a backward branch, or ASL) for x * y");
    }

    [Test]
    public void ProgramComputingConsecutiveProducts()
    {
        // End to end: a := x * y; b := x * z must each load x before its
        // multiply. The low-level optimizer used to drop the first Load(x)
        // as overwritten by the second, so the first product multiplied the
        // stale accumulator value instead of x.
        var result = Compile(ConsecutiveProductsProgram);

        Assert.That(result.Success, Is.True, result.ErrorMessage);
        Assert.That(result.Code, Is.Not.Null);
        Assert.That(result.Code!.Length, Is.EqualTo(4096));

        byte[] rom = result.Code;
        const int bodyStart = 15; // past the reset prologue (SEI..BNE clear loop)

        // The var declarations allocate from $80 in order (x = $80, y =
        // $81, z = $82, a = $83, b = $84), and the multiply scratch pair
        // takes $85/$86. Each multiply starts by loading its left operand
        // (LDA $80) and saving it to the multiplicand scratch (STA $85),
        // then loading its right operand.
        int first = IndexOfSequence(rom, bodyStart, 0xA5, 0x80, 0x85, 0x85, 0xA5, 0x81);
        Assert.That(first, Is.GreaterThanOrEqualTo(0),
            "the first product must load x (LDA $80) before multiplying by y");
        int second = IndexOfSequence(rom, first + 6, 0xA5, 0x80, 0x85, 0x85, 0xA5, 0x82);
        Assert.That(second, Is.GreaterThanOrEqualTo(0),
            "the second product must load x (LDA $80) before multiplying by z");
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

    private const string GotoProgram =
        "program Probe;\n" +
        "label 1;\n" +
        "var x: Integer;\n" +
        "begin\n" +
        "  x := 1;\n" +
        "  goto 1;\n" +
        "  x := 2;\n" +
        "1: x := 3;\n" +
        "end.";

    /// <summary>Finds a JMP (0x4C) whose absolute target lands exactly on
    /// the given byte sequence inside the ROM image; returns the index of
    /// the JMP instruction, or -1 when no JMP lands there.</summary>
    private static int FindJmpToSequence(byte[] rom, params byte[] sequence)
    {
        for (int i = 0; i + 2 < rom.Length; i++)
        {
            if (rom[i] != 0x4C) continue;
            int target = (rom[i + 1] | (rom[i + 2] << 8)) - 0xF000;
            if (target < 0 || target + sequence.Length > rom.Length) continue;
            bool match = true;
            for (int j = 0; j < sequence.Length; j++)
            {
                if (rom[target + j] != sequence[j]) { match = false; break; }
            }
            if (match) return i;
        }
        return -1;
    }

    [Test]
    public void ProgramJumpingToALabel()
    {
        // `goto 1` must skip `x := 2` and land on the labelled statement
        // `x := 3`: the ROM must contain a JMP whose target is exactly
        // the labelled statement's code (LDA #3; STA $80), with the
        // skipped statement's code (LDA #2; STA $80) lying between the
        // JMP and its target. Today labels and goto are dropped in the
        // frontend, so all three assignments run in sequence and no such
        // JMP exists. The target assertion is also the guard against the
        // unresolved-label fallback: a goto whose label never resolved
        // would JMP into the $F000 reset prologue instead of landing on
        // the labelled statement.
        var result = Compile(GotoProgram);
        Assert.That(result.Success, Is.True, result.ErrorMessage);
        Assert.That(result.Code, Is.Not.Null);
        Assert.That(result.Code!.Length, Is.EqualTo(4096));

        var rom = result.Code;
        int jmpIdx = FindJmpToSequence(rom, 0xA9, 0x03, 0x85, 0x80);
        Assert.That(jmpIdx, Is.GreaterThanOrEqualTo(0),
            "the goto must emit a JMP landing exactly on the labelled statement (LDA #3; STA $80)");

        int target = (rom[jmpIdx + 1] | (rom[jmpIdx + 2] << 8)) - 0xF000;
        bool skippedBetween = false;
        for (int i = jmpIdx + 3; i + 4 <= target; i++)
        {
            if (rom[i] == 0xA9 && rom[i + 1] == 0x02 && rom[i + 2] == 0x85 && rom[i + 3] == 0x80)
            { skippedBetween = true; break; }
        }
        Assert.That(skippedBetween, Is.True,
            "the skipped statement (LDA #2; STA $80) must lie between the JMP and its target");
    }
}
