using System.Collections.Generic;

namespace GameVM.Compiler.Pascal
{
    public class ProcedureNode : PascalASTNode
    {
        public required string Name { get; set; }
        public required List<VariableNode> Parameters { get; set; }
        public required PascalASTNode Block { get; set; }
    }
}
