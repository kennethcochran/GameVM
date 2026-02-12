using GameVM.Compiler.Core.Interfaces;
using GameVM.Compiler.Core.IR;

namespace GameVM.Compiler.Core.SemanticAnalysis
{
    /// <summary>
    /// Basic semantic analyzer implementation focused on type checking and symbol resolution
    /// </summary>
    public class BasicSemanticAnalyzer : ISemanticAnalyzer
    {
        public SemanticAnalysisResult Analyze(HighLevelIR hlir)
        {
            var result = new SemanticAnalysisResult();
            
            // Phase 1: Basic semantic validation
            ValidateBasicSemantics(hlir, result);
            
            // Phase 2: Type checking
            ValidateTypes(hlir, result);
            
            // Phase 3: Symbol resolution
            ValidateSymbolUsage(hlir, result);
            
            result.Success = !result.Errors.Any();
            return result;
        }

        private void ValidateBasicSemantics(HighLevelIR hlir, SemanticAnalysisResult result)
        {
            // Basic structural validation
            if (hlir == null)
            {
                result.Errors.Add("HLIR cannot be null");
                return;
            }

            // Check for required program structure
            if (!hlir.Functions.Any())
            {
                result.Errors.Add("Program must contain at least one function");
            }
        }

        private void ValidateTypes(HighLevelIR hlir, SemanticAnalysisResult result)
        {
            // Walk through all functions and validate type assignments
            foreach (var function in hlir.Functions)
            {
                ValidateFunctionTypes(function, result);
            }
        }

        private void ValidateFunctionTypes(HighLevelIR.Function function, SemanticAnalysisResult result)
        {
            // Check for type mismatches in assignments
            foreach (var instruction in function.Instructions)
            {
                if (instruction is HighLevelIR.AssignmentInstruction assignment)
                {
                    ValidateAssignmentTypes(assignment, result);
                }
            }
        }

        private void ValidateAssignmentTypes(HighLevelIR.AssignmentInstruction assignment, SemanticAnalysisResult result)
        {
            // Basic type compatibility check
            var targetType = assignment.Target.Type;
            var sourceType = assignment.Value.Type;

            if (targetType != null && sourceType != null)
            {
                if (!AreTypesCompatible(targetType, sourceType))
                {
                    result.Errors.Add($"Type mismatch: cannot assign {sourceType.Name} to {targetType.Name}");
                }
            }
        }

        private void ValidateSymbolUsage(HighLevelIR hlir, SemanticAnalysisResult result)
        {
            // Build symbol table from all declarations
            var symbolTable = new Dictionary<string, HighLevelIR.HlType>(StringComparer.OrdinalIgnoreCase);
            
            // Collect all declared symbols
            foreach (var function in hlir.Functions)
            {
                CollectDeclaredSymbols(function, symbolTable);
            }

            // Validate symbol usage
            foreach (var function in hlir.Functions)
            {
                ValidateSymbolUsage(function, symbolTable, result);
            }
        }

        private void CollectDeclaredSymbols(HighLevelIR.Function function, Dictionary<string, HighLevelIR.HlType> symbolTable)
        {
            // Collect parameters
            foreach (var param in function.Parameters)
            {
                symbolTable[param.Name] = param.Type;
            }

            // Collect local variables from instructions
            foreach (var instruction in function.Instructions)
            {
                if (instruction is HighLevelIR.VariableDeclaration varDecl)
                {
                    symbolTable[varDecl.Name] = varDecl.Type;
                }
            }
        }

        private void ValidateSymbolUsage(HighLevelIR.Function function, Dictionary<string, HighLevelIR.HlType> symbolTable, SemanticAnalysisResult result)
        {
            foreach (var instruction in function.Instructions)
            {
                if (instruction is HighLevelIR.AssignmentInstruction assignment)
                {
                    // Check if target is declared
                    if (!symbolTable.ContainsKey(assignment.Target.Name))
                    {
                        result.Errors.Add($"Undefined variable: {assignment.Target.Name}");
                    }
                }
            }
        }

        private bool AreTypesCompatible(HighLevelIR.HlType targetType, HighLevelIR.HlType sourceType)
        {
            // Basic compatibility rules
            if (targetType == sourceType)
                return true;

            // Allow implicit conversions (e.g., integer to real)
            if (targetType.Name.Equals("Real", StringComparison.OrdinalIgnoreCase) && 
                sourceType.Name.Equals("Integer", StringComparison.OrdinalIgnoreCase))
                return true;

            // Add more compatibility rules as needed
            return false;
        }
    }
}