using System.Collections.Generic;

namespace GameVM.Compiler.Pascal
{
    public class ProgramNode : PascalAstNode
    {
        public required string Name { get; set; }
        public required List<PascalAstNode> UsesUnits { get; set; }
        public required BlockNode Block { get; set; }

        public ProgramNode()
        {
            UsesUnits = new List<PascalAstNode>();
        }
    }
}
