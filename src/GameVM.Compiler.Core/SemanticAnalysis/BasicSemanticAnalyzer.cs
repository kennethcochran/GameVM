using System;
using System.Collections.Generic;
using GameVM.Compiler.Core.Interfaces;
using GameVM.Compiler.Core.IR;
using GameVM.Compiler.Core.IR.Slab;
using GameVM.Compiler.Core.IR.SlabProcessing;
using GameVM.Compiler.Core.IR.Buffers;
#pragma warning disable CS0618 // Type or member is obsolete

namespace GameVM.Compiler.Core.SemanticAnalysis
{
    public class BasicSemanticAnalyzer : ISemanticAnalyzer
    {
        public SemanticAnalysisResult Analyze(HighLevelIR hlir)
        {
            var result = new SemanticAnalysisResult();
            
            if (hlir == null)
            {
                result.Success = false;
                result.Errors.Add("HLIR is null");
                return result;
            }
            
            // For now, we'll analyze the OOP HLIR structure
            // In a full DOD implementation, this would process the slab directly
            if (hlir.Globals != null)
            {
                foreach (var global in hlir.Globals.Values)
                {
                    if (global is HighLevelIR.Variable variable && string.IsNullOrEmpty(variable.Type?.Name))
                    {
                        result.Errors.Add($"Global variable '{variable.Name}' has invalid type");
                        result.Success = false;
                    }
                }
            }
            
            return result;
        }

        /// <summary>
        /// Analyzes a DOD HLIR slab using linear iteration and switch-based processing.
        /// This is the DOD-native semantic analysis that replaces tree traversal patterns.
        /// </summary>
        public SemanticAnalysisResult AnalyzeSlab(uint[] hlirSlab)
        {
            var result = new SemanticAnalysisResult();
            
            if (hlirSlab == null || hlirSlab.Length < SlabHeader.HeaderIndex.Length)
            {
                result.Success = false;
                result.Errors.Add("Invalid HLIR slab: too small or null");
                return result;
            }
            
            var header = SlabHeader.Read(hlirSlab);
            if (!header.HasValidMagic())
            {
                result.Success = false;
                result.Errors.Add("Invalid HLIR slab: invalid magic number");
                return result;
            }
            
            if (header.IrStage != 1)
            {
                result.Success = false;
                result.Errors.Add($"Expected HLIR slab (stage 1), got stage {header.IrStage}");
                return result;
            }
            
            var diagnostics = new DiagnosticJournal();
            int offset = SlabHeader.HeaderIndex.Length;
            int functionCount = 0;
            
            while (offset < hlirSlab.Length)
            {
                var metadata = hlirSlab[offset];
                var size = InstructionMetadata.DecodeSize(metadata);
                var kind = InstructionMetadata.DecodeKind(metadata);
                
                if (size == 0 || offset + size > hlirSlab.Length)
                {
                    diagnostics.Record((uint)offset, 0, 0, 2001); // Invalid instruction size
                    break;
                }
                
                if (kind == InstructionMetadataFlags.METHOD_DECLARATION)
                {
                    ProcessFunction(hlirSlab, offset, size, diagnostics);
                    functionCount++;
                }
                
                offset += size;
            }
            
            result.Success = diagnostics.Count == 0;
            for (uint i = 0; i < diagnostics.Count; i++)
            {
                if (diagnostics.TryGet(i, out var diag))
                {
                    result.Errors.Add($"[Offset {diag.SlabOffset}] Semantic error: {diag.DiagnosticCode}");
                }
            }
            
            return result;
        }
        
        private void ProcessFunction(uint[] slab, int funcOffset, int funcSize, DiagnosticJournal diagnostics)
        {
            if (funcOffset + 1 >= slab.Length) return;
            
            int bodyOffset = funcOffset + 2;
            int bodyEndOffset = funcOffset + funcSize;
            int currentOffset = bodyOffset;
            
            var functionScope = new Dictionary<string, TypeInfo>();
            
            while (currentOffset < bodyEndOffset && currentOffset < slab.Length)
            {
                var stmtMeta = slab[currentOffset];
                var stmtSize = InstructionMetadata.DecodeSize(stmtMeta);
                var stmtKind = InstructionMetadata.DecodeKind(stmtMeta);
                
                if (stmtSize == 0 || currentOffset + stmtSize > slab.Length)
                {
                    diagnostics.Record((uint)currentOffset, 0, 0, 2002);
                    break;
                }
                
                ProcessStatement(slab, currentOffset, stmtKind, functionScope, diagnostics);
                currentOffset += stmtSize;
            }
        }
        
        private void ProcessStatement(uint[] slab, int offset, byte kind,
            Dictionary<string, TypeInfo> scope, DiagnosticJournal diagnostics)
        {
            switch (kind)
            {
                case InstructionMetadataFlags.ASSIGNMENT:
                    ProcessAssignment(slab, offset, scope, diagnostics);
                    break;
                case InstructionMetadataFlags.EXPRESSION_STATEMENT:
                    ProcessExpressionStatement(slab, offset, scope, diagnostics);
                    break;
                case InstructionMetadataFlags.IF_STATEMENT:
                    ProcessIfStatement(slab, offset, scope, diagnostics);
                    break;
                case InstructionMetadataFlags.WHILE_STATEMENT:
                    ProcessWhileStatement(slab, offset, scope, diagnostics);
                    break;
                case InstructionMetadataFlags.RETURN_STATEMENT:
                    ProcessReturnStatement(slab, offset, scope, diagnostics);
                    break;
                case InstructionMetadataFlags.BLOCK:
                    ProcessBlock(slab, offset, scope, diagnostics);
                    break;
                case InstructionMetadataFlags.VARIABLE_DECLARATION:
                    ProcessVariableDeclaration(slab, offset, scope, diagnostics);
                    break;
                default:
                    if (IsExpressionKind(kind))
                    {
                        ProcessExpressionStatement(slab, offset, scope, diagnostics);
                    }
                    else
                    {
                        diagnostics.Record((uint)offset, 0, 0, 2003);
                    }
                    break;
            }
        }
        
        private void ProcessAssignment(uint[] slab, int offset, Dictionary<string, TypeInfo> scope, DiagnosticJournal diagnostics)
        {
            if (offset + 2 >= slab.Length) return;
            
            uint targetOffset = slab[offset + 1];
            uint valueOffset = slab[offset + 2];
            
            string targetName = ResolveIdentifier(slab, targetOffset);
            if (string.IsNullOrEmpty(targetName))
            {
                diagnostics.Record((uint)offset, 0, 0, 2004);
                return;
            }
            
            var valueType = ResolveExpressionType(slab, valueOffset, scope, diagnostics);
            if (valueType == null)
            {
                diagnostics.Record((uint)offset, 0, 0, 2005);
                return;
            }
            
            if (!scope.TryGetValue(targetName, out var targetVar))
            {
                scope[targetName] = new TypeInfo { Name = targetName, Type = valueType, IsInitialized = true };
            }
            else
            {
                if (!AreTypesCompatible(targetVar.Type, valueType))
                {
                    diagnostics.Record((uint)offset, 0, 0, 2006);
                }
                targetVar.IsInitialized = true;
            }
        }
        
        private void ProcessExpressionStatement(uint[] slab, int offset, Dictionary<string, TypeInfo> scope, DiagnosticJournal diagnostics)
        {
            if (offset + 1 >= slab.Length) return;
            
            uint exprOffset = slab[offset + 1];
            ResolveExpressionType(slab, exprOffset, scope, diagnostics);
        }
        
        private void ProcessIfStatement(uint[] slab, int offset, Dictionary<string, TypeInfo> scope, DiagnosticJournal diagnostics)
        {
            if (offset + 2 >= slab.Length) return;
            
            uint conditionOffset = slab[offset + 1];
            uint thenBlockOffset = slab[offset + 2];
            uint elseBlockOffset = (offset + 3 < slab.Length) ? slab[offset + 3] : 0;
            
            var condType = ResolveExpressionType(slab, conditionOffset, scope, diagnostics);
            if (condType != null && condType != "Boolean" && condType != "bool")
            {
                diagnostics.Record((uint)offset, 0, 0, 2007);
            }
            
            if (thenBlockOffset != 0 && thenBlockOffset < slab.Length)
            {
                var thenScope = new Dictionary<string, TypeInfo>(scope);
                ProcessBlockAtOffset(slab, thenBlockOffset, thenScope, diagnostics);
            }
            
            if (elseBlockOffset != 0 && elseBlockOffset < slab.Length)
            {
                var elseScope = new Dictionary<string, TypeInfo>(scope);
                ProcessBlockAtOffset(slab, elseBlockOffset, elseScope, diagnostics);
            }
        }
        
        private void ProcessWhileStatement(uint[] slab, int offset, Dictionary<string, TypeInfo> scope, DiagnosticJournal diagnostics)
        {
            if (offset + 2 >= slab.Length) return;
            
            uint conditionOffset = slab[offset + 1];
            uint bodyBlockOffset = slab[offset + 2];
            
            var condType = ResolveExpressionType(slab, conditionOffset, scope, diagnostics);
            if (condType != null && condType != "Boolean" && condType != "bool")
            {
                diagnostics.Record((uint)offset, 0, 0, 2007);
            }
            
            if (bodyBlockOffset != 0 && bodyBlockOffset < slab.Length)
            {
                var loopScope = new Dictionary<string, TypeInfo>(scope);
                ProcessBlockAtOffset(slab, bodyBlockOffset, loopScope, diagnostics);
            }
        }
        
        private void ProcessReturnStatement(uint[] slab, int offset, Dictionary<string, TypeInfo> scope, DiagnosticJournal diagnostics)
        {
            if (offset + 1 < slab.Length)
            {
                uint exprOffset = slab[offset + 1];
                if (exprOffset < slab.Length)
                {
                    ResolveExpressionType(slab, exprOffset, scope, diagnostics);
                }
            }
        }
        
        private void ProcessBlock(uint[] slab, int offset, Dictionary<string, TypeInfo> parentScope, DiagnosticJournal diagnostics)
        {
            int stmtIndex = offset + 1;
            var blockScope = new Dictionary<string, TypeInfo>(parentScope);
            
            while (stmtIndex < slab.Length)
            {
                uint potentialOffset = slab[stmtIndex];
                if (potentialOffset >= slab.Length) break;
                
                var meta = slab[potentialOffset];
                var size = InstructionMetadata.DecodeSize(meta);
                if (size == 0 || (int)potentialOffset + size > slab.Length) break;
                
                var kind = InstructionMetadata.DecodeKind(meta);
                ProcessStatement(slab, (int)potentialOffset, kind, blockScope, diagnostics);
                ++stmtIndex;
            }
        }
        
        private void ProcessBlockAtOffset(uint[] slab, uint blockOffset, Dictionary<string, TypeInfo> scope, DiagnosticJournal diagnostics)
        {
            if (blockOffset >= slab.Length) return;
            
            var meta = slab[blockOffset];
            var size = InstructionMetadata.DecodeSize(meta);
            var kind = InstructionMetadata.DecodeKind(meta);
            
            if (size > 0 && kind == InstructionMetadataFlags.BLOCK)
            {
                ProcessBlock(slab, (int)blockOffset, scope, diagnostics);
            }
        }
        
        private void ProcessVariableDeclaration(uint[] slab, int offset, Dictionary<string, TypeInfo> scope, DiagnosticJournal diagnostics)
        {
            if (offset + 2 >= slab.Length) return;
            
            uint typeKind = slab[offset + 1];
            uint varNameHash = slab[offset + 2];
            
            string varName = $"var_{varNameHash:X}";
            string varType = GetTypeNameFromKind((byte)typeKind);
            
            if (scope.ContainsKey(varName))
            {
                diagnostics.Record((uint)offset, 0, 0, 2008);
                return;
            }
            
            string initType = varType;
            int nextOffset = offset + 3;
            if (nextOffset < slab.Length)
            {
                var nextMeta = slab[nextOffset];
                var nextSize = InstructionMetadata.DecodeSize(nextMeta);
                var nextKind = InstructionMetadata.DecodeKind(nextMeta);
                
                if (nextSize > 0 && IsExpressionKind(nextKind))
                {
                    initType = ResolveExpressionType(slab, (uint)nextOffset, scope, diagnostics) ?? varType;
                }
            }
            
            scope[varName] = new TypeInfo 
            { 
                Name = varName, 
                Type = initType, 
                IsInitialized = initType != null 
            };
        }
        
        private static string ResolveIdentifier(uint[] slab, uint exprOffset)
        {
            if (exprOffset >= slab.Length) return string.Empty;
            
            var metadata = slab[exprOffset];
            var size = InstructionMetadata.DecodeSize(metadata);
            var kind = InstructionMetadata.DecodeKind(metadata);
            
            if (size == 0 || exprOffset + size > slab.Length) return string.Empty;
            
            if (kind == InstructionMetadataFlags.IDENTIFIER)
            {
                uint nameHash = slab[exprOffset + 1];
                return $"id_{nameHash:X}";
            }
            
            return string.Empty;
        }
        
        private string ResolveExpressionType(uint[] slab, uint exprOffset, Dictionary<string, TypeInfo> scope, DiagnosticJournal diagnostics)
        {
            if (exprOffset >= slab.Length) return "Unknown";
            
            var metadata = slab[exprOffset];
            var size = InstructionMetadata.DecodeSize(metadata);
            var kind = InstructionMetadata.DecodeKind(metadata);
            
            if (size == 0 || exprOffset + size > slab.Length) return "Unknown";
            
            return kind switch
            {
                InstructionMetadataFlags.LITERAL_INT => "Integer",
                InstructionMetadataFlags.LITERAL_STRING => "String",
                InstructionMetadataFlags.LITERAL_BOOL => "Boolean",
                InstructionMetadataFlags.IDENTIFIER => ResolveIdentifierType(slab, exprOffset, scope),
                InstructionMetadataFlags.BINARY_OP => ResolveBinaryOpType(slab, exprOffset, scope, diagnostics),
                _ => "Unknown"
            };
        }
        
        private static string ResolveIdentifierType(uint[] slab, uint exprOffset, Dictionary<string, TypeInfo> scope)
        {
            string name = ResolveIdentifier(slab, exprOffset);
            if (string.IsNullOrEmpty(name)) return "Unknown";
            
            if (scope.TryGetValue(name, out var var))
            {
                return var.Type;
            }
            
            return "Unknown";
        }
        
        private string ResolveBinaryOpType(uint[] slab, uint exprOffset, Dictionary<string, TypeInfo> scope, DiagnosticJournal diagnostics)
        {
            if (exprOffset + 3 >= slab.Length) return "Unknown";
            
            uint leftOffset = slab[exprOffset + 1];
            uint rightOffset = slab[exprOffset + 2];
            _ = slab[exprOffset + 3]; // opHash - reserved for future use
            
            string leftType = ResolveExpressionType(slab, leftOffset, scope, diagnostics) ?? "Unknown";
            string rightType = ResolveExpressionType(slab, rightOffset, scope, diagnostics) ?? "Unknown";
            
            if (leftType == rightType)
            {
                return leftType;
            }
            
            diagnostics.Record(exprOffset, 0, 0, 2009);
            return "Unknown";
        }
        
        private static bool AreTypesCompatible(string targetType, string valueType)
        {
            if (string.IsNullOrEmpty(targetType) || string.IsNullOrEmpty(valueType))
                return false;
            
            if (targetType == valueType) return true;
            
            return (targetType == "Real" && valueType == "Integer") ||
                   (targetType == "Integer" && valueType == "Real") ||
                   (targetType == "String" && valueType == "Char");
        }
        
        private static string GetTypeNameFromKind(byte kind)
        {
            return kind switch
            {
                1 => "Integer",
                2 => "Real",
                3 => "Boolean",
                4 => "Char",
                5 => "String",
                _ => "Unknown"
            };
        }
        
        private static bool IsExpressionKind(byte kind)
        {
            return kind == InstructionMetadataFlags.LITERAL_INT || 
                   kind == InstructionMetadataFlags.LITERAL_STRING || 
                   kind == InstructionMetadataFlags.LITERAL_BOOL || 
                   kind == InstructionMetadataFlags.IDENTIFIER || 
                   kind == InstructionMetadataFlags.BINARY_OP;
        }
        
        internal sealed class TypeInfo
        {
            public string Name { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty;
            public bool IsInitialized { get; set; }
            public bool IsParameter { get; set; }
            public bool IsConstant { get; set; }
        }
    }
}