namespace GameVM.Compiler.Pascal
{
    public class VariantCaseNode : PascalASTNode
    {
        public required object Value { get; set; }
        public required List<FieldDeclarationNode> Fields { get; set; }
    }
}
