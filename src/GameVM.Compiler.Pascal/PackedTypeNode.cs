namespace GameVM.Compiler.Pascal
{
    public class PackedTypeNode : TypeNode
    {
        public required TypeNode InnerType { get; set; }
    }
}
