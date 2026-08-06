using System;
using GameVM.Compiler.Pascal;

var frontend = new PascalFrontend();
var source = "program Test;\nbegin\n  WriteLn('Hello');\nend.";
var astSlab = frontend.ParseToSlab(source);
var hlirSlab = frontend.ConvertToHlirSlab(astSlab);
var pool = frontend.StringPool;
Console.WriteLine($"SLAB COUNT: {hlirSlab.Count}");
for (int i = 0; i < hlirSlab.Count; i++)
{
    byte kind = hlirSlab.GetKind(i);
    var ops = hlirSlab.GetOperands(i);
    var str = new string[ops.Length];
    for (int j = 0; j < ops.Length; j++)
        str[j] = $"'{pool.Resolve(ops[j])}'";
    Console.WriteLine($"[{i}] kind=0x{kind:X} ops=[{string.Join(", ", str)}]");
}
