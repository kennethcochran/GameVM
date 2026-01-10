using System.Collections.Generic;

namespace GameVM.Compiler.Pascal
{
    public class CaseBranchNode : PascalASTNode
    {
        public required List<ExpressionNode> Labels { get; set; }
        public required PascalASTNode Statement { get; set; }
    }
}