using Antlr4.Runtime.Tree;
using GameVM.Compiler.Pascal.ANTLR;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GameVM.Compiler.Pascal
{
    /// <summary>
    /// Visitor for declaration-related AST nodes in Pascal
    /// </summary>
    public class DeclarationVisitor : PascalBaseVisitor<PascalASTNode>
    {
        private readonly ExpressionVisitor _expressionVisitor;
        private readonly ASTBuilder _astBuilder;
        private ASTVisitor _mainVisitor;

        public DeclarationVisitor(ExpressionVisitor expressionVisitor, ASTBuilder astBuilder = null)
        {
            _expressionVisitor = expressionVisitor;
            _astBuilder = astBuilder ?? new ASTBuilder();
        }

        public void SetMainVisitor(ASTVisitor mainVisitor)
        {
            _mainVisitor = mainVisitor;
        }

        public override PascalASTNode VisitProcedureDeclaration(PascalParser.ProcedureDeclarationContext context)
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
                Parameters = new List<VariableNode>(),
                Block = block
            };

            var formalParams = context.formalParameterList()?.formalParameterSection();
            if (formalParams != null)
            {
                foreach (var param in formalParams)
                {
                    var paramGroup = param.parameterGroup();
                    var identList = paramGroup?.identifierList();
                    var identifiers = identList?.identifier();
                    if (identifiers != null)
                    {
                        foreach (var id in identifiers)
                        {
                            procNode.Parameters.Add(new VariableNode { Name = id.GetText() });
                        }
                    }
                }
            }

            return procNode;
        }

        public override PascalASTNode VisitVariableDeclaration(PascalParser.VariableDeclarationContext context)
        {
            var identifiers = context.identifierList().identifier();
            var typeCtx = context.type_();
            var typeNode = Visit(typeCtx) as TypeNode;

            if (identifiers == null || typeNode == null)
            {
                return new ErrorNode("Variable declaration is incomplete");
            }

            // If there are multiple identifiers, for now we return a block or just handles them in HLIR transformer
            // But VariableDeclarationNode needs a single name.
            // Let's create multiple nodes if needed, or return the last one for now?
            // Cleanest is to change VariableDeclarationNode to support multiple names or use another way.
            // For now, let's just return a BlockNode with multiple VariableDeclarationNodes.
            var nodes = new List<PascalASTNode>();
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

        public override PascalASTNode VisitFunctionDeclaration(PascalParser.FunctionDeclarationContext context)
        {
            var identifier = context.identifier();
            if (identifier == null)
            {
                return new ErrorNode("Function declaration is missing an identifier");
            }

            var block = (_mainVisitor?.VisitBlock(context.block()) ?? Visit(context.block())) as BlockNode;
            if (block == null)
            {
                return new ErrorNode("Function declaration is missing a block");
            }

            var parameters = new List<VariableNode>();
            var formalParams = context.formalParameterList()?.formalParameterSection();
            if (formalParams != null)
            {
                foreach (var param in formalParams)
                {
                    var paramGroup = param.parameterGroup();
                    var identList = paramGroup?.identifierList();
                    var identifiers = identList?.identifier();
                    if (identifiers != null)
                    {
                        foreach (var id in identifiers)
                        {
                            parameters.Add(new VariableNode { Name = id.GetText() });
                        }
                    }
                }
            }

            return new FunctionNode
            {
                Name = identifier.GetText(),
                Parameters = parameters,
                Block = block
            };
        }

        public override PascalASTNode VisitTypeDefinitionPart(PascalParser.TypeDefinitionPartContext context)
        {
            var nodes = new List<PascalASTNode>();
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

        public override PascalASTNode VisitTypeDefinition(PascalParser.TypeDefinitionContext context)
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

        public override PascalASTNode VisitType_(PascalParser.Type_Context context)
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

        public override PascalASTNode VisitSimpleType(PascalParser.SimpleTypeContext context)
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
                // Fix for missing required member 'TypeNode.TypeName'
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

        public override PascalASTNode VisitSubrangeType(PascalParser.SubrangeTypeContext context)
        {
            var constants = context.constant();
            if (constants.Length != 2)
            {
                return new ErrorNode("Subrange type must have exactly two bounds");
            }

            var lowNode = _expressionVisitor.Visit(constants[0]);
            var highNode = _expressionVisitor.Visit(constants[1]);

            // Allow both ConstantNode and LiteralNode
            var low = lowNode as ExpressionNode;
            // In strict mode we might want to check if it's constant, but parser grammar enforces constant() rule.
            // But we need to verify types are effectively constant/literal.
            // For now, accept ExpressionNode as RangeNode properties are ExpressionNode.

            // However, previous code cast to ConstantNode specifically.
            // If I just cast to ExpressionNode, it should work for RangeNode.

            if (low == null || highNode == null)
            {
                return new ErrorNode("Invalid subrange bounds");
            }

            return new RangeNode
            {
                Low = lowNode as ExpressionNode,
                High = highNode as ExpressionNode
            };
        }

        public override PascalASTNode VisitArrayType(PascalParser.ArrayTypeContext context)
        {
            var dimensions = new List<RangeNode>();
            foreach (var indexType in context.typeList().indexType())
            {
                var simpleType = Visit(indexType.simpleType());
                // Convert the simple type to a range if possible
                var range = simpleType as RangeNode;
                if (range == null && simpleType is TypeNode typeNode)
                {
                    // For predefined types like INTEGER, create a range with typical bounds
                    if (typeNode.TypeName.Equals("INTEGER", StringComparison.OrdinalIgnoreCase))
                    {
                        range = new RangeNode
                        {
                            Low = _astBuilder.CreateIntegerLiteral(int.MinValue.ToString()),
                            High = _astBuilder.CreateIntegerLiteral(int.MaxValue.ToString())
                        };
                    }
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

        public override PascalASTNode VisitSetType(PascalParser.SetTypeContext context)
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

        public override PascalASTNode VisitStructuredType(PascalParser.StructuredTypeContext context)
        {
            if (context.PACKED() != null)
            {
                // Handle packed type
                var inner = Visit(context.unpackedStructuredType()) as TypeNode;
                if (inner == null)
                    return new ErrorNode("Packed type missing inner type");
                return new PackedTypeNode
                {
                    TypeName = "packed",
                    InnerType = inner
                };
            }
            // For non-packed, return the unpacked structured type directly
            return Visit(context.unpackedStructuredType());
        }

        public override PascalASTNode VisitUnpackedStructuredType(PascalParser.UnpackedStructuredTypeContext context)
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

        public override PascalASTNode VisitPointerType(PascalParser.PointerTypeContext context)
        {
            // pointerType: POINTER typeIdentifier;
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

        public override PascalASTNode VisitFileType(PascalParser.FileTypeContext context)
        {
            // Always set TypeName in the object initializer
            return new SimpleTypeNode
            {
                TypeName = "file"
            };
        }

        public override PascalASTNode VisitScalarType(PascalParser.ScalarTypeContext context)
        {
            // scalarType: LPAREN identifierList RPAREN;
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

        public override PascalASTNode VisitLabelDeclarationPart(PascalParser.LabelDeclarationPartContext context)
        {
            // labelDeclarationPart: LABEL label (COMMA label)* SEMI;
            var labels = new List<LabelNode>();
            foreach (var labelCtx in context.label())
            {
                if (int.TryParse(labelCtx.GetText(), out int labelValue))
                {
                    labels.Add(new LabelNode { Label = labelValue });
                }
            }
            // Return as a block node for now
            return new BlockNode { Statements = labels.Cast<PascalASTNode>().ToList() };
        }

        public override PascalASTNode VisitProcedureOrFunctionDeclaration(PascalParser.ProcedureOrFunctionDeclarationContext context)
        {
            if (context.procedureDeclaration() != null)
            {
                return VisitProcedureDeclaration(context.procedureDeclaration());
            }
            else if (context.functionDeclaration() != null)
            {
                return VisitFunctionDeclaration(context.functionDeclaration());
            }

            return null;
        }

        public override PascalASTNode VisitProcedureAndFunctionDeclarationPart(PascalParser.ProcedureAndFunctionDeclarationPartContext context)
        {
            var procOrFunc = context.procedureOrFunctionDeclaration();
            if (procOrFunc != null)
            {
                return Visit(procOrFunc);
            }
            return null;
        }

        public override PascalASTNode VisitIndexType(PascalParser.IndexTypeContext context)
        {
            return Visit(context.simpleType()) ?? new ErrorNode("Invalid index type in array");
        }

        // Helper for set type
        private PascalASTNode VisitSetTypeWithDefaultName(PascalParser.SetTypeContext setTypeContext)
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
            // If not a SetTypeNode, propagate the error or return as is
            return setTypeNode ?? new ErrorNode("Invalid set type");
        }

        // Helper for file type
        private PascalASTNode VisitFileTypeWithDefaultName(PascalParser.FileTypeContext fileTypeContext)
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

        // Variant records (Pascal: record ... case ... of ...)
        private TypeNode BuildRecordTypeNode(PascalParser.RecordTypeContext recordType)
        {
            Console.WriteLine($"[DEBUG] BuildRecordTypeNode called for recordType: {recordType.GetText()}");
            var record = new RecordTypeNode
            {
                TypeName = "record",
                Fields = new List<FieldDeclarationNode>()
            };
            var fieldList = recordType.fieldList();
            if (fieldList?.fixedPart()?.recordSection() != null)
            {
                foreach (var section in fieldList.fixedPart().recordSection())
                {
                    var identifiers = section.identifierList().identifier();
                    var typeNode = Visit(section.type_()) as TypeNode;
                    if (typeNode != null)
                    {
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
            }
            // Handle variant part
            if (fieldList?.variantPart() != null)
            {
                var variantPart = fieldList.variantPart();
                var tag = variantPart.tag();
                string tagName = tag?.identifier()?.GetText() ?? "";
                TypeNode tagType = tag?.typeIdentifier() != null
                    ? new TypeIdentifierNode
                    {
                        Name = tag.typeIdentifier().GetText(),
                        TypeName = tag.typeIdentifier().GetText()
                    }
                    : new TypeIdentifierNode { Name = "", TypeName = "" };
                var variants = new List<VariantCaseNode>();
                foreach (var variant in variantPart.variant())
                {
                    var constList = variant.constList();
                    var value = constList.constant(0).GetText();
                    var fields = new List<FieldDeclarationNode>();
                    var fieldListCtx = variant.fieldList();
                    if (fieldListCtx?.fixedPart()?.recordSection() != null)
                    {
                        foreach (var section in fieldListCtx.fixedPart().recordSection())
                        {
                            var ids = section.identifierList().identifier();
                            var tNode = Visit(section.type_()) as TypeNode;
                            if (tNode != null)
                            {
                                foreach (var id in ids)
                                {
                                    fields.Add(new FieldDeclarationNode
                                    {
                                        Name = id.GetText(),
                                        Type = tNode
                                    });
                                }
                            }
                        }
                    }
                    variants.Add(new VariantCaseNode
                    {
                        Value = value,
                        Fields = fields
                    });
                }
                Console.WriteLine($"[DEBUG] Returning VariantRecordNode for recordType: {recordType.GetText()}");
                return new VariantRecordNode
                {
                    TypeName = "variantrecord",
                    Fields = record.Fields,
                    VariantFieldName = tagName,
                    VariantFieldType = tagType,
                    Variants = variants
                };
            }
            Console.WriteLine($"[DEBUG] Returning RecordTypeNode for recordType: {recordType.GetText()}");
            return record;
        }

        protected override PascalASTNode DefaultResult => null!;
    }
}
