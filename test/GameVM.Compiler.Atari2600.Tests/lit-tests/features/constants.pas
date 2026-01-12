// RUN: %compile --input %s --output %t
// RUN: %bincheck %t %s
// CHECK: 00000000  a9 00 85 80

program Constants;
const
    MAX = 100;
    MIN = 0;
var x: Integer;
begin
    x := MAX;
    x := MIN;
end.

