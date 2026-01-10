using System.Collections.Generic;

namespace GameVM.Compiler.Pascal
{
    public class SetNode : ExpressionNode
    {
        public required List<ExpressionNode> Elements { get; set; }
    }
}