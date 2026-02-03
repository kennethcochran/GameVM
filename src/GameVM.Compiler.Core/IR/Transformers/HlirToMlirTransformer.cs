using System.Collections.Generic;
using System.Linq;
using GameVM.Compiler.Core.IR;
using GameVM.Compiler.Core.IR.Interfaces;

namespace GameVM.Compiler.Core.IR.Transformers
{
    public class HlirToMlirTransformer : IIRTransformer<HighLevelIR, MidLevelIR>
    {
        private HighLevelIR? _currentHlir;
        public MidLevelIR Transform(HighLevelIR hlir)
        {
            _currentHlir = hlir;
            var mlir = new MidLevelIR { SourceFile = hlir.SourceFile };
            mlir.Modules.Clear();

            SyncGlobals(hlir, mlir);
            ProcessModules(hlir, mlir);
            ProcessLegacyFunctions(hlir, mlir);

            return mlir;
        }

        private static void SyncGlobals(HighLevelIR hlir, MidLevelIR mlir)
        {
            foreach (var global in hlir.Globals)
            {
                mlir.Globals[global.Key] = global.Value;
            }
        }

        private void ProcessModules(HighLevelIR hlir, MidLevelIR mlir)
        {
            if (hlir.Modules == null || hlir.Modules.Count == 0)
                return;

            foreach (var hlModule in hlir.Modules)
            {
                if (!ShouldProcessModule(hlModule))
                    continue;

                var mlModule = ProcessModule(hlModule);
                mlir.Modules.Add(mlModule);
            }
        }

        private static bool ShouldProcessModule(HlModule hlModule)
        {
            return hlModule.Functions.Count > 0 || 
                   hlModule.Types.Count > 0 || 
                   hlModule.Variables.Count > 0;
        }

        private MidLevelIR.MLModule ProcessModule(HlModule hlModule)
        {
            var mlModule = new MidLevelIR.MLModule { Name = hlModule.Name };

            foreach (var hlFunc in hlModule.Functions)
            {
                var mlFunc = ProcessFunction(hlFunc);
                mlModule.Functions.Add(mlFunc);
            }

            return mlModule;
        }

        private MidLevelIR.MLFunction ProcessFunction(HighLevelIR.Function hlFunc)
        {
            var mlFunc = new MidLevelIR.MLFunction { Name = hlFunc.Name };
            ProcessStatements(hlFunc.Body.Statements, mlFunc);
            return mlFunc;
        }

        private void ProcessLegacyFunctions(HighLevelIR hlir, MidLevelIR mlir)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            if (hlir.GlobalFunctions.Count == 0)
                return;

            var defaultModule = GetOrCreateDefaultModule(mlir);

            foreach (var hlFunc in hlir.GlobalFunctions.Values)
            {
                var mlFunc = new MidLevelIR.MLFunction { Name = hlFunc.Name };
                ProcessStatements(hlFunc.Body.Statements, mlFunc);
                defaultModule.Functions.Add(mlFunc);
            }
#pragma warning restore CS0618
        }

        private static MidLevelIR.MLModule GetOrCreateDefaultModule(MidLevelIR mlir)
        {
            var defaultModule = mlir.Modules.FirstOrDefault(m => m.Name == "default");
            if (defaultModule == null)
            {
                defaultModule = new MidLevelIR.MLModule { Name = "default" };
                mlir.Modules.Add(defaultModule);
            }
            return defaultModule;
        }

        private void ProcessStatements(List<HighLevelIR.Statement> nodes, MidLevelIR.MLFunction mlFunc)
        {
            foreach (var node in nodes)
            {
                ProcessStatement(node, mlFunc);
            }
        }

        private void ProcessStatement(HighLevelIR.Statement node, MidLevelIR.MLFunction mlFunc)
        {
            switch (node)
            {
                case HighLevelIR.Assignment assign:
                    ProcessAssignment(assign, mlFunc);
                    break;
                case HighLevelIR.ExpressionStatement exprStmt:
                    ProcessExpressionStatement(exprStmt, mlFunc);
                    break;
                case HighLevelIR.Block nestedBlock:
                    ProcessStatements(nestedBlock.Statements, mlFunc);
                    break;
                case HighLevelIR.IfStatement ifStmt:
                    ProcessIfStatement(ifStmt, mlFunc);
                    break;
                case HighLevelIR.While whileStmt:
                    ProcessWhileStatement(whileStmt, mlFunc);
                    break;
                case HighLevelIR.ReturnStatement returnStmt:
                    ProcessReturnStatement(returnStmt);
                    break;
            }
        }

        private void ProcessAssignment(HighLevelIR.Assignment assign, MidLevelIR.MLFunction mlFunc)
        {
            mlFunc.Instructions.Add(new MidLevelIR.MLAssign
            {
                Target = assign.Target,
                Source = GetExpressionValue(assign.Value)
            });
        }

        private void ProcessExpressionStatement(HighLevelIR.ExpressionStatement exprStmt, MidLevelIR.MLFunction mlFunc)
        {
            if (exprStmt.Expression is HighLevelIR.FunctionCall call)
            {
                var args = call.Arguments.Select(GetExpressionValue).ToList();
                var name = GetFunctionName(call);
                mlFunc.Instructions.Add(new MidLevelIR.MLCall
                {
                    Name = name,
                    Arguments = args
                });
            }
            else
            {
                // For non-function-call expressions (literals, identifiers, binary ops, etc.),
                // create a temporary assignment to _temp with the expression value
                var value = GetExpressionValue(exprStmt.Expression);
                mlFunc.Instructions.Add(new MidLevelIR.MLAssign
                {
                    Target = "_temp",
                    Source = value
                });
            }
        }

        private static string GetFunctionName(HighLevelIR.FunctionCall call)
        {
            return call.CallTarget is HighLevelIR.Identifier ident ? ident.Name : "unknown";
        }

        private void ProcessIfStatement(HighLevelIR.IfStatement ifStmt, MidLevelIR.MLFunction mlFunc)
        {
            var labelIdx = mlFunc.Instructions.Count;
            var thenLabel = $"then_{labelIdx}";
            var endLabel = $"endif_{labelIdx}";

            AddConditionalBranch(ifStmt, mlFunc, thenLabel, endLabel);
        }

        private void AddConditionalBranch(HighLevelIR.IfStatement ifStmt, MidLevelIR.MLFunction mlFunc, string thenLabel, string endLabel)
        {
            mlFunc.Instructions.Add(new MidLevelIR.MLBranch { Target = thenLabel, Condition = GetExpressionValue(ifStmt.Condition) });
            
            if (HasElseBlock(ifStmt))
            {
                ProcessIfWithElse(ifStmt, mlFunc, thenLabel, endLabel);
            }
            else
            {
                ProcessIfWithoutElse(ifStmt, mlFunc, thenLabel, endLabel);
            }
        }

        private static bool HasElseBlock(HighLevelIR.IfStatement ifStmt)
        {
            var elseBlock = ifStmt.GetElseBlock();
            return elseBlock != null && elseBlock.Count > 0;
        }

        private void ProcessIfWithElse(HighLevelIR.IfStatement ifStmt, MidLevelIR.MLFunction mlFunc, string thenLabel, string endLabel)
        {
            var elseLabel = $"else_{mlFunc.Instructions.Count}";
            mlFunc.Instructions.Add(new MidLevelIR.MLBranch { Target = elseLabel });
            mlFunc.Instructions.Add(new MidLevelIR.MLLabel { Name = thenLabel });
            
            var thenStatements = ifStmt.GetThenBlock().Cast<HighLevelIR.Statement>().ToList();
            if (thenStatements.Count > 0)
            {
                ProcessStatements(thenStatements, mlFunc);
            }
            
            mlFunc.Instructions.Add(new MidLevelIR.MLLabel { Name = endLabel });
        }

        private void ProcessIfWithoutElse(HighLevelIR.IfStatement ifStmt, MidLevelIR.MLFunction mlFunc, string thenLabel, string endLabel)
        {
            mlFunc.Instructions.Add(new MidLevelIR.MLBranch { Target = endLabel });
            mlFunc.Instructions.Add(new MidLevelIR.MLLabel { Name = thenLabel });
            
            var thenStatements = ifStmt.GetThenBlock().Cast<HighLevelIR.Statement>().ToList();
            if (thenStatements.Count > 0)
            {
                ProcessStatements(thenStatements, mlFunc);
            }
            
            mlFunc.Instructions.Add(new MidLevelIR.MLLabel { Name = endLabel });
        }

        private void ProcessWhileStatement(HighLevelIR.While whileStmt, MidLevelIR.MLFunction mlFunc)
        {
            // Process the loop body statements
            // While loop transformation with labels and branches will be implemented in a future iteration
            ProcessStatements(whileStmt.Body.Statements, mlFunc);
        }

        private static void ProcessReturnStatement(HighLevelIR.ReturnStatement returnStmt)
        {
            if (returnStmt.Value != null)
            {
                // For now, we ignore return values as MLIR doesn't have a concept of return values
            }
        }

        private string GetExpressionValue(HighLevelIR.Expression expr)
        {
            return expr switch
            {
                HighLevelIR.Literal literal => literal.Value?.ToString() ?? "0",
                HighLevelIR.Identifier ident => GetIdentifierValue(ident),
                HighLevelIR.BinaryOp op => GetBinaryOpValue(op),
                _ => "0"
            };
        }

        private string GetIdentifierValue(HighLevelIR.Identifier ident)
        {
            // Check if it's a constant in Globals
            if (_currentHlir != null && _currentHlir.Globals.TryGetValue(ident.Name, out var symbol) && symbol.IsConstant && symbol.InitialValue != null)
            {
                return symbol.InitialValue.ToString() ?? string.Empty;
            }
            return ident.Name;
        }

        private string GetBinaryOpValue(HighLevelIR.BinaryOp op)
        {
            var left = GetExpressionValue(op.Left);
            var right = GetExpressionValue(op.Right);

            var foldedResult = TryConstantFolding(left, right, op.Operator);
            return foldedResult ?? $"({left} {op.Operator} {right})";
        }

        private static string? TryConstantFolding(string left, string right, string op)
        {
            if (!int.TryParse(left, out int lVal) || !int.TryParse(right, out int rVal))
                return null;

            return op switch
            {
                "+" => (lVal + rVal).ToString(),
                "-" => (lVal - rVal).ToString(),
                "*" => (lVal * rVal).ToString(),
                "/" or "div" => rVal != 0 ? (lVal / rVal).ToString() : "0",
                _ => null
            };
        }
    }
}
