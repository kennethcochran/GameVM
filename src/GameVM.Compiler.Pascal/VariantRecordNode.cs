namespace GameVM.Compiler.Pascal
{
    public class VariantRecordNode : RecordTypeNode
    {
        public required string VariantFieldName { get; set; }
        public required TypeNode VariantFieldType { get; set; }
        public required List<VariantCaseNode> Variants { get; set; }
    }
}
