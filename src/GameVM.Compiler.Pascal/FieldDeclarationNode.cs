namespace GameVM.Compiler.Pascal
{
    public class FieldDeclarationNode : PascalASTNode
    {
        public required string Name { get; set; }
        public required TypeNode Type { get; set; }
    }
}