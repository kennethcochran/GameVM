using System.Collections.Generic;

namespace GameVM.Compiler.Pascal
{
    public class ProcedureCallNode : PascalAstNode
    {
        public required string Name { get; set; }
        public required List<PascalAstNode> Arguments { get; set; }
    }
}