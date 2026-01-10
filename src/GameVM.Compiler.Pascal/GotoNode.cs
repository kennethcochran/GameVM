namespace GameVM.Compiler.Pascal
{
    public class GotoNode : PascalASTNode
    {
        public required int TargetLabel { get; set; }
    }
}
