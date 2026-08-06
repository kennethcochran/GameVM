using System;
using System.IO;
using GameVM.Compiler.Core.IR.Slab;
using GameVM.Compiler.Core.IR.Soa;
using GameVM.Compiler.Core.IR.SlabProcessing;
using NUnit.Framework;
namespace GameVM.Compiler.Core.Tests.IR.Slab;

public class SlabPrinterTests
{
    private static InstList BuildSlab()
    {
        // 3 instructions:
        //  [0] NOP (kind 0, no args)
        //  [1] ADD kind 4, 2 args (terminator)
        //  [2] CALL kind 22, 1 arg (diagnostic)
        var builder = new InstListBuilder();
        builder.Add(0, InstructionFlag.None, 0);
        builder.Add(4, InstructionFlag.Terminator, 0, 0xDEAD0001u, 0xDEAD0002u);
        builder.Add(22, InstructionFlag.Diagnostic, 0, 0x00001234u);
        return builder.Build();
    }

    [Test]
    public void Print_EmitsInstructionsAndBlockBoundary()
    {
        var output = new SlabPrinter(BuildSlab()).Print();

        Assert.That(output, Does.Contain("; === instructions ==="));
        Assert.That(output, Does.Contain("; --- basic block boundary ---"));
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
        var builder = new InstListBuilder();
        builder.Add(200, InstructionFlag.None, 0); // unmapped kind
        builder.Add(1, InstructionFlag.None, 0);   // LOAD

        var output = new SlabPrinter(builder.Build()).Print();

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
    public void Print_EmptyList_EmitsHeaderOnly()
    {
        var output = new SlabPrinter(new InstListBuilder().Build()).Print();
        Assert.That(output, Does.Not.Contain(":"), "No instruction lines should be printed for an empty list");
    }
}