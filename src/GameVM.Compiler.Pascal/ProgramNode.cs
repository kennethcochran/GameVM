using System.Collections.Generic;

namespace GameVM.Compiler.Pascal
{
    public class ProgramNode : PascalASTNode
    {
        public required string Name { get; set; }
        public required List<PascalASTNode> UsesUnits { get; set; }
        public required BlockNode Block { get; set; }

        public ProgramNode()
        {
            UsesUnits = new List<PascalASTNode>();
        }
    }
}
