namespace GameVM.Compiler.Pascal
{
    public class FunctionNode : PascalAstNode
    {
        public required string Name { get; set; }
        public required List<VariableNode> Parameters { get; set; }
        public required PascalAstNode Block { get; set; }
    }
}
