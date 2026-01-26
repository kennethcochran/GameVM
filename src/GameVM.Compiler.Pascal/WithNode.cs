using System.Collections.Generic;

namespace GameVM.Compiler.Pascal
{
    public class WithNode : PascalAstNode
    {
        public required List<VariableNode> RecordVariables { get; set; }
        public required PascalAstNode Block { get; set; }
    }
}