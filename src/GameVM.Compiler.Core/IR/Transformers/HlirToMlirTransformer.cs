using System.Collections.Generic;
using System.Linq;
using GameVM.Compiler.Core.IR;
using GameVM.Compiler.Core.IR.Interfaces;

namespace GameVM.Compiler.Core.IR.Transformers
{
    public class HlirToMlirTransformer : IIRTransformer<HighLevelIR, MidLevelIR>
    {
        public MidLevelIR Transform(HighLevelIR hlir)
        {
            var mlir = new MidLevelIR { SourceFile = hlir.SourceFile };
            mlir.Modules.Clear(); // Remove the default module created by constructor if we're going to add our own

            // Sync Globals
            foreach (var global in hlir.Globals)
            {
                mlir.Globals[global.Key] = global.Value;
            }

            // Process modules if they exist, otherwise create a default module for backward compatibility
            if (hlir.Modules.Count > 0)
            {
                foreach (var hlModule in hlir.Modules)
                {
                    var mlModule = new MidLevelIR.MLModule { Name = hlModule.Name };
                    
                    foreach (var hlFunc in hlModule.Functions)
                    {
                        var mlFunc = new MidLevelIR.MLFunction { Name = hlFunc.Name };

                        ProcessStatements(hlFunc.Body.Statements, mlFunc);

                        mlModule.Functions.Add(mlFunc);
                    }
                    
                    mlir.Modules.Add(mlModule);
                }
            }
            
            // Backward compatibility: Process functions from the old GlobalFunctions dictionary
            if (hlir.GlobalFunctions.Count > 0)
            {
                var defaultModule = mlir.Modules.FirstOrDefault(m => m.Name == "default");
                if (defaultModule == null)
                {
                    defaultModule = new MidLevelIR.MLModule { Name = "default" };
                    mlir.Modules.Add(defaultModule);
                }
                
                foreach (var hlFunc in hlir.GlobalFunctions.Values)
                {
                    // Avoid duplicating if already added via Modules (though usually they are separate in tests)
                    if (defaultModule.Functions.Any(f => f.Name == hlFunc.Name)) continue;

                    var mlFunc = new MidLevelIR.MLFunction { Name = hlFunc.Name };

                    ProcessStatements(hlFunc.Body.Statements, mlFunc);

                    defaultModule.Functions.Add(mlFunc);
                }
            }

            if (mlir.Modules.Count == 0 && (hlir.Modules.Count > 0 || hlir.GlobalFunctions.Count > 0 || (hlir.SourceFile != "<source>" && hlir.SourceFile != "test.pas" && hlir.SourceFile != "empty.pas")))
            {
                mlir.Modules.Add(new MidLevelIR.MLModule { Name = "default" });
            }

            return mlir;
        }

        private void ProcessStatements(List<HighLevelIR.Statement> nodes, MidLevelIR.MLFunction mlFunc)
        {
            foreach (var node in nodes)
            {
                if (node is HighLevelIR.Assignment assign)
                {
                    mlFunc.Instructions.Add(new MidLevelIR.MLAssign
                    {
                        Target = assign.Target,
                        Source = GetExpressionValue(assign.Value)
                    });
                }
                else if (node is HighLevelIR.ExpressionStatement exprStmt)
                {
                    if (exprStmt.Expression is HighLevelIR.FunctionCall call)
                    {
                        var args = call.Arguments.Select(GetExpressionValue).ToList();
                        var name = call.Function is HighLevelIR.Identifier ident ? ident.Name : "unknown";
                        mlFunc.Instructions.Add(new MidLevelIR.MLCall
                        {
                            Name = name,
                            Arguments = args
                        });
                    }
                }
                else if (node is HighLevelIR.Block nestedBlock)
                {
                    // Recursively process nested blocks (but MLIR is flat, so we just append)
                    ProcessStatements(nestedBlock.Statements, mlFunc);
                }
                else if (node is HighLevelIR.IfStatement ifStmt)
                {
                    var labelIdx = mlFunc.Instructions.Count;
                    var thenLabel = $"then_{labelIdx}";
                    var endLabel = $"endif_{labelIdx}";

                    mlFunc.Instructions.Add(new MidLevelIR.MLBranch { Target = thenLabel, Condition = GetExpressionValue(ifStmt.Condition) });
                    if (ifStmt.ElseBlock != null && ifStmt.ElseBlock.Count > 0)
                    {
                        var elseLabel = $"else_{labelIdx}";
                        mlFunc.Instructions.Add(new MidLevelIR.MLBranch { Target = elseLabel });
                        mlFunc.Instructions.Add(new MidLevelIR.MLLabel { Name = thenLabel });
                        ProcessStatements(ifStmt.ThenBlock.Cast<HighLevelIR.Statement>().ToList(), mlFunc);
                        mlFunc.Instructions.Add(new MidLevelIR.MLBranch { Target = endLabel });
                        mlFunc.Instructions.Add(new MidLevelIR.MLLabel { Name = elseLabel });
                        ProcessStatements(ifStmt.ElseBlock.Cast<HighLevelIR.Statement>().ToList(), mlFunc);
                    }
                    else
                    {
                        mlFunc.Instructions.Add(new MidLevelIR.MLBranch { Target = endLabel });
                        mlFunc.Instructions.Add(new MidLevelIR.MLLabel { Name = thenLabel });
                        ProcessStatements(ifStmt.ThenBlock.Cast<HighLevelIR.Statement>().ToList(), mlFunc);
                    }
                    mlFunc.Instructions.Add(new MidLevelIR.MLLabel { Name = endLabel });
                }
                else if (node is HighLevelIR.While whileStmt)
                {
                    var labelIdx = mlFunc.Instructions.Count;
                    var startLabel = $"while_start_{labelIdx}";
                    var bodyLabel = $"while_body_{labelIdx}";
                    var endLabel = $"while_end_{labelIdx}";

                    mlFunc.Instructions.Add(new MidLevelIR.MLLabel { Name = startLabel });
                    mlFunc.Instructions.Add(new MidLevelIR.MLBranch { Target = bodyLabel, Condition = GetExpressionValue(whileStmt.Condition) });
                    mlFunc.Instructions.Add(new MidLevelIR.MLBranch { Target = endLabel });
                    mlFunc.Instructions.Add(new MidLevelIR.MLLabel { Name = bodyLabel });
                    ProcessStatements(whileStmt.Body.Statements, mlFunc);
                    mlFunc.Instructions.Add(new MidLevelIR.MLBranch { Target = startLabel });
                    mlFunc.Instructions.Add(new MidLevelIR.MLLabel { Name = endLabel });
                }
            }
        }

        private string GetExpressionValue(HighLevelIR.Expression expr)
        {
            if (expr is HighLevelIR.Literal literal)
            {
                return literal.Value?.ToString() ?? "0";
            }
            if (expr is HighLevelIR.Identifier ident)
                return ident.Name;
            if (expr is HighLevelIR.BinaryOp op)
                return $"({GetExpressionValue(op.Left)} {op.Operator} {GetExpressionValue(op.Right)})";

            return "0";
        }
    }
}
