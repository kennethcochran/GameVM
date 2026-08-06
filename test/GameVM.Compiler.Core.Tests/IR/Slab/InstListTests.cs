using System;
using NUnit.Framework;
using GameVM.Compiler.Core.IR.Soa;

namespace GameVM.Compiler.Core.Tests.IR.Slab;

[TestFixture]
public class InstListTests
{
    [Test]
    public void InstList_BasicOperations_WorksCorrectly()
    {
        var builder = new InstListBuilder();
        
        builder.Add(0, InstructionFlag.None, 0);
        builder.Add(0, InstructionFlag.Terminator, 1, 42u);
        builder.Add(0, InstructionFlag.Diagnostic, 100, 100u, 200u);
        builder.Add(0, InstructionFlag.None, 1, 1u, 2u, 3u);
        builder.Add(0, InstructionFlag.None, 10, 10u, 20u, 30u, 40u); // Exactly MAX_FIXED_OPS=4
        
        Span<uint> fiveOps = [50u, 60u, 70u, 80u, 90u];
        builder.Add(0, InstructionFlag.None, 10, fiveOps); // 5 operands (extra pool)
        
        InstList list = builder.Build();
        
        Assert.That(list.Count, Is.EqualTo(6));
        
        // Test instruction 0: no operands
        Assert.That(list.GetKind(0), Is.EqualTo((byte)0));
        Assert.That(list.GetFlags(0), Is.EqualTo((ushort)0));
        Assert.That(list.GetArgCount(0), Is.EqualTo((ushort)0));
        Assert.That(list.GetBlockId(0), Is.EqualTo(0));
        Assert.That(list.GetOperands(0).Length, Is.EqualTo(0));
        
        // Test instruction 1: one operand
        Assert.That(list.GetKind(1), Is.EqualTo((byte)0));
        Assert.That(list.GetFlags(1), Is.EqualTo((ushort)InstructionFlag.Terminator));
        Assert.That(list.GetArgCount(1), Is.EqualTo((ushort)1));
        Assert.That(list.GetBlockId(1), Is.EqualTo(1));
        Assert.That(list.GetOperands(1).Length, Is.EqualTo(1));
        Assert.That(list.GetOperands(1)[0], Is.EqualTo(42u));
        
        // Test instruction 2: two operands
        Assert.That(list.GetKind(2), Is.EqualTo((byte)0));
        Assert.That(list.GetFlags(2), Is.EqualTo((ushort)InstructionFlag.Diagnostic));
        Assert.That(list.GetArgCount(2), Is.EqualTo((ushort)2));
        Assert.That(list.GetBlockId(2), Is.EqualTo(100));
        Assert.That(list.GetOperands(2).Length, Is.EqualTo(2));
        Assert.That(list.GetOperands(2)[0], Is.EqualTo(100u));
        Assert.That(list.GetOperands(2)[1], Is.EqualTo(200u));
        
        // Test instruction 3: three operands
        Assert.That(list.GetKind(3), Is.EqualTo((byte)0));
        Assert.That(list.GetFlags(3), Is.EqualTo((ushort)0));
        Assert.That(list.GetArgCount(3), Is.EqualTo((ushort)3));
        Assert.That(list.GetBlockId(3), Is.EqualTo(1));
        Assert.That(list.GetOperands(3).Length, Is.EqualTo(3));
        Assert.That(list.GetOperands(3)[0], Is.EqualTo(1u));
        Assert.That(list.GetOperands(3)[1], Is.EqualTo(2u));
        Assert.That(list.GetOperands(3)[2], Is.EqualTo(3u));
        
        // Test instruction 4: four operands (exactly MAX_FIXED_OPS, fast path)
        Assert.That(list.GetKind(4), Is.EqualTo((byte)0));
        Assert.That(list.GetFlags(4), Is.EqualTo((ushort)0));
        Assert.That(list.GetArgCount(4), Is.EqualTo((ushort)4));
        Assert.That(list.GetBlockId(4), Is.EqualTo(10));
        Assert.That(list.GetOperands(4).Length, Is.EqualTo(4));
        Assert.That(list.GetOperands(4)[0], Is.EqualTo(10u));
        Assert.That(list.GetOperands(4)[1], Is.EqualTo(20u));
        Assert.That(list.GetOperands(4)[2], Is.EqualTo(30u));
        Assert.That(list.GetOperands(4)[3], Is.EqualTo(40u));
        
        // Test instruction 5: five operands (slow path, extra pool)
        Assert.That(list.GetKind(5), Is.EqualTo((byte)0));
        Assert.That(list.GetFlags(5), Is.EqualTo((ushort)0));
        Assert.That(list.GetArgCount(5), Is.EqualTo((ushort)5));
        Assert.That(list.GetBlockId(5), Is.EqualTo(10));
        Assert.That(list.GetOperands(5).Length, Is.EqualTo(5));
        Assert.That(list.GetOperands(5)[0], Is.EqualTo(50u));
        Assert.That(list.GetOperands(5)[1], Is.EqualTo(60u));
        Assert.That(list.GetOperands(5)[2], Is.EqualTo(70u));
        Assert.That(list.GetOperands(5)[3], Is.EqualTo(80u));
        Assert.That(list.GetOperands(5)[4], Is.EqualTo(90u));
    }
    
    [Test]
    public void InstList_GetOperands_FastPath_SpanIsCorrect()
    {
        var builder = new InstListBuilder();
        builder.Add(0, InstructionFlag.None, 0, 10u, 20u, 30u); // 3 operands (<= MAX_FIXED_OPS=4)
        
        Span<uint> slowOps = [1u, 2u, 3u, 4u, 5u];
        builder.Add(0, InstructionFlag.None, 0, slowOps); // 5 operands (> MAX_FIXED_OPS)
        
        InstList list = builder.Build();
        
        // Fast path: span over fixedOps
        ReadOnlySpan<uint> ops1 = list.GetOperands(0);
        Assert.That(ops1.Length, Is.EqualTo(3));
        Assert.That(ops1[0], Is.EqualTo(10u));
        Assert.That(ops1[1], Is.EqualTo(20u));
        Assert.That(ops1[2], Is.EqualTo(30u));
        
        // Slow path: span over extra pool
        ReadOnlySpan<uint> ops2 = list.GetOperands(1);
        Assert.That(ops2.Length, Is.EqualTo(5));
        Assert.That(ops2[0], Is.EqualTo(1u));
        Assert.That(ops2[1], Is.EqualTo(2u));
        Assert.That(ops2[2], Is.EqualTo(3u));
        Assert.That(ops2[3], Is.EqualTo(4u));
        Assert.That(ops2[4], Is.EqualTo(5u));
    }
    
    [Test]
    public void InstList_GetOperand_ReturnsCorrectValue()
    {
        var builder = new InstListBuilder();
        builder.Add(0, InstructionFlag.None, 0, 100u, 200u, 300u); // 3 operands
        
        Span<uint> slowOps = [1u, 2u, 3u, 4u, 5u];
        builder.Add(0, InstructionFlag.None, 0, slowOps); // 5 operands
        
        InstList list = builder.Build();
        
        // Fast path
        Assert.That(list.GetOperand(0, 0), Is.EqualTo(100u));
        Assert.That(list.GetOperand(0, 1), Is.EqualTo(200u));
        Assert.That(list.GetOperand(0, 2), Is.EqualTo(300u));
        
        // Slow path
        Assert.That(list.GetOperand(1, 0), Is.EqualTo(1u));
        Assert.That(list.GetOperand(1, 1), Is.EqualTo(2u));
        Assert.That(list.GetOperand(1, 2), Is.EqualTo(3u));
        Assert.That(list.GetOperand(1, 3), Is.EqualTo(4u));
        Assert.That(list.GetOperand(1, 4), Is.EqualTo(5u));
    }
    
    [Test]
    public void InstList_GetOperandOffset_ReturnsCorrectOffset()
    {
        var builder = new InstListBuilder();
        builder.Add(0, InstructionFlag.None, 0, 10u, 20u); // 2 operands (fast path)
        
        Span<uint> slowOps = [1u, 2u, 3u, 4u, 5u];
        builder.Add(0, InstructionFlag.None, 0, slowOps); // 5 operands (slow path)
        
        InstList list = builder.Build();
        
        // Fast path
        Assert.That(list.GetOperandOffset(0, 0), Is.EqualTo(0 * InstConstants.MAX_FIXED_OPS + 0));
        Assert.That(list.GetOperandOffset(0, 1), Is.EqualTo(0 * InstConstants.MAX_FIXED_OPS + 1));
        
        // Slow path values verified via GetOperand
        Assert.That(list.GetOperand(1, 0), Is.EqualTo(1u));
        Assert.That(list.GetOperand(1, 1), Is.EqualTo(2u));
        Assert.That(list.GetOperand(1, 2), Is.EqualTo(3u));
        Assert.That(list.GetOperand(1, 3), Is.EqualTo(4u));
        Assert.That(list.GetOperand(1, 4), Is.EqualTo(5u));
    }
    
    [Test]
    public void InstList_Indexer_ReturnsCorrectMetadata()
    {
        var builder = new InstListBuilder();
        builder.Add(0, InstructionFlag.Terminator, 42, 100u, 200u);
        InstList list = builder.Build();
        
        InstMetadata meta = list[0];
        
        Assert.That(meta.Kind, Is.EqualTo((byte)0));
        Assert.That(meta.Flags, Is.EqualTo((ushort)InstructionFlag.Terminator));
        Assert.That(meta.ArgCount, Is.EqualTo((ushort)2));
        Assert.That(meta.BlockId, Is.EqualTo(42));
        Assert.That(meta.IsTerminator, Is.True);
        Assert.That(meta.IsDiagnostic, Is.False);
        Assert.That(meta.Index.Value, Is.EqualTo(0));
    }
    
    [Test]
    public void InstList_SpanProperties_ReturnCorrectSpans()
    {
        var builder = new InstListBuilder();
        builder.Add(0, InstructionFlag.Terminator, 0, 1u, 2u); // Inst 0: 2 operands
        builder.Add(0, InstructionFlag.Diagnostic, 1, 3u, 4u, 5u); // Inst 1: 3 operands
        builder.Add(0, InstructionFlag.None, 2, 6u); // Inst 2: 1 operand
        
        InstList list = builder.Build();
        
        ReadOnlySpan<byte> tags = list.Tags;
        Assert.That(tags.Length, Is.EqualTo(3));
        
        ReadOnlySpan<ushort> flags = list.Flags;
        Assert.That(flags.Length, Is.EqualTo(3));
        Assert.That(flags[0], Is.EqualTo((ushort)InstructionFlag.Terminator));
        Assert.That(flags[1], Is.EqualTo((ushort)InstructionFlag.Diagnostic));
        Assert.That(flags[2], Is.EqualTo((ushort)0));
        
        ReadOnlySpan<ushort> argCounts = list.ArgCounts;
        Assert.That(argCounts.Length, Is.EqualTo(3));
        Assert.That(argCounts[0], Is.EqualTo((ushort)2));
        Assert.That(argCounts[1], Is.EqualTo((ushort)3));
        Assert.That(argCounts[2], Is.EqualTo((ushort)1));
        
        ReadOnlySpan<int> blockIds = list.BlockIds;
        Assert.That(blockIds.Length, Is.EqualTo(3));
        Assert.That(blockIds[0], Is.EqualTo(0));
        Assert.That(blockIds[1], Is.EqualTo(1));
        Assert.That(blockIds[2], Is.EqualTo(2));
        
        ReadOnlySpan<uint> fixedOps = list.FixedOps;
        Assert.That(fixedOps.Length, Is.EqualTo(3 * InstConstants.MAX_FIXED_OPS));
        Assert.That(fixedOps[0], Is.EqualTo(1u)); // Inst 0, operand 0
        Assert.That(fixedOps[1], Is.EqualTo(2u)); // Inst 0, operand 1
        Assert.That(fixedOps[4], Is.EqualTo(3u)); // Inst 1, operand 0
        Assert.That(fixedOps[5], Is.EqualTo(4u)); // Inst 1, operand 1
        Assert.That(fixedOps[6], Is.EqualTo(5u)); // Inst 1, operand 2
        Assert.That(fixedOps[8], Is.EqualTo(6u)); // Inst 2, operand 0
        
        ReadOnlySpan<uint> extra = list.Extra;
        // All instructions have operands <= MAX_FIXED_OPS (4), so no extra used
        Assert.That(extra.Length, Is.EqualTo(0));
    }
    
    [Test]
    public void InstList_SpanProperties_WithExtraPool_ExtraIsCorrect()
    {
        var builder = new InstListBuilder();
        builder.Add(0, InstructionFlag.None, 0, 1u, 2u, 3u, 4u); // 4 operands (fits in fixed)
        
        Span<uint> extraOps = [10u, 20u, 30u, 40u, 50u]; // 5 operands (extra pool)
        builder.Add(0, InstructionFlag.None, 0, extraOps);
        
        InstList list = builder.Build();
        
        ReadOnlySpan<uint> extra = list.Extra;
        Assert.That(extra.Length, Is.EqualTo(5)); // All 5 operands in extra
        Assert.That(extra[4], Is.EqualTo(50u)); // The last operand
    }
    
    [Test]
    public void InstList_InstMetadata_GetOperands_ReturnsCorrectSpan()
    {
        var builder = new InstListBuilder();
        builder.Add(0, InstructionFlag.None, 42, 10u, 20u, 30u);
        InstList list = builder.Build();
        InstMetadata meta = list[0];
        
        ReadOnlySpan<uint> ops = meta.GetOperands(list);
        
        Assert.That(ops.Length, Is.EqualTo(3));
        Assert.That(ops[0], Is.EqualTo(10u));
        Assert.That(ops[1], Is.EqualTo(20u));
        Assert.That(ops[2], Is.EqualTo(30u));
    }
    
    [Test]
    public void InstList_Constructor_CreatesValidList()
    {
        // Arrange
        byte[] tags = { 10, 20, 30 };
        ushort[] flags = { (ushort)InstructionFlag.Terminator, (ushort)InstructionFlag.None, (ushort)InstructionFlag.Diagnostic };
        ushort[] argCounts = { 2, 1, 3 };
        uint[] fixedOps = { 100, 200, 0, 0, 0, 300, 0, 0, 0, 0, 0, 400 };
        uint[] extra = { 500 };
        uint[] extraOffsets = { 0, 0, 0 };
        int[] blockIds = { 0, 1, 2 };
        
        // Act
        var list = new InstList(tags, flags, argCounts, fixedOps, extra, extraOffsets, blockIds, 3, 1);
        
        // Assert
        Assert.That(list.Count, Is.EqualTo(3));
        Assert.That(list.ExtraUsed, Is.EqualTo(1));
        Assert.That(list.GetKind(0), Is.EqualTo((byte)10));
        Assert.That(list.GetFlags(1), Is.EqualTo((ushort)InstructionFlag.None));
        Assert.That(list.GetArgCount(2), Is.EqualTo((ushort)3));
        Assert.That(list.GetBlockId(2), Is.EqualTo(2));
    }
    
    [Test]
    public void InstListBuilder_AutoResize_HandlesGrowth()
    {
        // Arrange: use small initial capacity
        var builder = new InstListBuilder(2);
        
        // Act: add more than initial capacity
        for (int i = 0; i < 10; i++)
        {
            builder.Add((byte)i, InstructionFlag.None, 1, 0u); // 1 operand each
        }
        
        InstList list = builder.Build();
        
        // Assert
        Assert.That(list.Count, Is.EqualTo(10));
        for (int i = 0; i < 10; i++)
        {
            Assert.That(list.GetKind(i), Is.EqualTo((byte)i));
            Assert.That(list.GetArgCount(i), Is.EqualTo((ushort)1));
        }
    }
    
    [Test]
    public void InstListBuilder_ExtraPool_AutoResize_HandlesGrowth()
    {
        // Arrange: use tiny extra capacity
        var builder = new InstListBuilder(2);
        
        // Act: add instructions that need extra pool, filling it up
        for (int i = 0; i < 100; i++)
        {
            Span<uint> ops = [1u, 2u, 3u, 4u, 5u, (uint)i]; // 55 fixed + 1 extra)
            builder.Add((byte)i, InstructionFlag.None, 0, ops);
        }
        
        InstList list = builder.Build();
        
        // Assert
        Assert.That(list.Count, Is.EqualTo(100));
        Assert.That(list.ExtraUsed, Is.EqualTo(600)); // 6 operands per instruction * 100 instructions (all in extra)
        for (int i = 0; i < 100; i++)
        {
            Assert.That(list.GetOperand(i, 5), Is.EqualTo((uint)i));
        }
    }
    
    [Test]
    public void InstListBuilder_Append_WithMismatchedOperandCount_Throws()
    {
        var builder = new InstListBuilder();
        
        var ex = Assert.Throws<ArgumentException>(() =>
            builder.Append(0, InstructionFlag.None, 3, 0, new ReadOnlySpan<uint>(new uint[2])));
        
        Assert.That(ex!.Message, Does.Contain("Operand count"));
    }
    
    [Test]
    public void InstList_CompactExtra_DefragmentsPool()
    {
        // Arrange: create a list with extra pool entries
        var builder = new InstListBuilder();
        // First inst: fast path
        builder.Add(0, InstructionFlag.None, 0, 1u, 2u, 3u, 4u);
        // Second inst: slow path (5 operands)
        Span<uint> ops2 = [10u, 20u, 30u, 40u, 50u];
        builder.Add(0, InstructionFlag.None, 0, ops2);
        // Third inst: slow path (6 operands)
        Span<uint> ops3 = [100u, 200u, 300u, 400u, 500u, 600u];
        builder.Add(0, InstructionFlag.None, 0, ops3);
        
        InstList list = builder.Build();
        
        // Act: compact extra
        InstList compacted = list.CompactExtra();
        
        // Assert: should have same operand data
        Assert.That(compacted.GetOperands(1).Length, Is.EqualTo(5));
        Assert.That(compacted.GetOperand(1, 0), Is.EqualTo(10u));
        Assert.That(compacted.GetOperand(1, 4), Is.EqualTo(50u));
        Assert.That(compacted.GetOperands(2).Length, Is.EqualTo(6));
        Assert.That(compacted.GetOperand(2, 0), Is.EqualTo(100u));
        Assert.That(compacted.GetOperand(2, 5), Is.EqualTo(600u));
    }
    
    [Test]
    public void InstList_GetOperands_OutOfRange_Throws()
    {
        var builder = new InstListBuilder();
        builder.Add(0, InstructionFlag.None, 0);
        InstList list = builder.Build();
        
        Assert.Throws<ArgumentOutOfRangeException>(() => list.GetOperands(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => list.GetOperands(1)); // Only 1 instruction (index 0)
    }
    
    [Test]
    public void InstList_GetOperand_OutOfRange_Throws()
    {
        var builder = new InstListBuilder();
        builder.Add(0, InstructionFlag.None, 0, 1u, 2u);
        InstList list = builder.Build();
        
        Assert.Throws<ArgumentOutOfRangeException>(() => list.GetOperand(0, 2)); // Only 2 operands
    }
    
    [Test]
    public void Handle_Types_ImplicitConversion_Works()
    {
        InstIndex instIdx = InstIndex.FromInt(5);
        int raw = instIdx; // implicit conversion to int
        Assert.That(raw, Is.EqualTo(5));
        
        BlockId blockId = BlockId.FromInt(3);
        int rawBlock = blockId; // implicit conversion to int
        Assert.That(rawBlock, Is.EqualTo(3));
        
        SymbolId symbolId = SymbolId.FromInt(7);
        int rawSymbol = symbolId;
        Assert.That(rawSymbol, Is.EqualTo(7));
        
        SlotIndex slotIdx = SlotIndex.FromInt(2);
        int rawSlot = slotIdx;
        Assert.That(rawSlot, Is.EqualTo(2));
    }
    
    [Test]
    public void Handle_Types_ExplicitConversion_Works()
    {
        InstIndex instIdx = (InstIndex)10;
        Assert.That(instIdx.Value, Is.EqualTo(10));
        Assert.That(instIdx.IsValid, Is.True);
        
        BlockId blockId = (BlockId)4;
        Assert.That(blockId.Value, Is.EqualTo(4));
        Assert.That(blockId.IsAssigned, Is.True);
        Assert.That(blockId.IsValid, Is.True);
        
        Assert.That(BlockId.Unassigned.IsValid, Is.True);
        Assert.That(BlockId.Unassigned.IsAssigned, Is.False);
        Assert.That(BlockId.Invalid.IsValid, Is.False);
        Assert.That(BlockId.Invalid.IsAssigned, Is.False);
        
        Assert.That(InstIndex.Invalid.IsValid, Is.False);
        Assert.That(SymbolId.Invalid.IsValid, Is.False);
        Assert.That(SlotIndex.Invalid.IsValid, Is.False);
    }
    
    [Test]
    public void Handle_Types_ComparisonOperators_Work()
    {
        InstIndex a = InstIndex.FromInt(1);
        InstIndex b = InstIndex.FromInt(2);
        
        Assert.That(a < b, Is.True);
        Assert.That(a <= b, Is.True);
        Assert.That(b > a, Is.True);
        Assert.That(b >= a, Is.True);
        Assert.That(a == b, Is.False);
        Assert.That(a.Equals(a), Is.True);
        Assert.That(a != b, Is.True);
        Assert.That(a.CompareTo(b), Is.LessThan(0));
    }
    
    [Test]
    public void Handle_Types_ToString_Works()
    {
        Assert.That(InstIndex.FromInt(5).ToString(), Is.EqualTo("InstIndex(5)"));
        Assert.That(InstIndex.Invalid.ToString(), Is.EqualTo("InstIndex.Invalid"));
        
        Assert.That(BlockId.FromInt(3).ToString(), Is.EqualTo("BlockId(3)"));
        Assert.That(BlockId.Unassigned.ToString(), Is.EqualTo("BlockId.Unassigned"));
        Assert.That(BlockId.Invalid.ToString(), Is.EqualTo("BlockId.Invalid"));
        
        Assert.That(SymbolId.FromInt(7).ToString(), Is.EqualTo("SymbolId(7)"));
        Assert.That(SymbolId.Invalid.ToString(), Is.EqualTo("SymbolId.Invalid"));
        
        Assert.That(SlotIndex.FromInt(2).ToString(), Is.EqualTo("SlotIndex(2)"));
        Assert.That(SlotIndex.Invalid.ToString(), Is.EqualTo("SlotIndex.Invalid"));
    }
}