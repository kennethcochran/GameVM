namespace GameVM.Compiler.Pascal
{
    public class ConstantDeclarationNode : PascalASTNode
    {
        public required string Name { get; set; }
        public required ExpressionNode Value { get; set; }
    }
}
