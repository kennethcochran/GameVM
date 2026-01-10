using System.Collections.Generic;

namespace GameVM.Compiler.Pascal
{
    public class RecordTypeNode : TypeNode
    {
        public required List<FieldDeclarationNode> Fields { get; set; }

        public RecordTypeNode()
        {
            Fields = new List<FieldDeclarationNode>();
            TypeName = "record";
        }
    }
}