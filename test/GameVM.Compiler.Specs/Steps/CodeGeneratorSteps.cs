using GameVM.Compiler.Backend.Atari2600;
using GameVM.Compiler.Core.Enums;
using GameVM.Compiler.Core.Exceptions;
using GameVM.Compiler.Core.IR;
using GameVM.Compiler.Core.IR.Buffers;
using GameVM.Compiler.Core.IR.Interfaces;
using GameVM.Compiler.Core.IR.Soa;
using NUnit.Framework;
using Reqnroll;

namespace GameVM.Compiler.Specs.Steps;

/// <summary>
/// Steps for the unresolved-label code-generation scenarios. The behaviour
/// under test lives in the code generator, below the Pascal frontend: a
/// Jump, Call, or Branch to a label with no matching Label instruction
/// must fail loudly instead of silently emitting a transfer to the reset
/// address ($F000) or a garbage branch displacement.
/// </summary>
[Binding]
public class CodeGeneratorSteps
{
    // A label id that no Label instruction in the slab defines.
    private const uint UndefinedLabel = 9999;

    // A label id defined by a Label instruction in the slab.
    private const uint DefinedLabel = 1000;

    private InstList _slab;
    private bool _slabBuilt;
    private byte[]? _rom;
    private GameVmException? _generationError;

    [Given(@"low-level code that jumps to a label that is never defined")]
    public void GivenLowLevelCodeThatJumpsToALabelThatIsNeverDefined()
    {
        var builder = new InstListBuilder();
        builder.Add((byte)LlirInstructionKind.Jump, InstructionFlag.None, 0, UndefinedLabel);
        _slab = builder.Build();
        _slabBuilt = true;
    }

    [Given(@"low-level code that calls a label that is never defined")]
    public void GivenLowLevelCodeThatCallsALabelThatIsNeverDefined()
    {
        var builder = new InstListBuilder();
        builder.Add((byte)LlirInstructionKind.Call, InstructionFlag.None, 0, UndefinedLabel);
        _slab = builder.Build();
        _slabBuilt = true;
    }

    [Given(@"low-level code that branches to a label that is never defined")]
    public void GivenLowLevelCodeThatBranchesToALabelThatIsNeverDefined()
    {
        var builder = new InstListBuilder();
        builder.Add((byte)LlirInstructionKind.Cmp, InstructionFlag.None, 0, 0x80, 0);
        builder.Add((byte)LlirInstructionKind.Branch, InstructionFlag.None, 0, UndefinedLabel);
        _slab = builder.Build();
        _slabBuilt = true;
    }

    [Given(@"low-level code that jumps to a label that is defined")]
    public void GivenLowLevelCodeThatJumpsToALabelThatIsDefined()
    {
        var builder = new InstListBuilder();
        builder.Add((byte)LlirInstructionKind.Jump, InstructionFlag.None, 0, DefinedLabel);
        builder.Add((byte)LlirInstructionKind.Label, InstructionFlag.None, 0, DefinedLabel);
        builder.Add((byte)LlirInstructionKind.Load, InstructionFlag.None, 0, 0x42);
        _slab = builder.Build();
        _slabBuilt = true;
    }

    [When(@"I generate the Atari 2600 ROM")]
    public void WhenIGenerateTheAtari2600Rom()
    {
        Assert.That(_slabBuilt, Is.True, "No low-level code was set up before generating the ROM.");
        try
        {
            _rom = new Atari2600CodeGenerator().GenerateFromSlab(_slab, new StringPool(), new CodeGenOptions());
        }
        catch (GameVmException ex)
        {
            _generationError = ex;
        }
    }

    [Then(@"ROM generation fails with an unresolved label error")]
    public void ThenRomGenerationFailsWithAnUnresolvedLabelError()
    {
        Assert.That(_generationError, Is.Not.Null,
            "Expected ROM generation to fail loudly on the unresolved label, but it produced a ROM instead.");
        Assert.That(_rom, Is.Null, "No ROM should be produced when a label cannot be resolved.");
    }

    [Then(@"ROM generation succeeds")]
    public void ThenRomGenerationSucceeds()
    {
        Assert.That(_generationError, Is.Null,
            $"Expected ROM generation to succeed, but it failed: {_generationError?.Message}");
        Assert.That(_rom, Is.Not.Null);
        Assert.That(_rom!.Length, Is.EqualTo(4096));
    }
}
