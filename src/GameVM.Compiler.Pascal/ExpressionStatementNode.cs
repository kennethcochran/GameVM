namespace GameVM.Compiler.Pascal
{
    public class ExpressionStatementNode : PascalASTNode
    {
        public required PascalASTNode Expression { get; set; }
    }
}