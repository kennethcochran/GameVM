namespace GameVM.Compiler.Pascal
{
    public class VariableDeclarationNode : PascalAstNode
    {
        public required string Name { get; set; }
        public required TypeNode Type { get; set; }
    }
}
