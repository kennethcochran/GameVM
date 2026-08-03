using GameVM.Compiler.Core.IR.Slab;
using GameVM.Compiler.Core.IR.SlabProcessing;
namespace GameVM.Compiler.Core.Tests.IR.Slab;

public class SlabPrinterTests
{
    private static uint[] BuildSlab()
    {
        // Header (6) + 3 instruction blocks:
        //  [6] NOP (kind 0, size 1)
        //  [7] ADD size 3, 2 args (terminator)
        // [10] CALL size 2, 1 arg
        var slab = new uint[12];
        var header = SlabHeader.ForStage(irStage: 1, elementCount: 6); // 6 elements past header
        header.WriteTo(slab);

        slab[6] = InstructionMetadata.Encode(0, 1, 0);                       // NOP
        slab[7] = InstructionMetadata.Encode(4, 3, 2, isTerminator: true);  // ADD, 2 args, TERM
        slab[8] = 0xDEAD0001u;
        slab[9] = 0xDEAD0002u;
        slab[10] = InstructionMetadata.Encode(22, 2, 1, hasDiagnostic: true); // CALL, 1 arg, DIAG
        slab[11] = 0x00001234u;
        return slab;
    }

    [Test]
    public void Print_EmitsHeaderSummary()
    {
        var output = new SlabPrinter(BuildSlab()).Print();

        Assert.That(output, Does.Contain("magic    = 0x4741564D"));
        Assert.That(output, Does.Contain("version  = 1.0"));
        Assert.That(output, Does.Contain("ir-stage = 1 (HLIR)"));
        Assert.That(output, Does.Contain("elements = 6"));
    }

    [Test]
    public void Print_RendersMnemonicAndArgsPerBlock()
    {
        var output = new SlabPrinter(BuildSlab()).Print();

        Assert.That(output, Does.Contain("    6: NOP"));
        Assert.That(output, Does.Contain("    7: ADD DEAD0001 DEAD0002 [TERM]"));
        Assert.That(output, Does.Contain("   10: CALL 00001234 [DIAG]"));
    }

    [Test]
    public void Print_MarksBasicBlockBoundaryAfterTerminator()
    {
        var output = new SlabPrinter(BuildSlab()).Print();

        Assert.That(output, Does.Contain("; --- basic block boundary ---"));
        // Boundary appears after the ADD (terminator) line and before the CALL line.
        var addIdx = output.IndexOf("ADD", StringComparison.Ordinal);
        var boundaryIdx = output.IndexOf("basic block boundary", StringComparison.Ordinal);
        var callIdx = output.IndexOf("CALL", StringComparison.Ordinal);
        Assert.That(boundaryIdx, Is.GreaterThan(addIdx));
        Assert.That(callIdx, Is.GreaterThan(boundaryIdx));
    }

    [Test]
    public void Print_RendersUnknownKindAsUnk()
    {
        var slab = new uint[8];
        SlabHeader.ForStage(0, 2).WriteTo(slab);
        slab[6] = InstructionMetadata.Encode(200, 1, 0); // unmapped kind
        slab[7] = InstructionMetadata.Encode(1, 1, 0);   // LOAD

        var output = new SlabPrinter(slab).Print();

        Assert.That(output, Does.Contain("UNK.200"));
        Assert.That(output, Does.Contain("LOAD"));
    }

    [Test]
    public void Print_ToWriter_WritesEquivalentContent()
    {
        var slab = BuildSlab();
        var sb = new System.Text.StringBuilder();
        new SlabPrinter(slab).Print(new StringWriter(sb));

        Assert.That(sb.ToString(), Is.EqualTo(new SlabPrinter(slab).Print()));
    }

    [Test]
    public void Print_RejectsSlabWithoutHeader()
    {
        var tiny = new uint[3];
        Assert.That(() => new SlabPrinter(tiny).Print(), Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public void Print_RejectsCorruptZeroSizeBlock()
    {
        var slab = new uint[7];
        SlabHeader.ForStage(0, 1).WriteTo(slab);
        slab[6] = InstructionMetadata.Encode(1, 0, 0); // size 0 -> corrupt

        Assert.That(() => new SlabPrinter(slab).Print(), Throws.TypeOf<InvalidOperationException>());
    }
}
