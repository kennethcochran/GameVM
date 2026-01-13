using System.Collections.Generic;

namespace GameVM.Compiler.Pascal
{
    public class FunctionCallNode : ExpressionNode
    {
        public required string Name { get; set; }
        public required List<PascalASTNode> Arguments { get; set; }
    }
}