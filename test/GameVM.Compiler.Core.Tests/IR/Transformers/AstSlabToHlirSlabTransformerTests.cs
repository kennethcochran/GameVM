using GameVM.Compiler.Core.IR.Transformers;
using GameVM.Compiler.Core.IR.Slab;
using GameVM.Compiler.Core.IR.Buffers;
using GameVM.Compiler.Core.IR.Soa;
using GameVM.Compiler.Core.IR;

namespace GameVM.Compiler.Core.Tests.IR.Transformers;

public class AstSlabToHlirSlabTransformerTests
{
    private readonly StringPool _stringPool = new StringPool();
    private readonly AstSlabToHlirSlabTransformer _transformer;

    public AstSlabToHlirSlabTransformerTests()
    {
        _transformer = new AstSlabToHlirSlabTransformer(_stringPool);
    }

    [Test]
    public void Transform_SingleAssignment_ProducesHlirAssign()
    {
        // AST slab layout via InstListBuilder:
        // - METHOD_DECLARATION: [name=0, bodyOffset=BLOCK]
        // - BLOCK: [stmt1=VAR_DECL, stmt2=ASSIGNMENT]
        // - VARIABLE_DECLARATION: [typeKind=1, nameOffset=x]
        // - ASSIGNMENT: [target=IDENT(x), value=LITERAL_INT(42)]
        
        var builder = new InstListBuilder();
        var xOffset = _stringPool.Intern("x");

        // Use temporary indices to construct AST
        // Since builder appends sequentially, let's build from bottom up to get indices or use placeholders:
        // Index 0: VARIABLE_DECLARATION (Integer, x)
        int varDeclIdx = builder.Add((byte)AstNodeKind.VariableDeclaration, InstructionFlag.None, 2, 1u, xOffset);
        
        // Index 1: IDENTIFIER (x)
        int identIdx = builder.Add((byte)AstNodeKind.Identifier, InstructionFlag.None, 1, xOffset);
        
        // Index 2: LITERAL_INT (42)
        int literalIdx = builder.Add((byte)AstNodeKind.LiteralInt, InstructionFlag.None, 1, 42u);
        
        // Index 3: ASSIGNMENT (identIdx, literalIdx)
        int assignIdx = builder.Add((byte)AstNodeKind.Assignment, InstructionFlag.None, 2, (uint)identIdx, (uint)literalIdx);
        
        // Index 4: BLOCK [varDeclIdx, assignIdx]
        int blockIdx = builder.Add((byte)AstNodeKind.Block, InstructionFlag.None, 2, (uint)varDeclIdx, (uint)assignIdx);
        
        // Index 5: METHOD_DECLARATION [nameOffset, blockIdx]
        builder.Add((byte)AstNodeKind.MethodDeclaration, InstructionFlag.None, 2, xOffset, (uint)blockIdx);

        InstList astSlab = builder.Build();
        InstList result = _transformer.Transform(astSlab);

        Assert.That(result.Count, Is.GreaterThan(0));
        bool assignFound = false;
        for (int i = 0; i < result.Count; i++)
        {
            if (result.GetKind(i) == (byte)MlirInstructionKind.Assign)
            {
                assignFound = true;
                break;
            }
        }
        Assert.That(assignFound, Is.True);
    }
}