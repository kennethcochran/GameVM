// RUN: %compile --input %s --output %t
// RUN: %bincheck %t %s
// CHECK: 00000000  00 00 00 00

program FunctionCalls;
function Add(a, b: Integer): Integer;
begin
    Add := a + b;
end;
begin
    writeln(Add(5, 3));
end.

