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

            foreach (var hlFunc in hlir.Functions.Values)
            {
                var mlFunc = new MidLevelIR.MLFunction { Name = hlFunc.Name };

                if (hlFunc is HighLevelIR.Function hlFunction)
                {
                    ProcessStatements(hlFunction.Body.Statements, mlFunc);
                }

                mlir.Functions[mlFunc.Name] = mlFunc;
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
