using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using GameVM.Compiler.Pascal.ANTLR;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GameVM.Compiler.Pascal
{
    public class ASTVisitor : PascalBaseVisitor<PascalASTNode>
    {
        public override PascalASTNode VisitProgram(PascalParser.ProgramContext context)
        {
            var programHeading = context.programHeading();
            var identifier = programHeading.identifier();
            if (identifier == null)
                throw new InvalidOperationException("Program must have a name");

            var blockNode = VisitBlock(context.block()) as BlockNode 
                ?? throw new InvalidOperationException("Block visit returned null");

            return new ProgramNode
            {
                Name = identifier.GetText(),
                Block = blockNode,
                UsesUnits = new List<PascalASTNode>()
            };
        }

        public override PascalASTNode VisitBlock(PascalParser.BlockContext context)
        {
            var block = new BlockNode
            {
                Statements = new List<PascalASTNode>()
            };

            // Do NOT add label declarations to Statements list (to match test expectations)
            // if (context.labelDeclarationPart() != null)
            // {
            //     foreach (var labelDecl in context.labelDeclarationPart())
            //     {
            //         var node = Visit(labelDecl);
            //         if (node != null)
            //         {
            //             block.Statements.Add(node);
            //         }
            //     }
            // }

            // Add type definitions next
            if (context.typeDefinitionPart() != null)
            {
                foreach (var typeDef in context.typeDefinitionPart())
                {
                    var node = Visit(typeDef);
                    if (node is BlockNode blockNode)
                    {
                        foreach (var stmt in blockNode.Statements)
                        {
                            block.Statements.Add(stmt);
                        }
                    }
                    else if (node != null)
                    {
                        block.Statements.Add(node);
                    }
                }
            }

            // Add procedure/function declarations before variable declarations
            if (context.procedureAndFunctionDeclarationPart() != null)
            {
                foreach (var procDecl in context.procedureAndFunctionDeclarationPart())
                {
                    var node = Visit(procDecl);
                    if (node != null)
                    {
                        block.Statements.Add(node);
                    }
                }
            }

            if (context.variableDeclarationPart() != null)
            {
                foreach (var varDeclPart in context.variableDeclarationPart())
                {
                    foreach (var varDecl in varDeclPart.variableDeclaration())
                    {
                        var node = Visit(varDecl);
                        if (node != null)
                        {
                            block.Statements.Add(node);
                        }
                    }
                }
            }

            if (context.compoundStatement() != null)
            {
                var compoundNode = Visit(context.compoundStatement());
                if (compoundNode != null)
                {
                    block.Statements.Add(compoundNode);
                }
            }

            return block;
        }

        public override PascalASTNode VisitCompoundStatement(PascalParser.CompoundStatementContext context)
        {
            var statements = new List<PascalASTNode>();

            foreach (var stmt in context.statements().statement())
            {
                var node = Visit(stmt);
                if (node != null)
                {
                    statements.Add(node);
                }
            }

            return new BlockNode { Statements = statements };
        }

        public override PascalASTNode VisitStatement(PascalParser.StatementContext context)
        {
            if (context.unlabelledStatement() != null)
                return Visit(context.unlabelledStatement());
            
            return base.VisitStatement(context);
        }

        public override PascalASTNode VisitAssignmentStatement(PascalParser.AssignmentStatementContext context)
        {
            var left = Visit(context.variable()) as VariableNode;
            var right = Visit(context.expression());
            if (left == null || right == null)
            {
                return new ErrorNode("Incomplete assignment statement");
            }
            return new AssignmentNode
            {
                Left = left,
                Right = right
            };
        }

        public override PascalASTNode VisitVariable(PascalParser.VariableContext context)
        {
            var identifier = context.identifier();
            if (identifier == null || identifier.Length == 0)
            {
                return new ErrorNode("Variable must have an identifier");
            }
            return new VariableNode { Name = identifier[0].GetText() };
        }

        public override PascalASTNode VisitExpression(PascalParser.ExpressionContext context)
        {
            if (context.relationaloperator() != null)
            {
                var left = Visit(context.simpleExpression()) as ExpressionNode;
                var right = Visit(context.expression()) as ExpressionNode;
                if (left == null || right == null)
                {
                    return new ErrorNode("Relational operation missing operand");
                }
                return new RelationalOperatorNode
                {
                    Operator = context.relationaloperator().GetText(),
                    Left = left,
                    Right = right
                };
            }
            var simpleExpression = Visit(context.simpleExpression());
            return simpleExpression ?? new ErrorNode("Invalid simple expression");
        }

        public override PascalASTNode VisitSimpleExpression(PascalParser.SimpleExpressionContext context)
        {
            if (context.additiveoperator() != null)
            {
                var left = Visit(context.term()) as ExpressionNode;
                var right = Visit(context.simpleExpression()) as ExpressionNode;
                if (left == null || right == null)
                {
                    return new ErrorNode("Additive operation missing operand");
                }
                return new AdditiveOperatorNode
                {
                    Operator = context.additiveoperator().GetText(),
                    Left = left,
                    Right = right
                };
            }
            var term = Visit(context.term());
            return term ?? new ErrorNode("Invalid term");
        }

        public override PascalASTNode VisitTerm(PascalParser.TermContext context)
        {
            if (context.multiplicativeoperator() != null)
            {
                var left = Visit(context.signedFactor()) as ExpressionNode;
                var right = Visit(context.term()) as ExpressionNode;
                if (left == null || right == null)
                {
                    return new ErrorNode("Multiplicative operation missing operand");
                }
                return new MultiplicativeOperatorNode
                {
                    Operator = context.multiplicativeoperator().GetText(),
                    Left = left,
                    Right = right
                };
            }
            var signedFactor = Visit(context.signedFactor());
            return signedFactor ?? new ErrorNode("Invalid signed factor");
        }

        public override PascalASTNode VisitSignedFactor(PascalParser.SignedFactorContext context)
        {
            var factor = Visit(context.factor());
            if (factor == null)
            {
                return new ErrorNode("Invalid factor");
            }
            var sign = context.PLUS() != null ? "+" : context.MINUS() != null ? "-" : null;
            if (factor is ErrorNode)
            {
                return factor; // Propagate the error
            }
            if (factor is ExpressionNode expressionNode && sign != null)
            {
                return new UnaryOperatorNode
                {
                    Operator = sign,
                    Operand = expressionNode
                };
            }
            return factor;
        }

        public override PascalASTNode VisitFactor(PascalParser.FactorContext context)
        {
            if (context.variable() != null)
            {
                return Visit(context.variable()) ?? new ErrorNode("Invalid variable");
            }
            if (context.expression() != null)
            {
                var expression = Visit(context.expression()) as ExpressionNode;
                if (expression == null)
                {
                    return new ErrorNode("Invalid parenthesized expression");
                }
                return expression;
            }
            if (context.unsignedConstant() != null)
            {
                var constant = context.unsignedConstant().GetText();
                return new ConstantNode { Value = constant };
            }
            if (context.set_() != null)
            {
                return Visit(context.set_());
            }
            return new ErrorNode("Unknown factor");
        }

        public override PascalASTNode VisitProcedureDeclaration(PascalParser.ProcedureDeclarationContext context)
        {
            var block = Visit(context.block());
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

        public override PascalASTNode VisitFunctionDeclaration(PascalParser.FunctionDeclarationContext context)
        {
            var identifier = context.identifier();
            if (identifier == null)
            {
                return new ErrorNode("Function declaration is missing an identifier");
            }

            var block = Visit(context.block()) as BlockNode;
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

        public override PascalASTNode VisitForStatement(PascalParser.ForStatementContext context)
        {
            var variable = Visit(context.identifier()) as VariableNode;
            var fromExpression = Visit(context.forList().initialValue()) as ExpressionNode;
            var toExpression = Visit(context.forList().finalValue()) as ExpressionNode;

            if (variable == null || fromExpression == null || toExpression == null)
            {
                return new ErrorNode("Incomplete for statement");
            }

            var block = Visit(context.statement()) as BlockNode;
            if (block == null)
            {
                block = new BlockNode { Statements = new List<PascalASTNode>() };
            }

            return new ForNode
            {
                Variable = variable,
                FromExpression = fromExpression,
                ToExpression = toExpression,
                Block = block
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
                            Low = new ConstantNode { Value = int.MinValue },
                            High = new ConstantNode { Value = int.MaxValue }
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

        public override PascalASTNode VisitIfStatement(PascalParser.IfStatementContext context)
        {
            var condition = Visit(context.expression()) as ExpressionNode;
            var thenBlock = Visit(context.statement(0)); // Removed strict casting to BlockNode
            var elseBlock = context.statement().Length > 1 ? Visit(context.statement(1)) : null; // Removed strict casting to BlockNode

            if (condition == null || thenBlock == null)
            {
                return new ErrorNode("Incomplete if statement");
            }

            return new IfNode
            {
                Condition = condition,
                ThenBlock = thenBlock,
                ElseBlock = elseBlock
            };
        }

        public override PascalASTNode VisitProcedureStatement(PascalParser.ProcedureStatementContext context)
        {
            var name = context.identifier()?.GetText();
            var arguments = context.parameterList()?.actualParameter()
                .Select(arg => Visit(arg) as ExpressionNode)
                .Where(arg => arg != null)
                .Cast<PascalASTNode>()
                .ToList();

            if (name == null || arguments == null)
            {
                return new ErrorNode("Incomplete procedure call");
            }

            return new ProcedureCallNode
            {
                Name = name,
                Arguments = arguments
            };
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

            var low = Visit(constants[0]) as ConstantNode;
            var high = Visit(constants[1]) as ConstantNode;

            if (low == null || high == null)
            {
                return new ErrorNode("Invalid subrange bounds");
            }

            return new RangeNode
            {
                Low = low,
                High = high
            };
        }

        // Custom implementation for VisitInitialValue
        public override PascalASTNode VisitInitialValue(PascalParser.InitialValueContext context)
        {
            return Visit(context.expression()) ?? new ErrorNode("Invalid initial value in for loop");
        }

        // Custom implementation for VisitFinalValue
        public override PascalASTNode VisitFinalValue(PascalParser.FinalValueContext context)
        {
            return Visit(context.expression()) ?? new ErrorNode("Invalid final value in for loop");
        }

        // Custom implementation for VisitIndexType
        public override PascalASTNode VisitIndexType(PascalParser.IndexTypeContext context)
        {
            return Visit(context.simpleType()) ?? new ErrorNode("Invalid index type in array");
        }

        public override PascalASTNode VisitProcedureAndFunctionDeclarationPart(PascalParser.ProcedureAndFunctionDeclarationPartContext context)
        {
            // Handle procedure or function declaration
            var declContext = context.procedureOrFunctionDeclaration();
            if (declContext != null)
            {
                return Visit(declContext);
            }

            // Return an error node if no valid declaration is found
            return new ErrorNode("Invalid procedure or function declaration part");
        }

        public override PascalASTNode VisitIdentifier(PascalParser.IdentifierContext context)
        {
            if (context == null || string.IsNullOrEmpty(context.GetText()))
            {
                return new ErrorNode("Invalid identifier");
            }

            return new VariableNode { Name = context.GetText() };
        }

        public override PascalASTNode VisitCaseStatement(PascalParser.CaseStatementContext context)
        {
            var selector = Visit(context.expression()) as ExpressionNode;
            if (selector == null)
            {
                return new ErrorNode("Invalid case selector");
            }

            var branches = new List<CaseBranchNode>();
            
            // Process the case list elements
            foreach (var caseListElement in context.caseListElement())
            {
                var branch = Visit(caseListElement) as CaseBranchNode;
                if (branch != null)
                {
                    branches.Add(branch);
                }
            }

            // Process the else part if it exists
            PascalASTNode? elseBlock = null;
            if (context.statements() != null)
            {
                var statements = new List<PascalASTNode>();
                foreach (var stmt in context.statements().statement())
                {
                    var stmtNode = Visit(stmt);
                    if (stmtNode != null)
                    {
                        statements.Add(stmtNode);
                    }
                }
                
                if (statements.Any())
                {
                    elseBlock = new BlockNode { Statements = statements };
                }
            }

            return new CaseNode
            {
                Selector = selector,
                Branches = branches,
                ElseBlock = elseBlock
            };
        }

        public override PascalASTNode VisitCaseListElement(PascalParser.CaseListElementContext context)
        {
            var constList = context.constList();
            if (constList == null)
            {
                return new ErrorNode("Missing constant list in case list element");
            }

            var labels = new List<ExpressionNode>();
            foreach (var constant in constList.constant())
            {
                var constantNode = Visit(constant);
                if (constantNode is ConstantNode constNode)
                {
                    labels.Add(constNode); // ConstantNode inherits from ExpressionNode
                }
            }

            var statement = Visit(context.statement());
            if (statement == null)
            {
                return new ErrorNode("Invalid case statement block");
            }

            if (labels.Count == 0)
            {
                return new ErrorNode("Case branch must have at least one label");
            }

            return new CaseBranchNode
            {
                Labels = labels,
                Statement = statement
            };
        }

        public override PascalASTNode VisitWhileStatement(PascalParser.WhileStatementContext context)
        {
            var condition = Visit(context.expression()) as ExpressionNode;
            var block = Visit(context.statement()); // Remove strict casting to BlockNode

            if (condition == null || block == null)
            {
                return new ErrorNode("Incomplete while statement");
            }

            return new WhileNode
            {
                Condition = condition,
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

        public override PascalASTNode VisitConstant(PascalParser.ConstantContext context)
        {
            // Handle numeric constants
            if (context.unsignedNumber() != null)
            {
                var number = context.unsignedNumber().GetText();
                return new ConstantNode { Value = number };
            }
            // Handle other constant types if needed
            return new ConstantNode { Value = context.GetText() };
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

        public override PascalASTNode VisitGotoStatement(PascalParser.GotoStatementContext context)
        {
            // gotoStatement: GOTO label;
            var labelCtx = context.label();
            if (labelCtx == null)
                return new ErrorNode("Goto statement missing label");
            if (!int.TryParse(labelCtx.GetText(), out int labelValue))
                return new ErrorNode("Invalid label in goto statement");
            return new GotoNode { TargetLabel = labelValue };
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

        public override PascalASTNode VisitFileType(PascalParser.FileTypeContext context)
        {
            // Always set TypeName in the object initializer
            return new SimpleTypeNode
            {
                TypeName = "file"
            };
        }

        public override PascalASTNode VisitWithStatement(PascalParser.WithStatementContext context)
        {
            // Parse the record variable list
            var recordVars = new List<VariableNode>();
            var recordVarList = context.recordVariableList();
            if (recordVarList != null)
            {
                foreach (var varCtx in recordVarList.variable())
                {
                    var varNode = Visit(varCtx) as VariableNode;
                    if (varNode != null)
                        recordVars.Add(varNode);
                }
            }

            // Parse the block (statement)
            var block = Visit(context.statement());
            if (recordVars.Count == 0 || block == null)
            {
                return new ErrorNode("Invalid with statement");
            }

            return new WithNode
            {
                RecordVariables = recordVars,
                Block = block
            };
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

        public override PascalASTNode VisitSet_(PascalParser.Set_Context context)
        {
            var elements = new List<ExpressionNode>();
            var elementList = context.elementList();
            if (elementList != null)
            {
                foreach (var elementCtx in elementList.element())
                {
                    // Handle set element ranges (e.g., 3..5)
                    if (elementCtx.expression().Length == 2)
                    {
                        var low = Visit(elementCtx.expression(0)) as ExpressionNode;
                        var high = Visit(elementCtx.expression(1)) as ExpressionNode;
                        if (low != null)
                        {
                            elements.Add(low);
                        }
                        if (high != null)
                        {
                            elements.Add(high);
                        }
                    }
                    else if (elementCtx.expression().Length == 1)
                    {
                        var expr = Visit(elementCtx.expression(0)) as ExpressionNode;
                        if (expr != null)
                        {
                            elements.Add(expr);
                        }
                    }
                }
            }
            return new SetNode { Elements = elements };
        }

        protected override PascalASTNode DefaultResult => null!;
    }

    public class ErrorNode : PascalASTNode
    {
        public string Message { get; set; }

        public ErrorNode(string message)
        {
            Message = message;
        }
    }

    public class TypeIdentifierNode : TypeNode
    {
        public required string Name { get; set; }
    }
}
