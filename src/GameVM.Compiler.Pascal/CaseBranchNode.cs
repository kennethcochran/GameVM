using System.Collections.Generic;

namespace GameVM.Compiler.Pascal
{
    public class CaseBranchNode : PascalAstNode
    {
        public required List<ExpressionNode> Labels { get; set; }
        public required PascalAstNode Statement { get; set; }
    }
}