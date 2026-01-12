// RUN: %compile --input %s --output %t
// RUN: %bincheck %t %s
// CHECK: 00000000  00 00 00 00

program VariableScope;
var global: Integer;
procedure TestProc;
var local: Integer;
begin
    local := 42;
    global := local;
end;
begin
    TestProc;
end.

