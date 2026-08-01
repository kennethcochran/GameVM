using NUnit.Framework;
using GameVM.Compiler.Core.IR.Transformers;
using GameVM.Compiler.Core.IR.Slab;
using GameVM.Compiler.Core.IR.SlabProcessing;
using GameVM.Compiler.Core.IR.Buffers;
using static GameVM.Compiler.Core.IR.Slab.InstructionMetadataFlags;
using static GameVM.Compiler.Core.IR.Slab.InstructionMetadata;

namespace GameVM.Compiler.Core.Tests.IR.Transformers;

public class AstSlabToHlirSlabTransformerTests
{
    private readonly ArenaAllocator _arena = new ArenaAllocator();
    private readonly AstSlabToHlirSlabTransformer _transformer;

    public AstSlabToHlirSlabTransformerTests()
    {
        _transformer = new AstSlabToHlirSlabTransformer(_arena, new StringPool());
    }

    [Test]
    public void Transform_SingleAssignment_ProducesHlirAssign()
    {
        // AST slab layout:
        // [0-5]   Header
        // [6-8]   METHOD_DECLARATION [metadata, name=0, bodyOffset=9]
        // [9-11]  BLOCK [metadata, stmt1=12, stmt2=15]
        // [12-14] VARIABLE_DECLARATION [metadata, typeKind=1(Integer), nameOffset=0]
        // [15-17] ASSIGNMENT [metadata, target=18(IDENTIFIER), value=20(LITERAL_INT)]
        // [18-19] IDENTIFIER [metadata, nameOffset=0]
        // [20-21] LITERAL_INT [metadata, value=42]
        uint[] astSlab = new uint[]
        {
            SlabHeader.Magic, 1, 0, 0, 1, 0,
            Encode(METHOD_DECLARATION, 3, 2), 0u, 9u,
            Encode(BLOCK, 3, 2), 12u, 15u,
            Encode(VARIABLE_DECLARATION, 3, 2), 1u, 0u,
            Encode(ASSIGNMENT, 3, 2), 18u, 20u,
            Encode(IDENTIFIER, 2, 1), 0u,
            Encode(LITERAL_INT, 2, 1), 42u
        };

        uint[] result = _transformer.Transform(astSlab);

        Assert.That(SlabHeader.Read(result).IrStage, Is.EqualTo(1u));
        bool assignFound = false;
        for (int i = SlabHeader.HeaderIndex.Length; i < result.Length; i++)
        {
            if (DecodeKind(result[i]) == HLIR_ASSIGN)
            {
                assignFound = true;
                break;
            }
        }
        Assert.That(assignFound, Is.True);
    }
}