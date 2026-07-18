using GameVM.Compiler.Core.IR.Slab;
using GameVM.Compiler.Core.Utilities;
using NUnit.Framework;

using GameVM.Compiler.Core.IR.SlabProcessing;
namespace GameVM.Compiler.Core.Tests.IR.Slab;

public class SlabCompactionUtilityTests
{
    private static readonly System.Func<uint, bool> KeepAll = _ => true;
    private static readonly System.Func<uint, bool> DropAll = _ => false;
    private static readonly System.Func<uint, bool> DefaultKeep = m => !MetadataDecoder.IsNop(m); // matches ProcessNext default (keep unless NOP)

    private static uint[] BuildSlab(params uint[] instructions)
    {
        var slab = new uint[6 + instructions.Length];
        slab[0] = 0x4741564D; // magic
        slab[1] = 1;
        slab[2] = 0;
        slab[3] = 1;
        slab[4] = (uint)instructions.Length; // element count
        slab[5] = 0;
        Array.Copy(instructions, 0, slab, 6, instructions.Length);
        return slab;
    }

    private static void RunToEnd(SlabCompactionUtility util)
    {
        while (util.ProcessNext(KeepAll))
        {
            _ = util.CompactedElementCount; // exercise accessor per block
        }
    }

    [Test]
    public void Constructor_WithNullSource_ThrowsArgumentNullException()
    {
        var target = new uint[32];
        Assert.Throws<System.ArgumentNullException>(() => new SlabCompactionUtility(null!, target));
    }

    [Test]
    public void Constructor_WithNullTarget_ThrowsArgumentNullException()
    {
        var source = BuildSlab(MetadataEncoder.CreateNop());
        Assert.Throws<System.ArgumentNullException>(() => new SlabCompactionUtility(source, null!));
    }

    [Test]
    public void ProcessNext_KeepsAllInstructions_CopiesEachBlock()
    {
        var source = BuildSlab(
            MetadataEncoder.Encode(1, 1, 0),
            MetadataEncoder.Encode(2, 1, 0));
        var target = new uint[32];

        var util = new SlabCompactionUtility(source, target);
        Assert.That(util.ProcessNext(KeepAll), Is.True);
        Assert.That(util.ProcessNext(KeepAll), Is.True);
        Assert.That(util.ProcessNext(KeepAll), Is.False); // end of slab

        // Header copied (6) + 2 single-uint blocks
        Assert.That(util.CompactedElementCount, Is.EqualTo(8));
        Assert.That(target[6], Is.EqualTo(MetadataEncoder.Encode(1, 1, 0)));
        Assert.That(target[7], Is.EqualTo(MetadataEncoder.Encode(2, 1, 0)));
    }

    [Test]
    public void ProcessNext_DefaultCallback_DropsNops()
    {
        var source = BuildSlab(
            MetadataEncoder.Encode(1, 1, 0),
            MetadataEncoder.CreateNop(), // tombstone -> dropped by default
            MetadataEncoder.Encode(2, 1, 0));
        var target = new uint[32];

        var util = new SlabCompactionUtility(source, target);
        Assert.That(util.ProcessNext(DefaultKeep), Is.True);
        Assert.That(util.ProcessNext(DefaultKeep), Is.True);
        Assert.That(util.ProcessNext(DefaultKeep), Is.True);
        Assert.That(util.ProcessNext(DefaultKeep), Is.False);
        util.FinalizeHeader();

        // NOP dropped: only 2 blocks survive
        Assert.That(util.CompactedElementCount, Is.EqualTo(8));
        Assert.That(target[4], Is.EqualTo(2u)); // header element count updated
        Assert.That(target[6], Is.EqualTo(MetadataEncoder.Encode(1, 1, 0)));
        Assert.That(target[7], Is.EqualTo(MetadataEncoder.Encode(2, 1, 0)));
    }

    [Test]
    public void ProcessNext_KeepCallbackCanRetainNop()
    {
        var source = BuildSlab(MetadataEncoder.CreateNop());
        var target = new uint[32];

        var util = new SlabCompactionUtility(source, target);
        // Explicit keep callback retains the NOP
        Assert.That(util.ProcessNext(KeepAll), Is.True);
        Assert.That(util.ProcessNext(KeepAll), Is.False);

        Assert.That(util.CompactedElementCount, Is.EqualTo(7));
        Assert.That(target[6], Is.EqualTo(MetadataEncoder.CreateNop()));
    }

    [Test]
    public void ProcessNext_DropsSelectedInstructions_CompactsGaps()
    {
        var source = BuildSlab(
            MetadataEncoder.Encode(1, 1, 0),
            MetadataEncoder.Encode(2, 1, 0),
            MetadataEncoder.Encode(3, 1, 0));
        var target = new uint[32];

        var util = new SlabCompactionUtility(source, target);
        int iterations = 0;
        while (util.ProcessNext(meta => MetadataDecoder.DecodeKind(meta) != 2))
        {
            iterations++;
        }
        util.FinalizeHeader();

        // 3 blocks visited (incl. dropped kind==2); only 2 survive in target
        Assert.That(iterations, Is.EqualTo(3));
        Assert.That(util.CompactedElementCount, Is.EqualTo(8));
        Assert.That(target[4], Is.EqualTo(2u));
        Assert.That(target[6], Is.EqualTo(MetadataEncoder.Encode(1, 1, 0)));
        Assert.That(target[7], Is.EqualTo(MetadataEncoder.Encode(3, 1, 0)));
    }

    [Test]
    public void ProcessNext_VariableSizeBlocks_CopiesOperands()
    {
        // size-2 block (metadata + 1 operand) followed by size-1 block
        var source = BuildSlab(
            MetadataEncoder.Encode(7, 2, 1),
            0xABCD,
            MetadataEncoder.Encode(8, 1, 0));
        var target = new uint[32];

        var util = new SlabCompactionUtility(source, target);
        RunToEnd(util);
        util.FinalizeHeader();

        // Header (6) + size-2 block + size-1 block = 9
        Assert.That(util.CompactedElementCount, Is.EqualTo(9));
        Assert.That(target[6], Is.EqualTo(MetadataEncoder.Encode(7, 2, 1)));
        Assert.That(target[7], Is.EqualTo(0xABCD)); // operand preserved
        Assert.That(target[8], Is.EqualTo(MetadataEncoder.Encode(8, 1, 0)));
    }

    [Test]
    public void FinalizeHeader_UpdatesElementCountExcludingHeader()
    {
        var source = BuildSlab(MetadataEncoder.Encode(1, 1, 0));
        var target = new uint[32];

        var util = new SlabCompactionUtility(source, target);
        RunToEnd(util);
        util.FinalizeHeader();

        // 1 instruction block preserved; count excludes 6 header indices
        Assert.That(target[4], Is.EqualTo(1u));
    }

    [Test]
    public void ProcessNext_EmptyInstructionBody_ReturnsFalseImmediately()
    {
        var source = BuildSlab(); // header only
        var target = new uint[32];

        var util = new SlabCompactionUtility(source, target);
        Assert.That(util.ProcessNext(KeepAll), Is.False);
        Assert.That(util.CompactedElementCount, Is.EqualTo(6)); // only header copied
    }

    [Test]
    public void CompactedElementCount_StartsAtHeaderSize()
    {
        var source = BuildSlab(MetadataEncoder.CreateNop());
        var target = new uint[32];

        var util = new SlabCompactionUtility(source, target);
        Assert.That(util.CompactedElementCount, Is.EqualTo(6)); // header copied in ctor
    }

    [Test]
    public void ProcessNext_DropAll_LeavesOnlyHeader()
    {
        var source = BuildSlab(MetadataEncoder.Encode(1, 1, 0));
        var target = new uint[32];

        var util = new SlabCompactionUtility(source, target);
        int processed = 0;
        while (util.ProcessNext(DropAll))
        {
            processed++;
        }
        util.FinalizeHeader();

        Assert.That(processed, Is.EqualTo(1));
        Assert.That(util.CompactedElementCount, Is.EqualTo(6));
        Assert.That(target[4], Is.EqualTo(0u));
    }

    [Test]
    public void ProcessNext_TargetSlabTooSmall_ThrowsInvalidOperationException()
    {
        // 2 single-uint blocks need 8 slots (header 6 + 2); target only has 7
        var source = BuildSlab(
            MetadataEncoder.Encode(1, 1, 0),
            MetadataEncoder.Encode(2, 1, 0));
        var target = new uint[7];

        var util = new SlabCompactionUtility(source, target);
        // First block fits (writes header slot + 1); second overflows the 7-slot target
        Assert.Throws<System.InvalidOperationException>(() =>
        {
            util.ProcessNext(KeepAll);
            util.ProcessNext(KeepAll);
        });
    }
}
