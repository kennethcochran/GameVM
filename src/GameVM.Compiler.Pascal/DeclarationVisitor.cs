using Antlr4.Runtime.Tree;
using GameVM.Compiler.Pascal.ANTLR;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GameVM.Compiler.Pascal
{
    public class DeclarationVisitor : PascalBaseVisitor<PascalAstNode>
    {
        private readonly ExpressionVisitor _expressionVisitor;
        private readonly AstBuilder _astBuilder;
        private AstVisitor? _mainVisitor;

        public DeclarationVisitor(ExpressionVisitor expressionVisitor, AstBuilder? astBuilder = null)
        {
            _expressionVisitor = expressionVisitor;
            _astBuilder = astBuilder ?? new AstBuilder();
        }

        public void SetMainVisitor(AstVisitor mainVisitor)
        {
            _mainVisitor = mainVisitor;
        }

        public override PascalAstNode VisitProcedureDeclaration(PascalParser.ProcedureDeclarationContext context)
        {
            var block = _mainVisitor?.VisitBlock(context.block()) ?? Visit(context.block());
            var identifier = context.identifier();

            if (block == null || identifier == null)
            {
                return new ErrorNode("Procedure declaration is incomplete");
            }

            var procNode = new ProcedureNode
            {
                Name = identifier.GetText(),
                Parameters = ProcessParameters(context),
                Block = block
            };

            return procNode;
        }

        private List<VariableNode> ProcessParameters(PascalParser.ProcedureDeclarationContext context)
        {
            var parameters = new List<VariableNode>();
            var formalParams = context.formalParameterList()?.formalParameterSection();
            
            if (formalParams == null)
                return parameters;

            foreach (var paramSection in formalParams)
            {
                var paramGroup = paramSection.parameterGroup();
                if (paramGroup == null)
                    continue;

                var identifiers = paramGroup.identifierList()?.identifier();
                if (identifiers == null)
                    continue;

                foreach (var id in identifiers)
                {
                    parameters.Add(new VariableNode { Name = id.GetText() });
                }
            }

            return parameters;
        }

        public override PascalAstNode VisitVariableDeclaration(PascalParser.VariableDeclarationContext context)
        {
            if (context == null)
                return new ErrorNode("Null variable declaration context");

            var identifierList = context.identifierList();
            if (identifierList == null)
                return new ErrorNode("Missing variable identifiers");

            var identifiers = identifierList.identifier();
            var typeCtx = context.type_();
            
            if (identifiers == null || identifiers.Length == 0)
                return new ErrorNode("Missing variable identifiers");

            if (typeCtx == null)
                return new ErrorNode("Missing type declaration");

            var typeNode = Visit(typeCtx) as TypeNode;

            if (typeNode == null)
            {
                return new ErrorNode("Variable declaration is incomplete");
            }

            var nodes = new List<PascalAstNode>();
            foreach (var identifier in identifiers)
            {
                nodes.Add(new VariableDeclarationNode
                {
                    Name = identifier.GetText(),
                    Type = typeNode
                });
            }

            if (nodes.Count == 1) return nodes[0];
            return new BlockNode { Statements = nodes };
        }

        public override PascalAstNode VisitFunctionDeclaration(PascalParser.FunctionDeclarationContext context)
        {
            if (context == null)
                return new ErrorNode("Null function declaration context");

            var identifier = context.identifier();
            if (identifier == null)
            {
                return new ErrorNode("Function declaration is missing an identifier");
            }

            var blockResult = ValidateFunctionBlock(context);
            if (blockResult is ErrorNode errorNode)
                return errorNode;

            var block = (BlockNode)blockResult;
            var parameters = ExtractFunctionParameters(context);

            return new FunctionNode
            {
                Name = identifier.GetText(),
                Parameters = parameters,
                Block = block
            };
        }

        private PascalAstNode ValidateFunctionBlock(PascalParser.FunctionDeclarationContext context)
        {
            var blockContext = context.block();
            if (blockContext == null)
                return new ErrorNode("Function declaration is missing a block");

            var block = (_mainVisitor?.VisitBlock(blockContext) ?? Visit(blockContext)) as BlockNode;
            if (block == null)
            {
                return new ErrorNode("Function declaration block visit returned null");
            }
            return block;
        }

        private static List<VariableNode> ExtractFunctionParameters(PascalParser.FunctionDeclarationContext context)
        {
            var parameters = new List<VariableNode>();
            var formalParams = context.formalParameterList()?.formalParameterSection();
            if (formalParams == null) return parameters;

            foreach (var paramSection in formalParams)
            {
                var paramGroup = paramSection.parameterGroup();
                if (paramGroup == null) continue;

                var identifiers = paramGroup.identifierList()?.identifier();
                if (identifiers == null) continue;

                foreach (var id in identifiers)
                {
                    parameters.Add(new VariableNode { Name = id.GetText() });
                }
            }

            return parameters;
        }

        public override PascalAstNode VisitTypeDefinitionPart(PascalParser.TypeDefinitionPartContext context)
        {
            var nodes = new List<PascalAstNode>();
            foreach (var typeDef in context.typeDefinition())
            {
                var node = Visit(typeDef);
                if (node != null)
                {
                    nodes.Add(node);
                }
            }
            if (nodes.Count == 1)
                return nodes[0];
            return new BlockNode { Statements = nodes };
        }

        public override PascalAstNode VisitTypeDefinition(PascalParser.TypeDefinitionContext context)
        {
            var name = context.identifier()?.GetText();
            if (string.IsNullOrEmpty(name))
            {
                Console.WriteLine("[ERROR] Type definition missing identifier");
                return new ErrorNode("Type definition missing identifier");
            }

            var type = Visit(context.type_());
            if (type == null)
            {
                Console.WriteLine($"[ERROR] Invalid type in type definition for {name}");
                return new ErrorNode("Invalid type in type definition");
            }

            if (type is not TypeNode typeNode)
            {
                Console.WriteLine($"[ERROR] Expected TypeNode but got {type.GetType().Name} in type definition for {name}");
                return new ErrorNode($"Expected TypeNode but got {type.GetType().Name}");
            }

            typeNode.TypeName = name;
            Console.WriteLine($"[DEBUG] Creating TypeDefinitionNode for {name} with type {typeNode.GetType().Name}");
            return new TypeDefinitionNode
            {
                Name = name,
                Type = typeNode
            };
        }

        public override PascalAstNode VisitConstantDefinitionPart(PascalParser.ConstantDefinitionPartContext context)
        {
            var nodes = new List<PascalAstNode>();
            foreach (var constDef in context.constantDefinition())
            {
                var node = Visit(constDef);
                if (node != null)
                {
                    nodes.Add(node);
                }
            }
            return new BlockNode { Statements = nodes };
        }

        public override PascalAstNode VisitConstantDefinition(PascalParser.ConstantDefinitionContext context)
        {
            var name = context.identifier()?.GetText();
            var valueNode = _expressionVisitor.Visit(context.constant());

            if (string.IsNullOrEmpty(name) || valueNode == null)
            {
                return new ErrorNode("Constant definition is incomplete");
            }

            return new ConstantDeclarationNode
            {
                Name = name,
                Value = (ExpressionNode)valueNode
            };
        }

        public override PascalAstNode VisitType_(PascalParser.Type_Context context)
        {
            if (context.simpleType() != null)
            {
                return Visit(context.simpleType());
            }
            if (context.structuredType() != null)
            {
                return Visit(context.structuredType());
            }
            if (context.pointerType() != null)
            {
                return Visit(context.pointerType());
            }
            return new ErrorNode("Unknown type");
        }

        public override PascalAstNode VisitSimpleType(PascalParser.SimpleTypeContext context)
        {
            if (context.scalarType() != null)
            {
                return Visit(context.scalarType());
            }
            if (context.subrangeType() != null)
            {
                return Visit(context.subrangeType());
            }
            if (context.typeIdentifier() != null)
            {
                return new TypeIdentifierNode
                {
                    Name = context.typeIdentifier().GetText(),
                    TypeName = context.typeIdentifier().GetText() // Setting TypeName to match Name
                };
            }
            if (context.stringtype() != null)
            {
                return Visit(context.stringtype());
            }
            return new ErrorNode("Unknown simple type");
        }

        public override PascalAstNode VisitSubrangeType(PascalParser.SubrangeTypeContext context)
        {
            var constants = context.constant();
            if (constants.Length != 2)
            {
                return new ErrorNode("Subrange type must have exactly two bounds");
            }

            var lowNode = _expressionVisitor.Visit(constants[0]);
            var highNode = _expressionVisitor.Visit(constants[1]);

            var low = lowNode as ExpressionNode;


            if (low == null || highNode == null)
            {
                return new ErrorNode("Invalid subrange bounds");
            }

            return new RangeNode
            {
                Low = lowNode as ExpressionNode ?? throw new InvalidOperationException("Low bound must be an expression"),
                High = highNode as ExpressionNode ?? throw new InvalidOperationException("High bound must be an expression")
            };
        }

        public override PascalAstNode VisitArrayType(PascalParser.ArrayTypeContext context)
        {
            var dimensions = new List<RangeNode>();
            foreach (var indexType in context.typeList().indexType())
            {
                var simpleType = Visit(indexType.simpleType());
                var range = simpleType as RangeNode;
                if (range == null && simpleType is TypeNode typeNode && typeNode.TypeName.Equals("INTEGER", StringComparison.OrdinalIgnoreCase))
                {
                    range = new RangeNode
                    {
                        Low = _astBuilder.CreateIntegerLiteral(int.MinValue.ToString()),
                        High = _astBuilder.CreateIntegerLiteral(int.MaxValue.ToString())
                    };
                }
                if (range != null)
                {
                    dimensions.Add(range);
                }
            }

            var elementType = Visit(context.componentType()) as TypeNode;
            if (dimensions.Count == 0 || elementType == null)
            {
                return new ErrorNode("Incomplete array type");
            }

            return new ArrayTypeNode
            {
                Dimensions = dimensions,
                ElementType = elementType,
                TypeName = "Array"
            };
        }

        public override PascalAstNode VisitSetType(PascalParser.SetTypeContext context)
        {
            var baseType = Visit(context.baseType()) as TypeNode;
            if (baseType == null)
            {
                return new ErrorNode("Incomplete set type");
            }

            return new SetTypeNode
            {
                BaseType = baseType,
                TypeName = "set"
            };
        }

        public override PascalAstNode VisitStructuredType(PascalParser.StructuredTypeContext context)
        {
            if (context.PACKED() != null)
            {
                var inner = Visit(context.unpackedStructuredType()) as TypeNode;
                if (inner == null)
                    return new ErrorNode("Packed type missing inner type");
                return new PackedTypeNode
                {
                    TypeName = "packed",
                    InnerType = inner
                };
            }
            return Visit(context.unpackedStructuredType());
        }

        public override PascalAstNode VisitUnpackedStructuredType(PascalParser.UnpackedStructuredTypeContext context)
        {
            if (context.arrayType() != null)
            {
                return Visit(context.arrayType());
            }
            else if (context.recordType() != null)
            {
                return BuildRecordTypeNode(context.recordType());
            }
            else if (context.setType() != null)
            {
                return VisitSetTypeWithDefaultName(context.setType());
            }
            else if (context.fileType() != null)
            {
                return VisitFileTypeWithDefaultName(context.fileType());
            }

            return new ErrorNode("Unknown structured type");
        }

        public override PascalAstNode VisitPointerType(PascalParser.PointerTypeContext context)
        {
            var typeId = context.typeIdentifier();
            if (typeId == null)
                return new ErrorNode("Pointer type missing type identifier");
            var targetType = new TypeIdentifierNode
            {
                Name = typeId.GetText(),
                TypeName = typeId.GetText()
            };
            return new PointerTypeNode
            {
                TypeName = "pointer",
                TargetType = targetType
            };
        }

        public override PascalAstNode VisitFileType(PascalParser.FileTypeContext context)
        {
            return new SimpleTypeNode
            {
                TypeName = "file"
            };
        }

        public override PascalAstNode VisitScalarType(PascalParser.ScalarTypeContext context)
        {
            var idList = context.identifierList();
            if (idList == null)
                return new ErrorNode("Enumerated type missing identifier list");
            var members = idList.identifier().Select(id => id.GetText()).ToList();
            return new EnumeratedTypeNode
            {
                TypeName = "enum",
                Members = members
            };
        }

        public override PascalAstNode VisitLabelDeclarationPart(PascalParser.LabelDeclarationPartContext context)
        {
            var labels = new List<LabelNode>();
            foreach (var labelCtx in context.label())
            {
                if (int.TryParse(labelCtx.GetText(), out int labelValue))
                {
                    labels.Add(new LabelNode { Label = labelValue });
                }
            }
            return new BlockNode { Statements = labels.Cast<PascalAstNode>().ToList() };
        }

        public override PascalAstNode VisitProcedureOrFunctionDeclaration(PascalParser.ProcedureOrFunctionDeclarationContext context)
        {
            if (context.procedureDeclaration() != null)
            {
                return VisitProcedureDeclaration(context.procedureDeclaration());
            }
            else if (context.functionDeclaration() != null)
            {
                return VisitFunctionDeclaration(context.functionDeclaration());
            }

            return new ErrorNode("Invalid procedure or function declaration");
        }

        public override PascalAstNode VisitProcedureAndFunctionDeclarationPart(PascalParser.ProcedureAndFunctionDeclarationPartContext context)
        {
            var procOrFunc = context.procedureOrFunctionDeclaration();
            if (procOrFunc != null)
            {
                return Visit(procOrFunc);
            }
            return new ErrorNode("Invalid procedure and function declaration part");
        }

        public override PascalAstNode VisitIndexType(PascalParser.IndexTypeContext context)
        {
            return Visit(context.simpleType()) ?? new ErrorNode("Invalid index type in array");
        }

        private PascalAstNode VisitSetTypeWithDefaultName(PascalParser.SetTypeContext setTypeContext)
        {
            var setTypeNode = Visit(setTypeContext);
            if (setTypeNode is SetTypeNode setType)
            {
                if (string.IsNullOrEmpty(setType.TypeName))
                {
                    setType.TypeName = "set";
                }
                return setType;
            }
            return setTypeNode ?? new ErrorNode("Invalid set type");
        }

        private PascalAstNode VisitFileTypeWithDefaultName(PascalParser.FileTypeContext fileTypeContext)
        {
            var fileType = Visit(fileTypeContext) as TypeNode;
            if (fileType == null)
            {
                fileType = new SimpleTypeNode { TypeName = "file" };
            }
            else if (string.IsNullOrEmpty(fileType.TypeName))
            {
                fileType.TypeName = "file";
            }
            return fileType;
        }

        private TypeNode BuildRecordTypeNode(PascalParser.RecordTypeContext recordType)
        {
            Console.WriteLine($"[DEBUG] BuildRecordTypeNode called for recordType: {recordType.GetText()}");
            
            var record = new RecordTypeNode
            {
                TypeName = "record",
                Fields = new List<FieldDeclarationNode>()
            };

            ProcessFixedFields(recordType, record);
            
            var variantPart = ExtractVariantPart(recordType);
            if (variantPart != null)
            {
                Console.WriteLine($"[DEBUG] Returning VariantRecordNode for recordType: {recordType.GetText()}");
                return variantPart;
            }

            Console.WriteLine($"[DEBUG] Returning RecordTypeNode for recordType: {recordType.GetText()}");
            return record;
        }

        private static void ProcessFixedFields(PascalParser.RecordTypeContext recordType, RecordTypeNode record)
        {
            var fieldList = recordType.fieldList();
            var fixedPart = fieldList?.fixedPart()?.recordSection();
            if (fixedPart == null) return;

            foreach (var section in fixedPart)
            {
                var identifiers = section.identifierList().identifier();
                var typeNode = new TypeIdentifierNode { Name = section.type_().GetText(), TypeName = section.type_().GetText() };
                
                if (string.IsNullOrEmpty(typeNode.TypeName))
                {
                    typeNode.TypeName = typeNode.GetType().Name.Replace("Node", "").ToLowerInvariant();
                }

                foreach (var identifier in identifiers)
                {
                    record.Fields.Add(new FieldDeclarationNode
                    {
                        Name = identifier.GetText(),
                        Type = typeNode
                    });
                }
            }
        }

        private VariantRecordNode? ExtractVariantPart(PascalParser.RecordTypeContext recordType)
        {
            var fieldList = recordType.fieldList();
            var variantPartContext = fieldList?.variantPart();
            if (variantPartContext == null) return null;

            var tag = variantPartContext.tag();
            var tagName = tag?.identifier()?.GetText() ?? "";
            var tagType = tag != null ? CreateTagType(tag) : new TypeIdentifierNode { Name = "", TypeName = "" };

            var variants = ProcessVariants(variantPartContext);
            var recordFields = new RecordTypeNode { Fields = new List<FieldDeclarationNode>(), TypeName = "record" };
            ProcessFixedFields(recordType, recordFields);

            return new VariantRecordNode
            {
                TypeName = "variantrecord",
                Fields = recordFields.Fields,
                VariantFieldName = tagName,
                VariantFieldType = tagType,
                Variants = variants
            };
        }

        private static TypeIdentifierNode CreateTagType(PascalParser.TagContext tag)
        {
            return tag?.typeIdentifier() != null
                ? new TypeIdentifierNode
                {
                    Name = tag.typeIdentifier().GetText(),
                    TypeName = tag.typeIdentifier().GetText()
                }
                : new TypeIdentifierNode { Name = "", TypeName = "" };
        }

        private static List<VariantCaseNode> ProcessVariants(PascalParser.VariantPartContext variantPartContext)
        {
            var variants = new List<VariantCaseNode>();
            
            foreach (var variant in variantPartContext.variant())
            {
                var constList = variant.constList();
                var value = constList.constant(0).GetText();
                var fields = ExtractVariantFields(variant);
                
                variants.Add(new VariantCaseNode
                {
                    Value = value,
                    Fields = fields
                });
            }

            return variants;
        }

        private static List<FieldDeclarationNode> ExtractVariantFields(PascalParser.VariantContext variant)
        {
            var fields = new List<FieldDeclarationNode>();
            var fieldListCtx = variant.fieldList();
            var fixedPart = fieldListCtx?.fixedPart()?.recordSection();
            
            if (fixedPart == null) return fields;

            foreach (var section in fixedPart)
            {
                var ids = section.identifierList().identifier();
                var tNode = new TypeIdentifierNode { Name = section.type_().GetText(), TypeName = section.type_().GetText() };
                
                foreach (var id in ids)
                {
                    fields.Add(new FieldDeclarationNode
                    {
                        Name = id.GetText(),
                        Type = tNode
                    });
                }
            }

            return fields;
        }

        protected override PascalAstNode DefaultResult => null!;
    }
}
