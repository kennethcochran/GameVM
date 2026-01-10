using System.Collections.Generic;

namespace GameVM.Compiler.Pascal
{
    public class WithNode : PascalASTNode
    {
        public required List<VariableNode> RecordVariables { get; set; }
        public required PascalASTNode Block { get; set; }
    }
}