using System.Collections.Generic;

namespace GameVM.Compiler.Pascal
{
    public class BlockNode : PascalASTNode
    {
        public required List<PascalASTNode> Statements { get; set; } = new();
    }
}
