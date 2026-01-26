namespace GameVM.Compiler.Pascal
{
    public class VariantCaseNode : PascalAstNode
    {
        public required object Value { get; set; }
        public required List<FieldDeclarationNode> Fields { get; set; }
    }
}
