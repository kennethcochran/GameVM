using System;
using System.Collections.Generic;
using GameVM.Compiler.Core.IR;
using GameVM.Compiler.Core.IR.Slab;
using GameVM.Compiler.Core.IR.SlabProcessing;
using static GameVM.Compiler.Core.IR.Slab.InstructionMetadataFlags;
using static GameVM.Compiler.Core.IR.Slab.InstructionMetadata;

namespace GameVM.Compiler.CSharp.Transformers
{
    public class CSharpAstToHlirTransformer
    {
        private readonly string _sourceFile;
        private GameVM.Compiler.Core.IR.HighLevelIR _hlir = new();
        private readonly Dictionary<uint, string> _stringLiterals = new();

        public CSharpAstToHlirTransformer(string sourceFile = "<source>")
        {
            _sourceFile = sourceFile;
        }

        public GameVM.Compiler.Core.IR.HighLevelIR Transform(uint[] astSlab)
        {
            _hlir = new GameVM.Compiler.Core.IR.HighLevelIR { SourceFile = _sourceFile };
            _stringLiterals.Clear();

            if (astSlab == null || astSlab.Length < GameVM.Compiler.Core.IR.Slab.SlabHeader.HeaderIndex.Length)
            {
                _hlir.Errors.Add("Invalid AST slab: too small");
                return _hlir;
            }

            var header = GameVM.Compiler.Core.IR.Slab.SlabHeader.Read(astSlab);
            if (!header.HasValidMagic())
            {
                _hlir.Errors.Add("Invalid AST slab: invalid magic number");
                return _hlir;
            }

            // First pass: collect string literals for better debugging
            CollectStringLiterals(astSlab);

            // Process the main program block
            ProcessProgramBlock(astSlab);

            return _hlir;
        }

        private void CollectStringLiterals(uint[] slab)
        {
            int offset = GameVM.Compiler.Core.IR.Slab.SlabHeader.HeaderIndex.Length;
            while (offset < slab.Length)
            {
                var metadata = slab[offset];
                var size = GameVM.Compiler.Core.IR.Slab.MetadataDecoder.DecodeSize(metadata);
                var kind = GameVM.Compiler.Core.IR.Slab.MetadataDecoder.DecodeKind(metadata);

                if (size == 0 || offset + size > slab.Length)
                    break;

                if (kind == GameVM.Compiler.Core.IR.Slab.InstructionMetadataFlags.LITERAL_STRING && size >= 2)
                {
                    uint stringHash = slab[offset + 1];
                    if (!_stringLiterals.ContainsKey(stringHash))
                    {
                        // We can't easily get the actual string from the hash,
                        // but we can store it for potential debugging
                        _stringLiterals[stringHash] = $"<string_{stringHash:X}>";
                    }
                }

                offset += size;
            }
        }

        private void ProcessProgramBlock(uint[] slab)
        {
            int offset = GameVM.Compiler.Core.IR.Slab.SlabHeader.HeaderIndex.Length;
            var mainFunction = CreateMainFunction();

            // Process all top-level statements
            while (offset < slab.Length)
            {
                var metadata = slab[offset];
                var size = GameVM.Compiler.Core.IR.Slab.MetadataDecoder.DecodeSize(metadata);
                var kind = GameVM.Compiler.Core.IR.Slab.MetadataDecoder.DecodeKind(metadata);
                var argCount = GameVM.Compiler.Core.IR.Slab.MetadataDecoder.DecodeArgCount(metadata);

                if (size == 0 || offset + size > slab.Length)
                {
                    _hlir.Errors.Add($"Invalid instruction at offset {offset}: size {size}");
                    break;
                }

                try
                {
                    ProcessStatement(slab, offset, mainFunction.Body);
                }
                catch (Exception ex)
                {
                    _hlir.Errors.Add($"Error processing statement at offset {offset}: {ex.Message}");
                }

                offset += size;
            }

            // Add the main function to the program
            _hlir.TopLevel.Add(mainFunction);
        }

        private GameVM.Compiler.Core.IR.HighLevelIR.Function CreateMainFunction()
        {
            return new GameVM.Compiler.Core.IR.HighLevelIR.Function
            {
                Name = "Main",
                SourceFile = _sourceFile,
                ReturnType = new GameVM.Compiler.Core.IR.HighLevelIR.BasicType(_sourceFile, "void"),
                Body = new GameVM.Compiler.Core.IR.HighLevelIR.Block(_sourceFile)
            };
        }

        private void ProcessStatement(uint[] slab, int offset, GameVM.Compiler.Core.IR.HighLevelIR.Block targetBlock)
        {
            var metadata = slab[offset];
            var size = GameVM.Compiler.Core.IR.Slab.MetadataDecoder.DecodeSize(metadata);
            var kind = GameVM.Compiler.Core.IR.Slab.MetadataDecoder.DecodeKind(metadata);
            var argCount = GameVM.Compiler.Core.IR.Slab.MetadataDecoder.DecodeArgCount(metadata);

            switch (kind)
            {
                case GameVM.Compiler.Core.IR.Slab.InstructionMetadataFlags.VARIABLE_DECLARATION:
                    ProcessVariableDeclaration(slab, offset, targetBlock);
                    break;
                case GameVM.Compiler.Core.IR.Slab.InstructionMetadataFlags.ASSIGNMENT:
                    ProcessAssignment(slab, offset, targetBlock);
                    break;
                case GameVM.Compiler.Core.IR.Slab.InstructionMetadataFlags.EXPRESSION_STATEMENT:
                    ProcessExpressionStatement(slab, offset, targetBlock);
                    break;
                case GameVM.Compiler.Core.IR.Slab.InstructionMetadataFlags.IF_STATEMENT:
                    ProcessIfStatement(slab, offset, targetBlock);
                    break;
                case GameVM.Compiler.Core.IR.Slab.InstructionMetadataFlags.WHILE_STATEMENT:
                    ProcessWhileStatement(slab, offset, targetBlock);
                    break;
                case GameVM.Compiler.Core.IR.Slab.InstructionMetadataFlags.RETURN_STATEMENT:
                    ProcessReturnStatement(slab, offset, targetBlock);
                    break;
                case GameVM.Compiler.Core.IR.Slab.InstructionMetadataFlags.BLOCK:
                    ProcessBlock(slab, offset, targetBlock);
                    break;
                default:
                    // For expression statements that aren't wrapped, treat as expression statement
                    if (IsExpression(kind))
                    {
                        ProcessExpressionStatement(slab, offset, targetBlock);
                    }
                    break;
            }
        }

        private static bool IsExpression(byte kind)
        {
            return kind == GameVM.Compiler.Core.IR.Slab.InstructionMetadataFlags.LITERAL_INT || 
                   kind == GameVM.Compiler.Core.IR.Slab.InstructionMetadataFlags.LITERAL_STRING || 
                   kind == GameVM.Compiler.Core.IR.Slab.InstructionMetadataFlags.LITERAL_BOOL ||
                   kind == GameVM.Compiler.Core.IR.Slab.InstructionMetadataFlags.IDENTIFIER;
        }

        private void ProcessVariableDeclaration(uint[] slab, int offset, GameVM.Compiler.Core.IR.HighLevelIR.Block targetBlock)
        {
            // VARIABLE_DECLARATION: [metadata, typeKind, varNameHash]
            if (slab.Length < offset + 3) return;

            byte typeKind = (byte)slab[offset + 1];
            uint varNameHash = slab[offset + 2];

            string typeName = typeKind switch
            {
                1 => "int",
                2 => "string",
                3 => "bool",
                _ => "unknown"
            };

            var variable = new GameVM.Compiler.Core.IR.HighLevelIR.Variable
            {
                Name = $"var_{varNameHash:X}",
                Type = new GameVM.Compiler.Core.IR.HighLevelIR.BasicType(_sourceFile, typeName),
                SourceFile = _sourceFile
            };

            targetBlock.Statements.Add(variable);

            // Check if the next instruction is an initializer expression
            int nextOffset = offset + 3; // Skip VARIABLE_DECLARATION instruction
            if (nextOffset < slab.Length)
            {
                var nextMeta = slab[nextOffset];
                var nextSize = GameVM.Compiler.Core.IR.Slab.MetadataDecoder.DecodeSize(nextMeta);
                
                if (nextSize > 0 && IsExpression(GameVM.Compiler.Core.IR.Slab.MetadataDecoder.DecodeKind(nextMeta)))
                {
                    // This is an initializer, create an assignment
                    var initExpr = ResolveExpression(slab, (uint)nextOffset);
                    if (initExpr != null)
                    {
                        var assignment = new GameVM.Compiler.Core.IR.HighLevelIR.Assignment
                        {
                            Target = variable.Name,
                            Value = (GameVM.Compiler.Core.IR.Expression)initExpr,
                            SourceFile = _sourceFile
                        };
                        
                        // Replace the variable declaration with an assignment
                        targetBlock.Statements.RemoveAt(targetBlock.Statements.Count - 1);
                        targetBlock.Statements.Add(assignment);
                        
                        // Skip the initializer expression in the main loop
                        // Note: This is a simplified approach - in reality we'd need to be more careful
                    }
                }
            }
        }

        private void ProcessAssignment(uint[] slab, int offset, GameVM.Compiler.Core.IR.HighLevelIR.Block targetBlock)
        {
            // ASSIGNMENT: [metadata, targetOffset, valueOffset]
            if (slab.Length < offset + 3) return;

            uint targetOffset = slab[offset + 1];
            uint valueOffset = slab[offset + 2];

            var targetExpr = ResolveExpression(slab, targetOffset);
            var valueExpr = ResolveExpression(slab, valueOffset);

            if (targetExpr != null && valueExpr != null)
            {
                var assignment = new GameVM.Compiler.Core.IR.HighLevelIR.Assignment
                {
                    Target = ExtractVariableName(targetExpr),
                    Value = (GameVM.Compiler.Core.IR.Expression)valueExpr,
                    SourceFile = _sourceFile
                };

                targetBlock.Statements.Add(assignment);
            }
        }

        private void ProcessExpressionStatement(uint[] slab, int offset, GameVM.Compiler.Core.IR.HighLevelIR.Block targetBlock)
        {
            // EXPRESSION_STATEMENT: [metadata, exprOffset]
            if (slab.Length < offset + 2) return;

            uint exprOffset = slab[offset + 1];
            var expr = ResolveExpression(slab, exprOffset);

            if (expr != null)
            {
                var exprStmt = new GameVM.Compiler.Core.IR.HighLevelIR.ExpressionStatement
                {
                    Expression = (GameVM.Compiler.Core.IR.Expression)expr,
                    SourceFile = _sourceFile
                };

                targetBlock.Statements.Add(exprStmt);
            }
        }

        private void ProcessIfStatement(uint[] slab, int offset, GameVM.Compiler.Core.IR.HighLevelIR.Block targetBlock)
        {
            // IF_STATEMENT: [metadata, conditionOffset, thenOffset, elseOffset?]
            if (slab.Length < offset + 3) return;

            uint conditionOffset = slab[offset + 1];
            uint thenOffset = slab[offset + 2];
            uint elseOffset = 0;
            bool hasElse = false;

            if (slab.Length >= offset + 4)
            {
                // Check if there's a fourth argument (else branch)
                uint possibleElse = slab[offset + 3];
                // Simple heuristic: if it's a reasonable offset, treat as else
                if (possibleElse < (uint)slab.Length && possibleElse > offset + 4)
                {
                    elseOffset = possibleElse;
                    hasElse = true;
                }
            }

            var condition = ResolveExpression(slab, conditionOffset);
            if (condition == null) return;

            var ifStmt = new GameVM.Compiler.Core.IR.HighLevelIR.IfStatement
            {
                Condition = (GameVM.Compiler.Core.IR.Expression)condition,
                SourceFile = _sourceFile
            };

            var thenBlock = new GameVM.Compiler.Core.IR.HighLevelIR.Block(_sourceFile);
            var elseBlock = hasElse ? new GameVM.Compiler.Core.IR.HighLevelIR.Block(_sourceFile) : null;

            // Process then block
            int thenPos = (int)thenOffset;
            while (thenPos < slab.Length && thenPos < (hasElse ? (int)elseOffset : slab.Length))
            {
                var thenMeta = slab[thenPos];
                var thenSize = GameVM.Compiler.Core.IR.Slab.MetadataDecoder.DecodeSize(thenMeta);
                if (thenSize == 0 || thenPos + thenSize > slab.Length) break;
                
                ProcessStatement(slab, thenPos, thenBlock);
                thenPos += (int)thenSize;
            }

            // Process else block if present
            if (hasElse && elseBlock != null)
            {
                int elsePos = (int)elseOffset;
                while (elsePos < slab.Length)
                {
                    var elseMeta = slab[elsePos];
                    var elseSize = GameVM.Compiler.Core.IR.Slab.MetadataDecoder.DecodeSize(elseMeta);
                    if (elseSize == 0 || elsePos + elseSize > slab.Length) break;
                    
                    ProcessStatement(slab, elsePos, elseBlock);
                    elsePos += (int)elseSize;
                }
            }

            ifStmt.SetThenBlock(thenBlock.Statements);
            ifStmt.SetElseBlock(elseBlock?.Statements);

            targetBlock.Statements.Add(ifStmt);
        }

        private void ProcessWhileStatement(uint[] slab, int offset, GameVM.Compiler.Core.IR.HighLevelIR.Block targetBlock)
        {
            // WHILE_STATEMENT: [metadata, conditionOffset, bodyOffset]
            if (slab.Length < offset + 3) return;

            uint conditionOffset = slab[offset + 1];
            uint bodyOffset = slab[offset + 2];

            var condition = ResolveExpression(slab, conditionOffset);
            if (condition == null) return;

            var whileStmt = new GameVM.Compiler.Core.IR.HighLevelIR.While
            {
                Condition = (GameVM.Compiler.Core.IR.Expression)condition,
                Body = new GameVM.Compiler.Core.IR.HighLevelIR.Block(_sourceFile),
                SourceFile = _sourceFile
            };

            int bodyPos = (int)bodyOffset;
            while (bodyPos < slab.Length)
            {
                var bodyMeta = slab[bodyPos];
                var bodySize = GameVM.Compiler.Core.IR.Slab.MetadataDecoder.DecodeSize(bodyMeta);
                if (bodySize == 0 || bodyPos + bodySize > slab.Length) break;
                
                ProcessStatement(slab, bodyPos, whileStmt.Body);
                bodyPos += (int)bodySize;
            }

            targetBlock.Statements.Add(whileStmt);
        }

        private void ProcessReturnStatement(uint[] slab, int offset, GameVM.Compiler.Core.IR.HighLevelIR.Block targetBlock)
        {
            // RETURN_STATEMENT: [metadata, expressionOffset?]
            if (slab.Length < offset + 1) return;

            uint exprOffset = slab.Length > offset + 1 ? slab[offset + 1] : 0;
            bool hasExpression = slab.Length > offset + 1;

            var returnStmt = new GameVM.Compiler.Core.IR.HighLevelIR.ReturnStatement
            {
                SourceFile = _sourceFile
            };

            if (hasExpression && exprOffset < slab.Length)
            {
                var expr = ResolveExpression(slab, exprOffset);
                if (expr != null)
                {
                    returnStmt.Value = (GameVM.Compiler.Core.IR.Expression)expr;
                }
            }

            targetBlock.Statements.Add(returnStmt);
        }

        private void ProcessBlock(uint[] slab, int offset, GameVM.Compiler.Core.IR.HighLevelIR.Block targetBlock)
        {
            // BLOCK: [metadata, statementOffset1, statementOffset2, ...]
            // First, count how many statements we have
            if (slab.Length < offset + 1) return;

            // Simple approach: treat everything after the metadata as statement offsets
            // until we hit an invalid offset or end of slab
            int stmtStartOffset = offset + 1;
            int currentOffset = stmtStartOffset;

            var block = new GameVM.Compiler.Core.IR.HighLevelIR.Block(_sourceFile);
            targetBlock.Statements.Add((GameVM.Compiler.Core.IR.Statement)block);

            while (currentOffset < slab.Length)
            {
                // Check if this looks like a valid statement offset
                uint potentialOffset = slab[currentOffset];
                if (potentialOffset >= slab.Length) break;
                
                var meta = slab[potentialOffset];
                var size = GameVM.Compiler.Core.IR.Slab.MetadataDecoder.DecodeSize(meta);
                if (size == 0 || potentialOffset + size > slab.Length) break;
                
                // Process this statement
                ProcessStatement(slab, (int)potentialOffset, block);
                
                // Move to next potential offset
                currentOffset++;
                
                // Safety check to prevent infinite loop
                if (currentOffset - stmtStartOffset > 100) break;
            }
        }

        private GameVM.Compiler.Core.IR.HighLevelIR.Expression ResolveExpression(uint[] slab, uint exprOffset)
        {
            if (exprOffset >= slab.Length) return null;

            var metadata = slab[exprOffset];
            var size = GameVM.Compiler.Core.IR.Slab.MetadataDecoder.DecodeSize(metadata);
            var kind = GameVM.Compiler.Core.IR.Slab.MetadataDecoder.DecodeKind(metadata);
            var argCount = GameVM.Compiler.Core.IR.Slab.MetadataDecoder.DecodeArgCount(metadata);

            if (size == 0 || exprOffset + size > slab.Length) return null;

            return kind switch
            {
                GameVM.Compiler.Core.IR.Slab.InstructionMetadataFlags.LITERAL_INT => new GameVM.Compiler.Core.IR.HighLevelIR.Literal
                {
                    Value = (int)slab[exprOffset + 1],
                    Type = new GameVM.Compiler.Core.IR.HighLevelIR.BasicType(_sourceFile, "int"),
                    SourceFile = _sourceFile
                },
                GameVM.Compiler.Core.IR.Slab.InstructionMetadataFlags.LITERAL_STRING => new GameVM.Compiler.Core.IR.HighLevelIR.Literal
                {
                    Value = _stringLiterals.TryGetValue(slab[exprOffset + 1], out var str) 
                            ? str 
                            : $"<string_{slab[exprOffset + 1]:X}>",
                    Type = new GameVM.Compiler.Core.IR.HighLevelIR.BasicType(_sourceFile, "string"),
                    SourceFile = _sourceFile
                },
                GameVM.Compiler.Core.IR.Slab.InstructionMetadataFlags.LITERAL_BOOL => new GameVM.Compiler.Core.IR.HighLevelIR.Literal
                {
                    Value = slab[exprOffset + 1] != 0,
                    Type = new GameVM.Compiler.Core.IR.HighLevelIR.BasicType(_sourceFile, "bool"),
                    SourceFile = _sourceFile
                },
                GameVM.Compiler.Core.IR.Slab.InstructionMetadataFlags.IDENTIFIER => new GameVM.Compiler.Core.IR.HighLevelIR.Identifier
                {
                    Name = $"id_{slab[exprOffset + 1]:X}",
                    Type = new GameVM.Compiler.Core.IR.HighLevelIR.BasicType(_sourceFile, "unknown"),
                    SourceFile = _sourceFile
                },
                _ => null
            };
        }

        private static string ExtractVariableName(GameVM.Compiler.Core.IR.HighLevelIR.Expression expr)
        {
            if (expr is GameVM.Compiler.Core.IR.Identifier ident)
            {
                return ident.Name;
            }
            return "unknown";
        }
    }
}