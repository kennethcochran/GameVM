using System.Collections.Generic;

namespace GameVM.Compiler.Pascal
{
    public class BlockNode : PascalAstNode
    {
        public required List<PascalAstNode> Statements { get; set; } = new();
    }
}
