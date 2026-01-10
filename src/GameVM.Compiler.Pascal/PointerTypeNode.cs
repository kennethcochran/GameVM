namespace GameVM.Compiler.Pascal
{
    public class PointerTypeNode : TypeNode
    {
        public required TypeNode TargetType { get; set; }

        public PointerTypeNode() {
            TypeName = "pointer";
        }
    }
}