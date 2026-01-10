using System.Collections.Generic;

namespace GameVM.Compiler.Pascal
{
    public class ArrayTypeNode : TypeNode
    {
        public required TypeNode ElementType { get; set; }
        public required List<RangeNode> Dimensions { get; set; }
    }
}