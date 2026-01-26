namespace GameVM.Compiler.Pascal
{
    public class GotoNode : PascalAstNode
    {
        public required int TargetLabel { get; set; }
    }
}
