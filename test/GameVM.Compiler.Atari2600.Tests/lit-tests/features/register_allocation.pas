// RUN: %compile --input %s --output %t
// RUN: %bincheck %t %s
// CHECK: 00000000  a9 01 85 80

program RegisterAllocation;
var x, y, z: Integer;
begin
    x := 1;
    y := 2;
    z := x + y;
end.

