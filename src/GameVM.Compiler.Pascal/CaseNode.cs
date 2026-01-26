using System.Collections.Generic;

namespace GameVM.Compiler.Pascal
{
    public class CaseNode : PascalAstNode
    {
        public required ExpressionNode Selector { get; set; }
        public required List<CaseBranchNode> Branches { get; set; } = new();
        public PascalAstNode? ElseBlock { get; set; }
    }
}