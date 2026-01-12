// RUN: %compile --input %s --output %t
// RUN: %bincheck %t %s
// CHECK: 00000000  a9 05 85 80

program Expressions;
var x, y, z: Integer;
begin
    x := 5;
    y := 3;
    z := (x + y) * 2 - 1;
end.

