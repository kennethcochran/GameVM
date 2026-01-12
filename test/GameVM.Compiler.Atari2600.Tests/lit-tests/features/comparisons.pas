// RUN: %compile --input %s --output %t
// RUN: %bincheck %t %s
// CHECK: 00000000  a9 85 80

program Comparisons;
var x, y: Integer;
var flag: Boolean;
begin
    flag := x = y;
    flag := x <> y;
    flag := x < y;
    flag := x > y;
    flag := x <= y;
    flag := x >= y;
end.

